// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Reflection;
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
    internal class TargetMenuItemViewModel : MenuItemViewModel
    {
        /// <summary>
        /// DefinitionItem used for navigation.
        /// </summary>
        public DefinitionItem.DetachedDefinitionItem DefinitionItem { get; }

        // Internal for testing purpose
        internal TargetMenuItemViewModel(
            string displayContent,
            ImageMoniker imageMoniker,
            DefinitionItem.DetachedDefinitionItem definitionItem) : base(displayContent, imageMoniker)
        {
            DefinitionItem = definitionItem;
        }

        public static TargetMenuItemViewModel Create(InheritanceTargetItem target, bool includeProjectName)
            => new(
                GetDisplayContent(target, includeProjectName),
                target.Glyph.GetImageMoniker(),
                target.DefinitionItem);

        private static string GetDisplayContent(InheritanceTargetItem target, bool includeProjectName)
        {
            return includeProjectName && target.ProjectName != null
                ? string.Format(FeaturesResources._0_1, target.DisplayName, target.ProjectName)
                : target.DisplayName;
        }
    }
}
