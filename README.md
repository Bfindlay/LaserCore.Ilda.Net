# LaserCore.Ilda.Net

A lightweight .NET library for reading and writing [ILDA](https://www.ilda.com/) laser show files. Supports all five ILDA format codes (0, 1, 2, 4, 5) per the IDTF14 rev011 specification.

## Features

- Parse ILDA files from `Stream` or `ReadOnlySpan<byte>`
- Write ILDA files in Format 5 (2D true color)
- All five ILDA format codes supported for reading (indexed color, true color, 2D, 3D, palettes)
- Custom and default 64-color palette support
- Blanking bit handling
- Zero-copy span-based parsing for performance
- No external dependencies

## Installation

Add a reference to the project or include the source files directly:

```bash
dotnet add reference path/to/LaserCore.Ilda.Net.csproj
```

## Quick Start

### Reading an ILDA file

```csharp
using LaserCore.Ilda.Net;
using LaserCore.Ilda.Net.Dto;

// Parse from a file
using var stream = File.OpenRead("show.ild");
List<IldaFrame> frames = IldaParser.Parse(stream);

Console.WriteLine($"Loaded {frames.Count} frames");

foreach (var frame in frames)
{
    Console.WriteLine($"Frame {frame.FrameNumber}: {frame.Points.Count} points");

    foreach (var point in frame.Points)
    {
        // Coordinates: signed 16-bit (-32768 to 32767)
        short x = point.X;
        short y = point.Y;

        // Color channels: unsigned 16-bit (0-65535)
        ushort r = point.R;
        ushort g = point.G;
        ushort b = point.B;
        ushort intensity = point.I;

        // Blanked points have R=G=B=0
        bool isBlanked = r == 0 && g == 0 && b == 0;
    }
}
```

### Parsing from a byte array

```csharp
byte[] fileBytes = File.ReadAllBytes("show.ild");
List<IldaFrame> frames = IldaParser.Parse(fileBytes.AsSpan());
```

### Writing an ILDA file

```csharp
using LaserCore.Ilda.Net;
using LaserCore.Ilda.Net.Dto;

// Build frames
var frames = new List<IldaFrame>
{
    new IldaFrame
    {
        FrameNumber = 0,
        Points = new List<IldaPoint>
        {
            new() { X = -10000, Y = -10000, R = 65535, G = 0, B = 0 },
            new() { X =  10000, Y = -10000, R = 0, G = 65535, B = 0 },
            new() { X =  10000, Y =  10000, R = 0, G = 0, B = 65535 },
            new() { X = -10000, Y =  10000, R = 65535, G = 65535, B = 0 },
            new() { X = -10000, Y = -10000, R = 65535, G = 0, B = 0 },
        }
    }
};

// Write to file (Format 5 - 2D true color)
using var output = File.Create("output.ild");
IldaWriter.Write(output, frames, name: "MyShow", company: "MyCompany");
```

### Round-trip: read, modify, write

```csharp
// Read
using var input = File.OpenRead("original.ild");
var frames = IldaParser.Parse(input);

// Modify - scale down by 50%
foreach (var frame in frames)
{
    for (int i = 0; i < frame.Points.Count; i++)
    {
        var p = frame.Points[i];
        frame.Points[i] = new IldaPoint
        {
            X = (short)(p.X / 2),
            Y = (short)(p.Y / 2),
            R = p.R, G = p.G, B = p.B, I = p.I
        };
    }
}

// Write
using var output = File.Create("scaled.ild");
IldaWriter.Write(output, frames);
```

## API Reference

### `IldaParser`

| Method | Description |
|--------|-------------|
| `static List<IldaFrame> Parse(Stream source)` | Parse ILDA frames from a stream |
| `static List<IldaFrame> Parse(ReadOnlySpan<byte> buffer)` | Parse ILDA frames from a byte span |

### `IldaWriter`

| Method | Description |
|--------|-------------|
| `static void Write(Stream output, List<IldaFrame> frames, string name = "CloudLase", string company = "CloudLase")` | Write frames as Format 5 (2D true color) ILDA |

### Data Types

#### `IldaPoint` (struct, 12 bytes)

| Field | Type | Range | Description |
|-------|------|-------|-------------|
| `X` | `short` | -32768 to 32767 | X coordinate |
| `Y` | `short` | -32768 to 32767 | Y coordinate |
| `R` | `ushort` | 0-65535 | Red channel |
| `G` | `ushort` | 0-65535 | Green channel |
| `B` | `ushort` | 0-65535 | Blue channel |
| `I` | `ushort` | 0-65535 | Intensity |

#### `IldaFrame`

| Property | Type | Description |
|----------|------|-------------|
| `FrameNumber` | `int` | Frame sequence number |
| `Points` | `List<IldaPoint>` | Points in this frame |

#### `IldaHeader`

| Property | Type | Description |
|----------|------|-------------|
| `Magic` | `string` | Always "ILDA" |
| `FormatCode` | `byte` | Format: 0, 1, 2, 4, or 5 |
| `Name` | `string` | Frame name (up to 8 chars) |
| `Company` | `string` | Company name (up to 8 chars) |
| `RecordCount` | `ushort` | Number of points in frame |
| `FrameNumber` | `ushort` | Frame index |
| `TotalFrames` | `ushort` | Total frame count |
| `ProjectorNumber` | `byte` | Projector ID |

## ILDA Format Support

| Format | Description | Read | Write |
|--------|-------------|------|-------|
| 0 | 3D Coordinates + Indexed Color | Yes | - |
| 1 | 2D Coordinates + Indexed Color | Yes | - |
| 2 | Color Palette | Yes | - |
| 4 | 3D Coordinates + True Color | Yes | - |
| 5 | 2D Coordinates + True Color | Yes | Yes |

## Requirements

- .NET 9.0+

## License

MIT
