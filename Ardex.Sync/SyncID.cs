using System;

namespace Ardex.Sync
{
    /// <summary>
    /// Unique identifier used by sync.
    /// </summary>
    public class SyncID : IEquatable<SyncID>, IComparable<SyncID>
    {
        private readonly string value;

        /// <summary>
        /// Creates a new SyncID.
        /// </summary>
        public SyncID(string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException("value");

            this.value = value;
        }

        public int CompareTo(SyncID other)
        {
            return this.value.CompareTo(other.value);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as SyncID);
        }

        public bool Equals(SyncID other)
        {
            return string.Equals(this.value, other.value, StringComparison.InvariantCulture);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value;
        }

        public static bool operator ==(SyncID left, SyncID right)
        {
            return object.Equals(left, right);
        }

        public static bool operator !=(SyncID left, SyncID right)
        {
            return !(left == right);
        }

        public static implicit operator SyncID(string str)
        {
            return new SyncID(str);
        }

        public static implicit operator SyncID(int num)
        {
            return new SyncID(num.ToString());
        }
    }
}
