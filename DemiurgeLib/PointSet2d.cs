using System;
using System.Collections;
using System.Collections.Generic;

namespace DemiurgeLib.Common
{
    public class PointSet2d : IEnumerable<Point2d>
    {
        private Dictionary<int, HashSet<int>> set = new Dictionary<int, HashSet<int>>();

        public void Add(Point2d p)
        {
            HashSet<int> row;
            if (!this.set.TryGetValue(p.y, out row))
            {
                row = new HashSet<int>();
                this.set.Add(p.y, row);
            }

            row.Add(p.x);
        }

        public void Remove(Point2d p)
        {
            HashSet<int> row;
            if (this.set.TryGetValue(p.y, out row))
            {
                row.Remove(p.x);
            }
        }

        public bool Contains(Point2d p)
        {
            return Contains(p.x, p.y);
        }

        public bool Contains(int x, int y)
        {
            HashSet<int> row;
            if (this.set.TryGetValue(y, out row))
            {
                return row.Contains(x);
            }
            else
            {
                return false;
            }
        }

        public int Count
        {
            get
            {
                int count = 0;
                foreach (var value in this.set.Values)
                {
                    count += value.Count;
                }

                return count;
            }
        }

        public IEnumerator<Point2d> GetEnumerator()
        {
            foreach (var y in this.set.Keys)
            {
                foreach (var x in this.set[y])
                {
                    yield return new Point2d(x, y);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
