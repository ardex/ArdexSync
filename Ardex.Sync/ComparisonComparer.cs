using System;
using System.Collections.Generic;

namespace Ardex.Sync
{
    internal class ComparisonComparer<T> : Comparer<T>
    {
        private readonly Comparison<T> __comparison;

        public ComparisonComparer(Comparison<T> comparison)
        {
            __comparison = comparison;
        }

        public override int Compare(T x, T y)
        {
            return __comparison(x, y);
        }
    }
}
