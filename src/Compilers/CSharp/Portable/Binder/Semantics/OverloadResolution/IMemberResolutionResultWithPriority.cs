// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Abstraction for use in <see cref="OverloadResolution.RemoveLowerPriorityMembers{TMemberResolution, TMember}(PooledObjects.ArrayBuilder{TMemberResolution})"/>,
/// to allow it to work generically with all member resolution types (method, unary operator, binary operator).
/// </summary>
internal interface IMemberResolutionResultWithPriority<TMember> where TMember : Symbol
{
    TMember? MemberWithPriority { get; }
}
