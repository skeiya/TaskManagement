using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ProjectsTM.Model
{
    public class ColorConditions : IEnumerable<ColorCondition>
    {
        private readonly List<ColorCondition> _list = new List<ColorCondition>();

        public IEnumerator<ColorCondition> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public void Add(ColorCondition cond)
        {
            _list.Add(cond);
        }

        public ColorCondition GetMatchColorCondition(string input, Color defaultBackColor)
        {
            foreach (var c in _list)
            {
                if (Regex.IsMatch(input, c.Pattern)) return c;
            }
            return new ColorCondition(string.Empty, defaultBackColor, Color.Black);
        }

        public void Remove(ColorCondition c)
        {
            _list.Remove(c);
        }

        public ColorCondition At(int i)
        {
            return _list.ElementAt(i);
        }

        public XElement ToXml()
        {
            var xml = new XElement(nameof(ColorConditions));
            _list.ForEach(c => xml.Add(c.ToXml()));
            return xml;
        }

        public static ColorConditions FromXml(XElement xml)
        {
            var result = new ColorConditions();
            foreach (var c in xml.Elements(nameof(ColorCondition)))
            {
                result.Add(ColorCondition.FromXml(c));
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ColorConditions target)) return false;
            if (_list.Count != target._list.Count) return false;
            for (var index = 0; index < _list.Count; index++)
            {
                if (!_list[index].Equals(target._list[index])) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return _list.GetHashCode();
        }

        public void Up(int index)
        {
            var cond = _list[index];
            _list[index] = _list[index - 1];
            _list[index - 1] = cond;
        }

        public void Down(int index)
        {
            var cond = _list[index];
            _list[index] = _list[index + 1];
            _list[index + 1] = cond;
        }

        public ColorConditions Clone()
        {
            var result = new ColorConditions();
            _list.ForEach(c => result.Add(c.Clone()));
            return result;
        }
    }
}