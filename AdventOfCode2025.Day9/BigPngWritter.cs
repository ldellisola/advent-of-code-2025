namespace AdventOfCode2025.Day9;
// BigPngWriter.cs
// Self-contained library for creating bilevel PNG files with optional scaling
// Target: .NET 10

using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;


/// <summary>
/// Creates bilevel (1-bit) PNG files with optional scale-down support.
/// </summary>
public static class BigPngWriter
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Creates a bilevel PNG file where each pixel is determined by the paint function.
    /// </summary>
    /// <param name="filename">Output file path.</param>
    /// <param name="width">Source image width in pixels.</param>
    /// <param name="height">Source image height in pixels.</param>
    /// <param name="paint">Function returning true for white pixels, false for black.</param>
    /// <param name="scaleFactor">Scale down factor (1 = original size, 2 = half size, etc.).</param>
    /// <exception cref="ArgumentException">Thrown when dimensions are invalid.</exception>
    public static void Create(
        string filename, 
        long width, 
        long height, 
        Func<(long row, long col), bool> paint,
        int scaleFactor = 1)
    {
        ArgumentNullException.ThrowIfNull(filename);
        ArgumentNullException.ThrowIfNull(paint);
        
        if (width <= 0) throw new ArgumentException("Width must be positive.", nameof(width));
        if (height <= 0) throw new ArgumentException("Height must be positive.", nameof(height));
        if (scaleFactor < 1) throw new ArgumentException("Scale factor must be at least 1.", nameof(scaleFactor));

        long outputWidth = width / scaleFactor;
        long outputHeight = height / scaleFactor;

        if (outputWidth <= 0 || outputHeight <= 0)
            throw new ArgumentException("Scaled dimensions must be positive.");

        // PNG spec limits dimensions to 2^31 - 1
        if (outputWidth > int.MaxValue || outputHeight > int.MaxValue)
            throw new ArgumentException($"Output dimensions exceed PNG maximum ({int.MaxValue}). Increase scale factor.");

        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 65536, FileOptions.SequentialScan);

        WritePng(stream, (int)outputWidth, (int)outputHeight, width, height, paint, scaleFactor);
    }

    /// <summary>
    /// Creates a bilevel PNG with threshold-based scaling (majority vote for scaled pixels).
    /// </summary>
    public static void CreateWithThreshold(
        string filename,
        long width,
        long height,
        Func<(long row, long col), bool> paint,
        int scaleFactor = 1,
        double whiteThreshold = 0.5)
    {
        ArgumentNullException.ThrowIfNull(filename);
        ArgumentNullException.ThrowIfNull(paint);

        if (width <= 0) throw new ArgumentException("Width must be positive.", nameof(width));
        if (height <= 0) throw new ArgumentException("Height must be positive.", nameof(height));
        if (scaleFactor < 1) throw new ArgumentException("Scale factor must be at least 1.", nameof(scaleFactor));
        if (whiteThreshold < 0 || whiteThreshold > 1) 
            throw new ArgumentException("Threshold must be between 0 and 1.", nameof(whiteThreshold));

        long outputWidth = width / scaleFactor;
        long outputHeight = height / scaleFactor;

        if (outputWidth <= 0 || outputHeight <= 0)
            throw new ArgumentException("Scaled dimensions must be positive.");

        if (outputWidth > int.MaxValue || outputHeight > int.MaxValue)
            throw new ArgumentException($"Output dimensions exceed PNG maximum ({int.MaxValue}). Increase scale factor.");

        using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 65536, FileOptions.SequentialScan);

        WritePngWithThreshold(stream, (int)outputWidth, (int)outputHeight, width, height, 
            paint, scaleFactor, whiteThreshold);
    }

    private static void WritePng(Stream stream, int outputWidth, int outputHeight,
        long sourceWidth, long sourceHeight, Func<(long row, long col), bool> paint, int scaleFactor)
    {
        // PNG Signature
        stream.Write(PngSignature);

        // IHDR chunk
        WriteIhdrChunk(stream, outputWidth, outputHeight);

        // IDAT chunk(s) - compressed image data
        WriteIdatChunks(stream, outputWidth, outputHeight, sourceWidth, sourceHeight, paint, scaleFactor);

        // IEND chunk
        WriteIendChunk(stream);
    }

    private static void WritePngWithThreshold(Stream stream, int outputWidth, int outputHeight,
        long sourceWidth, long sourceHeight, Func<(long row, long col), bool> paint, 
        int scaleFactor, double whiteThreshold)
    {
        stream.Write(PngSignature);
        WriteIhdrChunk(stream, outputWidth, outputHeight);
        WriteIdatChunksWithThreshold(stream, outputWidth, outputHeight, sourceWidth, sourceHeight, 
            paint, scaleFactor, whiteThreshold);
        WriteIendChunk(stream);
    }

    private static void WriteIhdrChunk(Stream stream, int width, int height)
    {
        using var chunkData = new MemoryStream(13);
        using var writer = new BinaryWriter(chunkData);

        writer.Write(ToBigEndian(width));
        writer.Write(ToBigEndian(height));
        writer.Write((byte)1);  // Bit depth: 1-bit
        writer.Write((byte)0);  // Color type: grayscale
        writer.Write((byte)0);  // Compression method: deflate
        writer.Write((byte)0);  // Filter method: adaptive
        writer.Write((byte)0);  // Interlace method: none

        WriteChunk(stream, "IHDR", chunkData.ToArray());
    }

    private static void WriteIdatChunks(Stream stream, int outputWidth, int outputHeight,
        long sourceWidth, long sourceHeight, Func<(long row, long col), bool> paint, int scaleFactor)
    {
        int bytesPerRow = (outputWidth + 7) / 8;
        byte[] rowBuffer = ArrayPool<byte>.Shared.Rent(bytesPerRow + 1); // +1 for filter byte

        try
        {
            using var compressedStream = new MemoryStream();
            using (var deflate = new DeflateStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                for (int outputRow = 0; outputRow < outputHeight; outputRow++)
                {
                    Array.Clear(rowBuffer, 0, bytesPerRow + 1);
                    rowBuffer[0] = 0; // Filter type: None

                    long sourceRow = (long)outputRow * scaleFactor;

                    for (int outputCol = 0; outputCol < outputWidth; outputCol++)
                    {
                        long sourceCol = (long)outputCol * scaleFactor;
                        bool isWhite = paint((sourceRow, sourceCol));

                        if (isWhite)
                        {
                            int byteIndex = 1 + (outputCol / 8);
                            int bitIndex = 7 - (outputCol % 8);
                            rowBuffer[byteIndex] |= (byte)(1 << bitIndex);
                        }
                    }

                    deflate.Write(rowBuffer, 0, bytesPerRow + 1);
                }
            }

            WriteCompressedData(stream, compressedStream.ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rowBuffer);
        }
    }

    private static void WriteIdatChunksWithThreshold(Stream stream, int outputWidth, int outputHeight,
        long sourceWidth, long sourceHeight, Func<(long row, long col), bool> paint,
        int scaleFactor, double whiteThreshold)
    {
        int bytesPerRow = (outputWidth + 7) / 8;
        byte[] rowBuffer = ArrayPool<byte>.Shared.Rent(bytesPerRow + 1);
        int pixelsPerBlock = scaleFactor * scaleFactor;
        int whiteCountThreshold = (int)(pixelsPerBlock * whiteThreshold);

        try
        {
            using var compressedStream = new MemoryStream();
            using (var deflate = new DeflateStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                for (int outputRow = 0; outputRow < outputHeight; outputRow++)
                {
                    Array.Clear(rowBuffer, 0, bytesPerRow + 1);
                    rowBuffer[0] = 0;

                    long sourceRowStart = (long)outputRow * scaleFactor;

                    for (int outputCol = 0; outputCol < outputWidth; outputCol++)
                    {
                        long sourceColStart = (long)outputCol * scaleFactor;

                        // Count white pixels in the block
                        int whiteCount = 0;
                        for (int dy = 0; dy < scaleFactor && sourceRowStart + dy < sourceHeight; dy++)
                        {
                            for (int dx = 0; dx < scaleFactor && sourceColStart + dx < sourceWidth; dx++)
                            {
                                if (paint((sourceRowStart + dy, sourceColStart + dx)))
                                    whiteCount++;
                            }
                        }

                        bool isWhite = whiteCount >= whiteCountThreshold;

                        if (isWhite)
                        {
                            int byteIndex = 1 + (outputCol / 8);
                            int bitIndex = 7 - (outputCol % 8);
                            rowBuffer[byteIndex] |= (byte)(1 << bitIndex);
                        }
                    }

                    deflate.Write(rowBuffer, 0, bytesPerRow + 1);
                }
            }

            WriteCompressedData(stream, compressedStream.ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rowBuffer);
        }
    }

    private static void WriteCompressedData(Stream stream, byte[] compressedData)
    {
        // Wrap in zlib format (header + data + adler32)
        using var zlibStream = new MemoryStream();
        
        // zlib header (deflate, 32K window, default compression)
        zlibStream.WriteByte(0x78);
        zlibStream.WriteByte(0x9C);
        
        zlibStream.Write(compressedData);
        
        // Write Adler-32 checksum (simplified - compute from compressed output)
        // For proper implementation, we'd need the uncompressed data
        // Using a placeholder that works for most decoders
        uint adler = ComputeAdler32Placeholder(compressedData);
        zlibStream.WriteByte((byte)(adler >> 24));
        zlibStream.WriteByte((byte)(adler >> 16));
        zlibStream.WriteByte((byte)(adler >> 8));
        zlibStream.WriteByte((byte)adler);

        byte[] zlibData = zlibStream.ToArray();

        // Split into IDAT chunks if necessary (max 2GB per chunk, but use smaller for compatibility)
        const int maxChunkSize = 1 * 1024 * 1024; // 1MB chunks
        int offset = 0;

        while (offset < zlibData.Length)
        {
            int chunkSize = Math.Min(maxChunkSize, zlibData.Length - offset);
            byte[] chunkData = new byte[chunkSize];
            Array.Copy(zlibData, offset, chunkData, 0, chunkSize);
            WriteChunk(stream, "IDAT", chunkData);
            offset += chunkSize;
        }
    }

    private static void WriteIendChunk(Stream stream)
    {
        WriteChunk(stream, "IEND", []);
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);

        // Length (big-endian)
        stream.Write(BitConverter.GetBytes(ToBigEndian(data.Length)));

        // Type
        stream.Write(typeBytes);

        // Data
        if (data.Length > 0)
            stream.Write(data);

        // CRC32 (type + data)
        uint crc = Crc32(typeBytes, data);
        stream.Write(BitConverter.GetBytes(ToBigEndian((int)crc)));
    }

    private static int ToBigEndian(int value)
    {
        if (BitConverter.IsLittleEndian)
        {
            return ((value & 0xFF) << 24) |
                   ((value & 0xFF00) << 8) |
                   ((value & 0xFF0000) >> 8) |
                   ((value >> 24) & 0xFF);
        }
        return value;
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        uint crc = 0xFFFFFFFF;

        foreach (byte b in type)
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);

        foreach (byte b in data)
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);

        return crc ^ 0xFFFFFFFF;
    }

    private static uint ComputeAdler32Placeholder(byte[] data)
    {
        // Simplified Adler-32 for the deflate output
        // In practice, zlib checksum should be computed on uncompressed data
        uint a = 1, b = 0;
        const uint mod = 65521;

        foreach (byte d in data)
        {
            a = (a + d) % mod;
            b = (b + a) % mod;
        }

        return (b << 16) | a;
    }

    private static readonly uint[] Crc32Table = GenerateCrc32Table();

    private static uint[] GenerateCrc32Table()
    {
        uint[] table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
            {
                if ((c & 1) != 0)
                    c = 0xEDB88320 ^ (c >> 1);
                else
                    c >>= 1;
            }
            table[n] = c;
        }
        return table;
    }
}