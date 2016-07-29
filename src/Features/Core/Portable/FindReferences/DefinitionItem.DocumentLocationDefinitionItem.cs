// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindReferences
{
    internal partial class DefinitionItem
    {
        /// <summary>
        /// Implementation of a <see cref="DefinitionItem"/> that sits on top of a 
        /// <see cref="DocumentLocation"/>.
        /// </summary>
        // internal for testing purposes.
        internal sealed class DocumentLocationDefinitionItem : DefinitionItem
        {
            public readonly DocumentLocation Location;

            public DocumentLocationDefinitionItem(
                ImmutableArray<string> tags,
                ImmutableArray<TaggedText> displayParts,
                ImmutableArray<DocumentLocation> additionalLocations,
                bool displayIfNoReferences,
                DocumentLocation location)
                : base(tags, displayParts, 
                      ImmutableArray.Create(new TaggedText(TextTags.Text, location.Document.Project.Name)),
                      additionalLocations, displayIfNoReferences)
            {
                Location = location;
            }

            public override bool CanNavigateTo() => Location.CanNavigateTo();
            public override bool TryNavigateTo() => Location.TryNavigateTo();
        }
    }
}