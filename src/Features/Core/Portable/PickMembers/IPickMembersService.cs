// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.PickMembers
{
    internal interface IPickMembersService : IWorkspaceService
    {
        PickMembersResult PickMembers(
            string title, ImmutableArray<ISymbol> members,
            ImmutableArray<PickMembersOption> options = default,
            bool selectAll = true);
    }

    internal class PickMembersOption(string id, string title, bool value)
    {
        public string Id { get; } = id;
        public string Title { get; } = title;
        public bool Value { get; set; } = value;
    }
}
