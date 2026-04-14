using System.Collections.Generic;

namespace LaserCore.Ilda.Net.Dto
{
    public class IldaFrame
    {
        public int FrameNumber { get; set; }
        public List<IldaPoint> Points { get; set; } = new();
    }
}
