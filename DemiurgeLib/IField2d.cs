using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
