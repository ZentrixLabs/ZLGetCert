using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ZLGetCert.Views;

namespace ZLGetCert.ViewModels
{
    /// <summary>
    /// ViewModel for users guide window
    /// </summary>
    public class UsersGuideViewModel : BaseViewModel
    {
        public UsersGuideViewModel()
        {
            // Initialize commands
            PrintGuideCommand = new RelayCommand(PrintGuide);
            CloseCommand = new RelayCommand(CloseGuide);
        }

        /// <summary>
        /// Print guide command
        /// </summary>
        public ICommand PrintGuideCommand { get; }

        /// <summary>
        /// Close guide command
        /// </summary>
        public ICommand CloseCommand { get; }

        /// <summary>
        /// Print the users guide
        /// </summary>
        private void PrintGuide()
        {
            try
            {
                var window = Application.Current.Windows.OfType<UsersGuideView>().FirstOrDefault();
                if (window != null)
                {
                    var printDialog = new PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        printDialog.PrintVisual(window, "ZLGetCert Users Guide");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing guide: {ex.Message}", "Print Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Close the users guide
        /// </summary>
        private void CloseGuide()
        {
            var window = Application.Current.Windows.OfType<UsersGuideView>().FirstOrDefault();
            window?.Close();
        }
    }
}
