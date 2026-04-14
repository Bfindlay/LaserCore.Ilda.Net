using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LaserCore.Ilda.Net.Dto;

namespace LaserCore.Ilda.Net
{
    /// <summary>
    /// Writes ILDA Format 5 (2D true color) binary files from frames.
    /// </summary>
    public static class IldaWriter
    {
        private const byte FormatCode = 5;
        private const int HeaderSize = 32;
        // Format 5 point: X(2) Y(2) Status(1) B(1) G(1) R(1) = 8 bytes
        private const int PointSize = 8;

        public static void Write(Stream output, List<IldaFrame> frames, string name = "LaserCore", string company = "LaserCore")
        {
            var totalFrames = (ushort)frames.Count;

            for (var i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                WriteHeader(output, name, company, (ushort)frame.Points.Count, (ushort)i, totalFrames);

                foreach (var point in frame.Points)
                {
                    WritePoint(output, point);
                }
            }

            // Closing header with 0 records signals end of file
            WriteHeader(output, name, company, 0, totalFrames, totalFrames);
        }

        private static void WriteHeader(Stream output, string name, string company, ushort recordCount, ushort frameNumber, ushort totalFrames)
        {
            Span<byte> header = stackalloc byte[HeaderSize];
            header.Clear();

            // Magic "ILDA" + 3 reserved bytes + format code
            Encoding.ASCII.GetBytes("ILDA", header);
            header[7] = FormatCode;

            // Name (8 bytes, padded)
            var nameBytes = Encoding.ASCII.GetBytes(name.Length > 8 ? name[..8] : name);
            nameBytes.CopyTo(header.Slice(8, 8));

            // Company (8 bytes, padded)
            var companyBytes = Encoding.ASCII.GetBytes(company.Length > 8 ? company[..8] : company);
            companyBytes.CopyTo(header.Slice(16, 8));

            BinaryPrimitives.WriteUInt16BigEndian(header.Slice(24, 2), recordCount);
            BinaryPrimitives.WriteUInt16BigEndian(header.Slice(26, 2), frameNumber);
            BinaryPrimitives.WriteUInt16BigEndian(header.Slice(28, 2), totalFrames);
            header[30] = 0; // projector number

            output.Write(header);
        }

        private static void WritePoint(Stream output, IldaPoint point)
        {
            Span<byte> buf = stackalloc byte[PointSize];

            BinaryPrimitives.WriteInt16BigEndian(buf, point.X);
            BinaryPrimitives.WriteInt16BigEndian(buf.Slice(2), point.Y);

            bool isBlanked = point.R == 0 && point.G == 0 && point.B == 0;
            buf[4] = isBlanked ? (byte)0x40 : (byte)0x00; // status: blank bit

            // BGR byte order
            buf[5] = MapUShortToByte(point.B);
            buf[6] = MapUShortToByte(point.G);
            buf[7] = MapUShortToByte(point.R);

            output.Write(buf);
        }

        /// <summary>Maps a ushort (0–65535) to byte (0–255).</summary>
        private static byte MapUShortToByte(ushort value) => (byte)(value / 257);
    }
}
