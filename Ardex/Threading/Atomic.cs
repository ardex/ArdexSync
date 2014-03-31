using System;
using System.Threading;

namespace Ardex.Threading
{
    /// <summary>
    /// Additional Interlocked methods.
    /// </summary>
    public static class Atomic
    {
        // Most of these transformation methods only work in really
        // simple scenarios where the transformation is irreversible
        // and its results predictable, unique and consistent over time,
        // i.e. incrementing a variable. If it is possible that the transformation
        // applied to the same value at different points in time will produce
        // different results, you *will* corrupt the state of your application.

        /// <summary>
        /// Jeffrey Richter's "interlocked anything" pattern
        /// implementation shamelessly stolen from his book.
        /// </summary>
        public static int Transform<TArgument>(ref int target, TArgument argument, Func<int, TArgument, int> transformation)
        {
            int currentVal = target, startVal, desiredVal;

            do
            {
                startVal = currentVal;
                desiredVal = transformation(startVal, argument);
                currentVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
            }
            while (startVal != currentVal);

            return desiredVal;
        }

        /// <summary>
        /// Jeffrey Richter's "interlocked anything" pattern
        /// implementation shamelessly stolen from his book.
        /// In performance-critical scenarios you should
        /// consider using the other overload.
        /// </summary>
        public static int Transform(ref int target, Func<int, int> transform)
        {
            int currentVal = target, startVal, desiredVal;

            do
            {
                startVal = currentVal;
                desiredVal = transform(startVal);
                currentVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
            }
            while (startVal != currentVal);

            return desiredVal;
        }

        /// <summary>
        /// Jeffrey Richter's "interlocked anything" pattern
        /// implementation shamelessly stolen from his book.
        /// </summary>
        public static T Transform<T, TArgument>(ref T target, TArgument argument, Func<T, TArgument, T> transformation)
            where T : class
        {
            T currentVal = target, startVal, desiredVal;

            do
            {
                startVal = currentVal;
                desiredVal = transformation(startVal, argument);
                currentVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
            }
            while (startVal != currentVal);

            return desiredVal;
        }

        /// <summary>
        /// Jeffrey Richter's "interlocked anything" pattern
        /// implementation shamelessly stolen from his book.
        /// In performance-critical scenarios you should
        /// consider using the other overload.
        /// </summary>
        public static T Transform<T>(ref T target, Func<T, T> transformation)
            where T : class
        {
            T currentVal = target, startVal, desiredVal;

            do
            {
                startVal = currentVal;
                desiredVal = transformation(startVal);
                currentVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
            }
            while (startVal != currentVal);

            return desiredVal;
        }
    }
}