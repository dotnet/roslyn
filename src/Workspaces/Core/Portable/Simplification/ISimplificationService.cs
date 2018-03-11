// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal interface ISimplificationService : ILanguageService
    {
        SyntaxNode Expand(
            SyntaxNode node,
            SemanticModel semanticModel,
            SyntaxAnnotation annotationForReplacedAliasIdentifier,
            Func<SyntaxNode, bool> expandInsideNode,
            bool expandParameter,
            CancellationToken cancellationToken);

        SyntaxToken Expand(
            SyntaxToken token,
            SemanticModel semanticModel,
            Func<SyntaxNode, bool> expandInsideNode,
            CancellationToken cancellationToken);

        Task<Document> ReduceAsync(
            Document document,
            ImmutableArray<TextSpan> spans,
            OptionSet optionSet = null,
            ImmutableArray<AbstractReducer> reducers = default,
            CancellationToken cancellationToken = default);
    }
}
