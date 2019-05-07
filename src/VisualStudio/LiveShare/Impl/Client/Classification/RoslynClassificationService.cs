//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Remote.Shared.CustomProtocol;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal class RoslynClassificationService : ISyntaxClassificationService
    {
        private readonly RoslynLSPClientServiceFactory roslynLSPClientServiceFactory;
        private readonly ISyntaxClassificationService originalService;
        private readonly ClassificationTypeMap classificationTypeMap;

        public RoslynClassificationService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, ISyntaxClassificationService originalService, ClassificationTypeMap classificationTypeMap)
        {
            this.roslynLSPClientServiceFactory = roslynLSPClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLSPClientServiceFactory));
            this.originalService = originalService ?? throw new ArgumentNullException(nameof(originalService));
            this.classificationTypeMap = classificationTypeMap ?? throw new ArgumentNullException(nameof(classificationTypeMap));
        }

        public void AddLexicalClassifications(SourceText text, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            this.originalService.AddLexicalClassifications(text, textSpan, result, cancellationToken);
        }

        public void AddSemanticClassifications(SemanticModel semanticModel, TextSpan textSpan, Workspace workspace, Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers, Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
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
            var lspClient = this.roslynLSPClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return;
            }

            var classificationParams = new ClassificationParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = lspClient.ProtocolConverter.ToProtocolUri(new Uri(filePath)) },
                Range = textSpan.ToRange(text)
            };

            ClassificationSpan[] classificationSpans = await lspClient.RequestAsync(RoslynMethods.Classifications, classificationParams, cancellationToken).ConfigureAwait(false);
            if (classificationSpans == null)
            {
                return;
            }

            foreach (var classificationSpan in classificationSpans)
            {
                // The host may return more classifications than are supported by the guest. As an example, 15.7 added classifications for type members which wouldnt be understood by a 15.6 guest.
                // Check with the classificationTypeMap to see if this is a known classification.
                var classification = classificationSpan.Classification;
                if (this.classificationTypeMap.GetClassificationType(classification) == null)
                {
                    classification = ClassificationTypeNames.Identifier;
                }

                var span = classificationSpan.Range.ToTextSpan(text);
                if (span.End <= text.Length)
                {
                    result.Add(new ClassifiedSpan(classification, span));
                }
            }
        }

        public void AddSyntacticClassifications(SyntaxTree syntaxTree, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            this.originalService.AddSyntacticClassifications(syntaxTree, textSpan, result, cancellationToken);
        }

        public ClassifiedSpan FixClassification(SourceText text, ClassifiedSpan classifiedSpan)
        {
            return this.originalService.FixClassification(text, classifiedSpan);
        }

        public ImmutableArray<ISyntaxClassifier> GetDefaultSyntaxClassifiers()
        {
            return this.originalService.GetDefaultSyntaxClassifiers();
        }
    }
}
