using System.IO;

namespace Microsoft.CodeAnalysis
{
    internal static class StreamExtensions
    {
        /// <summary>
        /// Attempts to read all of the requested bytes from the stream into the buffer
        /// </summary>
        /// <returns>
        /// The number of bytes read. Less than <paramref name="count" /> will
        /// only be returned if the end of stream is reached before all bytes can be read.
        /// </returns>
        public static int TryReadAll(
            this Stream stream,
            byte[] buffer,
            int offset,
            int count)
        {
            int bytesRead = 0;
            for (int totalBytesRead = 0; totalBytesRead < count; totalBytesRead += bytesRead)
            {
                bytesRead = stream.Read(buffer,
                                        offset + totalBytesRead,
                                        count - totalBytesRead);
                if (bytesRead == 0)
                {
                    return totalBytesRead;
                }
            }
            return count;
        }
    }
}
