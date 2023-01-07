
namespace RottrExtractor
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this._statusBar = new System.Windows.Forms.StatusStrip();
            this._lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this._progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this._spltMain = new System.Windows.Forms.SplitContainer();
            this._grpFiles = new System.Windows.Forms.GroupBox();
            this._tvFiles = new RottrExtractor.Controls.ArchiveFileTreeView();
            this._pnlExtractFiles = new System.Windows.Forms.TableLayoutPanel();
            this._btnExtractFiles = new System.Windows.Forms.Button();
            this._grpResources = new System.Windows.Forms.GroupBox();
            this._tvResources = new RottrExtractor.Controls.ResourceTreeView();
            this._pnlExtractResources = new System.Windows.Forms.TableLayoutPanel();
            this._btnExtractAllResources = new System.Windows.Forms.Button();
            this._btnExtractSelectedResources = new System.Windows.Forms.Button();
            this._statusBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._spltMain)).BeginInit();
            this._spltMain.Panel1.SuspendLayout();
            this._spltMain.Panel2.SuspendLayout();
            this._spltMain.SuspendLayout();
            this._grpFiles.SuspendLayout();
            this._pnlExtractFiles.SuspendLayout();
            this._grpResources.SuspendLayout();
            this._pnlExtractResources.SuspendLayout();
            this.SuspendLayout();
            // 
            // _statusBar
            // 
            this._statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._lblStatus,
            this._progressBar});
            this._statusBar.Location = new System.Drawing.Point(0, 651);
            this._statusBar.Name = "_statusBar";
            this._statusBar.Size = new System.Drawing.Size(963, 22);
            this._statusBar.TabIndex = 2;
            this._statusBar.Text = "statusStrip1";
            // 
            // _lblStatus
            // 
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(948, 17);
            this._lblStatus.Spring = true;
            this._lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _progressBar
            // 
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(100, 16);
            this._progressBar.Visible = false;
            // 
            // _spltMain
            // 
            this._spltMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this._spltMain.Location = new System.Drawing.Point(0, 0);
            this._spltMain.Name = "_spltMain";
            // 
            // _spltMain.Panel1
            // 
            this._spltMain.Panel1.Controls.Add(this._grpFiles);
            // 
            // _spltMain.Panel2
            // 
            this._spltMain.Panel2.Controls.Add(this._grpResources);
            this._spltMain.Size = new System.Drawing.Size(963, 651);
            this._spltMain.SplitterDistance = 403;
            this._spltMain.TabIndex = 4;
            // 
            // _grpFiles
            // 
            this._grpFiles.Controls.Add(this._tvFiles);
            this._grpFiles.Controls.Add(this._pnlExtractFiles);
            this._grpFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grpFiles.Location = new System.Drawing.Point(0, 0);
            this._grpFiles.Name = "_grpFiles";
            this._grpFiles.Size = new System.Drawing.Size(403, 651);
            this._grpFiles.TabIndex = 0;
            this._grpFiles.TabStop = false;
            this._grpFiles.Text = "Files";
            // 
            // _tvFiles
            // 
            this._tvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tvFiles.Location = new System.Drawing.Point(3, 15);
            this._tvFiles.Name = "_tvFiles";
            this._tvFiles.Size = new System.Drawing.Size(397, 577);
            this._tvFiles.TabIndex = 5;
            this._tvFiles.SelectionChanged += new System.EventHandler(this._tvFiles_SelectionChanged);
            // 
            // _pnlExtractFiles
            // 
            this._pnlExtractFiles.ColumnCount = 1;
            this._pnlExtractFiles.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._pnlExtractFiles.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this._pnlExtractFiles.Controls.Add(this._btnExtractFiles, 0, 0);
            this._pnlExtractFiles.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._pnlExtractFiles.Location = new System.Drawing.Point(3, 592);
            this._pnlExtractFiles.Name = "_pnlExtractFiles";
            this._pnlExtractFiles.RowCount = 1;
            this._pnlExtractFiles.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._pnlExtractFiles.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this._pnlExtractFiles.Size = new System.Drawing.Size(397, 56);
            this._pnlExtractFiles.TabIndex = 6;
            // 
            // _btnExtractFiles
            // 
            this._btnExtractFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnExtractFiles.Location = new System.Drawing.Point(3, 3);
            this._btnExtractFiles.Name = "_btnExtractFiles";
            this._btnExtractFiles.Size = new System.Drawing.Size(391, 50);
            this._btnExtractFiles.TabIndex = 4;
            this._btnExtractFiles.Text = "Extract selected";
            this._btnExtractFiles.UseVisualStyleBackColor = true;
            this._btnExtractFiles.Click += new System.EventHandler(this._btnExtractFiles_Click);
            // 
            // _grpResources
            // 
            this._grpResources.Controls.Add(this._tvResources);
            this._grpResources.Controls.Add(this._pnlExtractResources);
            this._grpResources.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grpResources.Location = new System.Drawing.Point(0, 0);
            this._grpResources.Name = "_grpResources";
            this._grpResources.Size = new System.Drawing.Size(556, 651);
            this._grpResources.TabIndex = 1;
            this._grpResources.TabStop = false;
            this._grpResources.Text = "Resources referenced by selected .drm file";
            // 
            // _tvResources
            // 
            this._tvResources.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tvResources.Location = new System.Drawing.Point(3, 15);
            this._tvResources.Name = "_tvResources";
            this._tvResources.Size = new System.Drawing.Size(550, 577);
            this._tvResources.TabIndex = 3;
            // 
            // _pnlExtractResources
            // 
            this._pnlExtractResources.ColumnCount = 2;
            this._pnlExtractResources.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._pnlExtractResources.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._pnlExtractResources.Controls.Add(this._btnExtractAllResources, 1, 0);
            this._pnlExtractResources.Controls.Add(this._btnExtractSelectedResources, 0, 0);
            this._pnlExtractResources.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._pnlExtractResources.Location = new System.Drawing.Point(3, 592);
            this._pnlExtractResources.Name = "_pnlExtractResources";
            this._pnlExtractResources.RowCount = 1;
            this._pnlExtractResources.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._pnlExtractResources.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this._pnlExtractResources.Size = new System.Drawing.Size(550, 56);
            this._pnlExtractResources.TabIndex = 4;
            // 
            // _btnExtractAllResources
            // 
            this._btnExtractAllResources.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnExtractAllResources.Location = new System.Drawing.Point(278, 3);
            this._btnExtractAllResources.Name = "_btnExtractAllResources";
            this._btnExtractAllResources.Size = new System.Drawing.Size(269, 50);
            this._btnExtractAllResources.TabIndex = 2;
            this._btnExtractAllResources.Text = "Extract all";
            this._btnExtractAllResources.UseVisualStyleBackColor = true;
            this._btnExtractAllResources.Click += new System.EventHandler(this._btnExtractAllResources_Click);
            // 
            // _btnExtractSelectedResources
            // 
            this._btnExtractSelectedResources.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnExtractSelectedResources.Location = new System.Drawing.Point(3, 3);
            this._btnExtractSelectedResources.Name = "_btnExtractSelectedResources";
            this._btnExtractSelectedResources.Size = new System.Drawing.Size(269, 50);
            this._btnExtractSelectedResources.TabIndex = 2;
            this._btnExtractSelectedResources.Text = "Extract selected";
            this._btnExtractSelectedResources.UseVisualStyleBackColor = true;
            this._btnExtractSelectedResources.Click += new System.EventHandler(this._btnExtractSelectedResources_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(963, 673);
            this.Controls.Add(this._spltMain);
            this.Controls.Add(this._statusBar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(550, 350);
            this.Name = "MainForm";
            this.Text = "ROTTR Extractor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this._statusBar.ResumeLayout(false);
            this._statusBar.PerformLayout();
            this._spltMain.Panel1.ResumeLayout(false);
            this._spltMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._spltMain)).EndInit();
            this._spltMain.ResumeLayout(false);
            this._grpFiles.ResumeLayout(false);
            this._pnlExtractFiles.ResumeLayout(false);
            this._grpResources.ResumeLayout(false);
            this._pnlExtractResources.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.StatusStrip _statusBar;
        private System.Windows.Forms.ToolStripStatusLabel _lblStatus;
        private System.Windows.Forms.ToolStripProgressBar _progressBar;
        private System.Windows.Forms.SplitContainer _spltMain;
        private System.Windows.Forms.GroupBox _grpFiles;
        private System.Windows.Forms.Button _btnExtractFiles;
        private System.Windows.Forms.GroupBox _grpResources;
        private System.Windows.Forms.Button _btnExtractSelectedResources;
        private Controls.ArchiveFileTreeView _tvFiles;
        private Controls.ResourceTreeView _tvResources;
        private System.Windows.Forms.TableLayoutPanel _pnlExtractResources;
        private System.Windows.Forms.TableLayoutPanel _pnlExtractFiles;
        private System.Windows.Forms.Button _btnExtractAllResources;
    }
}

