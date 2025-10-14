# Password UI Implementation Guide

## Overview
The `CertificateRequestViewModel` has been updated to use `SecureString` for password handling.
When implementing the UI, you MUST use `PasswordBox` controls (not `TextBox`) to maintain security.

## Required XAML Implementation

When you build out the `CertificateRequestView.xaml` or integrate password fields into `MainWindow.xaml`, use this pattern:

### XAML Code

```xml
<!-- PFX Password Field -->
<Label Content="PFX Password:" />
<PasswordBox x:Name="PfxPasswordBox"
             PasswordChanged="PfxPasswordBox_PasswordChanged"
             ToolTip="Enter a strong password to protect the certificate (min 8 chars, uppercase, lowercase, numbers)"/>

<!-- Password Strength Indicator -->
<TextBlock Text="{Binding PasswordStrength}" 
           Margin="5,0,0,0"
           FontStyle="Italic"/>

<!-- Confirm Password Field -->
<Label Content="Confirm Password:" />
<PasswordBox x:Name="ConfirmPasswordBox"
             PasswordChanged="ConfirmPasswordBox_PasswordChanged"
             ToolTip="Re-enter password to confirm"/>

<!-- Optional: Show Password Toggle (NOT RECOMMENDED for production) -->
<!-- Only include if absolutely necessary for UX -->
<CheckBox Content="Show Password" 
          IsChecked="{Binding ShowPassword}"
          Visibility="Collapsed"/> <!-- Hidden by default for security -->
```

## Required Code-Behind (CertificateRequestView.xaml.cs)

Add these event handlers to the code-behind file:

```csharp
using System.Windows;
using System.Windows.Controls;
using ZLGetCert.ViewModels;

namespace ZLGetCert.Views
{
    public partial class CertificateRequestView : UserControl
    {
        public CertificateRequestView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handle PFX password changes and update ViewModel with SecureString
        /// </summary>
        private void PfxPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as CertificateRequestViewModel;
            if (viewModel != null)
            {
                // SecurePassword property returns a SecureString - no conversion needed!
                viewModel.PfxPassword = ((PasswordBox)sender).SecurePassword;
            }
        }

        /// <summary>
        /// Handle confirm password changes and update ViewModel with SecureString
        /// </summary>
        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as CertificateRequestViewModel;
            if (viewModel != null)
            {
                // SecurePassword property returns a SecureString - no conversion needed!
                viewModel.ConfirmPassword = ((PasswordBox)sender).SecurePassword;
            }
        }
    }
}
```

## Security Notes

### ✅ DO:
- Use `PasswordBox` controls for password input
- Bind to `SecurePassword` property (already a `SecureString`)
- Clear password fields when form is reset
- Dispose ViewModel when form closes (already implemented)

### ❌ DON'T:
- Use `TextBox` for password input
- Convert `SecureString` to regular `string` in UI code
- Store passwords in plain text anywhere
- Use data binding for `Password` property (not supported by WPF for security reasons)

## Password Validation

The ViewModel automatically validates password strength. The `PasswordStrength` property returns:
- "No password set" - Empty password
- "Weak" - Less than 8 chars or missing required complexity
- "Medium" - 8+ chars with some complexity
- "Strong" - 12+ chars with full complexity

Display this to users to encourage strong passwords:

```xml
<TextBlock Text="{Binding PasswordStrength}">
    <TextBlock.Style>
        <Style TargetType="TextBlock">
            <Style.Triggers>
                <DataTrigger Binding="{Binding PasswordStrength}" Value="Weak">
                    <Setter Property="Foreground" Value="Red"/>
                </DataTrigger>
                <DataTrigger Binding="{Binding PasswordStrength}" Value="Medium">
                    <Setter Property="Foreground" Value="Orange"/>
                </DataTrigger>
                <DataTrigger Binding="{Binding PasswordStrength}" Value="Strong">
                    <Setter Property="Foreground" Value="Green"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </TextBlock.Style>
</TextBlock>
```

## Testing the Implementation

After implementing the UI:

1. **Test password entry:** Verify passwords are captured correctly
2. **Test password confirmation:** Ensure mismatched passwords prevent form submission
3. **Test strength indicator:** Enter various passwords and verify strength display
4. **Test clear/reset:** Verify passwords are cleared from memory
5. **Test form submission:** Verify certificate generation works with SecureString passwords

## Memory Security

The ViewModel implements `IDisposable` and automatically:
- Disposes `SecureString` objects when cleared
- Disposes `SecureString` objects when ViewModel is disposed
- Creates copies of `SecureString` when passing to certificate service

This ensures passwords are cleared from memory as soon as possible.

## Integration Checklist

When adding password UI:
- [ ] Add `PasswordBox` controls to XAML
- [ ] Add `PasswordChanged` event handlers to code-behind
- [ ] Wire up password strength indicator
- [ ] Test password entry and validation
- [ ] Test certificate generation with passwords
- [ ] Verify SecureStrings are disposed on form close
- [ ] Remove any `TextBox` controls used for passwords
- [ ] Update user documentation

---

**SECURITY WARNING:** Never use `TextBox` for password input. Always use `PasswordBox` to prevent passwords from being visible in XAML tree or debug tools.

**Last Updated:** October 14, 2025

