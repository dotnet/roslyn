// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionExpression;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

internal sealed class CSharpUseCollectionInitializerAnalyzer : AbstractUseCollectionInitializerAnalyzer<
    ExpressionSyntax,
    StatementSyntax,
    BaseObjectCreationExpressionSyntax,
    MemberAccessExpressionSyntax,
    InvocationExpressionSyntax,
    ExpressionStatementSyntax,
    LocalDeclarationStatementSyntax,
    VariableDeclaratorSyntax,
    CSharpUseCollectionInitializerAnalyzer>
{
    protected override IUpdateExpressionSyntaxHelper<ExpressionSyntax, StatementSyntax> SyntaxHelper
        => CSharpUpdateExpressionSyntaxHelper.Instance;

    protected override bool IsInitializerOfLocalDeclarationStatement(LocalDeclarationStatementSyntax localDeclarationStatement, BaseObjectCreationExpressionSyntax rootExpression, [NotNullWhen(true)] out VariableDeclaratorSyntax? variableDeclarator)
        => CSharpObjectCreationHelpers.IsInitializerOfLocalDeclarationStatement(localDeclarationStatement, rootExpression, out variableDeclarator);

    protected override bool IsComplexElementInitializer(SyntaxNode expression, out int initializerElementCount)
    {
        if (expression is InitializerExpressionSyntax(SyntaxKind.ComplexElementInitializerExpression) initializer)
        {
            initializerElementCount = initializer.Expressions.Count;
            return true;
        }
        else
        {
            initializerElementCount = 0;
            return false;
        }
    }

    protected override bool HasExistingInvalidInitializerForCollection()
    {
        // Can't convert to a collection expression if it already has a { X = ... } object-initializer.
        //
        // Note 1: we do allow conversion of empty `{ }` initializer.  So we only block if the expression count is more than zero.
        if (_objectCreationExpression.Initializer is InitializerExpressionSyntax(SyntaxKind.ObjectInitializerExpression)
            {
                Expressions: [var firstExpression, ..],
            })
        {
            // Note 2: we do allow `{ [k] = v }` initializers if k:v elements are supported.
            if (firstExpression is AssignmentExpressionSyntax
                {
                    Left: ImplicitElementAccessSyntax { ArgumentList.Arguments.Count: 1 }
                } &&
                this.SyntaxFacts.SupportsKeyValuePairElement(_objectCreationExpression.SyntaxTree.Options))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    protected override bool AnalyzeMatchesAndCollectionConstructorForCollectionExpression(
        ArrayBuilder<CollectionMatch<SyntaxNode>> preMatches,
        ArrayBuilder<CollectionMatch<SyntaxNode>> postMatches,
        out bool mayChangeSemantics,
        CancellationToken cancellationToken)
    {
        mayChangeSemantics = false;

        // Constructor wasn't called with any arguments.  Nothing to validate.
        var argumentList = _objectCreationExpression.ArgumentList;
        if (argumentList is null || argumentList.Arguments.Count == 0)
            return true;

        // See if we can specialize a single argument, by potentially spreading it, or dropping it entirely if redundant.
        var supportsWithArgument = _objectCreationExpression.SyntaxTree.Options.LanguageVersion().IsCSharp14OrAbove();
        if (TrySpecializeSingleArgument(out mayChangeSemantics))
            return true;

        // Anything beyond just a single capacity argument (or single value to populate the collection with) isn't
        // anything we can handle.
        if (argumentList.Arguments.Count != 1)
            return false;

        if (this.SemanticModel.GetSymbolInfo(_objectCreationExpression, cancellationToken).Symbol is not IMethodSymbol
            {
                MethodKind: MethodKind.Constructor,
                Parameters: [var firstParameter],
            } constructor)
        {
            return false;
        }

        // Otherwise, if we're in C#14 or above, we can use the 'with(args)' argument trivially.
        if (supportsWithArgument)
        {
            preMatches.Add(new(argumentList, UseSpread: false, UseKeyValue: false));
            return true;
        }

        return false;

        bool TrySpecializeSingleArgument(out bool mayChangeSemantics)
        {
            mayChangeSemantics = false;

            // Anything beyond just a single capacity argument (or single value to populate the collection with) isn't
            // anything we can handle.
            if (argumentList.Arguments.Count != 1)
                return false;

            // We have one argument.  We can do a few special things here.  First, if it's a capacity argument, that matches
            // up with the number of elements we're adding to the collection, we can drop the capacity argument entirely.
            // Otherwise, if we're passing a collection to the constructor, we can spread that collection into the final
            // collection.  Finally, if we're in C#14 or above, we can use the 'with(args)' argument trivially.

            if (this.SemanticModel.GetSymbolInfo(_objectCreationExpression, cancellationToken).Symbol is not IMethodSymbol
                {
                    MethodKind: MethodKind.Constructor,
                    Parameters: [var firstParameter],
                } constructor)
            {
                return false;
            }

            // If it took a single argument that implements IEnumerable<T>.  We handle this by spreading that argument
            // as the first thing added to the collection.  Note: if we support 'with()', we prefer to use that as we know
            // it preserves the semantics here perfectly.
            if (!supportsWithArgument)
            {
                if (CanSpreadFirstParameter(constructor.ContainingType, firstParameter))
                {
                    preMatches.Add(new(argumentList.Arguments[0].Expression, UseSpread: true, UseKeyValue: false));

                    // Can't be certain that spreading the elements will be the same as passing to the constructor.  So pass
                    // that uncertainty up to the caller so they can inform the user.
                    mayChangeSemantics = true;
                    return true;
                }
            }

            // Otherwise, if it's a single `int capacity` constructor, we can try to see if the capacity matches up with
            // the number of elements we're adding to the collection.  If so, we can drop the capacity argument
            // entirely.
            if (firstParameter is { Type.SpecialType: SpecialType.System_Int32, Name: "capacity" })
            {
                // The original collection could have been passed elements explicitly in its initializer.  Ensure we account for
                // that as well.
                var individualElementCount = _objectCreationExpression.Initializer?.Expressions.Count ?? 0;

                // Walk the matches, determining what individual elements are added as-is, as well as what values are going to
                // be spread into the final collection.  We'll then ensure a correspondance between both and the expression the
                // user is currently passing to the 'capacity' argument to make sure they're entirely congruent.
                using var _1 = ArrayBuilder<ExpressionSyntax>.GetInstance(out var spreadElements);
                foreach (var match in postMatches)
                {
                    switch (match.Node)
                    {
                        case ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation } expressionStatement:
                            // x.AddRange(y).  Have to make sure we see y.Count in the capacity list.
                            // x.Add(y, z).  Increment the total number of elements by the arg count.
                            if (match.UseSpread)
                                spreadElements.Add(invocation.ArgumentList.Arguments[0].Expression);
                            else
                                individualElementCount += invocation.ArgumentList.Arguments.Count;

                            continue;

                        case ForEachStatementSyntax foreachStatement:
                            // foreach (var v in expr) x.Add(v).  Have to make sure we see expr.Count in the capacity list.
                            spreadElements.Add(foreachStatement.Expression);
                            continue;

                        default:
                            // Something we don't support (yet).
                            return false;
                    }
                }

                // Now, break up an expression like `1 + x.Length + y.Count` into the parts separated by the +'s
                var currentArgumentExpression = argumentList.Arguments[0].Expression;
                using var _2 = ArrayBuilder<ExpressionSyntax>.GetInstance(out var expressionPieces);

                while (true)
                {
                    if (currentArgumentExpression is BinaryExpressionSyntax binaryExpression)
                    {
                        if (binaryExpression.Kind() != SyntaxKind.AddExpression)
                            return false;

                        expressionPieces.Add(binaryExpression.Right);
                        currentArgumentExpression = binaryExpression.Left;
                    }
                    else
                    {
                        expressionPieces.Add(currentArgumentExpression);
                        break;
                    }
                }

                // Determine the total constant value provided in the expression.  For each constant we see, remove that
                // constant from the pieces list.  That way the pieces list only corresponds to the values to spread.
                var totalConstantValue = 0;
                for (var i = expressionPieces.Count - 1; i >= 0; i--)
                {
                    var piece = expressionPieces[i];
                    var constant = this.SemanticModel.GetConstantValue(piece, cancellationToken);
                    if (constant.Value is int value)
                    {
                        totalConstantValue += value;
                        expressionPieces.RemoveAt(i);
                    }
                }

                // If the constant didn't match the number of individual elements to add, we can't update this code.
                if (totalConstantValue != individualElementCount)
                    return false;

                // After removing the constants, we should have an expression for each value we're going to spread.
                if (expressionPieces.Count != spreadElements.Count)
                    return false;

                // Now make sure we have a match for each part of `x.Length + y.Length` to an element being spread
                // into the collection.
                foreach (var piece in expressionPieces)
                {
                    // we support x.Length, x.Count, and x.Count()
                    var current = piece;
                    if (piece is InvocationExpressionSyntax invocationExpression)
                    {
                        if (invocationExpression.ArgumentList.Arguments.Count != 0)
                            return false;

                        current = invocationExpression.Expression;
                    }

                    if (current is not MemberAccessExpressionSyntax(SyntaxKind.SimpleMemberAccessExpression) { Name.Identifier.ValueText: "Length" or "Count" } memberAccess)
                        return false;

                    current = memberAccess.Expression;

                    // Now see if we have an item we're spreading matching 'x'.
                    var matchIndex = spreadElements.FindIndex(SyntaxFacts.AreEquivalent, current);
                    if (matchIndex < 0)
                        return false;

                    spreadElements.RemoveAt(matchIndex);
                }

                // If we had any spread elements remaining we can't proceed.
                if (spreadElements.Count > 0)
                    return false;

                // We're all good.  The items we found matches up precisely to the capacity provided!
                return true;
            }

            return false;
        }

        bool CanSpreadFirstParameter(INamedTypeSymbol constructedType, IParameterSymbol firstParameter)
        {
            var compilation = this.SemanticModel.Compilation;

            var ienumerableOfTType = compilation.IEnumerableOfTType();
            if (!Equals(firstParameter.Type.OriginalDefinition, ienumerableOfTType) &&
                !firstParameter.Type.AllInterfaces.Any(i => Equals(i.OriginalDefinition, ienumerableOfTType)))
            {
                return false;
            }

            // Looks like something passed to the constructor call that we could potentially spread instead. e.g. `new
            // HashSet(someList)` can become `[.. someList]`.  However, check for certain cases we know where this is
            // wrong and we can't do this.

            // BlockingCollection<T> and Collection<T> both take ownership of the collection passed to them.  So adds to
            // them will add through to the original collection.  They do not take the original collection and add their
            // elements to itself.

            var collectionType = compilation.CollectionOfTType();
            var blockingCollectionType = compilation.BlockingCollectionOfTType();
            if (constructedType.GetBaseTypesAndThis().Any(
                    t => Equals(collectionType, t.OriginalDefinition) ||
                         Equals(blockingCollectionType, t.OriginalDefinition)))
            {
                return false;
            }

            return true;
        }
    }
}
