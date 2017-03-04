using System;
using System.Linq.Expressions;

namespace DemiurgeLib
{
    public class NormalizedComposition2d<T> : Composition2d<T>
    {
        private static Func<T, T, T> Subtract;
        private static Func<T, T, T> Divide;
        private static Func<T, T, bool> GreaterThan;

        static NormalizedComposition2d()
        {
            ParameterExpression lh = Expression.Parameter(typeof(T), "lh");
            ParameterExpression rh = Expression.Parameter(typeof(T), "rh");
            BinaryExpression subBody = Expression.Subtract(lh, rh);
            BinaryExpression divBody = Expression.Divide(lh, rh);
            BinaryExpression compBody = Expression.GreaterThan(lh, rh);
            NormalizedComposition2d<T>.Subtract = Expression.Lambda<Func<T, T, T>>(subBody, lh, rh).Compile();
            NormalizedComposition2d<T>.Divide = Expression.Lambda<Func<T, T, T>>(divBody, lh, rh).Compile();
            NormalizedComposition2d<T>.GreaterThan = Expression.Lambda<Func<T, T, bool>>(compBody, lh, rh).Compile();
        }

        protected T minimum;
        protected T maximum;

        /// <summary>
        /// Creates a normalization between a pre-defined minimum and maximum.
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <param name="fields"></param>
        public NormalizedComposition2d(T minimum, T maximum, params IField2d<T>[] fields) : base(fields)
        {
            this.minimum = minimum;
            this.maximum = maximum;
        }

        /// <summary>
        /// Creates a normalization between a pre-defined minimum (such as zero) and a discovered maximum.
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="fields"></param>
        public NormalizedComposition2d(T minimum, params IField2d<T>[] fields) : base(fields)
        {
            DiscoverExtrema();

            this.minimum = minimum;
        }

        /// <summary>
        /// Creates a normalization between discovered minimum and maximum.
        /// </summary>
        /// <param name="fields"></param>
        public NormalizedComposition2d(params IField2d<T>[] fields) : base(fields)
        {
            DiscoverExtrema();
        }

        private void DiscoverExtrema()
        {
            this.minimum = base[0, 0];
            this.maximum = base[0, 0];

            for (int x = 1, y = 0; y < this.Height; y += ++x / this.Width, x %= this.Width)
            {
                T value = base[y, x];

                if (GreaterThan(this.minimum, value))
                {
                    this.minimum = value;
                }

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
                return Divide(Subtract(base[y, x], minimum), this.maximum);
            }
        }
    }
}
