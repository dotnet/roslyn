﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an in-memory Portable-Executable image.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal sealed class MetadataImageReference : PortableExecutableReference
    {
        private readonly string _display;
        private readonly Metadata _metadata;

        internal MetadataImageReference(Metadata metadata, MetadataReferenceProperties properties, DocumentationProvider documentation, string filePath, string display)
            : base(properties, filePath, documentation ?? DocumentationProvider.Default)
        {
            _display = display;
            _metadata = metadata;
        }

        protected override Metadata GetMetadataImpl()
        {
            return _metadata;
        }

        protected override DocumentationProvider CreateDocumentationProvider()
        {
            // documentation provider is initialized in the constructor
            throw ExceptionUtilities.Unreachable;
        }

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
        {
            return new MetadataImageReference(
                _metadata,
                properties,
                this.DocumentationProvider,
                this.FilePath,
                _display);
        }

        public override string Display
        {
            get
            {
                return _display ?? FilePath ?? (Properties.Kind == MetadataImageKind.Assembly ? CodeAnalysisResources.InMemoryAssembly : CodeAnalysisResources.InMemoryModule);
            }
        }
    }
}
