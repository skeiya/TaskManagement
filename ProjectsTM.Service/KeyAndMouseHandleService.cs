using FreeGridControl;
using ProjectsTM.Logic;
using ProjectsTM.Model;
using ProjectsTM.ViewModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ProjectsTM.Service
{
    public class KeyAndMouseHandleService : IDisposable
    {
        private readonly MainViewData _viewData;
        private readonly IWorkItemGrid _grid;
        private readonly WorkItemDragService _workItemDragService;
        private readonly DrawService _drawService;
        private readonly WorkItemEditService _editService;
        private readonly Func<Point, ClientPoint> _global2Client;
        private Cursor _originalCursor;

        private readonly ToolTipService _toolTipService;
        private bool disposedValue;

        public KeyAndMouseHandleService(MainViewData viewData, IWorkItemGrid grid, WorkItemDragService workItemDragService, DrawService drawService, WorkItemEditService editService, Control parentControl, EditorFindService editorFindService, Func<Point, ClientPoint> global2Client)
        {
            this._viewData = viewData;
            this._grid = grid;
            this._workItemDragService = workItemDragService;
            this._drawService = drawService;
            this._editService = editService;
            this._global2Client = global2Client;
            this._toolTipService = new ToolTipService(parentControl, _viewData.Core, editorFindService, GetCusorWorkItem, GetCursorDay, IsDragging, IsMilestoneArea);
        }

        private bool IsMilestoneArea()
        {
            return _grid.IsFixedArea(_global2Client(Cursor.Position));
        }

        private bool IsDragging()
        {
            return _workItemDragService.IsActive();
        }

        private CallenderDay GetCursorDay()
        {
            RawPoint cur = _grid.Global2Raw(Cursor.Position);
            return _grid.Y2Day(cur.Y);
        }

        private WorkItem GetCusorWorkItem()
        {
            RawPoint cur = _grid.Global2Raw(Cursor.Position);
            if (_viewData.FilteredItems.PickWorkItem(_grid.X2Member(cur.X), _grid.Y2Day(cur.Y), out var workItem)) return workItem;
            return WorkItem.Invalid;
        }

        public void MouseDown(MouseEventArgs e)
        {
            var location = ClientPoint.Create(e);
            if (_grid.IsFixedArea(location)) return;
            var curOnRaw = _grid.Client2Raw(location);

            if (e.Button == MouseButtons.Right)
            {
                _workItemDragService.StartRangeSelect(curOnRaw);
            }

            if (e.Button == MouseButtons.Left)
            {
                if (IsWorkItemExpandArea(location))
                {
                    _workItemDragService.StartExpand(GetExpandDirection(_viewData, location), _viewData.Selected, _grid.Y2Day(curOnRaw.Y));
                    return;
                }
            }

            if (!_grid.PickWorkItemFromPoint(curOnRaw, out var wi))
            {
                _viewData.Selected.Clear();
                return;
            }

            if (_viewData.Selected.IsEmpty())
            {
                _viewData.Selected.Set(new WorkItems(wi));
                _workItemDragService.StartMove(_viewData.Selected, _grid.GetWorkItemDrawRectClient(_viewData.Selected, _viewData.Original.Members), curOnRaw, _grid.Y2Day(curOnRaw.Y));
            }
            else
            {
                if (e.Button == MouseButtons.Left && KeyState.IsControlDown)
                {
                    if (!_viewData.Selected.Contains(wi))
                    {
                        _viewData.Selected.Add(wi);
                    }
                    else
                    {
                        _viewData.Selected.Remove(wi);
                    }
                    _workItemDragService.StartCopy(_viewData, _grid.GetWorkItemDrawRectClient(_viewData.Selected, _viewData.Original.Members), curOnRaw, _grid.Y2Day(curOnRaw.Y), _drawService.InvalidateMembers);
                }
                else
                {
                    if (!_viewData.Selected.Contains(wi))
                    {
                        _viewData.Selected.Set(new WorkItems(wi));
                    }
                    _workItemDragService.StartMove(_viewData.Selected, _grid.GetWorkItemDrawRectClient(_viewData.Selected, _viewData.Original.Members), curOnRaw, _grid.Y2Day(curOnRaw.Y));
                }
            }
        }

        private int GetExpandDirection(MainViewData viewData, ClientPoint location)
        {
            foreach (var w in viewData.Selected)
            {
                var bounds = _grid.GetWorkItemDrawRectClient(w, viewData.FilteredItems.Members);
                if (bounds.IsEmpty) return 0;
                if (IsTopBar(bounds, location)) return +1;
                if (IsBottomBar(bounds, location)) return -1;
            }
            return 0;
        }

        public void MouseMove(ClientPoint location, Control control)
        {
            _workItemDragService.UpdateDraggingItem(_grid, _grid.Client2Raw(location), _viewData.Core);
            if (IsWorkItemExpandArea(location))
            {
                if (control.Cursor != Cursors.SizeNS)
                {
                    _originalCursor = control.Cursor;
                    control.Cursor = Cursors.SizeNS;
                }
            }
            else
            {
                if (control.Cursor == Cursors.SizeNS)
                {
                    control.Cursor = _originalCursor;
                }
            }
        }


        private bool IsWorkItemExpandArea(ClientPoint location)
        {
            return PickExpandingWorkItem(location).Any();
        }

        public IEnumerable<WorkItem> PickExpandingWorkItem(ClientPoint location)
        {
            foreach (var w in _viewData.Selected)
            {
                var bounds = _grid.GetWorkItemDrawRectClient(w, _viewData.FilteredItems.Members);
                if (bounds.IsEmpty) continue;
                if (IsTopBar(bounds, location)) yield return w;
                if (IsBottomBar(bounds, location)) yield return w;
            }
        }

        internal static bool IsTopBar(ClientRectangle workItemBounds, ClientPoint point)
        {
            var topBar = WorkItemDragService.GetTopBarRect(workItemBounds);
            return topBar.Contains(point);
        }

        internal static bool IsBottomBar(ClientRectangle workItemBounds, ClientPoint point)
        {
            var bottomBar = WorkItemDragService.GetBottomBarRect(workItemBounds);
            return bottomBar.Contains(point);
        }

        public void DoubleClick(MouseEventArgs e)
        {
            var locaion = ClientPoint.Create(e);
            if (_grid.IsFixedArea(locaion)) return;
            RawPoint curOnRaw = _grid.Client2Raw(locaion);

            if (!_viewData.Selected.IsEmpty())
            {
                _grid.EditSelectedWorkItem();
                return;
            }
            var day = _grid.Y2Day(curOnRaw.Y);
            var member = _grid.X2Member(curOnRaw.X);
            if (day == null || member == null) return;
            var proto = WorkItem.CreateProto(new Period(day, day), member);
            _grid.AddNewWorkItem(proto);
        }

        public void KeyDown(KeyEventArgs e)
        {
            var ctrl = (e.Modifiers & Keys.Control) == Keys.Control;
            var shift = (e.Modifiers & Keys.Shift) == Keys.Shift;

            if (ctrl && shift && e.KeyCode == Keys.Up)
            {
                _editService.ExpandDays(-1);
                return;
            }
            if (ctrl && e.KeyCode == Keys.Up)
            {
                _editService.ShiftDays(-1);
                return;
            }

            if (ctrl && shift && e.KeyCode == Keys.Down)
            {
                _editService.ExpandDays(1);
                return;
            }
            if (ctrl && e.KeyCode == Keys.Down)
            {
                _editService.ShiftDays(1);
                return;
            }

            if (e.KeyCode == Keys.ControlKey)
            {
                _workItemDragService.ToCopyMode(_viewData.Original.WorkItems, _drawService.InvalidateMembers);
            }
            if (e.KeyCode == Keys.Escape)
            {
                _workItemDragService.End(_editService, _viewData.Core, true, null);
                _viewData.Selected.Clear();
            }
        }

        public void KeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                _editService.Delete();
            }
            if (e.KeyCode == Keys.ControlKey)
            {
                _workItemDragService.ToMoveMode(_viewData.Original.WorkItems, _drawService.InvalidateMembers);
            }
        }

        public void MouseWheel(MouseEventArgs e)
        {
            if (KeyState.IsControlDown)
            {
                if (e.Delta > 0)
                {
                    _viewData.IncRatio();
                }
                else
                {
                    _viewData.DecRatio();
                }
            }
        }

        public void MouseUp()
        {
            using (new RedrawLock(_drawService, () => _grid.Invalidate()))
            {
                _workItemDragService.End(_editService, _viewData.Core, false, RangeSelect);
            }
        }

        void RangeSelect()
        {
            var range = _grid.GetRangeSelectBound();
            if (range.IsEmpty) return;
            var members = _viewData.FilteredItems.Members;
            var selected = new WorkItems();
            foreach (var c in _grid.VisibleRowColRange.Cols)
            {
                var m = _grid.Col2Member(c);
                foreach (var w in _viewData.FilteredItems.GetWorkItemsOfMember(m))
                {
                    var rect = _grid.GetWorkItemDrawRectClient(w, members);
                    if (rect.IsEmpty) continue;
                    if (range.Contains(rect)) selected.Add(w);
                }
            }
            _viewData.Selected.Set(selected);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _toolTipService.Dispose();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~KeyAndMouseHandleService()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
