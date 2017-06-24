using System;

namespace DemiurgeLib
{
    public class CenCatRomSpline : BaseSpline<vFloat>
    {
        private float[] ts;

        public CenCatRomSpline(vFloat[] controlPoints, float alpha) : base(controlPoints)
        {
            this.ts = new float[this.controlPoints.Length];

            this.ts[0] = 0f;
            for (int idx = 1; idx < this.ts.Length; idx++)
            {
                var u = this.controlPoints[idx - 1];
                var v = this.controlPoints[idx];
                this.ts[idx] = (float)Math.Pow(u.l2dst(v), alpha) + this.ts[idx - 1];
            }
        }

        public override vFloat Sample(float t)
        {
            // Find the proper four control points for a piecewise calculation.
            t *= this.ts[this.ts.Length - 1];

            int i1 = 0;
            while (i1 + 1 < this.ts.Length && this.ts[i1 + 1] < t)
                i1++;

            if (i1 == 0)
                return this.controlPoints[1];
            else if (i1 + 2 >= this.controlPoints.Length)
                return this.controlPoints[this.controlPoints.Length - 2];

            int i0 = i1 - 1;
            int i2 = i1 + 1;
            int i3 = i1 + 2;

            // Get all the variables conveniently named.

            vFloat p0 = this.controlPoints[i0];
            vFloat p1 = this.controlPoints[i1];
            vFloat p2 = this.controlPoints[i2];
            vFloat p3 = this.controlPoints[i3];

            float t0 = this.ts[i0];
            float t1 = this.ts[i1];
            float t2 = this.ts[i2];
            float t3 = this.ts[i3];

            // Perform the appropriate calculations.
            var a1 = (t1 - t) / (t1 - t0) * p0 +
                (t - t0) / (t1 - t0) * p1;
            var a2 = (t2 - t) / (t2 - t1) * p1 + 
                (t - t1) / (t2 - t1) * p2;
            var a3 = (t3 - t) / (t3 - t2) * p2 +
                (t - t2) / (t3 - t2) * p3;
            var b1 = (t2 - t) / (t2 - t0) * a1 +
                (t - t0) / (t2 - t0) * a2;
            var b2 = (t3 - t) / (t3 - t1) * a2 +
                (t - t1) / (t3 - t1) * a3;
            
            return (t2 - t) / (t2 - t1) * b1 +
                (t - t1) / (t2 - t1) * b2;
        }
    }
}
