// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using LS = Microsoft.VisualStudio.LiveShare.LanguageServices;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Classification
{
    internal class RoslynClassificationService : ISyntaxClassificationService
    {
        private readonly AbstractLspClientServiceFactory _roslynLspClientServiceFactory;
        private readonly ISyntaxClassificationService _originalService;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IThreadingContext _threadingContext;

        public RoslynClassificationService(AbstractLspClientServiceFactory roslynLspClientServiceFactory, ISyntaxClassificationService originalService,
            ClassificationTypeMap classificationTypeMap, IThreadingContext threadingContext)
        {
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLspClientServiceFactory));
            _originalService = originalService ?? throw new ArgumentNullException(nameof(originalService));
            _classificationTypeMap = classificationTypeMap ?? throw new ArgumentNullException(nameof(classificationTypeMap));
            _threadingContext = threadingContext;
        }

        public void AddLexicalClassifications(SourceText text, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            _originalService.AddLexicalClassifications(text, textSpan, result, cancellationToken);
        }

        public void AddSemanticClassifications(SemanticModel semanticModel, TextSpan textSpan, Workspace workspace, Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers, Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            _threadingContext.JoinableTaskFactory.Run(async () =>
            {
                var sourceText = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
                await AddRemoteSemanticClassificationsAsync(sourceText, semanticModel.SyntaxTree.FilePath, textSpan, result, cancellationToken).ConfigureAwait(false);
            });
        }

        public async Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers, Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            // If we are in the preview workspace, the request is to try to colorize a lightbulb preview. The document would have changed
            // in this workspace and we currently don't support requests for anything but the Workspace.CurrentSolution.
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.Preview)
            {
                return;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            await AddRemoteSemanticClassificationsAsync(text, document.FilePath, textSpan, result, cancellationToken).ConfigureAwait(false);
        }

        private async Task AddRemoteSemanticClassificationsAsync(SourceText text, string filePath, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return;
            }

            var classificationParams = new ClassificationParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = lspClient.ProtocolConverter.ToProtocolUri(new Uri(filePath)) },
                Range = ProtocolConversions.TextSpanToRange(textSpan, text)
            };

            var request = new LS.LspRequest<ClassificationParams, ClassificationSpan[]>(RoslynMethods.ClassificationsName);
            var classificationSpans = await lspClient.RequestAsync(request, classificationParams, cancellationToken).ConfigureAwait(false);
            if (classificationSpans == null)
            {
                return;
            }

            foreach (var classificationSpan in classificationSpans)
            {
                // The host may return more classifications than are supported by the guest. As an example, 15.7 added classifications for type members which wouldnt be understood by a 15.6 guest.
                // Check with the classificationTypeMap to see if this is a known classification.
                var classification = classificationSpan.Classification;
                if (_classificationTypeMap.GetClassificationType(classification) == null)
                {
                    classification = ClassificationTypeNames.Identifier;
                }

                var span = ProtocolConversions.RangeToTextSpan(classificationSpan.Range, text);
                if (span.End <= text.Length)
                {
                    result.Add(new ClassifiedSpan(classification, span));
                }
            }
        }

        public void AddSyntacticClassifications(SyntaxTree syntaxTree, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            _originalService.AddSyntacticClassifications(syntaxTree, textSpan, result, cancellationToken);
        }

        public ClassifiedSpan FixClassification(SourceText text, ClassifiedSpan classifiedSpan)
        {
            return _originalService.FixClassification(text, classifiedSpan);
        }

        public ImmutableArray<ISyntaxClassifier> GetDefaultSyntaxClassifiers()
        {
            return _originalService.GetDefaultSyntaxClassifiers();
        }
    }
}
