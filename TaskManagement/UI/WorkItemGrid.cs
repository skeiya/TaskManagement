﻿using FreeGridControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TaskManagement.Logic;
using TaskManagement.Model;
using TaskManagement.Service;
using TaskManagement.ViewModel;

namespace TaskManagement.UI
{
    public class WorkItemGrid : FreeGridControl.GridControl, IWorkItemGrid
    {
        private ViewData _viewData;
        private Cursor _originalCursor;
        private WorkItemDragService _workItemDragService = new WorkItemDragService();
        private UndoService _undoService = new UndoService();
        private WorkItemEditService _editService;
        private ToolTipService _toolTipService = new ToolTipService();
        private DrawService _drawService;
        private RowColResolver _rowColResolver;

        public WorkItemEditService EditService => _editService;

        public SizeF FullSize => new SizeF(GridWidth, GridHeight);

        public Size VisibleSize => new Size(Width, Height);

        public SizeF FixedSize => new SizeF(FixedWidth, FixedHeight);

        public Point ScrollOffset => new Point(HOffset, VOffset);

        public RowColRange VisibleRowColRange => new RowColRange(VisibleNormalLeftCol, VisibleNormalTopRow, VisibleNormalColCount, VisibleNormalRowCount);

        public event EventHandler<EditedEventArgs> UndoChanged;
        public event EventHandler<string> HoveringTextChanged;
        public event EventHandler<float> RatioChanged;
        public WorkItemGrid() { }

        internal void Initialize(ViewData viewData)
        {
            LockUpdate = true;
            if (_viewData != null) DetatchEvents();
            this._viewData = viewData;
            AttachEvents();
            this.FixedRowCount = WorkItemGridConstants.FixedRows;
            this.FixedColCount = WorkItemGridConstants.FixedCols;
            this.RowCount = _viewData.GetFilteredDays().Count + this.FixedRowCount;
            this.ColCount = _viewData.GetFilteredMembers().Count + this.FixedColCount;
            _rowColResolver = new RowColResolver(this, _viewData);
            ApplyDetailSetting(_viewData.Detail);

            _editService = new WorkItemEditService(_viewData, _undoService);

            _rowColResolver.UpdateCache();
            LockUpdate = false;
            RefreshDraw();
        }

        public void RefreshDraw()
        {
            if (_drawService != null)
            {
                _drawService.Dispose();
                _drawService = null;
            }
            _drawService = new DrawService(
                _viewData,
                this,
                () => _workItemDragService.IsActive(),
                this.Font);
            this.Invalidate();
        }

        public IEnumerable<Member> GetNeighbers(IEnumerable<Member> members)
        {
            var neighbers = new HashSet<Member>();
            foreach (var m in members)
            {
                var c = Member2Col(m);
                var l = Col2Member(new ColIndex(c.Value - 1));
                var r = Col2Member(new ColIndex(c.Value + 1));

                neighbers.Add(m);
                if (l != null) neighbers.Add(l);
                if (r != null) neighbers.Add(r);
            }
            return neighbers;
        }

        private void _viewData_FilterChanged(object sender, EventArgs e)
        {
            _rowColResolver.UpdateCache();
            RefreshDraw();
        }

        internal void AdjustForPrint(Rectangle printRect)
        {
            var vRatio = printRect.Height / (float)GridHeight;
            var hRatio = printRect.Width / (float)GridWidth;
            LockUpdate = true;
            for (var c = 0; c < ColCount; c++)
            {
                ColWidths[c] = (ColWidths[c] * hRatio);
            }
            for (var r = 0; r < RowCount; r++)
            {
                RowHeights[r] = (RowHeights[r] * vRatio);
            }
            LockUpdate = false;
        }

