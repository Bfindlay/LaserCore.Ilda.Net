using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LaserCore.Ilda.Net.Dto;

namespace LaserCore.Ilda.Net
{
    /// <summary>
    /// Parses ILDA (International Laser Display Association) binary files into frames.
    /// Supports formats 0 (3D indexed), 1 (2D indexed), 4 (3D true color), and 5 (2D true color).
    /// </summary>
    public static class IldaParser
    {
        private const int HeaderSize = 32;
        private const byte BlankBit = 0x40;

        public static List<IldaFrame> Parse(Stream source)
        {
            if (source.CanSeek)
                source.Position = 0;

            using var ms = new MemoryStream();
            source.CopyTo(ms);
            return Parse(ms.GetBuffer().AsSpan(0, (int)ms.Length));
        }

        public static List<IldaFrame> Parse(ReadOnlySpan<byte> buffer)
        {
            var frames = new List<IldaFrame>();
            var offset = 0;

            while (offset + HeaderSize <= buffer.Length)
            {
                var headerSlice = buffer.Slice(offset, HeaderSize);
                var header = ParseHeader(headerSlice);

                if (header.Magic != "ILDA" || header.RecordCount == 0)
                    break;

                var pointSize = GetPointSize(header.FormatCode);
                if (pointSize < 0)
                    break;

                var recordBytes = header.RecordCount * pointSize;
                if (offset + HeaderSize + recordBytes > buffer.Length)
                    break;

                var recordData = buffer.Slice(offset + HeaderSize, recordBytes);
                var points = new List<IldaPoint>(header.RecordCount);

                for (var i = 0; i < header.RecordCount; i++)
                {
                    var pointSlice = recordData.Slice(i * pointSize, pointSize);
                    points.Add(ParsePoint(header.FormatCode, pointSlice));
                }

                if (points.Count > 0)
                {
                    frames.Add(new IldaFrame
                    {
                        FrameNumber = header.FrameNumber,
                        Points = points
                    });
                }

                offset += HeaderSize + recordBytes;
            }

            return frames;
        }

        private static IldaHeader ParseHeader(ReadOnlySpan<byte> data)
        {
            return new IldaHeader
            {
                Magic = Encoding.ASCII.GetString(data.Slice(0, 4)),
                FormatCode = data[7],
                Name = Encoding.ASCII.GetString(data.Slice(8, 8)).TrimEnd('\0'),
                Company = Encoding.ASCII.GetString(data.Slice(16, 8)).TrimEnd('\0'),
                RecordCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(24, 2)),
                FrameNumber = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(26, 2)),
                TotalFrames = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(28, 2)),
                ProjectorNumber = data[30],
            };
        }

        private static IldaPoint ParsePoint(byte formatCode, ReadOnlySpan<byte> data)
        {
            var x = BinaryPrimitives.ReadInt16BigEndian(data);
            var y = BinaryPrimitives.ReadInt16BigEndian(data[2..]);

            bool is3D = formatCode is 0 or 4;
            int statusOffset = is3D ? 6 : 4;
            bool isBlanked = (data[statusOffset] & BlankBit) != 0;

            if (isBlanked)
                return new IldaPoint { X = x, Y = y };

            bool isTrueColor = formatCode is 4 or 5;
            if (isTrueColor)
            {
                // BGR byte order after status code
                int colorOffset = statusOffset + 1;
                return new IldaPoint
                {
                    X = x,
                    Y = y,
                    R = MapByteToUShort(data[colorOffset + 2]),
                    G = MapByteToUShort(data[colorOffset + 1]),
                    B = MapByteToUShort(data[colorOffset]),
                };
            }

            // Indexed color — palette lookup not implemented, default to white
            return new IldaPoint
            {
                X = x,
                Y = y,
                R = ushort.MaxValue,
                G = ushort.MaxValue,
                B = ushort.MaxValue,
            };
        }

        private static int GetPointSize(byte formatCode) => formatCode switch
        {
            0 => 8,  // 3D indexed: X(2) Y(2) Z(2) Status(1) ColorIndex(1)
            1 => 6,  // 2D indexed: X(2) Y(2) Status(1) ColorIndex(1)
            4 => 10, // 3D true color: X(2) Y(2) Z(2) Status(1) B(1) G(1) R(1)
            5 => 8,  // 2D true color: X(2) Y(2) Status(1) B(1) G(1) R(1)
            _ => -1
        };

        /// <summary>Maps a byte (0–255) to ushort (0–65535).</summary>
        private static ushort MapByteToUShort(byte value) => (ushort)(value * 257);
    }
}
