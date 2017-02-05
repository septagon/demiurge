namespace DemiurgeLib
{
    public class Field2d<T> : IField2d<T>
    {
        private T[,] values;

        protected Field2d(int width, int height)
        {
            this.values = new T[height, width];
        }

        public Field2d(IField2d<T> field)
        {
            this.values = new T[field.Height, field.Width];

            for (int x = 0, y = 0; y < this.Height; y += ++x / this.Width, x %= this.Width)
            {
                this.values[y, x] = field[y, x];
            }
        }

        virtual public T this[int y, int x]
        {
            get
            {
                return this.values[y, x];
            }

            protected set
            {
                this.values[y, x] = value;
            }
        }

        virtual public int Width { get { return this.values.GetLength(1); } }
        virtual public int Height { get { return this.values.GetLength(0); } }
    }
}
