using System;
using System.Linq;

namespace Ardex
{
    /// <summary>
    /// Immutable wrapper for a fixed length byte array.
    /// </summary>
    public class ByteArray : IEquatable<ByteArray>, IComparable<ByteArray>
    {
        // Backing storage.
        private readonly byte[] __bytes;

        /// <summary>
        /// Returns a copy of the underlying value.
        /// </summary>
        public byte[] Value
        {
            get
            {
                return __bytes.ToArray();
            }
        }

        /// <summary>
        /// Creates a byte array by copying
        /// the specified byte array.
        /// </summary>
        public ByteArray(params byte[] initialValue)
        {
            __bytes = initialValue.ToArray();
        }

        /// <summary>
        /// Creates a byte array from a
        /// 64-bit signed integer value.
        /// </summary>
        public ByteArray(long value)
        {
            __bytes = BitConverter
                .GetBytes(value)
                .Reverse()
                .ToArray();
        }

        /// <summary>
        /// Creates a byte array of specified length
        /// from a 64-bit signed integer value.
        /// </summary>
        public ByteArray(long value, int length)
        {
            var bytes = BitConverter
                .GetBytes(value)
                .Reverse()
                .ToList();

            // Truncate if necessary.
            while (bytes.Count > length)
            {
                if (bytes[0] != 0)
                {
                    throw new InvalidOperationException();
                }

                bytes.RemoveAt(0);
            }

            // Pad if necessary.
            while (bytes.Count < length)
            {
                bytes.Insert(0, (byte)0);
            }

            __bytes = bytes.ToArray();
        }

        /// <summary>
        /// Returns the value as a HEX string.
        /// </summary>
        public override string ToString()
        {
            var hex = BitConverter.ToString(__bytes);

            return hex;
        }

        /// <summary>
        /// Returns the timestamp as a 32-bit signed integer.
        /// </summary>
        public long ToLong()
        {
            var hexString = "0x" + this.ToString();

            return Convert.ToInt64(hexString, 16);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ByteArray);
        }

        public bool Equals(ByteArray other)
        {
            if (other == null)
            {
                return false;
            }

            return object.Equals(this.Value, other.Value);
        }

        public int CompareTo(ByteArray other)
        {
            throw new NotImplementedException();
        }
    }
}
