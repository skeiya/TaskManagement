﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace ProjectsTM.Model
{
    public class WorkItems : IEnumerable<WorkItem>
    {
        public WorkItems() { }
        public WorkItems(WorkItem w)
        {
            Add(w);
        }

        public WorkItems(IEnumerable<WorkItem> wis)
        {
            foreach (var w in wis) Add(w);
        }

        private readonly SortedDictionary<Member, MembersWorkItems> _items = new SortedDictionary<Member, MembersWorkItems>();

        public void Clear()
        {
            _items.Clear();
        }

        public IEnumerable<MembersWorkItems> EachMembers => _items.Values;

        public WorkItem Unique
        {
            get
            {
                Debug.Assert(this.Count() == 1);
                return _items.First().Value.First();
            }
        }

        public MembersWorkItems OfMember(Member m) => _items.ContainsKey(m) ? _items[m] : new MembersWorkItems();

        public XElement ToXml()
        {
            var xml = new XElement(nameof(WorkItems));
            foreach (var m in _items)
            {
                var eachMember = new XElement("WorkItemsOfEachMember");
                eachMember.SetAttributeValue("Name", m.Key.ToSerializeString());
                eachMember.Add(m.Value.ToXml());
                xml.Add(eachMember);
            }
            return xml;
        }

        public static WorkItems FromXml(XElement xml, int ver)
        {
            var result = new WorkItems();
            if (ver < 5)
            {
                foreach (var w in xml.Elements("WorkItem"))
                {
                    var assign = Member.Parse(w.Element("AssignedMember").Element("MemberElement").Value);
                    result.Add(WorkItem.FromXml(w, assign, ver));
                }
                return result;
            }
            foreach (var m in xml.Elements("WorkItemsOfEachMember"))
            {
                var assign = Member.Parse(m.Attribute("Name").Value);
                foreach (var w in m.Element(nameof(MembersWorkItems))
                    .Elements(nameof(WorkItem)))
                {
                    result.Add(WorkItem.FromXml(w, assign, ver));
                }
            }
            return result;
        }

        public void Add(IEnumerable<WorkItem> wis)
        {
            foreach (var wi in wis) Add(wi);
        }

        public void Add(WorkItem wi)
        {
            if (!_items.ContainsKey(wi.AssignedMember))
            {
                _items.Add(wi.AssignedMember, new MembersWorkItems());
            }
            _items[wi.AssignedMember].Add(wi);
        }

        public int GetWorkItemDaysOfGetsudo(int year, int month, Member member, Project project, Callender callender)
        {
            int result = 0;
            foreach (var wi in this.Where((w) => w.AssignedMember.Equals(member) && w.Project.Equals(project)))
            {
                foreach (var d in callender.GetPeriodDays(wi.Period))
                {
                    if (!Callender.IsSameGetsudo(d, year, month)) continue;
                    result++;
                }
            }
            return result;
        }

        public IEnumerator<WorkItem> GetEnumerator()
        {
            return _items.SelectMany((s) => s.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is WorkItems target)) return false;
            if (_items.Count != target._items.Count) return false;
            return _items.SequenceEqual(target._items);
        }

        public void Remove(IEnumerable<WorkItem> selected)
        {
            foreach (var wi in selected) Remove(wi);
        }

        public void Remove(WorkItem selected)
        {
            if (!_items[selected.AssignedMember].Remove(selected))
            {
                throw new System.Exception();
            }
        }

        public override int GetHashCode()
        {
            return -566117206 + EqualityComparer<SortedDictionary<Member, MembersWorkItems>>.Default.GetHashCode(_items);
        }

        public void SortByPeriodStartDate()
        {
            foreach (var workItems in this.EachMembers)
            {
                workItems.SortByPeriodStartDate();
            }
        }
    }
}
