using System;
using System.Runtime.InteropServices;

namespace LaserCore.Ilda.Net.Dto
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe public struct PointDto
    {
        public short X;
        public short Y;
        public ushort R;
        public ushort G;
        public ushort B;
        public ushort I;
    }

    // Format 4 - 3D Coordinate with True Color
    public class IldaTrueColorPoint3D
    {
        public short X;
        public short Y;
        public short Z;
        public byte StatusCode;
        public byte B;
        public byte G;
        public byte R;
    }

    // Format 5 - 2D Point with True COlor
    public class IldaTrueColorPoint2D
    {
        public short X;
        public short Y;
        public byte StatusCode;
        public byte B;
        public byte G;
        public byte R;
    }

    #region ----- Static Point Helpers

    public static class DacPoint
    {
        public static PointDto XYRgb(short x, short y, ushort r, ushort g, ushort b)
        {
            return new PointDto()
            {
                X = x,
                Y = y,
                R = r,
                G = g,
                B = b,
                I = 0,
            };
        }

        public static PointDto XYLuma(short x, short y, ushort luma)

        {
            return new PointDto()
            {
                X = x,
                Y = y,
                R = luma,
                G = luma,
                B = luma,
                I = 0,
            };
        }

        public static PointDto XYBlank(short x, short y)
        {
            return XYLuma(x, y, 0);
        }

        public static Span<byte> serialize(PointDto point)
        {
            Span<byte> bytes = MemoryMarshal.Cast<PointDto, byte>(MemoryMarshal.CreateSpan<PointDto>(ref point, 1));
            return bytes;
        }
    }

    #endregion
}
