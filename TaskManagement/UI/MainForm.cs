﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Serialization;
using TaskManagement.Logic;
using TaskManagement.Model;
using TaskManagement.Service;
using TaskManagement.ViewModel;

namespace TaskManagement.UI
{
    public partial class MainForm : Form
    {
        private ViewData _viewData = new ViewData(new AppData());
        private SearchWorkitemForm SearchForm { get; set; }
        private PrintService PrintService { get; set; }
        private AppDataFileIOService FileIOService { get; } = new AppDataFileIOService();

        private FileDragService _fileDragService = new FileDragService();
        private OldFileService _oldFileService = new OldFileService();
        private CalculateSumService _calculateSumService = new CalculateSumService();
        private bool _isDirty = false;

        public MainForm()
        {
            InitializeComponent();
            menuStrip1.ImageScalingSize = new Size(16, 16);
            PrintService = new PrintService(_viewData, workItemGrid1.Font);

            statusStrip1.Items.Add("");
            InitializeTaskDrawArea();
            InitializeFilterCombobox();
            InitializeViewData();
            this.FormClosed += MainForm_FormClosed;
            this.FormClosing += MainForm_FormClosing;
            this.Shown += JumpTodayAtFirstDraw;
            LoadUserSetting();
            workItemGrid1.Initialize(_viewData);
            workItemGrid1.UndoChanged += _undoService_Changed;
            workItemGrid1.HoveringTextChanged += WorkItemGrid1_HoveringTextChanged;
            toolStripStatusLabelViewRatio.Text = "拡大率:" + _viewData.Detail.ViewRatio.ToString();
            workItemGrid1.RatioChanged += WorkItemGrid1_RatioChanged;
            FileIOService.FileChanged += _fileIOService_FileChanged;
            FileIOService.FileSaved += _fileIOService_FileSaved;
        }

        private void WorkItemGrid1_RatioChanged(object sender, float ratio)
        {
            toolStripStatusLabelViewRatio.Text = "拡大率:" + ratio.ToString();
            workItemGrid1.Initialize(_viewData);
        }

        private void WorkItemGrid1_HoveringTextChanged(object sender, string e)
        {
            toolStripStatusLabelSelect.Text = e;
        }

        private void _fileIOService_FileSaved(object sender, EventArgs e)
        {
            _isDirty = false;
        }

