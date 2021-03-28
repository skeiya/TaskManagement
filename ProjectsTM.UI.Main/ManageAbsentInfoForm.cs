using ProjectsTM.Model;
using ProjectsTM.UI.Common;
using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ProjectsTM.UI.Main
{
    public partial class ManageAbsentInfoForm : BaseForm
    {
        private readonly Member _member;
        public readonly AbsentTerms _absentTerms;
        private readonly Callender _callender;

        public AbsentTerms Edited => _absentTerms;

        public ManageAbsentInfoForm(Member member, AbsentTerms absentTerms, Callender callender)
        {
            InitializeComponent();
            _member = member;
            _absentTerms = absentTerms;
            _callender = callender;
            listBox1.DisplayMember = "NaturalString";
            this.Text = _member.NaturalString + "：不在情報の編集";
            UpdateList();
        }

        private void UpdateList()
        {
            listBox1.Items.Clear();
            foreach (var a in _absentTerms)
            {
                var from = a.Period.From == AbsentTerm.UnlimitedFrom ? AbsentTerm.UnlimitedStr : a.Period.From.ToString();
                var to = a.Period.To == AbsentTerm.UnlimitedTo ? AbsentTerm.UnlimitedStr : a.Period.To.ToString();
                listBox1.Items.Add($"不在期間 : {from} - {to}");
            }
        }

        private void ButtonEdit_Click(object sender, EventArgs e)
        {
            Edit();
        }

        private void Edit()
        {
            string selectedItem = (string)listBox1.SelectedItem;
            if (selectedItem == null) return;
            if (!TryParseAbsentTerm(selectedItem, out var before)) return;
            using (var dlg = new EditAbsentTermForm(_member, before, _callender))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                if (!dlg.TryGetAbsentTerm(out var after)) return;
                _absentTerms.Replace(before, after);
            }
            UpdateList();
        }

        private void ListBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Edit();
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            using (var dlg = new EditAbsentTermForm(_member, null, _callender))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                if (!dlg.TryGetAbsentTerm(out var after)) return;
                _absentTerms.Add(after);
            }
            UpdateList();
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            string selectedItem = (string)listBox1.SelectedItem;
            if (selectedItem == null) return;
            if (!TryParseAbsentTerm(selectedItem, out var absentTerm)) return;
            _absentTerms.Remove(absentTerm);
            UpdateList();
        }

        public bool TryParseAbsentTerm(string str, out AbsentTerm result)
        {
            result = new AbsentTerm(null, null);
            var m = Regex.Match(str, @"不在期間 : (.+) - (.+)");
            if (!m.Success) return false;
            result = new AbsentTerm(_member, ParsePeriod(m.Groups[1].Value, m.Groups[2].Value));
            return true;
        }

        public static Period ParsePeriod(string from, string to)
        {
            var f = from == AbsentTerm.UnlimitedStr ? AbsentTerm.UnlimitedFrom : CallenderDay.Parse(from);
            var t = to == AbsentTerm.UnlimitedStr ? AbsentTerm.UnlimitedTo : CallenderDay.Parse(to);
            return new Period(f, t);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
