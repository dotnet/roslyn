// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

    protected override bool IsComplexElementInitializer(SyntaxNode expression)
        => expression.IsKind(SyntaxKind.ComplexElementInitializerExpression);

    protected override bool HasExistingInvalidInitializerForCollection()
    {
        // Can't convert to a collection expression if it already has an object-initializer.  Note, we do allow
        // conversion of empty `{ }` initializer.  So we only block if the expression count is more than zero.
        return _objectCreationExpression.Initializer is InitializerExpressionSyntax
        {
            RawKind: (int)SyntaxKind.ObjectInitializerExpression,
            Expressions.Count: > 0,
        };
    }

    protected override bool AnalyzeMatchesAndCollectionConstructorForCollectionExpression(
        ArrayBuilder<Match> preMatches,
        ArrayBuilder<Match> postMatches,
        CancellationToken cancellationToken)
    {
        // Constructor wasn't called with any arguments.  Nothing to validate.
        var argumentList = _objectCreationExpression.ArgumentList;
        if (argumentList is null || argumentList.Arguments.Count == 0)
            return true;

        // Anything beyond just a single capacity argument (or single value to populate the collection with) isn't
        // anything we can handle.
        if (argumentList.Arguments.Count != 1)
            return false;

        if (this.SemanticModel.GetSymbolInfo(_objectCreationExpression, cancellationToken).Symbol is not IMethodSymbol
            {
                MethodKind: MethodKind.Constructor,
                Parameters.Length: 1,
            } constructor)
        {
            return false;
        }

        var ienumerableOfTType = this.SemanticModel.Compilation.IEnumerableOfTType();
        var firstParameter = constructor.Parameters[0];
        if (Equals(firstParameter.Type.OriginalDefinition, ienumerableOfTType) ||
            firstParameter.Type.AllInterfaces.Any(i => Equals(i.OriginalDefinition, ienumerableOfTType)))
        {
            // Took a single argument that implements IEnumerable<T>.  We handle this by spreading that argument as the
            // first thing added to the collection.
            preMatches.Add(new(argumentList.Arguments[0].Expression, UseSpread: true));
            return true;
        }
        else if (firstParameter is { Type.SpecialType: SpecialType.System_Int32, Name: "capacity" })
        {
            // is a single `int capacity` constructor.

            // The original collection could have been passed elements explicitly in its initializer.  Ensure we account for
            // that as well.
            var individualElementCount = _objectCreationExpression.Initializer?.Expressions.Count ?? 0;

            // Walk the matches, determining what individual elements are added as-is, as well as what values are going to
            // be spread into the final collection.  We'll then ensure a correspondance between both and the expression the
            // user is currently passing to the 'capacity' argument to make sure they're entirely congruent.
            using var _1 = ArrayBuilder<ExpressionSyntax>.GetInstance(out var spreadElements);
            foreach (var match in postMatches)
            {
                switch (match.StatementOrExpression)
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
}
