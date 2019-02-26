// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.PickMembers
{
    internal interface IPickMembersService : IWorkspaceService
    {
        PickMembersResult PickMembers(
            string title, ImmutableArray<ISymbol> members,
            ImmutableArray<PickMembersOption> options = default);
    }

    internal class PickMembersOption
    {
        public PickMembersOption(string id, string title, bool value)
        {
            Id = id;
            Title = title;
            Value = value;
        }

        public string Id { get; }
        public string Title { get; }
        public bool Value { get; set; }
    }
}
