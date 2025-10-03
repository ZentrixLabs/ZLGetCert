using System.Windows;
using ZLGetCert.ViewModels;

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
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ZLGetCert - Certificate Management for On-Premises CA\n\nVersion 1.0", "About ZLGetCert", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
