using System;
using System.Linq;

namespace Ardex.Sync
{
    /// <summary>
    /// Non-random Guid version specifically tailored to sync scenarios.
    /// </summary>
    public class SyncGuid
    {
        private readonly Guid __guid;

        /// <summary>
        /// ID of the replica which created a particular record.
        /// </summary>
        public int ReplicaID
        {
            get
            {
                var guidBytes = __guid.ToByteArray();
                var replicaIdBytes = new byte[4];

                // Least significant byte first: don't need to reverse.
                for (var i = 0; i < replicaIdBytes.Length; i++)
                {
                    replicaIdBytes[i] = guidBytes[i];
                }

                return BitConverter.ToInt32(replicaIdBytes, 0);
            }
        }

        /// <summary>
        /// ID of the article that this record was created for.
        /// </summary>
        public short ArticleID
        {
            get
            {
                var guidBytes = __guid.ToByteArray();
                var articleIdBytes = new byte[2];

                // Least significant byte first: don't need to reverse.
                for (var i = 0; i < articleIdBytes.Length; i++)
                {
                    articleIdBytes[i] = guidBytes[i + 6];
                }

                return BitConverter.ToInt16(articleIdBytes, 0);
            }
        }

        /// <summary>
        /// Replica-specific entity identifier.
        /// </summary>
        public long EntityID
        {
            get
            {
                var guidBytes = __guid.ToByteArray();
                var entityIdBytes = new byte[8];

                // Most significant byte first: reversed.
                var guidIndex = guidBytes.Length - 1;

                for (var i = 0; i < entityIdBytes.Length; i++)
                {
                    entityIdBytes[i] = guidBytes[guidIndex--];
                }

                return BitConverter.ToInt64(entityIdBytes, 0);
            }
        }

        /// <summary>
        /// Creates a new instance of SyncGuid based on the given Guid value.
        /// </summary>
        public SyncGuid(Guid guid)
        {
            __guid = guid;
        }

        /// <summary>
        /// Creates a new instance of SyncGuid based on the given replica and entity IDs.
        /// </summary>
        public SyncGuid(int replicaID, long entityID) : this(replicaID, 0, entityID) { }

        /// <summary>
        /// Creates a new instance of SyncGuid based on the given
        /// replica and entity IDs (useful for very long IDs).
        /// </summary>
        public SyncGuid(uint replicaID, ulong entityID) : this(replicaID, 0, entityID) { }

        /// <summary>
        /// Creates a new instance of SyncGuid based on
        /// the given replica, article and entity IDs.
        /// </summary>
        public SyncGuid(int replicaID, short articleID, long entityID)
        {
            // Most significant byte first.
            var entityIdBytes = BitConverter
                .GetBytes(entityID)
                .Reverse()
                .ToArray();

            __guid = new Guid(replicaID, 0, articleID, entityIdBytes);
        }

        /// <summary>
        /// Creates a new instance of SyncGuid based on
        /// the given replica, article and entity IDs.
        /// (useful for very long IDs).
        /// </summary>
        public SyncGuid(uint replicaID, ushort articleID, ulong entityID)
        {
            var replicaIdBytes = BitConverter.GetBytes(replicaID);
            var articleIdBytes = BitConverter.GetBytes(articleID);

            // Most significant byte first.
            var entityIdBytes = BitConverter
                .GetBytes(entityID)
                .Reverse()
                .ToArray();

            var bytes = replicaIdBytes
                .Concat(new byte[2]) // Unused.
                .Concat(articleIdBytes)
                .Concat(entityIdBytes)
                .ToArray();

            __guid = new Guid(bytes);
        }

        /// <summary>
        /// Returns the value of this instance as a Guid.
        /// </summary>
        public Guid ToGuid()
        {
            return __guid;
        }

        public static implicit operator SyncGuid(Guid guid)
        {
            return new SyncGuid(guid);
        }

        public static implicit operator Guid(SyncGuid syncGuid)
        {
            return syncGuid.ToGuid();
        }
    }
}
