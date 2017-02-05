using System;
using System.Linq;
using System.Linq.Expressions;

namespace DemiurgeLib
{
    public class Composition2d<TValue> : I2dField<TValue>
    {
        public const int INVALID_WIDTH = -1;
        public const int INVALID_HEIGHT = -1;

        private static Func<TValue, TValue, TValue> Add;

        static Composition2d()
        {
            // http://www.yoda.arachsys.com/csharp/genericoperators.html
            ParameterExpression lh = Expression.Parameter(typeof(TValue), "lh");
            ParameterExpression rh = Expression.Parameter(typeof(TValue), "rh");
            BinaryExpression addBody = Expression.Add(lh, rh);
            Composition2d<TValue>.Add = Expression.Lambda<Func<TValue, TValue, TValue>>(addBody, lh, rh).Compile();
        }

        private I2dField<TValue>[] fields;

        public Composition2d(params I2dField<TValue>[] fields)
        {
            this.fields = fields;
            
            if (fields.Length > 0)
            {
                // Assert that all fields have the same dimensions.
                System.Diagnostics.Debug.Assert(!fields.Any(field => field.Width != fields[0].Width));
                System.Diagnostics.Debug.Assert(!fields.Any(field => field.Height != fields[0].Height));
            }
        }

        virtual public TValue this[int y, int x]
        {
            get
            {
                if (this.fields.Length == 0)
                {
                    return default(TValue);
                }
                else
                {
                    TValue value = this.fields[0][y, x];
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
