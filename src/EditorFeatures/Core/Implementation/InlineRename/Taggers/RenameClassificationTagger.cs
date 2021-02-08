// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal class RenameClassificationTagger : AbstractRenameTagger<IClassificationTag>
    {
        private readonly IClassificationType _classificationType;

        public RenameClassificationTagger(ITextBuffer buffer, InlineRenameService renameService, IClassificationType classificationType)
            : base(buffer, renameService)
        {
            _classificationType = classificationType;
        }

        protected override bool TryCreateTagSpan(SnapshotSpan span, RenameSpanKind type, out TagSpan<IClassificationTag> tagSpan)
        {
            if (type == RenameSpanKind.Reference)
            {
                tagSpan = new TagSpan<IClassificationTag>(span, new ClassificationTag(_classificationType));
                return true;
            }

            tagSpan = null;
            return false;
        }
    }
}
