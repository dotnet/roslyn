// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class NullableHelpers
    {
        /// <summary>
        /// Given an encapsulating operation, returns true if the symbol passed in
        /// is ever assigned a possibly null value as determined by nullable flow state. Returns
        /// null if no references are found, letting the caller determine what to do with that information
        /// </summary>
        public static bool? IsSymbolAssignedPossiblyNullValue(SemanticModel semanticModel, IOperation containingOperation, ISymbol symbol)
        {
            var references = containingOperation.DescendantsAndSelf()
                .Where(o => IsSymbolReferencedByOperation(o, symbol));

            var hasReference = false;

            foreach (var reference in references)
            {
                hasReference = true;

                if (reference is IForEachLoopOperation forEachLoop)
                {
                    var foreachInfo = semanticModel.GetForEachStatementInfo((ForEachStatementSyntax)forEachLoop.Syntax);

                    // Use NotAnnotated here to keep both Annotated and None (oblivious) treated the same, since
                    // this is directly looking at the annotation and not the flow state
                    if (foreachInfo.ElementType.NullableAnnotation != NullableAnnotation.NotAnnotated)
                    {
                        return true;
                    }
                }

                var syntax = reference switch
                {
                    IVariableDeclaratorOperation variableDeclarator => variableDeclarator.GetVariableInitializer().Value.Syntax,
                    _ => reference.Syntax
                };

                var typeInfo = semanticModel.GetTypeInfo(syntax);

                if (typeInfo.Nullability.FlowState == NullableFlowState.MaybeNull)
                {
                    return true;
                }
            }

            return hasReference ? (bool?)false : null;
        }

        private static bool IsSymbolReferencedByOperation(IOperation operation, ISymbol symbol, bool allowNullInitializer = false)
            => operation switch
            {
                ILocalReferenceOperation localReference => localReference.Local.Equals(symbol),
                IParameterReferenceOperation parameterReference => parameterReference.Parameter.Equals(symbol),
                IAssignmentOperation assignment => IsSymbolReferencedByOperation(assignment.Target, symbol),
                IForEachLoopOperation loopOperation => IsSymbolReferencedByOperation(loopOperation.LoopControlVariable, symbol, allowNullInitializer: true),
                IVariableDeclaratorOperation variableDeclarator => (allowNullInitializer || variableDeclarator.GetVariableInitializer() != null) && variableDeclarator.Symbol.Equals(symbol),
                _ => false
            };
    }
}
