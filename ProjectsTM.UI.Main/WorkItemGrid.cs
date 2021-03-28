using FreeGridControl;
using ProjectsTM.Logic;
using ProjectsTM.Model;
using ProjectsTM.Service;
using ProjectsTM.UI.Common;
using ProjectsTM.ViewModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ProjectsTM.UI.Main
{
    public class WorkItemGrid : FreeGridControl.GridControl, IWorkItemGrid
    {
        private readonly MainViewData _viewData;
        private readonly WorkItemDragService _workItemDragService = new WorkItemDragService();
        private readonly WorkItemEditService _editService;
        private readonly WorkItemCopyPasteService _workItemCopyPasteService = new WorkItemCopyPasteService();
        private readonly DrawService _drawService;
        private readonly KeyAndMouseHandleService _keyAndMouseHandleService;
        private readonly RowColResolver _rowColResolver;
        public WorkItemEditService EditService => _editService;

        public Size FullSize => new Size(GridWidth, GridHeight);

        public Size VisibleSize => new Size(Width, Height);

        public Size FixedSize => new Size(FixedWidth, FixedHeight);

        public Point ScrollOffset => new Point(HOffset, VOffset);

        public WorkItemGrid(MainViewData viewData, EditorFindService editorFindService, AppDataFileIOService fileIOService)
        {
            this.Dock = DockStyle.Fill;
            this._viewData = viewData;
            this.FixedRowCount = WorkItemGridConstants.FixedRows;
            this.FixedColCount = WorkItemGridConstants.FixedCols;
            _rowColResolver = new RowColResolver(this, _viewData.Core);
            _editService = new WorkItemEditService(_viewData.Core);

            UpdateGridFrame();

            _drawService = new DrawService(
                _viewData,
                this,
                () => _workItemDragService.IsActive(),
                () => _workItemDragService.IsMoveing(),
                () => _workItemDragService.DragStartInfo,
                this.Font);

            ContextMenuStrip = new MainFormContextMenuStrip(_viewData.Core, this);
            _keyAndMouseHandleService = new KeyAndMouseHandleService(_viewData, this, _workItemDragService, _drawService, _editService, this, editorFindService, Global2Client);

            AttachEvents();
            AllowDrop = true;
            DragEnter += (s, e) => FileDragService.DragEnter(e);

            this.DragDrop += (s, e) =>
            {
                var fileName = FileDragService.Drop(e);
                if (!fileIOService.TryOpenFile(fileName, out var appData)) return;
                _viewData.SetAppData(appData);
            };
        }

        private ClientPoint Global2Client(Point global)
        {
            return new ClientPoint(PointToClient(global));
        }

        public void UpdateGridFrame()
        {
            LockUpdate = true;
            this.RowCount = _viewData.FilteredItems.Days.Count() + this.FixedRowCount;
            this.ColCount = _viewData.FilteredItems.Members.Count() + this.FixedColCount;

            var font = FontCache.GetFont(this.Font.FontFamily, _viewData.FontSize, false);
            var g = this.CreateGraphics();
            var calWidth = (int)Math.Ceiling(g.MeasureString("2000A12A31", font).Width);
            var memberHeight = (int)Math.Ceiling(g.MeasureString("NAME", font).Height);
            var height = memberHeight;
            var width = (int)(Math.Ceiling(this.CreateGraphics().MeasureString("ABCDEF", font).Width) + 1);
            this.ColWidths[WorkItemGridConstants.YearCol.Value] = (int)(calWidth / 2f) + 1;
            this.ColWidths[WorkItemGridConstants.MonthCol.Value] = (int)(calWidth / 4f) + 1;
            this.ColWidths[WorkItemGridConstants.DayCol.Value] = (int)(calWidth / 4f) + 1;
            for (var c = FixedColCount; c < ColCount; c++)
            {
                this.ColWidths[c] = width;
            }
            this.RowHeights[WorkItemGridConstants.CompanyRow.Value] = memberHeight;
            this.RowHeights[WorkItemGridConstants.LastNameRow.Value] = memberHeight;
            this.RowHeights[WorkItemGridConstants.FirstNameRow.Value] = memberHeight;
            for (var r = FixedRowCount; r < RowCount; r++)
            {
                this.RowHeights[r] = height;
            }
            LockUpdate = false;
            _drawService?.ClearBuffer();
        }

        private bool SelectNextWorkItem(bool prev)
        {
            if (_viewData.Selected.IsEmpty())
            {
                var all = _viewData.FilteredItems.WorkItems.ToList();
                all.Sort();
                if (prev) all.Reverse();

                _viewData.Selected.Set(new WorkItems(all.FirstOrDefault()));
                return true;
            }
            if (_viewData.Selected.Count() == 1)
            {
                var all = _viewData.FilteredItems.WorkItems.ToList();
                all.Sort();
                if (prev) all.Reverse();

                var find = all.FindIndex(wi => _viewData.Selected.Unique.Equals(wi));
                WorkItem next = all.Skip(find + 1).FirstOrDefault();
                if (next == null) return false;

                _viewData.Selected.Set(new WorkItems(next));
                return true;
            }
            return false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var tab = keyData == Keys.Tab;
            var shifttab = keyData == (Keys.Shift | Keys.Tab);

            if (tab)
            {
                if (SelectNextWorkItem(false))
                {
                    return true;
                }
            }
            if (shifttab)
            {
                if (SelectNextWorkItem(true))
                {
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
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

        private void _undoService_Changed(object sender, IEditedEventArgs e)
        {
            _drawService.InvalidateMembers(e.UpdatedMembers);
            this.Invalidate();
        }

        public ColIndex Member2Col(Member m)
        {
            return Member2Col(m, _viewData.FilteredItems.Members);
        }

        public bool TryGetMemberDrawRect(Member m, out Rectangle result)
        {
            result = Rectangle.Empty;
            var col = Member2Col(m, _viewData.FilteredItems.Members);
            var rect = GetRectRaw(col, VisibleNormalTopRow, 1);
            if (rect.IsEmpty) return false;
            result = new Rectangle(rect.Value.X, FixedHeight, ColWidths[col.Value], GridHeight);
            return true;
        }

        private void AttachEvents()
        {
            this._viewData.SelectedWorkItemChanged += _viewData_SelectedWorkItemChanged;
            this._viewData.RatioChanged += (s, e) => { UpdateGridFrame(); };
            this._viewData.AppDataChanged += (s, e) => { _rowColResolver.ClearCache(); };
            this._viewData.FilterChanged += (s, e) => { _rowColResolver.ClearCache(); };
            this.OnDrawNormalArea += WorkItemGrid_OnDrawNormalArea;
            this.MouseDown += WorkItemGrid_MouseDown;
            this.MouseUp += WorkItemGrid_MouseUp;
            this.MouseDoubleClick += WorkItemGrid_MouseDoubleClick;
            this.MouseWheel += WorkItemGrid_MouseWheel;
            this._viewData.UndoBuffer.Changed += _undoService_Changed;
            this.MouseMove += WorkItemGrid_MouseMove;
            this.KeyDown += WorkItemGrid_KeyDown;
            this.KeyUp += WorkItemGrid_KeyUp;
        }

        private void WorkItemGrid_MouseWheel(object sender, MouseEventArgs e)
        {
            _keyAndMouseHandleService.MouseWheel(e);
        }

        private void WorkItemGrid_KeyDown(object sender, KeyEventArgs e)
        {
            _keyAndMouseHandleService.KeyDown(e);
            this.Invalidate();
        }

        private void WorkItemGrid_KeyUp(object sender, KeyEventArgs e)
        {
            _keyAndMouseHandleService.KeyUp(e);
            this.Invalidate();
        }

        private void WorkItemGrid_MouseUp(object sender, MouseEventArgs e)
        {
            _keyAndMouseHandleService.MouseUp();
        }

        public ClientRectangle GetRangeSelectBound()
        {
            if (_workItemDragService.State != DragState.RangeSelect) return FreeGridControl.ClientRectangle.Empty;
            var p1 = this.PointToClient(Cursor.Position);
            var p2 = Raw2Client(_workItemDragService.DragStartInfo.Location);
            return Point2Rect.GetRectangle(p1, p2);
        }

        internal bool TryGetUniqueSelect(out WorkItem result)
        {
            if (_viewData.Selected.Count() == 1)
            {
                result = _viewData.Selected.Unique;
                return true;
            }
            result = WorkItem.Invalid;
            return false;
        }

        internal void Divide()
        {
            if (!TryGetUniqueSelect(out var selected)) return;
            var count = _viewData.Original.Callender.GetPeriodDayCount(selected.Period);
            using (var dlg = new DivideWorkItemForm(count))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                _editService.Divide(selected, dlg.Divided, dlg.Remain);
            }
        }

        private void WorkItemGrid_MouseMove(object sender, MouseEventArgs e)
        {
            _keyAndMouseHandleService.MouseMove(ClientPoint.Create(e), this);
            this.Invalidate();
        }

        private void WorkItemGrid_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            _keyAndMouseHandleService.DoubleClick(e);
        }

        public void AddNewWorkItem(WorkItem proto)
        {
            using (var dlg = new EditWorkItemForm(proto, _viewData.Original.WorkItems, _viewData.Original.Callender, _viewData.FilteredItems.Members))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                if (!dlg.TryGetWorkItem(out var wi)) return;
                _editService.Add(wi);
                _viewData.UndoBuffer.Push();
            }
        }

        public void EditSelectedWorkItem()
        {
            if (!TryGetUniqueSelect(out var wi)) return;
            using (var dlg = new EditWorkItemForm(wi.Clone(), _viewData.Original.WorkItems, _viewData.Original.Callender, _viewData.FilteredItems.Members))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                if (!dlg.TryGetWorkItem(out var newWi)) return;
                _editService.Replace(wi, newWi);
                _viewData.Selected.Set(new WorkItems(newWi));
            }
        }
        public void CopyWorkItem()
        {
            _workItemCopyPasteService.CopyWorkItem(_viewData.Selected);
        }

        public void PasteWorkItem()
        {
            _workItemCopyPasteService.PasteWorkItem(GetCursorDay(), GetCursorMember(), _editService);
        }

        public RawPoint Global2Raw(Point global)
        {
            return Client2Raw(new ClientPoint(PointToClient(global)));
        }

        public CallenderDay GetCursorDay()
        {
            var point = PointToClient(Cursor.Position);
            var client = new ClientPoint(point);
            if (IsFixedArea(client)) return CallenderDay.Invalid;

            var rawPoint = Client2Raw(client);
            return Y2Day(rawPoint.Y);
        }

        public Member GetCursorMember()
        {
            var point = PointToClient(Cursor.Position);
            var client = new ClientPoint(point);
            if (IsFixedArea(client)) return Member.Invalid;

            var rawPoint = Client2Raw(client);
            return X2Member(rawPoint.X);
        }


        private void _viewData_SelectedWorkItemChanged(object sender, SelectedWorkItemChangedArg e)
        {
            _drawService.InvalidateMembers(e.UpdatedMembers);
            if (!TryGetUniqueSelect(out var wi)) return;
            var rowRange = GetRowRange(wi);
            MoveVisibleRowColRange(rowRange.Row, rowRange.Count, Member2Col(wi.AssignedMember, _viewData.FilteredItems.Members));
            this.Invalidate();
        }

        private void WorkItemGrid_MouseDown(object sender, MouseEventArgs e)
        {
            _keyAndMouseHandleService.MouseDown(e);
        }

        public CallenderDay Y2Day(int y)
        {
            if (GridHeight < y) return CallenderDay.Invalid;
            var r = Y2Row(y);
            return Row2Day(r);
        }

        public CallenderDay Row2Day(RowIndex r)
        {
            return _rowColResolver.Row2Day(r);
        }

        public Member X2Member(int x)
        {
            if (GridWidth < x) return Member.Invalid;
            var c = X2Col(x);
            return Col2Member(c);
        }

        public Member Col2Member(ColIndex c)
        {
            return _rowColResolver.Col2Member(c);
        }

        internal void MoveToToday()
        {
            TryGetUniqueSelect(out var wi);
            var m = wi.IsInvalid ? X2Member(FixedWidth) : wi.AssignedMember;
            MoveToTodayAndMember(m);
        }

        internal void MoveToMeToday()
        {
            MoveToTodayAndMember(_viewData.Detail.Me);
        }

        private void MoveToTodayAndMember(Member m)
        {
            var today = _viewData.Original.Callender.NearestFromToday;
            MoveVisibleDayAndMember(today, m);
        }

        private void MoveVisibleDayAndMember(CallenderDay day, Member m)
        {
            if (!Day2Row(day, out var row)) return;
            MoveVisibleRowCol(row, Member2Col(m, _viewData.FilteredItems.Members));
        }

        private void WorkItemGrid_OnDrawNormalArea(object sender, DrawNormalAreaEventArgs e)
        {
            _drawService.Draw(e.Graphics, e.IsAllDraw);
        }

        public RawRectangle GetWorkItemDrawRectRaw(WorkItem wi, IEnumerable<Member> members)
        {
            var rowRange = GetRowRange(wi);
            if (rowRange.Row == null) return RawRectangle.Empty;
            return GetRectRaw(Member2Col(wi.AssignedMember, members), rowRange.Row, rowRange.Count);
        }

        public ClientRectangle GetWorkItemDrawRectClient(WorkItem wi, IEnumerable<Member> members)
        {
            var rowRange = GetRowRange(wi);
            if (rowRange.Row == null) return FreeGridControl.ClientRectangle.Empty;
            return GetRectClient(Member2Col(wi.AssignedMember, members), rowRange.Row, rowRange.Count, GetVisibleRect(false, false));
        }

        public IEnumerable<ClientRectangle> GetWorkItemDrawRectClient(IEnumerable<WorkItem> wis, IEnumerable<Member> members)
        {
            var rects = new List<ClientRectangle>();
            foreach (var wi in wis)
            {
                rects.Add(GetWorkItemDrawRectClient(wi, members));
            }
            return rects;
        }

        private ColIndex Member2Col(Member m, IEnumerable<Member> members)
        {
            return _rowColResolver.Member2Col(m, members);
        }

        private RowRange GetRowRange(WorkItem wi)
        {
            RowIndex row = null;
            int count = 0;
            foreach (var d in _viewData.Original.Callender.GetPeriodDays(wi.Period))
            {
                if (!_viewData.FilteredItems.Days.Contains(d)) continue;
                if (row == null)
                {
                    Day2Row(d, out row);
                }
                count++;
            }
            return new RowRange(row, count);
        }

        private bool Day2Row(CallenderDay day, out RowIndex result)
        {
            return _rowColResolver.Day2Row(day, out result);
        }

        public bool IsSelected(Member m)
        {
            return _viewData.Selected.ContainsMember(m);
        }

        public bool IsSelected(CallenderDay d)
        {
            return _viewData.Selected.ContainsDay(d);
        }

        public bool PickWorkItemFromPoint(RawPoint location, out WorkItem result)
        {
            var m = X2Member(location.X);
            var d = Y2Day(location.Y);
            if (m == null || d == null)
            {
                result = null;
                return false;
            }
            return _viewData.FilteredItems.PickWorkItem(m, d, out result);
        }
    }
}