// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal class TargetDisplayRowViewModel
    {
        public string DisplayContent { get; }

        public ImageMoniker ImageMoniker { get; }

        public string ToolTip { get; }

        public TargetDisplayRowViewModel(InheritanceTargetItem target)
        {
            DisplayContent = target.DefinitionItem.DisplayParts.JoinText();
            ImageMoniker = InheritanceMarginHelpers.GetMoniker(target.RelationToMember);
            ToolTip = $"Navigate to {DisplayContent}.";
        }
    }
}
