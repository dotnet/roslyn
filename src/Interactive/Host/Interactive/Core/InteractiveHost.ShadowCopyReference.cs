// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class InteractiveHost
    {
        /// <summary>
        /// Specialize <see cref="PortableExecutableReference"/> with path being the original path of the copy.
        /// Logically this reference represents that file, the fact that we load the image from a copy is an implementation detail.
        /// </summary>
        private sealed class ShadowCopyReference : PortableExecutableReference
        {
            private readonly MetadataShadowCopyProvider _provider;

            public ShadowCopyReference(MetadataShadowCopyProvider provider, string originalPath, MetadataReferenceProperties properties)
                : base(properties, originalPath)
            {
                _provider = provider;
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                // TODO (tomat): use file next to the dll (or shadow copy)
                return DocumentationProvider.Default;
            }

            protected override Metadata GetMetadataImpl()
            {
                return _provider.GetMetadata(FilePath, Properties.Kind);
            }

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            {
                return new ShadowCopyReference(_provider, FilePath!, properties);
            }
        }
    }
}
