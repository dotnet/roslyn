// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags
{
    internal class FixupTag : TextMarkerTag
    {
        internal const string TagId = "RoslynRenameFixupTag";

        public static readonly FixupTag Instance = new FixupTag();

        private FixupTag()
            : base(TagId)
        {
        }
    }
}
