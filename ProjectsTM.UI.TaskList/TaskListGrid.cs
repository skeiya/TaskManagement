﻿using FreeGridControl;
using ProjectsTM.Logic;
using ProjectsTM.Model;
using ProjectsTM.Service;
using ProjectsTM.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;

namespace ProjectsTM.UI.TaskList
{
    public partial class TaskListGrid : FreeGridControl.GridControl
    {
        private List<TaskListItem> _listItems;
        private ViewData _viewData;
        private string _pattern;
        private WorkItemEditService _editService;
        public event EventHandler ListUpdated;
        private ColIndex _sortCol = new ColIndex(6);
        private bool _isReverse = false;
        private RowIndex _lastSelect;
        private AuditService _auditService;
        private List<TaskListItem> errList = null;
        private Func<bool> _IsWorItemDragActive;

        public TaskListGrid()
        {
            InitializeComponent();
            this.OnDrawNormalArea += TaskListGrid_OnDrawNormalArea;
            this.MouseDoubleClick += TaskListGrid_MouseDoubleClick;
            this.MouseClick += TaskListGrid_MouseClick;
            this.Disposed += TaskListGrid_Disposed;
            this.KeyDown += TaskListGrid_KeyDown;
        }

        private void TaskListGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                MoveSelect(+1);
            }
            else if (e.KeyCode == Keys.Up)
            {
                MoveSelect(-1);
            }
            else if(KeyState.IsControlDown && (e.KeyCode == Keys.C))
            {
                CopyToClipboard();
            }
        }

        private void SetStrOneLine(StringBuilder copyData, WorkItem  w)
        {
            const string DOUBLE_Q = "\"";
            const string TAB = "\t";
            copyData.Append(w.Name.ToString());             copyData.Append(TAB);
            copyData.Append(w.Project.ToString());          copyData.Append(TAB);
            copyData.Append(w.AssignedMember.ToString());   copyData.Append(TAB);
            copyData.Append(w.Tags.ToString());             copyData.Append(TAB);
            copyData.Append(w.State);                       copyData.Append(TAB);
            copyData.Append(w.Period.From.ToString());      copyData.Append(TAB);
            copyData.Append(w.Period.To.ToString());        copyData.Append(TAB);
            copyData.Append(_viewData.Original.Callender.GetPeriodDayCount(w.Period).ToString());
            copyData.Append(TAB);
            copyData.Append(DOUBLE_Q); copyData.Append(w.Description); copyData.Append(DOUBLE_Q);
            copyData.AppendLine(TAB);
        }

        private void CopyToClipboard()
        {
            if (_viewData.Selected == null) return;
            var workItems = _viewData.Selected;
            StringBuilder copyData = new StringBuilder(string.Empty);
            foreach (var w in workItems) { SetStrOneLine(copyData, w); }
            Clipboard.SetData(DataFormats.Text, copyData.ToString());
        }

        private void MoveSelect(int offset)
        {
            if (_viewData.Selected == null) return;
            if (_viewData.Selected.Count() != 1) return;
            var idx = _listItems.FindIndex(l => l.WorkItem.Equals(_viewData.Selected.Unique));
            var oneStep = offset / Math.Abs(offset);
            while (true)
            {
                idx += oneStep;
                if (idx < 0 || _listItems.Count <= idx) return;
                if (_listItems.ElementAt(idx).IsMilestone) continue;
                offset -= oneStep;
                if (offset != 0) continue;
                _viewData.Selected = new WorkItems(_listItems.ElementAt(idx).WorkItem);
            }
        }

        private void SelectItems(RowIndex r)
        {
            var from = r.Value - FixedRowCount;
            var to = r.Value - FixedRowCount;
            if (IsMultiSelect()) from = _lastSelect.Value - FixedRowCount;
            SelectRange(from, to);
        }

        private bool IsMultiSelect()
        {
            if (!KeyState.IsShiftDown) return false;
            if (_lastSelect == null) return false;
            return true;
        }

        private void SelectRange(int from,int to)
        {
            SwapIfUpsideDown(ref from, ref to);
            var selects = new WorkItems();
            for (var idx = from; idx <= to; idx++)
            {
                var l = _listItems[idx];
                if (l.IsMilestone) continue;
                selects.Add(l.WorkItem);
            }
            _viewData.Selected = selects;
        }

        private void SwapIfUpsideDown(ref int from, ref int to)
        {
            if (from <= to) return;
            int buf = from;
            from = to;
            to = buf;
        }

        private void TaskListGrid_MouseClick(object sender, MouseEventArgs e)
        {
            var rawLocation = Client2Raw(ClientPoint.Create(e));
            var r = Y2Row(rawLocation.Y);
            if (r.Value < FixedRowCount)
            {
                HandleSortRequest(rawLocation);
                return;
            }
            SelectItems(r);
        }

        private void HandleSortRequest(RawPoint rawLocation)
        {
            var c = X2Col(rawLocation.X);
            if (_sortCol.Equals(c))
            {
                _isReverse = !_isReverse;
            }
            else
            {
                _isReverse = false;
            }
            _sortCol = c;
            InitializeGrid(false);
        }

        private void Sort()
        {
            if (IsDayCountCol(_sortCol))
            {
                _listItems = _listItems.OrderBy(l => _viewData.Original.Callender.GetPeriodDayCount(l.WorkItem.Period)).ToList();
            }
            else
            {
                _listItems = _listItems.OrderBy(l => GetText(l, _sortCol)).ToList();
            }
            if (_isReverse)
            {
                _listItems.Reverse();
            }
        }

        private static bool IsDayCountCol(ColIndex c)
        {
            return c.Value == 7;
        }

        private void TaskListGrid_Disposed(object sender, EventArgs e)
        {
            DetatchEvents();
        }

        private void TaskListGrid_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var r = Y2Row(Client2Raw(ClientPoint.Create(e)).Y);
            if (r.Value < FixedRowCount) return;
            var item = _listItems[r.Value - FixedRowCount];
            using (var dlg = new EditWorkItemForm(item.WorkItem.Clone(), _viewData.Original.Callender, _viewData.GetFilteredMembers()))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var newWi = dlg.GetWorkItem();
                _viewData.UpdateCallenderAndMembers(newWi);
                _editService.Replace(item.WorkItem, newWi);
                _viewData.Selected = new WorkItems(newWi);
            }
        }

        internal int GetDayCount()
        {
            return _listItems.Where(l => !l.IsMilestone).Sum(l => _viewData.Original.Callender.GetPeriodDayCount(l.WorkItem.Period));
        }

        internal void Initialize(ViewData viewData, string pattern, Func<bool> IsWorkItemDragActive)
        {
            this._pattern = pattern;
            this._editService = new WorkItemEditService(viewData);
            if (_viewData != null) DetatchEvents();
            this._viewData = viewData;
            this._auditService = new AuditService();
            this._IsWorItemDragActive = IsWorkItemDragActive;
            AttachEvents();
            InitializeGrid(true);
        }

        private void _InitializeGrid()
        {
            LockUpdate = true;
            UpdateListItem();
            ColCount = 10;
            FixedRowCount = 1;
            RowCount = _listItems.Count + FixedRowCount;
            SetHeightAndWidth();
            LockUpdate = false;
            UpdateLastSelect();
        }

        private async void InitializeGridForAuditAsync()
        {
            if (_auditService.IsActive) return;
            WorkItems workitems;
            Callender callender;

            _viewData.CloneWorkitemsAndCallender(out workitems, out callender);
            Task<List<TaskListItem>> task = _auditService.StartAuditTask(workitems, callender);

            errList = await task;
            _viewData.CloneWorkitemsAndCallender(out workitems, out callender);
            if (_auditService.WorkitemsAndCallenderChanged(workitems, callender)) InitializeGridForAuditAsync();
            else InitializeGrid(false);
        }

        private void ApplyDragEdit(WorkItems before, WorkItems after)
        {
            for (int i = 0; i < before.Count(); i++)
            {
                var listIdx = _listItems.FindIndex(l => l.WorkItem.Equals(before.ElementAt(i)));
                if (listIdx == -1) return;
                var item = _listItems[listIdx];
                _listItems[listIdx] = new TaskListItem(after.ElementAt(i), item.Color, item.IsMilestone, item.ErrMsg);
            }
            Sort();
        }

        internal void DragEditDone(WorkItems before, WorkItems after) 
        {
            ApplyDragEdit(before, after);
            SelectedWorkItemChanged();
            InitializeGridForAuditAsync();
        }

        private void InitializeGrid(bool execAudit)
        {
            if (_IsWorItemDragActive()) return;
            _InitializeGrid();
            if (execAudit) InitializeGridForAuditAsync();
        }

        private void AttachEvents()
        {
            _viewData.UndoService.Changed += _undoService_Changed;
            _viewData.SelectedWorkItemChanged += _viewData_SelectedWorkItemChanged;
        }

        private void DetatchEvents()
        {
            _viewData.UndoService.Changed -= _undoService_Changed;
            _viewData.SelectedWorkItemChanged -= _viewData_SelectedWorkItemChanged;
        }

        private void UpdateLastSelect()
        {
            if (_viewData.Selected == null ||
                _viewData.Selected.Count() == 0) { _lastSelect = null; return; }

            if (_viewData.Selected.Count() == 1)
            {
                var idx = _listItems.FindIndex(l => l.WorkItem.Equals(_viewData.Selected.Unique));
                _lastSelect = new RowIndex(idx + FixedRowCount);
            }
        }

        private void SelectedWorkItemChanged()
        {
            UpdateLastSelect();
            MoveSelectedVisible();
            this.Invalidate();
        }

        private void _viewData_SelectedWorkItemChanged(object sender, SelectedWorkItemChangedArg e)
        {
            if (_IsWorItemDragActive()) return;
            SelectedWorkItemChanged();
        }

        private void MoveSelectedVisible()
        {
            if (_viewData.Selected == null) return;
            if (_viewData.Selected.Count() != 1) return;
            var listIdx = _listItems.FindIndex(l => l.WorkItem.Equals(_viewData.Selected.Unique));
            if (listIdx == -1) return;
            MoveVisibleRowCol(new RowIndex(listIdx + FixedRowCount), new ColIndex(0)); // TODO グリッドの上側に移動してしまう。下側にはみ出ていた時は下のままにする。
        }

        private void _undoService_Changed(object sender, IEditedEventArgs e)
        {
            InitializeGrid(true);
            this.Invalidate();
        }

        private void SetHeightAndWidth()
        {
            var font = this.Font;
            var g = this.CreateGraphics();
            var calculator = new HeightAndWidthCalcultor(font, g, _listItems, GetText, GetTitle, ColCount);
            foreach (var c in ColIndex.Range(0, ColCount))
            {
                ColWidths[c.Value] = calculator.GetWidth(c);
            }
            foreach (var r in RowIndex.Range(0, RowCount))
            {
                RowHeights[r.Value] = calculator.GetHeight(r);
            }
        }

        private void UpdateListItem()
        {
            _listItems = GetFilterList(errList);
            Sort();
            ListUpdated?.Invoke(this, null);
        }

 
        private bool HasError(WorkItems errWorkItems, WorkItem wi)
        {
            return errWorkItems.Count() >= 0 && errWorkItems.Contains(wi);
        }
        private List<TaskListItem> GetFilterList(List<TaskListItem> errList)
        {
            var list = new List<TaskListItem>();
            var errWorkItems = new WorkItems();
            if (errList != null) { foreach (var item in errList) errWorkItems.Add(item.WorkItem); };

            foreach (var wi in _viewData.GetFilteredWorkItems())
            {
                if (_pattern != null && !Regex.IsMatch(wi.ToString(), _pattern)) continue;
                list.Add(HasError(errWorkItems, wi) ?
                    errList.First(i => i.WorkItem == wi) :
                    new TaskListItem(wi, GetColor(wi.State), false, string.Empty));
            }
            foreach (var ms in _viewData.Original.MileStones)
            {
                list.Add(new TaskListItem(ConvertWorkItem(ms), ms.Color, true, string.Empty));
            }
            return list;
        }

        private static WorkItem ConvertWorkItem(MileStone ms)
        {
            return new WorkItem(new Model.Project("noPrj"), ms.Name, new Tags(new List<string>()), new Period(ms.Day, ms.Day), new Member(), TaskState.Active, "");
        }

        private static Color GetColor(TaskState state)
        {
            switch (state)
            {
                case TaskState.Active:
                    return Color.White;
                case TaskState.Background:
                    return Color.LightGreen;
                case TaskState.Done:
                    return Color.LightGray;
                case TaskState.New:
                    return Color.LightBlue;
                default:
                    return Color.White;
            }
        }

        private void TaskListGrid_OnDrawNormalArea(object sender, FreeGridControl.DrawNormalAreaEventArgs e)
        {
            using (var format = new StringFormat() { LineAlignment = StringAlignment.Center })
            {
                var g = e.Graphics;
                DrawTitleRow(g);
                foreach (var r in RowIndex.Range(VisibleNormalTopRow.Value, VisibleNormalRowCount))
                {
                    DrawItemRow(g, r, format);
                }
            }
        }

        private void DrawItemRow(Graphics g, RowIndex r, StringFormat format)
        {
            var item = _listItems[r.Value - FixedRowCount];
            var visibleArea = GetVisibleRect(false, false);
            foreach (var c in ColIndex.Range(VisibleNormalLeftCol.Value, VisibleNormalColCount))
            {
                var res = GetRectClient(c, r, 1, visibleArea);
                if (!res.HasValue) continue;
                g.FillRectangle(BrushCache.GetBrush(item.Color), res.Value.Value);
                g.DrawRectangle(Pens.Black, Rectangle.Round(res.Value.Value));
                var text = GetText(item, c);
                var rect = res.Value;
                rect.Y += 1;
                g.DrawString(text, this.Font, Brushes.Black, rect.Value, format);

            }
            if (_viewData.Selected != null && _viewData.Selected.Contains(item.WorkItem))
            {
                var res = GetRectClient(new ColIndex(0), r, 1, visibleArea);
                if (!res.HasValue) return;
                var rect = new Rectangle(0, res.Value.Top, GridWidth, res.Value.Height);
                g.DrawRectangle(PenCache.GetPen(Color.DarkBlue, 3), rect);
            }
        }

        private string GetText(TaskListItem item, ColIndex c)
        {
            var colIndex = c.Value;
            var wi = item.WorkItem;
            if (colIndex == 0)
            {
                return wi.Name;
            }
            else if (colIndex == 1)
            {
                return wi.Project.ToString();
            }
            else if (colIndex == 2)
            {
                return wi.AssignedMember.ToString();
            }
            else if (colIndex == 3)
            {
                return wi.Tags.ToString();
            }
            else if (colIndex == 4)
            {
                return wi.State.ToString();
            }
            else if (colIndex == 5)
            {
                return wi.Period.From.ToString();
            }
            else if (colIndex == 6)
            {
                return wi.Period.To.ToString();
            }
            else if (colIndex == 7)
            {
                return _viewData.Original.Callender.GetPeriodDayCount(wi.Period).ToString();
            }
            else if (colIndex == 8)
            {
                return wi.Description;
            }
            else if (colIndex == 9)
            {
                return item.ErrMsg;
            }
            return string.Empty;
        }

        private void DrawTitleRow(Graphics g)
        {
            using (var format = new StringFormat() { Alignment = StringAlignment.Far })
            {
                var visibleArea = GetVisibleRect(true, false);
                foreach (var c in ColIndex.Range(VisibleNormalLeftCol.Value, VisibleNormalColCount))
                {
                    var res = GetRectClient(c, new RowIndex(0), 1, visibleArea);
                    if (!res.HasValue) return;
                    g.FillRectangle(Brushes.Gray, res.Value.Value);
                    g.DrawRectangle(Pens.Black, res.Value.Value);
                    var rect = res.Value;
                    rect.Y += 1;
                    g.DrawString(GetTitle(c), this.Font, Brushes.Black, rect.Value);
                    if (c.Equals(_sortCol)) g.DrawString(_isReverse ? "▼" : "▲", this.Font, Brushes.Black, rect.Value, format);
                }
            }
        }

        private static string GetTitle(ColIndex c)
        {
            string[] titles = new string[] { "名前", "プロジェクト", "担当", "タグ", "状態", "開始", "終了", "人日", "備考", "エラー" };
            return titles[c.Value];
        }
    }
}
