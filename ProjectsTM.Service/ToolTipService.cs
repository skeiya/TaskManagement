using ProjectsTM.Model;
using ProjectsTM.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ProjectsTM.Service
{
    public class ToolTipService : IDisposable
    {
        private readonly ToolTip _toolTip = new ToolTip();
        private bool disposedValue;
        private readonly ViewData _viewData;
        private readonly Control _parentControl;
        private readonly EditorFindService _editorFindService;
        private readonly Func<WorkItem> _getCusorWorkItem;
        private readonly Func<CallenderDay> _getCursorDay;
        private readonly Func<bool> _isDragging;
        private readonly Func<bool> _isMilestoneArea;
        private readonly Timer _timer;

        public ToolTipService(Control parent, ViewData viewData, EditorFindService editorFindService, Func<WorkItem> getCusorWorkItem, Func<CallenderDay> getCursorDay, Func<bool> isDragging, Func<bool> isMilestoneArea)
        {
            this._toolTip.ShowAlways = true;
            this._parentControl = parent;
            this._viewData = viewData;
            this._editorFindService = editorFindService;
            this._getCusorWorkItem = getCusorWorkItem;
            this._getCursorDay = getCursorDay;
            this._isDragging = isDragging;
            this._isMilestoneArea = isMilestoneArea;
            this._timer = new Timer();
            this._timer.Interval = 100;
            this._timer.Tick += _timer_Tick;
            this._timer.Start();
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            if (_isDragging())
            {
                Hide();
                return;
            }
            if (_isMilestoneArea())
            {
                UpdateHoveringMileStoneText();
                return;
            }
            Update(_getCusorWorkItem(), _viewData.Original.Callender);
        }

        private string GetDescrptionFromOtherWorkItem(WorkItem hoveringWorkItem)
        {
            foreach (var w in _viewData.FilteredItems.WorkItems)
            {
                if (w.Name != hoveringWorkItem.Name) continue;
                if (w.Equals(hoveringWorkItem)) continue;
                if (string.IsNullOrEmpty(w.Description)) continue;

                return w.Description +
                    Environment.NewLine +
                    "※同名作業項目のメモ※";
            }
            return string.Empty;
        }

        private string CreateDescriptionContent(WorkItem hoveringWorkItem)
        {
            if (!string.IsNullOrEmpty(hoveringWorkItem.Description)) return hoveringWorkItem.Description;
            return GetDescrptionFromOtherWorkItem(hoveringWorkItem);
        }

        private static StringBuilder GetDescriptionContent(string strDescription)
        {
            var result = new StringBuilder();
            if (string.IsNullOrEmpty(strDescription)) return result;
            result.AppendLine();
            result.AppendLine("---作業項目メモ---");
            result.AppendLine(strDescription);
            return result;
        }

        private StringBuilder GetDescription(WorkItem hoveringWorkItem)
        {
            string result = CreateDescriptionContent(hoveringWorkItem);
            return GetDescriptionContent(result);
        }

        private string GetDisplayString(WorkItem wi, Callender callender, string editor)
        {
            StringBuilder s = new StringBuilder();
            s.Append("名前:"); s.AppendLine(wi.Name);
            s.Append("物件:"); s.AppendLine(wi.Project.ToString());
            s.Append("担当:"); s.AppendLine(wi.AssignedMember.ToString());
            s.Append("タグ:"); s.AppendLine(wi.Tags.ToString());
            s.Append("状態:"); s.AppendLine(wi.State.ToString());
            s.AppendLine();
            s.Append("開始:"); s.AppendLine(wi.Period.From.ToString());
            s.Append("終了:"); s.AppendLine(wi.Period.To.ToString());
            s.Append("人日:"); s.AppendLine(callender.GetPeriodDayCount(wi.Period).ToString());
            s.AppendLine();
            if (!string.IsNullOrEmpty(editor))
            {
                s.Append("最終更新："); s.AppendLine(editor);
            }
            s.Append(GetDescription(wi));
            return s.ToString();
        }

        private void UpdateHoveringMileStoneText()
        {
            var day = _getCursorDay();
            if (day == null)
            {
                Hide();
                return;
            }
            var ms = _viewData.Original.MileStones.Where(m => day.Equals(m.Day));
            if (!ms.Any())
            {
                Hide();
                return;
            }
            _toolTip.SetToolTip(_parentControl, GetMilestoneDisplay(day, ms));
        }

        private static string GetMilestoneDisplay(CallenderDay day, IEnumerable<MileStone> mss)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(day.ToString());
            foreach (var m in mss) sb.AppendLine(m.Name);
            return sb.ToString();
        }

        public async void Update(WorkItem wi, Callender callender)
        {
            if (WorkItem.Invalid.Equals(wi))
            {
                this.Hide();
                return;
            }
            if (_editorFindService.TryFind(wi, out var dislayString))
            {
                string value = GetDisplayString(wi, callender, dislayString);
                if (!value.Equals(_toolTip.GetToolTip(_parentControl))) _toolTip.SetToolTip(_parentControl, value);
                return;
            }
            string s = GetDisplayString(wi, callender, string.Empty);
            if (!s.Equals(_toolTip.GetToolTip(_parentControl))) _toolTip.SetToolTip(_parentControl, s);

            var editor = await _editorFindService.Find(wi).ConfigureAwait(true);
            if (disposedValue) return;
            if (!wi.Equals(_getCusorWorkItem()))
            {
                Hide();
                return;
            }
            s = GetDisplayString(wi, callender, editor);
            if (!s.Equals(_toolTip.GetToolTip(_parentControl))) _toolTip.SetToolTip(_parentControl, s);
        }

        public void Hide() { _toolTip.Hide(_parentControl); }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _toolTip.Dispose();
                    _timer.Dispose();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~ToolTipService()
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
