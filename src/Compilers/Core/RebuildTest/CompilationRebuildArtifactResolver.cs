// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    internal sealed class CompilationRebuildArtifactResolver : IRebuildArtifactResolver
    {
        internal Compilation Compilation { get; }

        public CompilationRebuildArtifactResolver(Compilation compilation)
        {
            Compilation = compilation;
        }

        public MetadataReference ResolveMetadataReference(MetadataReferenceInfo metadataReferenceInfo) =>
            Compilation
                .References
                .Single(x =>
                    x.GetModuleVersionId() == metadataReferenceInfo.ModuleVersionId &&
                    x.Properties.Aliases.SingleOrDefault() == metadataReferenceInfo.ExternAlias);

        public SourceText ResolveSourceText(SourceTextInfo sourceTextInfo) =>
            Compilation
                .SyntaxTrees
                .Select(x => x.GetText())
                .Single(x => x.GetChecksum().SequenceEqual(sourceTextInfo.Hash));

    }
}
