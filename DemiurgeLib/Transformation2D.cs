using System;

namespace DemiurgeLib
{
    /*
     * I'm coming to realize that the entire Transformation concept can, I think, be much more concisely 
     * and elegantly expressed in C++ with variadic templates.  Consider reimplementing this entire part
     * from scratch if we ever decide to port.
     */

    public class Transformation2d<TFrom1, TFrom2, TTo> : IField2d<TTo>
    {
        private IField2d<TFrom1> field1;
        private IField2d<TFrom2> field2;
        private Func<int, int, TFrom1, TFrom2, TTo> transformation;

        public Transformation2d(IField2d<TFrom1> field1, IField2d<TFrom2> field2, Func<TFrom1, TFrom2, TTo> transformation)
            : this(field1, field2, (x, y, val1, val2) => transformation(val1, val2)) { }
        public Transformation2d(IField2d<TFrom1> field1, IField2d<TFrom2> field2, Func<int, int, TFrom1, TFrom2, TTo> transformation)
        {
            System.Diagnostics.Debug.Assert(field1.Width == field2.Width && field1.Height == field2.Height, "Inputs to transformations must have equal dimensions.");

            this.field1 = field1;
            this.field2 = field2;
            this.transformation = transformation;
        }

        virtual public TTo this[int y, int x]
        {
            get
            {
                return this.transformation(x, y, this.field1[y, x], this.field2[y, x]);
            }
        }

        virtual public int Width { get { return field1.Width; } }
        virtual public int Height { get { return field1.Height; } }
    }
    
    public class Transformation2d<TFrom, TTo> : IField2d<TTo>
    {
        private IField2d<TFrom> field;
        private Func<int, int, TFrom, TTo> transformation;

        public Transformation2d(IField2d<TFrom> field, Func<TFrom, TTo> transformation) : this(field, (x, y, val) => transformation(val)){ }
        public Transformation2d(IField2d<TFrom> field, Func<int, int, TFrom, TTo> transformation)
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
        public Transformation2d(IField2d<T> field, Func<T, T> transformation) : base(field, transformation) { }
        public Transformation2d(IField2d<T> field, Func<int, int, T, T> transformation) : base(field, transformation) { }
    }

    public class Transformation2d : Transformation2d<float>
    {
        public Transformation2d(IField2d<float> field, Func<float, float> transformation) : base(field, transformation) { }
        public Transformation2d(IField2d<float> field, Func<int, int, float, float> transformation) : base(field, transformation) { }
    }
}
