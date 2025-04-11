// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers;

internal sealed class StaticMemberSelectionViewModel : AbstractNotifyPropertyChanged
{
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
    private readonly ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> _symbolToDependentsMap;
    private readonly ImmutableDictionary<ISymbol, SymbolViewModel<ISymbol>> _symbolToMemberViewMap;

    public StaticMemberSelectionViewModel(
        IUIThreadOperationExecutor uiThreadOperationExecutor,
        ImmutableArray<SymbolViewModel<ISymbol>> members,
        ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> dependentsMap)
    {
        _uiThreadOperationExecutor = uiThreadOperationExecutor;
        _members = members;
        _symbolToDependentsMap = dependentsMap;
        _symbolToMemberViewMap = members.ToImmutableDictionary(memberViewModel => memberViewModel.Symbol);
    }

    public ImmutableArray<SymbolViewModel<ISymbol>> CheckedMembers => Members.WhereAsArray(m => m.IsChecked);

    private ImmutableArray<SymbolViewModel<ISymbol>> _members;
    public ImmutableArray<SymbolViewModel<ISymbol>> Members
    {
        get => _members;
        set => SetProperty(ref _members, value);
    }

    public void SelectAll()
        => SelectMembers(Members);

    internal void DeselectAll()
        => SelectMembers(Members, isChecked: false);

    public void SelectDependents()
    {
        var checkedMembers = Members
            .WhereAsArray(member => member.IsChecked);

        var result = _uiThreadOperationExecutor.Execute(
            title: ServicesVSResources.Move_static_members_to_another_type_colon,
            defaultDescription: ServicesVSResources.Calculating_dependents,
            allowCancellation: true,
            showProgress: true,
            context =>
            {
                foreach (var member in Members)
                {
                    _symbolToDependentsMap[member.Symbol].Wait(context.UserCancellationToken);
                }
            });

        if (result == UIThreadOperationStatus.Completed)
        {
            foreach (var member in checkedMembers)
            {
                var membersToSelected = FindDependents(member.Symbol).SelectAsArray(symbol => _symbolToMemberViewMap[symbol]);
                SelectMembers(membersToSelected);
            }
        }
    }

    private static void SelectMembers(ImmutableArray<SymbolViewModel<ISymbol>> members, bool isChecked = true)
    {
        foreach (var member in members)
        {
            member.IsChecked = isChecked;
        }
    }

    private ImmutableHashSet<ISymbol> FindDependents(ISymbol member)
    {
        var queue = new Queue<ISymbol>();
        // Under situation like two methods call each other, this hashset is used to 
        // prevent the infinity loop.
        var visited = new HashSet<ISymbol>();
        var result = new HashSet<ISymbol>();
        queue.Enqueue(member);
        visited.Add(member);
        while (!queue.IsEmpty())
        {
            var currentMember = queue.Dequeue();
            result.Add(currentMember);
            visited.Add(currentMember);
            foreach (var dependent in _symbolToDependentsMap[currentMember].Result)
            {
                if (!visited.Contains(dependent))
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        return [.. result];
    }
}
