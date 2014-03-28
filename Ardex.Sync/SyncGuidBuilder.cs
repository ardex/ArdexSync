using System;
using System.Linq;

namespace Ardex.Sync
{
    /// <summary>
    /// Non-random Guid version specifically tailored to sync scenarios.
    /// </summary>
    public class SyncGuidBuilder
    {
        private Guid Guid { get; set; }

        /// <summary>
        /// ID of the replica which created a particular record.
        /// </summary>
        public int ReplicaID
        {
            get
            {
                var guidBytes = this.Guid.ToByteArray();
                var replicaIdBytes = new byte[4];
                var replicaIdBytesBenchmark = new byte[4];

                // Least significant byte first: don't need to reverse.
                Array.Copy(guidBytes, replicaIdBytes, replicaIdBytes.Length);

                for (var i = 0; i < replicaIdBytesBenchmark.Length; i++)
                {
                    replicaIdBytesBenchmark[i] = guidBytes[i];
                }

                if (!replicaIdBytes.SequenceEqual(replicaIdBytesBenchmark))
                {
                    throw new InvalidOperationException("Test failed.");
                }

                return BitConverter.ToInt32(replicaIdBytes, 0);
            }
            set
            {
                this.Mutate(value, this.ArticleID, this.EntityID);
            }
        }

        /// <summary>
        /// ID of the article that this record was created for.
        /// </summary>
        public short ArticleID
        {
            get
            {
                var guidBytes = this.Guid.ToByteArray();
                var articleIdBytes = new byte[2];
                var articleIdBytesBenchmark = new byte[2];

                // Least significant byte first: don't need to reverse.
                Array.Copy(guidBytes, 6, articleIdBytes, 0, articleIdBytes.Length);

                for (var i = 0; i < articleIdBytesBenchmark.Length; i++)
                {
                    articleIdBytesBenchmark[i] = guidBytes[i + 6];
                }

                if (!articleIdBytes.SequenceEqual(articleIdBytesBenchmark))
                {
                    throw new InvalidOperationException("Test failed.");
                }

                return BitConverter.ToInt16(articleIdBytes, 0);
            }
            set
            {
                this.Mutate(this.ReplicaID, value, this.EntityID);
            }
        }

        /// <summary>
        /// Replica and article-specific entity identifier.
        /// </summary>
        public long EntityID
        {
            get
            {
                var guidBytes = this.Guid.ToByteArray();
                var entityIdBytes = new byte[8];

                // Most significant byte first: reversed.
                var guidIndex = guidBytes.Length - 1;

                for (var i = 0; i < entityIdBytes.Length; i++)
                {
                    entityIdBytes[i] = guidBytes[guidIndex--];
                }

                return BitConverter.ToInt64(entityIdBytes, 0);
            }
            set
            {
                this.Mutate(this.ReplicaID, this.ArticleID, value);
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SyncGuidBuilder()
        {
            this.Guid = Guid.Empty;
        }

        /// <summary>
        /// Creates a new instance of SyncGuidBuilder based on the given Guid value.
        /// </summary>
        public SyncGuidBuilder(Guid guid)
        {
            this.Guid = guid;
        }

        /// <summary>
        /// Creates a new instance of SyncGuidBuilder based on the given replica and entity IDs.
        /// </summary>
        public SyncGuidBuilder(int replicaID, long entityID) : this(replicaID, 0, entityID) { }

        /// <summary>
        /// Creates a new instance of SyncGuidBuilder based on the given
        /// replica and entity IDs (useful for very long IDs).
        /// </summary>
        public SyncGuidBuilder(uint replicaID, ulong entityID) : this(replicaID, 0, entityID) { }

        /// <summary>
        /// Creates a new instance of SyncGuidBuilder based on
        /// the given replica, article and entity IDs.
        /// </summary>
        public SyncGuidBuilder(int replicaID, short articleID, long entityID)
        {
            this.Mutate(replicaID, articleID, entityID);
        }

        /// <summary>
        /// Creates a new instance of SyncGuidBuilder based on
        /// the given replica, article and entity IDs.
        /// (useful for very long IDs).
        /// </summary>
        public SyncGuidBuilder(uint replicaID, ushort articleID, ulong entityID)
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

            this.Guid = new Guid(bytes);
        }

        /// <summary>
        /// Mutates this instance by changing
        /// the underlying Guid value.
        /// </summary>
        private void Mutate(int replicaID, short articleID, long entityID)
        {
            // Most significant byte first.
            var entityIdBytes = BitConverter
                .GetBytes(entityID)
                .Reverse()
                .ToArray();

            this.Guid = new Guid(replicaID, 0, articleID, entityIdBytes);
        }

        /// <summary>
        /// Returns the value of this instance as a Guid.
        /// </summary>
        public Guid ToGuid()
        {
            return this.Guid;
        }

        /// <summary>
        /// Returns the string representation of the underlying Guid.
        /// </summary>
        public override string ToString()
        {
            return this.Guid.ToString();
        }
    }
}