using System.Collections.Generic;
using System.IO;
using LaserCore.Ilda.Net.Dto;

namespace LaserCore.Ilda.Net.FileHelpers
{
    public static class IldaParser
    {
        public static List<IldaFrame> Parse(Stream source)
        {
            var points = IldaFileReader.Read(source);
            return points;
        }
    }
}
