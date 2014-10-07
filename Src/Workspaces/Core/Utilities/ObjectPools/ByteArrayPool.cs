using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Used to reduce the # of temporary byte[]s created to satisfy serialization and
    /// other I/O requests
    /// </summary>
    internal class ByteArrayPool : ObjectPool<byte[]>
    {
        public static readonly ByteArrayPool Instance = new ByteArrayPool();

        // 128 KB of pooled buffers
        public const int BufferSize = 4 * 1024;
        private const int BufferCount = 32;

        private ByteArrayPool() : base(() => new byte[BufferSize], BufferCount)
        {
        }
    }
}