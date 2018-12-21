// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using static Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface.ExtractInterfaceDialogViewModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog
{
    internal class PullMemberUpSymbolViewModel : MemberSymbolViewModel
    {
        /// <summary>
        /// Property controls the 'Make abstract' check box's Visibility.
        /// The check box is hidden for members impossbile to be made to abstract.
        /// </summary>
        public Visibility MakeAbstractVisibility => MemberSymbol.Kind == SymbolKind.Field || MemberSymbol.IsAbstract ? Visibility.Hidden : Visibility.Visible;

        private bool _makeAbstract;

        /// <summary>
        /// Property indicates whether 'Make abstract' check box is checked.
        /// </summary>
        public bool MakeAbstract { get => _makeAbstract; set => SetProperty(ref _makeAbstract, value, nameof(MakeAbstract)); }

        private bool _isCheckable;

        /// <summary>
        /// Property indicates whether this member checkable.
        /// </summary>
        public bool IsCheckable { get => _isCheckable; set => SetProperty(ref _isCheckable, value, nameof(IsCheckable)); }

        /// <summary>
        /// The content of tooltip.
        /// </summary>
        public string Accessibility => MemberSymbol.DeclaredAccessibility.ToString();

        public PullMemberUpSymbolViewModel(IGlyphService glyphService, ISymbol symbol) : base(symbol, glyphService)
        {
        }
    }
}
