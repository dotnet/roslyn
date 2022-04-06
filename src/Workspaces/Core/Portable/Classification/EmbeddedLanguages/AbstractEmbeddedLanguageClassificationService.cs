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
        /// <summary>
        /// The kinds of literal tokens that we want to do embedded language classification for.
        /// </summary>
        private readonly HashSet<int> _syntaxTokenKinds = new();

        /// <summary>
        /// Classifiers that can annotated older APIs not updated to use the [StringSyntax] attribute.
        /// </summary>
        private readonly ImmutableArray<Lazy<IEmbeddedLanguageClassifier, EmbeddedLanguageMetadata>> _legacyClassifiers;

        /// <summary>
        /// Finally classifier to run if there is no embedded language in a string.  It will just classify escape sequences.
        /// </summary>
        private readonly IEmbeddedLanguageClassifier _fallbackClassifier;

        /// <summary>
        /// Ordered mapping of a lang ID (like 'Json') to all the classifiers that can actually classify that language.
        /// This allows for multiple classifiers to be available.  The first classifier though that returns
        /// classifications for a string will 'win' and no other classifiers will contribute.
        /// </summary>
        private readonly Dictionary<string, ArrayBuilder<Lazy<IEmbeddedLanguageClassifier, EmbeddedLanguageMetadata>>> _identifierToClassifiers = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Helper to look at string literals and determine what language they are annotated to take.
        /// </summary>
        private readonly EmbeddedLanguageDetector _detector;

        protected AbstractEmbeddedLanguageClassificationService(
            string languageName,
            EmbeddedLanguageInfo info,
            ISyntaxKinds syntaxKinds,
            IEmbeddedLanguageClassifier fallbackClassifier,
            IEnumerable<Lazy<IEmbeddedLanguageClassifier, EmbeddedLanguageMetadata>> allClassifiers)
        {
            _fallbackClassifier = fallbackClassifier;

            // Order the classifiers to respect the [Order] annotations.
            var orderedClassifiers = ExtensionOrderer.Order(allClassifiers).Where(c => c.Metadata.Language == languageName).ToImmutableArray();

            // Grab out the classifiers that handle unannotated literals and APIs.
            _legacyClassifiers = orderedClassifiers.WhereAsArray(c => c.Metadata.SupportsUnannotatedAPIs);

            foreach (var classifier in orderedClassifiers)
            {
                foreach (var identifier in classifier.Metadata.Identifiers)
                    _identifierToClassifiers.MultiAdd(identifier, classifier);
            }

            foreach (var (_, classifiers) in _identifierToClassifiers)
                classifiers.RemoveDuplicates();

            _detector = new EmbeddedLanguageDetector(info, _identifierToClassifiers.Keys.ToImmutableArray());

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
            using var _ = ArrayBuilder<IEmbeddedLanguageClassifier>.GetInstance(out var classifierBuffer);
            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
            var worker = new Worker(this, semanticModel, textSpan, options, result, classifierBuffer, cancellationToken);
            worker.Recurse(root);
        }

        private ref struct Worker
        {
            private readonly AbstractEmbeddedLanguageClassificationService _service;
            private readonly SemanticModel _semanticModel;
            private readonly TextSpan _textSpan;
            private readonly ClassificationOptions _options;
            private readonly ArrayBuilder<ClassifiedSpan> _result;
            private readonly ArrayBuilder<IEmbeddedLanguageClassifier> _classifierBuffer;
            private readonly CancellationToken _cancellationToken;

            public Worker(
                AbstractEmbeddedLanguageClassificationService service,
                SemanticModel semanticModel,
                TextSpan textSpan,
                ClassificationOptions options,
                ArrayBuilder<ClassifiedSpan> result,
                ArrayBuilder<IEmbeddedLanguageClassifier> classifierBuffer,
                CancellationToken cancellationToken)
            {
                _service = service;
                _semanticModel = semanticModel;
                _textSpan = textSpan;
                _options = options;
                _result = result;
                _classifierBuffer = classifierBuffer;
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
                    _classifierBuffer.Clear();

                    var context = new EmbeddedLanguageClassificationContext(
                        _semanticModel, token, _options, _result, _cancellationToken);

                    // First, see if this is a string annotated with either a comment or [StringSyntax] attribute. If
                    // so, delegate to the first classifier we have registered for whatever language ID we find.
                    if (_service._detector.IsEmbeddedLanguageToken(token, _semanticModel, _cancellationToken, out var identifier, out _) &&
                        _service._identifierToClassifiers.TryGetValue(identifier, out var classifiers))
                    {
                        foreach (var classifier in classifiers)
                        {
                            // keep track of what classifiers we've run so we don't call into them multiple times.
                            _classifierBuffer.Add(classifier.Value);

                            // If this classifier added values then need to check the other ones.
                            if (TryClassify(classifier.Value, context))
                                return;
                        }
                    }

                    // It wasn't an annotated API.  See if it's some legacy API our historical classifiers have direct
                    // support for (for example, .net APIs prior to Net6).
                    foreach (var legacyClassifier in _service._legacyClassifiers)
                    {
                        // don't bother trying to classify again if we already tried above.
                        if (_classifierBuffer.Contains(legacyClassifier.Value))
                            continue;

                        // If this classifier added values then need to check the other ones.
                        if (TryClassify(legacyClassifier.Value, context))
                            return;
                    }

                    // Finally, give the fallback classifier a chance to classify basic language escapes.
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
