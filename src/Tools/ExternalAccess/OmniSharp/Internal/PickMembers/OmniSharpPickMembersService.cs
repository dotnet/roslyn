// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.PickMembers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PickMembers;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Internal.PickMembers
{
    [Shared]
    [ExportWorkspaceService(typeof(IPickMembersService), ServiceLayer.Host)]
    internal class OmniSharpPickMembersService : IPickMembersService
    {
        private readonly IOmniSharpPickMembersService _omniSharpPickMembersService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OmniSharpPickMembersService(IOmniSharpPickMembersService omniSharpPickMembersService)
        {
            _omniSharpPickMembersService = omniSharpPickMembersService;
        }

        public PickMembersResult PickMembers(string title, ImmutableArray<ISymbol> members, ImmutableArray<PickMembersOption> options = default, bool selectAll = true)
        {
            var result = _omniSharpPickMembersService.PickMembers(title, members, options.IsDefault ? default : options.SelectAsArray(o => new OmniSharpPickMembersOption(o)), selectAll: true);
            return new(result.Members, result.Options.SelectAsArray(o => o.PickMembersOptionInternal), result.SelectedAll);
        }
    }
}
