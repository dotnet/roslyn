// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Interactive
{
    partial class InteractiveHost
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
                Debug.Assert(originalPath != null);
                Debug.Assert(provider != null);

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
                return new ShadowCopyReference(_provider, this.FilePath, properties);
            }
        }
    }
}
