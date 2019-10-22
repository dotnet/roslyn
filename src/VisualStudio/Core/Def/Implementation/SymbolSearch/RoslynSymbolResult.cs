using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
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
    internal class RoslynSymbolResult : SymbolSearchResult, IResultInLocalFile, /*IResultWithDecoratedDefinition,*/ IResultWithClassifiedContext, IResultInNamedProject, IResultWithVSGuids, IResultWithKind, IResultInNamedCode
    {
        private DefinitionItem Definition { get; set; }
        private SourceReferenceItem Reference { get; set; }
        private RoslynSymbolSource Source { get; }

        public override string Name => string.Empty;
        public override string Origin => PredefinedSymbolOrigins.LocalCode;
        public override ISymbolSource Owner => Source;

        private RoslynSymbolResult(RoslynSymbolSource source)
        {
            this.Source = source;
        }

        internal static async Task<RoslynSymbolResult> MakeAsync(RoslynSymbolSource symbolSource, DefinitionItem definition, DocumentSpan documentSpan, CancellationToken token)
        {
            var result = new RoslynSymbolResult(symbolSource);
            result.Definition = definition;
            await MakeContent(result, documentSpan, token).ConfigureAwait(false);
            return result;
        }

        internal static async Task<RoslynSymbolResult> MakeAsync(RoslynSymbolSource symbolSource, SourceReferenceItem reference, DocumentSpan documentSpan, CancellationToken token)
        {
            // TODO: Understand how Roslyn calls GetReferenceGroupsAsync
            // it seems that at this time, we have full knowledge of definitions
            // and their references

            var result = new RoslynSymbolResult(symbolSource);
            result.Reference = reference;
            result.Definition = reference.Definition;
            await MakeContent(result, documentSpan, token).ConfigureAwait(false);

            return result;
        }

        private static async Task MakeContent(RoslynSymbolResult result, DocumentSpan documentSpan, CancellationToken token)
        {
            if (result.Reference != null)
            {
                result.Kind = result.Reference.SymbolUsageInfo.IsWrittenTo()
                    ? "Write"
                    : result.Reference.SymbolUsageInfo.IsReadFrom()
                        ? "Read"
                        : string.Empty;
            }
            else
            {
                result.DefinitionIcon = result.Definition.Tags.GetFirstGlyph().GetImageId();
            }
            /*
            result.ClassifiedDefinition = new ClassifiedTextElement(
                definition.NameDisplayParts.Select(n =>
                    new ClassifiedTextRun(n.Tag.ToClassificationTypeName(), n.ToVisibleDisplayString(includeLeftToRightMarker: true))));
            result.ClassifiedContext = new ClassifiedTextElement(
                definition.DisplayParts.Select(n =>
                    new ClassifiedTextRun(n.Tag.ToClassificationTypeName(), n.ToVisibleDisplayString(includeLeftToRightMarker: true))));
*/
            var (projectId, projectName, sourceText, documentId) = await GetGuidAndProjectNameAndSourceTextAsync(documentSpan.Document)
                .ConfigureAwait(false);
            result.ProjectId = projectId.ToString();
            result.ProjectName = projectName;
            result.DocumentId = documentId.ToString();

            var (excerptResult, lineText) = await ExcerptAsync(sourceText, documentSpan, token).ConfigureAwait(false);

            var classifiedText = new ClassifiedTextElement(excerptResult.ClassifiedSpans.Select(cspan =>
                new ClassifiedTextRun(cspan.ClassificationType, sourceText.ToString(cspan.TextSpan))));
            result.ClassifiedContext = classifiedText;


            /*result.ClassifiedContext = new ClassifiedTextElement(
                excerptResult.ClassifiedSpans.Select(n => new ClassifiedTextRun(n.ClassificationType, n.
                );
            excerptResult.ClassifiedSpans*/
            // decorate the parent definition:
            //result.ClassifiedDefinition = await ProtocolConversions.DocumentSpanToLocationWithTextAsync(definition.SourceSpans.First(), sourceText, token).ConfigureAwait(false);

            // do this for each reference:
            if (result.Reference != null)
            {
                var classifiedSpansAndHighlightSpan = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(result.Reference.SourceSpan, token).ConfigureAwait(false);
                var classifiedSpans = classifiedSpansAndHighlightSpan.ClassifiedSpans;
                var docText = await result.Reference.SourceSpan.Document.GetTextAsync(token).ConfigureAwait(false);
                result.ClassifiedContext = new ClassifiedTextElement(classifiedSpans.Select(cspan => new ClassifiedTextRun(cspan.ClassificationType, docText.ToString(cspan.TextSpan))));
                result.HighlightSpan = AsSpan(classifiedSpansAndHighlightSpan.HighlightSpan);
                // TODO: peeek at test1 and test2 above ^

                if (result.Reference.AdditionalProperties.TryGetValue(AbstractReferenceFinder.ContainingTypeInfoPropertyName, out var containingTypeInfo))
                {
                    result.ContainingTypeName = containingTypeInfo;
                }

                if (result.Reference.AdditionalProperties.TryGetValue(AbstractReferenceFinder.ContainingMemberInfoPropertyName, out var containingMemberInfo))
                {
                    result.ContainingMemberName = containingMemberInfo;
                }
            }
            /*
            var referenceLocation = await ProtocolConversions.DocumentSpanToLocationAsync(reference.SourceSpan, cancellationToken).ConfigureAwait(false);
            var classifiedText = new ClassifiedTextElement(classifiedSpans.Select(cspan => new ClassifiedTextRun(cspan.ClassificationType, docText.ToString(cspan.TextSpan))));
            var locationWithText = new LSP.LocationWithText { Range = referenceLocation.Range, Uri = referenceLocation.Uri, Text = classifiedText };
            result.ClassifiedContext = 
            */
            var mappedDocumentSpan = await TryMapAndGetFirstAsync(documentSpan, sourceText, token).ConfigureAwait(false);
            if (mappedDocumentSpan.HasValue)
            {
                var location = mappedDocumentSpan.Value;
                result.PersistentSpan = result.Source.ServiceProvider.PersistentSpanFactory.Create(
                    documentSpan.Document.FilePath,
                    location.LinePositionSpan.Start.Line,
                    location.LinePositionSpan.Start.Character,
                    location.LinePositionSpan.End.Line,
                    location.LinePositionSpan.End.Character,
                    SpanTrackingMode.EdgeInclusive);
            }
        }

        public IPersistentSpan PersistentSpan { get; private set; }

        public ClassifiedTextElement ClassifiedDefinition { get; private set; }

        public Span HighlightSpan { get; private set; }

        public string DocumentId { get; private set; }

        public string ProjectId { get; private set; }

        public ClassifiedTextElement ClassifiedContext { get; private set; }

        public ImageId DefinitionIcon { get; private set; }

        public string ProjectName { get; private set; }

        public string Kind { get; private set; }

        public string ContainingTypeName { get; private set; }

        public string ContainingMemberName { get; private set; }

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
