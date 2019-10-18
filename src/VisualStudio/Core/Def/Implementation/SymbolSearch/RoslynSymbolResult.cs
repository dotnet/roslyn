using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch.Capabilities;
using Microsoft.VisualStudio.LanguageServices.FindUsages;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal class RoslynSymbolResult : SymbolSearchResult, IResultInLocalFile, IResultWithDecoratedDefinition, IResultWithClassifiedContext, IResultInNamedProject, IResultWithVSGuids
    {
        private DefinitionItem definition;
        private SourceReferenceItem reference;
        private RoslynSymbolSource source;
        private static ISymbolOriginDefinition LocalSymbolOrigin = null;

        public override string Name { get; }
        public override string Origin { get; }
        public override ISymbolSource Owner { get; }

        private RoslynSymbolResult(string name, RoslynSymbolSource owner)
        {
            this.Owner = owner;
            this.Name = name;
            this.Origin = PredefinedSymbolOrigins.LocalCode;
        }

        internal static async Task<RoslynSymbolResult> MakeAsync(RoslynSymbolSource symbolSource, DefinitionItem definition, DocumentSpan documentSpan, CancellationToken token)
        {
            var result = new RoslynSymbolResult(definition.NameDisplayParts.FirstOrDefault().Text, symbolSource);
            result.source = symbolSource;
            result.definition = definition;

            result.ClassifiedDefinition = new ClassifiedTextElement(
                definition.NameDisplayParts.Select(n =>
                    new ClassifiedTextRun(n.Tag.ToClassificationTypeName(), n.ToVisibleDisplayString(includeLeftToRightMarker: true))));
            result.ClassifiedContext = new ClassifiedTextElement(
                definition.DisplayParts.Select(n =>
                    new ClassifiedTextRun(n.Tag.ToClassificationTypeName(), n.ToVisibleDisplayString(includeLeftToRightMarker: true))));

            var (projectId, projectName, sourceText, documentId) = await GetGuidAndProjectNameAndSourceTextAsync(documentSpan.Document)
                .ConfigureAwait(false);
            result.ProjectId = projectId.ToString();
            result.ProjectName = projectName;
            result.DocumentId = documentId.ToString();

            var (excerptResult, lineText) = await ExcerptAsync(sourceText, documentSpan, token).ConfigureAwait(false);

            // decorate the parent definition:
            //result.ClassifiedDefinition = await ProtocolConversions.DocumentSpanToLocationWithTextAsync(definition.SourceSpans.First(), sourceText, token).ConfigureAwait(false);
            result.DefinitionIcon = definition.Tags.GetFirstGlyph().GetImageId();
            /*
            // do this for each reference:
            var classifiedSpansAndHighlightSpan = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(reference.SourceSpan, context.CancellationToken).ConfigureAwait(false);
            var classifiedSpans = classifiedSpansAndHighlightSpan.ClassifiedSpans;
            var referenceLocation = await ProtocolConversions.DocumentSpanToLocationAsync(reference.SourceSpan, cancellationToken).ConfigureAwait(false);
            var docText = await reference.SourceSpan.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            var classifiedText = new ClassifiedTextElement(classifiedSpans.Select(cspan => new ClassifiedTextRun(cspan.ClassificationType, docText.ToString(cspan.TextSpan))));
            var locationWithText = new LSP.LocationWithText { Range = referenceLocation.Range, Uri = referenceLocation.Uri, Text = classifiedText };

            result.ClassifiedContext = 
            */
            var mappedDocumentSpan = await TryMapAndGetFirstAsync(documentSpan, sourceText, token).ConfigureAwait(false);
            if (mappedDocumentSpan.HasValue)
            {
                var location = mappedDocumentSpan.Value;
                result.PersistentSpan = symbolSource.ServiceProvider.PersistentSpanFactory.Create(
                    documentSpan.Document.FilePath,
                    location.LinePositionSpan.Start.Line,
                    location.LinePositionSpan.Start.Character,
                    location.LinePositionSpan.End.Line,
                    location.LinePositionSpan.End.Character,
                    SpanTrackingMode.EdgeInclusive);
            }

            return result;
        }

        internal static async Task<RoslynSymbolResult> MakeAsync(RoslynSymbolSource symbolSource, SourceReferenceItem reference, DocumentSpan documentSpan, CancellationToken token)
        {
            // TODO: Understand how Roslyn calls GetReferenceGroupsAsync
            // it seems that at this time, we have full knowledge of definitions
            // and their references

            var result = await MakeAsync(symbolSource, reference.Definition, documentSpan, token)
                .ConfigureAwait(false);
            //result.Name = reference.ToString();
            return result;
        }

        public IPersistentSpan PersistentSpan { get; private set; }

        public ClassifiedTextElement ClassifiedDefinition { get; private set; }

        public Span HighlightSpan { get; private set; }

        public string DocumentId { get; private set; }

        public string ProjectId { get; private set; }

        public ClassifiedTextElement ClassifiedContext { get; private set; }

        public ImageId DefinitionIcon { get; private set; }

        public string ProjectName { get; set; }

        public Task NavigateToAsync(CancellationToken token)
        {
            // how to get the workspace?
            //return definition.TryNavigateTo()
            return Task.CompletedTask;
        }

        private static Span AsSpan(TextSpan sourceSpan)
        {
            return new Span(sourceSpan.Start, sourceSpan.Length);
        }

        // Taken from AbstractTableDataSourceFindUsagesContext
        internal static async Task<(Guid projectId, string projectName, SourceText text, Guid documentId)> GetGuidAndProjectNameAndSourceTextAsync(Document document)
        {
            // The FAR system needs to know the guid for the project that a def/reference is 
            // from (to support features like filtering).  Normally that would mean we could
            // only support this from a VisualStudioWorkspace.  However, we want till work 
            // in cases like Any-Code (which does not use a VSWorkspace).  So we are tolerant
            // when we have another type of workspace.  This means we will show results, but
            // certain features (like filtering) may not work in that context.
            var vsWorkspace = document.Project.Solution.Workspace as VisualStudioWorkspace;

            var projectName = document.Project.Name;
            var projectGuid = vsWorkspace?.GetProjectGuid(document.Project.Id) ?? Guid.Empty;
            var documentGuid = vsWorkspace?.GetDocumentIdInCurrentContext(document.Id).Id ?? Guid.Empty;

            // TODO: get cancellation token from the IStreamingSymbolSearchSink
            var sourceText = await document.GetTextAsync(default).ConfigureAwait(false);
            return (projectGuid, projectName, sourceText, documentGuid);
        }

        // Taken from AbstractDocumentSpanEntry
        public static SourceText GetLineContainingPosition(SourceText text, int position)
        {
            var line = text.Lines.GetLineFromPosition(position);

            return text.GetSubText(line.Span);
        }

        // Taken from AbstractDocumentSpanEntry
        public static async Task<MappedSpanResult?> TryMapAndGetFirstAsync(DocumentSpan documentSpan, SourceText sourceText, CancellationToken cancellationToken)
        {
            var service = documentSpan.Document.Services.GetService<ISpanMappingService>();
            if (service == null)
            {
                return new MappedSpanResult(documentSpan.Document.FilePath, sourceText.Lines.GetLinePositionSpan(documentSpan.SourceSpan), documentSpan.SourceSpan);
            }

            var results = await service.MapSpansAsync(
                documentSpan.Document, SpecializedCollections.SingletonEnumerable(documentSpan.SourceSpan), cancellationToken).ConfigureAwait(false);

            if (results.IsDefaultOrEmpty)
            {
                return new MappedSpanResult(documentSpan.Document.FilePath, sourceText.Lines.GetLinePositionSpan(documentSpan.SourceSpan), documentSpan.SourceSpan);
            }

            // if span mapping service filtered out the span, make sure
            // to return null so that we remove the span from the result
            return results.FirstOrNullable(r => !r.IsDefault);
        }

        // Taken from WithReferencesFindUsagesContext
        private static async Task<(ExcerptResult, SourceText)> ExcerptAsync(SourceText sourceText, DocumentSpan documentSpan, CancellationToken token)
        {
            var excerptService = documentSpan.Document.Services.GetService<IDocumentExcerptService>();
            if (excerptService != null)
            {
                var result = await excerptService.TryExcerptAsync(documentSpan.Document, documentSpan.SourceSpan, ExcerptMode.SingleLine, token).ConfigureAwait(false);
                if (result != null)
                {
                    return (result.Value, GetLineContainingPosition(result.Value.Content, result.Value.MappedSpan.Start));
                }
            }

            var classificationResult = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(documentSpan, token).ConfigureAwait(false);

            // need to fix the span issue tracking here - https://github.com/dotnet/roslyn/issues/31001
            var excerptResult = new ExcerptResult(
                sourceText,
                classificationResult.HighlightSpan,
                classificationResult.ClassifiedSpans,
                documentSpan.Document,
                documentSpan.SourceSpan);

            return (excerptResult, GetLineContainingPosition(sourceText, documentSpan.SourceSpan.Start));
        }
    }
}
