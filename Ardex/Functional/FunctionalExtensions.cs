using System;

namespace Ardex.Functional
{
    /// <summary>
    /// Allows Kirill to stay productive
    /// and have fun while programming
    /// in a slightly more functional way.
    /// </summary>
    public static class FunctionalExtensions
    {
        /// <summary>
        /// Enables simple inline operations on objects of any type.
        /// </summary>
        public static void Apply<T>(this T input, Action<T> action)
        {
            action(input);
        }

        /// <summary>
        /// Enables simple inline functional transformations on objects of any type.
        /// </summary>
        public static TResult Apply<TInput, TResult>(this TInput input, Func<TInput, TResult> transformation)
        {
            return transformation(input);
        }

        /// <summary>
        /// Enables simple inline operations on objects of any type.
        /// </summary>
        public static void IfNotNull<T>(this T input, Action<T> action) where T : class
        {
            if (input != null)
            {
                action(input);
            }
        }

        /// <summary>
        /// Enables simple inline functional transformations on objects of any type.
        /// </summary>
        public static TResult IfNotNull<TInput, TResult>(this TInput input, Func<TInput, TResult> transformation, Func<TResult> elseFunc) where TInput : class
        {
            if (input == null)
            {
                return elseFunc();
            }

            return transformation(input);
        }

        /// <summary>
        /// Enables simple inline functional transformations on objects of any type.
        /// </summary>
        public static TResult IfNotNull<TInput, TResult>(this TInput input, Func<TInput, TResult> transformation, TResult elseValue = default(TResult)) where TInput : class
        {
            if (input == null)
            {
                return elseValue;
            }

            return transformation(input);
        }
    }
}

