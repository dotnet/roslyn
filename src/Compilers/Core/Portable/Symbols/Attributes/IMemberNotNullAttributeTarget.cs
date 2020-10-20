// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    interface IMemberNotNullAttributeTarget
    {
        void AddNotNullMember(string memberName);

        void AddNotNullMember(ArrayBuilder<string> memberNames);

        ImmutableArray<string> NotNullMembers { get; }

        void AddNotNullWhenMember(bool sense, string memberName);

        void AddNotNullWhenMember(bool sense, ArrayBuilder<string> memberNames);

        ImmutableArray<string> NotNullWhenTrueMembers { get; }

        ImmutableArray<string> NotNullWhenFalseMembers { get; }
    }
}
