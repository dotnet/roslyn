// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal sealed partial class RenameTagger : AbstractRenameTagger<ITextMarkerTag>
    {
        public RenameTagger(ITextBuffer buffer, InlineRenameService renameService)
            : base(buffer, renameService)
        {
        }

        protected override bool TryCreateTagSpan(SnapshotSpan span, RenameSpanKind type, out TagSpan<ITextMarkerTag> tagSpan)
        {
            ITextMarkerTag tagKind;
            switch (type)
            {
                case RenameSpanKind.Reference:
                    tagKind = ValidTag.Instance;
                    break;
                case RenameSpanKind.UnresolvedConflict:
                    tagKind = ConflictTag.Instance;
                    break;
                case RenameSpanKind.Complexified:
                    tagKind = FixupTag.Instance;
                    break;
                default:
                    throw ExceptionUtilities.Unreachable;
            }

            tagSpan = new TagSpan<ITextMarkerTag>(span, tagKind);
            return true;
        }
    }
}
