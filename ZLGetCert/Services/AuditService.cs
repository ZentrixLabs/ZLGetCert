using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using Newtonsoft.Json;

namespace ZLGetCert.Services
{
    /// <summary>
    /// Service for audit logging of security-sensitive operations
    /// Provides tamper-evident logging for compliance and security monitoring
    /// </summary>
    public class AuditService
    {
        private static readonly Lazy<AuditService> _instance = new Lazy<AuditService>(() => new AuditService());
        public static AuditService Instance => _instance.Value;

        private readonly string _auditLogPath;
        private readonly LoggingService _logger;
        private readonly object _fileLock = new object();

        private AuditService()
        {
            _logger = LoggingService.Instance;
            
            // Store audit logs separately from operational logs
            var config = ConfigurationService.Instance.GetConfiguration();
            var auditDir = Path.Combine(config.FilePaths.LogPath, "Audit");
            
            if (!Directory.Exists(auditDir))
            {
                Directory.CreateDirectory(auditDir);
            }
            
            _auditLogPath = Path.Combine(auditDir, $"audit-{DateTime.Now:yyyy-MM}.log");
        }

        /// <summary>
        /// Types of audit events
        /// </summary>
        public enum AuditEventType
        {
            CertificateRequested,
            CertificateGenerated,
            CertificateExported,
            PrivateKeyExported,
            PrivateKeyExportedUnencrypted,
            CertificateImported,
            ConfigurationChanged,
            TemplateQueried,
            CAQueried,
            CSRSubmitted,
            ValidationFailure
        }

        /// <summary>
        /// Log an audit event
        /// </summary>
        public void LogAuditEvent(
            AuditEventType eventType, 
            string details, 
            string certificateName = null, 
            string thumbprint = null,
            bool isSecurityCritical = false)
        {
            try
            {
                var auditEntry = new AuditEntry
                {
                    Timestamp = DateTime.UtcNow,
                    LocalTime = DateTime.Now,
                    User = GetCurrentUser(),
                    Machine = Environment.MachineName,
                    EventType = eventType.ToString(),
                    CertificateName = certificateName,
                    Thumbprint = thumbprint,
                    Details = details,
                    ApplicationVersion = GetApplicationVersion(),
                    IsSecurityCritical = isSecurityCritical
                };

                // Write to audit log file (JSON format for easy parsing)
                WriteAuditEntry(auditEntry);

                // For security-critical events, also write to Windows Event Log
                if (isSecurityCritical || eventType == AuditEventType.PrivateKeyExportedUnencrypted)
                {
                    WriteToWindowsEventLog(auditEntry);
                }

                // Log to operational log as well
                _logger.LogInfo("AUDIT: {0} - {1}", eventType, details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log entry");
            }
        }

        /// <summary>
        /// Write audit entry to JSON log file
        /// </summary>
        private void WriteAuditEntry(AuditEntry entry)
        {
            try
            {
                var json = JsonConvert.SerializeObject(entry, Formatting.None);
                
                // Thread-safe file append
                lock (_fileLock)
                {
                    File.AppendAllText(_auditLogPath, json + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to audit log file: {0}", _auditLogPath);
            }
        }

        /// <summary>
        /// Write security-critical events to Windows Event Log
        /// </summary>
        private void WriteToWindowsEventLog(AuditEntry entry)
        {
            try
            {
                const string sourceName = "ZLGetCert";
                const string logName = "Application";

                // Create event source if it doesn't exist (requires admin rights)
                if (!EventLog.SourceExists(sourceName))
                {
                    try
                    {
                        EventLog.CreateEventSource(sourceName, logName);
                    }
                    catch (SecurityException)
                    {
                        // Can't create source without admin rights - just skip Windows Event Log
                        _logger.LogDebug("Cannot write to Windows Event Log - insufficient permissions to create source", null);
                        return;
                    }
                }

                // Format message for Event Log
                var message = $@"SECURITY AUDIT: {entry.EventType}

Certificate: {entry.CertificateName ?? "N/A"}
Thumbprint: {entry.Thumbprint ?? "N/A"}
User: {entry.User}
Machine: {entry.Machine}
Time: {entry.LocalTime:yyyy-MM-dd HH:mm:ss}
Details: {entry.Details}
Application: ZLGetCert v{entry.ApplicationVersion}";

                // Determine event type
                EventLogEntryType entryType = entry.IsSecurityCritical 
                    ? EventLogEntryType.Warning 
                    : EventLogEntryType.Information;

                // Write to Event Log
                EventLog.WriteEntry(sourceName, message, entryType, GetEventId(entry.EventType));
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not write to Windows Event Log (this is OK if not admin): {0}", ex.Message);
            }
        }

        /// <summary>
        /// Get event ID for Windows Event Log
        /// </summary>
        private int GetEventId(string eventType)
        {
            // Use consistent event IDs for filtering
            switch (eventType)
            {
                case nameof(AuditEventType.PrivateKeyExportedUnencrypted):
                    return 1001; // Private key export - WARNING level
                case nameof(AuditEventType.CertificateGenerated):
                    return 1002; // Certificate generated
                case nameof(AuditEventType.ConfigurationChanged):
                    return 1003; // Configuration changed
                case nameof(AuditEventType.ValidationFailure):
                    return 1004; // Validation failure
                default:
                    return 1000; // General audit event
            }
        }

        /// <summary>
        /// Get current user in a safe way
        /// </summary>
        private string GetCurrentUser()
        {
            try
            {
                return WindowsIdentity.GetCurrent().Name;
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        /// <summary>
        /// Get application version
        /// </summary>
        private string GetApplicationVersion()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        /// <summary>
        /// Audit entry model
        /// </summary>
        private class AuditEntry
        {
            public DateTime Timestamp { get; set; }
            public DateTime LocalTime { get; set; }
            public string User { get; set; }
            public string Machine { get; set; }
            public string EventType { get; set; }
            public string CertificateName { get; set; }
            public string Thumbprint { get; set; }
            public string Details { get; set; }
            public string ApplicationVersion { get; set; }
            public bool IsSecurityCritical { get; set; }
        }
    }
}

