using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public class TestMetadataReference : PortableExecutableReference
    {
        private readonly Metadata metadata;
        private readonly string display;

        public TestMetadataReference(Metadata metadata = null, string fullPath = null, string display = null)
            : base(MetadataReferenceProperties.Assembly, fullPath)
        {
            this.metadata = metadata;
            this.display = display;
        }

        public override string Display
        {
            get
            {
                return display;
            }
        }

        protected override DocumentationProvider CreateDocumentationProvider()
        {
            throw new NotImplementedException();
        }

        protected override Metadata GetMetadataImpl()
        {
            if (metadata == null)
            {
                throw new FileNotFoundException();
            }

            return metadata;
        }

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
        {
            throw new NotImplementedException();
        }
    }

    public class TestImageReference : PortableExecutableReference
    {
        private readonly ImmutableArray<byte> metadataBytes;
        private readonly string display;

        public TestImageReference(byte[] metadataBytes, string display)
            : this(ImmutableArray.Create(metadataBytes), display)
        {
        }

        public TestImageReference(ImmutableArray<byte> metadataBytes, string display)
            : base(MetadataReferenceProperties.Assembly)
        {
            this.metadataBytes = metadataBytes;
            this.display = display;
        }

        public override string Display
        {
            get
            {
                return display;
            }
        }

        protected override DocumentationProvider CreateDocumentationProvider()
        {
            throw new NotImplementedException();
        }

        protected override Metadata GetMetadataImpl()
        {
            return AssemblyMetadata.CreateFromImage(metadataBytes);
        }

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
        {
            throw new NotImplementedException();
        }
    }
}
