// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal interface ISimplificationService : ISimplification, ILanguageService
    {
        SyntaxNode Expand(
            SyntaxNode node,
            SemanticModel semanticModel,
            SyntaxAnnotation? annotationForReplacedAliasIdentifier,
            Func<SyntaxNode, bool>? expandInsideNode,
            bool expandParameter,
            CancellationToken cancellationToken);

        SyntaxToken Expand(
            SyntaxToken token,
            SemanticModel semanticModel,
            Func<SyntaxNode, bool>? expandInsideNode,
            CancellationToken cancellationToken);

        Task<Document> ReduceAsync(
            Document document,
            ImmutableArray<TextSpan> spans,
            SimplifierOptions options,
            ImmutableArray<AbstractReducer> reducers = default,
            CancellationToken cancellationToken = default);
    }
}
