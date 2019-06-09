// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags
{
    internal class RenameConflictTag : TextMarkerTag
    {
        // Only used for theming, does not need localized
        internal const string TagId = "RoslynRenameConflictTag";

        public static readonly RenameConflictTag Instance = new RenameConflictTag();

        private RenameConflictTag()
            : base(TagId)
        {
        }
    }
}
