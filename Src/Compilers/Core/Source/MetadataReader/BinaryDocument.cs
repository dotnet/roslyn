namespace Roslyn.Compilers.MetadataReader
{
    /// <summary>
    /// Class representing a binary document
    /// </summary>
    internal sealed class BinaryDocument : IBinaryDocument
    {
        private uint length;
        private string location;

        /// <summary>
        /// Constructor for the Binary Document.
        /// </summary>
        public BinaryDocument(string location, uint length)
        {
            this.location = location;
            this.length = length;
        }

        uint IBinaryDocument.Length
        {
            get { return this.length; }
        }

        string IDocument.Location
        {
            get { return this.location; }
        }
    }
}