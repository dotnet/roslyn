// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    /// <summary>
    /// View model used to show the MenuItem for inheritance target.
    /// </summary>
    internal class TargetMenuItemViewModel : InheritanceMenuItemViewModel
    {
        /// <summary>
        /// DefinitionItem used for navigation.
        /// </summary>
        public DefinitionItem DefinitionItem { get; }

        private TargetMenuItemViewModel(
            string displayContent,
            ImageMoniker imageMoniker,
            string automationName,
            DefinitionItem definitionItem) : base(displayContent, imageMoniker, automationName)
        {
            DefinitionItem = definitionItem;
        }

        public static TargetMenuItemViewModel Create(InheritanceTargetItem target)
        {
            var targetName = target.DefinitionItem.DisplayParts.JoinText();
            var displayContent = string.Format(ServicesVSResources._0_in_1, targetName, target.DisplayNameForContainingType);
            var imageMoniker = target.Glyph.GetImageMoniker();
            return new TargetMenuItemViewModel(displayContent, imageMoniker, displayContent, target.DefinitionItem);
        }
    }
}
