// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UseCollectionExpression;

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

        var typeInfo = semanticModel.GetTypeInfo(topMostExpression, cancellationToken);
        if (typeInfo.Type is IErrorTypeSymbol)
            return false;

        if (typeInfo.ConvertedType is null or IErrorTypeSymbol)
            return false;

        if (typeInfo.Type != null && !typeInfo.Type.Equals(typeInfo.ConvertedType))
            return false;

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
        Contract.ThrowIfFalse(originalInitializer.Parent is ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax or BaseObjectCreationExpressionSyntax);

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
}
