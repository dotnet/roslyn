// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class JsonPatternTokenClassifier : AbstractSyntaxClassifier
    {
        public override ImmutableArray<int> SyntaxTokenKinds { get; } = ImmutableArray.Create((int)SyntaxKind.StringLiteralToken);

        public override void AddClassifications(Workspace workspace, SyntaxToken token, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            Debug.Assert(token.Kind() == SyntaxKind.StringLiteralToken);
            CommonJsonPatternTokenClassifier.AddClassifications(
                workspace, token, semanticModel, result,
                CSharpSyntaxFactsService.Instance,
                CSharpSemanticFactsService.Instance,
                CSharpVirtualCharService.Instance,
                cancellationToken);
        }
    }
}
