using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync.PropertyMapping
{
    public class ReadWriteMapping<TProperty>
    {
        private readonly Func<TProperty> __getter;
        private readonly Action<TProperty> __setter;

        public ReadWriteMapping(Func<TProperty> getter, Action<TProperty> setter)
        {
            __getter = getter;
            __setter = setter;
        }

        public TProperty Get()
        {
            return __getter();
        }

        public void Set(TProperty value)
        {
            __setter(value);
        }
    }

    public class ReadWriteMapping<TEntity, TProperty>
    {
        private readonly Func<TEntity, TProperty> __getter;
        private readonly Action<TEntity, TProperty> __setter;

        public ReadWriteMapping(Func<TEntity, TProperty> getter, Action<TEntity, TProperty> setter)
        {
            __getter = getter;
            __setter = setter;
        }

        public TProperty Get(TEntity entity)
        {
            return __getter(entity);
        }

        public void Set(TEntity entity, TProperty value)
        {
            __setter(entity, value);
        }
    }
}
