using System;
using System.Windows;
using ZLGetCert.Services;

namespace ZLGetCert
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Initialize logging
                var logger = LoggingService.Instance;
                logger.LogInfo("ZLGetCert application starting");

                // Ensure required directories exist
                var fileService = FileManagementService.Instance;
                fileService.EnsureCertificateFolderExists();
                fileService.EnsureLogFolderExists();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start application: {ex.Message}", "Startup Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                var logger = LoggingService.Instance;
                logger.LogInfo("ZLGetCert application shutting down");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
            }

            base.OnExit(e);
        }
    }
}
