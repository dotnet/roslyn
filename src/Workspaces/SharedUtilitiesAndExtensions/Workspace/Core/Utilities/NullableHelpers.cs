// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal static class NullableHelpers
{
    /// <summary>
    /// Gets the declared symbol and root operation from the passed in declarationSyntax and calls <see
    /// cref="IsSymbolAssignedPossiblyNullValue"/>. Note that this is bool and not bool? because we know that the symbol
    /// is at the very least declared, so there's no need to return a null value. 
    /// </summary>
    public static bool IsDeclaredSymbolAssignedPossiblyNullValue(
        ISemanticFacts semanticFacts,
        SemanticModel semanticModel,
        SyntaxNode declarationSyntax,
        CancellationToken cancellationToken)
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

        return IsSymbolAssignedPossiblyNullValue(
            semanticFacts, semanticModel, rootOperation, declaredSymbol, rootOperation.Syntax.Span, includeDeclaration: true, cancellationToken) == true;
    }

    /// <summary>
    /// Given an operation, goes through all descendent operations and returns true if the symbol passed in
    /// is ever assigned a possibly null value as determined by nullable flow state. Returns
    /// null if no references are found, letting the caller determine what to do with that information
    /// </summary>
    public static bool? IsSymbolAssignedPossiblyNullValue(
        ISemanticFacts semanticFacts,
        SemanticModel semanticModel,
        IOperation rootOperation,
        ISymbol symbol,
        TextSpan span,
        bool includeDeclaration,
        CancellationToken cancellationToken)
    {
        var hasReference = false;

        using var _ = ArrayBuilder<IOperation>.GetInstance(out var stack);
        stack.Push(rootOperation);

        while (stack.TryPop(out var operation))
        {
            if (!span.IntersectsWith(operation.Syntax.Span))
                continue;

            if (span.Contains(operation.Syntax.Span) &&
                IsSymbolReferencedByOperation(operation))
            {
                hasReference = true;

                if (operation is IAssignmentOperation assignmentOperation &&
                    assignmentOperation.Syntax.RawKind == semanticFacts.SyntaxFacts.SyntaxKinds.SimpleAssignmentExpression)
                {
                    // IsSymbolReferencedByOperation will ensure that the reference is the target of the assignment.
                    //
                    // Note: we care about the value after the assignment, so we have to check the RHS to see if maybe-null
                    // is flowing in.  In other  words `currentlyNotNull = maybeNUll;` will be maybe-null *after* the
                    // assignment. and should cause our caller to keep the type as nullable.
                    var typeInfo = semanticModel.GetTypeInfo(assignmentOperation.Value.Syntax, cancellationToken);
                    if (IsMaybeNull(typeInfo))
                        return true;

                    // We specifically are not recursing down the left side of this variable.  If we have `x = not-null`
                    // then 'x' maybe-null in flowing in, but we care about what it is when flowing out, which the above
                    // check handled.
                    stack.Push(assignmentOperation.Value);
                    continue;
                }

                // foreach statements are handled special because the iterator is not assignable, so the element type
                // annotation is accurate for determining if the loop declaration has a reference that allows the symbol to
                // be null
                if (operation is IForEachLoopOperation forEachLoop)
                {
                    var foreachInfo = semanticFacts.GetForEachSymbols(semanticModel, forEachLoop.Syntax);

                    // Use NotAnnotated here to keep both Annotated and None (oblivious) treated the same, since
                    // this is directly looking at the annotation and not the flow state
                    if (foreachInfo.ElementType != null && foreachInfo.ElementType.NullableAnnotation != NullableAnnotation.NotAnnotated)
                        return true;

                    // intentional fall through.
                }
                else if (operation is IVariableDeclaratorOperation variableDeclarator)
                {
                    // IsSymbolReferencedByOperation ensures that GetVariableInitializer() returns a non-null value
                    var syntax = variableDeclarator.GetVariableInitializer()!.Value.Syntax;
                    var typeInfo = semanticModel.GetTypeInfo(syntax, cancellationToken);
                    if (IsMaybeNull(typeInfo))
                        return true;

                    // intentional fall through.
                }
                else
                {
                    var typeInfo = semanticModel.GetTypeInfo(operation.Syntax, cancellationToken);
                    if (IsMaybeNull(typeInfo))
                        return true;

                    // intentional fall through.
                }
            }

            foreach (var childOperation in operation.ChildOperations.Reverse())
                stack.Push(childOperation);
        }

        return hasReference ? false : null;

        static bool IsMaybeNull(TypeInfo typeInfo)
            => typeInfo.Nullability.FlowState == NullableFlowState.MaybeNull;

        // <summary>
        // Determines if an operations references a specific symbol. Note that this will recurse in some
        // cases to work for operations like IAssignmentOperation, which logically references a symbol even if it
        // is the Target operation that actually does. 
        // </summary>
        bool IsSymbolReferencedByOperation(IOperation operation)
            => operation switch
            {
                ILocalReferenceOperation localReference => localReference.Local.Equals(symbol),
                IParameterReferenceOperation parameterReference => parameterReference.Parameter.Equals(symbol),
                IAssignmentOperation assignment => IsSymbolReferencedByOperation(assignment.Target),
                ITupleOperation tupleOperation => tupleOperation.Elements.Any(IsSymbolReferencedByOperation),
                IForEachLoopOperation { LoopControlVariable: IVariableDeclaratorOperation variableDeclarator } => variableDeclarator.Symbol.Equals(symbol),

                // A variable initializer is required for this to be a meaningful operation for determining possible null assignment
                IVariableDeclaratorOperation variableDeclarator when includeDeclaration => variableDeclarator.GetVariableInitializer() != null && variableDeclarator.Symbol.Equals(symbol),
                _ => false
            };
    }
}
