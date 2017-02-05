using System;

namespace DemiurgeLib
{
    public class Transformation2d<TFrom, TTo> : I2dField<TTo>
    {
        private I2dField<TFrom> field;
        private Func<int, int, TFrom, TTo> transformation;

        public Transformation2d(I2dField<TFrom> field, Func<TFrom, TTo> transformation) : this(field, (x, y, val) => transformation(val)){ }
        public Transformation2d(I2dField<TFrom> field, Func<int, int, TFrom, TTo> transformation)
        {
            this.field = field;
            this.transformation = transformation;
        }

        virtual public TTo this[int y, int x]
        {
            get
            {
                return this.transformation(x, y, this.field[y, x]);
            }
        }

        virtual public int Width { get { return field.Width; } }
        virtual public int Height { get { return field.Height; } }
    }

    public class Transformation2d<T> : Transformation2d<T, T>
    {
        public Transformation2d(I2dField<T> field, Func<T, T> transformation) : base(field, transformation) { }
        public Transformation2d(I2dField<T> field, Func<int, int, T, T> transformation) : base(field, transformation) { }
    }

    public class Transformation2d : Transformation2d<float>
    {
        public Transformation2d(I2dField<float> field, Func<float, float> transformation) : base(field, transformation) { }
        public Transformation2d(I2dField<float> field, Func<int, int, float, float> transformation) : base(field, transformation) { }
    }
}
