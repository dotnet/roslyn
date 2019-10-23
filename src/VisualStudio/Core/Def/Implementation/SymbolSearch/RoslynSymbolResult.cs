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

            var (projectGuid, documentGuid, projectName, sourceText) = await FindUsagesUtilities.GetGuidAndProjectNameAndSourceTextAsync(documentSpan.Document, token)
                .ConfigureAwait(false);
            result.ProjectId = projectGuid.ToString();
            result.ProjectName = projectName;
            result.DocumentId = documentGuid.ToString();

            var (excerptResult, lineText) = await FindUsagesUtilities.ExcerptAsync(sourceText, documentSpan, token).ConfigureAwait(false);

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
            var mappedDocumentSpan = await FindUsagesUtilities.TryMapAndGetFirstAsync(documentSpan, sourceText, token).ConfigureAwait(false);
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
    }
}
