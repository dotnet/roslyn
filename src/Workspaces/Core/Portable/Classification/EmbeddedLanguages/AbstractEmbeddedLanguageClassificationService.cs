// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract class AbstractEmbeddedLanguageClassificationService : IEmbeddedLanguageClassificationService
    {
        private readonly HashSet<int> _syntaxTokenKinds = new();
        private readonly ImmutableArray<Lazy<IEmbeddedLanguageClassifier, EmbeddedLanguageMetadata>> _classifiers;
        private readonly IEmbeddedLanguageClassifier _fallbackClassifier;

        private readonly Dictionary<string, List<Lazy<IEmbeddedLanguageClassifier, EmbeddedLanguageMetadata>>> _identifierToClassifiers = new();

        protected AbstractEmbeddedLanguageClassificationService(
            IEnumerable<Lazy<IEmbeddedLanguageClassifier, EmbeddedLanguageMetadata>> classifiers,
            IEmbeddedLanguageClassifier fallbackClassifier,
            ISyntaxKinds syntaxKinds,
            string languageName)
        {
            _fallbackClassifier = fallbackClassifier;

            _classifiers = ExtensionOrderer.Order(classifiers).Where(c => c.Metadata.Language == languageName).ToImmutableArray();

            foreach (var classifier in classifiers)
            {
                foreach (var identifier in classifier.Metadata.Identifiers)
                    _identifierToClassifiers.MultiAdd(identifier, classifier);
            }

            _syntaxTokenKinds.Add(syntaxKinds.CharacterLiteralToken);
            _syntaxTokenKinds.Add(syntaxKinds.StringLiteralToken);
            _syntaxTokenKinds.Add(syntaxKinds.InterpolatedStringTextToken);

            if (syntaxKinds.SingleLineRawStringLiteralToken != null)
                _syntaxTokenKinds.Add(syntaxKinds.SingleLineRawStringLiteralToken.Value);

            if (syntaxKinds.MultiLineRawStringLiteralToken != null)
                _syntaxTokenKinds.Add(syntaxKinds.MultiLineRawStringLiteralToken.Value);
        }

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
            var worker = new Worker(this, semanticModel, textSpan, options, result, cancellationToken);
            worker.Recurse(root);
        }

        private ref struct Worker
        {
            private readonly AbstractEmbeddedLanguageClassificationService _service;
            private readonly SemanticModel _semanticModel;
            private readonly TextSpan _textSpan;
            private readonly ClassificationOptions _options;
            private readonly ArrayBuilder<ClassifiedSpan> _result;
            private readonly CancellationToken _cancellationToken;

            public Worker(
                AbstractEmbeddedLanguageClassificationService service,
                SemanticModel semanticModel,
                TextSpan textSpan,
                ClassificationOptions options,
                ArrayBuilder<ClassifiedSpan> result,
                CancellationToken cancellationToken)
            {
                _service = service;
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

            private void ClassifyToken(SyntaxToken token)
            {
                if (token.Span.IntersectsWith(_textSpan) && _service._syntaxTokenKinds.Contains(token.RawKind))
                {
                    var context = new EmbeddedLanguageClassificationContext(
                        _semanticModel, token, _options, _result, _cancellationToken);
                    foreach (var classifier in _service._classifiers)
                    {
                        // This classifier added values.  No need to check the other ones.
                        if (TryClassify(classifier.Value, context))
                            return;
                    }

                    TryClassify(_service._fallbackClassifier, context);
                }
            }

            private bool TryClassify(IEmbeddedLanguageClassifier classifier, EmbeddedLanguageClassificationContext context)
            {
                var count = _result.Count;
                classifier.RegisterClassifications(context);
                return _result.Count != count;
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
