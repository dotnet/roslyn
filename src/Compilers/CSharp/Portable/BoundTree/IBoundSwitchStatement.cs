// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal interface IBoundSwitchStatement
    {
        BoundNode Value { get; }
        ImmutableArray<BoundStatementList> Cases { get; }
    }
}
