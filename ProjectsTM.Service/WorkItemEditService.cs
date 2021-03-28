using ProjectsTM.Model;
using ProjectsTM.ViewModel;
using System.Collections.Generic;
using System.Linq;

namespace ProjectsTM.Service
{
    public class WorkItemEditService
    {
        private readonly ViewData _viewData;

        public WorkItemEditService(ViewData viewData)
        {
            this._viewData = viewData;
        }

        public void Add(IEnumerable<WorkItem> wis)
        {
            if (wis == null) return;
            var items = _viewData.Original.WorkItems;
            foreach (var w in wis)
            {
                if (items.Contains(w)) continue;
                items.Add(w);
                _viewData.UndoBuffer.Add(w);
            }
            _viewData.UndoBuffer.Push();
        }

        internal void CopyAndAdd(WorkItem orgItem, CallenderDay newFrom, Member newMember)
        {
            if (WorkItem.Invalid.Equals(orgItem)) return;
            if (CallenderDay.Invalid.Equals(newFrom)) return;
            if (Member.Invalid.Equals(newMember)) return;

            var copyItem = orgItem.Clone();
            var offset = _viewData.Original.Callender.GetOffset(copyItem.Period.From, newFrom);
            if (!copyItem.Period.TryApplyOffset(offset, _viewData.Original.Callender, out var newPeriod)) return;
            copyItem.Period = newPeriod;
            copyItem.AssignedMember = newMember;

            Add(copyItem);
        }

        public void Add(WorkItem wi)
        {
            if (wi == null) return;
            var items = _viewData.Original.WorkItems;
            if (items.Contains(wi)) return;
            items.Add(wi);
            _viewData.UndoBuffer.Add(wi);
            _viewData.UndoBuffer.Push();
        }

        public void Delete()
        {
            _viewData.Original.WorkItems.Remove(_viewData.Selected);
            _viewData.UndoBuffer.Delete(_viewData.Selected);
            _viewData.Selected.Clear();
            _viewData.UndoBuffer.Push();
        }

        public void Divide(WorkItem selected, int divided, int remain)
        {
            var d1 = selected.Clone();
            var d2 = selected.Clone();

            if (!_viewData.Original.Callender.TryApplyOffset(d1.Period.To, -remain, out var to)) return;
            if (!_viewData.Original.Callender.TryApplyOffset(d2.Period.From, divided, out var from)) return;
            d1.Period.To = to;
            d2.Period.From = from;

            var workItems = _viewData.Original.WorkItems;
            _viewData.Selected.Clear();
            workItems.Remove(selected);
            workItems.Add(d1);
            workItems.Add(d2);

            _viewData.UndoBuffer.Delete(selected);
            _viewData.UndoBuffer.Add(d1);
            _viewData.UndoBuffer.Add(d2);
            _viewData.UndoBuffer.Push();
        }

        public void Replace(IEnumerable<WorkItem> before, IEnumerable<WorkItem> after)
        {
            _viewData.Original.WorkItems.Remove(before);
            _viewData.Original.WorkItems.Add(after);
            _viewData.UndoBuffer.Delete(before);
            _viewData.UndoBuffer.Add(after);
            _viewData.UndoBuffer.Push();
        }

        public void Replace(WorkItem before, WorkItem after)
        {
            if (before.Equals(after)) return;
            _viewData.Original.WorkItems.Remove(before);
            _viewData.Original.WorkItems.Add(after);
            _viewData.UndoBuffer.Delete(before);
            _viewData.UndoBuffer.Add(after);
            _viewData.UndoBuffer.Push();
        }

        public void ChangeState(SelectedWorkItems selected, TaskState state)
        {
            var newWis = selected.Clone();

            foreach (var w in newWis) w.State = state;

            var workItems = _viewData.Original.WorkItems;
            workItems.Remove(selected);
            workItems.Add(newWis);

            _viewData.UndoBuffer.Delete(selected);
            _viewData.UndoBuffer.Add(newWis);
            _viewData.UndoBuffer.Push();

            _viewData.Selected.Clear();
        }

        public void SelectAfterward(IEnumerable<WorkItem> starts)
        {
            var newSetected = new WorkItems();
            foreach (var s in starts)
            {
                newSetected.Add(GetSameMemberAfterItems(s));
            }
            _viewData.Selected.Set(newSetected);
        }

