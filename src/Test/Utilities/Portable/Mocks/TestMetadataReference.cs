// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public class TestMetadataReference : PortableExecutableReference
    {
        private readonly Metadata _metadata;
        private readonly string _display;

        public TestMetadataReference(Metadata metadata = null, string fullPath = null, string display = null)
            : base(MetadataReferenceProperties.Assembly, fullPath)
        {
            _metadata = metadata;
            _display = display;
        }

        public override string Display
        {
            get
            {
                return _display;
            }
        }

        protected override DocumentationProvider CreateDocumentationProvider()
        {
            throw new NotImplementedException();
        }

        protected override Metadata GetMetadataImpl()
        {
            if (_metadata == null)
            {
                throw new FileNotFoundException();
            }

            return _metadata;
        }

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
        {
            throw new NotImplementedException();
        }
    }

    public class TestImageReference : PortableExecutableReference
    {
        private readonly ImmutableArray<byte> _metadataBytes;
        private readonly string _display;

        public TestImageReference(byte[] metadataBytes, string display)
            : this(ImmutableArray.Create(metadataBytes), display)
        {
        }

        public TestImageReference(ImmutableArray<byte> metadataBytes, string display)
            : base(MetadataReferenceProperties.Assembly)
        {
            _metadataBytes = metadataBytes;
            _display = display;
        }

        public override string Display
        {
            get
            {
                return _display;
            }
        }

        protected override DocumentationProvider CreateDocumentationProvider()
        {
            throw new NotImplementedException();
        }

        protected override Metadata GetMetadataImpl()
        {
            return AssemblyMetadata.CreateFromImage(_metadataBytes);
        }

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
        {
            throw new NotImplementedException();
        }
    }
}
