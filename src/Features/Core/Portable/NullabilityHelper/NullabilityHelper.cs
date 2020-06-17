// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.NullabilityHelper
{
    internal static class NullabilityHelper
    {
        public static ITypeSymbol TryRemoveNullableAnnotationForScope(SemanticModel semanticModel, IOperation containingOperation, ITypeSymbol symbol)
        {
            switch (symbol)
            {
                case ILocalSymbol localSymbol when localSymbol.NullableAnnotation == NullableAnnotation.Annotated:
                case IParameterSymbol parameterSymbol when parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated:

                    // For local symbols and parameters, we can check what the flow state 
                    // for refences to the symbols are and determine if we can change 
                    // the nullability to a less permissive state.
                    var references = containingOperation.DescendantsAndSelf()
                        .Where(o => IsSymbolReferencedByOperation(o, symbol));

                    if (AreAllReferencesNotNull(semanticModel, references))
                    {
                        return symbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                    }

                    return symbol;

                default:
                    return symbol;
            }
        }

        private static bool AreAllReferencesNotNull(SemanticModel semanticModel, IEnumerable<IOperation> references)
             => references.All(r => semanticModel.GetTypeInfo(r.Syntax).Nullability.FlowState == NullableFlowState.NotNull);

        private static bool IsSymbolReferencedByOperation(IOperation operation, ITypeSymbol symbol)
            => operation switch
            {
                ILocalReferenceOperation localReference => localReference.Local.Equals(symbol),
                IParameterReferenceOperation parameterReference => parameterReference.Parameter.Equals(symbol),
                IAssignmentOperation assignment => IsSymbolReferencedByOperation(assignment.Target, symbol),
                _ => false
            };
    }
}
