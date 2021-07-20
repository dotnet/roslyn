// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembersToType
{
    internal class MoveMembersToTypeDialogViewModel : AbstractNotifyPropertyChanged
    {

        private bool _selectAllCheckBoxThreeStateEnable;
        public bool SelectAllCheckBoxThreeStateEnable { get => _selectAllCheckBoxThreeStateEnable; set => SetProperty(ref _selectAllCheckBoxThreeStateEnable, value, nameof(SelectAllCheckBoxThreeStateEnable)); }

        private bool? _selectAllCheckBoxState;
        public bool? SelectAllCheckBoxState { get => _selectAllCheckBoxState; set => SetProperty(ref _selectAllCheckBoxState, value, nameof(SelectAllCheckBoxState)); }

        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
        private readonly ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> _symbolToDependentsMap;

        private bool _canSubmit = true;
        public bool CanSubmit { get => _canSubmit; private set => SetProperty(ref _canSubmit, value); }

        public NewTypeDestinationSelectionViewModel DestinationViewModel { get; }
        public MemberSelectionViewModel _memberSelectionViewModel { get; }

        public MoveMembersToTypeDialogViewModel(
            IUIThreadOperationExecutor uiThreadOperationExecutor,
            ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> dependentsMap)
        {
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
            _symbolToDependentsMap = dependentsMap;
        }
    }
}
