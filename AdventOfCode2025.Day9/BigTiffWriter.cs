namespace AdventOfCode2025.Day9;

// BigTiffWriter.cs
// Self-contained library for creating bilevel TIFF/BigTIFF files
// Supports large dimensions via BigTIFF format (64-bit offsets)
// Target: .NET 10

using System;
using System.IO;
using System.Buffers;


/// <summary>
/// Creates bilevel (1-bit) TIFF files with automatic BigTIFF support for large images.
/// </summary>
public static class BigTiffWriter
{
    private const ushort ClassicTiffMagic = 42;
    private const ushort BigTiffMagic = 43;
    private const ushort LittleEndianMarker = 0x4949; // "II"
    
    // TIFF Tag IDs
    private const ushort TagImageWidth = 256;
    private const ushort TagImageLength = 257;
    private const ushort TagBitsPerSample = 258;
    private const ushort TagCompression = 259;
    private const ushort TagPhotometricInterpretation = 262;
    private const ushort TagStripOffsets = 273;
    private const ushort TagRowsPerStrip = 278;
    private const ushort TagStripByteCounts = 279;
    private const ushort TagXResolution = 282;
    private const ushort TagYResolution = 283;
    private const ushort TagResolutionUnit = 296;
    
    // TIFF Type IDs
    private const ushort TypeShort = 3;
    private const ushort TypeLong = 4;
    private const ushort TypeRational = 5;
    private const ushort TypeLong8 = 16; // BigTIFF 64-bit unsigned

    /// <summary>
    /// Creates a bilevel TIFF file where each pixel is determined by the paint function.
    /// </summary>
    /// <param name="filename">Output file path.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="paint">Function returning true for white pixels, false for black.</param>
    /// <exception cref="ArgumentException">Thrown when dimensions are invalid.</exception>
    /// <exception cref="IOException">Thrown when file operations fail.</exception>
    public static void Create(string filename, long width, long height, Func<(long row, long col), bool> paint)
    {
        ArgumentNullException.ThrowIfNull(filename);
        ArgumentNullException.ThrowIfNull(paint);
        
        if (width <= 0) throw new ArgumentException("Width must be positive.", nameof(width));
        if (height <= 0) throw new ArgumentException("Height must be positive.", nameof(height));

        long bytesPerRow = (width + 7) / 8;
        long imageDataSize = bytesPerRow * height;
        bool useBigTiff = NeedsBigTiff(imageDataSize, height);

        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 
            bufferSize: 65536, FileOptions.SequentialScan);
        using var writer = new BinaryWriter(stream);

