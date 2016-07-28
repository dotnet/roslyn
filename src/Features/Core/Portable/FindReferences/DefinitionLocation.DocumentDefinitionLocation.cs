// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindReferences
{
    internal partial class DefinitionLocation
    {
        /// <summary>
        /// Implementation of a <see cref="DefinitionLocation"/> that sits on top of a 
        /// <see cref="DocumentLocation"/>.
        /// </summary>
        // Internal for testing purposes only.
        internal sealed class DocumentDefinitionLocation : DefinitionLocation
        {
            public readonly DocumentLocation Location;

            public DocumentDefinitionLocation(DocumentLocation location)
            {
                Location = location;
            }

            /// <summary>
            /// Show the project that this <see cref="DocumentLocation"/> is contained in as the
            /// Origination of this <see cref="DefinitionLocation"/>.
            /// </summary>
            public override ImmutableArray<TaggedText> OriginationParts =>
                ImmutableArray.Create(new TaggedText(TextTags.Text, Location.Document.Project.Name));

            public override bool CanNavigateTo() => true;
            public override bool TryNavigateTo() => Location.TryNavigateTo();
        }
    }
}