﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskManagement
{
    public class WorkItem
    {
        public Project Project { get; private set; }
        public Tags Tags { get; private set; }
        public string Name { get; private set; }
        public Period Period { get; set; }
        public Member AssignedMember { get; set; }

        public WorkItem(Project project, string name, Tags tags, Period period, Member assignedMember)
        {
            this.Project = project;
            this.Name = name;
            Tags = tags;
            Period = period;
            AssignedMember = assignedMember;
        }

        public WorkItem()
        {
        }

        public override string ToString()
        {
            return "[" + Name + "][" + Project.ToString() + "][" + AssignedMember.ToString() + "][" + Tags.ToString() + "][" + Period.ToString() + "d]";
        }

        internal string ToSerializeString()
        {
            return Name + "," + Project.ToString() + "," + AssignedMember.ToSerializeString() + "," + Tags.ToString() + "," + Period.From.ToString() + "," + Period.To.ToString();
        }

        internal static WorkItem Parse(string value, Callender callender)
        {
            var words = value.Split(',');
            if (words.Length < 6) return null;
            var taskName = words[0];
            var project = new Project(words[1]);
            var member = Member.Parse(words[2]);
            var tags = Tags.Parse(words[3]);
            var period = new Period(CallenderDay.Parse(words[4]), CallenderDay.Parse(words[5]), callender);
            var result = new WorkItem(project, taskName, tags, period, member);
            return result;
        }

        internal void Edit(Project project, string v, Period period, Member member, Tags tags)
        {
            this.Project = project;
            this.Name = v;
            this.Period = period;
            this.AssignedMember = member;
            this.Tags = tags;
        }
    }
}