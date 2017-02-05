using System;
using System.Linq.Expressions;

namespace DemiurgeLib
{
    public class NormalizedComposition2d<TValue> : Composition2d<TValue>
    {
        private static Func<TValue, TValue, TValue> Divide;
        private static Func<TValue, TValue, bool> GreaterThan;

        static NormalizedComposition2d()
        {
            ParameterExpression lh = Expression.Parameter(typeof(TValue), "lh");
            ParameterExpression rh = Expression.Parameter(typeof(TValue), "rh");
            BinaryExpression divBody = Expression.Divide(lh, rh);
            BinaryExpression compBody = Expression.GreaterThan(lh, rh);
            NormalizedComposition2d<TValue>.Divide = Expression.Lambda<Func<TValue, TValue, TValue>>(divBody, lh, rh).Compile();
            NormalizedComposition2d<TValue>.GreaterThan = Expression.Lambda<Func<TValue, TValue, bool>>(compBody, lh, rh).Compile();
        }

        protected TValue maximum;

        public NormalizedComposition2d(params I2dField<TValue>[] fields) : base(fields)
        {
            this.maximum = base[0, 0];
            for (int x = 1, y = 0; y < this.Height; y += ++x / this.Width, x %= this.Width)
            {
                TValue value = base[y, x];
                if (GreaterThan(value, this.maximum))
                {
                    this.maximum = value;
                }
            }
        }

        override public TValue this[int y, int x]
        {
            get
            {
                return Divide(base[y, x], this.maximum);
            }
        }
    }
}
