using System;
using System.IO;
using Roslyn.Compilers;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A class to represent text data that can be serialized w/o using strings.
    /// </summary>
    [Serializable]
    internal class SerializableText : SerializableData
    {
        private char[][] chunks;
        internal const int ChunkSize = 1024;

        public SerializableText(char[][] chunks)
        {
            this.chunks = chunks;
        }

        public static SerializableText From(IText text)
        {
            int n = text.Length;
            int chunkCount = (n + ChunkSize - 1) / ChunkSize;
            char[][] chunks = new char[chunkCount][];

            for (int i = 0, c = 0; i < n; i += ChunkSize, c++)
            {
                int count = Math.Min(ChunkSize, n - i);
                var chunk = new char[count];
                text.CopyTo(i, chunk, 0, count);
                chunks[c] = chunk;
            }

            return new SerializableText(chunks);
        }

        public TextReader ToTextReader()
        {
            return new Reader(this.chunks);
        }

        private class Reader : TextReader
        {
            private char[][] chunks;
            private int chunkIndex;
            private int chunkOffset;

            public Reader(char[][] chunks)
            {
                this.chunks = chunks;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                if (count <= 0)
                {
                    return 0;
                }

            top:
                if (chunkIndex >= chunks.Length)
                {
                    return 0;
                }

                var chunk = chunks[chunkIndex];

                if (chunkOffset >= chunk.Length)
                {
                    chunkIndex++;
                    chunkOffset = 0;
                    goto top;
                }

                int copyCount = Math.Min(chunk.Length - chunkOffset, count);
                Array.Copy(chunk, chunkOffset, buffer, index, copyCount);
                chunkOffset += copyCount;

                return copyCount;
            }
        }
    }
}