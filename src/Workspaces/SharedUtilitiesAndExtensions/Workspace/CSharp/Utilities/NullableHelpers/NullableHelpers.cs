// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis;

internal static class NullableHelpers
{
    /// <summary>
    /// Gets the declared symbol and root operation from the passed in declarationSyntax and calls
    /// <see cref="IsSymbolAssignedPossiblyNullValue(SemanticModel, IOperation, ISymbol)"/>. Note
    /// that this is bool and not bool? because we know that the symbol is at the very least declared, 
    /// so there's no need to return a null value. 
    /// </summary>
    public static bool IsDeclaredSymbolAssignedPossiblyNullValue(SemanticModel semanticModel, SyntaxNode declarationSyntax, CancellationToken cancellationToken)
    {
        var declaredSymbol = semanticModel.GetRequiredDeclaredSymbol(declarationSyntax, cancellationToken);
        var declaredOperation = semanticModel.GetRequiredOperation(declarationSyntax, cancellationToken);

        var rootOperation = declaredOperation;

        // Walk up the tree to find a root for the operation
        // that contains the declaration
        while (rootOperation is not IBlockOperation &&
            rootOperation.Parent is not null)
        {
            rootOperation = rootOperation.Parent;
        }

        return IsSymbolAssignedPossiblyNullValue(semanticModel, rootOperation, declaredSymbol) == true;
    }

    /// <summary>
    /// Given an operation, goes through all decendent operations and returns true if the symbol passed in
    /// is ever assigned a possibly null value as determined by nullable flow state. Returns
    /// null if no references are found, letting the caller determine what to do with that information
    /// </summary>
    public static bool? IsSymbolAssignedPossiblyNullValue(SemanticModel semanticModel, IOperation operation, ISymbol symbol)
    {
        var references = operation.DescendantsAndSelf()
            .Where(o => IsSymbolReferencedByOperation(o, symbol));

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

                if (foreachInfo.ElementType is null)
                {
                    continue;
                }

                // Use NotAnnotated here to keep both Annotated and None (oblivious) treated the same, since
                // this is directly looking at the annotation and not the flow state
                if (foreachInfo.ElementType.NullableAnnotation != NullableAnnotation.NotAnnotated)
                {
                    return true;
                }

                continue;
            }

            var syntax = reference is IVariableDeclaratorOperation variableDeclarator
                ? variableDeclarator.GetVariableInitializer()!.Value.Syntax
                : reference.Syntax;

            var typeInfo = semanticModel.GetTypeInfo(syntax);

            if (typeInfo.Nullability.FlowState == NullableFlowState.MaybeNull)
            {
                return true;
            }
        }

        return hasReference ? (bool?)false : null;
    }

    /// <summary>
    /// Determines if an operations references a specific symbol. Note that this will recurse in some
    /// cases to work for operations like IAssignmentOperation, which logically references a symbol even if it
    /// is the Target operation that actually does. 
    /// </summary>
    private static bool IsSymbolReferencedByOperation(IOperation operation, ISymbol symbol)
        => operation switch
        {
            ILocalReferenceOperation localReference => localReference.Local.Equals(symbol),
            IParameterReferenceOperation parameterReference => parameterReference.Parameter.Equals(symbol),
            IAssignmentOperation assignment => IsSymbolReferencedByOperation(assignment.Target, symbol),
            ITupleOperation tupleOperation => tupleOperation.Elements.Any(predicate: static (element, symbol) => IsSymbolReferencedByOperation(element, symbol), arg: symbol),
            IForEachLoopOperation { LoopControlVariable: IVariableDeclaratorOperation variableDeclarator } => variableDeclarator.Symbol.Equals(symbol),

            // A variable initializer is required for this to be a meaningful operation for determining possible null assignment
            IVariableDeclaratorOperation variableDeclarator => variableDeclarator.GetVariableInitializer() != null && variableDeclarator.Symbol.Equals(symbol),
            _ => false
        };
}
