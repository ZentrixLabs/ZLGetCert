using System;
using System.IO;
using ZLGetCert.Models;

namespace ZLGetCert.Services
{
    /// <summary>
    /// Service for file management and cleanup operations
    /// </summary>
    public class FileManagementService
    {
        private static readonly Lazy<FileManagementService> _instance = new Lazy<FileManagementService>(() => new FileManagementService());
        public static FileManagementService Instance => _instance.Value;

        private readonly LoggingService _logger;
        private readonly ConfigurationService _configService;

        private FileManagementService()
        {
            _logger = LoggingService.Instance;
            _configService = ConfigurationService.Instance;
        }

        /// <summary>
        /// Ensure directory exists
        /// </summary>
        public bool EnsureDirectoryExists(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    _logger.LogInfo("Created directory: {0}", path);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating directory: {0}", path);
                return false;
            }
        }

        /// <summary>
        /// Ensure certificate folder exists
        /// </summary>
        public bool EnsureCertificateFolderExists()
        {
            var config = _configService.GetConfiguration();
            return EnsureDirectoryExists(config.FilePaths.CertificateFolder);
        }

        /// <summary>
        /// Ensure log folder exists
        /// </summary>
        public bool EnsureLogFolderExists()
        {
            var config = _configService.GetConfiguration();
            return EnsureDirectoryExists(config.FilePaths.LogPath);
        }

        /// <summary>
        /// Clean up temporary files
        /// </summary>
        public void CleanupTemporaryFiles(string basePath, string certificateName)
        {
            try
            {
                var filesToDelete = new[]
                {
                    Path.Combine(basePath, $"{certificateName}.inf"),
                    Path.Combine(basePath, $"{certificateName}.csr"),
                    Path.Combine(basePath, $"{certificateName}.rsp")
                };

                foreach (var file in filesToDelete)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        _logger.LogDebug("Deleted temporary file: {0}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error cleaning up temporary files for {0}: {1}", certificateName, ex.Message);
            }
        }

        /// <summary>
        /// Clean up old log files
        /// </summary>
        public void CleanupOldLogFiles()
        {
            try
            {
                var config = _configService.GetConfiguration();
                var logPath = config.FilePaths.LogPath;

                if (!Directory.Exists(logPath))
                    return;

                var logFiles = Directory.GetFiles(logPath, "*.log");
                var maxFiles = config.Logging.MaxLogFiles;

                if (logFiles.Length > maxFiles)
                {
                    Array.Sort(logFiles, (x, y) => File.GetCreationTime(x).CompareTo(File.GetCreationTime(y)));

                    for (int i = 0; i < logFiles.Length - maxFiles; i++)
                    {
                        File.Delete(logFiles[i]);
                        _logger.LogDebug("Deleted old log file: {0}", logFiles[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error cleaning up old log files: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Get file size in human readable format
        /// </summary>
        public string GetFileSizeString(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return "File not found";

                var fileInfo = new FileInfo(filePath);
                var bytes = fileInfo.Length;

                if (bytes < 1024)
                    return $"{bytes} B";
                if (bytes < 1024 * 1024)
                    return $"{bytes / 1024.0:F1} KB";
                if (bytes < 1024 * 1024 * 1024)
                    return $"{bytes / (1024.0 * 1024.0):F1} MB";
                
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file size for {0}", filePath);
                return "Unknown";
            }
        }

        /// <summary>
        /// Check if file exists and is accessible
        /// </summary>
        public bool IsFileAccessible(string filePath)
        {
            try
            {
                return File.Exists(filePath) && File.GetAttributes(filePath) != FileAttributes.Directory;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get certificate file paths for a given certificate name
        /// </summary>
        public CertificateFilePaths GetCertificateFilePaths(string certificateName)
        {
            var config = _configService.GetConfiguration();
            var basePath = config.FilePaths.CertificateFolder;

            return new CertificateFilePaths
            {
                CerPath = Path.Combine(basePath, $"{certificateName}.cer"),
                PfxPath = Path.Combine(basePath, $"{certificateName}.pfx"),
                PemPath = Path.Combine(basePath, $"{certificateName}.pem"),
                KeyPath = Path.Combine(basePath, $"{certificateName}.key"),
                ChainPath = Path.Combine(basePath, "certificate-chain.pem")
            };
        }

        /// <summary>
        /// Validate certificate files exist
        /// </summary>
        public CertificateFileStatus ValidateCertificateFiles(string certificateName)
        {
            var paths = GetCertificateFilePaths(certificateName);

            return new CertificateFileStatus
            {
                CerExists = IsFileAccessible(paths.CerPath),
                PfxExists = IsFileAccessible(paths.PfxPath),
                PemExists = IsFileAccessible(paths.PemPath),
                KeyExists = IsFileAccessible(paths.KeyPath),
                ChainExists = IsFileAccessible(paths.ChainPath)
            };
        }
    }

    /// <summary>
    /// Certificate file paths
    /// </summary>
    public class CertificateFilePaths
    {
        public string CerPath { get; set; }
        public string PfxPath { get; set; }
        public string PemPath { get; set; }
        public string KeyPath { get; set; }
        public string ChainPath { get; set; }
    }

    /// <summary>
    /// Certificate file status
    /// </summary>
    public class CertificateFileStatus
    {
        public bool CerExists { get; set; }
        public bool PfxExists { get; set; }
        public bool PemExists { get; set; }
        public bool KeyExists { get; set; }
        public bool ChainExists { get; set; }

        public bool HasBasicFiles => CerExists && PfxExists;
        public bool HasPemFiles => PemExists && KeyExists;
        public bool HasCompleteFiles => HasBasicFiles && HasPemFiles && ChainExists;
    }
}
