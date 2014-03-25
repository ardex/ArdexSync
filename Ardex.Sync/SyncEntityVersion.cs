using System.Runtime.Serialization;

namespace Ardex.Sync
{
    public static class SyncEntityVersion
    {
        public static SyncEntityVersion<TEntity, TVersion> Create<TEntity, TVersion>(TEntity entity, TVersion version)
        {
            return new SyncEntityVersion<TEntity, TVersion>(entity, version);
        }
    }

    [DataContract]
    public class SyncEntityVersion<TEntity, TVersion>
    {
        [DataMember(EmitDefaultValue = false)]
        public TEntity Entity { get; private set; }

        [DataMember(EmitDefaultValue = false)]
        public TVersion Version { get; private set; }

        public SyncEntityVersion(TEntity entity, TVersion version)
        {
            this.Entity = entity;
            this.Version = version;
        }
    }
}