        private void _fileIOService_FileChanged(object sender, EventArgs e)
        {
            this.BeginInvoke((Action)(() =>
            {
                var msg = "開いているファイルが外部で変更されました。リロードしますか？";
                if (MessageBox.Show(this, msg, "message", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                ToolStripMenuItemReload_Click(null, null);
            }));
        }

        private void JumpTodayAtFirstDraw(object sender, System.EventArgs e)
        {
            JumpTodayMenu_Click(null, null);
        }

        private void LoadUserSetting()
        {
            try
            {
                var setting = UserSettingUIService.Load(UserSettingPath);
                toolStripComboBoxFilter.Text = setting.FilterName;
                _viewData.FontSize = setting.FontSize;
                _viewData.Detail = setting.Detail;
                OpenAppData(FileIOService.OpenFile(setting.FilePath));
            }
            catch
            {

            }
        }

        private static string UserSettingPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskManagementTool", "UserSetting.xml");

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isDirty) return;
            if (MessageBox.Show("保存されていない変更があります。上書き保存しますか？", "保存", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            if (!FileIOService.Save(_viewData.Original)) e.Cancel = true;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            var setting = new UserSetting
            {
                FilterName = toolStripComboBoxFilter.Text,
                FontSize = _viewData.FontSize,
                FilePath = FileIOService.FilePath,
                Detail = _viewData.Detail
            };
            UserSettingUIService.Save(UserSettingPath, setting);
        }

        private void InitializeViewData()
        {
            _viewData.FilterChanged += _viewData_FilterChanged;
            _viewData.AppDataChanged += _viewData_AppDataChanged;
        }

        private void _undoService_Changed(object sender, EditedEventArgs e)
        {
            _isDirty = true;
            UpdateDisplayOfSum(e.UpdatedMembers);
        }

        private void _viewData_AppDataChanged(object sender, EventArgs e)
        {
            UpdateDisplayOfSum(null);
        }


        private void UpdateDisplayOfSum(List<Member> updatedMembers)
        {
            var sum = _calculateSumService.Calculate(_viewData, updatedMembers);
            toolStripStatusLabelSum.Text = string.Format("SUM:{0}人日({1:0.0}人月)", sum, sum / 20f);
        }

        private static string DirPath => "./filters";
        private List<string> _allPaths = new List<string>();

        private void InitializeFilterCombobox()
        {
            toolStripComboBoxFilter.Items.Clear();
            toolStripComboBoxFilter.Items.Add("ALL");
            try
            {
                _allPaths.Clear();
                _allPaths.AddRange(Directory.GetFiles(DirPath));
                foreach (var f in _allPaths)
                {
                    toolStripComboBoxFilter.Items.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            catch
            {
            }
            finally
            {
                toolStripComboBoxFilter.SelectedIndex = 0;
                toolStripComboBoxFilter.SelectedIndexChanged += ToolStripComboBoxFilter_SelectedIndexChanged;
            }
        }

        private void ToolStripComboBoxFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            _viewData.Selected = null;
            var idx = toolStripComboBoxFilter.SelectedIndex;
            if (idx == 0)
            {
                _viewData.SetFilter(null);
                return;
            }
            var path = _allPaths[idx - 1];
            if (!File.Exists(path)) return;
            using (var rs = StreamFactory.CreateReader(path))
            {
                var x = new XmlSerializer(typeof(Filter));
                var filter = (Filter)x.Deserialize(rs);
                _viewData.SetFilter(filter);
            }
        }

        void InitializeTaskDrawArea()
        {
            InitializeContextMenu();
            workItemGrid1.AllowDrop = true;
            workItemGrid1.DragEnter += TaskDrawArea_DragEnter;
            workItemGrid1.DragDrop += TaskDrawArea_DragDrop;
        }

        private void InitializeContextMenu()
        {
            workItemGrid1.ContextMenuStrip = new ContextMenuStrip();
            workItemGrid1.ContextMenuStrip.Items.Add("編集...").Click += EditMenu_Click;
            workItemGrid1.ContextMenuStrip.Items.Add("削除").Click += DeleteMenu_Click;
            workItemGrid1.ContextMenuStrip.Items.Add("分割...").Click += DivideMenu_Click;
            workItemGrid1.ContextMenuStrip.Items.Add("今日にジャンプ").Click += JumpTodayMenu_Click;
            workItemGrid1.ContextMenuStrip.Items.Add("→Done").Click += DoneMenu_Click;
            var manageItem = new ToolStripMenuItem("管理用");
            workItemGrid1.ContextMenuStrip.Items.Add(manageItem);
            manageItem.DropDownItems.Add("2分割").Click += DivideInto2PartsMenu_Click;
            manageItem.DropDownItems.Add("半分に縮小").Click += MakeHalfMenu_Click;
            manageItem.DropDownItems.Add("以降を選択").Click += SelectAfterwardMenu_Click;
            manageItem.DropDownItems.Add("以降を前詰めに整列").Click += AlignAfterwardMenu_Click;
            manageItem.DropDownItems.Add("選択中の作業項目を隙間なく並べる").Click += AlignSelectedMenu_Click;
        }

        private void DeleteMenu_Click(object sender, EventArgs e)
        {
            workItemGrid1.EditService.Delete();
        }

        private void AlignSelectedMenu_Click(object sender, EventArgs e)
        {
            workItemGrid1.EditService.AlignSelected();
        }

        private void MakeHalfMenu_Click(object sender, EventArgs e)
        {
            workItemGrid1.EditService.MakeHalf();
        }

        private void DivideInto2PartsMenu_Click(object sender, EventArgs e)
        {
            workItemGrid1.EditService.DivideInto2Parts();
        }

        private void AlignAfterwardMenu_Click(object sender, EventArgs e)
        {
            if (!workItemGrid1.EditService.AlignAfterward())
            {
                MessageBox.Show(this, "期間を正常に更新できませんでした。");
            }
        }

        private void SelectAfterwardMenu_Click(object sender, EventArgs e)
        {
            var selected = _viewData.Selected;
            if (selected == null) return;
            workItemGrid1.EditService.SelectAfterward(selected);
        }

        private void DoneMenu_Click(object sender, EventArgs e)
        {
            var selected = _viewData.Selected;
            if (selected == null) return;
            workItemGrid1.EditService.Done(selected);
        }

        private void JumpTodayMenu_Click(object sender, EventArgs e)
        {
            workItemGrid1.MoveToToday();
        }

        private void DivideMenu_Click(object sender, EventArgs e)
        {
            Divide();
        }

        private void Divide()
        {
            workItemGrid1.Divide();
        }

        private void EditMenu_Click(object sender, EventArgs e)
        {
            workItemGrid1.EditSelectedWorkItem();
        }

        private void TaskDrawArea_DragDrop(object sender, DragEventArgs e)
        {
            var fileName = _fileDragService.Drop(e);
            if (string.IsNullOrEmpty(fileName)) return;
            var appData = FileIOService.OpenFile(fileName);
            OpenAppData(appData);
        }

        private void TaskDrawArea_DragEnter(object sender, DragEventArgs e)
        {
            _fileDragService.DragEnter(e);
        }

        private void _viewData_FilterChanged(object sender, EventArgs e)
        {
            _viewData.Selected = new WorkItems();
            SearchForm?.Clear();
            workItemGrid1.Initialize(_viewData);
            UpdateDisplayOfSum(null);
        }

        private void ToolStripMenuItemImportOldFile_Click(object sender, EventArgs e)
        {
            _oldFileService.ImportMemberAndWorkItems(_viewData);
            workItemGrid1.Initialize(_viewData);
        }

        private void ToolStripMenuItemExportRS_Click(object sender, EventArgs e)
        {

            using (var dlg = new RsExportSelectForm())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                if (dlg.allPeriod)
                {
                    RSFileExporter.Export(_viewData.Original);
                }
                else
                {
                    RSFileExporter.ExportSelectGetsudo(_viewData.Original, dlg.selectGetsudo);
                }
            }
        }

        private void ToolStripMenuItemPrint_Click(object sender, EventArgs e)
        {
            _viewData.Selected = null;
            PrintService.Print();
        }

        private void ToolStripMenuItemAddWorkItem_Click(object sender, EventArgs e)
        {
            workItemGrid1.AddNewWorkItem(null);
        }

        private void ToolStripMenuItemSave_Click(object sender, EventArgs e)
        {
            FileIOService.Save(_viewData.Original);
        }

        private void ToolStripMenuItemOpen_Click(object sender, EventArgs e)
        {
            OpenAppData(FileIOService.Open());
        }

        private void ToolStripMenuItemFilter_Click(object sender, EventArgs e)
        {
            using (var dlg = new FilterForm(_viewData.Original.Members, _viewData.Filter == null ? new Filter() : _viewData.Filter.Clone(), _viewData.Original.Callender, _viewData.GetFilteredWorkItems(), IsMemberMatchText))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _viewData.SetFilter(dlg.GetFilter());
            }
        }

        private bool IsMemberMatchText(Member m, string text)
        {
            return _viewData.GetFilteredWorkItemsOfMember(m).Any(w => Regex.IsMatch(w.ToString(), text));
        }

        private void ToolStripMenuItemColor_Click(object sender, EventArgs e)
        {
            using (var dlg = new ColorManagementForm(_viewData.Original.ColorConditions))
            {
                dlg.ShowDialog();
            }
            workItemGrid1.RefreshDraw();
        }

        private void ToolStripMenuItemLargerFont_Click(object sender, EventArgs e)
        {
            _viewData.IncFont();
        }

        private void ToolStripMenuItemSmallFont_Click(object sender, EventArgs e)
        {
            _viewData.DecFont();
        }

        private void ToolStripMenuItemSearch_Click(object sender, EventArgs e)
        {

            if (SearchForm == null || SearchForm.IsDisposed)
            {
                SearchForm = new SearchWorkitemForm(_viewData, workItemGrid1.EditService);
            }
            if (!SearchForm.Visible) SearchForm.Show(this);
        }

        private void ToolStripMenuItemWorkingDas_Click(object sender, EventArgs e)
        {
            using (var dlg = new ManagementWokingDaysForm(_viewData.Original.Callender, _viewData.Original.WorkItems))
            {
                dlg.ShowDialog();
                workItemGrid1.Initialize(_viewData);
            }
            UpdateDisplayOfSum(null);
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
            FileIOService.SaveOtherName(_viewData.Original);
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

        private void ToolStripMenuItemHelp_Click_1(object sender, EventArgs e)
        {
            Process.Start(@".\Help\help.html");
        }

        private void ToolStripMenuItemDivide_Click(object sender, EventArgs e)
        {
            Divide();
        }

        private void ToolStripMenuItemGenerateDummyData_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                DummyDataService.Save(dlg.FileName);
            }
        }

        private void ToolStripMenuItemDetail_Click(object sender, EventArgs e)
        {
            using (var dlg = new ViewDetailSettingForm(_viewData.Detail.Clone()))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                _viewData.Detail = dlg.Detail;
                workItemGrid1.Initialize(_viewData);
            }
        }

        private void ToolStripMenuItemReload_Click(object sender, EventArgs e)
        {
            OpenAppData(FileIOService.ReOpen());
        }

        private void OpenAppData(AppData appData)
        {
            if (appData == null) return;
            _viewData.Original = appData;
            _viewData.Selected = null;
            workItemGrid1.Initialize(_viewData);
            _isDirty = false;
        }
    }
}
