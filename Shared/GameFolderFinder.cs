using System;
using System.Configuration;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using Ookii.Dialogs.WinForms;

namespace RottrModManager.Shared
{
    public static class GameFolderFinder
    {
        public static string Find()
        {
            Func<string>[] getters =
            {
                GetGameFolderFromConfiguration,
                GetGameFolderFromRegistry,
                GetGameFolderFromFolderBrowser
            };
            foreach (Func<string> getter in getters)
            {
                string gameFolderPath = getter();
                if (string.IsNullOrEmpty(gameFolderPath) || !File.Exists(Path.Combine(gameFolderPath, "ROTTR.exe")))
                    continue;

                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["GameFolder"].Value = gameFolderPath;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
                return gameFolderPath;
            }

            return null;
        }

        private static string GetGameFolderFromConfiguration()
        {
            return ConfigurationManager.AppSettings["GameFolder"];
        }

        private static string GetGameFolderFromRegistry()
        {
            using RegistryKey uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstallKey == null)
                return null;

            foreach (string appName in uninstallKey.GetSubKeyNames())
            {
                using RegistryKey appKey = uninstallKey.OpenSubKey(appName);
                if (appKey?.GetValue("DisplayName") as string == "Rise of the Tomb Raider")
                    return appKey.GetValue("InstallLocation") as string;
            }

            return null;
        }

        private static string GetGameFolderFromFolderBrowser()
        {
            MessageBox.Show(
                "Could not automatically determine the Rise of the Tomb Raider installation folder. Please select it manually.",
                "ROTTR Mod Manager",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            while (true)
            {
                using VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
                if (dialog.ShowDialog() != DialogResult.OK)
                    return null;

                if (File.Exists(Path.Combine(dialog.SelectedPath, "ROTTR.exe")))
                    return dialog.SelectedPath;

                MessageBox.Show("Could not find ROTTR.exe in the selected folder.", "Game not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
