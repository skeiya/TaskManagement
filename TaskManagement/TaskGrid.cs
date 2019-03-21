﻿using System.Collections.Generic;
using System.Drawing;

namespace TaskManagement
{
    internal class TaskGrid
    {
        private CommonGrid _grid;
        private Dictionary<int, Member> _colToMember = new Dictionary<int, Member>();
        private Dictionary<Member, int> _memberToCol = new Dictionary<Member, int>();
        private Dictionary<CallenderDay, int> _dayToRow = new Dictionary<CallenderDay, int>();
        private Dictionary<int, CallenderDay> _rowToDay = new Dictionary<int, CallenderDay>();
        private List<WorkItem> _workItems;

        public TaskGrid(AppData appData, Graphics g, Rectangle pageBounds, Font font)
        {
            _grid = new CommonGrid(g, new Font(font.FontFamily, 4));

            UpdateRowColMap(appData);

            _grid.RowCount = appData.Callender.Days.Count + Members.RowCount;
            SetRowHeight(pageBounds);

            _grid.ColCount = appData.Members.Count + Callender.ColCount;
            SetColWidth(g, pageBounds);

            _workItems = appData.WorkItems;
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

        private void UpdateRowColMap(AppData appData)
        {
            int c = Callender.ColCount;
            foreach (var m in appData.Members)
            {
                _colToMember.Add(c, m);
                _memberToCol.Add(m, c);
                c++;
            }

            int r = Members.RowCount;
            foreach (var d in appData.Callender.Days)
            {
                _dayToRow.Add(d, r);
                _rowToDay.Add(r, d);
                r++;
            }
        }

        internal RectangleF GetBounds(Period period, Member assignedMember)
        {
            var col = _memberToCol[assignedMember];
            var rowTop = _dayToRow[period.From];
            var rowBottom = _dayToRow[period.To];
            var top = _grid.GetCellBounds(rowTop, col);
            var bottom = _grid.GetCellBounds(rowBottom, col);
            return new RectangleF(top.Location, new SizeF(top.Width, bottom.Y - top.Y + top.Height));
        }

        internal void Draw()
        {
            DrawCallenderDays();
            DrawTeamMembers();
            DrawWorkItems();
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
                _grid.DrawString(_colToMember[c].Name, rect);
            }
        }

        private void DrawWorkItems()
        {
            foreach (var wi in _workItems)
            {
                var bounds = GetBounds(wi.Period, wi.AssignedMember);
                _grid.DrawString(wi.ToString(), bounds);
                _grid.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(bounds));
            }
        }
    }
}