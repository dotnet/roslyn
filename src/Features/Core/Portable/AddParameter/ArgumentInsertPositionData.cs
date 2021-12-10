// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.AddParameter
{
    internal readonly struct ArgumentInsertPositionData<TArgumentSyntax> where TArgumentSyntax : SyntaxNode
    {
        public ArgumentInsertPositionData(IMethodSymbol methodToUpdate, TArgumentSyntax argumentToInsert, int argumentInsertionIndex)
        {
            MethodToUpdate = methodToUpdate;
            ArgumentToInsert = argumentToInsert;
            ArgumentInsertionIndex = argumentInsertionIndex;
        }

        public IMethodSymbol MethodToUpdate { get; }
        public TArgumentSyntax ArgumentToInsert { get; }
        public int ArgumentInsertionIndex { get; }
    }
}
