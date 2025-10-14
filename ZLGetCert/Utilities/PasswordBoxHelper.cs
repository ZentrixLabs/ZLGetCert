using System;
using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace ZLGetCert.Utilities
{
    /// <summary>
    /// Helper class to enable binding SecureString to PasswordBox
    /// </summary>
    public static class PasswordBoxHelper
    {
        private static bool _isUpdating = false;

        // Attached property for binding SecureString to PasswordBox
        public static readonly DependencyProperty SecurePasswordProperty =
            DependencyProperty.RegisterAttached(
                "SecurePassword",
                typeof(SecureString),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSecurePasswordChanged));

        // Attached property to enable the binding behavior
        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached(
                "Attach",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnAttachChanged));

        public static SecureString GetSecurePassword(DependencyObject obj)
        {
            return (SecureString)obj.GetValue(SecurePasswordProperty);
        }

        public static void SetSecurePassword(DependencyObject obj, SecureString value)
        {
            obj.SetValue(SecurePasswordProperty, value);
        }

        public static bool GetAttach(DependencyObject obj)
        {
            return (bool)obj.GetValue(AttachProperty);
        }

        public static void SetAttach(DependencyObject obj, bool value)
        {
            obj.SetValue(AttachProperty, value);
        }

        private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                if ((bool)e.NewValue)
                {
                    passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                }
                else
                {
                    passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                }
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdating) return;

            var passwordBox = (PasswordBox)sender;
            _isUpdating = true;
            
            try
            {
                // Dispose old SecureString
                var oldPassword = GetSecurePassword(passwordBox);
                oldPassword?.Dispose();
                
                // Set new SecureString
                SetSecurePassword(passwordBox, passwordBox.SecurePassword.Copy());
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private static void OnSecurePasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (_isUpdating) return;

            if (d is PasswordBox passwordBox)
            {
                _isUpdating = true;
                
                try
                {
                    var newPassword = e.NewValue as SecureString;
                    
                    // Only update if password is different
                    if (newPassword == null || newPassword.Length == 0)
                    {
                        passwordBox.Password = string.Empty;
                    }
                    else
                    {
                        // Convert SecureString to plain text for PasswordBox
                        var plainPassword = SecureStringHelper.SecureStringToString(newPassword);
                        if (passwordBox.Password != plainPassword)
                        {
                            passwordBox.Password = plainPassword;
                        }
                    }
                }
                finally
                {
                    _isUpdating = false;
                }
            }
        }
    }
}

