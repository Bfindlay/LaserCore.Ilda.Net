using System.Collections.Generic;
using System.IO;
using laserCore.Ilda.Net.Dto;

namespace laserCore.Ilda.Net.FileHelpers
{
    public static class IldaParser
    {
        public static List<PointDto> Parse(FileStream source)
        {
            var points = IldaFileReader.Read(source);
            return points;
        }
    }
}
