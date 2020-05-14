// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.PickMembers
{
    internal class PickMembersResult
    {
        public static readonly PickMembersResult Canceled = new PickMembersResult(isCanceled: true);

        public readonly bool IsCanceled;
        public readonly ImmutableArray<ISymbol> Members;
        public readonly ImmutableArray<PickMembersOption> Options;

        private PickMembersResult(bool isCanceled)
            => IsCanceled = isCanceled;

        public PickMembersResult(
            ImmutableArray<ISymbol> members,
            ImmutableArray<PickMembersOption> options)
        {
            Members = members;
            Options = options;
        }
    }
}
