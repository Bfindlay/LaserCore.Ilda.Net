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
    /// Supports all five ILDA formats per IDTF14 rev011:
    ///   Format 0 – 3D Coordinates with Indexed Color (8 bytes/record)
    ///   Format 1 – 2D Coordinates with Indexed Color (6 bytes/record)
    ///   Format 2 – Color Palette (3 bytes/record: R, G, B)
    ///   Format 4 – 3D Coordinates with True Color (10 bytes/record)
    ///   Format 5 – 2D Coordinates with True Color (8 bytes/record)
    /// </summary>
    public static class IldaParser
    {
        private const int HeaderSize = 32;
        private const byte BlankBit = 0x40;  // Status code bit 6

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

            // Per-projector color palettes (spec §3.4): format 2 sections set the
            // palette for a projector, used by subsequent indexed-color frames.
            var palettes = new Dictionary<byte, byte[]>();

            while (offset + HeaderSize <= buffer.Length)
            {
                var headerSlice = buffer.Slice(offset, HeaderSize);
                var header = ParseHeader(headerSlice);

                if (header.Magic != "ILDA" || header.RecordCount == 0)
                    break;

                // Format 2 — Color Palette (3 bytes per record: R, G, B)
                if (header.FormatCode == 2)
                {
                    var paletteBytes = header.RecordCount * 3;
                    if (offset + HeaderSize + paletteBytes > buffer.Length)
                        break;

                    var paletteData = buffer.Slice(offset + HeaderSize, paletteBytes);
                    var palette = new byte[header.RecordCount * 3];
                    paletteData.CopyTo(palette);
                    palettes[header.ProjectorNumber] = palette;

                    offset += HeaderSize + paletteBytes;
                    continue;
                }

                var pointSize = GetPointSize(header.FormatCode);
                if (pointSize < 0)
                    break;

                var recordBytes = header.RecordCount * pointSize;
                if (offset + HeaderSize + recordBytes > buffer.Length)
                    break;

                var recordData = buffer.Slice(offset + HeaderSize, recordBytes);

                // Resolve which palette to use for indexed-color formats
                byte[]? activePalette = null;
                if (header.FormatCode is 0 or 1)
                    palettes.TryGetValue(header.ProjectorNumber, out activePalette);

                var points = new List<IldaPoint>(header.RecordCount);
                for (var i = 0; i < header.RecordCount; i++)
                {
                    var pointSlice = recordData.Slice(i * pointSize, pointSize);
                    points.Add(ParsePoint(header.FormatCode, pointSlice, activePalette));
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

        private static IldaPoint ParsePoint(byte formatCode, ReadOnlySpan<byte> data, byte[]? customPalette)
        {
            var x = BinaryPrimitives.ReadInt16BigEndian(data);
            var y = BinaryPrimitives.ReadInt16BigEndian(data[2..]);

            bool is3D = formatCode is 0 or 4;
            int statusOffset = is3D ? 6 : 4;
            bool isBlanked = (data[statusOffset] & BlankBit) != 0;

            // Spec §5.2.4: blanking bit takes precedence — all RGB treated as zero
            if (isBlanked)
                return new IldaPoint { X = x, Y = y };

            bool isTrueColor = formatCode is 4 or 5;
            if (isTrueColor)
            {
                // BGR byte order after status code (spec §5.1.4/§5.1.5)
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

            // Indexed color — lookup in custom palette (format 2) or default palette
            int colorIndex = data[statusOffset + 1];
            var (pr, pg, pb) = GetPaletteColor(colorIndex, customPalette);
            return new IldaPoint
            {
                X = x,
                Y = y,
                R = pr,
                G = pg,
                B = pb,
            };
        }

        /// <summary>
        /// Looks up a color index in the given custom palette, or falls back to
        /// the ILDA suggested default 64-color palette (spec Appendix A).
        /// </summary>
        private static (ushort R, ushort G, ushort B) GetPaletteColor(int index, byte[]? customPalette)
        {
            if (customPalette != null)
            {
                var maxIndex = customPalette.Length / 3 - 1;
                var ci = Math.Clamp(index, 0, maxIndex) * 3;
                return (MapByteToUShort(customPalette[ci]), MapByteToUShort(customPalette[ci + 1]), MapByteToUShort(customPalette[ci + 2]));
            }

            return GetDefaultPaletteColor(index);
        }

        /// <summary>ILDA suggested default 64-color palette (spec Appendix A).</summary>
        private static (ushort R, ushort G, ushort B) GetDefaultPaletteColor(int index)
        {
            ReadOnlySpan<byte> palette = new byte[]
            {
                255,0,0,     255,16,0,    255,32,0,    255,48,0,    //  0- 3: red → orange
                255,64,0,    255,80,0,    255,96,0,    255,112,0,   //  4- 7
                255,128,0,   255,144,0,   255,160,0,   255,176,0,   //  8-11: orange → yellow
                255,192,0,   255,208,0,   255,224,0,   255,240,0,   // 12-15
                255,255,0,   224,255,0,   192,255,0,   160,255,0,   // 16-19: yellow → green
                128,255,0,   96,255,0,    64,255,0,    32,255,0,    // 20-23
                0,255,0,     0,255,36,    0,255,73,    0,255,109,   // 24-27: green → cyan
                0,255,146,   0,255,182,   0,255,219,   0,255,255,   // 28-31
                0,227,255,   0,198,255,   0,170,255,   0,142,255,   // 32-35: cyan → blue
                0,113,255,   0,85,255,    0,56,255,    0,28,255,    // 36-39
                0,0,255,     32,0,255,    64,0,255,    96,0,255,    // 40-43: blue → magenta
                128,0,255,   160,0,255,   192,0,255,   224,0,255,   // 44-47
                255,0,255,   255,32,255,  255,64,255,  255,96,255,  // 48-51: magenta → pink
                255,128,255, 255,160,255, 255,192,255, 255,224,255, // 52-55
                255,255,255, 255,224,224, 255,192,192, 255,160,160, // 56-59: white → pink/red
                255,128,128, 255,96,96,   255,64,64,   255,32,32,   // 60-63
            };

            var i = Math.Clamp(index, 0, 63) * 3;
            return (MapByteToUShort(palette[i]), MapByteToUShort(palette[i + 1]), MapByteToUShort(palette[i + 2]));
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
