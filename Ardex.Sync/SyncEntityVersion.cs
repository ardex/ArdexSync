using System;
using System.Collections.Generic;

namespace Ardex.Sync
{
    public static class SyncEntityVersion
    {
        public static SyncEntityVersion<TEntity, TVersion> Create<TEntity, TVersion>(TEntity entity, TVersion version)
        {
            return new SyncEntityVersion<TEntity, TVersion>(entity, version);
        }
    }

    public class SyncEntityVersion<TEntity, TVersion>
    {
        public TEntity Entity { get; private set; }
        public TVersion Version { get; private set; }

        public SyncEntityVersion(TEntity entity, TVersion version)
        {
            this.Entity = entity;
            this.Version = version;
        }
    }
}
