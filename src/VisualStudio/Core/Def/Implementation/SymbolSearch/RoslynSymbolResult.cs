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
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch.Capabilities;
using Microsoft.VisualStudio.LanguageServices.FindUsages;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal class RoslynSymbolResult : SymbolSearchResult, IResultInLocalFile, IResultWithClassifiedContext, IResultInNamedProject, IResultWithVSGuids, IResultWithKind, IResultInNamedCode, IResultWithReferenceDefinitionRelationship, IResultWithIcon
    {
        private DefinitionItem DefinitionItem { get; set; }
        private SourceReferenceItem ReferenceItem { get; set; }
        public SymbolSearchContext Context { get; }
        private RoslynSymbolSource SourceInternal { get; }
        private string PlainText { get; set; }

        public override ISymbolOrigin Origin { get; }
        public override ISymbolSource Source => SourceInternal;
        public override string Text => PlainText;

        private RoslynSymbolResult(SymbolSearchContext context)
        {
            this.Context = context;
            this.SourceInternal = context.SymbolSource;
            this.Origin = context.RootSymbolOrigin;
        }

        internal static async Task<RoslynSymbolResult> MakeAsync(SymbolSearchContext context, DefinitionItem definition, DocumentSpan documentSpan, CancellationToken token)
        {
            var result = new RoslynSymbolResult(context);
            result.DefinitionItem = definition;
            await MakeContent(result, documentSpan, token).ConfigureAwait(false);
            return result;
        }

        internal static async Task<RoslynSymbolResult> MakeAsync(SymbolSearchContext context, SourceReferenceItem reference, DocumentSpan documentSpan, CancellationToken token)
        {
            var result = new RoslynSymbolResult(context);
            result.ReferenceItem = reference;
            result.DefinitionItem = reference.Definition;
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

            if (result.ReferenceItem != null)
            {
                var classifiedSpansAndHighlightSpan = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(result.ReferenceItem.SourceSpan, token).ConfigureAwait(false);
                var classifiedSpans = classifiedSpansAndHighlightSpan.ClassifiedSpans;
                var docText = await result.ReferenceItem.SourceSpan.Document.GetTextAsync(token).ConfigureAwait(false);
                result.ClassifiedContext = new ClassifiedTextElement(classifiedSpans.Select(cspan => new ClassifiedTextRun(cspan.ClassificationType, docText.ToString(cspan.TextSpan))));
                result.HighlightSpan = AsSpan(classifiedSpansAndHighlightSpan.HighlightSpan);

                if (result.ReferenceItem.AdditionalProperties.TryGetValue(AbstractReferenceFinder.ContainingTypeInfoPropertyName, out var containingTypeInfo))
                {
                    result.ContainingTypeName = containingTypeInfo;
                }

                if (result.ReferenceItem.AdditionalProperties.TryGetValue(AbstractReferenceFinder.ContainingMemberInfoPropertyName, out var containingMemberInfo))
                {
                    result.ContainingMemberName = containingMemberInfo;
                }

                if (result.ReferenceItem.SymbolUsageInfo.IsWrittenTo())
                {
                    result.IsWrittenTo = true;
                }
                else if (result.ReferenceItem.SymbolUsageInfo.IsReadFrom())
                {
                    result.IsReadFrom = true;
                }

                var definitionResult = result.Context.GetDefinitionResult(result.ReferenceItem.Definition);
                if (definitionResult != null)
                {
                    result.Definition = definitionResult;
                }
                else
                {
                    System.Diagnostics.Debugger.Break();
                    // I assumed that definitions are profferred first, but we just received a reference before getting a definition.
                    // In this case, create a queue of definitions that need to have their Results realized.
                }
            }
            else if (result.DefinitionItem != null)
            {
                result.IsDefinition = true;
                if (result.DefinitionItem.DisplayableProperties.TryGetValue(AbstractReferenceFinder.ContainingTypeInfoPropertyName, out var containingTypeInfo))
                {
                    result.ContainingTypeName = containingTypeInfo;
                }

                if (result.DefinitionItem.DisplayableProperties.TryGetValue(AbstractReferenceFinder.ContainingMemberInfoPropertyName, out var containingMemberInfo))
                {
                    result.ContainingMemberName = containingMemberInfo;
                }
                result.Kind = "Definition";

                result.Icon = new ImageElement(result.DefinitionItem.Tags.GetFirstGlyph().GetImageId());

                // TODO: Use DocumentSpanEntry
                result.ClassifiedContext = new ClassifiedTextElement(excerptResult.ClassifiedSpans.Select(cspan =>
                    new ClassifiedTextRun(cspan.ClassificationType, sourceText.ToString(cspan.TextSpan))));

                result.HighlightSpan = AsSpan(excerptResult.MappedSpan);
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

        public ImageElement Icon { get; private set; }

        public string ProjectName { get; private set; }

        public string Kind { get; private set; }

        public string ContainingTypeName { get; private set; }

        public string ContainingMemberName { get; private set; }

        public SymbolSearchResult Definition { get; private set; }

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
