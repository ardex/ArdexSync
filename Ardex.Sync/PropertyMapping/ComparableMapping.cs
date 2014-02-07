using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync.PropertyMapping
{
    public class ComparableMapping<TEntity>
    {
        private readonly Func<TEntity, IComparable> __getter;

        public ComparableMapping(Func<TEntity, IComparable> getter)
        {
            __getter = getter;
        }

        public IComparable Get(TEntity entity)
        {
            return __getter(entity);
        }
    }
}
