using System;

namespace DemiurgeLib.Common
{
    /// <summary>
    /// Performs a absolute value reflectance transform (a.k.a. the "mountain transform") on the
    /// input field.  Expects input field to be normalized to range [0,maxValue]. 
    /// </summary>
    public class AbsTransform : Transformation2d
    {
        public AbsTransform(IField2d<float> field, float inflectionPoint = 0.5f, float maxValue = 1f) : base(field, (val) =>
        {
            float divisor = Math.Max(inflectionPoint, maxValue - inflectionPoint);
            return Math.Abs(val - inflectionPoint) / divisor;
        }) { }
    }

    public class InvertTransform : Transformation2d
    {
        public InvertTransform(IField2d<float> field) : base(field, (val) => -1f * val + 1f) { }
    }

    public class ScaleTransform : Transformation2d
    {
        public ScaleTransform(IField2d<float> field, float scalar) : base(field, (val) => scalar * val) { }
    }

    public class FunctionField<T> : IField2d<T>
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public T this[int y, int x]
        {
            get
            {
                return this.function(x, y);
            }
        }

        private Func<int, int, T> function;

        public FunctionField(int width, int height, Func<int, int, T> function)
        {
            this.Width = width;
            this.Height = height;
            this.function = function;
        }
    }

    public class ConstantField<T> : FunctionField<T>
    {
        public ConstantField(int width, int height, T value) : base(width, height, (x, y) => value) { }
    }
}
