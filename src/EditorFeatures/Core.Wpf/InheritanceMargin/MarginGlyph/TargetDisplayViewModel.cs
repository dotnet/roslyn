// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal class TargetDisplayViewModel
    {
        public DefinitionItem DefinitionItem { get; }
        public string DisplayName { get; }
        public ImageMoniker ImageMoniker { get; }
        public string ToolTip { get; }
        public string AutomationName { get; }

        public TargetDisplayViewModel(InheritanceTargetItem target)
        {
            DisplayName = target.DefinitionItem.DisplayParts.JoinText();
            ImageMoniker = InheritanceMarginHelpers.GetMoniker(target.RelationToMember);

            // TODO: Move this to resources.
            ToolTip = $"Navigate to '{DisplayName}'.";
            AutomationName = ToolTip;

            DefinitionItem = target.DefinitionItem;
        }
    }
}