        private WorkItems GetSameMemberAfterItems(WorkItem s)
        {
            var result = new WorkItems();
            foreach (var w in _viewData.FilteredItems.GetWorkItemsOfMember(s.AssignedMember))
            {
                if (_viewData.Original.Callender.GetOffset(s.Period.From, w.Period.From) >= 0)
                {
                    if (result.Contains(w)) continue;
                    result.Add(w);
                }
            }
            return result;
        }

        public bool AlignAfterward()
        {
            var starts = _viewData.Selected;
            if (starts == null) return false;
            if (HasSameMember(starts)) return false;
            var before = new WorkItems();
            var after = new WorkItems();
            var cal = _viewData.Original.Callender;
            foreach (var s in starts)
            {
                var lastTo = s.Period.To;
                foreach (var w in GetSameMemberAfterItems(s).OrderBy(o => o.Period.From))
                {
                    if (w.Equals(s)) continue;
                    before.Add(w.Clone());
                    if (!cal.TryApplyOffset(lastTo, 1, out var nextFrom)) return false;
                    var offset = cal.GetOffset(w.Period.From, nextFrom);
                    var a = w.Clone();
                    if (!w.Period.TryApplyOffset(offset, cal, out var period)) return false;
                    a.Period = period;
                    lastTo = a.Period.To;
                    after.Add(a);
                }
            }
            Replace(before, after);
            return true;
        }

        public void AlignSelected()
        {
            var before = _viewData.Selected.Clone().OrderBy(w => w.Period.From);
            var after = new WorkItems();
            var isFirst = true;
            CallenderDay lastTo = null;
            var cal = _viewData.Original.Callender;
            foreach (var w in before)
            {
                if (isFirst)
                {
                    isFirst = false;
                    lastTo = w.Period.To;
                    after.Add(w);
                    continue;
                }
                if (!cal.TryApplyOffset(lastTo, 1, out var nextFrom)) return;
                var offset = cal.GetOffset(w.Period.From, nextFrom);
                var newWorkItem = w.Clone();
                if (!w.Period.TryApplyOffset(offset, cal, out var period)) return;
                newWorkItem.Period = period;
                lastTo = newWorkItem.Period.To;
                after.Add(newWorkItem);
            }
            Replace(_viewData.Selected, after);
            _viewData.Selected.Set(after);
        }

        private static bool HasSameMember(IEnumerable<WorkItem> starts)
        {
            var members = new List<Member>();
            foreach (var s in starts)
            {
                if (members.Contains(s.AssignedMember)) return true;
                members.Add(s.AssignedMember);
            }
            return false;
        }

        public void DivideInto2Parts()
        {
            DivideCore(false);
        }

        public void MakeHalf()
        {
            DivideCore(true);
        }

        public void DivideCore(bool makeHalf)
        {
            var add = new WorkItems();
            var divided = new WorkItems();
            var callender = _viewData.Original.Callender;
            foreach (var w in _viewData.Selected)
            {
                var period = w.Period;
                var offset = callender.GetOffset(period.From, period.To);
                if (offset < 1) continue;

                var w1 = w.Clone();
                if (!callender.TryApplyOffset(w.Period.From, offset / 2, out var p1)) return;
                w1.Period.To = p1;
                add.Add(w1);
                if (!makeHalf)
                {
                    var w2 = w.Clone();
                    if (!callender.TryApplyOffset(w.Period.From, +offset / 2 + 1, out var p2)) return;
                    w2.Period.From = p2;
                    add.Add(w2);
                }
                divided.Add(w);
            }
            if (!divided.Any()) return;

            var workItems = _viewData.Original.WorkItems;
            workItems.Remove(divided);
            workItems.Add(add);
            _viewData.Selected.Set(add);

            _viewData.UndoBuffer.Delete(divided);
            _viewData.UndoBuffer.Add(add);
            _viewData.UndoBuffer.Push();
        }

        internal void ShiftDays(int shift)
        {
            if (_viewData.Selected.Count() != 1) return;

            var before = _viewData.Selected.Unique;
            var after = before.Clone();
            if (after.Period.TryApplyOffset(shift, _viewData.Original.Callender, out var period)) return;
            after.Period = period;

            Replace(before, after);
            _viewData.Selected.Set(new WorkItems(after));
        }

        internal void ExpandDays(int shift)
        {
            if (_viewData.Selected.Count() != 1) return;

            var before = _viewData.Selected.Unique;
            var after = before.Clone();

            if (!_viewData.Original.Callender.TryApplyOffset(after.Period.To, shift, out var newTo)) return;
            if (newTo < after.Period.From) return;

            after.Period.To = newTo;

            Replace(before, after);
            _viewData.Selected.Set(new WorkItems(after));
        }
    }
}
