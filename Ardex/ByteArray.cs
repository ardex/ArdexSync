using System;
using System.Collections.Generic;
using System.Linq;

namespace Ardex
{
    /// <summary>
    /// Immutable wrapper for a fixed length byte array.
    /// Provides a more natural way of working with binary data.
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

        /// <summary>
        /// Creates a byte array from a hex string.
        /// </summary>
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
        /// Gets the byte at the specified index.
        /// </summary>
        public byte this[int index]
        {
            get
            {
                return __bytes[index];
            }
        }

        /// <summary>
        /// Enumerates over the byte array's contents.
        /// </summary>
        public IEnumerable<byte> AsEnumerable()
        {
            foreach (var b in __bytes)
            {
                yield return b;
            }
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
        public int ToInt32()
        {
            var hexString = "0x" + this.ToString().Replace("-", string.Empty);

            return Convert.ToInt32(hexString, 16);
        }

        /// <summary>
        /// Returns the timestamp as a 64-bit signed integer.
        /// </summary>
        public long ToInt64()
        {
            var hexString = "0x" + this.ToString().Replace("-", string.Empty);

            return Convert.ToInt64(hexString, 16);
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
        /// Compares this instance with the given object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as ByteArray);
        }

        /// <summary>
        /// Compares this instance with another byte array.
        /// </summary>
        public bool Equals(ByteArray other)
        {
            if (other == null)
            {
                return false;
            }

            return object.Equals(this.__bytes, other.__bytes);
        }

        /// <summary>
        /// Returns the byte array's hashcode.
        /// </summary>
        public override int GetHashCode()
        {
            return __bytes.GetHashCode();
        }

        /// <summary>
        /// Compares this instance to another
        /// byte array and returns the difference.
        /// </summary>
        public int CompareTo(ByteArray other)
        {
            var x = this.__bytes;
            var y = other.__bytes;

            // Argument validation.
            if (object.ReferenceEquals(x, null)) throw new InvalidOperationException("This instance's underlying byte array is null.");
            if (object.ReferenceEquals(y, null)) throw new InvalidOperationException("Other instance's underlying byte array is null.");
            if (x.Length != y.Length) throw new ArgumentException("Cannot compare arrays of different lengths.");

            var length = x.Length;

            for (int i = 0; i < length; i++)
            {
                var b1 = x[i];
                var b2 = y[i];
                var comp = b1.CompareTo(b2);

                if (comp != 0)
                {
                    // Found the difference.
                    return (comp > 0) ? 1 : -1;
                }
            }

            // Equal.
            return 0;
        }

        public static bool operator ==(ByteArray x, ByteArray y)
        {
            return object.Equals(x, y);
        }

        public static bool operator !=(ByteArray x, ByteArray y)
        {
            return !object.Equals(x, y);
        }

        public static implicit operator ByteArray(byte[] bytes)
        {
            return new ByteArray(bytes);
        }

        public static implicit operator byte[](ByteArray byteArray)
        {
            return byteArray.ToArray();
        }

        public static ByteArray operator ++(ByteArray bytes)
        {
            var lng = bytes.ToInt64() + 1;

            return new ByteArray(lng, bytes.Length);
        }

        public static ByteArray operator --(ByteArray bytes)
        {
            var lng = bytes.ToInt64() - 1;

            return new ByteArray(lng, bytes.Length);
        }
    }
}
