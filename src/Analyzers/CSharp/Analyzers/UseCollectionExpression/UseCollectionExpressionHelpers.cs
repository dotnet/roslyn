// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;

internal static class UseCollectionExpressionHelpers
{
    private static readonly CollectionExpressionSyntax s_emptyCollectionExpression = CollectionExpression();

    public static bool CanReplaceWithCollectionExpression(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        bool skipVerificationForReplacedNode,
        CancellationToken cancellationToken)
    {
        var compilation = semanticModel.Compilation;

        var topMostExpression = expression.WalkUpParentheses();
        if (topMostExpression.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            return false;

        var parent = topMostExpression.GetRequiredParent();

        if (!IsInTargetTypedLocation(semanticModel, topMostExpression, cancellationToken))
            return false;

        // X[] = new Y[] { 1, 2, 3 }
        //
        // First, we don't change things if X and Y are different.  That could lead to something observable at
        // runtime in the case of something like:  object[] x = new string[] ...

        var originalTypeInfo = semanticModel.GetTypeInfo(topMostExpression, cancellationToken);
        if (originalTypeInfo.Type is IErrorTypeSymbol)
            return false;

        if (originalTypeInfo.ConvertedType is null or IErrorTypeSymbol)
            return false;

        // Conservatively, avoid making this change if the original expression was itself converted. Consider, for
        // example, `IEnumerable<string> x = new List<string>()`.  If we change that to `[]` we will still compile,
        // but it's possible we'll end up with different types at runtime that may cause problems.
        //
        // Note: we can relax this on a case by case basis if we feel like it's acceptable.
        if (originalTypeInfo.Type != null && !originalTypeInfo.Type.Equals(originalTypeInfo.ConvertedType))
        {
            var isOk =
                originalTypeInfo.Type.Name == nameof(Span<int>) &&
                originalTypeInfo.ConvertedType.Name == nameof(ReadOnlySpan<int>) &&
                originalTypeInfo.Type.OriginalDefinition.Equals(compilation.SpanOfTType()) &&
                originalTypeInfo.ConvertedType.OriginalDefinition.Equals(compilation.ReadOnlySpanOfTType());
            if (!isOk)
                return false;
        }

        // Looks good as something to replace.  Now check the semantics of making the replacement to see if there would
        // any issues.  To keep things simple, all we do is replace the existing expression with the `[]` literal. This
        // is an 'untyped' collection expression literal, so it tells us if the new code will have any issues moving to
        // something untyped.  This will also tell us if we have any ambiguities (because there are multiple destination
        // types that could accept the collection expression).
        var speculationAnalyzer = new SpeculationAnalyzer(
            topMostExpression,
            s_emptyCollectionExpression,
            semanticModel,
            cancellationToken,
            skipVerificationForReplacedNode,
            failOnOverloadResolutionFailuresInOriginalCode: true);

        if (speculationAnalyzer.ReplacementChangesSemantics())
            return false;

        // Ensure that we have a collection conversion with the replacement.  If not, this wasn't a legal replacement
        // (for example, we're trying to replace an expression that is converted to something that isn't even a
        // collection type).
        var conversion = speculationAnalyzer.SpeculativeSemanticModel.GetConversion(speculationAnalyzer.ReplacedExpression, cancellationToken);
        if (!conversion.IsCollectionExpression)
            return false;

        // The new expression's converted type has to equal the old expressions as well.  Otherwise, we're now
        // converting this to some different collection type unintentionally.
        var replacedTypeInfo = speculationAnalyzer.SpeculativeSemanticModel.GetTypeInfo(speculationAnalyzer.ReplacedExpression, cancellationToken);
        if (!originalTypeInfo.ConvertedType.Equals(replacedTypeInfo.ConvertedType))
            return false;

        return true;
    }

    private static bool IsInTargetTypedLocation(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var topExpression = expression.WalkUpParentheses();
        var parent = topExpression.Parent;
        return parent switch
        {
            EqualsValueClauseSyntax equalsValue => IsInTargetTypedEqualsValueClause(equalsValue),
            CastExpressionSyntax castExpression => IsInTargetTypedCastExpression(castExpression),
            // a ? [1, 2, 3] : ...  is target typed if either the other side is *not* a collection,
            // or the entire ternary is target typed itself.
            ConditionalExpressionSyntax conditionalExpression => IsInTargetTypedConditionalExpression(conditionalExpression, topExpression),
            // Similar rules for switches.
            SwitchExpressionArmSyntax switchExpressionArm => IsInTargetTypedSwitchExpressionArm(switchExpressionArm),
            InitializerExpressionSyntax initializerExpression => IsInTargetTypedInitializerExpression(initializerExpression, topExpression),
            AssignmentExpressionSyntax assignmentExpression => IsInTargetTypedAssignmentExpression(assignmentExpression, topExpression),
            BinaryExpressionSyntax binaryExpression => IsInTargetTypedBinaryExpression(binaryExpression, topExpression),
            ArgumentSyntax or AttributeArgumentSyntax => true,
            ReturnStatementSyntax => true,
            _ => false,
        };

        bool HasType(ExpressionSyntax expression)
            => semanticModel.GetTypeInfo(expression, cancellationToken).Type is not null and not IErrorTypeSymbol;

        static bool IsInTargetTypedEqualsValueClause(EqualsValueClauseSyntax equalsValue)
            // If we're after an `x = ...` and it's not `var x`, this is target typed.
            => equalsValue.Parent is not VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: true } };

