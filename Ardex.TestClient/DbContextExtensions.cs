using System.Data.Entity;
using System.Linq;

namespace Ardex.TestClient
{
    public static class DbContextExtensions
    {
        public static void Insert<T>(this DbContext dx, T entity) where T : class
        {
            dx.Entry(entity).State = EntityState.Added;

            dx.SaveChanges();
        }

        public static void Update<T>(this DbContext dx, T entity) where T : class
        {
            dx.Entry(entity).State = EntityState.Modified;

            dx.SaveChanges();
        }

        public static void Delete<T>(this DbContext dx, T entity) where T : class
        {
            dx.Entry(entity).State = EntityState.Deleted;

            dx.SaveChanges();
        }

        public static void Clear<T>(this DbContext dx) where T : class
        {
            var set = dx.Set<T>();

            set.RemoveRange(set.AsEnumerable());
            dx.SaveChanges();
        }
    }
}
