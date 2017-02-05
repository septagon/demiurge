using System;
using System.Linq.Expressions;

namespace DemiurgeLib
{
    public class NormalizedComposition2d<T> : Composition2d<T>
    {
        private static Func<T, T, T> Divide;
        private static Func<T, T, bool> GreaterThan;

        static NormalizedComposition2d()
        {
            ParameterExpression lh = Expression.Parameter(typeof(T), "lh");
            ParameterExpression rh = Expression.Parameter(typeof(T), "rh");
            BinaryExpression divBody = Expression.Divide(lh, rh);
            BinaryExpression compBody = Expression.GreaterThan(lh, rh);
            NormalizedComposition2d<T>.Divide = Expression.Lambda<Func<T, T, T>>(divBody, lh, rh).Compile();
            NormalizedComposition2d<T>.GreaterThan = Expression.Lambda<Func<T, T, bool>>(compBody, lh, rh).Compile();
        }

        protected T maximum;

        public NormalizedComposition2d(params IField2d<T>[] fields) : base(fields)
        {
            this.maximum = base[0, 0];
            for (int x = 1, y = 0; y < this.Height; y += ++x / this.Width, x %= this.Width)
            {
                T value = base[y, x];
                if (GreaterThan(value, this.maximum))
                {
                    this.maximum = value;
                }
            }
        }

        override public T this[int y, int x]
        {
            get
            {
                return Divide(base[y, x], this.maximum);
            }
        }
    }
}
