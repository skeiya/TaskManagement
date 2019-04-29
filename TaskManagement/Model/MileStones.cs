﻿using System.Collections;
using System.Collections.Generic;

namespace TaskManagement.Model
{
    public class MileStones : IEnumerable<MileStone>
    {
        private List<MileStone> _list = new List<MileStone>();

        internal MileStones Clone()
        {
            var result = new MileStones();
            foreach (var m in _list)
            {
                result.Add(m.Clone());
            }
            return result;
        }

        public void Add(MileStone m)
        {
            _list.Add(m);
        }

        public IEnumerator<MileStone> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
