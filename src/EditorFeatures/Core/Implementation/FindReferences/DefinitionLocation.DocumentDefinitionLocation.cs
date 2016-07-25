// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    internal partial class DefinitionLocation
    {
        private sealed class DocumentDefinitionLocation : DefinitionLocation
        {
            private readonly DocumentLocation _location;
            private readonly ImmutableArray<TaggedText> _originationParts;

            public DocumentDefinitionLocation(
                DocumentLocation location, 
                ImmutableArray<TaggedText> originationParts)
            {
                _location = location;

                _originationParts = originationParts.IsDefault
                    ? ImmutableArray.Create(new TaggedText(TextTags.Text, _location.Document.Project.Name))
                    : originationParts;
            }

            public override ImmutableArray<TaggedText> OriginationParts => _originationParts;

            public override bool CanNavigateTo()
            {
                return _location.CanNavigateTo();
            }

            public override bool TryNavigateTo()
            {
                return _location.TryNavigateTo();
            }
        }
    }
}
