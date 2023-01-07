using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RottrModManager.Shared;
using RottrModManager.Shared.Cdc;

namespace RottrExtractor
{
    public partial class MainForm : Form, ITaskProgress
    {
        private readonly ArchiveSet _archiveSet;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private ResourceCollection _currentResourceCollection;
        private bool _closeRequested;

        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(string gameFolderPath)
            : this()
        {
            _archiveSet = new ArchiveSet(gameFolderPath);

            string exeFolderPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            foreach (string hashFilePath in Directory.EnumerateFiles(exeFolderPath, "*.list"))
            {
                CdcHash.AddLookups(hashFilePath);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _tvFiles.Populate(_archiveSet);

            Font groupBoxFont = new Font(Font.Name, Font.Size, FontStyle.Bold);
            _grpFiles.Font = groupBoxFont;
            foreach (Control control in _grpFiles.Controls)
            {
                control.Font = Font;
            }

            _grpResources.Font = groupBoxFont;
            foreach (Control control in _grpResources.Controls)
            {
                control.Font = Font;
            }
        }

        private void _tvFiles_SelectionChanged(object sender, EventArgs e)
        {
            _tvResources.Clear();
            _currentResourceCollection = null;

            List<ArchiveFileReference> selectedFiles = _tvFiles.SelectedFiles;
            if (selectedFiles.Count != 1)
                return;

            ArchiveFileReference file = selectedFiles[0];
            if (Path.GetExtension(file.Name) != ".drm")
                return;

            try
            {
                _currentResourceCollection = _archiveSet.GetResourceCollection(file);
                if (_currentResourceCollection != null)
                    _tvResources.Populate(_archiveSet, _currentResourceCollection);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                _archiveSet.CloseStreams();
            }
        }

        private async void _btnExtractFiles_Click(object sender, EventArgs e)
        {
            List<ArchiveFileReference> files = _tvFiles.SelectedFiles;
            if (files.Count == 0)
            {
                MessageBox.Show("No files selected for extraction.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                FileExtractor extractor = new FileExtractor(_archiveSet);
                string folderPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                await Task.Run(() => extractor.Extract(files, folderPath, this, _cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void _btnExtractAllResources_Click(object sender, EventArgs e)
        {
            if (_currentResourceCollection != null)
                await ExtractResourcesAsync(_currentResourceCollection.ResourceReferences.ToList());
        }

        private async void _btnExtractSelectedResources_Click(object sender, EventArgs e)
        {
            await ExtractResourcesAsync(_tvResources.SelectedFiles);
        }

        private async Task ExtractResourcesAsync(List<ResourceReference> resourceRefs)
        {
            if (_tvFiles.ActiveFile == null || resourceRefs.Count == 0)
            {
                MessageBox.Show("No resources selected for extraction.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ResourceExtractor extractor = new ResourceExtractor(_archiveSet);
                string folderPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                await Task.Run(() => extractor.Extract(_tvFiles.ActiveFile, resourceRefs, folderPath, this, _cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_progressBar.Visible)
                return;

            _statusBar.Text = "Canceling...";
            _cancellationTokenSource.Cancel();
            _closeRequested = true;
            e.Cancel = true;
        }

        void ITaskProgress.Begin(string statusText)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => ((ITaskProgress)this).Begin(statusText)));
                return;
            }

            EnableUi(false);
            _lblStatus.Text = statusText;
            _progressBar.Value = 0;
            _progressBar.Visible = true;
        }

        void ITaskProgress.Report(float progress)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => ((ITaskProgress)this).Report(progress)));
                return;
            }

            _progressBar.Value = (int)(progress * _progressBar.Maximum);
        }

        void ITaskProgress.End()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => ((ITaskProgress)this).End()));
                return;
            }

            EnableUi(true);
            _lblStatus.Text = string.Empty;
            _progressBar.Visible = false;
            if (_closeRequested)
                Close();
        }

        private void EnableUi(bool enable)
        {
            _spltMain.Enabled = enable;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                components?.Dispose();

            _archiveSet.Dispose();

            base.Dispose(disposing);
        }
    }
}
