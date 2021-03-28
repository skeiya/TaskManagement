using ProjectsTM.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ProjectsTM.Service
{
    public static class RSFileExportService
    {
        public static void Export(AppData appData)
        {
            string result = MakeTextAllData(appData);

            using (var dlg = new SaveFileDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                File.WriteAllText(dlg.FileName, result);
            }
        }

        public static void ExportSelectGetsudo(AppData appData, string selectGetsudo)
        {
            string result = MakeTextCore(appData, selectGetsudo);

            using (var dlg = new SaveFileDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                File.WriteAllText(dlg.FileName, result);
            }
        }

        public static string MakeTextAllData(AppData appData)
        {
            return MakeTextCore(appData, string.Empty);
        }

        private static string MakeTextCore(AppData appData, string selectGetsudo)
        {
            var members = GetMembers(appData.Members);
            var months = new List<Tuple<int, int>>();
            if (selectGetsudo.Length == 0)
            {
                months = GetMonths(appData.Callender);
            }
            else
            {
                try
                {
                    var words = selectGetsudo.Split('/');
                    months.Add(new Tuple<int, int>(int.Parse(words[0]), int.Parse(words[1])));
                }
                catch
                {
                    return string.Empty;
                }

            }
            var projects = GetProjects(appData.WorkItems);
            var rowCount = members.Count * projects.Count + 1;
            var colCount = months.Count + 3;
            var csv = new string[rowCount, colCount];

            MakeTitle(months, csv);
            MakeMembersAndProjects(members, projects, csv);
            MakeRatio(appData, members, months, projects, csv);
            return MakeText(rowCount, colCount, csv);
        }

        private static void MakeTitle(List<Tuple<int, int>> months, string[,] csv)
        {
            csv[0, 0] = "Com";
            csv[0, 1] = "Mem";
            csv[0, 2] = "Proj";
            var c = 3;
            foreach (var m in months)
            {
                csv[0, c] = m.Item1.ToString() + "/" + m.Item2.ToString();
                c++;
            }
        }

        private static string MakeText(int rowCount, int colCount, string[,] csv)
        {
            var result = string.Empty;
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                {
                    result += csv[row, col] + "\t";
                }
                result += Environment.NewLine;
            }

            return result;
        }

        private static void MakeRatio(AppData appData, List<Member> members, List<Tuple<int, int>> months, List<Project> projects, string[,] csv)
        {
            var c = 3;
            foreach (var month in months)
            {
                var r = 1;
                foreach (var member in members)
                {
                    foreach (var project in projects)
                    {
                        csv[r, c] = GetRatio(month.Item1, month.Item2, member, project, appData.Callender, appData.WorkItems);
                        r++;
                    }
                }
                c++;
            }
        }

        private static void MakeMembersAndProjects(List<Member> members, List<Project> projects, string[,] csv)
        {
            var r = 1;
            foreach (var m in members)
            {
                foreach (var p in projects)
                {
                    csv[r, 0] = m.Company;
                    csv[r, 1] = m.LastName + " " + m.FirstName;
                    csv[r, 2] = p.ToString();
                    r++;
                }
            }
        }

        private static List<Project> GetProjects(WorkItems workItems)
        {
            var result = new List<Project>();
            foreach (var wi in workItems)
            {
                if (!result.Contains(wi.Project)) result.Add(wi.Project);
            }
            return result;
        }

        private static List<Member> GetMembers(Members members)
        {
            var result = new List<Member>();
            foreach (var m in members)
            {
                result.Add(m);
            }
            result.Sort(); // 会社優先でソート
            return result;
        }

        private static List<Tuple<int, int>> GetMonths(Callender callender)
        {
            var result = new List<Tuple<int, int>>();

            var month = 0;
            foreach (var d in callender)
            {
                if (month != d.Month)
                {
                    result.Add(new Tuple<int, int>(d.Year, d.Month));
                    month = d.Month;
                }
            }
            return result;
        }

        private static string GetRatio(int year, int month, Member member, Project project, Callender callender, WorkItems workItems)
        {
            return string.Format("{0:f2}", GetTargetDays(year, month, member, project, workItems, callender) / (float)GetTotalDays(year, month, callender));
        }

        private static int GetTotalDays(int year, int month, Callender callender)
        {
            return callender.GetDaysOfGetsudo(year, month);
        }

        private static int GetTargetDays(int year, int month, Member member, Project project, WorkItems workItems, Callender callender)
        {
            return workItems.GetWorkItemDaysOfGetsudo(year, month, member, project, callender);
        }
    }
}
