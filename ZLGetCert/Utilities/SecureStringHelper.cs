using System;
using System.Security;
using System.Text;

namespace ZLGetCert.Utilities
{
    /// <summary>
    /// Helper utilities for SecureString operations
    /// </summary>
    public static class SecureStringHelper
    {
        /// <summary>
        /// Convert SecureString to string
        /// </summary>
        public static string SecureStringToString(SecureString secureString)
        {
            if (secureString == null)
                return string.Empty;

            var ptr = IntPtr.Zero;
            try
            {
                ptr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
        }

        /// <summary>
        /// Convert string to SecureString
        /// </summary>
        public static SecureString StringToSecureString(string plainString)
        {
            if (string.IsNullOrEmpty(plainString))
                return null;

            var secureString = new SecureString();
            foreach (char c in plainString)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();
            return secureString;
        }

        /// <summary>
        /// Create a copy of a SecureString
        /// </summary>
        public static SecureString CopySecureString(SecureString source)
        {
            if (source == null)
                return null;

            var plainString = SecureStringToString(source);
            return StringToSecureString(plainString);
        }

        /// <summary>
        /// Extension method to copy a SecureString
        /// </summary>
        public static SecureString Copy(this SecureString source)
        {
            return CopySecureString(source);
        }

        /// <summary>
        /// Clear a SecureString from memory
        /// </summary>
        public static void ClearSecureString(SecureString secureString)
        {
            if (secureString != null && !secureString.IsReadOnly())
            {
                secureString.Clear();
            }
        }

        /// <summary>
        /// Check if two SecureStrings are equal
        /// </summary>
        public static bool SecureStringEquals(SecureString secureString1, SecureString secureString2)
        {
            if (secureString1 == null && secureString2 == null)
                return true;
            if (secureString1 == null || secureString2 == null)
                return false;

            var str1 = SecureStringToString(secureString1);
            var str2 = SecureStringToString(secureString2);
            return str1 == str2;
        }

        /// <summary>
        /// Get length of SecureString
        /// </summary>
        public static int GetSecureStringLength(SecureString secureString)
        {
            return secureString?.Length ?? 0;
        }

        /// <summary>
        /// Validate password strength
        /// </summary>
        public static PasswordStrength ValidatePasswordStrength(SecureString password)
        {
            if (password == null || password.Length == 0)
                return PasswordStrength.Empty;

            var plainPassword = SecureStringToString(password);
            var length = plainPassword.Length;

            if (length < 8)
                return PasswordStrength.Weak;

            var hasUpper = false;
            var hasLower = false;
            var hasDigit = false;
            var hasSpecial = false;

            foreach (char c in plainPassword)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else if (char.IsPunctuation(c) || char.IsSymbol(c)) hasSpecial = true;
            }

            var score = 0;
            if (hasUpper) score++;
            if (hasLower) score++;
            if (hasDigit) score++;
            if (hasSpecial) score++;

            if (length >= 12 && score >= 3)
                return PasswordStrength.Strong;
            if (length >= 8 && score >= 2)
                return PasswordStrength.Medium;
            
            return PasswordStrength.Weak;
        }
    }

    /// <summary>
    /// Password strength levels
    /// </summary>
    public enum PasswordStrength
    {
        Empty,
        Weak,
        Medium,
        Strong
    }
}
