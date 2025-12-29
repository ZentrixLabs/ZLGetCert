using System;
using System.Collections.Generic;
using System.IO;
using ZentrixLabs.ZLGetCert.Core.Contracts;

namespace ZentrixLabs.ZLGetCert.Core.Doctor.Checks
{
    public sealed class ExportDestinationReadinessCheck : IDoctorCheck
    {
        public string Id => "export.destinations.ready";

        public DoctorCheckResult Run(DoctorContext context)
        {
            var request = context.Request;

            // If no exports are enabled, return pass
            if (request.Exports == null)
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "pass",
                    Summary = "No exports requested",
                    Evidence = new Dictionary<string, object>()
                };
            }

            var targetResults = new List<Dictionary<string, object>>();
            var failures = new List<string>();

            // Check each enabled export target
            CheckExportTarget(request.Exports.LeafPem, "leaf.pem", targetResults, failures, request.Overwrite);
            CheckExportTarget(request.Exports.KeyPem, "key.pem", targetResults, failures, request.Overwrite);
            CheckExportTarget(request.Exports.CaBundlePem, "ca-bundle.pem", targetResults, failures, request.Overwrite);

            var evidence = new Dictionary<string, object>
            {
                { "targets", targetResults }
            };

            // If any failures occurred, return fail result
            if (failures.Count > 0)
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.ExportError,
                    Summary = "Export destinations not ready",
                    Detail = string.Join("; ", failures),
                    Evidence = evidence,
                    Remediation = "Choose writable paths and set overwrite=true only if intentional."
                };
            }

            // All targets are ready
            return new DoctorCheckResult
            {
                Id = Id,
                Status = "pass",
                Summary = "All export destinations are ready",
                Evidence = evidence
            };
        }

        private void CheckExportTarget(ExportTarget target, string targetName, List<Dictionary<string, object>> targetResults, List<string> failures, bool overwriteAllowed)
        {
            var targetInfo = new Dictionary<string, object>
            {
                { "name", targetName }
            };

            // If target is not enabled, skip it but include basic info
            if (target == null || !target.Enabled)
            {
                targetInfo["enabled"] = false;
                targetResults.Add(targetInfo);
                return;
            }

            targetInfo["enabled"] = true;
            string inputPath = target.Path;
            targetInfo["inputPath"] = inputPath ?? "(null)";

            // Check path is non-empty
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                targetInfo["resolvedPath"] = "(invalid)";
                targetInfo["parentDir"] = "(invalid)";
                targetInfo["parentExists"] = false;
                targetInfo["writable"] = false;
                targetInfo["exists"] = false;
                targetInfo["overwriteAllowed"] = overwriteAllowed;
                targetResults.Add(targetInfo);
                failures.Add($"{targetName}: path is empty");
                return;
            }

            // Resolve to absolute path
            string resolvedPath;
            try
            {
                resolvedPath = Path.GetFullPath(inputPath);
            }
            catch (Exception ex)
            {
                targetInfo["resolvedPath"] = "(invalid)";
                targetInfo["parentDir"] = "(invalid)";
                targetInfo["parentExists"] = false;
                targetInfo["writable"] = false;
                targetInfo["exists"] = false;
                targetInfo["overwriteAllowed"] = overwriteAllowed;
                targetInfo["pathResolutionError"] = ex.Message;
                targetResults.Add(targetInfo);
                failures.Add($"{targetName}: cannot resolve path '{inputPath}' ({ex.Message})");
                return;
            }

            targetInfo["resolvedPath"] = resolvedPath;

            // Determine parent directory
            string parentDir = Path.GetDirectoryName(resolvedPath);
            targetInfo["parentDir"] = parentDir ?? "(no parent)";

            // Check parent directory exists
            bool parentExists = !string.IsNullOrWhiteSpace(parentDir) && Directory.Exists(parentDir);
            targetInfo["parentExists"] = parentExists;

            if (!parentExists)
            {
                targetInfo["writable"] = false;
                targetInfo["exists"] = false;
                targetInfo["overwriteAllowed"] = overwriteAllowed;
                targetResults.Add(targetInfo);
                failures.Add($"{targetName}: parent directory does not exist: {parentDir}");
                return;
            }

            // Check if target file exists
            bool fileExists = File.Exists(resolvedPath);
            targetInfo["exists"] = fileExists;

            // If file exists and overwrite is false, fail
            if (fileExists && !overwriteAllowed)
            {
                targetInfo["writable"] = false; // Don't check writability if we can't overwrite anyway
                targetInfo["overwriteAllowed"] = overwriteAllowed;
                targetResults.Add(targetInfo);
                failures.Add($"{targetName}: file exists and overwrite=false");
                return;
            }

            // Check writability by creating and deleting a temporary file in the parent directory
            bool writable = CheckDirectoryWritable(parentDir);
            targetInfo["writable"] = writable;
            targetInfo["overwriteAllowed"] = overwriteAllowed;

            if (!writable)
            {
                targetResults.Add(targetInfo);
                failures.Add($"{targetName}: parent directory is not writable: {parentDir}");
                return;
            }

            // All checks passed for this target
            targetResults.Add(targetInfo);
        }

        private bool CheckDirectoryWritable(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                return false;

            try
            {
                // Create a temporary file in the directory to test writability
                string tempFilePath = Path.Combine(directoryPath, Path.GetRandomFileName());
                try
                {
                    using (FileStream fs = File.Create(tempFilePath))
                    {
                        // File created successfully, directory is writable
                    }
                    // Clean up the temporary file
                    File.Delete(tempFilePath);
                    return true;
                }
                catch
                {
                    // If file creation fails, try to clean up if it was created
                    try
                    {
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

