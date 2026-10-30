// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;

/// <summary>
/// View model used to show the MenuItem for inheritance target.
/// </summary>
internal class TargetMenuItemViewModel : MenuItemViewModel
{
    public override bool IsIndented { get; }

    /// <summary>
    /// DefinitionItem used for navigation.
    /// </summary>
    public DetachedDefinitionItem DefinitionItem { get; }

    // Internal for testing purpose
    internal TargetMenuItemViewModel(
        string displayContent,
        ImageMoniker imageMoniker,
        DetachedDefinitionItem definitionItem,
        bool isIndented = true) : base(displayContent, imageMoniker)
    {
        DefinitionItem = definitionItem;
        IsIndented = isIndented;
    }

    public static TargetMenuItemViewModel Create(InheritanceTargetItem target, string displayContent)
        => new(
            displayContent,
            target.Glyph.GetImageMoniker(),
            target.DefinitionItem,
            isIndented: target.RelationToMember != InheritanceRelationship.InheritedImport);
}
