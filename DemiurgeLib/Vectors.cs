using System;

namespace DemiurgeLib
{
    public abstract class Vector<TValue, TDescendent> where TDescendent : Vector<TValue, TDescendent>
    {
        protected TValue[] values;

        public TValue this[int idx]
        {
            get
            {
                if (idx < this.values.Length && idx >= 0)
                    return this.values[idx];
                else
                    return default(TValue);
            }

            set
            {
                if (idx < this.values.Length && idx >= 0)
                    this.values[idx] = value;
            }
        }

        protected Vector(params TValue[] values)
        {
            this.values = values;
        }

        protected abstract TDescendent Factory(TValue[] values);

        public TDescendent Map(Func<TValue, TValue> f)
        {
            TValue[] values = new TValue[this.values.Length];

            for (int idx = 0; idx < values.Length; idx++)
            {
                values[idx] = f(this.values[idx]);
            }

            return Factory(values);
        }

        public TDescendent Zip(TDescendent other, Func<TValue, TValue, TValue> f)
        {
            TValue[] values = new TValue[Math.Max(this.values.Length, other.values.Length)];

            for (int idx = 0; idx < values.Length; idx++)
            {
                values[idx] = f(this.values[idx], other.values[idx]);
            }

            return Factory(values);
        }

        public TResult Foldl<TResult>(Func<TResult, TValue, TResult> f, TResult seed = default(TResult))
        {
            for (int idx = 0; idx < this.values.Length; idx++)
            {
                seed = f(seed, this.values[idx]);
            }

            return seed;
        }
    }

    public class vFloat : Vector<float, vFloat>
    {
        public vFloat(params float[] values) : base(values) { }
        
        protected override vFloat Factory(float[] values)
        {
            return new vFloat(values);
        }

        public static vFloat operator +(vFloat l, vFloat r)
        {
            return l.Zip(r, (a, b) => a + b);
        }

        public static vFloat operator *(float s, vFloat v)
        {
            return v.Map(a => s * a);
        }

        public static vFloat operator *(vFloat v, float s)
        {
            return s * v;
        }

        public static vFloat operator -(vFloat v)
        {
            return -1f * v;
        }

        public static vFloat operator -(vFloat l, vFloat r)
        {
            return l + -r;
        }

        public static vFloat operator /(vFloat v, float d)
        {
            return v * (1f / d);
        }

        public float l1dst(vFloat other)
        {
            return (this - other).Map(Math.Abs).Foldl<float>((a, b) => a + b);
        }

        public float l2sq(vFloat other)
        {
            return (this - other).Map(a => a * a).Foldl<float>((a, b) => a + b);
        }

        public float l2dst(vFloat other)
        {
            return (float)Math.Sqrt(this.l2sq(other));
        }

        public float dot(vFloat other)
        {
            return this.Zip(other, (a, b) => a * b).Foldl<float>((a, b) => a + b);
        }

        public float magSq()
        {
            return this.dot(this);
        }
        
        public float mag()
        {
            return (float)Math.Sqrt(magSq());
        }

        public vFloat norm()
        {
            return this / this.mag();
        }

        public static vFloat Cross3d(vFloat l, vFloat r)
        {
            if (l.values.Length != 3 || r.values.Length != 3)
            {
                throw new ArgumentException("Arguments to Cross3d() were not 3D!");
            }
            
            return new vFloat(
                l[1] * r[2] - l[2] * r[1],
                l[2] * r[0] - l[0] * r[2],
                l[0] * r[1] - l[1] * r[0]);
        }
    }
}
