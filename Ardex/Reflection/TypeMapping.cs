using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Ardex.Reflection
{
    /// <summary>
    /// Reflection-based property mapper.
    /// </summary>
    public class TypeMapping<T>
    {
        private readonly PropertyInfo[] __mappedProperties;

        /// <summary>
        /// Returns a copy of the underlying list of
        /// properties which are mapped by this instance.
        /// </summary>
        public PropertyInfo[] MappedProperties
        {
            get
            {
                return __mappedProperties.ToArray();
            }
        }

        /// <summary>
        /// Creates a custom IEqualityComparer which
        /// uses this instance's Equals method.
        /// </summary>
        public virtual IEqualityComparer<T> EqualityComparer
        {
            get
            {
                return new CustomEqualityComparer<T>(this.Equals);
            }
        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public TypeMapping()
        {
            var type = typeof(T);
            var props = type.GetRuntimeProperties();

            __mappedProperties = props
                .Where(p => p.CanRead)
                .ToArray();
        }

        private TypeMapping(IEnumerable<PropertyInfo> mappedProperties)
        {
            __mappedProperties = mappedProperties.ToArray();
        }

        /// <summary>
        /// Reconciles the differences where necessary,
        /// and returns the number of changes applied.
        /// </summary>
        public virtual int CopyValues(T source, T target)
        {
            var changeCount = 0;

            foreach (var prop in __mappedProperties)
            {
                if (prop.CanWrite)
                {
                    var newValue = prop.GetValue(source);
                    var oldValue = prop.GetValue(target);

                    if (!object.Equals(newValue, oldValue))
                    {
                        prop.SetValue(target, newValue);
                        changeCount++;
                    }
                }
            }

            return changeCount;
        }

        /// <summary>
        /// Creates a shallow clone of the given instance.
        /// </summary>
        public virtual T ShallowClone(T original)
        {
            var clone = Activator.CreateInstance<T>();

            this.CopyValues(original, clone);

            return clone;
        }

        /// <summary>
        /// Excludes the given property from
        /// the list of mapped properties.
        /// Returns this mutated instance of TypeMapping.
        /// </summary>
        public TypeMapping<T> Exclude<TProperty>(Expression<Func<T, TProperty>> propertyExpr)
        {
            var memberExpr = (MemberExpression)propertyExpr.Body;
            var prop = memberExpr.Member as PropertyInfo;

            if (prop == null)
            {
                throw new InvalidOperationException("Specified member is not a property.");
            }

            var mappedProperties = __mappedProperties.ToList();

            if (!mappedProperties.Remove(prop))
            {
                throw new InvalidOperationException(
                    "Specified property was not found in the list of mapped properties.");
            }

            return new TypeMapping<T>(mappedProperties);
        }

        /// <summary>
        /// Checks the given entities for equality
        /// based on their mapped property values.
        /// </summary>
        public virtual bool Equals(T x, T y)
        {
            // Let's do a null check first.
            if (object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null)) return true;
            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null)) return false;

            foreach (var prop in __mappedProperties)
            {
                var xValue = prop.GetValue(x);
                var yValue = prop.GetValue(y);

                if (!object.Equals(xValue, yValue))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a string describing the object
        /// which includes values of all mapped properties.
        /// </summary>
        public string ToString(T obj)
        {
            var sb = new StringBuilder();
            var actualType = obj.GetType();

            sb.Append(actualType.Name);
            sb.Append(" { ");

            for (var i = 0; i < __mappedProperties.Length; i++)
            {
                var prop = __mappedProperties[i];

                sb.Append(prop.Name);
                sb.Append(" = ");
                sb.Append(prop.GetValue(obj));

                if (i != __mappedProperties.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(" }");

            return sb.ToString();
        }
    }
}
