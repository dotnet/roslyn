// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.FindReferences
{
    /// <summary>
    /// Information about a symbol's definition that can be displayed in an editor
    /// and used for navigation.
    /// 
    /// Standard implmentations can be obtained through the various <see cref="DefinitionItem"/>.Create
    /// overloads.
    /// 
    /// Subclassing is also supported for scenarios that fall outside the bounds of
    /// these common cases.
    /// </summary>
    internal abstract partial class DefinitionItem
    {
        /// <summary>
        /// Descriptive tags from <see cref="CompletionTags"/>. These tags may influence how the 
        /// item is displayed.
        /// </summary>
        public ImmutableArray<string> Tags { get; }

        /// <summary>
        /// The display text that should be displayed to the user.
        /// </summary>
        public ImmutableArray<TaggedText> DisplayParts { get; }

        /// <summary>
        /// Where the location originally came from (for example, the containing assembly or
        /// project name).  May be used in the presentation of a definition.
        /// </summary>
        public ImmutableArray<TaggedText> OriginationParts { get; }

        /// <summary>
        /// Additional locations to present in the UI.  A definition may have multiple locations 
        /// for cases like partial types/members.
        /// </summary>
        public ImmutableArray<DocumentSpan> SourceSpans { get; }

        /// <summary>
        /// Whether or not this definition should be presented if we never found any references to
        /// it.  For example, when searching for a property, the FindReferences engine will cascade
        /// to the accessors in case any code specifically called those accessors (can happen in 
        /// cross-language cases).  However, in the normal case where there were no calls specifically
        /// to the accessor, we would not want to display them in the UI.  
        /// 
        /// For most definitions we will want to display them, even if no references were found.  
        /// This property allows for this customization in behavior.
        /// </summary>
        public bool DisplayIfNoReferences { get; }

        internal abstract bool IsExternal { get; }

        protected DefinitionItem(
            ImmutableArray<string> tags,
            ImmutableArray<TaggedText> displayParts,
            ImmutableArray<TaggedText> originationParts = default(ImmutableArray<TaggedText>),
            ImmutableArray<DocumentSpan> sourceSpans = default(ImmutableArray<DocumentSpan>),
            bool displayIfNoReferences = true)
        {
            Tags = tags;
            DisplayParts = displayParts;
            OriginationParts = originationParts.NullToEmpty();
            SourceSpans = sourceSpans.NullToEmpty();
            DisplayIfNoReferences = displayIfNoReferences;
        }

        public abstract bool CanNavigateTo();
        public abstract bool TryNavigateTo();

        public static DefinitionItem Create(
            ImmutableArray<string> tags,
            ImmutableArray<TaggedText> displayParts,
            DocumentSpan sourceSpan,
            bool displayIfNoReferences = true)
        {
            return Create(tags, displayParts, ImmutableArray.Create(sourceSpan), displayIfNoReferences);
        }

        public static DefinitionItem Create(
           ImmutableArray<string> tags,
           ImmutableArray<TaggedText> displayParts,
           ImmutableArray<DocumentSpan> sourceSpans,
           bool displayIfNoReferences = true)
        {
            if (sourceSpans.Length == 0)
            {
                throw new ArgumentException($"{nameof(sourceSpans)} cannot be empty.");
            }

            return new DocumentLocationDefinitionItem(
                tags, displayParts, sourceSpans, displayIfNoReferences);
        }

        internal static DefinitionItem CreateMetadataDefinition(
            ImmutableArray<string> tags,
            ImmutableArray<TaggedText> displayParts,
            Solution solution, ISymbol symbol,
            bool displayIfNoReferences = true)
        {
            return new MetadataDefinitionItem(
                tags, displayParts, displayIfNoReferences, solution, symbol);
        }

        public static DefinitionItem CreateNonNavigableItem(
            ImmutableArray<string> tags,
            ImmutableArray<TaggedText> displayParts,
            ImmutableArray<TaggedText> originationParts = default(ImmutableArray<TaggedText>),
            bool displayIfNoReferences = true)
        {
            return new NonNavigatingDefinitionItem(
                tags, displayParts, originationParts, displayIfNoReferences);
        }

        internal static ImmutableArray<TaggedText> GetOriginationParts(ISymbol symbol)
        {
            // We don't show an origination location for a namespace because it can span over
            // both metadata assemblies and source projects.
            //
            // Otherwise show the assembly this symbol came from as the Origination of
            // the DefinitionItem.
            if (symbol.Kind != SymbolKind.Namespace)
            {
                var assemblyName = symbol.ContainingAssembly?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    return ImmutableArray.Create(new TaggedText(TextTags.Assembly, assemblyName));
                }
            }

            return ImmutableArray<TaggedText>.Empty;
        }
    }
}