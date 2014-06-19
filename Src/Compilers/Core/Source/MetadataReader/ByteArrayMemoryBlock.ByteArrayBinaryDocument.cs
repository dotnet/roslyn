namespace Roslyn.Compilers.MetadataReader
{
    partial class ByteArrayMemoryBlock
    {
        private class ByteArrayBinaryDocument : IBinaryDocument
        {
            private readonly string uniqueName;
            private readonly byte[] bytes;

            public ByteArrayBinaryDocument(string uniqueName, byte[] bytes)
            {
                this.uniqueName = uniqueName;
                this.bytes = bytes;
            }

            public uint Length
            {
                get
                {
                    return checked((uint)bytes.Length);
                }
            }

            public string Location
            {
                get
                {
                    return uniqueName;
                }
            }
        }
    }
}