﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            if (result.HasValue && result.Value)
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
