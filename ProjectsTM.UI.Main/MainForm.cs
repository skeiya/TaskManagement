﻿using ProjectsTM.Model;
using ProjectsTM.Service;
using ProjectsTM.UI.TaskList;
using ProjectsTM.ViewModel;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ProjectsTM.UI.Main
{
    public partial class MainForm : Form
    {
        private readonly ViewData _viewData = new ViewData(new AppData(), new UndoService());
        private TaskListForm TaskListForm { get; set; }
        private readonly AppDataFileIOService _fileIOService = new AppDataFileIOService();
        private readonly CalculateSumService _calculateSumService = new CalculateSumService();
        private readonly FilterComboBoxService _filterComboBoxService;
        private PatternHistory _patternHistory = new PatternHistory();
        private string _userName = "未設定";
        private readonly RemoteChangePollingService _remoteChangePollingService;

        public MainForm()
        {
            InitializeComponent();
            _filterComboBoxService = new FilterComboBoxService(_viewData, toolStripComboBoxFilter);
            _viewData.FilterChanged += _viewData_FilterChanged;
            _viewData.AppDataChanged += _viewData_AppDataChanged;
            _fileIOService.FileWatchChanged += _fileIOService_FileWatchChanged;
            _fileIOService.FileOpened += FileIOService_FileOpened;
            _remoteChangePollingService = new RemoteChangePollingService(_fileIOService);
            _remoteChangePollingService.FoundRemoteChange += _remoteChangePollingService_FoundRemoteChange;
            workItemGrid1.AllowDrop = true;
            workItemGrid1.DragEnter += TaskDrawArea_DragEnter;
            workItemGrid1.DragDrop += TaskDrawArea_DragDrop;
            workItemGrid1.UndoChanged += _undoService_Changed;
            workItemGrid1.HoveringTextChanged += WorkItemGrid1_HoveringTextChanged;
            workItemGrid1.RatioChanged += WorkItemGrid1_RatioChanged;
            this.FormClosed += MainForm_FormClosed;
            this.FormClosing += MainForm_FormClosing;
            this.Shown += (a, b) => workItemGrid1.MoveToTodayMe(_userName);
            this.Load += MainForm_Load;
        }

        private void UpdateView()
        {
            _viewData.Selected = new WorkItems();
            if (TaskListForm != null && TaskListForm.Visible) TaskListForm.UpdateView();
            workItemGrid1.Initialize(_viewData);
            _filterComboBoxService.UpdateAppDataPart();
            UpdateDisplayOfSum(null);
            toolStripStatusLabelViewRatio.Text = "拡大率:" + _viewData.Detail.ViewRatio.ToString();
        }

        private void _remoteChangePollingService_FoundRemoteChange(object sender, bool isRemoteBranchAppDataNew)
        {
            if (isRemoteBranchAppDataNew)
            {
                this.Text = "ProjectsTM     ***リモートブランチのデータに更新があります***";
                return;
            }
            this.Text = "ProjectsTM";
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            FormWindowState state;
            state = FormSizeRestoreService.LoadLastTimeFormState("MainFormState");

            switch (state)
            {
                case FormWindowState.Maximized:
                    this.WindowState = state;
                    break;
                case FormWindowState.Normal:
                    Size = FormSizeRestoreService.LoadFormSize("MainFormSize");
                    break;
            }
            LoadUserSetting();
        }

        private void FileIOService_FileOpened(object sender, string filePath)
        {
            _filterComboBoxService.UpdateFilePart(filePath);
            _patternHistory.Load(FilePathService.GetPatternHistoryPath(filePath));
        }

        private void WorkItemGrid1_HoveringTextChanged(object sender, WorkItem e)
        {
            toolStripStatusLabelSelect.Text = e == null ? string.Empty : e.ToString();
        }

        static bool _alreadyShow = false;
        private void _fileIOService_FileWatchChanged(object sender, EventArgs e)
        {
            this.BeginInvoke((Action)(() =>
            {
                if (_alreadyShow) return;
                _alreadyShow = true;
                var msg = "開いているファイルが外部で変更されました。リロードしますか？";
                if (MessageBox.Show(this, msg, "message", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                ToolStripMenuItemReload_Click(null, null);
                _alreadyShow = false;
            }));
        }

        private void LoadUserSetting()
        {
            try
            {
                var setting = UserSettingUIService.Load();
                _viewData.FontSize = setting.FontSize;
                _viewData.Detail = setting.Detail;
                _patternHistory = setting.PatternHistory;
                OpenAppData(_fileIOService.OpenFile(setting.FilePath));
                _filterComboBoxService.Text = setting.FilterName;
                _userName = setting.UserName;
            }
            catch
            {

            }
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_fileIOService.IsDirty) return;
            if (MessageBox.Show("保存されていない変更があります。上書き保存しますか？", "保存", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            if (!_fileIOService.Save(_viewData.Original, ShowTaskListForm)) e.Cancel = true;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            var setting = new UserSetting
            {
                FilterName = _filterComboBoxService.Text,
                FontSize = _viewData.FontSize,
                FilePath = _fileIOService.FilePath,
                Detail = _viewData.Detail,
                PatternHistory = _patternHistory,
                UserName = _userName,
            };
            UserSettingUIService.Save(setting);
            FormSizeRestoreService.SaveFormSize(Height, Width, "MainFormSize");
            FormSizeRestoreService.SaveFormState(this.WindowState, "MainFormState");
        }

        private void _undoService_Changed(object sender, IEditedEventArgs e)
        {
            _fileIOService.SetDirty();
            UpdateDisplayOfSum(e.UpdatedMembers);
        }

        private void UpdateDisplayOfSum(IEnumerable<Member> updatedMembers)
        {
            var sum = _calculateSumService.Calculate(_viewData, updatedMembers);
            toolStripStatusLabelSum.Text = string.Format("SUM:{0}人日({1:0.0}人月)", sum, sum / 20f);
        }

        private void TaskDrawArea_DragDrop(object sender, DragEventArgs e)
        {
            var fileName = FileDragService.Drop(e);
            if (string.IsNullOrEmpty(fileName)) return;
            var appData = _fileIOService.OpenFile(fileName);
            OpenAppData(appData);
        }

        private void TaskDrawArea_DragEnter(object sender, DragEventArgs e)
        {
            FileDragService.DragEnter(e);
        }

        private void WorkItemGrid1_RatioChanged(object sender, float ratio)
        {
            UpdateView();
        }

        private void _viewData_FilterChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void _viewData_AppDataChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void ToolStripMenuItemImportOldFile_Click(object sender, EventArgs e)
        {
            OldFileService.ImportMemberAndWorkItems(_viewData);
            UpdateView();
        }

        private void ToolStripMenuItemExportRS_Click(object sender, EventArgs e)
        {

            using (var dlg = new RsExportSelectForm())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                if (dlg.allPeriod)
                {
                    RSFileExportService.Export(_viewData.Original);
                }
                else
                {
                    RSFileExportService.ExportSelectGetsudo(_viewData.Original, dlg.selectGetsudo);
                }
            }
        }

        private void ToolStripMenuItemOutputImage_Click(object sender, EventArgs e)
        {
            ImageOutputer.Save(_viewData, workItemGrid1);
        }

        private void ToolStripMenuItemAddWorkItem_Click(object sender, EventArgs e)
        {
            workItemGrid1.AddNewWorkItem(null);
        }

        private void ToolStripMenuItemSave_Click(object sender, EventArgs e)
        {
            _fileIOService.Save(_viewData.Original, ShowTaskListForm);
        }

        private void ToolStripMenuItemOpen_Click(object sender, EventArgs e)
        {
            OpenAppData(_fileIOService.Open());
        }

        private void ToolStripMenuItemFilter_Click(object sender, EventArgs e)
        {
            using (var dlg = new FilterForm(_viewData, _patternHistory))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _viewData.SetFilter(dlg.GetFilter());
            }
        }

        private void ToolStripMenuItemColor_Click(object sender, EventArgs e)
        {
            using (var dlg = new ColorManagementForm(_viewData.Original.ColorConditions.Clone()))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _viewData.SetColorConditions(dlg.GetColorConditions());
            }
        }

        private void ToolStripMenuItemSmallRatio_Click(object sender, EventArgs e)
        {
            workItemGrid1.DecRatio();
        }

        private void ToolStripMenuItemLargeRatio_Click(object sender, EventArgs e)
        {
            workItemGrid1.IncRatio();
        }

        private void ToolStripMenuItemManageMember_Click(object sender, EventArgs e)
        {
            using (var dlg = new ManageMemberForm(_viewData.Original))
            {
                dlg.ShowDialog(this);
                workItemGrid1.Initialize(_viewData);
            }
        }

        private void ToolStripMenuItemSaveAsOtherName_Click(object sender, EventArgs e)
        {
            _fileIOService.SaveOtherName(_viewData.Original, ShowTaskListForm);
        }

        private void ToolStripMenuItemUndo_Click(object sender, EventArgs e)
        {
            workItemGrid1.Undo();
        }

        private void ToolStripMenuItemRedo_Click(object sender, EventArgs e)
        {
            workItemGrid1.Redo();
        }

        private void ToolStripMenuItemMileStone_Click(object sender, EventArgs e)
        {
            using (var dlg = new ManageMileStoneForm(_viewData.Original.MileStones.Clone(), _viewData.Original.Callender))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                _viewData.Original.MileStones = dlg.MileStones;
            }
            workItemGrid1.Refresh();
        }

        private void ToolStripMenuItemDivide_Click(object sender, EventArgs e)
        {
            workItemGrid1.Divide();
        }

        private void ToolStripMenuItemGenerateDummyData_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                DummyDataService.Save(dlg.FileName);
            }
        }

        private void ToolStripMenuItemReload_Click(object sender, EventArgs e)
        {
            OpenAppData(_fileIOService.ReOpen());
        }

        private void OpenAppData(AppData appData)
        {
            if (appData == null) return;
            _viewData.SetAppData(appData, new UndoService());
        }

        private void ToolStripMenuItemHowToUse_Click(object sender, EventArgs e)
        {
            LaunchHelpService.Show();
        }

        private void ToolStripMenuItemVersion_Click(object sender, EventArgs e)
        {
            using (var dlg = new VersionForm())
            {
                dlg.ShowDialog(this);
            }
        }

        private void ToolStripMenuItemTaskList_Click(object sender, EventArgs e)
        {
            ShowTaskListForm();
        }

        private void ShowTaskListForm()
        {
            if (TaskListForm == null || TaskListForm.IsDisposed)
            {
                TaskListForm = new TaskListForm(_viewData, _patternHistory);
            }
            if (!TaskListForm.Visible) TaskListForm.Show(this);
        }

        private void toolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ToolStripMenuItemMySetting_Click(object sender, EventArgs e)
        {
            using (var dlg = new ManageMySettingForm(_viewData.Original.Members, _userName))
            {
                dlg.ShowDialog(this);
                _userName = dlg.Selected;
            }
        }

        private void ToolStripMenuItemTrendChart_Click(object sender, EventArgs e)
        {
            ShowTrendChartForm();
        }

        private void ShowTrendChartForm()
        {
            using (var dlg = new TrendChart(_viewData.Original, _fileIOService.FilePath))
            {
                dlg.ShowDialog(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                // Dispose stuff here
                _fileIOService.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}