using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Ardex.Sync
{
    /// <summary>
    /// Reflection-based change reconciler.
    /// </summary>
    public class SyncEntityChangeReconciler<TEntity>
    {
        /// <summary>
        /// List of properties whose values will be
        /// checked when ApplyDataChange is called.
        /// </summary>
        public List<PropertyInfo> ReconciledProperties { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public SyncEntityChangeReconciler()
        {
            var type = typeof(TEntity);
            var props = type.GetProperties();

            this.ReconciledProperties = props
                .Where(p => p.CanRead && p.CanWrite)
                .ToList();
        }

        /// <summary>
        /// Reconciles the differences where necessary,
        /// and returns the number of changes applied.
        /// </summary>
        public virtual int ApplyDataChange(TEntity original, TEntity modified)
        {
            var changeCount = 0;

            foreach (var prop in this.ReconciledProperties)
            {
                var oldValue = prop.GetValue(original);
                var newValue = prop.GetValue(modified);

                if (!object.Equals(oldValue, newValue))
                {
                    prop.SetValue(original, newValue);
                    changeCount++;
                }
            }

            return changeCount;
        }

        /// <summary>
        /// Excludes the given property from
        /// the list of reconciled properties.
        /// Returns the mutated instance.
        /// </summary>
        public SyncEntityChangeReconciler<TEntity> Exclude<T>(Expression<Func<TEntity, T>> expr)
        {
            var memberExpr = (MemberExpression)expr.Body;
            var prop = memberExpr.Member as PropertyInfo;

            if (prop == null)
            {
                throw new InvalidOperationException("Specified member is not a property.");
            }

            if (!this.ReconciledProperties.Remove(prop))
            {
                throw new InvalidOperationException(
                    "Specified property was not found in the list of reconciled properties.");
            }

            return this;
        }

        /// <summary>
        /// Checks the given entities for equality based
        /// on their reconciled property values.
        /// </summary>
        public virtual bool Equals(TEntity x, TEntity y)
        {
            // Let's do a null check first.
            if (object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null)) return true;
            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null)) return false;

            foreach (var prop in this.ReconciledProperties)
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
    }
}
