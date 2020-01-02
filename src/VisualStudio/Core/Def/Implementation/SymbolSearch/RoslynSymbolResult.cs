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
using Microsoft.VisualStudio.LanguageServices.FindUsages;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal class RoslynSymbolResult : SymbolSearchResult, IResultWithFileLocation, IResultWithContext, IResultWithNamedProject, IResultWithVSGuids, IResultWithReferenceOrDefinition, IResultWithIcon, IResultWithCustomData /* takes care of containin member, containing type and kind */
    {
        private DefinitionItem DefinitionItem { get; set; }
        private SourceReferenceItem ReferenceItem { get; set; }
        public SymbolSearchContext Context { get; }
        private RoslynSymbolSource SourceInternal { get; }
        private string PlainText { get; set; }

        private RoslynSymbolResult(SymbolSearchContext context, string plainText) : base(context.SymbolSource, context.LocalOrigin, plainText)
        {
            this.Context = context;
            this.SourceInternal = context.SymbolSource;
        }

        internal static async Task<RoslynSymbolResult> MakeAsync(SymbolSearchContext context, DefinitionItem definition, DocumentSpan documentSpan, CancellationToken token)
        {
            return await MakeResultAsync(context, documentSpan, referenceItem: null, definitionItem: definition, cancellationToken: token).ConfigureAwait(false);
        }

        internal static async Task<RoslynSymbolResult> MakeAsync(SymbolSearchContext context, SourceReferenceItem reference, DocumentSpan documentSpan, CancellationToken token)
        {
            return await MakeResultAsync(context, documentSpan, referenceItem: reference, definitionItem: reference.Definition, cancellationToken: token).ConfigureAwait(false);
        }

        private static async Task<RoslynSymbolResult> MakeResultAsync(SymbolSearchContext context, DocumentSpan documentSpan, SourceReferenceItem referenceItem, DefinitionItem definitionItem, CancellationToken cancellationToken)
        {
            var (projectGuid, documentGuid, projectName, sourceText) = await FindUsagesUtilities.GetGuidAndProjectNameAndSourceTextAsync(documentSpan.Document, cancellationToken)
                .ConfigureAwait(false);

            var (excerptResult, lineText) = await FindUsagesUtilities.ExcerptAsync(sourceText, documentSpan, cancellationToken).ConfigureAwait(false);
            var plainText = excerptResult.Content.ToString();
            var result = new RoslynSymbolResult(context, plainText);

            result.ProjectGuid = projectGuid.ToString();
            result.DocumentGuid = documentGuid.ToString();
            result.ProjectName = projectName;

            if (referenceItem != null)
            {
                var classifiedSpansAndHighlightSpan = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(referenceItem.SourceSpan, cancellationToken).ConfigureAwait(false);
                var classifiedSpans = classifiedSpansAndHighlightSpan.ClassifiedSpans;
                var docText = await referenceItem.SourceSpan.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                result.ContextText = new ClassifiedTextElement(classifiedSpans.Select(cspan => new ClassifiedTextRun(cspan.ClassificationType, docText.ToString(cspan.TextSpan))));
                result.ContextHighlightSpan = AsSpan(classifiedSpansAndHighlightSpan.HighlightSpan);

                if (referenceItem.AdditionalProperties.TryGetValue(AbstractReferenceFinder.ContainingTypeInfoPropertyName, out var containingTypeInfo))
                {
                    result.ContainingTypeName = containingTypeInfo;
                }

                if (referenceItem.AdditionalProperties.TryGetValue(AbstractReferenceFinder.ContainingMemberInfoPropertyName, out var containingMemberInfo))
                {
                    result.ContainingMemberName = containingMemberInfo;
                }

                // Get read\written information
                if (referenceItem.IsWrittenTo)
                {
                    result.IsWrittenTo = true;
                }
                else
                {
                    result.IsReadFrom = true;
                }

                // Override read\writtern information from SymbolUsageInfo
                if (referenceItem.SymbolUsageInfo.IsWrittenTo() && referenceItem.SymbolUsageInfo.IsReadFrom())
                {
                    result.IsReadFrom = true;
                    result.IsWrittenTo = true;
                }
                else if (referenceItem.SymbolUsageInfo.IsWrittenTo())
                {
                    result.IsReadFrom = false;
                    result.IsWrittenTo = true;
                }
                else if (referenceItem.SymbolUsageInfo.IsReadFrom())
                {
                    result.IsReadFrom = true;
                    result.IsWrittenTo = false;
                }

                var definitionResult = context.GetDefinitionResult(referenceItem.Definition);
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
            else if (definitionItem != null)
            {
                result.IsDefinition = true;
                if (definitionItem.DisplayableProperties.TryGetValue(AbstractReferenceFinder.ContainingTypeInfoPropertyName, out var containingTypeInfo))
                {
                    result.ContainingTypeName = containingTypeInfo;
                }

                if (definitionItem.DisplayableProperties.TryGetValue(AbstractReferenceFinder.ContainingMemberInfoPropertyName, out var containingMemberInfo))
                {
                    result.ContainingMemberName = containingMemberInfo;
                }
                result.Kind = "Definition";

                result.Icon = new ImageElement(definitionItem.Tags.GetFirstGlyph().GetImageId());

                // TODO: Use DocumentSpanEntry
                result.ContextText = new ClassifiedTextElement(excerptResult.ClassifiedSpans.Select(cspan =>
                    new ClassifiedTextRun(cspan.ClassificationType, sourceText.ToString(cspan.TextSpan))));

                result.ContextHighlightSpan = AsSpan(excerptResult.MappedSpan);
                // TODO: add definition context and its highlight
            }

            // Set location so that Editor can navigate to the result
            var mappedDocumentSpan = await FindUsagesUtilities.TryMapAndGetFirstAsync(documentSpan, sourceText, cancellationToken).ConfigureAwait(false);
            if (mappedDocumentSpan.HasValue)
            {
                var location = mappedDocumentSpan.Value;

                result.RelativePath = documentSpan.Document.FilePath;
                result.LineNumber = location.LinePositionSpan.Start.Line;
                result.CharacterNumber = location.LinePositionSpan.Start.Character;

                result.PersistentSpan = context.SymbolSource.ServiceProvider.PersistentSpanFactory.Create(
                    documentSpan.Document.FilePath,
                    location.LinePositionSpan.Start.Line,
                    location.LinePositionSpan.Start.Character,
                    location.LinePositionSpan.End.Line,
                    location.LinePositionSpan.End.Character,
                    SpanTrackingMode.EdgeInclusive);
            }

            return result;
        }

        private static Span AsSpan(TextSpan sourceSpan)
        {
            return new Span(sourceSpan.Start, sourceSpan.Length);
        }

        public string RelativePath { get; private set; }

        public int LineNumber { get; private set; }

        public int CharacterNumber { get; private set; }

        public IPersistentSpan PersistentSpan { get; private set; }

        public ClassifiedTextElement ClassifiedDefinition { get; private set; }

        public Span ContextHighlightSpan { get; private set; }

        public string DocumentGuid { get; private set; }

        public string ProjectGuid { get; private set; }

        public ClassifiedTextElement ContextText { get; private set; }

        public ImageElement Icon { get; private set; }

        public string ProjectName { get; private set; }

        public SymbolSearchResult Definition { get; private set; }

        public bool IsDefinition { get; private set; }

        // Private, because these fields are accessed from TryGetValue until LSP spec is finalized

        private string Kind { get; set; }

        private bool IsReadFrom { get; set; }

        private bool IsWrittenTo { get; set; }

        private string ContainingTypeName { get; set; }

        private string ContainingMemberName { get; set; }

        /// <summary>
        /// Allows us to pass data whose API hasn't been fully fleshed out in the LSP standard
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string key, out object value)
        {
            switch (key)
            {
                case "ContainingTypeName":
                    value = ContainingTypeName;
                    return true;
                case "ContainingMemberName":
                    value = ContainingMemberName;
                    return true;
                case "IsReadFrom":
                    value = IsReadFrom;
                    return true;
                case "IsWrittenTo":
                    value = IsWrittenTo;
                    return true;
                case "Kind":
                    value = Kind;
                    return true;
            }
            value = null;
            return false;
        }
    }
}
