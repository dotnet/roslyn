// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Wpf;
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
        /// Display content for the target.
        /// </summary>
        public string DisplayContent { get; }

        /// <summary>
        /// ImageMoniker shown before the display name.
        /// </summary>
        public ImageMoniker ImageMoniker { get; }

        /// <summary>
        /// AutomationName for the MenuItem.
        /// </summary>
        public string AutomationName { get; }

        public TargetDisplayViewModel(InheritanceTargetItem target)
        {
            var targetName = target.DefinitionItem.DisplayParts.JoinText();
            DisplayContent = string.Format(EditorFeaturesWpfResources._0_in_1, targetName, target.DisplayNameForContainingType);
            ImageMoniker = target.Glyph.GetImageMoniker();
            AutomationName = DisplayContent;
            DefinitionItem = target.DefinitionItem;
        }
    }
}
