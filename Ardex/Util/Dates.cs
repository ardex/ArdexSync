using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ardex.Util
{
    /// <summary>
    /// Common DateTime utilities.
    /// </summary>
    public static class Dates
    {
        /// <summary>
        /// Specifies the kind of the given date to be UTC.
        /// </summary>
        public static DateTime MakeUtc(DateTime date)
        {
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        /// <summary>
        /// Specifies the kind of all DateTime properties
        /// to be UTC in order to prevent JSON serializer
        /// from appending the time zone offset.
        /// </summary>
        public static void MakeUtc(object obj)
        {
            if (obj == null)
            {
                return;
            }

            var enumerable = obj as IEnumerable;

            if (enumerable != null)
            {
                foreach (var element in enumerable)
                {
                    Dates.MakeUtc(element);
                }
            }
            else
            {
                // Fix single object.
                var type = obj.GetType();
                var properties = type.GetProperties();

                foreach (var prop in properties)
                {
                    // Fix DateTime properties.
                    if (prop.CanWrite && prop.CanRead && (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?)))
                    {
                        var value = prop.GetValue(obj);

                        if (value != null)
                        {
                            var date = (DateTime)value;
                            var dateFixed = DateTime.SpecifyKind(date, DateTimeKind.Utc);

                            prop.SetValue(obj, dateFixed);
                        }
                    }
                    else if (prop.PropertyType.IsClass)
                    {
                        // Recursively fix child objects in the graph.
                        var value = prop.GetValue(obj);

                        Dates.MakeUtc(value);
                    }
                }
            }
        }
    }
}