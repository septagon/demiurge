using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemiurgeLib.Common
{
    public class SubField<T> : IField2d<T>
    {
        private IField2d<T> baseField;
        private Rectangle subsection;

        public SubField(IField2d<T> baseField, Rectangle subsection)
        {
            this.baseField = baseField;
            this.subsection = subsection;
        }

        public T this[int y, int x]
        {
            get
            {
                if (x >= this.subsection.Width || y >= this.subsection.Height)
                {
                    throw new IndexOutOfRangeException("Cannot request out-of-bounds values from subsection.");
                }

                return baseField[y + this.subsection.Y, x + this.subsection.X];
            }
        }

        public int Height
        {
            get
            {
                return this.subsection.Height;
            }
        }

        public int Width
        {
            get
            {
                return this.subsection.Width;
            }
        }
    }
}
