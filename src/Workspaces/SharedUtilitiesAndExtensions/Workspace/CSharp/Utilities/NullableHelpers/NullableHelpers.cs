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
                .Where(o => IsSymbolReferencedByOperation(o, symbol, allowNullInitializer: false));

            var hasReference = false;

            foreach (var reference in references)
            {
                hasReference = true;

                // foreach statements are handled special because the iterator is not assignable, so the elementtype 
                // annotation is accurate for determining if the loop declaration has a reference that allows the symbol
                // to be null
                if (reference is IForEachLoopOperation forEachLoop)
                {
                    var foreachInfo = semanticModel.GetForEachStatementInfo((CommonForEachStatementSyntax)forEachLoop.Syntax);

                    // Use NotAnnotated here to keep both Annotated and None (oblivious) treated the same, since
                    // this is directly looking at the annotation and not the flow state
                    if (foreachInfo.ElementType.NullableAnnotation != NullableAnnotation.NotAnnotated)
                    {
                        return true;
                    }

                    continue;
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

        private static bool IsSymbolReferencedByOperation(IOperation operation, ISymbol symbol, bool allowNullInitializer)
            => operation switch
            {
                ILocalReferenceOperation localReference => localReference.Local.Equals(symbol),
                IParameterReferenceOperation parameterReference => parameterReference.Parameter.Equals(symbol),
                IAssignmentOperation assignment => IsSymbolReferencedByOperation(assignment.Target, symbol, allowNullInitializer: false),
                IForEachLoopOperation loopOperation => IsSymbolReferencedByOperation(loopOperation.LoopControlVariable, symbol, allowNullInitializer: true),

                // We want to be explicit about if we allow the variable to have a null variable initializer. The use case for now is around foreach loops 
                // where the LoopControlVariable of the operation will have a null initializer.
                IVariableDeclaratorOperation variableDeclarator => (allowNullInitializer || variableDeclarator.GetVariableInitializer() != null) && variableDeclarator.Symbol.Equals(symbol),
                _ => false
            };
    }
}
