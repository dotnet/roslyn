// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal sealed partial class RenameTagger(ITextBuffer buffer, InlineRenameService renameService) : AbstractRenameTagger<ITextMarkerTag>(buffer, renameService)
{
    protected override bool TryCreateTagSpan(SnapshotSpan span, RenameSpanKind type, out TagSpan<ITextMarkerTag> tagSpan)
    {
        ITextMarkerTag tagKind;
        switch (type)
        {
            case RenameSpanKind.Reference:
                tagKind = RenameFieldBackgroundAndBorderTag.Instance;
                break;
            case RenameSpanKind.UnresolvedConflict:
                tagKind = RenameConflictTag.Instance;
                break;
            case RenameSpanKind.Complexified:
                tagKind = RenameFixupTag.Instance;
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(type);
        }

        tagSpan = new TagSpan<ITextMarkerTag>(span, tagKind);
        return true;
    }
}
