using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RottrModManager.Mod;
using RottrModManager.Shared;
using RottrModManager.Shared.Cdc;

namespace RottrModManager
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string gameFolderPath = GameFolderFinder.Find();
            if (gameFolderPath == null)
                return;

            if (args.Length == 1)
            {
                HandleCommandLine(gameFolderPath, args[0]);
                return;
            }
            
            Application.Run(new MainForm(gameFolderPath));
        }

        private static void HandleCommandLine(string gameFolderPath, string modPath)
        {
            using ArchiveSet archiveSet = new ArchiveSet(gameFolderPath);
            ResourceUsageCache resourceUsageCache = new ResourceUsageCache(archiveSet);

            try
            {
                bool reinstallMods = archiveSet.DuplicateArchives.Count > 0;

                if (!resourceUsageCache.Load())
                {
                    RunTaskWithProgress(resourceUsageCache.Refresh);
                    resourceUsageCache.Save();
                    reinstallMods = true;
                }

                if (reinstallMods)
                {
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
