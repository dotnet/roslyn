// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification.Classifiers
{
    internal sealed class EmbeddedLanguageClassificationService : IEmbeddedLanguageClassificationService
    {
        private readonly IEmbeddedLanguagesProvider _languagesProvider;

        private readonly HashSet<int> _syntaxTokenKinds = new();

        public EmbeddedLanguageClassificationService(
            IEmbeddedLanguagesProvider languagesProvider,
            ISyntaxKinds syntaxKinds)
        {
            _languagesProvider = languagesProvider;

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
            var worker = new Worker(_languagesProvider, _syntaxTokenKinds, semanticModel, textSpan, options, result, cancellationToken);
            worker.Recurse(root);
        }

        private ref struct Worker
        {
            private readonly IEmbeddedLanguagesProvider _languagesProvider;
            private readonly HashSet<int> _syntaxTokenKinds;
            private readonly SemanticModel _semanticModel;
            private readonly TextSpan _textSpan;
            private readonly ClassificationOptions _options;
            private readonly ArrayBuilder<ClassifiedSpan> _result;
            private readonly CancellationToken _cancellationToken;

            public Worker(
                IEmbeddedLanguagesProvider languagesProvider,
                HashSet<int> syntaxTokenKinds,
                SemanticModel semanticModel,
                TextSpan textSpan,
                ClassificationOptions options,
                ArrayBuilder<ClassifiedSpan> result,
                CancellationToken cancellationToken)
            {
                _languagesProvider = languagesProvider;
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
                    var context = new EmbeddedLanguageClassifierContext(
                        _semanticModel, token, _options, _result, _cancellationToken);
                    foreach (var language in _languagesProvider.Languages)
                    {
                        var classifier = language.Classifier;
                        if (classifier != null)
                        {
                            var count = _result.Count;
                            classifier.RegisterClassifications(context);
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
