﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemiurgeLib
{
    namespace Common
    {
        /// <summary>
        /// Performs a absolute value reflectance transform (a.k.a. the "mountain transform") on the
        /// input field.  Expects input field to be normalized to range [0,maxValue]. 
        /// </summary>
        public class AbsTransform : Transformation2d
        {
            public AbsTransform(I2dField<float> field, float inflectionPoint = 0.5f, float maxValue = 1f) : base(field, (val) =>
            {
                float divisor = Math.Max(inflectionPoint, maxValue - inflectionPoint);
                return Math.Abs(val - inflectionPoint) / divisor;
            }) { }
        }

        public class InvertTransform : Transformation2d
        {
            public InvertTransform(I2dField<float> field) : base(field, (val) => -1f * val + 1f) { }
        }

        public class ScaleTransform : Transformation2d
        {
            public ScaleTransform(I2dField<float> field, float scalar) : base(field, (val) => scalar * val) { }
        }
    }
}