        private void ApplyDetailSetting(Detail detail)
        {
            this.ColWidths[0] = detail.DateWidth / 2;
            this.ColWidths[1] = detail.DateWidth / 4;
            this.ColWidths[2] = detail.DateWidth / 4;
            for (var c = FixedColCount; c < ColCount; c++)
            {
                this.ColWidths[c] = detail.ColWidth;
            }
            this.RowHeights[0] = detail.CompanyHeight;
            this.RowHeights[1] = detail.NameHeight;
            this.RowHeights[2] = detail.NameHeight;
            for (var r = FixedRowCount; r < RowCount; r++)
            {
                this.RowHeights[r] = detail.RowHeight;
            }
        }

        private void _undoService_Changed(object sender, EditedEventArgs e)
        {
            UndoChanged?.Invoke(this, e);
            _drawService.InvalidateMembers(e.UpdatedMembers);
            this.Invalidate();
        }

        public ColIndex Member2Col(Member m)
        {
            return Member2Col(m, _viewData.GetFilteredMembers());
        }

        public RectangleF? GetMemberDrawRect(Member m)
        {
            var col = Member2Col(m, _viewData.GetFilteredMembers());
            var rect = GetRect(col, VisibleNormalTopRow, 1, false, false, false);
            if (!rect.HasValue) return null;
            return new RectangleF(rect.Value.X, FixedHeight, ColWidths[col.Value], GridHeight);
        }

        private void AttachEvents()
        {
            this._viewData.SelectedWorkItemChanged += _viewData_SelectedWorkItemChanged;
            this._viewData.FontChanged += _viewData_FontChanged;
            this._viewData.FilterChanged += _viewData_FilterChanged;
            this.OnDrawNormalArea += WorkItemGrid_OnDrawNormalArea;
            this.MouseDown += WorkItemGrid_MouseDown;
            this.MouseUp += WorkItemGrid_MouseUp;
            this.MouseDoubleClick += WorkItemGrid_MouseDoubleClick;
            this.MouseWheel += WorkItemGrid_MouseWheel;
            this._undoService.Changed += _undoService_Changed;
            this.MouseMove += WorkItemGrid_MouseMove;
            this.KeyDown += WorkItemGrid_KeyDown;
            this.KeyUp += WorkItemGrid_KeyUp;
        }

        private void DetatchEvents()
        {
            this._viewData.SelectedWorkItemChanged -= _viewData_SelectedWorkItemChanged;
            this._viewData.FontChanged -= _viewData_FontChanged;
            this._viewData.FilterChanged -= _viewData_FilterChanged;
            this.OnDrawNormalArea -= WorkItemGrid_OnDrawNormalArea;
            this.MouseDown -= WorkItemGrid_MouseDown;
            this.MouseUp -= WorkItemGrid_MouseDown;
            this.MouseDoubleClick -= WorkItemGrid_MouseDoubleClick;
            this.MouseWheel -= WorkItemGrid_MouseWheel;
            this._undoService.Changed -= _undoService_Changed;
            this.MouseMove -= WorkItemGrid_MouseMove;
            this.KeyDown -= WorkItemGrid_KeyDown;
        }

        private void _viewData_FontChanged(object sender, EventArgs e)
        {
            RefreshDraw();
        }

        private void WorkItemGrid_MouseWheel(object sender, MouseEventArgs e)
        {
            if (IsControlDown())
            {
                if (e.Delta > 0)
                {
                    IncRatio();
                }
                else
                {
                    DecRatio();
                }
            }
        }

