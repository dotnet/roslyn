// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface ISyntaxClassificationService : ILanguageService
    {
        ImmutableArray<ISyntaxClassifier> GetDefaultSyntaxClassifiers();

        void AddLexicalClassifications(SourceText text,
            TextSpan textSpan,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken);

        void AddSyntacticClassifications(SyntaxTree syntaxTree,
            TextSpan textSpan,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken);

        Task AddSemanticClassificationsAsync(Document document,
            TextSpan textSpan,
            Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
            Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken);

        void AddSemanticClassifications(
            SemanticModel semanticModel,
            TextSpan textSpan,
            Workspace workspace,
            Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
            Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken);

        ClassifiedSpan FixClassification(SourceText text, ClassifiedSpan classifiedSpan);
    }
}
