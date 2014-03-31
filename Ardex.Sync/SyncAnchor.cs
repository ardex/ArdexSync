using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Ardex.Sync
{
    /// <summary>
    /// Dictionary where the key is the replica ID,
    /// and the value is the maximum known version value
    /// for any entity associated with this replica ID.
    /// </summary>
    [DataContract]
    public class SyncAnchor<TVersion>
    {
        /// <summary>
        /// Internal dictionary where the key
        /// is the unique ID of the replica
        /// described by the anchor entry.
        /// </summary>
        private Dictionary<int, TVersion> Dictionary;

        /// <summary>
        /// Source replica info specified when this instance was created.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public SyncReplicaInfo ReplicaInfo { get; private set; }

        /// <summary>
        /// Gets the underlying collection of anchor entries.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public SyncAnchorEntry<TVersion>[] Entries
        {
            get
            {
                return this.Dictionary
                    .Select(kvp => new SyncAnchorEntry<TVersion>(kvp.Key, kvp.Value))
                    .ToArray();
            }
            private set
            {
                // Required for serialization to work.
                if (this.Dictionary != null)
                {
                    throw new InvalidOperationException("Entries have already been initialised.");
                }

                this.Dictionary = value.ToDictionary(kvp => kvp.ReplicaID, kvp => kvp.MaxVersion);
            }
        }

        /// <summary>
        /// Gets the version with the given sync replica ID.
        /// </summary>
        public TVersion this[int replicaID]
        {
            get
            { 
                return this.Dictionary[replicaID];
            }
            set
            {
                this.Dictionary[replicaID] = value;
            }
        }

        /// <summary>
        /// Creates a new instance of SyncAnchor{TVersion}.
        /// </summary>
        public SyncAnchor(SyncReplicaInfo replicaInfo)
        {
            // Parameters.
            this.ReplicaInfo = replicaInfo;
            this.Dictionary = new Dictionary<int, TVersion>();
        }

        /// <summary>
        /// Adds the specified key and value to the underlying dictionary.
        /// </summary>
        public void Add(int replicaID, TVersion maxVersion)
        {
            this.Dictionary.Add(replicaID, maxVersion);
        }

        /// <summary>
        /// Gets the max known version for the given sync replica ID.
        /// </summary>
        public bool TryGetValue(int replicaID, out TVersion maxVersion)
        {
            return this.Dictionary.TryGetValue(replicaID, out maxVersion);
        }
    }
}