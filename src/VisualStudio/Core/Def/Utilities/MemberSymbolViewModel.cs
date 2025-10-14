// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Utilities;

internal sealed class MemberSymbolViewModel : SymbolViewModel<ISymbol>
{
    public string MakeAbstractCheckBoxAutomationText => string.Format(ServicesVSResources.Make_0_abstract, Symbol.Name);
    public string RowSelectionAutomationText => ServicesVSResources.Select_member;

    /// <summary>
    /// Property controls the 'Make abstract' check box's Visibility.
    /// The check box is hidden for members impossbile to be made to abstract.
    /// </summary>
    public Visibility MakeAbstractVisibility => Symbol.Kind == SymbolKind.Field || Symbol.IsAbstract ? Visibility.Hidden : Visibility.Visible;

    /// <summary>
    /// Indicates whether 'Make abstract' check box is checked.
    /// </summary>
    public bool MakeAbstract { get; set => SetProperty(ref field, value, nameof(MakeAbstract)); }

    /// <summary>
    /// Indicates whether make abstract check box is enabled or not. (e.g. When user selects on interface destination, it will be disabled)
    /// </summary>
    public bool IsMakeAbstractCheckable { get; set => SetProperty(ref field, value, nameof(IsMakeAbstractCheckable)); }

    /// <summary>
    /// Indicates whether this member checkable.
    /// </summary>
    public bool IsCheckable { get; set => SetProperty(ref field, value, nameof(IsCheckable)); }

    /// <summary>
    /// Tooltip text, also used as HelpText for screen readers. Should be empty
    /// when no tool tip should be shown
    /// </summary>
    public string TooltipText { get; set => SetProperty(ref field, value, nameof(TooltipText)); }

    /// <summary>
    /// The content of tooltip.
    /// </summary>
    public string Accessibility => Symbol.DeclaredAccessibility.ToString();

    public MemberSymbolViewModel(ISymbol symbol, IGlyphService glyphService) : base(symbol, glyphService)
    {
    }
}
