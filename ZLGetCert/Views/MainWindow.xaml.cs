using System.Windows;
using ZLGetCert.ViewModels;
using ZLGetCert.Utilities;

namespace ZLGetCert.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            
            // Set window title with dynamic version from git tags or assembly
            var version = VersionHelper.GetVersion();
            Title = $"ZLGetCert v{version} - Certificate Management";
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }

        private void ClearCSR_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CertificateRequest.CsrFilePath = "";
            }
        }
    }
}
