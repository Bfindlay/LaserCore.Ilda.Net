using System.Collections.Generic;
using System.IO;
using laserCore.Ilda.Net.Dto;
using laserCore.Ilda.Net.FileHelpers;

namespace laserCore.Ilda.Net
{
    public static class IldaImporter
    {
        public static List<PointDto> ParseIlda(FileStream source)
        {
            return IldaParser.Parse(source);
        }
    }
}