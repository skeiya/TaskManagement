using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace ProjectsTM.Model
{
    public class Member : IComparable<Member>
    {
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string Company { get; private set; } = string.Empty;

        public string MemberElement
        {
            get { return ToSerializeString(); }
            set
            {
                var words = value.Split('/');
                LastName = words[0];
                FirstName = words[1];
                Company = words[2];
            }
        }

        internal Member Clone()
        {
            return Member.Parse(ToSerializeString());
        }

        public Member() { }

        public Member(string lastName, string firstName, string company)
        {
            FirstName = firstName;
            LastName = lastName;
            Company = company;
        }

        public XElement ToXml()
        {
            var xml = new XElement(nameof(Member));
            xml.Value = ToSerializeString();
            return xml;
        }

        public static Member FromXml(XElement m)
        {
            var result = new Member();
            result.MemberElement = m.Value;
            return result;
        }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(LastName) && string.IsNullOrEmpty(FirstName)) return "XX";
                if (!string.IsNullOrEmpty(LastName) && !string.IsNullOrEmpty(FirstName))
                {
                    return LastName.Substring(0, 1) + FirstName.Substring(0, 1);
                }
                if (!string.IsNullOrEmpty(LastName)) return LastName.Substring(0, 1);
                return FirstName.Substring(0, 1);
            }
        }

        public void EditApply(string text)
        {
            var m = Member.Parse(text);
            if (m == null) return;
            LastName = m.LastName;
            FirstName = m.FirstName;
            Company = m.Company;
        }

        public string ToSerializeString()
        {
            return LastName + "/" + FirstName + "/" + Company;
        }

        public string NaturalString => LastName + " " + FirstName + "(" + Company + ")";

        public static Member Invalid { get; } = new Member();

        public static Member Parse(string text)
        {
            var words = text.Split('/');
            if (words.Length < 3) throw new Exception("parse error");
            return new Member(words[0], words[1], words[2]);
        }

        public override string ToString()
        {
            return LastName + " " + FirstName + "(" + Company + ")";
        }

        public override bool Equals(object obj)
        {
            var target = obj as Member;
            if (target == null) return false;
            if (!FirstName.Equals(target.FirstName)) return false;
            if (!LastName.Equals(target.LastName)) return false;
            return Company.Equals(target.Company);
        }

        public override int GetHashCode()
        {
            var hashCode = 826127486;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FirstName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LastName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Company);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DisplayName);
            return hashCode;
        }

        public int CompareTo(Member other)
        {
            return this.ToSerializeString().CompareTo(other.ToSerializeString().ToString());
        }

        public static bool operator ==(Member left, Member right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(Member left, Member right)
        {
            return !(left == right);
        }

        public static bool operator <(Member left, Member right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Member left, Member right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(Member left, Member right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(Member left, Member right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }
    }
}