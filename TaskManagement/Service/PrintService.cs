﻿using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using TaskManagement.UI;
using TaskManagement.ViewModel;

namespace TaskManagement.Service
{
    class PrintService
    {
        private PrintDocument _printDocument = new PrintDocument();
        private PrintPreviewDialog _printPreviewDialog1 = new PrintPreviewDialog();
        private Font _font;
        private ViewData _viewData;

        internal PrintService(ViewData viewData, Font font)
        {
            _font = font;
            _viewData = viewData;
            foreach (PaperSize s in _printDocument.DefaultPageSettings.PrinterSettings.PaperSizes)
            {
                if (s.Kind == PaperKind.A3)
                {
                    _printDocument.DefaultPageSettings.PaperSize = s;
                }
            }
            _printDocument.DefaultPageSettings.Landscape = true;
            _printDocument.PrintPage += PrintDocument_PrintPage;
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            var grid = new WorkItemGrid();
            grid.Size = e.PageBounds.Size;
            grid.Initialize(_viewData);
            grid.AdjustForPrint(e.PageBounds);
            grid.RefreshDraw();
            grid.Print(e.Graphics);
        }

        internal void Print()
        {
            _font = new Font(_font.FontFamily, _viewData.FontSize);
            _printPreviewDialog1.Document = _printDocument;
            using (var dlg = new PrintDialog())
            {
                dlg.Document = _printPreviewDialog1.Document;
                if (dlg.ShowDialog() != DialogResult.OK) return;
            }
            if (_printPreviewDialog1.ShowDialog() != DialogResult.OK) return;
        }
    }
}
