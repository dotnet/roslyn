// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog;

internal class PullMemberUpDialogViewModel : AbstractNotifyPropertyChanged
{
    public bool OkButtonEnabled { get => _okButtonEnabled; set => SetProperty(ref _okButtonEnabled, value, nameof(OkButtonEnabled)); }
    public bool? SelectAllCheckBoxState { get => _selectAllCheckBoxState; set => SetProperty(ref _selectAllCheckBoxState, value, nameof(SelectAllCheckBoxState)); }
    public bool SelectAllCheckBoxThreeStateEnable { get => _selectAllCheckBoxThreeStateEnable; set => SetProperty(ref _selectAllCheckBoxThreeStateEnable, value, nameof(SelectAllCheckBoxThreeStateEnable)); }
    public string SelectAllCheckBoxAutomationText => ServicesVSResources.Select_All;
    public string DestinationTreeViewAutomationText => ServicesVSResources.Select_destination;
    public string SelectMemberListViewAutomationText => ServicesVSResources.Select_member;

    private bool _selectAllCheckBoxThreeStateEnable;
    private bool? _selectAllCheckBoxState;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
    private readonly ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> _symbolToDependentsMap;
    private bool _okButtonEnabled;

    public PullMemberUpDialogViewModel(
        IUIThreadOperationExecutor uiThreadOperationExecutor,
        ImmutableArray<MemberSymbolViewModel> members,
        BaseTypeTreeNodeViewModel destinationTreeViewModel,
        ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> dependentsMap)
    {
        _uiThreadOperationExecutor = uiThreadOperationExecutor;
        _symbolToDependentsMap = dependentsMap;

        MemberSelectionViewModel = new MemberSelectionViewModel(
            _uiThreadOperationExecutor,
            members,
            _symbolToDependentsMap);

        MemberSelectionViewModel.PropertyChanged += (s, e)
            =>
            {
                if (e.PropertyName == nameof(MemberSelectionViewModel.CheckedMembers))
                {
                    EnableOrDisableOkButton();
                }
            };

        DestinationTreeNodeViewModel = destinationTreeViewModel;
        _selectedDestination = destinationTreeViewModel;
        EnableOrDisableOkButton();
    }

    public BaseTypeTreeNodeViewModel DestinationTreeNodeViewModel { get; }

    public MemberSelectionViewModel MemberSelectionViewModel { get; }

    private BaseTypeTreeNodeViewModel _selectedDestination;
    public BaseTypeTreeNodeViewModel SelectedDestination
    {
        get => _selectedDestination;
        set
        {
            if (SetProperty(ref _selectedDestination, value))
            {
                MemberSelectionViewModel.UpdateMembersBasedOnDestinationKind(_selectedDestination.Symbol.TypeKind);
                EnableOrDisableOkButton();
            }
        }
    }

    public PullMembersUpOptions CreatePullMemberUpOptions()
    {
        var selectedOptionFromDialog = MemberSelectionViewModel.GetSelectedMembers();
        var options = PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(
            SelectedDestination.Symbol,
            selectedOptionFromDialog);
        return options;
    }
    private void EnableOrDisableOkButton()
    {
        var selectedMembers = MemberSelectionViewModel.CheckedMembers;
        OkButtonEnabled = SelectedDestination != DestinationTreeNodeViewModel && selectedMembers.Any();
    }
}
