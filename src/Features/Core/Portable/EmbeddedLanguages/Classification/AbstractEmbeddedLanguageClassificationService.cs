// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification;

internal abstract class AbstractEmbeddedLanguageClassificationService :
    AbstractEmbeddedLanguageFeatureService<IEmbeddedLanguageClassifier>, IEmbeddedLanguageClassificationService
{
    /// <summary>
    /// Finally classifier to run if there is no embedded language in a string.  It will just classify escape sequences.
    /// </summary>
    private readonly IEmbeddedLanguageClassifier _fallbackClassifier;

    protected AbstractEmbeddedLanguageClassificationService(
        string languageName,
        EmbeddedLanguageInfo info,
        ISyntaxKinds syntaxKinds,
        IEmbeddedLanguageClassifier fallbackClassifier,
        IEnumerable<Lazy<IEmbeddedLanguageClassifier, EmbeddedLanguageMetadata>> allClassifiers)
        : base(languageName, info, syntaxKinds, allClassifiers)
    {
        _fallbackClassifier = fallbackClassifier;
    }

    public async Task AddEmbeddedLanguageClassificationsAsync(
        Document document, ImmutableArray<TextSpan> textSpans, ClassificationOptions options, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        var project = document.Project;
        SemanticModel semanticModel;
#pragma warning disable RSEXPERIMENTAL001 // Internal usage of experimental API
        semanticModel = await document.GetRequiredNullableDisabledSemanticModelAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore RSEXPERIMENTAL001

        AddEmbeddedLanguageClassifications(
            project.Solution.Services, project, semanticModel, textSpans, options, result, cancellationToken);
    }

    public void AddEmbeddedLanguageClassifications(
        SolutionServices services, Project? project, SemanticModel semanticModel, ImmutableArray<TextSpan> textSpans, ClassificationOptions options, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        if (project is null)
            return;

        var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
        foreach (var textSpan in textSpans)
        {
            var worker = new Worker(this, services, project, semanticModel, textSpan, options, result, cancellationToken);
            worker.VisitTokens(root);
        }
    }

    private readonly ref struct Worker(
        AbstractEmbeddedLanguageClassificationService service,
        SolutionServices solutionServices,
        Project project,
        SemanticModel semanticModel,
        TextSpan textSpan,
        ClassificationOptions options,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        private readonly AbstractEmbeddedLanguageClassificationService _owner = service;
        private readonly SolutionServices _solutionServices = solutionServices;
        private readonly Project _project = project;
        private readonly SemanticModel _semanticModel = semanticModel;
        private readonly TextSpan _textSpan = textSpan;
        private readonly ClassificationOptions _options = options;
        private readonly SegmentedList<ClassifiedSpan> _result = result;
        private readonly CancellationToken _cancellationToken = cancellationToken;

        public void VisitTokens(SyntaxNode node)
        {
            using var pooledStack = SharedPools.Default<Stack<SyntaxNodeOrToken>>().GetPooledObject();
            var stack = pooledStack.Object;
            stack.Push(node);
            while (stack.TryPop(out var currentNodeOrToken))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (currentNodeOrToken.FullSpan.IntersectsWith(_textSpan))
                {
                    if (currentNodeOrToken.IsNode)
                    {
                        foreach (var child in currentNodeOrToken.ChildNodesAndTokens().Reverse())
                        {
                            stack.Push(child);
                        }
                    }
                    else
                    {
                        ProcessToken(currentNodeOrToken.AsToken());
                    }
                }
            }
        }

        private void ProcessToken(SyntaxToken token)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            // Directives need to be processes as they can contain strings, which then have escapes in them.
            if (token.ContainsDirectives)
                ProcessTriviaList(token.LeadingTrivia);

            ClassifyToken(token);
        }

        private void ClassifyToken(SyntaxToken token)
        {
            if (token.Span.IntersectsWith(_textSpan) && _owner.SyntaxTokenKinds.Contains(token.RawKind))
            {
                var (classifiers, identifier) = _owner.GetServices(_semanticModel, token, _cancellationToken);
                var context = new EmbeddedLanguageClassificationContext(
                    _solutionServices, _project, _semanticModel, token, _textSpan, _options, _owner.Info.VirtualCharService,
                    languageIdentifier: identifier, _result, _cancellationToken);

                foreach (var classifier in classifiers)
                {
                    // If this classifier added values then no need to check the other ones.
                    if (TryClassify(classifier.Value, context))
                        return;
                }

                // If not other classifier classified this, then give the fallback classifier a chance to classify basic language escapes.
                TryClassify(_owner._fallbackClassifier, context);
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
            if (trivia.IsDirective && trivia.FullSpan.IntersectsWith(_textSpan))
                VisitTokens(trivia.GetStructure()!);
        }
    }
}
