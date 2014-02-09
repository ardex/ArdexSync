using System;
using System.Collections.Generic;

namespace Ardex.Sync
{
    public class CustomComparer<T> : Comparer<T>
    {
        private readonly Comparison<T> __comparison;

        public CustomComparer(Comparison<T> comparison)
        {
            __comparison = comparison;
        }

        public override int Compare(T x, T y)
        {
            return __comparison(x, y);
        }
    }
}
