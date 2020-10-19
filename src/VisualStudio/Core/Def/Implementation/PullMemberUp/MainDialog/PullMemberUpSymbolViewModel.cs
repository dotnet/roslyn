// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog
{
    internal class PullMemberUpSymbolViewModel : SymbolViewModel<ISymbol>
    {
        private bool _isCheckable;
        private bool _makeAbstract;
        private bool _isMakeAbstractCheckable;
        private string _tooltipText;
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
        public bool MakeAbstract { get => _makeAbstract; set => SetProperty(ref _makeAbstract, value, nameof(MakeAbstract)); }

        /// <summary>
        /// Indicates whether make abstract check box is enabled or not. (e.g. When user selects on interface destination, it will be disabled)
        /// </summary>
        public bool IsMakeAbstractCheckable { get => _isMakeAbstractCheckable; set => SetProperty(ref _isMakeAbstractCheckable, value, nameof(IsMakeAbstractCheckable)); }

        /// <summary>
        /// Indicates whether this member checkable.
        /// </summary>
        public bool IsCheckable { get => _isCheckable; set => SetProperty(ref _isCheckable, value, nameof(IsCheckable)); }

        /// <summary>
        /// Tooltip text, also used as HelpText for screen readers. Should be empty
        /// when no tool tip should be shown
        /// </summary>
        public string TooltipText { get => _tooltipText; set => SetProperty(ref _tooltipText, value, nameof(TooltipText)); }

        /// <summary>
        /// The content of tooltip.
        /// </summary>
        public string Accessibility => Symbol.DeclaredAccessibility.ToString();

        public PullMemberUpSymbolViewModel(ISymbol symbol, IGlyphService glyphService) : base(symbol, glyphService)
        {
        }
    }
}
