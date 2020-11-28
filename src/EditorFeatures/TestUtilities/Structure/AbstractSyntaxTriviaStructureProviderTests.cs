// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    public abstract class AbstractSyntaxTriviaStructureProviderTests : AbstractSyntaxStructureProviderTests
    {
        internal abstract AbstractSyntaxStructureProvider CreateProvider();

        internal sealed override async Task<ImmutableArray<BlockSpan>> GetBlockSpansWorkerAsync(Document document, int position)
        {
            var root = await document.GetSyntaxRootAsync();
            var trivia = root.FindTrivia(position, findInsideTrivia: true);

            var outliner = CreateProvider();
            var actualRegions = ArrayBuilder<BlockSpan>.GetInstance();
            var optionProvider = new BlockStructureOptionProvider(
                document.Project.Solution.Options,
                isMetadataAsSource: document.Project.Solution.Workspace.Kind == CodeAnalysis.WorkspaceKind.MetadataAsSource);
            outliner.CollectBlockSpans(trivia, actualRegions, optionProvider, CancellationToken.None);

            // TODO: Determine why we get null outlining spans.
            return actualRegions.ToImmutableAndFree();
        }
    }
}
