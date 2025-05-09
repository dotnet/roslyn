// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NET6_0_OR_GREATER

using System.Reflection;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal sealed class QueryFunctions(MethodInfo find, MethodInfo? updateCSharp, MethodInfo? updateVisualBasic)
{
    public MethodInfo Find { get; } = find;
    public MethodInfo? UpdateCSharp { get; } = updateCSharp;
    public MethodInfo? UpdateVisualBasic { get; } = updateVisualBasic;
}
#endif
