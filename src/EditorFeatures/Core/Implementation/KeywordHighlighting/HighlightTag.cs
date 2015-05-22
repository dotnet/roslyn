// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
{
    internal class HighlightTag : AbstractNavigatableReferenceHighlightingTag
    {
        internal const string TagId = "MarkerFormatDefinition/HighlightedReference";

        public static readonly HighlightTag Instance = new HighlightTag();

        private HighlightTag()
            : base(TagId)
        {
        }
    }
}
