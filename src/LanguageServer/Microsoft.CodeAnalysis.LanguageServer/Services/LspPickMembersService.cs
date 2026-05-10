// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PickMembers;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

[ExportWorkspaceService(typeof(IPickMembersService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspPickMembersService() : IPickMembersService
{
    public PickMembersResult PickMembers(
        string title,
        ImmutableArray<ISymbol> members,
        ImmutableArray<PickMembersOption> options = default,
        bool selectAll = true)
        => new(members, options.IsDefault ? [] : options, selectAll);
}
