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
        /// Returns the length of byte array.
        /// </summary>
        public int Length
        {
            get { return __bytes.Length; }
        }

        /// <summary>
        /// Creates a byte array by copying
        /// the specified byte array.
        /// </summary>
        public ByteArray(byte[] initialValue)
        {
            __bytes = initialValue.ToArray();
        }

        public ByteArray(string hex)
        {
            if (string.IsNullOrEmpty(hex)) throw new ArgumentException("hex");
            if (hex.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase)) hex = hex.Substring(2);

            hex = hex.Replace("-", string.Empty);

            var numOfChars = hex.Length;

            if (numOfChars % 2 != 0)
            {
                throw new InvalidOperationException("Number of characters must be even.");
            }

            __bytes = new byte[numOfChars / 2];

            for (int i = 0; i < numOfChars; i += 2)
            {
                __bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
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
        /// Returns a copy of the underlying value.
        /// </summary>
        public byte[] ToArray()
        {
            return __bytes.ToArray();
        }

        /// <summary>
        /// Returns the timestamp as a 32-bit signed integer.
        /// </summary>
        public long ToInt32()
        {
            var hexString = "0x" + this.ToString().Replace("-", string.Empty);

            return Convert.ToInt32(hexString, 16);
        }

        /// <summary>
        /// Returns the timestamp as a 64-bit signed integer.
        /// </summary>
        public long ToLong()
        {
            var hexString = "0x" + this.ToString().Replace("-", string.Empty);

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

            return object.Equals(this.__bytes, other.__bytes);
        }

        public override int GetHashCode()
        {
            return __bytes.GetHashCode();
        }

        public int CompareTo(ByteArray other)
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(ByteArray x, ByteArray y)
        {
            return object.Equals(x, y);
        }

        public static bool operator !=(ByteArray x, ByteArray y)
        {
            return !object.Equals(x, y);
        }

        public static implicit operator ByteArray(string hex)
        {
            return new ByteArray(hex);
        }

        public static ByteArray operator ++(ByteArray bytes)
        {
            var lng = bytes.ToLong() + 1;

            return new ByteArray(lng, bytes.Length);
        }

        public static ByteArray operator --(ByteArray bytes)
        {
            var lng = bytes.ToLong() - 1;

            return new ByteArray(lng, bytes.Length);
        }
    }
}
