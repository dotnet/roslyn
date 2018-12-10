// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            IsCanceled = isCanceled;
        }

        public PickMembersResult(
            ImmutableArray<ISymbol> members,
            ImmutableArray<PickMembersOption> options)
        {
            Members = members;
            Options = options;
        }
    }
}
