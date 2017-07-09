using System.Drawing;

namespace DemiurgeLib
{
    public class SubContinuum<T> : IField2d<T>, IContinuum2d<T>
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private IContinuum2d<T> source;
        private Rectangle sourceRegion;

        public SubContinuum(int width, int height, IContinuum2d<T> source, Rectangle sourceRegion)
        {
            this.Width = width;
            this.Height = height;
            this.source = source;
            this.sourceRegion = sourceRegion;
        }

        public T this[float y, float x]
        {
            get
            {
                y = this.sourceRegion.Height * y / this.Height + sourceRegion.Top;
                x = this.sourceRegion.Width * x / this.Width + sourceRegion.Left;
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
    }
}
