// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionState
{
    /// <summary>
    /// Special subclass so we can attach more data to the PEReference.
    /// </summary>
    private sealed class SkeletonPortableExecutableReference : PortableExecutableReference
    {
        private readonly AssemblyMetadata _metadata;
        private readonly string? _display;
        private readonly DocumentationProvider _documentationProvider;

        /// <summary>
        /// Tie lifetime of the underlying direct memory to this metadata reference.  This way the memory will not be
        /// GC'ed while this reference is alive.
        /// </summary>
        private readonly ISupportDirectMemoryAccess? _directMemoryAccess;

        public SkeletonPortableExecutableReference(
            AssemblyMetadata metadata,
            MetadataReferenceProperties properties,
            DocumentationProvider documentationProvider,
            string? display,
            ISupportDirectMemoryAccess? directMemoryAccess)
            : base(properties, fullPath: null, documentationProvider)
        {
            _metadata = metadata;
            _display = display;
            _documentationProvider = documentationProvider;
            _directMemoryAccess = directMemoryAccess;
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
            return new SkeletonPortableExecutableReference(
                _metadata,
                properties,
                _documentationProvider,
                _display,
                _directMemoryAccess);
        }

        public override string Display => _display ?? FilePath ?? "";
    }
}
