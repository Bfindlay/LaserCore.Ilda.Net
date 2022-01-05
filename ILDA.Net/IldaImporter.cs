using System.Collections.Generic;
using System.IO;
using LaserCore.Ilda.Net.Dto;
using LaserCore.Ilda.Net.FileHelpers;

namespace LaserCore.Ilda.Net
{
    public static class IldaImporter
    {
        public static List<IldaFrame> ParseIlda(FileStream source)
        {
            return IldaParser.Parse(source);
        }
    }
}