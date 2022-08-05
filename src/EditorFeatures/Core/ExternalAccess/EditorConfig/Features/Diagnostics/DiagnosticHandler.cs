// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using System.Collections.Immutable;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using StreamJsonRpc;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    [ExportStatelessLspService(typeof(PullDiagnosticHandler), ProtocolConstants.EditorConfigLanguageContract), Shared]
    [Method(VSInternalMethods.DocumentPullDiagnosticName)]
    internal sealed class PullDiagnosticHandler : AbstractPullDiagnosticHandler<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport, VSInternalDiagnosticReport[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PullDiagnosticHandler(
            IDiagnosticAnalyzerService analyzerService,
            EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource,
            IGlobalOptionService globalOptions)
            : base(analyzerService, editAndContinueDiagnosticUpdateSource, globalOptions)
        {
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(VSInternalDocumentDiagnosticsParams request) => request.TextDocument;

        protected override VSInternalDiagnosticReport CreateReport(TextDocumentIdentifier? _, VisualStudio.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
            => new() { Diagnostics = diagnostics, ResultId = resultId };

        protected override VSInternalDiagnosticReport CreateRemovedReport(TextDocumentIdentifier identifier)
            => CreateReport(identifier, diagnostics: null, resultId: null);

        protected override VSInternalDiagnosticReport CreateUnchangedReport(TextDocumentIdentifier identifier, string resultId)
            => CreateReport(identifier, diagnostics: null, resultId);

        protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(VSInternalDocumentDiagnosticsParams diagnosticsParams)
        {
            if (diagnosticsParams.PreviousResultId != null && diagnosticsParams.TextDocument != null)
            {
                return ImmutableArray.Create(new PreviousPullResult(diagnosticsParams.PreviousResultId, diagnosticsParams.TextDocument));
            }

            // The client didn't provide us with a previous result to look for, so we can't lookup anything.
            return null;
        }

        protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
            => ConvertTags(diagnosticData, potentialDuplicate: false);

        protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
        {
            return ValueTaskFactory.FromResult(GetRequestedDocument(context));
        }

        protected override VSInternalDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalDiagnosticReport> progress)
        {
            return progress.GetValues();
        }

        internal static ImmutableArray<IDiagnosticSource> GetRequestedDocument(RequestContext context)
        {
            if (context.AdditionalDocument == null)
            {
                context.TraceInformation("Ignoring diagnostics request because no document was provided");
                return ImmutableArray<IDiagnosticSource>.Empty;
            }

            if (!context.IsTracking(context.AdditionalDocument.GetURI()))
            {
                context.TraceWarning($"Ignoring diagnostics request for untracked document: {context.AdditionalDocument.GetURI()}");
                return ImmutableArray<IDiagnosticSource>.Empty;
            }

            return ImmutableArray.Create<IDiagnosticSource>(new DocumentDiagnosticSource(context.AdditionalDocument));
        }

        private static Dictionary<ProjectOrDocumentId, PreviousPullResult> GetIdToPreviousDiagnosticParams(RequestContext context, ImmutableArray<PreviousPullResult> previousResults, out ImmutableArray<PreviousPullResult> removedDocuments)
        {
            Contract.ThrowIfNull(context.Solution);

            var result = new Dictionary<ProjectOrDocumentId, PreviousPullResult>();
            using var _ = ArrayBuilder<PreviousPullResult>.GetInstance(out var removedDocumentsBuilder);
            foreach (var diagnosticParams in previousResults)
            {
                if (diagnosticParams.TextDocument != null)
                {
                    var id = GetIdForPreviousResult(diagnosticParams.TextDocument, context.Solution);
                    if (id != null)
                    {
                        result[id.Value] = diagnosticParams;
                    }
                    else
                    {
                        // The client previously had a result from us for this document, but we no longer have it in our solution.
                        // Record it so we can report to the client that it has been removed.
                        removedDocumentsBuilder.Add(diagnosticParams);
                    }
                }
            }

            removedDocuments = removedDocumentsBuilder.ToImmutable();
            return result;

            static ProjectOrDocumentId? GetIdForPreviousResult(TextDocumentIdentifier textDocumentIdentifier, Solution solution)
            {
                var additionalDocument = solution.GetAdditionalDocument(textDocumentIdentifier);
                if (additionalDocument != null)
                {
                    return new ProjectOrDocumentId(additionalDocument.Id);
                }

                return null;
            }
        }

        private async Task<VSInternalDiagnosticReport> ComputeAndReportCurrentDiagnosticsAsync(
            RequestContext context,
            DocumentDiagnosticSource diagnosticSource,
            string resultId,
            DiagnosticMode diagnosticMode,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<VSDiagnostic>.GetInstance(out var result);
            var diagnostics = await diagnosticSource.GetDiagnosticsAsync(DiagnosticAnalyzerService, context, diagnosticMode, cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
                result.Add(ConvertDiagnostic(diagnosticSource, diagnostic));

            return CreateReport(new TextDocumentIdentifier { Uri = diagnosticSource.GetUri() }, result.ToArray(), resultId);
        }

        private static void HandleRemovedDocuments(RequestContext context, ImmutableArray<PreviousPullResult> removedPreviousResults, BufferedProgress<VSInternalDiagnosticReport> progress)
        {
            foreach (var removedResult in removedPreviousResults)
            {
                context.TraceInformation($"Clearing diagnostics for removed document: {removedResult.TextDocument.Uri}");

                progress.Report(CreateRemovedReport(removedResult.TextDocument));
            }
        }

        private static VSDiagnostic ConvertDiagnostic(DocumentDiagnosticSource diagnosticSource, DiagnosticData diagnosticData)
        {
            Contract.ThrowIfNull(diagnosticData.Message, $"Got a document diagnostic that did not have a {nameof(diagnosticData.Message)}");

            var project = diagnosticSource.GetProject();

            var vsDiagnostic = CreateBaseLspDiagnostic();
            vsDiagnostic.DiagnosticType = diagnosticData.Category;
            vsDiagnostic.Projects = new[]
            {
                new VSDiagnosticProjectInformation
                {
                    ProjectIdentifier = project.Id.Id.ToString(),
                    ProjectName = project.Name,
                },
            };

            return vsDiagnostic;

            // We can just use VSDiagnostic as it doesn't have any default properties set that
            // would get automatically serialized.
            VSDiagnostic CreateBaseLspDiagnostic()
            {
                var diagnostic = new VSDiagnostic
                {
                    Code = diagnosticData.Id,
                    CodeDescription = ProtocolConversions.HelpLinkToCodeDescription(diagnosticData.GetValidHelpLinkUri()),
                    Message = diagnosticData.Message,
                    Severity = ConvertDiagnosticSeverity(diagnosticData.Severity),
                    Tags = ConvertTags(diagnosticData, potentialDuplicate: false),
                };
                var range = GetRange(diagnosticData.DataLocation);
                if (range != null)
                {
                    diagnostic.Range = range;
                }

                return diagnostic;
            }

            static Range? GetRange(DiagnosticDataLocation? dataLocation)
            {
                if (dataLocation == null)
                {
                    return null;
                }

                return new Range
                {
                    Start = new Position
                    {
                        Character = dataLocation.OriginalStartColumn,
                        Line = dataLocation.OriginalStartLine,
                    },
                    End = new Position
                    {
                        Character = dataLocation.OriginalEndColumn,
                        Line = dataLocation.OriginalEndLine,
                    }
                };
            }
        }

        private static LSP.DiagnosticSeverity ConvertDiagnosticSeverity(DiagnosticSeverity severity)
            => severity switch
            {
                DiagnosticSeverity.Hidden => LSP.DiagnosticSeverity.Hint,
                DiagnosticSeverity.Info => LSP.DiagnosticSeverity.Hint,
                DiagnosticSeverity.Warning => LSP.DiagnosticSeverity.Warning,
                DiagnosticSeverity.Error => LSP.DiagnosticSeverity.Error,
                _ => throw ExceptionUtilities.UnexpectedValue(severity),
            };

        /// <summary>
        /// Returns all the documents that should be processed.
        /// </summary>
        public static ImmutableArray<TextDocument> GetAdditionalDocuments(RequestContext context)
        {
            return context.AdditionalDocument == null ? ImmutableArray<TextDocument>.Empty : ImmutableArray.Create(context.AdditionalDocument);
        }

        private record struct DocumentDiagnosticSource(TextDocument Document)
        {
            public ProjectOrDocumentId GetId() => new(Document.Id);

            public Project GetProject() => Document.Project;

            public Uri GetUri() => Document.GetURI();

            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
            {
                var document = context.AdditionalDocument;
                Contract.ThrowIfNull(document);

                var workspace = document.Project.Solution.Workspace;

                var filePath = document.FilePath;
                Contract.ThrowIfNull(filePath);

                var optionSet = workspace.Options;
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var diagnostics = new ImmutableArray<DiagnosticData>();

                foreach (var line in text.Lines)
                {
                    var lineText = line.ToString();
                    if (lineText.Where(c => c == '=').Count() > 1)
                    {
                        diagnostics.Add(new DiagnosticData
                        {

                        });
                    }
                    if (lineText.Contains('='))
                    {
                        
                    }
                }

                return new();
            }
        }
    }
}
