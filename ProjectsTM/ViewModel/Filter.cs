﻿using ProjectsTM.Model;
using System;
using System.Collections.Generic;

namespace ProjectsTM.ViewModel
{
    public class Filter : IEquatable<Filter>
    {
        public Filter() { }
        public Filter(string v, Period period, Members hideMembers, bool isFreeTimeMemberShow, MileStoneFilters mileStoneFilters)
        {
            WorkItem = v;
            Period = period;
            HideMembers = hideMembers;
            IsFreeTimeMemberShow = isFreeTimeMemberShow;
            if (mileStoneFilters != null) MileStoneFilters = mileStoneFilters;
        }

        public Members HideMembers { get; set; }
        public Period Period { get; set; }
        public string WorkItem { get; set; }
        public bool IsFreeTimeMemberShow { get; set; } = true;
        public MileStoneFilters MileStoneFilters { get; set; } = new MileStoneFilters();
        public static Filter All => new Filter(null, null, new Members(), false, null);

        public bool Equals(Filter other)
        {
            return other != null &&
                   EqualityComparer<Members>.Default.Equals(HideMembers, other.HideMembers) &&
                   EqualityComparer<Period>.Default.Equals(Period, other.Period) &&
                   WorkItem == other.WorkItem &&
                   IsFreeTimeMemberShow == other.IsFreeTimeMemberShow &&
                   MileStoneFilters.Equals(other.MileStoneFilters);                    
        }

        public override int GetHashCode()
        {
            int hashCode = 69401243;
            hashCode = hashCode * -1521134295 + EqualityComparer<Members>.Default.GetHashCode(HideMembers);
            hashCode = hashCode * -1521134295 + EqualityComparer<Period>.Default.GetHashCode(Period);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(WorkItem);
            hashCode = hashCode * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(IsFreeTimeMemberShow);
            hashCode = hashCode * -1521134295 + EqualityComparer<MileStoneFilters>.Default.GetHashCode(MileStoneFilters);
            return hashCode;
        }

        internal Filter Clone()
        {
            var result = Filter.All;
            if (this.HideMembers != null) result.HideMembers = this.HideMembers.Clone();
            if (this.WorkItem != null) result.WorkItem = (string)this.WorkItem.Clone();
            if (this.Period != null) result.Period = this.Period.Clone();
            result.IsFreeTimeMemberShow = this.IsFreeTimeMemberShow;
            result.MileStoneFilters = this.MileStoneFilters.Clone();
            return result;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Filter);
        }
    }
}