// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly string? _display;
        private readonly Metadata _metadata;

        internal MetadataImageReference(Metadata metadata, MetadataReferenceProperties properties, DocumentationProvider? documentation, string? filePath, string? display)
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
            throw ExceptionUtilities.Unreachable();
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

        private string GetDebuggerDisplay()
        {
            var sb = new StringBuilder();
            sb.Append(Properties.Kind == MetadataImageKind.Module ? "Module" : "Assembly");
            if (!Properties.Aliases.IsEmpty)
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

            if (_display != null)
            {
                sb.Append(" Display='");
                sb.Append(_display);
                sb.Append("'");
            }

            return sb.ToString();
        }
    }
}
