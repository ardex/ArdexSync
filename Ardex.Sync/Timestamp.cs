using System;
using System.Linq;

namespace Ardex.Sync
{
    /// <summary>
    /// Data type used by timestamp columns.
    /// Uses a 64-bit (8-byte) array as backing storage.
    /// </summary>
    public sealed class Timestamp : ByteArray
    {
        /// <summary>
        /// Copy constructor.
        /// </summary>
        public Timestamp(byte[] value) : base(value)
        {
            if (this.Length != 8)
            {
                throw new ArgumentException("Timestamp value must be 8 bytes long.");
            }
        }

        public Timestamp(string value) : base(value)
        {
            if (this.Length != 8)
            {
                throw new ArgumentException("Timestamp value must be 8 bytes long.");
            }
        }

        public Timestamp(long value) : base(value, 8)
        {

        }

        public static Timestamp operator ++(Timestamp value)
        {
            return new Timestamp(value.ToInt64() + 1);
        }

        public static Timestamp operator --(Timestamp value)
        {
            return new Timestamp(value.ToInt64() - 1);
        }
    }
}
