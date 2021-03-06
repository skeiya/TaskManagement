﻿using ProjectsTM.Logic;
using ProjectsTM.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ProjectsTM.ViewModel
{
    public class ImageBuffer : IDisposable
    {
        private readonly Bitmap _bitmap;
        private readonly Graphics _bitmapGraphics;
        private readonly Dictionary<Member, HashSet<WorkItem>> _validList = new Dictionary<Member, HashSet<WorkItem>>();

        public Graphics Graphics => _bitmapGraphics;

        public Image Image => _bitmap;

        public ImageBuffer(int width, int height)
        {
            if (_bitmap == null)
            {
                _bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                _bitmapGraphics = System.Drawing.Graphics.FromImage(_bitmap);
                _bitmapGraphics.Clear(Control.DefaultBackColor);
            }

        }

        public void Validate(WorkItem wi)
        {
            if (IsValid(wi)) return;

            if (_validList.TryGetValue(wi.AssignedMember, out var workItems))
            {
                workItems.Add(wi);
                return;
            }
            _validList.Add(wi.AssignedMember, new HashSet<WorkItem>() { wi });
        }

        public bool IsValid(WorkItem wi)
        {
            if (!_validList.TryGetValue(wi.AssignedMember, out var workItems)) return false;
            return workItems.Contains(wi);
        }

        public void Invalidate(IEnumerable<Member> members, IWorkItemGrid grid)
        {
            //該当メンバの列を少し広めにクリアFill
            foreach (var m in members)
            {
                if (!grid.TryGetMemberDrawRect(m, out var rect)) continue;
                rect.Inflate(1, 1);
                _bitmapGraphics.FillRectangle(BrushCache.GetBrush(Control.DefaultBackColor), rect);
            }
            foreach (var m in grid.GetNeighbers(members))
            {
                _validList.Remove(m);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _bitmapGraphics.Dispose();
                    _bitmap.Dispose();
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~InvalidArea()
        // {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
