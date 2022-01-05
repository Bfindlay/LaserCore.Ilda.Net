using System.Collections.Generic;
using System.Linq;

namespace LaserCore.Ilda.Net.Dto
{
    public class IldaFrame
    {
        public int FrameNumber { get; set; }
        public IEnumerable<PointDto> Points { get; set; } = Enumerable.Empty<PointDto>();
    }
}
