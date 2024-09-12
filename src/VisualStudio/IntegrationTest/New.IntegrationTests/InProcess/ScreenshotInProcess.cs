// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Extensibility.Testing;
using Roslyn.Utilities;
using Xunit.Harness;
using Cursor = System.Windows.Forms.Cursor;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Screen = System.Windows.Forms.Screen;
using Size = System.Drawing.Size;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

/// <summary>
/// Test service to support capturing animated screenshots of integration tests (APNG format).
/// </summary>
/// <seealso href="https://www.w3.org/TR/png">Portable Network Graphics (PNG) Specification (Third Edition)</seealso>
[TestService]
internal partial class ScreenshotInProcess
{
    private static readonly SharedStopwatch s_timer = SharedStopwatch.StartNew();

    /// <summary>
    /// Frames captured for the current test. The elapsed time field is a timestamp relative to a fixed but unspecified
    /// point in time.
    /// </summary>
    /// <remarks>
    /// Lock on this object before accessing to prevent concurrent accesses.
    /// </remarks>
    private static readonly List<(TimeSpan elapsed, BitmapSource image)> s_frames = [];
    private static readonly System.Buffers.ArrayPool<byte> s_pool = System.Buffers.ArrayPool<byte>.Shared;
    private static ScreenshotInProcess? s_currentInstance;

    private static readonly ReadOnlyMemory<byte> s_pngHeader = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', (byte)'\r', (byte)'\n', 0x1A, (byte)'\n' };

    private static ReadOnlySpan<byte> Ihdr => "IHDR"u8;
    private static ReadOnlySpan<byte> Idat => "IDAT"u8;
    private static ReadOnlySpan<byte> Iend => "IEND"u8;
    private static ReadOnlySpan<byte> Srgb => "sRGB"u8;
    private static ReadOnlySpan<byte> Gama => "gAMA"u8;
    private static ReadOnlySpan<byte> Phys => "pHYs"u8;
    private static ReadOnlySpan<byte> Actl => "acTL"u8;
    private static ReadOnlySpan<byte> Fctl => "fcTL"u8;
    private static ReadOnlySpan<byte> Fdat => "fdAT"u8;

    private enum PngColorType : byte
    {
        Grayscale = 0,
        TrueColor = 2,
        Indexed = 3,
        GrayscaleWithAlpha = 4,
        TrueColorWithAlpha = 6,
    }

    private enum PngCompressionMethod : byte
    {
        /// <summary>
        /// <see href="https://www.w3.org/TR/png/#dfn-deflate">deflate</see> compression with a sliding window of at
        /// most 32768 bytes.
        /// </summary>
        Deflate = 0,
    }

    private enum PngFilterMethod : byte
    {
        /// <summary>
        /// Adaptive filtering with five basic filter types.
        /// </summary>
        Adaptive = 0,
    }

    private enum PngInterlaceMethod : byte
    {
        /// <summary>
        /// No interlacing.
        /// </summary>
        None = 0,

        /// <summary>
        /// Adam7 interlace.
        /// </summary>
        Adam7 = 1,
    }

    private enum ApngDisposeOp : byte
    {
        /// <summary>
        /// No disposal is done on this frame before rendering the next; the contents of the output buffer are left
        /// as-is.
        /// </summary>
        None = 0,

        /// <summary>
        /// The frame's region of the output buffer is to be cleared to fully transparent black before rendering the
        /// next frame.
        /// </summary>
        Background = 1,

        /// <summary>
        /// The frame's region of the output buffer is to be reverted to the previous contents before rendering the next
        /// frame.
        /// </summary>
        Previous = 2,
    }

    private enum ApngBlendOp : byte
    {
        /// <summary>
        /// All color components of the frame, including alpha, overwrite the current contents of the frame's output
        /// buffer region.
        /// </summary>
        Source = 0,

        /// <summary>
        /// The frame should be composited onto the output buffer based on its alpha, using a simple OVER operation as
        /// described in <see href="https://www.w3.org/TR/png/#13Alpha-channel-processing">Alpha Channel Processing</see>.
        /// </summary>
        Over = 1,
    }

