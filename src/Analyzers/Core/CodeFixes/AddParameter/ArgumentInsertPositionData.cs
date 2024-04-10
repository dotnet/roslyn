// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.AddParameter;

internal readonly struct ArgumentInsertPositionData<TArgumentSyntax>(IMethodSymbol methodToUpdate, TArgumentSyntax argumentToInsert, int argumentInsertionIndex) where TArgumentSyntax : SyntaxNode
{
    public IMethodSymbol MethodToUpdate { get; } = methodToUpdate;
    public TArgumentSyntax ArgumentToInsert { get; } = argumentToInsert;
    public int ArgumentInsertionIndex { get; } = argumentInsertionIndex;
}
