using ProjectsTM.Model;
using System;
using System.Linq;
using System.Windows.Forms;

namespace ProjectsTM.UI.Common
{
    public partial class EditAbsentTermForm : BaseForm
    {
        private readonly Member _member;
        private readonly Callender _callender;

        public EditAbsentTermForm(Member member, AbsentTerm absentTerm, Callender callender)
        {
            InitializeComponent();
            this._member = member;
            if (absentTerm == null) absentTerm = new AbsentTerm(member, new Period());
            this._callender = callender;
            Period p = absentTerm.Period;
            textBoxFrom.Text = (p?.From == null || p.From == AbsentTerm.UnlimitedFrom) ? string.Empty : p.From.ToString();
            textBoxTo.Text = (p?.To == null || p.To == AbsentTerm.UnlimitedTo) ? string.Empty : p.To.ToString();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (!CheckEdit()) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        bool CheckEdit()
        {
            return TryGetAbsentTerm(out var _);
        }

        public bool TryGetAbsentTerm(out AbsentTerm result)
        {
            result = new AbsentTerm(null, null);
            if (!TryGetPeriod(textBoxFrom.Text, textBoxTo.Text, out var period)) return false;
            result = new AbsentTerm(_member, period);
            return true;
        }

        private bool TryGetPeriod(string fromText, string toText, out Period result)
        {
            result = new Period();
            var from = string.IsNullOrEmpty(fromText) ? AbsentTerm.UnlimitedFrom : GetDayByDate(fromText);
            var to = string.IsNullOrEmpty(toText) ? AbsentTerm.UnlimitedTo : GetDayByDate(toText);
            if (!CheckAbsentPeriod(from, to)) return false;
            result = new Period(from, to);
            return true;
        }

        private bool CheckAbsentPeriod(CallenderDay from, CallenderDay to)
        {
            if (from == null || to == null) return false;
            if (from == AbsentTerm.UnlimitedFrom && to == AbsentTerm.UnlimitedTo) return false;
            if (from != AbsentTerm.UnlimitedFrom && !_callender.Contains(from)) return false;
            if (to != AbsentTerm.UnlimitedTo && !_callender.Contains(to)) return false;
            if (from >= to) return false;
            return true;
        }

        private static CallenderDay GetDayByDate(string text)
        {
            return CallenderDay.Parse(text);
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
