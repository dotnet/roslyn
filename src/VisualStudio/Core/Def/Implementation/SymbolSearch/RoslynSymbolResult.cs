// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch.Capabilities;
using Microsoft.VisualStudio.LanguageServices.FindUsages;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal class RoslynSymbolResult : SymbolSearchResult, IResultInLocalFile, /*IResultWithDecoratedDefinition,*/ IResultWithClassifiedContext, IResultInNamedProject, IResultWithVSGuids, IResultWithKind, IResultInNamedCode, IResultWithReferenceDefinitionRelationship
    {
        private DefinitionItem Definition { get; set; }
        private SourceReferenceItem Reference { get; set; }
        private RoslynSymbolSource SourceInternal { get; }
        private string PlainText { get; set; }

        public override ISymbolOrigin Origin { get; }
        public override ISymbolSource Source => SourceInternal;
        public override string Text => PlainText;

        private RoslynSymbolResult(RoslynSymbolSource source, ISymbolOrigin origin)
        {
            this.SourceInternal = source;
            this.Origin = origin;
        }

        internal static async Task<RoslynSymbolResult> MakeAsync(SymbolSearchContext context, DefinitionItem definition, DocumentSpan documentSpan, CancellationToken token)
        {
            var result = new RoslynSymbolResult(context.SymbolSource, context.RootSymbolOrigin);
            result.Definition = definition;
            await MakeContent(result, documentSpan, token).ConfigureAwait(false);
            return result;
        }

        internal static async Task<RoslynSymbolResult> MakeAsync(SymbolSearchContext context, SourceReferenceItem reference, DocumentSpan documentSpan, CancellationToken token)
        {
            // TODO: Understand how Roslyn calls GetReferenceGroupsAsync
            // it seems that at this time, we have full knowledge of definitions
            // and their references

            var result = new RoslynSymbolResult(context.SymbolSource, context.RootSymbolOrigin);
            result.Reference = reference;
            result.Definition = reference.Definition;
            await MakeContent(result, documentSpan, token).ConfigureAwait(false);

            return result;
        }

        private static async Task MakeContent(RoslynSymbolResult result, DocumentSpan documentSpan, CancellationToken token)
        {
            var (projectGuid, documentGuid, projectName, sourceText) = await FindUsagesUtilities.GetGuidAndProjectNameAndSourceTextAsync(documentSpan.Document, token)
                .ConfigureAwait(false);

            // GUIDs allow scoping to current project and document
            result.ProjectId = projectGuid.ToString();
            result.DocumentId = documentGuid.ToString();
            result.ProjectName = projectName;

            var (excerptResult, lineText) = await FindUsagesUtilities.ExcerptAsync(sourceText, documentSpan, token).ConfigureAwait(false);
            result.PlainText = excerptResult.Content.ToString();

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

                if (result.Reference.SymbolUsageInfo.IsWrittenTo())
                {
                    result.Kind = "Write";
                    result.IsWrittenTo = true;
                }
                else if (result.Reference.SymbolUsageInfo.IsReadFrom())
                {
                    result.Kind = "Write";
                    result.IsReadFrom = true;
                }
            }
            else if (result.Definition != null)
            {
                result.IsDefinition = true;
                result.DefinitionIcon = result.Definition.Tags.GetFirstGlyph().GetImageId();

                // TODO: Use DocumentSpanEntry
                result.ClassifiedContext = new ClassifiedTextElement(excerptResult.ClassifiedSpans.Select(cspan =>
                    new ClassifiedTextRun(cspan.ClassificationType, sourceText.ToString(cspan.TextSpan))));
            }

            // Set location so that Editor can navigate to the result
            var mappedDocumentSpan = await FindUsagesUtilities.TryMapAndGetFirstAsync(documentSpan, sourceText, token).ConfigureAwait(false);
            if (mappedDocumentSpan.HasValue)
            {
                var location = mappedDocumentSpan.Value;
                result.PersistentSpan = result.SourceInternal.ServiceProvider.PersistentSpanFactory.Create(
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

        SymbolSearchResult IResultWithReferenceDefinitionRelationship.Definition { get; }

        public bool IsDefinition { get; private set; }

        public bool IsReadFrom { get; private set; }

        public bool IsWrittenTo { get; private set; }

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
