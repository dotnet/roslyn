// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using LS = Microsoft.VisualStudio.LiveShare.LanguageServices;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Classification
{
    internal class RoslynSyntaxClassificationService : ISyntaxClassificationService
    {
        private readonly AbstractLspClientServiceFactory _roslynLspClientServiceFactory;
        private readonly RemoteLanguageServiceWorkspace _remoteLanguageServiceWorkspace;
        private readonly ISyntaxClassificationService _originalService;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IExperimentationService _experimentationService;
        private readonly IThreadingContext _threadingContext;

        public RoslynSyntaxClassificationService(AbstractLspClientServiceFactory roslynLspClientServiceFactory, RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace, ISyntaxClassificationService originalService,
            ClassificationTypeMap classificationTypeMap, IExperimentationService experimentationService, IThreadingContext threadingContext)
        {
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory;
            _remoteLanguageServiceWorkspace = remoteLanguageServiceWorkspace;
            _originalService = originalService;
            _classificationTypeMap = classificationTypeMap;
            _experimentationService = experimentationService;
            _threadingContext = threadingContext;
        }

        public void AddLexicalClassifications(SourceText text, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            if (ShouldRunExperiment(WellKnownExperimentNames.SyntacticExp_Local))
            {
                using (new RequestLatencyTracker(SyntacticLspLogger.RequestType.LexicalClassifications))
                {
                    _originalService.AddLexicalClassifications(text, textSpan, result, cancellationToken);
                }
            }
            else if (ShouldRunExperiment(WellKnownExperimentNames.SyntacticExp_Remote))
            {
                var documentId = _remoteLanguageServiceWorkspace.GetDocumentIdInCurrentContext(text.Container);
                var document = _remoteLanguageServiceWorkspace.CurrentSolution.GetDocument(documentId);
                if (document == null)
                {
                    // It is expected that the document cannot be found (e.g. source text comes from preview workspace).
                    // These cases are already not supported in liveshare, so for now just return.
                    return;
                }
                using (new RequestLatencyTracker(SyntacticLspLogger.RequestType.LexicalClassifications))
                {
                    _threadingContext.JoinableTaskFactory.Run(async () =>
                    {
                        await AddRemoteClassificationsAsync(LexicalClassificationsHandler.LexicalClassificationsMethodName, document.FilePath, text, textSpan, result.Add, cancellationToken).ConfigureAwait(false);
                    });
                }
            }
            else if (ShouldRunExperiment(WellKnownExperimentNames.SyntacticExp_LiveShareTagger_Remote) ||
                     ShouldRunExperiment(WellKnownExperimentNames.SyntacticExp_LiveShareTagger_TextMate))
            {
                // do nothing here so that existing RoslynSyntacticTagger return nothing in this mode
                return;
            }
            else
            {
                // Some other invalid flight.  Just fallback to the regular service.  Don't want to block the user based on an experimentation failure.
                _originalService.AddLexicalClassifications(text, textSpan, result, cancellationToken);
            }
        }

        public void AddSemanticClassifications(SemanticModel semanticModel, TextSpan textSpan, Workspace workspace, Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers, Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            _threadingContext.JoinableTaskFactory.Run(async () =>
            {
                var sourceText = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
                await AddRemoteClassificationsAsync(RoslynMethods.ClassificationsName, semanticModel.SyntaxTree.FilePath, sourceText, textSpan, result.Add, cancellationToken).ConfigureAwait(false);
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

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            await AddRemoteClassificationsAsync(RoslynMethods.ClassificationsName, document.FilePath, sourceText, textSpan, result.Add, cancellationToken).ConfigureAwait(false);
        }

        public void AddSyntacticClassifications(SyntaxTree syntaxTree, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            if (ShouldRunExperiment(WellKnownExperimentNames.SyntacticExp_Local))
            {
                using (new RequestLatencyTracker(SyntacticLspLogger.RequestType.SyntacticClassifications))
                {
                    _originalService.AddSyntacticClassifications(syntaxTree, textSpan, result, cancellationToken);
                }
            }
            else if (ShouldRunExperiment(WellKnownExperimentNames.SyntacticExp_Remote))
            {
                using (new RequestLatencyTracker(SyntacticLspLogger.RequestType.SyntacticClassifications))
                {
                    _threadingContext.JoinableTaskFactory.Run(async () =>
                    {
                        var sourceText = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        await AddRemoteClassificationsAsync(SyntaxClassificationsHandler.SyntaxClassificationsMethodName, syntaxTree.FilePath, sourceText, textSpan, result.Add, cancellationToken).ConfigureAwait(false);
                    });
                }
            }
            else if (ShouldRunExperiment(WellKnownExperimentNames.SyntacticExp_LiveShareTagger_Remote) ||
                     ShouldRunExperiment(WellKnownExperimentNames.SyntacticExp_LiveShareTagger_TextMate))
            {
                // do nothing here so that existing RoslynSyntacticTagger return nothing in this mode
                return;
            }
            else
            {
                // Invalid experiment flight or older client.  Since this is an experiment, just fallback.
                _originalService.AddSyntacticClassifications(syntaxTree, textSpan, result, cancellationToken);
            }
        }

        public ClassifiedSpan FixClassification(SourceText text, ClassifiedSpan classifiedSpan)
        {
            return _originalService.FixClassification(text, classifiedSpan);
        }

        public ImmutableArray<ISyntaxClassifier> GetDefaultSyntaxClassifiers()
        {
            return _originalService.GetDefaultSyntaxClassifiers();
        }

        /// <summary>
        /// Check if the experiment should run.
        /// Only runs the experiment if the server provides the capability
        /// and the experiment flight is enabled.
        /// </summary>
        public bool ShouldRunExperiment(string experimentName)
        {
            return ShouldRunExperiment(_roslynLspClientServiceFactory, _experimentationService, experimentName);
        }

        public static bool ShouldRunExperiment(
            AbstractLspClientServiceFactory lspClientServiceFactory,
            IExperimentationService experimentationService,
            string experimentName)
        {
            if (lspClientServiceFactory.ServerCapabilities?.Experimental is RoslynExperimentalCapabilities experimentalCapabilities)
            {
                return experimentalCapabilities.SyntacticLspProvider && experimentationService.IsExperimentEnabled(experimentName);
            }

            return false;
        }

        public async Task AddRemoteClassificationsAsync(string classificationsServiceName, string filePath, SourceText sourceText, TextSpan textSpan, Action<ClassifiedSpan> tagAdder, CancellationToken cancellationToken)
        {
            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return;
            }

            await EnsureInitializationAsync(cancellationToken).ConfigureAwait(false);

            var classificationParams = new ClassificationParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = lspClient.ProtocolConverter.ToProtocolUri(new Uri(filePath)) },
                Range = ProtocolConversions.TextSpanToRange(textSpan, sourceText)
            };

            var request = new LS.LspRequest<ClassificationParams, ClassificationSpan[]>(classificationsServiceName);
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

                var span = ProtocolConversions.RangeToTextSpan(classificationSpan.Range, sourceText);
                if (span.End <= sourceText.Length)
                {
                    tagAdder(new ClassifiedSpan(classification, span));
                }
            }
        }

        public Task EnsureInitializationAsync(CancellationToken cancellationToken)
        {
            return EnsureInitializationAsync(_roslynLspClientServiceFactory, cancellationToken);
        }

        public static async Task EnsureInitializationAsync(AbstractLspClientServiceFactory lspClientServiceFactory, CancellationToken cancellationToken)
        {
            // TODO - Move to roslyn client initialization once liveshare initialization is fixed.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/964288
            await lspClientServiceFactory.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
