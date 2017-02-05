using System;
using System.Linq;
using System.Linq.Expressions;

namespace DemiurgeLib
{
    public class Composition2d<T> : IField2d<T>
    {
        public const int INVALID_WIDTH = -1;
        public const int INVALID_HEIGHT = -1;

        private static Func<T, T, T> Add;

        static Composition2d()
        {
            // http://www.yoda.arachsys.com/csharp/genericoperators.html
            ParameterExpression lh = Expression.Parameter(typeof(T), "lh");
            ParameterExpression rh = Expression.Parameter(typeof(T), "rh");
            BinaryExpression addBody = Expression.Add(lh, rh);
            Composition2d<T>.Add = Expression.Lambda<Func<T, T, T>>(addBody, lh, rh).Compile();
        }

        private IField2d<T>[] fields;

        public Composition2d(params IField2d<T>[] fields)
        {
            this.fields = fields;
            
            if (fields.Length > 0)
            {
                // Assert that all fields have the same dimensions.
                System.Diagnostics.Debug.Assert(!fields.Any(field => field.Width != fields[0].Width));
                System.Diagnostics.Debug.Assert(!fields.Any(field => field.Height != fields[0].Height));
            }
        }

        virtual public T this[int y, int x]
        {
            get
            {
                if (this.fields.Length == 0)
                {
                    return default(T);
                }
                else
                {
                    T value = this.fields[0][y, x];
                    for (int idx = 1; idx < this.fields.Length; idx++)
                    {
                        value = Add(value, this.fields[idx][y, x]);
                    }

                    return value;
                }
            }
        }

        public int Width
        {
            get
            {
                if (this.fields.Length == 0)
                {
                    return INVALID_WIDTH;
                }
                else
                {
                    return this.fields[0].Width;
                }
            }
        }

        public int Height
        {
            get
            {
                if (this.fields.Length == 0)
                {
                    return INVALID_HEIGHT;
                }
                else
                {
                    return this.fields[0].Height;
                }
            }
        }
    }
}
