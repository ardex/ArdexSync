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
                this.ReplaceSegment(ref _segment1, value);
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
                this.ReplaceSegment(ref _segment2, value);
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
                this.ReplaceSegment(ref _segment3, value);
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
                this.ReplaceSegment(ref _segment4, value);
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
                this.ReplaceSegment(ref _segment5, value);
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

            _segment1 = new ByteArray(segments[0]);
            _segment2 = new ByteArray(segments[1]);
            _segment3 = new ByteArray(segments[2]);
            _segment4 = new ByteArray(segments[3]);
            _segment5 = new ByteArray(segments[4]);
        }

        public GuidBuilder(int head, short segment2, short segment3, short segment4, long tail)
        {
            _segment1 = new ByteArray(head, 4);
            _segment2 = new ByteArray(segment2, 2);
            _segment3 = new ByteArray(segment3, 2);
            _segment4 = new ByteArray(segment4, 2);
            _segment5 = new ByteArray(tail, 6);
        }

        private void ReplaceSegment(ref ByteArray segment, ByteArray newValue)
        {
            if (newValue.Length != segment.Length)
            {
                throw new InvalidOperationException("Invalid segment length.");
            }

            segment = newValue;
        }

        public byte[] ToByteArray()
        {
            var segments = new[] {
                this.Segment1.ToArray().Reverse().ToArray(),
                this.Segment2.ToArray().Reverse().ToArray(),
                this.Segment3.ToArray().Reverse().ToArray(),
                this.Segment4.ToArray(),
                this.Segment5.ToArray()
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
