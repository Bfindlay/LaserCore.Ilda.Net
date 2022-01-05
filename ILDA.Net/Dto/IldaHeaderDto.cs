namespace LaserCore.Ilda.Net.Dto
{
    public class IldaHeader
    {
        // ASCII Letters ILDA
        public string ILDA;

        // First reserved portion of the header
        public byte[] Reserved;

        // The format, or type, of the header.
        public byte FormatCode;

        // Frame or Color Palette Name
        public string Name;

        // Name of company that produced frame
        public string Company;

        // Number of records / points following the header
        public ushort RecordCount;

        // If frame part of anumation, this is the frame number
        public ushort FrameNumber;

        // Total number of frames in this sequence
        public ushort TotalFrames;

        // Projector Number to display this frame on
        public byte ProjectorNumber;

        // Final reserved portion
        public byte Reserved2;
    }
}

/*
 *  Name       | Bytes
 *  --------------------
 *  ILDA       | 4
 *  R1         | 3
 *  Format     | 1
 *  Name       | 8
 *  Company    | 8
 *  Records    | 2 // unsigned integer ( 0 - 65535)
 *  Frame num  | 2 // unsigned integer ( 0 - 65535)
 *  num Frames | 2 // unsigned integer (1 - 65535) or if a pallet is 0
 *  projector  | 1 
 *  reserved 2 | 1
 */