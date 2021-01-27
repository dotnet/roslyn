﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags
{
    internal class RenameFixupTag : TextMarkerTag
    {
        // Only used for theming, does not need localized
        internal const string TagId = "RoslynRenameFixupTag";

        public static readonly RenameFixupTag Instance = new RenameFixupTag();

        private RenameFixupTag()
            : base(TagId)
        {
        }
    }
}
