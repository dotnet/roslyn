// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly string display;
        private readonly Metadata metadata;

        internal MetadataImageReference(Metadata metadata, MetadataReferenceProperties properties, DocumentationProvider documentation, string filePath, string display)
            : base(properties, filePath, documentation ?? DocumentationProvider.Default)
        {
            this.display = display;
            this.metadata = metadata;
        }

        protected override Metadata GetMetadataImpl()
        {
            return metadata;
        }

        protected override DocumentationProvider CreateDocumentationProvider()
        {
            // documentation provider is initialized in the constructor
            throw ExceptionUtilities.Unreachable;
        }

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
        {
            return new MetadataImageReference(
                this.metadata,
                properties,
                this.DocumentationProvider,
                this.FilePath,
                this.display);
        }

        public override string Display
        {
            get
            {
                return display ?? FilePath ?? (Properties.Kind == MetadataImageKind.Assembly ? CodeAnalysisResources.InMemoryAssembly : CodeAnalysisResources.InMemoryModule);
            }
        }

        private string GetDebuggerDisplay()
        {
            var sb = new StringBuilder();
            sb.Append(Properties.Kind == MetadataImageKind.Module ? "Module" : "Assembly");
            if (!Properties.Aliases.IsDefaultOrEmpty)
            {
                sb.Append(" Aliases={");
                sb.Append(string.Join(", ", Properties.Aliases));
                sb.Append("}");
            }

            if (Properties.EmbedInteropTypes)
            {
                sb.Append(" Embed");
            }

            if (FilePath != null)
            {
                sb.Append(" Path='");
                sb.Append(FilePath);
                sb.Append("'");
            }

            if (display != null)
            {
                sb.Append(" Display='");
                sb.Append(display);
                sb.Append("'");
            }

            return sb.ToString();
        }
    }
}
