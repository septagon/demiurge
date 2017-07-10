namespace DemiurgeLib
{
    public class ReResContinuum : IField2d<float>, IContinuum2d<float>
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private IContinuum2d<float> source;
        private float toSourceCoords;

        public ReResContinuum(int width, int height, IContinuum2d<float> source)
        {
            this.Width = width;
            this.Height = height;
            this.source = source;

            this.toSourceCoords = 1f * source.Width / this.Width;
        }

        public float this[float y, float x]
        {
            get
            {
                return source[this.toSourceCoords * y, this.toSourceCoords * x];
            }
        }

        public float this[int y, int x]
        {
            get
            {
                return this[(float)y, (float)x];
            }
        }
    }
}
