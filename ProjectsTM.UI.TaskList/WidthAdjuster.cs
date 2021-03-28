using FreeGridControl;
using System;
using System.Windows.Forms;

namespace ProjectsTM.UI.TaskList
{
    internal class WidthAdjuster
    {
        private RawPoint _orgLocation;
        private int _orgWidth = -1;
        private readonly Func<RawPoint, bool> _isAdjustCol;
        private Action<int> _adjustWidth;

        public WidthAdjuster(Func<RawPoint, bool> isAdjustCol)
        {
            this._isAdjustCol = isAdjustCol;
        }

        internal void Start(RawPoint location, int orgWidth, Action<int> adjustWidth)
        {
            _orgLocation = location;
            _orgWidth = orgWidth;
            _adjustWidth = adjustWidth;
        }

        internal void End()
        {
            _orgWidth = -1;
            _adjustWidth = null;
        }

        internal Cursor Update(RawPoint location)
        {
            var updatedWidth = _orgWidth + (location.X - _orgLocation.X);
            _adjustWidth?.Invoke(updatedWidth);
            return IsActive || _isAdjustCol(location) ? Cursors.SizeWE : Cursors.Default;
        }

        internal bool IsActive => _orgWidth != -1;
    }
}