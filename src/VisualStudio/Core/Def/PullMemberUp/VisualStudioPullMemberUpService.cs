// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp;

[ExportWorkspaceService(typeof(IPullMemberUpOptionsService), ServiceLayer.Host), Shared]
internal sealed class VisualStudioPullMemberUpService : IPullMemberUpOptionsService
{
    private readonly IGlyphService _glyphService;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioPullMemberUpService(IGlyphService glyphService, IUIThreadOperationExecutor uiThreadOperationExecutor)
    {
        _glyphService = glyphService;
        _uiThreadOperationExecutor = uiThreadOperationExecutor;
    }

    public PullMembersUpOptions GetPullMemberUpOptions(Document document, ImmutableArray<ISymbol> selectedMembers)
    {
        // all selected members must have the same containing type
        var containingType = selectedMembers[0].ContainingType;
        var membersInType = containingType.GetMembers().
            WhereAsArray(MemberAndDestinationValidator.IsMemberValid);
        var memberViewModels = membersInType
            .SelectAsArray(member =>
                new MemberSymbolViewModel(member, _glyphService)
                {
                    // The member user selected will be checked at the beginning.
                    IsChecked = selectedMembers.Any(SymbolEquivalenceComparer.Instance.Equals, member),
                    MakeAbstract = false,
                    IsMakeAbstractCheckable = !member.IsKind(SymbolKind.Field) && !member.IsAbstract,
                    IsCheckable = true
                });

        using var cancellationTokenSource = new CancellationTokenSource();
        var baseTypeRootViewModel = BaseTypeTreeNodeViewModel.CreateBaseTypeTree(
            _glyphService,
            document.Project.Solution,
            containingType,
            cancellationTokenSource.Token);
        var memberToDependentsMap = SymbolDependentsBuilder.FindMemberToDependentsMap(membersInType, document.Project, cancellationTokenSource.Token);
        var viewModel = new PullMemberUpDialogViewModel(_uiThreadOperationExecutor, memberViewModels, baseTypeRootViewModel, memberToDependentsMap);
        var dialog = new PullMemberUpDialog(viewModel);
        var result = dialog.ShowModal();

        // Dialog has finshed its work, cancel finding dependents task.
        cancellationTokenSource.Cancel();
        if (result.GetValueOrDefault())
        {
            return dialog.ViewModel.CreatePullMemberUpOptions();
        }
        else
        {
            return null;
        }
    }
}
