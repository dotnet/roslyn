// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract class AbstractEmbeddedLanguageClassificationService : IEmbeddedLanguageClassificationService
    {
        private readonly HashSet<int> _syntaxTokenKinds = new();
        private readonly ImmutableArray<Lazy<IEmbeddedLanguageClassifier, OrderableLanguageMetadata>> _classifiers;

        protected AbstractEmbeddedLanguageClassificationService(
            IEnumerable<Lazy<IEmbeddedLanguageClassifier, OrderableLanguageMetadata>> classifiers,
            ISyntaxKinds syntaxKinds)
        {
            // Move the fallback classifier to the end if it exists.
            var classifierList = ExtensionOrderer.Order(classifiers).Where(c => c.Metadata.Language == this.Language).ToList();
            var fallbackClassifier = classifierList.FirstOrDefault(c => c.Metadata.Name == PredefinedEmbeddedLanguageClassifierNames.Fallback);
            if (fallbackClassifier != null)
            {
                classifierList.Remove(fallbackClassifier);
                classifierList.Add(fallbackClassifier);
            }

            _classifiers = classifierList.ToImmutableArray();

            _syntaxTokenKinds.Add(syntaxKinds.CharacterLiteralToken);
            _syntaxTokenKinds.Add(syntaxKinds.StringLiteralToken);
            _syntaxTokenKinds.Add(syntaxKinds.InterpolatedStringTextToken);

            if (syntaxKinds.SingleLineRawStringLiteralToken != null)
                _syntaxTokenKinds.Add(syntaxKinds.SingleLineRawStringLiteralToken.Value);

            if (syntaxKinds.MultiLineRawStringLiteralToken != null)
                _syntaxTokenKinds.Add(syntaxKinds.MultiLineRawStringLiteralToken.Value);
        }

        protected abstract string Language { get; }

        public async Task AddEmbeddedLanguageClassificationsAsync(
            Document document, TextSpan textSpan, ClassificationOptions options, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            AddEmbeddedLanguageClassifications(semanticModel, textSpan, options, result, cancellationToken);
        }

        public void AddEmbeddedLanguageClassifications(
            SemanticModel semanticModel, TextSpan textSpan, ClassificationOptions options, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
            var worker = new Worker(_classifiers, _syntaxTokenKinds, semanticModel, textSpan, options, result, cancellationToken);
            worker.Recurse(root);
        }

        private ref struct Worker
        {
            private readonly ImmutableArray<Lazy<IEmbeddedLanguageClassifier, OrderableLanguageMetadata>> _classifiers;
            private readonly HashSet<int> _syntaxTokenKinds;
            private readonly SemanticModel _semanticModel;
            private readonly TextSpan _textSpan;
            private readonly ClassificationOptions _options;
            private readonly ArrayBuilder<ClassifiedSpan> _result;
            private readonly CancellationToken _cancellationToken;

            public Worker(
                ImmutableArray<Lazy<IEmbeddedLanguageClassifier, OrderableLanguageMetadata>> classifiers,
                HashSet<int> syntaxTokenKinds,
                SemanticModel semanticModel,
                TextSpan textSpan,
                ClassificationOptions options,
                ArrayBuilder<ClassifiedSpan> result,
                CancellationToken cancellationToken)
            {
                _classifiers = classifiers;
                _syntaxTokenKinds = syntaxTokenKinds;
                _semanticModel = semanticModel;
                _textSpan = textSpan;
                _options = options;
                _result = result;
                _cancellationToken = cancellationToken;
            }

            public void Recurse(SyntaxNode node)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (node.Span.IntersectsWith(_textSpan))
                {
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        if (child.IsNode)
                        {
                            Recurse(child.AsNode()!);
                        }
                        else
                        {
                            ProcessToken(child.AsToken());
                        }
                    }
                }
            }

            private void ProcessToken(SyntaxToken token)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                ProcessTriviaList(token.LeadingTrivia);
                ClassifyToken(token);
                ProcessTriviaList(token.TrailingTrivia);
            }

            private readonly void ClassifyToken(SyntaxToken token)
            {
                if (token.Span.IntersectsWith(_textSpan) && _syntaxTokenKinds.Contains(token.RawKind))
                {
                    var context = new EmbeddedLanguageClassificationContext(
                        _semanticModel, token, _options, _result, _cancellationToken);
                    foreach (var classifier in _classifiers)
                    {
                        if (classifier != null)
                        {
                            var count = _result.Count;
                            classifier.Value.RegisterClassifications(context);
                            if (_result.Count != count)
                            {
                                // This classifier added values.  No need to check the other ones.
                                return;
                            }
                        }
                    }
                }
            }

            private void ProcessTriviaList(SyntaxTriviaList triviaList)
            {
                foreach (var trivia in triviaList)
                    ProcessTrivia(trivia);
            }

            private void ProcessTrivia(SyntaxTrivia trivia)
            {
                if (trivia.HasStructure && trivia.FullSpan.IntersectsWith(_textSpan))
                    Recurse(trivia.GetStructure()!);
            }
        }
    }
}
