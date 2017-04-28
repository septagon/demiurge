using System;
using System.Collections.Generic;

namespace DemiurgeLib
{
    public class ColumnField<TValue> : IField2d<TValue>
    {
        private IList<TValue> values;
        private int startingOffset;
        public int Height { get; private set; }
        public int Width { get; private set; }

        public TValue this[int y, int x]
        {
            get
            {
                if (x < 0 || x >= this.Width || y < 0 || y >= this.Height)
                    throw new IndexOutOfRangeException();
                return this.values[x + this.startingOffset];
            }
        }

        public ColumnField(int width, int height, IList<TValue> values, int startingOffset = 0)
        {
            this.Width = width;
            this.Height = height;
            this.values = values;
            this.startingOffset = startingOffset;
        }
    }
}