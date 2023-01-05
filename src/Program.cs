using System;
using System.Configuration;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Ookii.Dialogs.WinForms;
using RottrModManager.Cdc;
using RottrModManager.Mod;

namespace RottrModManager
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string gameFolderPath = GetGameFolder();
            if (gameFolderPath == null)
                return;

            if (args.Length == 1)
            {
                HandleCommandLine(gameFolderPath, args[0]);
                return;
            }
            
            Application.Run(new MainForm(gameFolderPath));
        }

        private static string GetGameFolder()
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

        private static void HandleCommandLine(string gameFolderPath, string modPath)
        {
            using ArchiveSet archiveSet = new ArchiveSet(gameFolderPath);
            ResourceUsageCache resourceUsageCache = new ResourceUsageCache(archiveSet);

            try
            {
                if (!resourceUsageCache.Load())
                {
                    RunTaskWithProgress(resourceUsageCache.Refresh);
                    resourceUsageCache.Save();

                    ModInstaller installer = new ModInstaller(archiveSet, resourceUsageCache);
                    RunTaskWithProgress((progress, cancellationToken) => installer.ReinstallAll(progress, cancellationToken));
                }

                InstallMod(archiveSet, resourceUsageCache, modPath);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private static void InstallMod(ArchiveSet archiveSet, ResourceUsageCache resourceUsageCache, string modPath)
        {
            ModInstaller installer = new ModInstaller(archiveSet, resourceUsageCache);
            if (File.Exists(modPath))
            {
                string extension = Path.GetExtension(modPath);
                if (extension != ".7z" && extension != ".zip" && extension != ".rar")
                {
                    MessageBox.Show(
                        "Only .zip and .7z files are supported for direct mod installation. Please extract the archive and install the folder instead.",
                        "File not supported",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation
                    );
                    return;
                }
                RunTaskWithProgress((progress, cancellationToken) => installer.InstallFromZip(modPath, progress, cancellationToken));
            }
            else if (Directory.Exists(modPath))
            {
                RunTaskWithProgress((progress, cancellationToken) => installer.InstallFromFolder(modPath, progress, cancellationToken));
            }
            else
            {
                MessageBox.Show("The specified mod path does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private static void RunTaskWithProgress(Action<ITaskProgress, CancellationToken> action)
        {
            using TaskProgressForm progressForm = new TaskProgressForm();
            ExceptionDispatchInfo exception = null;
            progressForm.Load +=
                async (s, e) =>
                {
                    try
                    {
                        await Task.Run(() => action(progressForm, progressForm.CancellationToken), progressForm.CancellationToken);
                    }
                    catch (Exception ex)
                    {
                        exception = ExceptionDispatchInfo.Capture(ex);
                    }
                    progressForm.Close();
                };
            Application.Run(progressForm);
            exception?.Throw();
        }
    }
}
