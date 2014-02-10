using System;
using System.Collections.Generic;
using System.Linq;

namespace Ardex
{
    public class GuidBuilder
    {
        // Fields.
        private ByteArray _segment1;
        private ByteArray _segment2;
        private ByteArray _segment3;
        private ByteArray _segment4;
        private ByteArray _segment5;

        /// <summary>
        /// 32-bit head segment.
        /// </summary>
        public ByteArray Segment1
        {
            get
            {
                return _segment1;
            }
            set
            {
                if (value.Value.Length != 4)
                {
                    throw new InvalidOperationException("Invalid segment length.");
                }

                _segment1 = value;
            }
        }

        /// <summary>
        /// 16-bit segment.
        /// </summary>
        public ByteArray Segment2
        {
            get
            {
                return _segment2;
            }
            set
            {
                if (value.Value.Length != 2)
                {
                    throw new InvalidOperationException("Invalid segment length.");
                }

                _segment2 = value;
            }
        }

        /// <summary>
        /// 16-bit segment.
        /// </summary>
        public ByteArray Segment3
        {
            get
            {
                return _segment3;
            }
            set
            {
                if (value.Value.Length != 2)
                {
                    throw new InvalidOperationException("Invalid segment length.");
                }

                _segment3 = value;
            }
        }

        /// <summary>
        /// 16-bit segment.
        /// </summary>
        public ByteArray Segment4
        {
            get
            {
                return _segment4;
            }
            set
            {
                if (value.Value.Length != 2)
                {
                    throw new InvalidOperationException("Invalid segment length.");
                }

                _segment4 = value;
            }
        }

        /// <summary>
        /// 48-bit tail segment.
        /// </summary>
        public ByteArray Segment5
        {
            get
            {
                return _segment5;
            }
            set
            {
                if (value.Value.Length != 6)
                {
                    throw new InvalidOperationException("Invalid segment length.");
                }

                _segment5 = value;
            }
        }

        public GuidBuilder() : this(Guid.Empty) { }

        public GuidBuilder(string guid) : this(Guid.Parse(guid)) { }

        public GuidBuilder(Guid guid)
        {
            var segments = new[] { 
                new byte[4],
                new byte[2],
                new byte[2],
                new byte[2],
                new byte[6]
            };

            var guidBytes = guid.ToByteArray();
            var counter = 0;

            foreach (var segment in segments)
            {
                for (var i = 0; i < segment.Length; i++)
                {
                    segment[i] = guidBytes[counter++];
                }
            }

            this.Segment1 = new ByteArray(segments[0]);
            this.Segment2 = new ByteArray(segments[1]);
            this.Segment3 = new ByteArray(segments[2]);
            this.Segment4 = new ByteArray(segments[3]);
            this.Segment5 = new ByteArray(segments[4]);
        }

        public byte[] ToByteArray()
        {
            var segments = new[] {
                this.Segment1.Value,
                this.Segment2.Value,
                this.Segment3.Value,
                this.Segment4.Value,
                this.Segment5.Value
            };

            var bytes = new byte[16];
            var counter = 0;

            foreach (var segment in segments)
            {
                for (var i = 0; i < segment.Length; i++)
                {
                    bytes[counter++] = segment[i];
                }
            }

            return bytes;
        }

        public Guid ToGuid()
        {
            var bytes = this.ToByteArray();

            return new Guid(bytes);
        }

        public override string ToString()
        {
            return this.ToGuid().ToString();
        }
    }
}
