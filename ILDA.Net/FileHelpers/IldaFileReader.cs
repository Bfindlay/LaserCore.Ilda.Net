using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using laserCore.Ilda.Net.Dto;

namespace laserCore.Ilda.Net.FileHelpers
{
    public class IldaFileReader
    {

        public static List<PointDto> Read(Span<byte> buffer)
        {

            List<PointDto> points = new List<PointDto>();

            var byteOffset = 0;
            while (true)
            {
                var header = buffer.Slice(byteOffset, 32);
                var currentPoints = 0;

                IldaHeader head = new IldaHeader()
                {
                    ILDA = Encoding.ASCII.GetString(header.Slice(0, 4).ToArray()),
                    Reserved = header.Slice(4, 3).ToArray(),
                    FormatCode = header.Slice(7, 1).ToArray()[0],
                    Name = Encoding.ASCII.GetString(header.Slice(8, 8)),
                    Company = Encoding.ASCII.GetString(header.Slice(16, 8)),
                    RecordCount = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(24, 2)),
                    FrameNumber = BitConverter.ToUInt16(header.Slice(26, 2)),
                    TotalFrames = BitConverter.ToUInt16(header.Slice(28, 2)),
                    ProjectorNumber = 0,
                    Reserved2 = header.Slice(31, 1).ToArray()[0]
                };
                

                var pointOffsetSize = head.FormatCode == 5 ? 8 : 10;


                var recordContent = buffer.Slice(byteOffset + 32, (head.RecordCount * pointOffsetSize));

                if (head.RecordCount == 0)
                {
                    break;
                }

                int pointer = 0;
                if (head.FormatCode == 4)
                {
                    while (currentPoints < head.RecordCount)
                    {
                        var pointContent = recordContent.Slice(pointer, 10);
                        IldaTrueColorPoint3D point = new IldaTrueColorPoint3D()
                        {
                            X = BinaryPrimitives.ReadInt16BigEndian(pointContent.Slice(0, 2)),
                            Y = BinaryPrimitives.ReadInt16BigEndian(pointContent.Slice(2, 2)),
                            Z = BinaryPrimitives.ReadInt16BigEndian(pointContent.Slice(4, 2)),
                            StatusCode = pointContent.Slice(6, 1).ToArray()[0],
                            B = pointContent.Slice(7, 1).ToArray()[0],
                            G = pointContent.Slice(8, 1).ToArray()[0],
                            R = pointContent.Slice(9, 1).ToArray()[0]
                        };
                        string yourByteString = Convert.ToString(pointContent.Slice(6, 1).ToArray()[0], 2).PadLeft(8, '0');
                        if (yourByteString != "00000000")
                        {

                            points.Add(DacPoint.XYBlank(point.X, point.Y));
                            currentPoints++;
                        }
                        else
                        {
                            points.Add(DacPoint.XYRgb(point.X, point.Y, Map(point.R), Map(point.G), Map(point.B)));
                            currentPoints++;
                        }

                        pointer += 10;

                    }
                    byteOffset += (head.RecordCount * 10) + 32; // 10 bytes per format 4 code;
                }
                else if (head.FormatCode == 5)
                {
                    while (currentPoints < head.RecordCount)
                    {
                        var pointContent = recordContent.Slice(pointer, 8);
                        IldaTrueColorPoint2D point = new IldaTrueColorPoint2D()
                        {
                            X = BinaryPrimitives.ReadInt16BigEndian(pointContent.Slice(0, 2)),
                            Y = BinaryPrimitives.ReadInt16BigEndian(pointContent.Slice(2, 2)),
                            StatusCode = pointContent.Slice(4, 1).ToArray()[0],
                            B = pointContent.Slice(5, 1).ToArray()[0],
                            G = pointContent.Slice(6, 1).ToArray()[0],
                            R = pointContent.Slice(7, 1).ToArray()[0]
                        };
                        string yourByteString = Convert.ToString(pointContent.Slice(4, 1).ToArray()[0], 2).PadLeft(8, '0');
                        if (yourByteString != "00000000")
                        {
                            points.Add(DacPoint.XYBlank(point.X, point.Y));
                            currentPoints++;
                        }
                        else
                        {
                            points.Add(DacPoint.XYRgb(point.X, point.Y, Map(point.R), Map(point.G), Map(point.B)));
                            currentPoints++;
                        }

                        pointer += 8;

                    }
                    byteOffset += (head.RecordCount * 8) + 32; // 8 bytes per format 5 code;
                }
                else
                {
                    throw new NotImplementedException($"Format Code {head.FormatCode} has not yet been implemented");
                }
            }

            return points;
        }
        public static List<PointDto> Read(Stream stream)
        {

            byte[] bytes = new byte[stream.Length];

            Span<byte> buffer = new Span<byte>(bytes);
            stream.Read(buffer);


            return Read(buffer);

        }

        /*
         * Map byte 0-255 to ushort value
         */
        private static ushort Map(int x)
        {
            const int inMin = 0;
            const int inMax = 255;
            const int outMin = 0;
            const int outMax = 65535;

            return (ushort)((x - inMin) * (outMax - outMin) / (inMax - inMin) + outMin);
        }

    }
}
