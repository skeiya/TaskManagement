﻿using ProjectsTM.Model;
using ProjectsTM.UI.TaskList;
using ProjectsTM.ViewModel;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ProjectsTM.UI.Main
{
    class TaskListManager
    {
        private readonly ViewData _viewData;
        private readonly PatternHistory _patternHistory;
        private readonly IWin32Window _parent;

        public TaskListManager(ViewData viewData, PatternHistory patternHistory, IWin32Window parent)
        {
            _viewData = viewData;
            _patternHistory = patternHistory;
            _parent = parent;
        }

        private readonly List<TaskListForm> taskListForms = new List<TaskListForm>();

        internal void UpdateView()
        {
            foreach (var f in taskListForms)
            {
                f.UpdateView();
            }
        }

        internal void UpdateMySetting(Member me)
        {
            taskListForms.ForEach(f =>
            {
                f.UpdateMySetting(me);
            });
        }

        internal void Show(Member me)
        {
            ShowCore(new TaskListOption(), me);
        }

        internal void ShowOverlapCheck()
        {
            var option = new TaskListOption()
            {
                ErrorDisplayType = ErrorDisplayType.OverlapOnly,
                IsShowMS = false,
            };
            ShowCore(option, null);
        }

        internal void ShowUsersError(Member me)
        {
            var option = new TaskListOption()
            {
                Pattern = me.ToString(),
                ErrorDisplayType = ErrorDisplayType.ErrorOnly,
                IsShowMS = false,
            };
            ShowCore(option, me);
        }

        private void ShowCore(TaskListOption option, Member me)
        {
            var f = new TaskListForm(_viewData, _patternHistory, option, me);
            f.FormClosed += taskListForm_FormClosed;
            f.Show(_parent);
            taskListForms.Add(f);
        }

        private void taskListForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!(sender is TaskListForm f)) return;
            taskListForms.Remove(f);
            f.Dispose();
        }
    }
}
