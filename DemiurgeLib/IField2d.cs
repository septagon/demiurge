namespace DemiurgeLib
{
    public interface IField2d<TValue>
    {
        TValue this[int y, int x]
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
