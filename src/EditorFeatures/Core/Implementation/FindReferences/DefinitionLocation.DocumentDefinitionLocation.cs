// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    internal partial class DefinitionLocation
    {
        private sealed class DocumentDefinitionLocation : DefinitionLocation
        {
            private readonly DocumentLocation _location;

            public DocumentDefinitionLocation(DocumentLocation location)
            {
                _location = location;
            }

            public override ImmutableArray<TaggedText> OriginationParts =>
                ImmutableArray.Create(new TaggedText(TextTags.Text, _location.Document.Project.Name));

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
