using System.Runtime.InteropServices;

namespace LaserCore.Ilda.Net.Dto
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IldaPoint
    {
        public short X;
        public short Y;
        public ushort R;
        public ushort G;
        public ushort B;
        public ushort I;
    }
}
