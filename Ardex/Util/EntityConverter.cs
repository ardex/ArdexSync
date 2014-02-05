using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ardex.Util
{
    /// <summary>
    /// Uses Reflection to convert between instances of 
    /// classes which both implement the same interface.
    /// Only fills in the properties explicitly defined
    /// by the interface type.
    /// </summary>
    [Obsolete("Use EntityConversion instead.")]
    public static class EntityConverter
    {
        /// <summary>
        /// Uses Reflection to convert between instances of 
        /// classes which both implement the same interface.
        /// Only fills in the properties explicitly defined
        /// by the interface type.
        /// </summary>
        public static TEntity Convert<TInterface, TEntity>(TInterface oldEntity, TEntity newEntity)
            where TEntity : TInterface
        {
            // Validation.
            if (oldEntity == null) throw new ArgumentNullException("oldEntity");
            if (newEntity == null) throw new ArgumentNullException("newEntity");

            EntityConverter.ValidateTypes(typeof(TInterface), typeof(TEntity));

            // Conversion.
            var type = typeof(TInterface);
            var props = type.GetProperties();

            foreach (var prop in props)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    var value = prop.GetValue(oldEntity, null);

                    prop.SetValue(newEntity, value, null);
                }
            }

            return newEntity;
        }

        /// <summary>
        /// Validates the types to ensure the conversion will succeed.
        /// </summary>
        private static void ValidateTypes(Type interfaceType, Type entityType)
        {
            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("oldEntity must be an interface type.");
            }

            if (interfaceType == entityType)
            {
                throw new ArgumentException("Entity and interface types must be different.");
            }
        }
    }
}
