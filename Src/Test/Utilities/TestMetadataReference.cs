using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public class TestMetadataReference : PortableExecutableReference
    {
        public TestMetadataReference(string fullPath = null)
            : base(MetadataReferenceProperties.Assembly, fullPath)
        {
        }

        protected override DocumentationProvider CreateDocumentationProvider()
        {
            throw new NotImplementedException();
        }

        protected override Metadata GetMetadataImpl()
        {
            throw new FileNotFoundException();
        }
    }
}
