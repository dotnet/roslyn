﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal partial class DefinitionItem
    {
        /// <summary>
        /// Implementation of a <see cref="DefinitionItem"/> that sits on top of a 
        /// <see cref="DocumentSpan"/>.
        /// </summary>
        // internal for testing purposes.
        internal sealed class DocumentLocationDefinitionItem : DefinitionItem
        {
            internal override bool IsExternal => false;

            public DocumentLocationDefinitionItem(
                ImmutableArray<string> tags,
                ImmutableArray<TaggedText> displayParts,
                ImmutableArray<TaggedText> nameDisplayParts,
                ImmutableArray<DocumentSpan> sourceSpans,
                ImmutableDictionary<string, string> properties,
                bool displayIfNoReferences)
                : base(tags, displayParts, nameDisplayParts,
                       ImmutableArray.Create(new TaggedText(TextTags.Text, sourceSpans[0].Document.Project.Name)),
                       sourceSpans, properties, displayIfNoReferences)
            {
            }

            public override bool CanNavigateTo() => SourceSpans[0].CanNavigateTo();
            public override bool TryNavigateTo() => SourceSpans[0].TryNavigateTo();
        }
    }
}