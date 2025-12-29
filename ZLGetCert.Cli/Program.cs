// Built exe name: zlgetcert.exe
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using ZentrixLabs.ZLGetCert.Core.Contracts;
using ZentrixLabs.ZLGetCert.Core.Doctor;

namespace ZentrixLabs.ZLGetCert.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] != "doctor")
            {
                Console.Error.WriteLine("Usage: zlgetcert doctor --request <path> [--format text|json]");
                return 1;
            }

            // Parse arguments
            string requestPath = null;
            string format = "text";

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--request" && i + 1 < args.Length)
                {
                    requestPath = args[i + 1];
                    i++; // Skip the next argument as it's the value
                }
                else if (args[i] == "--format" && i + 1 < args.Length)
                {
                    format = args[i + 1].ToLowerInvariant();
                    if (format != "text" && format != "json")
                    {
                        Console.Error.WriteLine("Invalid format. Must be 'text' or 'json'.");
                        return 1;
                    }
                    i++; // Skip the next argument as it's the value
                }
            }

            // Validate --request is provided
            if (string.IsNullOrEmpty(requestPath))
            {
                Console.Error.WriteLine("Error: --request is required");
                return 2;
            }

            // Validate request file exists and can be read
            if (!File.Exists(requestPath))
            {
                if (format == "json")
                {
                    var errorResult = new DoctorResult
                    {
                        Status = "fail",
                        Passed = 0,
                        Failed = 1,
                        Warnings = 0,
                        Checks = new List<DoctorCheckResult>
                        {
                            new DoctorCheckResult
                            {
                                Id = "cli.file.read",
                                Status = "fail",
                                Category = FailureCategory.ConfigurationError,
                                Summary = "Request file not found",
                                Detail = $"The request file does not exist: {requestPath}",
                                Evidence = new Dictionary<string, object> { { "path", requestPath } },
                                Remediation = "Verify the file path is correct and the file exists."
                            }
                        }
                    };
                    Console.WriteLine(SerializeToJson(errorResult));
                }
                else
                {
                    Console.WriteLine("ConfigurationError: Request file not found");
                    Console.WriteLine($"  Path: {requestPath}");
                    Console.WriteLine("  Remediation: Verify the file path is correct and the file exists.");
                }
                return 2;
            }

            // Read and parse request file
            CertificateRequest request;
            try
            {
                string jsonContent = File.ReadAllText(requestPath);
                request = DeserializeRequest(jsonContent);
                if (request == null)
                {
                    throw new InvalidOperationException("Failed to deserialize request file");
                }
            }
            catch (Exception ex)
            {
                if (format == "json")
                {
                    var errorResult = new DoctorResult
                    {
                        Status = "fail",
                        Passed = 0,
                        Failed = 1,
                        Warnings = 0,
                        Checks = new List<DoctorCheckResult>
                        {
                            new DoctorCheckResult
                            {
                                Id = "cli.file.read",
                                Status = "fail",
                                Category = FailureCategory.ConfigurationError,
                                Summary = "Failed to read or parse request file",
                                Detail = ex.Message,
                                Evidence = new Dictionary<string, object> { { "path", requestPath }, { "error", ex.Message } },
                                Remediation = "Verify the file is valid JSON and matches the CertificateRequest schema."
                            }
                        }
                    };
                    Console.WriteLine(SerializeToJson(errorResult));
                }
                else
                {
                    Console.WriteLine("ConfigurationError: Failed to read or parse request file");
                    Console.WriteLine($"  Path: {requestPath}");
                    Console.WriteLine($"  Error: {ex.Message}");
                    Console.WriteLine("  Remediation: Verify the file is valid JSON and matches the CertificateRequest schema.");
                }
                return 2;
            }

            // Run doctor checks
            var checks = DefaultDoctorChecks.Create();
            var runner = new DoctorRunner(checks);
            var context = new DoctorContext(request, requestPath: requestPath);
            var result = runner.Run(context);

            // Output results
            if (format == "json")
            {
                Console.WriteLine(SerializeToJson(result));
            }
            else
            {
                PrintTextOutput(result);
            }

            // Return appropriate exit code
            return result.Status == "pass" ? 0 : 2;
        }

        static CertificateRequest DeserializeRequest(string json)
        {
            var serializer = new JavaScriptSerializer();
            var dict = serializer.Deserialize<Dictionary<string, object>>(json);

            var request = new CertificateRequest();

            // RequestId
            if (dict.ContainsKey("requestId"))
                request.RequestId = dict["requestId"]?.ToString();

            // OutputFormat
            if (dict.ContainsKey("outputFormat"))
                request.OutputFormat = dict["outputFormat"]?.ToString();

            // Mode
            if (dict.ContainsKey("mode"))
            {
                var modeStr = dict["mode"]?.ToString();
                if (string.Equals(modeStr, "newKeypair", StringComparison.OrdinalIgnoreCase))
                    request.Mode = RequestMode.NewKeypair;
                else if (string.Equals(modeStr, "signExistingCsr", StringComparison.OrdinalIgnoreCase))
                    request.Mode = RequestMode.SignExistingCsr;
            }

            // CsrPath
            if (dict.ContainsKey("csrPath"))
                request.CsrPath = dict["csrPath"]?.ToString();

            // AuthMode
            if (dict.ContainsKey("authMode"))
                request.AuthMode = dict["authMode"]?.ToString();

            // Overwrite
            if (dict.ContainsKey("overwrite"))
            {
                if (bool.TryParse(dict["overwrite"]?.ToString(), out bool overwrite))
                    request.Overwrite = overwrite;
            }

            // Subject
            if (dict.ContainsKey("commonName") || dict.ContainsKey("subjectDn") || dict.ContainsKey("subjectAlternativeNames"))
            {
                request.Subject = new SubjectIdentity();
                if (dict.ContainsKey("commonName"))
                    request.Subject.CommonName = dict["commonName"]?.ToString();
                if (dict.ContainsKey("subjectDn"))
                    request.Subject.SubjectDn = dict["subjectDn"]?.ToString();
                if (dict.ContainsKey("subjectAlternativeNames"))
                {
                    var sans = dict["subjectAlternativeNames"] as object[];
                    if (sans != null)
                    {
                        request.Subject.SubjectAlternativeNames = sans.Select(s => s?.ToString()).ToList();
                    }
                }
                if (dict.ContainsKey("wildcard"))
                {
                    if (bool.TryParse(dict["wildcard"]?.ToString(), out bool wildcard))
                        request.Subject.Wildcard = wildcard;
                }
            }

            // CA
            if (dict.ContainsKey("caConfig") || dict.ContainsKey("template"))
            {
                request.Ca = new CaTarget();
                if (dict.ContainsKey("template"))
                    request.Ca.Template = dict["template"]?.ToString();

                if (dict.ContainsKey("caConfig"))
                {
                    var caConfigDict = dict["caConfig"] as Dictionary<string, object>;
                    if (caConfigDict != null)
                    {
                        request.Ca.CaConfig = new CaConfig();
                        if (caConfigDict.ContainsKey("caServer"))
                            request.Ca.CaConfig.CaServer = caConfigDict["caServer"]?.ToString();
                        if (caConfigDict.ContainsKey("caName"))
                            request.Ca.CaConfig.CaName = caConfigDict["caName"]?.ToString();
                        if (caConfigDict.ContainsKey("configString"))
                            request.Ca.CaConfig.ConfigString = caConfigDict["configString"]?.ToString();
                    }
                }
            }

            // Crypto
            if (dict.ContainsKey("keyAlgorithm") || dict.ContainsKey("keySize") || dict.ContainsKey("hashAlgorithm"))
            {
                request.Crypto = new CryptoProfile();
                if (dict.ContainsKey("keyAlgorithm"))
                    request.Crypto.KeyAlgorithm = dict["keyAlgorithm"]?.ToString();
                if (dict.ContainsKey("keySize"))
                {
                    if (int.TryParse(dict["keySize"]?.ToString(), out int keySize))
                        request.Crypto.KeySize = keySize;
                }
                if (dict.ContainsKey("hashAlgorithm"))
                    request.Crypto.HashAlgorithm = dict["hashAlgorithm"]?.ToString();
                if (dict.ContainsKey("exportablePrivateKey"))
                {
                    if (bool.TryParse(dict["exportablePrivateKey"]?.ToString(), out bool exportable))
                        request.Crypto.ExportablePrivateKey = exportable;
                }
            }

            // Exports
            if (dict.ContainsKey("exports"))
            {
                var exportsDict = dict["exports"] as Dictionary<string, object>;
                if (exportsDict != null)
                {
                    request.Exports = new ExportPlan();
                    if (exportsDict.ContainsKey("leafPem"))
                    {
                        var leafPemDict = exportsDict["leafPem"] as Dictionary<string, object>;
                        if (leafPemDict != null)
                        {
                            request.Exports.LeafPem = new ExportTarget();
                            if (leafPemDict.ContainsKey("enabled"))
                            {
                                if (bool.TryParse(leafPemDict["enabled"]?.ToString(), out bool enabled))
                                    request.Exports.LeafPem.Enabled = enabled;
                            }
                            if (leafPemDict.ContainsKey("path"))
                                request.Exports.LeafPem.Path = leafPemDict["path"]?.ToString();
                        }
                    }
                    if (exportsDict.ContainsKey("keyPem"))
                    {
                        var keyPemDict = exportsDict["keyPem"] as Dictionary<string, object>;
                        if (keyPemDict != null)
                        {
                            request.Exports.KeyPem = new ExportTarget();
                            if (keyPemDict.ContainsKey("enabled"))
                            {
                                if (bool.TryParse(keyPemDict["enabled"]?.ToString(), out bool enabled))
                                    request.Exports.KeyPem.Enabled = enabled;
                            }
                            if (keyPemDict.ContainsKey("path"))
                                request.Exports.KeyPem.Path = keyPemDict["path"]?.ToString();
                        }
                    }
                    if (exportsDict.ContainsKey("caBundlePem"))
                    {
                        var caBundlePemDict = exportsDict["caBundlePem"] as Dictionary<string, object>;
                        if (caBundlePemDict != null)
                        {
                            request.Exports.CaBundlePem = new ExportTarget();
                            if (caBundlePemDict.ContainsKey("enabled"))
                            {
                                if (bool.TryParse(caBundlePemDict["enabled"]?.ToString(), out bool enabled))
                                    request.Exports.CaBundlePem.Enabled = enabled;
                            }
                            if (caBundlePemDict.ContainsKey("path"))
                                request.Exports.CaBundlePem.Path = caBundlePemDict["path"]?.ToString();
                        }
                    }
                }
            }

            return request;
        }

        static string SerializeToJson(DoctorResult result)
        {
            var serializer = new JavaScriptSerializer();
            var dict = new Dictionary<string, object>
            {
                ["status"] = result.Status,
                ["passed"] = result.Passed,
                ["failed"] = result.Failed,
                ["warnings"] = result.Warnings,
                ["checks"] = result.Checks?.Select(c => SerializeCheckResult(c)).ToArray() ?? new object[0]
            };
            return serializer.Serialize(dict);
        }

        static Dictionary<string, object> SerializeCheckResult(DoctorCheckResult check)
        {
            var dict = new Dictionary<string, object>
            {
                ["id"] = check.Id,
                ["status"] = check.Status
            };

            if (check.Category.HasValue)
                dict["category"] = check.Category.Value.ToString();

            if (!string.IsNullOrEmpty(check.Summary))
                dict["summary"] = check.Summary;

            if (!string.IsNullOrEmpty(check.Detail))
                dict["detail"] = check.Detail;

            // Serialize Evidence - convert Dictionary<string, object> to a serializable format
            if (check.Evidence != null && check.Evidence.Count > 0)
            {
                // Convert Evidence to a format that JavaScriptSerializer can handle
                var evidenceDict = new Dictionary<string, object>();
                foreach (var kvp in check.Evidence)
                {
                    // Handle common types
                    if (kvp.Value == null)
                        evidenceDict[kvp.Key] = null;
                    else if (kvp.Value is string || kvp.Value is int || kvp.Value is bool || kvp.Value is double)
                        evidenceDict[kvp.Key] = kvp.Value;
                    else if (kvp.Value is object[])
                        evidenceDict[kvp.Key] = kvp.Value;
                    else
                        evidenceDict[kvp.Key] = kvp.Value.ToString();
                }
                dict["evidence"] = evidenceDict;
            }

            if (!string.IsNullOrEmpty(check.Remediation))
                dict["remediation"] = check.Remediation;

            return dict;
        }

        static void PrintTextOutput(DoctorResult result)
        {
            Console.WriteLine($"Doctor Status: {result.Status.ToUpperInvariant()}");
            Console.WriteLine($"  Passed: {result.Passed}");
            Console.WriteLine($"  Failed: {result.Failed}");
            Console.WriteLine($"  Warnings: {result.Warnings}");
            Console.WriteLine();

            if (result.Checks != null && result.Checks.Count > 0)
            {
                foreach (var check in result.Checks)
                {
                    string statusPrefix = check.Status.ToUpperInvariant();
                    Console.WriteLine($"{statusPrefix} {check.Id} - {check.Summary}");

                    if ((check.Status == "fail" || check.Status == "warn") && !string.IsNullOrEmpty(check.Remediation))
                    {
                        Console.WriteLine($"  {check.Remediation}");
                    }
                }
            }
        }
    }
}

