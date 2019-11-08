// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PickMembers
{
    [ExportWorkspaceService(typeof(IPickMembersService), ServiceLayer.Host), Shared]
    internal class VisualStudioPickMembersService : IPickMembersService
    {
        private readonly IGlyphService _glyphService;

        [ImportingConstructor]
        public VisualStudioPickMembersService(IGlyphService glyphService)
        {
            _glyphService = glyphService;
        }

        public PickMembersResult PickMembers(
            string title, ImmutableArray<ISymbol> members, ImmutableArray<PickMembersOption> options)
        {
            options = options.NullToEmpty();

            var viewModel = new PickMembersDialogViewModel(_glyphService, members, options);
            var dialog = new PickMembersDialog(viewModel, title);
            var result = dialog.ShowModal();

            if (result is { HasValue: true, Value: true })
            {
                return new PickMembersResult(
                    viewModel.MemberContainers.Where(c => c.IsChecked)
                                              .Select(c => c.Symbol)
                                              .ToImmutableArray(),
                    options);
            }
            else
            {
                return PickMembersResult.Canceled;
            }
        }
    }
}
