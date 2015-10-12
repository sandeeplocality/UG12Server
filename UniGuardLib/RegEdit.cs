using System;
using Microsoft.Win32;

namespace UniGuardLib
{
    public static class RegEdit
    {
        private static RegistryKey baseRegistryKey = Registry.LocalMachine;
        private static string subKey = @"SOFTWARE\UniGuard12Server";

        /// <summary>
        /// Reads a registry value
        /// </summary>
        /// <param name="keyName">Name of key</param>
        /// <returns>Value of key</returns>
        public static string Read(string keyName)
        {
            RegistryKey rk = RegEdit.baseRegistryKey;
            RegistryKey sk = rk.OpenSubKey(RegEdit.subKey);
            // If the reigstry sub key doesn't exit -> null
            if (sk == null) return null;
            else
            {
                try
                {
                    return (string)sk.GetValue(keyName.ToUpper());
                }
                catch (Exception ex)
                {
                    Log.Error("Registry read error:\r\n" + ex.ToString());
                    return null;
                }
            }
        }

        /// <summary>
        /// Write a registry value inside of keyName
        /// </summary>
        /// <param name="keyName">Name of key</param>
        /// <param name="value">Value of key</param>
        /// <returns></returns>
        public static bool Write(string keyName, object value)
        {
            try
            {
                RegistryKey rk = RegEdit.baseRegistryKey;
                // CreateSubKey because OpenSubKey opens as read-only
                RegistryKey sk = rk.CreateSubKey(RegEdit.subKey);
                // Save value
                sk.SetValue(keyName.ToUpper(), value);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Registry write error:\r\n" + ex.ToString());
                return false;
            }
        }

        public static bool Delete(string keyName)
        {
            try
            {
                RegistryKey rk = RegEdit.baseRegistryKey;
                // CreateSubKey because OpenSubKey opens as read-only
                RegistryKey sk = rk.CreateSubKey(RegEdit.subKey);
                // If it doesn't exit, just return true
                if (sk == null) return true;
                else
                {
                    sk.DeleteValue(keyName);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Registry delete error:\r\n" + ex.ToString());
                return false;
            }
        }

    }
}
