namespace LaserCore.Ilda.Net.Dto
{
    public class IldaHeader
    {
        public string Magic { get; init; } = string.Empty;
        public byte FormatCode { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Company { get; init; } = string.Empty;
        public ushort RecordCount { get; init; }
        public ushort FrameNumber { get; init; }
        public ushort TotalFrames { get; init; }
        public byte ProjectorNumber { get; init; }
    }
}