        private void WorkItemGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                _workItemDragService.ToCopyMode(_viewData.Original.WorkItems, _drawService.InvalidateMembers);
            }
            if (e.KeyCode == Keys.Escape)
            {
                _workItemDragService.End(_editService, _viewData, true, null);
                _viewData.Selected = null;
            }
            this.Invalidate();
        }

        private void WorkItemGrid_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (_viewData.Selected == null) return;
                _editService.Delete();
            }
            if (e.KeyCode == Keys.ControlKey)
            {
                _workItemDragService.ToMoveMode(_viewData.Original.WorkItems, _drawService.InvalidateMembers);
            }
            this.Invalidate();
        }

        private void WorkItemGrid_MouseUp(object sender, MouseEventArgs e)
        {
            using (new RedrawLock(_drawService, () => this.Invalidate()))
            {
                _workItemDragService.End(_editService, _viewData, false, RangeSelect, this);
            }
        }

        public Rectangle? GetRangeSelectBound()
        {
            if (_workItemDragService.State != DragState.RangeSelect) return null;
            var p1 = this.PointToClient(Cursor.Position);
            var p2 = Raw2Client(_workItemDragService.DragedLocation);
            return Point2Rect.GetRectangle(p1, p2);
        }

        void RangeSelect()
        {
            var range = GetRangeSelectBound();
            if (!range.HasValue) return;
            var members = _viewData.GetFilteredMembers();
            var selected = new WorkItems();
            foreach (var c in VisibleRowColRange.Cols)
            {
                var m = Col2Member(c);
                foreach (var w in _viewData.GetFilteredWorkItemsOfMember(m))
                {
                    var rect = GetWorkItemDrawRect(w, members, true);
                    if (!rect.HasValue) continue;
                    if (range.Value.Contains(Rectangle.Round(rect.Value))) selected.Add(w);
                }
            }
            _viewData.Selected = selected;
        }

        internal WorkItem GetUniqueSelect()
        {
            if (_viewData.Selected == null) return null;
            if (_viewData.Selected.Count() != 1) return null;
            return _viewData.Selected.Unique;
        }

        internal void Divide()
        {
            var selected = GetUniqueSelect();
            if (selected == null) return;
            var count = _viewData.Original.Callender.GetPeriodDayCount(selected.Period);
            using (var dlg = new DivideWorkItemForm(count))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                EditService.Divide(selected, dlg.Divided, dlg.Remain);
            }
        }

        private void WorkItemGrid_MouseMove(object sender, MouseEventArgs e)
        {
            UpdateHoveringText((Control)sender, e);
            _workItemDragService.UpdateDraggingItem(this, Client2Raw(e.Location), _viewData);
            if (IsWorkItemExpandArea(_viewData, e.Location))
            {
                if (this.Cursor != Cursors.SizeNS)
                {
                    _originalCursor = this.Cursor;
                    this.Cursor = Cursors.SizeNS;
                }
            }
            else
            {
                if (this.Cursor == Cursors.SizeNS)
                {
                    this.Cursor = _originalCursor;
                }
            }
            this.Invalidate();
        }

        private void UpdateHoveringText(Control c, MouseEventArgs e)
        {
            if (_workItemDragService.IsActive()) return;
            if (IsFixedArea(e.Location)) { _toolTipService.Hide(c); return; }
            RawPoint cur = Client2Raw(e.Location);
            var wi = _viewData.PickFilterdWorkItem(X2Member(cur.X), Y2Day(cur.Y));
            HoveringTextChanged?.Invoke(this, wi == null ? string.Empty : wi.ToString());
            _toolTipService.Update(c, wi);
        }

        private void WorkItemGrid_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (IsFixedArea(e.Location)) return;
            RawPoint curOnRaw = Client2Raw(e.Location);

            if (_viewData.Selected != null)
            {
                EditSelectedWorkItem();
                return;
            }
            var day = Y2Day(curOnRaw.Y);
            var member = X2Member(curOnRaw.X);
            if (day == null || member == null) return;
            var proto = new WorkItem(new Project(""), "", new Tags(new List<string>()), new Period(day, day), member, TaskState.Active);
            AddNewWorkItem(proto);
        }

        public void AddNewWorkItem(WorkItem proto)
        {
            using (var dlg = new EditWorkItemForm(proto, _viewData.Original.Callender))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var wi = dlg.GetWorkItem();
                _viewData.UpdateCallenderAndMembers(wi);
                _editService.Add(wi);
                _undoService.Push();
            }
        }

        public void EditSelectedWorkItem()
        {
            var wi = GetUniqueSelect();
            if (wi == null) return;
            using (var dlg = new EditWorkItemForm(wi.Clone(), _viewData.Original.Callender))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var newWi = dlg.GetWorkItem();
                _viewData.UpdateCallenderAndMembers(newWi);
                _editService.Replace(wi, newWi);
                _viewData.Selected = new WorkItems(newWi);
            }
        }

        private void _viewData_SelectedWorkItemChanged(object sender, SelectedWorkItemChangedArg e)
        {
            _drawService.InvalidateMembers(e.UpdatedMembers);
            var wi = GetUniqueSelect();
            if (wi != null)
            {
                var rowRange = GetRowRange(wi);
                MoveVisibleRowColRange(rowRange.row, rowRange.count, Member2Col(wi.AssignedMember, _viewData.GetFilteredMembers()));
            }
            this.Invalidate();
        }

        private void MoveVisibleDayAndMember(CallenderDay day, Member m)
        {
            if (day == null || m == null) return;
            var row = Day2Row(day);
            if (row == null) return;
            MoveVisibleRowCol(row, Member2Col(m, _viewData.GetFilteredMembers()));
        }

        private void WorkItemGrid_MouseDown(object sender, MouseEventArgs e)
        {
            if (IsFixedArea(e.Location)) return;
            RawPoint curOnRaw = Client2Raw(e.Location);

            if (e.Button == MouseButtons.Right)
            {
                _workItemDragService.StartRangeSelect(curOnRaw);
            }

            if (e.Button == MouseButtons.Left)
            {
                if (IsWorkItemExpandArea(_viewData, e.Location))
                {
                    _workItemDragService.StartExpand(GetExpandDirection(_viewData, e.Location), _viewData.Selected, Y2Day(curOnRaw.Y));
                    return;
                }
            }

            var wi = PickWorkItemFromPoint(curOnRaw);
            if (wi == null)
            {
                _viewData.Selected = null;
                return;
            }
            if (_viewData.Selected == null)
            {
                _viewData.Selected = new WorkItems(wi);
            }
            else
            {
                if (e.Button == MouseButtons.Left && IsControlDown())
                {
                    if (!_viewData.Selected.Contains(wi))
                    {
                        _viewData.Selected.Add(wi);
                    }
                    else
                    {
                        _viewData.Selected.Remove(wi);
                    }
                }
                else
                {
                    if (!_viewData.Selected.Contains(wi))
                    {
                        _viewData.Selected = new WorkItems(wi);
                    }
                }
            }
            if (e.Button == MouseButtons.Left)
            {
                if (IsControlDown())
                {
                    _workItemDragService.StartCopy(_viewData, curOnRaw, Y2Day(curOnRaw.Y), _drawService.InvalidateMembers);
                }
                else
                {
                    _workItemDragService.StartMove(_viewData.Selected, curOnRaw, Y2Day(curOnRaw.Y));
                }
            }
        }

        private int GetExpandDirection(ViewData viewData, Point location)
        {
            if (viewData.Selected == null) return 0;
            foreach (var w in viewData.Selected)
            {
                var bounds = GetWorkItemDrawRect(w, viewData.GetFilteredMembers(), true);
                if (!bounds.HasValue) return 0;
                if (IsTopBar(bounds.Value, location)) return +1;
                if (IsBottomBar(bounds.Value, location)) return -1;
            }
            return 0;
        }

        private bool IsWorkItemExpandArea(ViewData viewData, Point location)
        {
            if (viewData.Selected == null) return false;
            return null != PickExpandingWorkItem(location);
        }

        internal static bool IsTopBar(RectangleF workItemBounds, PointF point)
        {
            var topBar = WorkItemDragService.GetTopBarRect(workItemBounds);
            return topBar.Contains(point);
        }

        internal static bool IsBottomBar(RectangleF workItemBounds, PointF point)
        {
            var bottomBar = WorkItemDragService.GetBottomBarRect(workItemBounds);
            return bottomBar.Contains(point);
        }

        public CallenderDay Y2Day(int y)
        {
            if (GridHeight < y) return null;
            var r = Y2Row(y);
            return Row2Day(r);
        }

        public CallenderDay Row2Day(RowIndex r)
        {
            return _rowColResolver.Row2Dary(r);
        }

        public Member X2Member(int x)
        {
            if (GridWidth < x) return null;
            var c = X2Col(x);
            return Col2Member(c);
        }

        public Member Col2Member(ColIndex c)
        {
            return _rowColResolver.Col2Member(c);
        }

        private WorkItem PickWorkItemFromPoint(RawPoint location)
        {
            var m = X2Member(location.X);
            var d = Y2Day(location.Y);
            if (m == null || d == null) return null;
            return _viewData.PickFilterdWorkItem(m, d);
        }

        internal void MoveToToday()
        {
            var wi = GetUniqueSelect();
            var m = wi != null ? wi.AssignedMember : X2Member((int)FixedWidth);
            var now = DateTime.Now;
            var today = new CallenderDay(now.Year, now.Month, now.Day);
            MoveVisibleDayAndMember(today, m);
        }

        private void WorkItemGrid_OnDrawNormalArea(object sender, DrawNormalAreaEventArgs e)
        {
            _drawService.Draw(e.Graphics, e.IsPrint);
        }

        internal void DecRatio()
        {
            _viewData.DecRatio();
            RatioChanged?.Invoke(this, _viewData.Detail.ViewRatio);
        }

        internal void IncRatio()
        {
            _viewData.IncRatio();
            RatioChanged?.Invoke(this, _viewData.Detail.ViewRatio);
        }

        public RectangleF? GetWorkItemDrawRect(WorkItem wi, Members members, bool isFrontView)
        {
            var rowRange = GetRowRange(wi);
            if (rowRange.row == null) return null;
            return GetRect(Member2Col(wi.AssignedMember, members), rowRange.row, rowRange.count, false, false, isFrontView);
        }

        private ColIndex Member2Col(Member m, Members members)
        {
            return _rowColResolver.Member2Col(m, members);
        }

        private (RowIndex row, int count) GetRowRange(WorkItem wi)
        {
            RowIndex row = null;
            int count = 0;
            foreach (var d in _viewData.Original.Callender.GetPediodDays(wi.Period))
            {
                if (!_viewData.GetFilteredDays().Contains(d)) continue;
                if (row == null)
                {
                    row = Day2Row(d);
                }
                count++;
            }
            return (row, count);
        }

        internal void Redo()
        {
            _undoService.Redo(_viewData);
        }

        internal void Undo()
        {
            _undoService.Undo(_viewData);
        }

        private RowIndex Day2Row(CallenderDay day)
        {
            return _rowColResolver.Day2Row(day);
        }

        public WorkItem PickExpandingWorkItem(Point location)
        {
            if (_viewData.Selected == null) return null;
            foreach (var w in _viewData.Selected)
            {
                var bounds = GetWorkItemDrawRect(w, _viewData.GetFilteredMembers(), true);
                if (!bounds.HasValue) continue;
                if (IsTopBar(bounds.Value, location)) return w;
                if (IsBottomBar(bounds.Value, location)) return w;
            }
            return null;
        }

        public bool IsSelected(Member m)
        {
            if (_viewData.Selected == null) return false;
            return _viewData.Selected.Any(w => w.AssignedMember.Equals(m));
        }

        public bool IsSelected(CallenderDay d)
        {
            if (_viewData.Selected == null) return false;
            return _viewData.Selected.Any(w => w.Period.Contains(d));
        }
    }
}