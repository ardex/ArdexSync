using System;
using System.Collections.Generic;

namespace Ardex.Sync
{
    public static class VersionInfo
    {
        public static VersionInfo<TEntity, TVersion> Create<TEntity, TVersion>(TEntity entity, TVersion version)
        {
            return new VersionInfo<TEntity, TVersion>(entity, version);
        }
    }

    public class VersionInfo<TEntity, TVersion>
    {
        public TEntity Entity { get; private set; }
        public TVersion Version { get; private set; }

        public VersionInfo(TEntity entity, TVersion version)
        {
            this.Entity = entity;
            this.Version = version;
        }

        //public int CompareTo(Version<TEntity> other)
        //{
        //    return this.Comparer.Compare(this, other);
        //}

        //public int CompareTo(object other)
        //{
        //    return this.CompareTo((Version<TEntity>)other);
        //}

        //public static bool operator >(Version<TEntity> x, Version<TEntity> y)
        //{
        //    return x.CompareTo(y) > 0;
        //}

        //public static bool operator <(Version<TEntity> x, Version<TEntity> y)
        //{
        //    return x.CompareTo(y) < 0;
        //}
    }
}
