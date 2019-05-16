﻿using System;
using System.Collections.Generic;
using System.Drawing;
using TaskManagement.UI;

namespace TaskManagement.UI
{
    class CommonGrid
    {
        private Dictionary<int, float> _rowToHeight = new Dictionary<int, float>();
        private Dictionary<int, float> _colToWidth = new Dictionary<int, float>();

        public CommonGrid(Font font)
        {
            Font = font;
        }

        public int RowCount { set; get; }
        public int ColCount { set; get; }

        public Font Font { get; }
        public Size Size
        {
            get
            {
                var width = 0f;
                foreach(var w in _colToWidth)
                {
                    width += w.Value;
                }
                var height = 0f;
                foreach(var h in _rowToHeight)
                {
                    height += h.Value;
                }
                return new Size((int)width, (int)height);
            }
        }

        public float RowHeight(int row)
        {
            return _rowToHeight[row];
        }

        public float ColWidth(int col)
        {
            return _colToWidth[col];
        }

        public void SetRowHeight(int r, float height)
        {
            _rowToHeight[r] = height;
        }

        public void SetColWidth(int c, float width)
        {
            _colToWidth[c] = width;
        }

        public SizeF MeasureString(Graphics g, string s)
        {
            return g.MeasureString(s, Font, 100, StringFormat.GenericTypographic);
        }

        public void DrawString(Graphics g, string s, RectangleF rect)
        {
            DrawString(g, s, rect, Color.Black);
        }

        internal void DrawString(Graphics g, string s, RectangleF rect, Color c)
        {
            var deflate = rect;
            deflate.X += 1;
            deflate.Y += 1;
            g.DrawString(s, Font, BrushCache.GetBrush(c), deflate, StringFormat.GenericTypographic);
        }

        internal void DrawMileStoneLine(Graphics g, float bottom, Color color)
        {
            using (var brush = new SolidBrush(color))
            {
                var height = 5f;
                var width = GetFullWidth();
                var x = 0f;
                var y = bottom - height;
                g.FillRectangle(brush, x, y, width, height);
            }
        }

        private float GetFullWidth()
        {
            var result = 0f;
            for (var c = 0; c < ColCount; c++)
            {
                result += ColWidth(c);
            }
            return result;
        }
    }
}
