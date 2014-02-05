//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Ardex.Util
//{
//    /// <summary>
//    /// Utility methods for working with binary timestamp values.
//    /// </summary>
//    public static class TimestampUtil
//    {
//        /// <summary>
//        /// Converts a binary timestamp to a hex string.
//        /// </summary>
//        public static string BytesToString(byte[] bytes)
//        {
//            var hex = BitConverter.ToString(bytes);

//            hex = hex
//                .Replace("-", string.Empty)
//                .TrimStart('0');

//            return hex;
//        }

//        /// <summary>
//        /// Converts a hex string into a binary value.
//        /// </summary>
//        public static byte[] StringToBytes(string hex)
//        {
//            if (string.IsNullOrEmpty(hex))
//            {
//                throw new ArgumentException("hex");
//            }

//            if (hex.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
//                hex = hex.Substring(2);

//            if (hex.Length > 16)
//                throw new Exception("Hex string too long to be converted to timestamp.");

//            if (hex.Length < 16)
//                hex = hex.PadLeft(16, '0');

//            var numOfChars = hex.Length;
//            var bytes = new byte[numOfChars / 2];

//            for (int i = 0; i < numOfChars; i += 2)
//            {
//                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
//            }

//            return bytes;
//        }
//    }

//    /// <summary>
//    /// Comparer for byte arrays of equal length.
//    /// </summary>
//    public class ByteArrayComparer : IComparer<byte[]>
//    {
//        /// <summary>
//        /// Compares the two given byte arrays of equal length.
//        /// </summary>
//        public int Compare(byte[] x, byte[] y)
//        {
//            // Argument validation.
//            if (x == null) throw new ArgumentNullException("x");
//            if (y == null) throw new ArgumentNullException("y");
//            if (x.Length != y.Length) throw new ArgumentException("Cannt compare arrays of different lengths.");

//            int length = x.Length;

//            for (int i = 0; i < length; i++)
//            {
//                byte b1 = x[i];
//                byte b2 = y[i];
//                int comp = b1.CompareTo(b2);

//                if (comp != 0)
//                {
//                    // Found the difference.
//                    return (comp > 0) ? 1 : -1;
//                }
//            }

//            // Equal.
//            return 0;
//        }
//    }
//}
