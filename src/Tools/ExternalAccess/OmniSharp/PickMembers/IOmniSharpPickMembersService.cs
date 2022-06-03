// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PickMembers;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.PickMembers
{
    internal interface IOmniSharpPickMembersService
    {
        OmniSharpPickMembersResult PickMembers(
            string title, ImmutableArray<ISymbol> members,
            ImmutableArray<OmniSharpPickMembersOption> options = default,
            bool selectAll = true);
    }

    internal class OmniSharpPickMembersOption
    {
        internal readonly PickMembersOption PickMembersOptionInternal;

        internal OmniSharpPickMembersOption(PickMembersOption pickMembersOption)
        {
            PickMembersOptionInternal = pickMembersOption;
        }

        public string Id => PickMembersOptionInternal.Id;
        public string Title => PickMembersOptionInternal.Title;
        public bool Value { get => PickMembersOptionInternal.Value; set => PickMembersOptionInternal.Value = value; }
    }

    internal class OmniSharpPickMembersResult
    {
        public readonly ImmutableArray<ISymbol> Members;
        public readonly ImmutableArray<OmniSharpPickMembersOption> Options;
        public readonly bool SelectedAll;

        public OmniSharpPickMembersResult(
            ImmutableArray<ISymbol> members,
            ImmutableArray<OmniSharpPickMembersOption> options,
            bool selectedAll)
        {
            Members = members;
            Options = options;
            SelectedAll = selectedAll;
        }
    }
}
