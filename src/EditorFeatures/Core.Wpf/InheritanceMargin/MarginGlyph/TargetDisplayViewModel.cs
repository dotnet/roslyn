// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    /// <summary>
    /// View model used to show the MenuItem for inheritance target.
    /// </summary>
    internal class TargetDisplayViewModel
    {
        /// <summary>
        /// DefinitionItem used for navigation.
        /// </summary>
        public DefinitionItem DefinitionItem { get; }

        /// <summary>
        /// Display name for the target.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// ImageMoniker indicate the relationship to the original member.
        /// </summary>
        public ImageMoniker ImageMoniker { get; }

        /// <summary>
        /// ToolTip for the MenuItem.
        /// </summary>
        public string ToolTip { get; }

        /// <summary>
        /// AutomationName for the MenuItem.
        /// </summary>
        public string AutomationName { get; }

        public TargetDisplayViewModel(InheritanceTargetItem target)
        {
            var targetName = target.DefinitionItem.DisplayParts.JoinText();
            DisplayName = targetName;
            ImageMoniker = InheritanceMarginHelpers.GetMoniker(target.RelationToMember);
            ToolTip = string.Format(EditorFeaturesWpfResources.Navigate_to_0, targetName);
            AutomationName = ToolTip;
            DefinitionItem = target.DefinitionItem;
        }
    }
}
