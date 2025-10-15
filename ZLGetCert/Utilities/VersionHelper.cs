using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace ZLGetCert.Utilities
{
    /// <summary>
    /// Helper class for retrieving version information from git tags or assembly
    /// </summary>
    public static class VersionHelper
    {
        private static string _cachedVersion;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the current version from git tags or assembly version
        /// </summary>
        public static string GetVersion()
        {
            if (_cachedVersion != null)
                return _cachedVersion;

            lock (_lock)
            {
                if (_cachedVersion != null)
                    return _cachedVersion;

                _cachedVersion = GetVersionInternal();
                return _cachedVersion;
            }
        }

        private static string GetVersionInternal()
        {
            // First, try to get assembly version (this will be stamped by the build process)
            var assemblyVersion = GetAssemblyVersion();
            if (!string.IsNullOrEmpty(assemblyVersion) && assemblyVersion != "Unknown" && !assemblyVersion.StartsWith("0.0"))
                return assemblyVersion;

            // Only try git if assembly version is not available or is default
            try
            {
                var gitVersion = GetGitVersion();
                if (!string.IsNullOrEmpty(gitVersion))
                    return gitVersion;
            }
            catch (Exception ex)
            {
                // Log the exception but don't throw - fall back to assembly version
                System.Diagnostics.Debug.WriteLine($"Failed to get git version: {ex.Message}");
            }

            // Final fallback to assembly version
            return assemblyVersion;
        }

        private static string GetGitVersion()
        {
            try
            {
                // Get the directory containing the executable
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var projectDirectory = Path.GetDirectoryName(assemblyLocation);

                // Navigate up to find the git repository root
                var gitRoot = FindGitRoot(projectDirectory);
                if (string.IsNullOrEmpty(gitRoot))
                    return null;

                // Run git describe to get the latest tag
                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "describe --tags --abbrev=0",
                    WorkingDirectory = gitRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                        return null;

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    
                    process.WaitForExit(5000); // 5 second timeout

                    if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        var version = output.Trim();
                        // Remove 'v' prefix if present
                        if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                            version = version.Substring(1);
                        return version;
                    }
                }
            }
            catch
            {
                // Ignore exceptions and fall back to assembly version
            }

            return null;
        }

        private static string FindGitRoot(string startDirectory)
        {
            var currentDir = startDirectory;
            
            while (!string.IsNullOrEmpty(currentDir))
            {
                var gitDir = Path.Combine(currentDir, ".git");
                if (Directory.Exists(gitDir))
                    return currentDir;

                var parentDir = Directory.GetParent(currentDir)?.FullName;
                if (parentDir == currentDir) // Reached root
                    break;

                currentDir = parentDir;
            }

            return null;
        }

        private static string GetAssemblyVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString(3) ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Gets the full version information including git commit if available
        /// </summary>
        public static string GetFullVersionInfo()
        {
            var version = GetVersion();
            var gitCommit = GetGitCommitHash();
            
            if (!string.IsNullOrEmpty(gitCommit))
            {
                return $"{version} (commit: {gitCommit.Substring(0, Math.Min(8, gitCommit.Length))})";
            }

            return version;
        }

        private static string GetGitCommitHash()
        {
            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var projectDirectory = Path.GetDirectoryName(assemblyLocation);
                var gitRoot = FindGitRoot(projectDirectory);
                
                if (string.IsNullOrEmpty(gitRoot))
                    return null;

                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse HEAD",
                    WorkingDirectory = gitRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                        return null;

                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);

                    if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        return output.Trim();
                    }
                }
            }
            catch
            {
                // Ignore exceptions
            }

            return null;
        }
    }
}
