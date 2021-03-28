using ProjectsTM.Model;
using ProjectsTM.UI.Common;
using System;
using System.Windows.Forms;

namespace ProjectsTM.UI.Main
{
    public partial class ColorManagementForm : BaseForm
    {
        private readonly ColorConditions _colorConditions;

        public ColorManagementForm(ColorConditions colorConditions)
        {
            InitializeComponent();
            this._colorConditions = colorConditions;
            UpdateList();
        }

        private void UpdateList()
        {
            this.listView1.Items.Clear();
            foreach (var cond in _colorConditions)
            {
                var i = new ListViewItem(cond.Pattern);
                i.BackColor = cond.BackColor;
                i.ForeColor = cond.ForeColor;
                i.Tag = cond;
                this.listView1.Items.Add(i);
            }
        }

        private void bottonAdd_Click(object sender, EventArgs e)
        {
            using (var dlg = new ColorConditionEditorForm(new ColorCondition()))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                _colorConditions.Add(dlg.ColorCondition);
            }
            UpdateList();
        }

        private void bottonDelete_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count != 1) return;
            _colorConditions.Remove(_colorConditions.At(listView1.SelectedIndices[0]));
            UpdateList();
        }

        private void ButtonEdit_Click(object sender, EventArgs e)
        {
            Edit();
        }

        private void Edit()
        {
            if (listView1.SelectedItems.Count != 1) return;
            var index = listView1.SelectedIndices[0];
            var cond = _colorConditions.At(index);
            using (var dlg = new ColorConditionEditorForm(cond.Clone()))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                cond.Apply(dlg.ColorCondition);
            }
            UpdateList();
        }

        private void ListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Edit();
        }

        private void ButtonUP_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count != 1) return;
            var index = listView1.SelectedIndices[0];
            if (index == 0) return;
            _colorConditions.Up(index);
            UpdateList();
            listView1.Items[index - 1].Selected = true;
        }

        private void ButtonDown_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count != 1) return;
            var index = listView1.SelectedIndices[0];
            if (listView1.Items.Count == index + 1) return;
            _colorConditions.Down(index);
            UpdateList();
            listView1.Items[index + 1].Selected = true;
        }

        internal ColorConditions GetColorConditions()
        {
            return _colorConditions;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void listView1_Resize(object sender, EventArgs e)
        {
            listView1.Columns[listView1.Columns.Count - 1].Width = -2;
        }
    }
}
