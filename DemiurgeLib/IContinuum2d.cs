namespace DemiurgeLib
{
    public interface IContinuum2d<TValue>
    {
        TValue this[float y, float x]
        {
            get;
        }

        int Width
        {
            get;
        }

        int Height
        {
            get;
        }
    }
}
