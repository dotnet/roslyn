// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class TempHelpers
    {
        /// <summary>
        /// Takes an expression and returns the bound local expression "temp" 
        /// and the bound assignment expression "temp = expr".
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="refKind"></param>
        /// <param name="containingMethod"></param>
        /// <returns></returns>
        public static Pair<BoundAssignmentOperator, BoundLocal> StoreToTemp(BoundExpression argument, RefKind refKind, MethodSymbol containingMethod)
        {
            var syntax = argument.Syntax;
            var type = argument.Type;
            var local = new BoundLocal(
                    syntax,
                    // TODO: the following temp local symbol should have its ContainingSymbol set.
                    new TempLocalSymbol(type, refKind, containingMethod),
                    null,
                    type);

            var store = new BoundAssignmentOperator(
                syntax,
                local,
                argument,
                refKind,
                type);

            return Pair.Make(store, local);
        }
    }
}
