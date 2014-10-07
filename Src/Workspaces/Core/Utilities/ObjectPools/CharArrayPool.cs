using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Used to reduce the # of temporary char[]s created to satisfy serialization and
    /// other I/O requests
    /// </summary>
    internal class CharArrayPool : ObjectPool<char[]>
    {
        public static readonly CharArrayPool Instance = new CharArrayPool();

        // 32 KB of pooled buffers
        public const int BufferSize = 32 * 1024;
        private const int BufferCount = 1;

        private CharArrayPool() : base(() => new char[BufferSize], BufferCount)
        {
        }
    }
}