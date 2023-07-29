// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UseCollectionExpression;

using static SyntaxFactory;

internal static class UseCollectionExpressionHelpers
{
    private static readonly LiteralExpressionSyntax s_nullLiteralExpression = LiteralExpression(SyntaxKind.NullLiteralExpression);

    public static bool CanReplaceWithCollectionExpression(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
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
        // any issues.  To keep things simple, all we do is replace the existing expression with the `null` literal.
        // This is a similarly 'untyped' literal (like a collection-expression is), so it tells us if the new code will
        // have any issues moving to something untyped.  This will also tell us if we have any ambiguities (because
        // there are multiple destination types that could accept the collection expression).
        var speculationAnalyzer = new SpeculationAnalyzer(
            topMostExpression,
            s_nullLiteralExpression,
            semanticModel,
            cancellationToken,
            skipVerificationForReplacedNode: true,
            failOnOverloadResolutionFailuresInOriginalCode: true);

        if (speculationAnalyzer.ReplacementChangesSemantics())
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
            => semanticModel.GetTypeInfo(expression, cancellationToken).Type != null;

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
}
