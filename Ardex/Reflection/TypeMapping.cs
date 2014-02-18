using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using Ardex.Linq.Expressions;

namespace Ardex.Reflection
{
    /// <summary>
    /// Reflection-based property mapper.
    /// </summary>
    public class TypeMapping<T>
    {
        private readonly List<PropertyInfo> __mappedProperties;

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
                .ToList();
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        private TypeMapping(IEnumerable<PropertyInfo> mappedProperties)
        {
            __mappedProperties = mappedProperties.ToList();
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
        /// </summary>
        public void Exclude<TProperty>(Expression<Func<T, TProperty>> propertyExpr)
        {
            var prop = ExpressionUtil.Property(propertyExpr);

            if (!__mappedProperties.Remove(prop))
            {
                throw new InvalidOperationException(
                    "Specified property was not found in the list of mapped properties.");
            }
        }

        /// <summary>
        /// Excludes the given property from
        /// the list of mapped properties.
        /// Returns a new instance of TypeMapping.
        /// </summary>
        public TypeMapping<T> Without<TProperty>(Expression<Func<T, TProperty>> propertyExpr)
        {
            var prop = ExpressionUtil.Property(propertyExpr);
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
            var actualType = obj.GetType();

            // Comma-separated list of
            // property names and their values.
            var propertyValues = string.Join(
                ", ", __mappedProperties.Select(p => p.Name + " = " + p.GetValue(obj)));

            return actualType.Name + " { " + propertyValues + " }";
        }
    }
}
