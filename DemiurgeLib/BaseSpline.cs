namespace DemiurgeLib
{
    public abstract class BaseSpline<TVector>
    {
        protected readonly TVector[] controlPoints;

        protected BaseSpline(TVector[] controlPoints)
        {
            this.controlPoints = controlPoints;
        }

        public abstract TVector Sample(float t);
    }
}
