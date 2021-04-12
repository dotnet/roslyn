// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags
{
    internal class RenameFieldBackgroundAndBorderTag : TextMarkerTag
    {
        // Only used for theming, does not need localized
        internal const string TagId = "RoslynRenameFieldBackgroundAndBorderTag";

        public static readonly RenameFieldBackgroundAndBorderTag Instance = new RenameFieldBackgroundAndBorderTag();

        private RenameFieldBackgroundAndBorderTag()
            : base(TagId)
        {
        }
    }

    // Only used to keep the closed repository building. This will be removed once the
    // closed repository is updated to use RenameFieldBackgroundAndBorderTag.
    internal class ValidTag
    {
        internal const string TagId = "";
    }
}