        static bool IsInTargetTypedCastExpression(CastExpressionSyntax castExpression)
            // (X[])[1, 2, 3] is target typed.  `(X)[1, 2, 3]` is currently not (because it looks like indexing into an expr).
            => castExpression.Type is not IdentifierNameSyntax;

        bool IsInTargetTypedConditionalExpression(ConditionalExpressionSyntax conditionalExpression, ExpressionSyntax expression)
        {
            if (conditionalExpression.WhenTrue == expression)
                return HasType(conditionalExpression.WhenFalse) || IsInTargetTypedLocation(semanticModel, conditionalExpression, cancellationToken);
            else if (conditionalExpression.WhenFalse == expression)
                return HasType(conditionalExpression.WhenTrue) || IsInTargetTypedLocation(semanticModel, conditionalExpression, cancellationToken);
            else
                return false;
        }

        bool IsInTargetTypedSwitchExpressionArm(SwitchExpressionArmSyntax switchExpressionArm)
        {
            var switchExpression = (SwitchExpressionSyntax)switchExpressionArm.GetRequiredParent();

            // check if any other arm has a type that this would be target typed against.
            foreach (var arm in switchExpression.Arms)
            {
                if (arm != switchExpressionArm && HasType(arm.Expression))
                    return true;
            }

            // All arms do not have a type, this is target typed if the switch itself is target typed.
            return IsInTargetTypedLocation(semanticModel, switchExpression, cancellationToken);
        }

        bool IsInTargetTypedInitializerExpression(InitializerExpressionSyntax initializerExpression, ExpressionSyntax expression)
        {
            // new X[] { [1, 2, 3] }.  Elements are target typed by array type.
            if (initializerExpression.Parent is ArrayCreationExpressionSyntax)
                return true;

            // new [] { [1, 2, 3], ... }.  Elements are target typed if there's another element with real type.
            if (initializerExpression.Parent is ImplicitArrayCreationExpressionSyntax)
            {
                foreach (var sibling in initializerExpression.Expressions)
                {
                    if (sibling != expression && HasType(sibling))
                        return true;
                }
            }

            // TODO: Handle these.
            if (initializerExpression.Parent is StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax)
                return false;

            // T[] x = [1, 2, 3];
            if (initializerExpression.Parent is EqualsValueClauseSyntax)
                return true;

            return false;
        }

        bool IsInTargetTypedAssignmentExpression(AssignmentExpressionSyntax assignmentExpression, ExpressionSyntax expression)
        {
            return expression == assignmentExpression.Right && HasType(assignmentExpression.Left);
        }

