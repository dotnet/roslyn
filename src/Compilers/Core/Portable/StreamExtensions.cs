using System.IO;

namespace Microsoft.CodeAnalysis
{
    internal static class StreamExtensions
    {
        public static void ReadAll(
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
                    throw new EndOfStreamException("Reached end of stream before end of read.");
                }
            }
        }
    }
}
