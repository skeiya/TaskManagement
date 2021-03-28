using FreeGridControl;
using ProjectsTM.Model;
using ProjectsTM.ViewModel;
using System.Collections.Generic;
using System.Linq;

namespace ProjectsTM.UI.Main
{
    class RowColResolver
    {
        private readonly Dictionary<CallenderDay, RowIndex> _day2RowCache = new Dictionary<CallenderDay, RowIndex>();
        private readonly Dictionary<RowIndex, CallenderDay> _row2DayChache = new Dictionary<RowIndex, CallenderDay>();
        private readonly Dictionary<Member, ColIndex> _member2ColChache = new Dictionary<Member, ColIndex>();
        private readonly Dictionary<ColIndex, Member> _col2MemberChache = new Dictionary<ColIndex, Member>();
        private readonly GridControl _grid;
        private readonly ViewData _viewData;

        public RowColResolver(GridControl grid, ViewData viewData)
        {
            this._grid = grid;
            this._viewData = viewData;
        }

        public void ClearCache()
        {
            _day2RowCache.Clear();
            _row2DayChache.Clear();
            _member2ColChache.Clear();
            _col2MemberChache.Clear();
        }

        internal CallenderDay Row2Day(RowIndex r)
        {
            if (_row2DayChache.TryGetValue(r, out var day)) return day;
            if (r == null) return CallenderDay.Invalid;
            var days = _viewData.FilteredItems.Days;
            if (r.Value - _grid.FixedRowCount < 0 || days.Count() <= r.Value - _grid.FixedRowCount) return CallenderDay.Invalid;
            day = days.ElementAt(r.Value - _grid.FixedRowCount);
            _row2DayChache.Add(r, day);
            return day;
        }

        internal ColIndex Member2Col(Member m, IEnumerable<Member> members)
        {
            if (_member2ColChache.TryGetValue(m, out var col)) return col;
            foreach (var c in ColIndex.Range(_grid.FixedColCount, _grid.ColCount - _grid.FixedColCount))
            {
                if (members.ElementAt(c.Value - _grid.FixedColCount).Equals(m))
                {
                    _member2ColChache.Add(m, c);
                    return c;
                }
            }
            return new ColIndex(_grid.FixedColCount);
        }

        internal bool Day2Row(CallenderDay day, out RowIndex result)
        {
            if (_day2RowCache.TryGetValue(day, out result)) return true;
            foreach (var r in RowIndex.Range(_grid.FixedRowCount, _grid.RowCount - _grid.FixedRowCount))
            {
                if (_viewData.FilteredItems.Days.ElementAt(r.Value - _grid.FixedRowCount).Equals(day))
                {
                    _day2RowCache.Add(day, r);
                    result = r;
                    return true;
                }
            }
            return false;
        }

        internal Member Col2Member(ColIndex c)
        {
            if (_col2MemberChache.TryGetValue(c, out var member)) return member;
            if (c == null) return Member.Invalid;
            var members = _viewData.FilteredItems.Members;
            if (c.Value - _grid.FixedColCount < 0 || members.Count() <= c.Value - _grid.FixedColCount) return Member.Invalid;
            var result = _viewData.FilteredItems.Members.ElementAt(c.Value - _grid.FixedColCount);
            _col2MemberChache.Add(c, result);
            return result;
        }
    }
}