        bool IsInTargetTypedBinaryExpression(BinaryExpressionSyntax binaryExpression, ExpressionSyntax expression)
        {
            return binaryExpression.Kind() == SyntaxKind.CoalesceExpression && binaryExpression.Right == expression && HasType(binaryExpression.Left);
        }
    }

    public static CollectionExpressionSyntax ConvertInitializerToCollectionExpression(
        InitializerExpressionSyntax initializer, bool wasOnSingleLine)
    {
        // if the initializer is already on multiple lines, keep it that way.  otherwise, squash from `{ 1, 2, 3 }` to `[1, 2, 3]`
        var openBracket = Token(SyntaxKind.OpenBracketToken).WithTriviaFrom(initializer.OpenBraceToken);
        var elements = initializer.Expressions.GetWithSeparators().SelectAsArray(
            i => i.IsToken ? i : ExpressionElement((ExpressionSyntax)i.AsNode()!));
        var closeBracket = Token(SyntaxKind.CloseBracketToken).WithTriviaFrom(initializer.CloseBraceToken);

        // If it was on a single line to begin with, then remove the inner spaces on the `{ ... }` to create `[...]`. If
        // it was multiline, leave alone as we want the brackets to just replace the existing braces exactly as they are.
        if (wasOnSingleLine)
        {
            // convert '{ ' to '['
            if (openBracket.TrailingTrivia is [(kind: SyntaxKind.WhitespaceTrivia), ..])
                openBracket = openBracket.WithTrailingTrivia(openBracket.TrailingTrivia.Skip(1));

            if (elements is [.., var lastNodeOrToken] && lastNodeOrToken.GetTrailingTrivia() is [.., (kind: SyntaxKind.WhitespaceTrivia)] trailingTrivia)
                elements = elements.Replace(lastNodeOrToken, lastNodeOrToken.WithTrailingTrivia(trailingTrivia.Take(trailingTrivia.Count - 1)));
        }

        return CollectionExpression(openBracket, SeparatedList<CollectionElementSyntax>(elements), closeBracket);
    }

    public static CollectionExpressionSyntax ReplaceWithCollectionExpression(
        SourceText sourceText,
        InitializerExpressionSyntax originalInitializer,
        CollectionExpressionSyntax newCollectionExpression,
        bool newCollectionIsSingleLine)
    {
        Contract.ThrowIfFalse(originalInitializer.Parent
            is ArrayCreationExpressionSyntax
            or ImplicitArrayCreationExpressionSyntax
            or StackAllocArrayCreationExpressionSyntax
            or ImplicitStackAllocArrayCreationExpressionSyntax
            or BaseObjectCreationExpressionSyntax);

        var initializerParent = originalInitializer.GetRequiredParent();

        return ShouldReplaceExistingExpressionEntirely(sourceText, originalInitializer, newCollectionIsSingleLine)
            ? newCollectionExpression.WithTriviaFrom(initializerParent)
            : newCollectionExpression
                .WithPrependedLeadingTrivia(originalInitializer.OpenBraceToken.GetPreviousToken().TrailingTrivia)
                .WithPrependedLeadingTrivia(ElasticMarker);
    }

    private static bool ShouldReplaceExistingExpressionEntirely(
        SourceText sourceText,
        InitializerExpressionSyntax initializer,
        bool newCollectionIsSingleLine)
    {
        // Any time we have `{ x, y, z }` in any form, then always just replace the whole original expression
        // with `[x, y, z]`.
        if (newCollectionIsSingleLine && sourceText.AreOnSameLine(initializer.OpenBraceToken, initializer.CloseBraceToken))
            return true;

        // initializer was on multiple lines, but started on the same line as the 'new' keyword.  e.g.:
        //
        //      var v = new[] {
        //          1, 2, 3
        //      };
        //
        // Just remove the `new...` section entirely, but otherwise keep the initialize multiline:
        //
        //      var v = [
        //          1, 2, 3
        //      ];
        var parent = initializer.GetRequiredParent();
        var newKeyword = parent.GetFirstToken();
        if (sourceText.AreOnSameLine(newKeyword, initializer.OpenBraceToken) &&
            !sourceText.AreOnSameLine(initializer.OpenBraceToken, initializer.CloseBraceToken))
        {
            return true;
        }

        // Initializer was on multiple lines, and was not on the same line as the 'new' keyword, and the 'new' is on a newline:
        //
        //      var v2 =
        //          new[]
        //          {
        //              1, 2, 3
        //          };
        //
        // For this latter, we want to just remove the new portion and move the collection to subsume it.
        var previousToken = newKeyword.GetPreviousToken();
        if (previousToken == default)
            return true;

        if (!sourceText.AreOnSameLine(previousToken, newKeyword))
            return true;

        // All that is left is:
        //
        //      var v2 = new[]
        //      {
        //          1, 2, 3
        //      };
        //
        // For this we want to remove the 'new' portion, but keep the collection on its own line.
        return false;
    }

    public static ImmutableArray<CollectionExpressionMatch> TryGetMatches<TArrayCreationExpressionSyntax>(
        SemanticModel semanticModel,
        TArrayCreationExpressionSyntax expression,
        Func<TArrayCreationExpressionSyntax, TypeSyntax> getType,
        Func<TArrayCreationExpressionSyntax, InitializerExpressionSyntax?> getInitializer,
        CancellationToken cancellationToken)
        where TArrayCreationExpressionSyntax : ExpressionSyntax
    {
        Contract.ThrowIfFalse(expression is ArrayCreationExpressionSyntax or StackAllocArrayCreationExpressionSyntax);

        // has to either be `stackalloc X[]` or `stackalloc X[const]`.
        if (getType(expression) is not ArrayTypeSyntax { RankSpecifiers: [{ Sizes: [var size] }, ..] })
            return default;

        using var _ = ArrayBuilder<CollectionExpressionMatch>.GetInstance(out var matches);

        var initializer = getInitializer(expression);
        if (size is OmittedArraySizeExpressionSyntax)
        {
            // `stackalloc int[]` on its own is illegal.  Has to either have a size, or an initializer.
            if (initializer is null)
                return default;
        }
        else
        {
            // if `stackalloc X[val]`, then it `val` has to be a constant value.
            if (semanticModel.GetConstantValue(size, cancellationToken).Value is not int sizeValue)
                return default;

            if (initializer != null)
            {
                // if there is an initializer, then it has to have the right number of elements.
                if (sizeValue != initializer.Expressions.Count)
                    return default;
            }
            else
            {
                // if there is no initializer, we have to be followed by direct statements that initialize the right
                // number of elements.

                // This needs to be local variable like `ReadOnlySpan<T> x = stackalloc ...
                if (expression.WalkUpParentheses().Parent is not EqualsValueClauseSyntax
                    {
                        Parent: VariableDeclaratorSyntax
                        {
                            Identifier.ValueText: var variableName,
                            Parent.Parent: LocalDeclarationStatementSyntax localDeclarationStatement
                        },
                    })
                {
                    return default;
                }

                var currentStatement = localDeclarationStatement.GetNextStatement();
                for (var currentIndex = 0; currentIndex < sizeValue; currentIndex++)
                {
                    // Each following statement needs to of the form:
                    //
                    //   x[...] =
                    if (currentStatement is not ExpressionStatementSyntax
                        {
                            Expression: AssignmentExpressionSyntax
                            {
                                Left: ElementAccessExpressionSyntax
                                {
                                    Expression: IdentifierNameSyntax { Identifier.ValueText: var elementName },
                                    ArgumentList.Arguments: [var elementArgument],
                                } elementAccess,
                            }
                        } expressionStatement)
                    {
                        return default;
                    }

                    // Ensure we're indexing into the variable created.
                    if (variableName != elementName)
                        return default;

                    // The indexing value has to equal the corresponding location in the result.
                    if (semanticModel.GetConstantValue(elementArgument.Expression, cancellationToken).Value is not int indexValue ||
                        indexValue != currentIndex)
                    {
                        return default;
                    }

                    // this looks like a good statement, add to the right size of the assignment to track as that's what
                    // we'll want to put in the final collection expression.
                    matches.Add(new(expressionStatement, UseSpread: false));
                    currentStatement = currentStatement.GetNextStatement();
                }
            }
        }

        if (!CanReplaceWithCollectionExpression(
                semanticModel, expression, skipVerificationForReplacedNode: true, cancellationToken))
        {
            return default;
        }

        return matches.ToImmutable();
    }
}
