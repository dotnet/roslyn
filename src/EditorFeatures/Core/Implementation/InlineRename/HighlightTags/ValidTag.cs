// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags
{
    internal class ValidTag : TextMarkerTag
    {
        internal const string TagId = "RoslynRenameValidTag";

        public static readonly ValidTag Instance = new ValidTag();

        private ValidTag()
            : base(TagId)
        {
        }
    }
}
