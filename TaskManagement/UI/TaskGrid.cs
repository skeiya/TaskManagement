﻿using System.Collections.Generic;
using System.Drawing;

namespace TaskManagement
{
    public class TaskGrid
    {
        private CommonGrid _grid;
        private Dictionary<int, Member> _colToMember = new Dictionary<int, Member>();
        private Dictionary<Member, int> _memberToCol = new Dictionary<Member, int>();
        private Dictionary<CallenderDay, int> _dayToRow = new Dictionary<CallenderDay, int>();
        private Dictionary<int, CallenderDay> _rowToDay = new Dictionary<int, CallenderDay>();
        private ColorConditions _colorConditions;

        public TaskGrid(ViewData viewData, Graphics g, Rectangle pageBounds, Font font)
        {
            _grid = new CommonGrid(g, font);

            UpdateRowColMap(viewData);

            _grid.RowCount = viewData.GetDaysCount() + Members.RowCount;
            SetRowHeight(pageBounds);

            _grid.ColCount = viewData.GetFilteredMembers().Count + Callender.ColCount;
            SetColWidth(g, pageBounds);

            _colorConditions = viewData.Original.ColorConditions;
        }

        private void SetColWidth(Graphics g, Rectangle pageBounds)
        {
            var year = _grid.MeasureString("0000/").Width;
            var month = _grid.MeasureString("00/").Width;
            var day = _grid.MeasureString("00").Width;
            _grid.SetColWidth(0, year);
            _grid.SetColWidth(1, month);
            _grid.SetColWidth(2, day);
            var member = ((float)(pageBounds.Width) - year - month - day) / (_grid.ColCount - Callender.ColCount);
            for (int c = Callender.ColCount; c < _grid.ColCount; c++)
            {
                _grid.SetColWidth(c, member);
            }

        }

        private void SetRowHeight(Rectangle pageBounds)
        {
            var company = _grid.MeasureString("K").Height;
            var name = _grid.MeasureString("下村HF").Height * 1.5f;
            _grid.SetRowHeight(0, company);
            _grid.SetRowHeight(1, name);
            var height = ((float)pageBounds.Height - name) / (_grid.RowCount - Members.RowCount);
            for (int r = Members.RowCount; r < _grid.RowCount; r++)
            {
                _grid.SetRowHeight(r, height);
            }

        }

        private void UpdateRowColMap(ViewData viewData)
        {
            int c = Callender.ColCount;
            foreach (var m in viewData.GetFilteredMembers())
            {
                _colToMember.Add(c, m);
                _memberToCol.Add(m, c);
                c++;
            }

            int r = Members.RowCount;
            foreach (var d in viewData.GetFilteredDays())
            {
                _dayToRow.Add(d, r);
                _rowToDay.Add(r, d);
                r++;
            }
        }

        public WorkItem PickFromPoint(PointF point, ViewData viewData)
        {
            var member = GetMemberFromX(point.X);
            var day = GetDayFromY(point.Y);
            if (member == null || day == null) return null;
            foreach (var wi in viewData.Original.WorkItems)
            {
                if (!wi.AssignedMember.Equals(member)) continue;
                if (!wi.Period.Contains(day)) continue;
                return wi;
            }
            return null;
        }

        public Member GetMemberFromX(float x)
        {
            float left = 0;
            for (int c = 0; c < _grid.ColCount; c++)
            {
                var w = _grid.ColWidth(c);
                if (left <= x && x < left + w)
                {
                    Member m;
                    if (_colToMember.TryGetValue(c, out m)) return m;
                    return null;
                }
                left += w;
            }
            return null;
        }

        public CallenderDay GetDayFromY(float y)
        {
            float top = 0;
            for (int r = 0; r < _grid.RowCount; r++)
            {
                var h = _grid.RowHeight(r);
                if (top <= y && y < top + h)
                {
                    CallenderDay d;
                    if (_rowToDay.TryGetValue(r, out d)) return d;
                    return null;
                }
                top += h;
            }
            return null;
        }

        public RectangleF GetBounds(Period period, Member assignedMember)
        {
            var col = _memberToCol[assignedMember];
            var rowTop = _dayToRow[period.From];
            var rowBottom = _dayToRow[period.To];
            var top = _grid.GetCellBounds(rowTop, col);
            var bottom = _grid.GetCellBounds(rowBottom, col);
            return new RectangleF(top.Location, new SizeF(top.Width, bottom.Y - top.Y + top.Height));
        }

        public void Draw(ViewData viewData)
        {
            DrawCallenderDays();
            DrawTeamMembers();
            DrawWorkItems(viewData);
        }

        private void DrawCallenderDays()
        {
            int y = 0;
            int m = 0;
            for (int r = Members.RowCount; r < _grid.RowCount; r++)
            {
                var year = _rowToDay[r].Year;
                if (y == year) continue;
                y = year;
                var rect = _grid.GetCellBounds(r, 0);
                rect.Height = rect.Height * 2;//TODO: 適当に広げている
                _grid.DrawString(year.ToString() + "/", rect);
            }
            for (int r = Members.RowCount; r < _grid.RowCount; r++)
            {
                var month = _rowToDay[r].Month;
                if (m == month) continue;
                m = month;
                var rect = _grid.GetCellBounds(r, 1);
                rect.Height = rect.Height * 2;//TODO: 適当に広げている
                _grid.DrawString(month.ToString() + "/", rect);
            }
            for (int r = Members.RowCount; r < _grid.RowCount; r++)
            {
                var rect = _grid.GetCellBounds(r, 2);
                _grid.DrawString(_rowToDay[r].Day.ToString(), rect);
            }
        }

        private void DrawTeamMembers()
        {
            for (int c = Callender.ColCount; c < _grid.ColCount; c++)
            {
                var rect = _grid.GetCellBounds(0, c);
                _grid.DrawString(_colToMember[c].Company, rect);
            }
            for (int c = Callender.ColCount; c < _grid.ColCount; c++)
            {
                var rect = _grid.GetCellBounds(1, c);
                _grid.DrawString(_colToMember[c].DisplayName, rect);
            }
        }

        private void DrawWorkItems(ViewData viewData)
        {
            foreach (var wi in viewData.GetFilteredWorkItems())
            {
                var bounds = GetBounds(GetDrawPeriod(viewData, wi), wi.AssignedMember);
                var color = _colorConditions.GetMatchColor(wi.ToString(viewData.Original.Callender));
                if (color != null) _grid.Graphics.FillRectangle(new SolidBrush(color.Value), Rectangle.Round(bounds));
                _grid.DrawString(wi.ToString(viewData.Original.Callender), bounds);
                _grid.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(bounds));
            }

            if (viewData.Selected != null)
            {
                var bounds = GetBounds(GetDrawPeriod(viewData, viewData.Selected), viewData.Selected.AssignedMember);
                _grid.Graphics.DrawRectangle(Pens.LightGreen, Rectangle.Round(bounds));
            }
        }

        private static Period GetDrawPeriod(ViewData viewData, WorkItem wi)
        {
            var org = wi.Period;
            var filter = viewData.GetFilteredPeriod();
            if (filter == null) return org;
            var from = org.From;
            var to = org.To;
            if (from.LesserThan(filter.From)) from = filter.From;
            if (filter.To.LesserThan(to)) to = filter.To;
            return new Period(from, to);
        }
    }
}