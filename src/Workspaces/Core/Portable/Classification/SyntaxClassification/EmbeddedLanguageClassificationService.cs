// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification.Classifiers
{
    internal sealed class EmbeddedLanguageClassificationService : IEmbeddedLanguageClassificationService
    {
        private readonly IEmbeddedLanguagesProvider _languagesProvider;

        private readonly HashSet<int> _syntaxTokenKinds = new();

        public EmbeddedLanguageClassificationService(IEmbeddedLanguagesProvider languagesProvider)
        {
            _languagesProvider = languagesProvider;
            _syntaxTokenKinds.AddRange(
                languagesProvider.Languages.Where(p => p.Classifier != null)
                                           .SelectMany(p => p.Classifier.SyntaxTokenKinds));
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
            var worker = new Worker(_languagesProvider, _syntaxTokenKinds, semanticModel, textSpan, options, result);
            worker.Recurse(root, cancellationToken);
        }

        private ref struct Worker
        {
            private readonly IEmbeddedLanguagesProvider _languagesProvider;
            private readonly HashSet<int> _syntaxTokenKinds;
            private readonly SemanticModel _semanticModel;
            private readonly TextSpan _textSpan;
            private readonly ClassificationOptions _options;
            private readonly ArrayBuilder<ClassifiedSpan> _result;

            public Worker(
                IEmbeddedLanguagesProvider _languagesProvider,
                HashSet<int> _syntaxTokenKinds,
                SemanticModel semanticModel,
                TextSpan textSpan,
                ClassificationOptions options,
                ArrayBuilder<ClassifiedSpan> result)
            {
                this._languagesProvider = _languagesProvider;
                this._syntaxTokenKinds = _syntaxTokenKinds;
                _semanticModel = semanticModel;
                _textSpan = textSpan;
                _options = options;
                _result = result;
            }

            public void Recurse(
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (node.Span.IntersectsWith(_textSpan))
                {
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        if (child.IsNode)
                        {
                            Recurse(child.AsNode()!, cancellationToken);
                        }
                        else
                        {
                            ProcessToken(child.AsToken(), cancellationToken);
                        }
                    }
                }
            }

            private void ProcessToken(SyntaxToken token, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessTriviaList(token.LeadingTrivia, cancellationToken);
                ClassifyToken(token, cancellationToken);
                ProcessTriviaList(token.TrailingTrivia, cancellationToken);
            }

            private readonly void ClassifyToken(SyntaxToken token, CancellationToken cancellationToken)
            {
                if (token.Span.IntersectsWith(_textSpan) && _syntaxTokenKinds.Contains(token.RawKind))
                {
                    foreach (var language in _languagesProvider.Languages)
                    {
                        var classifier = language.Classifier;
                        if (classifier != null)
                        {
                            var count = _result.Count;
                            classifier.AddClassifications(token, _semanticModel, _options, _result, cancellationToken);
                            if (_result.Count != count)
                            {
                                // This classifier added values.  No need to check the other ones.
                                return;
                            }
                        }
                    }
                }
            }

            private void ProcessTriviaList(SyntaxTriviaList triviaList, CancellationToken cancellationToken)
            {
                foreach (var trivia in triviaList)
                    ProcessTrivia(trivia, cancellationToken);
            }

            private void ProcessTrivia(SyntaxTrivia trivia, CancellationToken cancellationToken)
            {
                if (trivia.HasStructure && trivia.FullSpan.IntersectsWith(_textSpan))
                    Recurse(trivia.GetStructure()!, cancellationToken);
            }
        }
    }
}
