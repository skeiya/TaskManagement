﻿namespace TaskManagement
{
    public class WorkItem
    {
        private Project Project;
        private string Name;
        public Period Period { get; }
        public Member AssignedMember { get; internal set; }

        public WorkItem(Project project, string name, Period period, Member assignedMember)
        {
            this.Project = project;
            this.Name = name;
            Period = period;
            AssignedMember = assignedMember;
        }

        public override string ToString()
        {
            return Name + " " + Project.ToString() + " " + Period.ToString() + "d";
        }
    }
}