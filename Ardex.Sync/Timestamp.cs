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
        private static byte[] Normalise(byte[] bytes)
        {
            if (bytes.Length == 8)
                return bytes;

            var newBytes = new byte[8];

            Array.Copy(bytes, 0, newBytes, newBytes.Length - bytes.Length, bytes.Length);

            return newBytes;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public Timestamp(byte[] value) : base(Timestamp.Normalise(value))
        {
            if (this.Length != 8)
            {
                throw new ArgumentException("Timestamp value must be 8 bytes long.");
            }
        }

        public Timestamp(string value) : base(value
            .Replace("0x", string.Empty)
            .Replace("-", string.Empty)
            .PadLeft(16, '0'))
        {
            if (this.Length != 8)
            {
                throw new ArgumentException("Timestamp value must be 8 bytes long.");
            }
        }

        public Timestamp(long value) : base(value, 8)
        {

        }

        public override string ToString()
        {
            return base.ToString()
                       .Replace("-", string.Empty)
                       .TrimStart('0');
        }

        public static Timestamp operator ++(Timestamp value)
        {
            return new Timestamp(value.ToInt64() + 1);
        }

        public static Timestamp operator --(Timestamp value)
        {
            return new Timestamp(value.ToInt64() - 1);
        }

        public static Timestamp Create(string timestampString)
        {
            if (string.IsNullOrEmpty(timestampString))
            {
                return null;
            }

            return new Timestamp(timestampString);
        }
    }
}