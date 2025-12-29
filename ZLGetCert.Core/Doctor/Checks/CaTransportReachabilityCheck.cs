using System;
using System.Collections.Generic;
using System.Net.Sockets;
using ZentrixLabs.ZLGetCert.Core.Contracts;
using ZentrixLabs.ZLGetCert.Core.Doctor;

namespace ZentrixLabs.ZLGetCert.Core.Doctor.Checks
{
    public sealed class CaTransportReachabilityCheck : IDoctorCheck
    {
        public string Id => "ca.transport";

        public DoctorCheckResult Run(DoctorContext context)
        {
            var request = context.Request;
            if (request?.Ca?.CaConfig == null)
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.ConfigurationError,
                    Summary = "CA server not specified",
                    Remediation = "Set ca.caConfig.caServer (or configString) in the request."
                };
            }

            var caServer = request.Ca.CaConfig.CaServer;
            if (string.IsNullOrWhiteSpace(caServer))
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.ConfigurationError,
                    Summary = "CA server not specified",
                    Remediation = "Set ca.caConfig.caServer (or configString) in the request."
                };
            }

            var port = request.Ca.CaConfig.Port;
            if (!port.HasValue)
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "warn",
                    Category = FailureCategory.ConnectivityError,
                    Summary = "CA transport reachability not verified",
                    Detail = "No CA port was provided; transport check is skipped to avoid guessing.",
                    Evidence = new Dictionary<string, object>
                    {
                        { "caServer", caServer },
                        { "port", (object)null }
                    },
                    Remediation = "Set ca.caConfig.port in the request to enable a TCP reachability check."
                };
            }

            // Attempt TCP connect with timeout
            TcpClient client = null;
            try
            {
                client = new TcpClient();
                var result = client.BeginConnect(caServer, port.Value, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                
                if (success)
                {
                    try
                    {
                        client.EndConnect(result);
                        if (client.Connected)
                        {
                            return new DoctorCheckResult
                            {
                                Id = Id,
                                Status = "pass",
                                Summary = "CA service transport reachable",
                                Evidence = new Dictionary<string, object>
                                {
                                    { "caServer", caServer },
                                    { "port", port.Value },
                                    { "connected", true }
                                }
                            };
                        }
                    }
                    catch (Exception connectEx)
                    {
                        return new DoctorCheckResult
                        {
                            Id = Id,
                            Status = "fail",
                            Category = FailureCategory.ConnectivityError,
                            Summary = "Unable to reach CA service",
                            Evidence = new Dictionary<string, object>
                            {
                                { "caServer", caServer },
                                { "port", port.Value },
                                { "connected", false },
                                { "errorType", connectEx.GetType().Name }
                            },
                            Remediation = "Verify routing/firewall and that the CA service is reachable on the specified port."
                        };
                    }
                }
                
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.ConnectivityError,
                    Summary = "Unable to reach CA service",
                    Evidence = new Dictionary<string, object>
                    {
                        { "caServer", caServer },
                        { "port", port.Value },
                        { "connected", false },
                        { "errorType", "Timeout" }
                    },
                    Remediation = "Verify routing/firewall and that the CA service is reachable on the specified port."
                };
            }
            catch (Exception ex)
            {
                return new DoctorCheckResult
                {
                    Id = Id,
                    Status = "fail",
                    Category = FailureCategory.ConnectivityError,
                    Summary = "Unable to reach CA service",
                    Evidence = new Dictionary<string, object>
                    {
                        { "caServer", caServer },
                        { "port", port.Value },
                        { "connected", false },
                        { "errorType", ex.GetType().Name }
                    },
                    Remediation = "Verify routing/firewall and that the CA service is reachable on the specified port."
                };
            }
            finally
            {
                if (client != null)
                {
                    try
                    {
                        client.Close();
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }
}

