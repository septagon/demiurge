using System.Drawing;

namespace DemiurgeLib
{
    public class SubContinuum<T> : IField2d<T>, IContinuum2d<T>
    {
        private int width;
        private int height;
        private IContinuum2d<T> source;
        private Rectangle sourceRegion;

        public SubContinuum(int width, int height, IContinuum2d<T> source, Rectangle sourceRegion)
        {
            this.width = width;
            this.height = height;
            this.source = source;
            this.sourceRegion = sourceRegion;
        }

        public T this[float y, float x]
        {
            get
            {
                y = this.sourceRegion.Height * y / this.height + sourceRegion.Top;
                x = this.sourceRegion.Width * x / this.width + sourceRegion.Left;
                return this.source[y, x];
            }
        }

        public T this[int y, int x]
        {
            get
            {
                return this[(float)y, (float)x];
            }
        }

        public int Height
        {
            get
            {
                return this.Height;
            }
        }

        public int Width
        {
            get
            {
                return this.Width;
            }
        }
    }
}
