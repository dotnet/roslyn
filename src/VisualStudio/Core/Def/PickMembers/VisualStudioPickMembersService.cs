// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PickMembers;

[ExportWorkspaceService(typeof(IPickMembersService), ServiceLayer.Host), Shared]
internal sealed class VisualStudioPickMembersService : IPickMembersService
{
    private readonly IGlyphService _glyphService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioPickMembersService(IGlyphService glyphService)
        => _glyphService = glyphService;

    public PickMembersResult PickMembers(
        string title,
        ImmutableArray<ISymbol> members,
        ImmutableArray<PickMembersOption> options,
        bool selectAll)
    {
        options = options.NullToEmpty();

        var viewModel = new PickMembersDialogViewModel(_glyphService, members, options, selectAll);
        var dialog = new PickMembersDialog(viewModel, title);
        var result = dialog.ShowModal();

        if (result == true)
        {
            return new PickMembersResult(
                viewModel.MemberContainers.SelectAsArray(c => c.IsChecked, c => c.Symbol),
                options,
                viewModel.SelectedAll);
        }
        else
        {
            return PickMembersResult.Canceled;
        }
    }
}
