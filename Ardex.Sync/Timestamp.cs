using System;
using System.Linq;

namespace Ardex.Sync
{
    /// <summary>
    /// Data type used by timestamp columns.
    /// </summary>
    public sealed class Timestamp : IEquatable<Timestamp>, IComparable<Timestamp>, IComparable
    {
        /// <summary>
        /// 8-byte backing array.
        /// </summary>
        private readonly byte[] __bytes;

        /// <summary>
        /// Creates a timestamp value from the given byte array.
        /// </summary>
        public Timestamp(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            if (bytes.Length != 8) throw new ArgumentException("The array must be exactly 8 bytes long.");

            __bytes = bytes.ToArray();
        }

        /// <summary>
        /// Creates a timestamp value from the given signed 64 bit integer.
        /// </summary>
        public Timestamp(long num)
        {
            if (num <= 0) throw new ArgumentOutOfRangeException("num");

            __bytes = BitConverter.GetBytes(num)
                                  .Reverse()
                                  .ToArray();
        }

        /// <summary>
        /// Creates a timetsamp valud from the given hex string.
        /// </summary>
        public Timestamp(string hex)
        {
            if (string.IsNullOrEmpty(hex)) throw new ArgumentException("hex");
            if (hex.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase)) hex = hex.Substring(2);
            if (hex.Length > 16) throw new Exception("Hex string too long to be converted to timestamp.");

            if (hex.Length < 16)
                hex = hex.PadLeft(16, '0');

            var numOfChars = hex.Length;

            __bytes = new byte[8];

            for (int i = 0; i < numOfChars; i += 2)
            {
                __bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
        }

        /// <summary>
        /// Returns the timestamp as a HEX string.
        /// </summary>
        public override string ToString()
        {
            var hex = BitConverter.ToString(__bytes);

            hex = hex
                .Replace("-", string.Empty)
                .TrimStart('0');

            return hex;
        }

        /// <summary>
        /// Returns the timestamp as a long HEX string.
        /// </summary>
        public string ToLongString()
        {
            var hex = BitConverter.ToString(__bytes);

            return "0x" + hex;
        }

        /// <summary>
        /// Returns a copy of the underlying byte array.
        /// </summary>
        public byte[] ToBytes()
        {
            return __bytes.ToArray();
        }

        /// <summary>
        /// Returns the timestamp as a 64-bit signed integer.
        /// </summary>
        public long ToLong()
        {
            var hexString = "0x" + this.ToString();

            return Convert.ToInt64(hexString, 16);
        }

        /// <summary>
        /// Returns true if the given object is
        /// of type Timestamp with the value
        /// equivalent to that of this instance.
        /// </summary>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as Timestamp);
        }

        /// <summary>
        /// Returns the hash code of the underlying byte array.
        /// </summary>
        public override int GetHashCode()
        {
            return __bytes.GetHashCode();
        }

        #region Operators

        /// <summary>
        /// Returns true if instances on both
        /// sides of the operator have equal value.
        /// </summary>
        public static bool operator ==(Timestamp x, Timestamp y)
        {
            if (object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null)) return true;
            if (object.ReferenceEquals(x, null)) return false;

            return x.Equals(y);
        }

        /// <summary>
        /// Returns true if instances on either
        /// side of the operator have different values.
        /// </summary>
        public static bool operator !=(Timestamp x, Timestamp y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Returns true if the value of the instance on
        /// the left side/ of the operator is greater than
        /// the value of the instance on the right side.
        /// </summary>
        public static bool operator >(Timestamp x, Timestamp y)
        {
            return x.CompareTo(y) > 0;
        }

        /// <summary>
        /// Returns true if the value of the instance on
        /// the left side/ of the operator is smaller than
        /// the value of the instance on the right side.
        /// </summary>
        public static bool operator <(Timestamp x, Timestamp y)
        {
            return x.CompareTo(y) < 0;
        }

        /// <summary>
        /// Increments the timestamp value by one and
        /// returns a new instance representing the new value.
        /// </summary>
        public static Timestamp operator ++(Timestamp timestamp)
        {
            var sum = timestamp.ToLong() + 1;

            return new Timestamp(sum);
        }

        /// <summary>
        /// Decrements the timestamp value by one and
        /// returns a new instance representing the new value.
        /// </summary>
        public static Timestamp operator --(Timestamp timestamp)
        {
            var sum = timestamp.ToLong() - 1;

            return new Timestamp(sum);
        }

        #endregion

        #region IEquatable implementation

        /// <summary>
        /// Returns true if the specified Timestamp instance
        /// has the value equivalent to that of this instance.
        /// </summary>
        public bool Equals(Timestamp other)
        {
            if (object.ReferenceEquals(other, null)) return false;

            var x = this.__bytes;
            var y = other.__bytes;

            if (x.Length != y.Length) throw new ArgumentException("Cannot compare arrays of different lengths.");

            for (var i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region IComparable implementation

        public int CompareTo(object value)
        {
            if (value == null)
            {
                return 1;
            }

            var timestamp = value as Timestamp;

            if (timestamp != null)
            {
                return this.CompareTo(timestamp);
            }

            throw new InvalidOperationException("Invalid comparison.");
        }

        /// <summary>
        /// Compares this instance with the other instance.
        /// </summary>
        public int CompareTo(Timestamp other)
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

        #endregion
    }
}