        if (useBigTiff)
        {
            WriteBigTiff(writer, width, height, bytesPerRow, imageDataSize, paint);
        }
        else
        {
            WriteClassicTiff(writer, width, height, bytesPerRow, imageDataSize, paint);
        }
    }

    private static bool NeedsBigTiff(long imageDataSize, long height)
    {
        // Use BigTIFF if:
        // - Image data exceeds 4GB
        // - Number of strips exceeds what 32-bit offsets can handle
        // - Any offset would exceed uint.MaxValue
        const long maxClassicSize = uint.MaxValue - 1024; // Leave room for headers/IFD
        return imageDataSize > maxClassicSize || height > int.MaxValue;
    }

    private static void WriteClassicTiff(BinaryWriter writer, long width, long height, 
        long bytesPerRow, long imageDataSize, Func<(long row, long col), bool> paint)
    {
        // Classic TIFF Header (8 bytes)
        writer.Write(LittleEndianMarker);
        writer.Write(ClassicTiffMagic);
        
        // Calculate offsets
        long headerSize = 8;
        long ifdOffset = headerSize + imageDataSize;
        
        // Ensure IFD is word-aligned
        if (ifdOffset % 2 != 0) ifdOffset++;
        
        writer.Write((uint)ifdOffset);

        // Write image data
        WriteImageData(writer, width, height, bytesPerRow, paint);

        // Pad to IFD offset if needed
        long currentPos = headerSize + imageDataSize;
        while (currentPos < ifdOffset)
        {
            writer.Write((byte)0);
            currentPos++;
        }

        // Write IFD
        WriteClassicIfd(writer, (uint)width, (uint)height, (uint)bytesPerRow, 
            headerSize, imageDataSize);
    }

    private static void WriteBigTiff(BinaryWriter writer, long width, long height, 
        long bytesPerRow, long imageDataSize, Func<(long row, long col), bool> paint)
    {
        // BigTIFF Header (16 bytes)
        writer.Write(LittleEndianMarker);
        writer.Write(BigTiffMagic);
        writer.Write((ushort)8);  // Offset size
        writer.Write((ushort)0);  // Always 0
        
        // Calculate offsets
        long headerSize = 16;
        long ifdOffset = headerSize + imageDataSize;
        
        // Ensure IFD is word-aligned
        if (ifdOffset % 2 != 0) ifdOffset++;
        
        writer.Write(ifdOffset);

        // Write image data
        WriteImageData(writer, width, height, bytesPerRow, paint);

        // Pad to IFD offset if needed
        long currentPos = headerSize + imageDataSize;
        while (currentPos < ifdOffset)
        {
            writer.Write((byte)0);
            currentPos++;
        }

        // Write IFD
        WriteBigTiffIfd(writer, width, height, bytesPerRow, headerSize, imageDataSize, ifdOffset);
    }

    private static void WriteImageData(BinaryWriter writer, long width, long height, 
        long bytesPerRow, Func<(long row, long col), bool> paint)
    {
        // Use pooled buffer for row data
        int bufferSize = (int)Math.Min(bytesPerRow, int.MaxValue);
        byte[] rowBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        
        try
        {
            for (long row = 0; row < height; row++)
            {
                Array.Clear(rowBuffer, 0, bufferSize);
                
                for (long col = 0; col < width; col++)
                {
                    bool isWhite = paint((row, col));
                    if (isWhite)
                    {
                        // Set bit (MSB first within each byte)
                        int byteIndex = (int)(col / 8);
                        int bitIndex = 7 - (int)(col % 8);
                        rowBuffer[byteIndex] |= (byte)(1 << bitIndex);
                    }
                }
                
                writer.Write(rowBuffer, 0, (int)bytesPerRow);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rowBuffer);
        }
    }

    private static void WriteClassicIfd(BinaryWriter writer, uint width, uint height, 
        uint bytesPerRow, long dataOffset, long imageDataSize)
    {
        const ushort tagCount = 11;
        writer.Write(tagCount);

        long currentOffset = writer.BaseStream.Position + (tagCount * 12) + 4; // After IFD entries + next IFD pointer

        // Tag: ImageWidth
        WriteClassicTag(writer, TagImageWidth, TypeLong, 1, width);
        
        // Tag: ImageLength
        WriteClassicTag(writer, TagImageLength, TypeLong, 1, height);
        
        // Tag: BitsPerSample
        WriteClassicTag(writer, TagBitsPerSample, TypeShort, 1, 1);
        
        // Tag: Compression (1 = no compression)
        WriteClassicTag(writer, TagCompression, TypeShort, 1, 1);
        
        // Tag: PhotometricInterpretation (1 = black is zero, white is 1)
        WriteClassicTag(writer, TagPhotometricInterpretation, TypeShort, 1, 1);
        
        // Tag: StripOffsets (single strip starting at dataOffset)
        WriteClassicTag(writer, TagStripOffsets, TypeLong, 1, (uint)dataOffset);
        
        // Tag: RowsPerStrip (all rows in one strip)
        WriteClassicTag(writer, TagRowsPerStrip, TypeLong, 1, height);
        
        // Tag: StripByteCounts
        WriteClassicTag(writer, TagStripByteCounts, TypeLong, 1, (uint)imageDataSize);
        
        // Tag: XResolution - will write rational after IFD
        long xResOffset = currentOffset;
        WriteClassicTagOffset(writer, TagXResolution, TypeRational, 1, (uint)xResOffset);
        currentOffset += 8;
        
        // Tag: YResolution
        long yResOffset = currentOffset;
        WriteClassicTagOffset(writer, TagYResolution, TypeRational, 1, (uint)yResOffset);
        currentOffset += 8;
        
        // Tag: ResolutionUnit (2 = inches)
        WriteClassicTag(writer, TagResolutionUnit, TypeShort, 1, 2);

        // Next IFD offset (0 = no more IFDs)
        writer.Write((uint)0);

        // Write resolution values (72 DPI as rational: 72/1)
        writer.Write((uint)72);
        writer.Write((uint)1);
        writer.Write((uint)72);
        writer.Write((uint)1);
    }

    private static void WriteBigTiffIfd(BinaryWriter writer, long width, long height, 
        long bytesPerRow, long dataOffset, long imageDataSize, long ifdOffset)
    {
        const ulong tagCount = 11;
        writer.Write(tagCount);

        long currentOffset = writer.BaseStream.Position + ((int)tagCount * 20) + 8; // After IFD entries + next IFD pointer

        // Tag: ImageWidth
        WriteBigTiffTag(writer, TagImageWidth, TypeLong8, 1, (ulong)width);
        
        // Tag: ImageLength
        WriteBigTiffTag(writer, TagImageLength, TypeLong8, 1, (ulong)height);
        
        // Tag: BitsPerSample
        WriteBigTiffTag(writer, TagBitsPerSample, TypeShort, 1, 1);
        
        // Tag: Compression
        WriteBigTiffTag(writer, TagCompression, TypeShort, 1, 1);
        
        // Tag: PhotometricInterpretation
        WriteBigTiffTag(writer, TagPhotometricInterpretation, TypeShort, 1, 1);
        
        // Tag: StripOffsets
        WriteBigTiffTag(writer, TagStripOffsets, TypeLong8, 1, (ulong)dataOffset);
        
        // Tag: RowsPerStrip
        WriteBigTiffTag(writer, TagRowsPerStrip, TypeLong8, 1, (ulong)height);
        
        // Tag: StripByteCounts
        WriteBigTiffTag(writer, TagStripByteCounts, TypeLong8, 1, (ulong)imageDataSize);
        
        // Tag: XResolution
        long xResOffset = currentOffset;
        WriteBigTiffTagOffset(writer, TagXResolution, TypeRational, 1, (ulong)xResOffset);
        currentOffset += 8;
        
        // Tag: YResolution
        long yResOffset = currentOffset;
        WriteBigTiffTagOffset(writer, TagYResolution, TypeRational, 1, (ulong)yResOffset);
        currentOffset += 8;
        
        // Tag: ResolutionUnit
        WriteBigTiffTag(writer, TagResolutionUnit, TypeShort, 1, 2);

        // Next IFD offset (0 = no more IFDs)
        writer.Write((ulong)0);

        // Write resolution values
        writer.Write((uint)72);
        writer.Write((uint)1);
        writer.Write((uint)72);
        writer.Write((uint)1);
    }

    private static void WriteClassicTag(BinaryWriter writer, ushort tagId, ushort type, 
        uint count, uint value)
    {
        writer.Write(tagId);
        writer.Write(type);
        writer.Write(count);
        writer.Write(value);
    }

    private static void WriteClassicTagOffset(BinaryWriter writer, ushort tagId, ushort type, 
        uint count, uint offset)
    {
        writer.Write(tagId);
        writer.Write(type);
        writer.Write(count);
        writer.Write(offset);
    }

    private static void WriteBigTiffTag(BinaryWriter writer, ushort tagId, ushort type, 
        ulong count, ulong value)
    {
        writer.Write(tagId);
        writer.Write(type);
        writer.Write(count);
        writer.Write(value);
    }

    private static void WriteBigTiffTagOffset(BinaryWriter writer, ushort tagId, ushort type, 
        ulong count, ulong offset)
    {
        writer.Write(tagId);
        writer.Write(type);
        writer.Write(count);
        writer.Write(offset);
    }
}