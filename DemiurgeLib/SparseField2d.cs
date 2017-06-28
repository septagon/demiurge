using DemiurgeLib.Common;
using System;
using System.Collections.Generic;
using System.Collections;

namespace DemiurgeLib
{
    public class SparseField2d<TValue> : IField2d<TValue>, IEnumerable<Tuple<Point2d, TValue>>
    {
        private Dictionary<Point2d, TValue> values;
        private TValue defaultValue;

        public int Height
        {
            get; private set;
        }

        public int Width
        {
            get; private set;
        }

        public SparseField2d(int width, int height, TValue defaultValue)
        {
            this.Width = width;
            this.Height = height;
            this.values = new Dictionary<Point2d, TValue>();
            this.defaultValue = defaultValue;
        }

        public TValue this[int y, int x]
        {
            get
            {
                Point2d pt = new Point2d(x, y);
                ValidatePoint(pt);

                TValue ret = this.defaultValue;
                if (this.values.TryGetValue(pt, out ret))
                {
                    return ret;
                }

                return this.defaultValue;
            }

            set
            {
                if (value.Equals(this.defaultValue))
                {
                    Remove(new Point2d(x, y));
                }
                else
                {
                    Add(new Point2d(x, y), value);
                }
            }
        }

        public void Add(Point2d pt, TValue value)
        {
            ValidatePoint(pt);
            this.values[pt] = value;
        }

        public void Remove(Point2d pt)
        {
            ValidatePoint(pt);
            this.values.Remove(pt);
        }

        public IEnumerator<Tuple<Point2d, TValue>> GetEnumerator()
        {
            foreach (var pair in this.values)
            {
                yield return new Tuple<Point2d, TValue>(pair.Key, pair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void ValidatePoint(Point2d pt)
        {
            if (pt.x < 0 || pt.x >= this.Width ||
                pt.y < 0 || pt.y >= this.Height)
            {
                throw new IndexOutOfRangeException("Point (" + pt.x + ", " + pt.y + ") is out of range.");
            }
        }
    }
}