    static ScreenshotInProcess()
    {
        DataCollectionService.RegisterCustomLogger(
            static fullPath =>
            {
                lock (s_frames)
                {
                    if (s_frames.Count == 0)
                        return;
                }

                // Try to capture an additional frame at the end
                s_currentInstance?.CaptureFrame();

                (TimeSpan elapsed, BitmapSource image)[] frames;
                lock (s_frames)
                {
                    if (s_frames.Count < 2)
                    {
                        // No animation available
                        return;
                    }

                    frames = s_frames.ToArray();
                }

                // Make sure the frames are processed in order of their timestamps
                Array.Sort(frames, (x, y) => x.elapsed.CompareTo(y.elapsed));
                var croppedFrames = DetectChangedRegions(frames);

                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    var crc = new Crc32();
                    var buffer = s_pool.Rent(4096);
                    try
                    {
                        var firstFrame = croppedFrames[0];
                        var firstEncoded = EncodeFrame(firstFrame.image);

                        // PNG Signature (8 bytes)
                        WritePngSignature(fileStream, buffer);

                        // IHDR
                        Write(fileStream, buffer, crc: null, firstEncoded.ihdr.Span);

                        // acTL
                        WriteActl(fileStream, buffer, crc, croppedFrames.Length, playCount: 1);

                        // Write the first frame data as IDAT
                        WriteFctl(fileStream, buffer, crc, sequenceNumber: 0, size: new Size(firstFrame.image.PixelWidth, firstFrame.image.PixelHeight), offset: firstFrame.offset, delay: TimeSpan.Zero, ApngDisposeOp.None, ApngBlendOp.Source);
                        foreach (var idat in firstEncoded.idat)
                        {
                            Write(fileStream, buffer, crc: null, idat.Span);
                        }

                        // Write the remaining frames as fDAT
                        var sequenceNumber = 1;
                        for (var i = 1; i < croppedFrames.Length; i++)
                        {
                            var elapsed = croppedFrames[i].elapsed - croppedFrames[i - 1].elapsed;
                            WriteFrame(fileStream, buffer, crc, ref sequenceNumber, croppedFrames[i].image, croppedFrames[i].offset, elapsed);
                        }

                        WriteIend(fileStream, buffer, crc);
                    }
                    finally
                    {
                        s_pool.Return(buffer);
                    }
                }
            },
            "",
            "apng");
    }

    private static (TimeSpan elapsed, BitmapSource image, Size offset)[] DetectChangedRegions((TimeSpan elapsed, BitmapSource image)[] frames)
    {
        var width = frames[0].image.PixelWidth;
        var height = frames[0].image.PixelHeight;

        const int BytesPerPixel = 4;
        var stride = width * BytesPerPixel;
        var totalImagePixels = width * height;
        var totalImageBytes = totalImagePixels * BytesPerPixel;

        List<(TimeSpan elapsed, BitmapSource image, Size offset)> resultFrames = new(frames.Length);
        const int BufferFrameCount = 2;
        var imageBuffer = Marshal.AllocHGlobal(BufferFrameCount * totalImageBytes);
        try
        {
            // Even frame indexes go into the first half of the image buffer. Odd frame indexes go into the second half.
            Contract.ThrowIfFalse(frames[0].image.Format.BitsPerPixel == BytesPerPixel * 8);
            frames[0].image.CopyPixels(Int32Rect.Empty, imageBuffer, totalImageBytes, stride);
            resultFrames.Add((frames[0].elapsed, frames[0].image, offset: Size.Empty));

            for (var i = 1; i < frames.Length; i++)
            {
                Contract.ThrowIfFalse(frames[i].image.PixelWidth == width);
                Contract.ThrowIfFalse(frames[i].image.PixelHeight == height);
                Contract.ThrowIfFalse(frames[i].image.Format.BitsPerPixel == BytesPerPixel * 8);

                var previousFrameBufferOffset = ((i - 1) % 2) * totalImageBytes;
                var currentFrameBufferOffset = (i % 2) * totalImageBytes;
                frames[i].image.CopyPixels(Int32Rect.Empty, IntPtr.Add(imageBuffer, currentFrameBufferOffset), totalImageBytes, stride);

                ReadOnlySpan<uint> previousImageData;
                ReadOnlySpan<uint> currentImageData;
                unsafe
                {
                    previousImageData = new ReadOnlySpan<uint>((void*)IntPtr.Add(imageBuffer, previousFrameBufferOffset), totalImagePixels);
                    currentImageData = new ReadOnlySpan<uint>((void*)IntPtr.Add(imageBuffer, currentFrameBufferOffset), totalImagePixels);
                }

                var firstChangedLine = -1;
                var lastChangedLine = -1;
                var firstChangedColumn = -1;
                var lastChangedColumn = -1;
                for (var line = 0; line < height; line++)
                {
                    var previousFrameLine = previousImageData.Slice(line * width, width);
                    var currentFrameLine = currentImageData.Slice(line * width, width);
                    for (var column = 0; column < previousFrameLine.Length; column++)
                    {
                        if (previousFrameLine[column] != currentFrameLine[column])
                        {
                            if (firstChangedLine == -1)
                                firstChangedLine = line;

                            lastChangedLine = line;
                            if (firstChangedColumn == -1 || column < firstChangedColumn)
                                firstChangedColumn = column;
                            lastChangedColumn = Math.Max(lastChangedColumn, column);
                            break;
                        }
                    }

                    for (var column = previousFrameLine.Length - 1; column > lastChangedColumn; column--)
                    {
                        if (previousFrameLine[column] != currentFrameLine[column])
                        {
                            lastChangedColumn = column;
                            break;
                        }
                    }
                }

                if (firstChangedLine == -1)
                {
                    // This image is identical to the previous one and can be skipped
                    continue;
                }
                else if (firstChangedLine == 0 && firstChangedColumn == 0 && lastChangedLine == height - 1 && lastChangedColumn == width - 1)
                {
                    // This image does not need to be cropped
                    resultFrames.Add((frames[i].elapsed, frames[i].image, Size.Empty));
                }
                else
                {
                    var offset = new Size(firstChangedColumn, firstChangedLine);
                    var croppedSource = new CroppedBitmap(frames[i].image, new Int32Rect(firstChangedColumn, firstChangedLine, lastChangedColumn - firstChangedColumn + 1, lastChangedLine - firstChangedLine + 1));
                    resultFrames.Add((frames[i].elapsed, croppedSource, offset));
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(imageBuffer);
        }

        return resultFrames.ToArray();
    }

    private static void WritePngSignature(Stream stream, byte[] buffer)
    {
        Write(stream, buffer, crc: null, s_pngHeader.Span);
    }

    private static void WriteIend(Stream stream, byte[] buffer, Crc32 crc)
    {
        crc.Reset();

        WriteChunkHeader(stream, buffer, crc, Iend, dataLength: 0);

        WriteCrc(stream, buffer, crc);
    }

    private static void WriteActl(Stream stream, byte[] buffer, Crc32 crc, int frameCount, int playCount)
    {
        crc.Reset();

        WriteChunkHeader(stream, buffer, crc, Actl, 8);

        // num_frames (4 bytes)
        WritePngUInt32(stream, buffer, crc, checked((uint)frameCount));
        // num_plays (4 bytes)
        WritePngUInt32(stream, buffer, crc, checked((uint)playCount));

        WriteCrc(stream, buffer, crc);
    }

    private static void WriteFrame(Stream stream, byte[] buffer, Crc32 crc, ref int sequenceNumber, BitmapSource frame, Size offset, TimeSpan delay)
    {
        WriteFctl(stream, buffer, crc, sequenceNumber++, size: new Size(frame.PixelWidth, frame.PixelHeight), offset: offset, delay, ApngDisposeOp.None, ApngBlendOp.Source);

        var (_, _, idats, _) = EncodeFrame(frame);
        foreach (var idat in idats)
        {
            WriteFdat(stream, buffer, crc, sequenceNumber++, idat.Span[8..^4]);
        }
    }

    private static void WriteFdat(Stream stream, byte[] buffer, Crc32 crc, int sequenceNumber, ReadOnlySpan<byte> data)
    {
        crc.Reset();

        WriteChunkHeader(stream, buffer, crc, Fdat, (uint)(data.Length + 4));

        // fdAT is sequence number followed by IDAT
        WritePngUInt32(stream, buffer, crc, (uint)sequenceNumber);
        Write(stream, buffer, crc, data);

        WriteCrc(stream, buffer, crc);
    }

    private static void WriteChunkHeader(Stream stream, byte[] buffer, Crc32 crc, ReadOnlySpan<byte> chunkType, uint dataLength)
    {
        WriteChunkDataLength(stream, buffer, dataLength);
        Write(stream, buffer, crc, chunkType);
    }

    private static void WriteChunkDataLength(Stream stream, byte[] buffer, uint dataLength)
    {
        WritePngUInt32(stream, buffer, crc: null, dataLength);
    }

    private static void WriteCrc(Stream stream, byte[] buffer, Crc32 crc)
    {
        WritePngUInt32(stream, buffer, crc: null, crc.GetCurrentHashAsUInt32());
    }

    private static (ReadOnlyMemory<byte> signature, ReadOnlyMemory<byte> ihdr, ImmutableArray<ReadOnlyMemory<byte>> idat, ReadOnlyMemory<byte> iend) EncodeFrame(BitmapSource frame)
    {
        using var stream = new MemoryStream();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(frame));
        encoder.Save(stream);

        var memory = stream.GetBuffer().AsMemory()[..(int)stream.Length];
        var signature = memory[..8];
        if (!signature.Span.SequenceEqual(s_pngHeader.Span))
            throw new InvalidOperationException();

        var ihdr = ReadOnlyMemory<byte>.Empty;
        List<ReadOnlyMemory<byte>> idat = [];
        var iend = ReadOnlyMemory<byte>.Empty;
        for (var remaining = memory[signature.Length..]; !remaining.IsEmpty; remaining = remaining[GetChunkLength(remaining)..])
        {
            var chunk = remaining[..GetChunkLength(remaining)];
            var chunkType = GetChunkType(chunk);
            if (chunkType.Span.SequenceEqual(Ihdr))
            {
                Contract.ThrowIfFalse(ihdr.IsEmpty);
                ihdr = chunk;
            }
            else if (chunkType.Span.SequenceEqual(Srgb)
                || chunkType.Span.SequenceEqual(Gama)
                || chunkType.Span.SequenceEqual(Phys))
            {
                // These are expected chunks in the PNG, but not needed for the final APNG
                continue;
            }
            else if (chunkType.Span.SequenceEqual(Idat))
            {
                idat.Add(chunk);
            }
            else if (chunkType.Span.SequenceEqual(Iend))
            {
                Contract.ThrowIfFalse(iend.IsEmpty);
                iend = chunk;
            }
            else
            {
                var type = Encoding.ASCII.GetString(chunkType.ToArray());
                throw new NotSupportedException($"Chunk \"{type}\" is not supported.");
            }
        }

        Contract.ThrowIfTrue(ihdr.IsEmpty);
        Contract.ThrowIfTrue(idat.Count == 0);
        Contract.ThrowIfTrue(iend.IsEmpty);

        return (signature, ihdr, idat.ToImmutableArrayOrEmpty(), iend);

        static ReadOnlyMemory<byte> GetChunkType(ReadOnlyMemory<byte> chunk)
            => chunk[4..8];

        static int GetDataLength(ReadOnlyMemory<byte> memory)
            => (int)BinaryPrimitives.ReadUInt32BigEndian(memory.Span);

        static int GetChunkLength(ReadOnlyMemory<byte> memory)
        {
            // Total chunk length = length field + chunk type + data length + crc
            return GetDataLength(memory) + 12;
        }
    }

    private static void WriteFctl(Stream stream, byte[] buffer, Crc32 crc, int sequenceNumber, Size size, Size offset, TimeSpan delay, ApngDisposeOp disposeOp, ApngBlendOp blendOp)
    {
        crc.Reset();

        WriteChunkHeader(stream, buffer, crc, Fctl, dataLength: 26);

        // sequence_number (4 bytes)
        WritePngUInt32(stream, buffer, crc, checked((uint)sequenceNumber));
        // width (4 bytes)
        WritePngUInt32(stream, buffer, crc, checked((uint)size.Width));
        // height (4 bytes)
        WritePngUInt32(stream, buffer, crc, checked((uint)size.Height));
        // x_offset (4 bytes)
        WritePngUInt32(stream, buffer, crc, checked((uint)offset.Width));
        // y_offset (4 bytes)
        WritePngUInt32(stream, buffer, crc, checked((uint)offset.Height));

        if (delay.TotalMilliseconds > ushort.MaxValue)
        {
            // Specify delay in 1/30 second (max allowed is a bit over 36 minutes)
            // delay_num (2 bytes)
            WritePngUInt16(stream, buffer, crc, checked((ushort)(delay.TotalMilliseconds / 100)));
            // delay_den (2 bytes)
            WritePngUInt16(stream, buffer, crc, 10);
        }
        else
        {
            // Specify delay in 1/1000 second
            // delay_num (2 bytes)
            WritePngUInt16(stream, buffer, crc, checked((ushort)delay.TotalMilliseconds));
            // delay_den (2 bytes)
            WritePngUInt16(stream, buffer, crc, 1000);
        }

        // dispose_op (1 bytes)
        WritePngByte(stream, crc, (byte)disposeOp);

        // blend_op (1 bytes)
        WritePngByte(stream, crc, (byte)blendOp);

        WriteCrc(stream, buffer, crc);
    }

    private static void WritePngByte(Stream stream, Crc32? crc, byte value)
    {
        Span<byte> buffer = stackalloc byte[] { value };
        crc?.Append(buffer);
        stream.WriteByte(value);
    }

    private static void WritePngUInt16(Stream stream, byte[] buffer, Crc32? crc, ushort value)
    {
        Span<byte> encoded = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(encoded, value);
        Write(stream, buffer, crc, encoded);
    }

    private static void WritePngUInt32(Stream stream, byte[] buffer, Crc32? crc, uint value)
    {
        Span<byte> encoded = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(encoded, value);
        Write(stream, buffer, crc, encoded);
    }

    private static void Write(Stream stream, byte[] buffer, Crc32? crc, ReadOnlySpan<byte> bytes)
    {
        crc?.Append(bytes);

        if (bytes.Length < buffer.Length)
        {
            bytes.CopyTo(buffer);
            stream.Write(buffer, 0, bytes.Length);
        }
        else
        {
            for (var remaining = bytes; !remaining.IsEmpty; remaining = remaining[Math.Min(buffer.Length, remaining.Length)..])
            {
                var current = remaining[..Math.Min(buffer.Length, remaining.Length)];
                current.CopyTo(buffer);
                stream.Write(buffer, 0, current.Length);
            }
        }
    }

    protected override async Task InitializeCoreAsync()
    {
        // Release the previous instance, if any
        s_currentInstance?.UnregisterEvents();
        s_currentInstance = this;

        await base.InitializeCoreAsync();

        var elapsed = s_timer.Elapsed;
        if (TryCaptureFullScreen() is { } image)
        {
            lock (s_frames)
            {
                s_frames.Clear();
                s_frames.Add((elapsed, image));
            }
        }
        else
        {
            lock (s_frames)
            {
                s_frames.Clear();
            }
        }

        RegisterEvents();
    }

    public void RegisterEvents()
    {
        Application.Current.MainWindow.LayoutUpdated += OnThrottledInteraction;
        Application.Current.MainWindow.PreviewMouseMove += OnThrottledInteraction;
        Application.Current.MainWindow.PreviewMouseDown += OnInteraction;
        Application.Current.MainWindow.PreviewKeyDown += OnInteraction;
    }

    public void UnregisterEvents()
    {
        Application.Current.MainWindow.LayoutUpdated -= OnThrottledInteraction;
        Application.Current.MainWindow.PreviewMouseMove -= OnThrottledInteraction;
        Application.Current.MainWindow.PreviewMouseDown -= OnInteraction;
        Application.Current.MainWindow.PreviewKeyDown -= OnInteraction;
    }

    private void OnThrottledInteraction(object? sender, EventArgs e)
    {
        lock (s_frames)
        {
            // Avoid taking too many screenshots for layout updates
            if (s_frames.Count > 0 && (s_timer.Elapsed - s_frames[^1].elapsed) < TimeSpan.FromSeconds(0.5))
                return;
        }

        CaptureFrame();
    }

    private void OnInteraction(object? sender, EventArgs e)
    {
        CaptureFrame();
    }

    public void CaptureFrame()
    {
        var elapsed = s_timer.Elapsed;
        if (TryCaptureFullScreen() is { } image)
        {
            lock (s_frames)
            {
                s_frames.Add((elapsed, image));
            }
        }
    }

    /// <summary>
    /// Captures the full screen to a <see cref="Bitmap"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="Bitmap"/> containing the screen capture of the desktop, or <see langword="null"/> if a screen
    /// capture can't be created.
    /// </returns>
    private static BitmapSource? TryCaptureFullScreen()
    {
        var width = Screen.PrimaryScreen.Bounds.Width;
        var height = Screen.PrimaryScreen.Bounds.Height;

        if (width <= 0 || height <= 0)
        {
            // Don't try to take a screenshot if there is no screen.
            // This may not be an interactive session.
            return null;
        }

        using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                sourceX: Screen.PrimaryScreen.Bounds.X,
                sourceY: Screen.PrimaryScreen.Bounds.Y,
                destinationX: 0,
                destinationY: 0,
                blockRegionSize: bitmap.Size,
                copyPixelOperation: CopyPixelOperation.SourceCopy);

            if (Cursor.Current is { } cursor)
            {
                var bounds = new Rectangle(Cursor.Position - (Size)cursor.HotSpot, cursor.Size);
                cursor.Draw(graphics, bounds);
            }

            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                return BitmapSource.Create(
                    bitmapData.Width,
                    bitmapData.Height,
                    bitmap.HorizontalResolution,
                    bitmap.VerticalResolution,
                    PixelFormats.Bgra32,
                    null,
                    bitmapData.Scan0,
                    bitmapData.Stride * bitmapData.Height,
                    bitmapData.Stride);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
    }
}
