// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;

/// <summary>
/// The View Model would be used when there are multiple targets with same name in the group.
/// It contains an additional image moniker represents the source language in the UI.
/// </summary>
internal class DisambiguousTargetMenuItemViewModel : TargetMenuItemViewModel
{
    /// <summary>
    /// Icon represents the source language of this target.
    /// </summary>
    public ImageMoniker LanguageMoniker { get; }

    // Internal for testing purpose
    internal DisambiguousTargetMenuItemViewModel(
        string displayContent,
        ImageMoniker imageMoniker,
        DetachedDefinitionItem definitionItem,
        ImageMoniker languageMoniker) : base(displayContent, imageMoniker, definitionItem)
    {
        LanguageMoniker = languageMoniker;
    }

    public static DisambiguousTargetMenuItemViewModel CreateWithSourceLanguageGlyph(
        InheritanceTargetItem target)
    {
        return new(
            target.DisplayName,
            target.Glyph.GetImageMoniker(),
            target.DefinitionItem,
            target.LanguageGlyph.GetImageMoniker());
    }
}
