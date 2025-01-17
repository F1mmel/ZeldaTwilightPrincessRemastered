using System;
using System.Numerics;

namespace KclLibrary
{
    internal static class Maths
    {

        internal static int GetNext2Exponent(float value)
        {
            if (value <= 1) return 0;
            return (int)Math.Ceiling(Math.Log(value, 2));
        }
    }
}
