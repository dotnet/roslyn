using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace Roslyn.Compilers
{
    /// <summary>
    /// A trivial IMetadataDocumentationProvider which never returns documentation.
    /// </summary>
    internal sealed class NullDocumentationProvider : IMetadataDocumentationProvider
    {
        internal static readonly NullDocumentationProvider Instance = new NullDocumentationProvider();

        private NullDocumentationProvider()
        {
        }

        public DocumentationComment GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken token)
        {
            return DocumentationComment.Empty;
        }

        public bool Equals(IMetadataDocumentationProvider other)
        {
            return other == this;
        }
    }
}
