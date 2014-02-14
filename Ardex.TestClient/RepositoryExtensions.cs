using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ardex.Collections.Generic;
using Ardex.Reflection;

namespace Ardex.TestClient
{
    public static class RepositoryExtensions
    {
        public static string ContentsDescription<TEntity>(this IRepository<TEntity> repository)
        {
            return string.Join(
                Environment.NewLine,
                repository.Select(e => new TypeMapping<TEntity>().ToString(e))
            );
        }
    }
}
