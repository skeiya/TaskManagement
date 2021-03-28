using ProjectsTM.Logic;
using ProjectsTM.Model;
using ProjectsTM.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ProjectsTM.Service
{
    public class FilterComboBoxService
    {
        private readonly ViewData _viewData;
        private readonly ToolStripComboBox _toolStripComboBoxFilter;
        private string DirPath => Path.Combine(Path.GetDirectoryName(_filepPath), "filters");
        private readonly List<string> _allPaths = new List<string>();

        private const string FilePrefix = "file:";
        private const string CompanyPrefix = "company:";
        private const string ProjectPrefix = "project:";
        private const string AllKeyword = "ALL";

        public string Text
        {
            get
            {
                return _toolStripComboBoxFilter.Text;
            }
            set
            {
                _toolStripComboBoxFilter.Text = value;
            }
        }


        public FilterComboBoxService(ViewData viewData, ToolStripComboBox toolStripComboBoxFilter)
        {
            _viewData = viewData;
            this._toolStripComboBoxFilter = toolStripComboBoxFilter;
            this._toolStripComboBoxFilter.Items.Add(AllKeyword);
            this._toolStripComboBoxFilter.DropDown += ToolStripComboBoxFilter_DropDown;
        }

        private void ToolStripComboBoxFilter_DropDown(object sender, EventArgs e)
        {
            UpdateFilePart(_filepPath);
            UpdateAppDataPart();
        }

        private string _filepPath = string.Empty;

        public void UpdateFilePart(string filePath)
        {
            _filepPath = filePath;
            var selectedItem = _toolStripComboBoxFilter.SelectedItem;
            var selectedText = selectedItem == null ? null : _toolStripComboBoxFilter.SelectedItem.ToString();
            if (_toolStripComboBoxFilter.Items.Count != 0) DetachEvent();
            UpdateByFiles();
            _toolStripComboBoxFilter.SelectedIndex = GetIndexBinder(selectedText);
            AttachEvent();
        }

        public void UpdateAppDataPart()
        {
            var selectedItem = _toolStripComboBoxFilter.SelectedItem;
            var selectedText = selectedItem == null ? null : _toolStripComboBoxFilter.SelectedItem.ToString();
            UpdateByProjects();
            UpdateByCompany();
            _toolStripComboBoxFilter.SelectedIndex = GetIndexBinder(selectedText);
        }

        private void UpdateByCompany()
        {
            PartClear(CompanyPrefix);
            var insertIdx = GetCompanyTopIndex();
            foreach (var com in GetCompanies())
            {
                var members = GetMembersConcerningWithCompany(com);
                _toolStripComboBoxFilter.Items.Insert(insertIdx++, CompanyPrefix + com + "(" + members.Count.ToString() + ")");
            }
        }

        private void PartClear(string prefix)
        {
            for (var idx = _toolStripComboBoxFilter.Items.Count - 1; idx >= 0; idx--)
            {
                if (!_toolStripComboBoxFilter.Items[idx].ToString().StartsWith(prefix)) continue;
                _toolStripComboBoxFilter.Items.RemoveAt(idx);
            }
        }

        private int GetCompanyTopIndex()
        {
            for (int idx = _toolStripComboBoxFilter.Items.Count - 1; idx >= 0; idx--)
            {
                if (!_toolStripComboBoxFilter.Items[idx].ToString().StartsWith(ProjectPrefix)) continue;
                return idx + 1;
            }
            return GetProjectTopIndex();
        }

        private IEnumerable<string> GetCompanies()
        {
            return _viewData.Original.WorkItems.Select(w => w.AssignedMember.Company).Distinct();
        }

        private void UpdateByProjects()
        {
            PartClear(ProjectPrefix);
            var insertIdx = GetProjectTopIndex();
            foreach (var pro in GetProjects())
            {
                _toolStripComboBoxFilter.Items.Insert(insertIdx++, ProjectPrefix + pro);
            }
        }

        private int GetProjectTopIndex()
        {
            for (var idx = _toolStripComboBoxFilter.Items.Count - 1; idx >= 0; idx--)
            {
                if (!_toolStripComboBoxFilter.Items[idx].ToString().StartsWith(FilePrefix)) continue;
                return idx + 1;
            }
            return GetFileTopIndex();
        }

        private IEnumerable<Project> GetProjects()
        {
            return _viewData.Original.WorkItems.Select(w => w.Project).Distinct();
        }

        private int GetIndexBinder(string selectedText)
        {
            try
            {
                var idx = _toolStripComboBoxFilter.Items.IndexOf(selectedText);
                if (idx < 0) idx = 0;
                return idx;
            }
            catch
            {
                return 0;
            }
        }

        private void UpdateByFiles()
        {
            PartClear(FilePrefix);
            _allPaths.Clear();
            if (string.IsNullOrEmpty(_filepPath)) return;
            if (!Directory.Exists(DirPath)) return;
            _allPaths.AddRange(Directory.GetFiles(DirPath));
            int insertIdx = GetFileTopIndex();
            foreach (var f in _allPaths)
            {
                _toolStripComboBoxFilter.Items.Insert(insertIdx++, FilePrefix + Path.GetFileNameWithoutExtension(f));
            }
        }

        private int GetFileTopIndex()
        {
            return _toolStripComboBoxFilter.Items.IndexOf(AllKeyword) + 1;
        }

        private void AttachEvent()
        {
            _toolStripComboBoxFilter.SelectedIndexChanged += ToolStripComboBoxFilter_SelectedIndexChanged;
        }

        private void DetachEvent()
        {
            _toolStripComboBoxFilter.SelectedIndexChanged -= ToolStripComboBoxFilter_SelectedIndexChanged;
        }

        private void ToolStripComboBoxFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            _viewData.Selected.Clear();
            var idx = _toolStripComboBoxFilter.SelectedIndex;
            if (idx == 0)
            {
                _viewData.SetFilter(Filter.All(_viewData));
                return;
            }
            idx--;
            if (!TryGetFilterByFiles(ref idx, out var filter)) 
            {
                if (!TryGetFilterByProjects(ref idx, out filter))
                {
                    if (!TryGetFilterByCompanies(ref idx, out filter)) return;
                }
            }
            _viewData.SetFilter(filter);
        }

        private bool TryGetFilterByCompanies(ref int idx, out Filter result)
        {
            result = new Filter();
            var companies = GetCompanies();
            if (companies.Count() <= idx)
            {
                idx -= companies.Count();
                return false;
            }
            var company = companies.ElementAt(idx);
            var members = GetMembersConcerningWithCompany(company);
            result = new Filter(null, null, members, false, company, false);
            return true;
        }

        private Members GetMembersConcerningWithCompany(string com)
        {
            var members = new Members();
            foreach (var m in _viewData.FilteredItems.MatchMembers(@"^\[.*?]\[.*?]\[.*?\(" + com + @"\)]\[.*?]\[.*?]"))
            {
                members.Add(m);
            }

            return members;
        }

        private bool TryGetFilterByProjects(ref int idx, out Filter result)
        {
            result = new Filter();
            var projects = GetProjects();
            if (projects.Count() <= idx)
            {
                idx -= projects.Count();
                return false;
            }
            var pro = projects.ElementAt(idx);
            var members = new Members();
            foreach (var m in _viewData.FilteredItems.MatchMembers(@"^\[.*?\]\[" + pro.ToString() + @"\]"))
            {
                members.Add(m);
            }
            result = new Filter(null, null, members, false, pro.ToString(), false);
            return true;
        }

        private bool TryGetFilterByFiles(ref int idx, out Filter result)
        {
            result = new Filter();
            if (_allPaths.Count <= idx)
            {
                idx -= _allPaths.Count;
                return false;
            }
            var path = _allPaths[idx];
            if (!File.Exists(path))
            {
                return false;
            }
            using (var rs = StreamFactory.CreateReader(path))
            {
                result = Filter.FromXml(XElement.Load(rs));
                return true;
            }
        }
    }
}
