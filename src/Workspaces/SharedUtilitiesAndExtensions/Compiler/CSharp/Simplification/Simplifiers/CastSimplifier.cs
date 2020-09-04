// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    internal static class CastSimplifier
    {
        public static bool IsUnnecessaryCast(ExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
            => cast is CastExpressionSyntax castExpression ? IsUnnecessaryCast(castExpression, semanticModel, cancellationToken) :
               cast is BinaryExpressionSyntax binaryExpression ? IsUnnecessaryAsCast(binaryExpression, semanticModel, cancellationToken) : false;

        public static bool IsUnnecessaryCast(CastExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
            => IsCastSafeToRemove(cast, cast.Expression, semanticModel, cancellationToken);

        public static bool IsUnnecessaryAsCast(BinaryExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
            => cast.Kind() == SyntaxKind.AsExpression &&
               IsCastSafeToRemove(cast, cast.Left, semanticModel, cancellationToken);

        private static bool IsCastSafeToRemove(
            ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode,
            SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var speculationAnalyzer = new SpeculationAnalyzer(castNode,
                castedExpressionNode, semanticModel, cancellationToken,
                skipVerificationForReplacedNode: true, failOnOverloadResolutionFailuresInOriginalCode: true);

            // First, check to see if the node ultimately parenting this cast has any
            // syntax errors. If so, we bail.
            if (speculationAnalyzer.SemanticRootOfOriginalExpression.ContainsDiagnostics)
                return false;

            // Look for simple patterns that are known to be absolutely safe to always remove.
            if (CastCanDefinitelyBeRemoved(castNode, castedExpressionNode, semanticModel, cancellationToken))
                return true;

            // Then look for patterns for cases where we never want to remove casts.  Note: we want these checks to be
            // very fast, and to eliminate as many cases as necessary.  Importantly, we want to be able to do these
            // checks before calling into the speculation analyzer.
            if (CastMustBePreserved(castNode, castedExpressionNode, semanticModel, cancellationToken))
                return false;

            // If this changes static semantics (i.e. causes a different overload to be called), then we can't remove it.
            if (speculationAnalyzer.ReplacementChangesSemantics())
                return false;

            var castTypeInfo = semanticModel.GetTypeInfo(castNode, cancellationToken);
            var castType = castTypeInfo.Type;
            var expressionTypeInfo = semanticModel.GetTypeInfo(castedExpressionNode, cancellationToken);
            var expressionType = expressionTypeInfo.Type;

            var expressionToCastType = semanticModel.ClassifyConversion(castNode.SpanStart, castedExpressionNode, castType, isExplicitInSource: true);
            var outerType = GetOuterCastType(castNode, semanticModel, out var parentIsOrAsExpression) ?? castTypeInfo.ConvertedType;

            // Clearest case.  We know we haven't changed static semantic, and we have an Identity (i.e. no-impact,
            // representation-preserving) cast.  This is always safe to remove.
            //
            // Note: while these casts are always safe to remove, there is a case where we still keep them.
            // Specifically, if the compiler would warn that the code is no longer clear, then we will keep the cast
            // around.  These warning checks should go into CastMustBePreserved above.
            if (expressionToCastType.IsIdentity)
                return true;

            // We already bailed out of we had an explicit/none conversions back in CastMustBePreserved 
            // (except for implicit user defined conversions).
            Debug.Assert(!expressionToCastType.IsExplicit || expressionToCastType.IsUserDefined);

            // At this point, the only type of conversion left are implicit or user-defined conversions.  These may be
            // conversions we can remove, but need further analysis.
            Debug.Assert(expressionToCastType.IsImplicit || expressionToCastType.IsUserDefined);

            if (expressionToCastType.IsInterpolatedString)
            {
                // interpolation casts are necessary to preserve semantics if our destination type is not itself
                // FormattableString or some interface of FormattableString.

                return castType.Equals(castTypeInfo.ConvertedType) ||
                       ImmutableArray<ITypeSymbol>.CastUp(castType.AllInterfaces).Contains(castTypeInfo.ConvertedType);
            }

            if (castedExpressionNode.WalkDownParentheses().IsKind(SyntaxKind.DefaultLiteralExpression) &&
                !castType.Equals(outerType) &&
                outerType.IsNullable())
            {
                // We have a cast like `(T?)(X)default`. We can't remove the inner cast as it effects what value
                // 'default' means in this context.
                return false;
            }

            if (parentIsOrAsExpression)
            {
                // Note: speculationAnalyzer.ReplacementChangesSemantics() ensures that the parenting is or as expression are not broken.
                // Here we just need to ensure that the original cast expression doesn't invoke a user defined operator.
                return !expressionToCastType.IsUserDefined;
            }

            if (outerType != null)
            {
                var castToOuterType = semanticModel.ClassifyConversion(castNode.SpanStart, castNode, outerType);
                var expressionToOuterType = GetSpeculatedExpressionToOuterTypeConversion(speculationAnalyzer.ReplacedExpression, speculationAnalyzer, cancellationToken);

                // if the conversion to the outer type doesn't exist, then we shouldn't offer, except for anonymous functions which can't be reasoned about the same way (see below)
                if (!expressionToOuterType.Exists && !expressionToOuterType.IsAnonymousFunction)
                {
                    return false;
                }

                // CONSIDER: Anonymous function conversions cannot be compared from different semantic models as lambda symbol comparison requires syntax tree equality. Should this be a compiler bug?
                // For now, just revert back to computing expressionToOuterType using the original semantic model.
                if (expressionToOuterType.IsAnonymousFunction)
                {
                    expressionToOuterType = semanticModel.ClassifyConversion(castNode.SpanStart, castedExpressionNode, outerType);
                }

                // If there is an user-defined conversion from the expression to the cast type or the cast
                // to the outer type, we need to make sure that the same user-defined conversion will be 
                // called if the cast is removed.
                if (castToOuterType.IsUserDefined || expressionToCastType.IsUserDefined)
                {
                    return !expressionToOuterType.IsExplicit &&
                        (HaveSameUserDefinedConversion(expressionToCastType, expressionToOuterType) ||
                         HaveSameUserDefinedConversion(castToOuterType, expressionToOuterType)) &&
                         UserDefinedConversionIsAllowed(castNode);
                }
                else if (expressionToOuterType.IsUserDefined)
                {
                    return false;
                }

                if (expressionToCastType.IsExplicit &&
                    expressionToOuterType.IsExplicit)
                {
                    return false;
                }

                // If the conversion from the expression to the cast type is implicit numeric or constant
                // and the conversion from the expression to the outer type is identity, we'll go ahead
                // and remove the cast.
                if (expressionToOuterType.IsIdentity &&
                    expressionToCastType.IsImplicit &&
                    (expressionToCastType.IsNumeric || expressionToCastType.IsConstantExpression))
                {
                    // Some implicit numeric conversions can cause loss of precision and must not be removed.
                    return !IsRequiredImplicitNumericConversion(expressionType, castType);
                }

                if (!castToOuterType.IsBoxing &&
                    castToOuterType == expressionToOuterType)
                {
                    if (castToOuterType.IsNullable)
                    {
                        // Even though both the nullable conversions (castToOuterType and expressionToOuterType) are equal, we can guarantee no data loss only if there is an
                        // implicit conversion from expression type to cast type and expression type is non-nullable. For example, consider the cast removal "(float?)" for below:

                        // Console.WriteLine((int)(float?)(int?)2147483647); // Prints -2147483648

                        // castToOuterType:         ExplicitNullable
                        // expressionToOuterType:   ExplicitNullable
                        // expressionToCastType:    ImplicitNullable

                        // We should not remove the cast to "float?".
                        // However, cast to "int?" is unnecessary and should be removable.
                        return expressionToCastType.IsImplicit && !expressionType.IsNullable();
                    }
                    else if (expressionToCastType.IsImplicit && expressionToCastType.IsNumeric && !castToOuterType.IsIdentity)
                    {
                        // Some implicit numeric conversions can cause loss of precision and must not be removed.
                        return !IsRequiredImplicitNumericConversion(expressionType, castType);
                    }

                    return true;
                }

                if (castToOuterType.IsIdentity &&
                    !expressionToCastType.IsUnboxing &&
                    expressionToCastType == expressionToOuterType)
                {
                    return true;
                }

                // Special case: It's possible to have useless casts inside delegate creation expressions.
                // For example: new Func<string, bool>((Predicate<object>)(y => true)).
                if (IsInDelegateCreationExpression(castNode, semanticModel))
                {
                    if (expressionToCastType.IsAnonymousFunction && expressionToOuterType.IsAnonymousFunction)
                    {
                        return !speculationAnalyzer.ReplacementChangesSemanticsOfUnchangedLambda(castedExpressionNode, speculationAnalyzer.ReplacedExpression);
                    }

                    if (expressionToCastType.IsMethodGroup && expressionToOuterType.IsMethodGroup)
                    {
                        return true;
                    }
                }

                // Case :
                // 1. IList<object> y = (IList<dynamic>)new List<object>()
                if (expressionToCastType.IsExplicit && castToOuterType.IsExplicit && expressionToOuterType.IsImplicit)
                {
                    // If both expressionToCastType and castToOuterType are numeric, then this is a required cast as one of the conversions leads to loss of precision.
                    // Cast removal can change program behavior.                    
                    return !(expressionToCastType.IsNumeric && castToOuterType.IsNumeric);
                }

                // Case :
                // 2. object y = (ValueType)1;
                if (expressionToCastType.IsBoxing && expressionToOuterType.IsBoxing && castToOuterType.IsImplicit)
                {
                    return true;
                }

                // Case :
                // 3. object y = (NullableValueType)null;
                if ((!castToOuterType.IsBoxing || expressionToCastType.IsNullLiteral) &&
                    castToOuterType.IsImplicit &&
                    expressionToCastType.IsImplicit &&
                    expressionToOuterType.IsImplicit)
                {
                    if (expressionToOuterType.IsAnonymousFunction)
                    {
                        return expressionToCastType.IsAnonymousFunction &&
                            !speculationAnalyzer.ReplacementChangesSemanticsOfUnchangedLambda(castedExpressionNode, speculationAnalyzer.ReplacedExpression);
                    }

                    return true;
                }
            }

            return false;
        }

        private static bool CastCanDefinitelyBeRemoved(
            ExpressionSyntax castNode,
            ExpressionSyntax castedExpressionNode,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // castNode is:             `(Type)expr` or `expr as Type`.
            // castedExpressionnode is: `expr`

            // The type in `(Type)...` or `... as Type`
            var castType = semanticModel.GetTypeInfo(castNode, cancellationToken).Type;

            // The type in `(...)expr` or `expr as ...`
            var castedExpressionType = semanticModel.GetTypeInfo(castedExpressionNode, cancellationToken).Type;

            // $"x {(object)y} z"    It's always safe to remove this `(object)` cast.
            if (IsObjectCastInInterpolation(castNode, castType))
                return true;

            if (IsEnumToNumericCastThatCanDefinitelyBeRemoved(castNode, castType, castedExpressionType, semanticModel, cancellationToken))
                return true;

            return false;
        }

        private static bool IsObjectCastInInterpolation(ExpressionSyntax castNode, ITypeSymbol castType)
        {
            // A casts to object can always be removed from an expression inside of an interpolation, since it'll be converted to object
            // in order to call string.Format(...) anyway.
            return castType?.SpecialType == SpecialType.System_Object &&
                   castNode.WalkUpParentheses().IsParentKind(SyntaxKind.Interpolation);
        }

        private static bool IsEnumToNumericCastThatCanDefinitelyBeRemoved(
            ExpressionSyntax castNode,
            ITypeSymbol castType,
            ITypeSymbol castedExpressionType,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (!castedExpressionType.IsEnumType(out var castedEnumType))
                return false;

            if (!Equals(castType, castedEnumType.EnumUnderlyingType))
                return false;

            // if we have `(E)~(int)e` then the cast to (int) is not necessary as enums always support `~`.
            castNode = castNode.WalkUpParentheses();
            if (castNode.IsParentKind(SyntaxKind.BitwiseNotExpression, out PrefixUnaryExpressionSyntax prefixUnary))
            {
                if (!prefixUnary.WalkUpParentheses().IsParentKind(SyntaxKind.CastExpression, out CastExpressionSyntax parentCast))
                    return false;

                // `(int)` in `(E?)~(int)e` is also redundant.
                var parentCastType = semanticModel.GetTypeInfo(parentCast.Type, cancellationToken).Type;
                if (parentCastType.IsNullable(out var underlyingType))
                    parentCastType = underlyingType;

                return castedEnumType.Equals(parentCastType);
            }

            // if we have `(int)e == 0` then the cast can be removed.  Note: this is only for the exact cast of
            // comparing to the constant 0.  All other comparisons are not allowed.
            if (castNode.Parent is BinaryExpressionSyntax binaryExpression)
            {
                if (binaryExpression.IsKind(SyntaxKind.EqualsExpression) || binaryExpression.IsKind(SyntaxKind.NotEqualsExpression))
                {
                    var otherSide = castNode == binaryExpression.Left ? binaryExpression.Right : binaryExpression.Left;
                    var otherSideType = semanticModel.GetTypeInfo(otherSide, cancellationToken).Type;
                    if (otherSideType.Equals(castedEnumType.EnumUnderlyingType))
                    {
                        var constantValue = semanticModel.GetConstantValue(otherSide, cancellationToken);
                        if (constantValue.HasValue &&
                            IntegerUtilities.IsIntegral(constantValue.Value) &&
                            IntegerUtilities.ToInt64(constantValue.Value) == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool CastMustBePreserved(
            ExpressionSyntax castNode,
            ExpressionSyntax castedExpressionNode,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // castNode is:             `(Type)expr` or `expr as Type`.
            // castedExpressionnode is: `expr`

            // The type in `(Type)...` or `... as Type`
            var castType = semanticModel.GetTypeInfo(castNode, cancellationToken).Type;

            // If we don't understand the type, we must keep it.
            if (castType == null)
                return true;

            // The type in `(...)expr` or `expr as ...`
            var castedExpressionType = semanticModel.GetTypeInfo(castedExpressionNode, cancellationToken).Type;

            var conversion = semanticModel.ClassifyConversion(castNode.SpanStart, castedExpressionNode, castType, isExplicitInSource: true);

            // If we've got an error for some reason, then we don't want to touch this at all.
            if (castType.IsErrorType())
                return true;

            // Almost all explicit conversions can cause an exception or data loss, hence can never be removed.
            if (IsExplicitCastThatMustBePreserved(conversion))
                return true;

            // If this conversion doesn't even exist, then this code is in error, and we don't want to touch it.
            if (!conversion.Exists)
                return true;

            // `dynamic` changes the semantics of everything and is rarely safe to remove. We could consider removing
            // absolutely safe casts (i.e. `(dynamic)(dynamic)a`), but it's likely not worth the effort, so we just
            // disallow touching them entirely.
            if (InvolvesDynamic(castNode, castType, castedExpressionType, semanticModel, cancellationToken))
                return true;

            // If removing the cast would cause the compiler to issue a specific warning, then we have to preserve it.
            if (CastRemovalWouldCauseSignExtensionWarning(castNode, semanticModel, cancellationToken))
                return true;

            // *(T*)null.  Can't remove this case.
            if (IsDereferenceOfNullPointerCast(castNode, castedExpressionNode))
                return true;

            if (ParamsArgumentCastMustBePreserved(castNode, castType, semanticModel, cancellationToken))
                return true;

            // `... ? (int?)1 : default`.  This cast is necessary as the 'null/default' on the other side of the
            // conditional can change meaning since based on the type on the other side.
            //
            // TODO(cyrusn): This should move into SpeculationAnalyzer as it's a static-semantics change.
            if (CastMustBePreservedInConditionalBranch(castNode, conversion))
                return true;

            // (object)"" == someObj
            //
            // This cast can be removed with no runtime or static-semantics change.  However, the compiler warns here
            // that this could be confusing (since it's not clear it's calling `==(object,object)` instead of
            // `==(string,string)`), so we have to preserve this.
            if (CastIsRequiredToPreventUnintendedComparisonWarning(castNode, castedExpressionNode, castType, semanticModel, cancellationToken))
                return true;

            // Identity fp-casts can actually change the runtime value of the fp number.  This can happen because the
            // runtime is allowed to perform the operations with wider precision than the actual specified fp-precision.
            // i.e. 64-bit doubles can actually be 80 bits at runtime.  Even though the language considers this to be an
            // identity cast, we don't want to remove these because the user may be depending on that truncation.
            if (IdentityFloatingPointCastMustBePreserved(castNode, castedExpressionNode, castType, castedExpressionType, semanticModel, conversion, cancellationToken))
                return true;

            if (PointerOrIntPtrCastMustBePreserved(conversion))
                return true;

            // If we have something like `((int)default).ToString()`. `default` has no type of it's own, but instead can
            // be target typed.  However `(...).ToString()` is not a location where a target type can appear.  So don't
            // even bother removing this.
            if (IsTypeLessExpressionNotInTargetTypedLocation(castNode, castedExpressionType))
                return true;

            return false;
        }

        private static bool IsTypeLessExpressionNotInTargetTypedLocation(ExpressionSyntax castNode, ITypeSymbol castedExpressionType)
        {
            // If we have something like `((int)default).ToString()`. `default` has no type of it's own, but instead can
            // be target typed.  However `(...).ToString()` is not a location where a target type can appear.  So don't
            // even bother removing this.

            // checked if the expression being casted is typeless.
            if (castedExpressionType != null)
                return false;

            if (IsInTargetTypingLocation(castNode))
                return false;

            // we don't have our own type, and we're not in a location where a type can be inferred. don't remove this
            // cast.
            return true;
        }

        private static bool IsInTargetTypingLocation(ExpressionSyntax node)
        {
            node = node.WalkUpParentheses();
            var parent = node.Parent;

            // note: the list below is not intended to be exhaustive.  For example there are places we can target type,
            // but which we don't want to bother doing all the work to validate.  For example, technically you can
            // target type `throw (Exception)null`, so we could allow `(Exception)` to be removed.  But it's such a corner
            // case that we don't care about supporting, versus all the hugely valuable cases users will actually run into.

            // also: the list doesn't have to be firmly accurate:
            //  1. If we have a false positive and we say something is a target typing location, then that means we
            //     simply try to remove the cast, but then catch the break later.
            //  2. If we have a false negative and we say something is not a target typing location, then we simply
            //     don't try to remove the cast and the user has no impact on their code.

            // `null op e2`.  Either side can target type the other.
            if (parent is BinaryExpressionSyntax)
                return true;

            // `Goo(null)`.  The type of the arg is target typed by the Goo method being called.
            // 
            // This also helps Tuples fall out as they're built of arguments.  i.e. `(string s, string y) = (null, null)`.
            if (parent is ArgumentSyntax)
                return true;

            // same as above
            if (parent is AttributeArgumentSyntax)
                return true;

            // `new SomeType[] { null }` or `new [] { null, expr }`.
            // Type of the element can be target typed by the array type, or the sibling expression types.
            if (parent is InitializerExpressionSyntax)
                return true;

            // `return null;`.  target typed by whatever method this is in.
            if (parent is ReturnStatementSyntax)
                return true;

            // `yield return null;` same as above.
            if (parent is YieldStatementSyntax)
                return true;

            // `x = null`.  target typed by the other side.
            if (parent is AssignmentExpressionSyntax)
                return true;

            // ... = null
            //
            // handles:  parameters, variable declarations and the like.
            if (parent is EqualsValueClauseSyntax)
                return true;

            // `(SomeType)null`.  Definitely can target type this type-less expression.
            if (parent is CastExpressionSyntax)
                return true;

            // `... ? null : ...`.  Either side can target type the other.
            if (parent is ConditionalExpressionSyntax)
                return true;

            // case null:
            if (parent is CaseSwitchLabelSyntax)
                return true;

            return false;
        }

        private static bool IsExplicitCastThatMustBePreserved(Conversion conversion)
        {
            if (conversion.IsExplicit)
            {
                // if it's not a user defined conversion, we must preserve it as it has runtime impact that we don't want to change.
                if (!conversion.IsUserDefined)
                    return true;

                // Casts that involve implicit conversions are still represented as explicit casts. Because they're
                // implicit though, we may be able to remove it. i.e. if we have `(C)0 + (C)1` we can remove one of the
                // casts because it will be inferred from the binary context.
                var userMethod = conversion.MethodSymbol;
                if (userMethod?.Name != WellKnownMemberNames.ImplicitConversionName)
                    return true;
            }

            return false;
        }

        private static bool PointerOrIntPtrCastMustBePreserved(Conversion conversion)
        {
            if (!conversion.IsIdentity)
                return false;

            // if we have a non-identity cast to an int* or IntPtr just do not touch this.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving on this conservative approach.
            //
            // NOTE(cyrusn): This code should not be necessary.  However there is additional code that deals with
            // `*(x*)expr` ends up masking that this change should not be safe.  That code is suspect and should be
            // changed.  Until then though we disable this.
            return conversion.IsPointer || conversion.IsIntPtr;
        }

        private static bool InvolvesDynamic(
            ExpressionSyntax castNode,
            ITypeSymbol castType,
            ITypeSymbol castedExpressionType,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // We do not remove any cast on 
            // 1. Dynamic Expressions
            // 2. If there is any other argument which is dynamic
            // 3. Dynamic Invocation
            // 4. Assignment to dynamic

            if (castType?.Kind == SymbolKind.DynamicType || castedExpressionType?.Kind == SymbolKind.DynamicType)
                return true;

            return IsDynamicInvocation(castNode, semanticModel, cancellationToken) ||
                   IsDynamicAssignment(castNode, semanticModel, cancellationToken);
        }

        private static bool IsDereferenceOfNullPointerCast(ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode)
        {
            return castNode.WalkUpParentheses().IsParentKind(SyntaxKind.PointerIndirectionExpression) &&
                   castedExpressionNode.WalkDownParentheses().IsKind(SyntaxKind.NullLiteralExpression, SyntaxKind.DefaultLiteralExpression);
        }

        private static bool CastMustBePreservedInConditionalBranch(
            ExpressionSyntax expression, Conversion conversion)
        {
            // `... ? (int?)i : default`.  This cast is necessary as the 'null/default' on the other side of the
            // conditional can change meaning since based on the type on the other side.

            // It's safe to remove the cast when it's an identity. for example:
            // `... ? (int)1 : default`.
            if (!conversion.IsIdentity)
            {
                expression = expression.WalkUpParentheses();
                if (expression.Parent is ConditionalExpressionSyntax conditionalExpression)
                {
                    if (conditionalExpression.WhenTrue == expression ||
                        conditionalExpression.WhenFalse == expression)
                    {
                        var otherSide = conditionalExpression.WhenTrue == expression
                            ? conditionalExpression.WhenFalse
                            : conditionalExpression.WhenTrue;

                        otherSide = otherSide.WalkDownParentheses();
                        return otherSide.IsKind(SyntaxKind.NullLiteralExpression) ||
                               otherSide.IsKind(SyntaxKind.DefaultLiteralExpression);
                    }
                }
            }

            return false;
        }

        private static bool CastRemovalWouldCauseSignExtensionWarning(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // Logic copied from DiagnosticsPass_Warnings.CheckForBitwiseOrSignExtend.  Including comments.

            if (!(expression is CastExpressionSyntax castExpression))
                return false;

            var castRoot = castExpression.WalkUpParentheses();

            // Check both binary-or, and assignment-or
            //
            //   x | (...)y
            //   x |= (...)y

            ExpressionSyntax leftOperand, rightOperand;

            if (castRoot.Parent is BinaryExpressionSyntax parentBinary)
            {
                if (!parentBinary.IsKind(SyntaxKind.BitwiseOrExpression))
                    return false;

                (leftOperand, rightOperand) = (parentBinary.Left, parentBinary.Right);
            }
            else if (castRoot.Parent is AssignmentExpressionSyntax parentAssignment)
            {
                if (!parentAssignment.IsKind(SyntaxKind.OrAssignmentExpression))
                    return false;

                (leftOperand, rightOperand) = (parentAssignment.Left, parentAssignment.Right);
            }
            else
            {
                return false;
            }

            // The native compiler skips this warning if both sides of the operator are constants.
            //
            // CONSIDER: Is that sensible? It seems reasonable that if we would warn on int | short
            // when they are non-constants, or when one is a constant, that we would similarly warn 
            // when both are constants.
            var constantValue = semanticModel.GetConstantValue(castRoot.Parent, cancellationToken);

            if (constantValue.HasValue && constantValue.Value != null)
                return false;

            // Start by determining *which bits on each side are going to be unexpectedly turned on*.

            var leftOperation = semanticModel.GetOperation(leftOperand.WalkDownParentheses(), cancellationToken);
            var rightOperation = semanticModel.GetOperation(rightOperand.WalkDownParentheses(), cancellationToken);

            if (leftOperand == null || rightOperand == null)
                return false;

            // Note: we are asking the question about if there would be a problem removing the cast. So we have to act
            // as if an explicit cast becomes an implicit one. We do this by ignoring the appropriate cast and not
            // treating it as explicit when we encounter it.

            var left = FindSurprisingSignExtensionBits(leftOperation, leftOperand == castRoot);
            var right = FindSurprisingSignExtensionBits(rightOperation, rightOperand == castRoot);

            // If they are all the same then there's no warning to give.
            if (left == right)
                return false;

            // Suppress the warning if one side is a constant, and either all the unexpected
            // bits are already off, or all the unexpected bits are already on.

            var constVal = GetConstantValueForBitwiseOrCheck(leftOperation);
            if (constVal != null)
            {
                var val = constVal.Value;
                if ((val & right) == right || (~val & right) == right)
                    return false;
            }

            constVal = GetConstantValueForBitwiseOrCheck(rightOperation);
            if (constVal != null)
            {
                var val = constVal.Value;
                if ((val & left) == left || (~val & left) == left)
                    return false;
            }

            // This would produce a warning.  Don't offer to remove the cast.
            return true;
        }

        private static ulong? GetConstantValueForBitwiseOrCheck(IOperation operation)
        {
            // We might have a nullable conversion on top of an integer constant. But only dig out
            // one level.
            if (operation is IConversionOperation conversion &&
                conversion.Conversion.IsImplicit &&
                conversion.Conversion.IsNullable)
            {
                operation = conversion.Operand;
            }

            var constantValue = operation.ConstantValue;
            if (!constantValue.HasValue || constantValue.Value == null)
                return null;

            if (!operation.Type.SpecialType.IsIntegralType())
                return null;

            return IntegerUtilities.ToUInt64(constantValue.Value);
        }

        // A "surprising" sign extension is:
        //
        // * a conversion with no cast in source code that goes from a smaller
        //   signed type to a larger signed or unsigned type.
        //
        // * an conversion (with or without a cast) from a smaller
        //   signed type to a larger unsigned type.

        private static ulong FindSurprisingSignExtensionBits(IOperation operation, bool treatExplicitCastAsImplicit)
        {
            if (!(operation is IConversionOperation conversion))
                return 0;

            var from = conversion.Operand.Type;
            var to = conversion.Type;

            if (from is null || to is null)
                return 0;

            if (from.IsNullable(out var fromUnderlying))
                from = fromUnderlying;

            if (to.IsNullable(out var toUnderlying))
                to = toUnderlying;

            var fromSpecialType = from.SpecialType;
            var toSpecialType = to.SpecialType;

            if (!fromSpecialType.IsIntegralType() || !toSpecialType.IsIntegralType())
                return 0;

            var fromSize = fromSpecialType.SizeInBytes();
            var toSize = toSpecialType.SizeInBytes();

            if (fromSize == 0 || toSize == 0)
                return 0;

            // The operand might itself be a conversion, and might be contributing
            // surprising bits. We might have more, fewer or the same surprising bits
            // as the operand.

            var recursive = FindSurprisingSignExtensionBits(conversion.Operand, treatExplicitCastAsImplicit: false);

            if (fromSize == toSize)
            {
                // No change.
                return recursive;
            }

            if (toSize < fromSize)
            {
                // We are casting from a larger type to a smaller type, and are therefore
                // losing surprising bits. 
                switch (toSize)
                {
                    case 1: return unchecked((ulong)(byte)recursive);
                    case 2: return unchecked((ulong)(ushort)recursive);
                    case 4: return unchecked((ulong)(uint)recursive);
                }
                Debug.Assert(false, "How did we get here?");
                return recursive;
            }

            // We are converting from a smaller type to a larger type, and therefore might
            // be adding surprising bits. First of all, the smaller type has got to be signed
            // for there to be sign extension.

            var fromSigned = fromSpecialType.IsSignedIntegralType();

            if (!fromSigned)
                return recursive;

            // OK, we know that the "from" type is a signed integer that is smaller than the
            // "to" type, so we are going to have sign extension. Is it surprising? The only
            // time that sign extension is *not* surprising is when we have a cast operator
            // to a *signed* type. That is, (int)myShort is not a surprising sign extension.

            var explicitInCode = !conversion.IsImplicit;
            if (!treatExplicitCastAsImplicit &&
                explicitInCode &&
                toSpecialType.IsSignedIntegralType())
            {
                return recursive;
            }

            // Note that we *could* be somewhat more clever here. Consider the following edge case:
            //
            // (ulong)(int)(uint)(ushort)mySbyte
            //
            // We could reason that the sbyte-to-ushort conversion is going to add one byte of
            // unexpected sign extension. The conversion from ushort to uint adds no more bytes.
            // The conversion from uint to int adds no more bytes. Does the conversion from int
            // to ulong add any more bytes of unexpected sign extension? Well, no, because we 
            // know that the previous conversion from ushort to uint will ensure that the top bit
            // of the uint is off! 
            //
            // But we are not going to try to be that clever. In the extremely unlikely event that
            // someone does this, we will record that the unexpectedly turned-on bits are 
            // 0xFFFFFFFF0000FF00, even though we could in theory deduce that only 0x000000000000FF00
            // are the unexpected bits.

            var result = recursive;
            for (var i = fromSize; i < toSize; ++i)
                result |= (0xFFUL) << (i * 8);

            return result;
        }

        private static bool IdentityFloatingPointCastMustBePreserved(
            ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode,
            ITypeSymbol castType, ITypeSymbol castedExpressionType,
            SemanticModel semanticModel, Conversion conversion, CancellationToken cancellationToken)
        {
            if (!conversion.IsIdentity)
                return false;

            // Floating point casts can have subtle runtime behavior, even between the same fp types. For example, a
            // cast from float-to-float can still change behavior because it may take a higher precision computation and
            // truncate it to 32bits.
            //
            // Because of this we keep floating point conversions unless we can prove that it's safe.  The only safe
            // times are when we're loading or storing into a location we know has the same size as the cast size
            // (i.e. reading/writing into a field).
            if (castedExpressionType.SpecialType != SpecialType.System_Double &&
                castedExpressionType.SpecialType != SpecialType.System_Single &&
                castType.SpecialType != SpecialType.System_Double &&
                castType.SpecialType != SpecialType.System_Single)
            {
                // wasn't a floating point conversion.
                return false;
            }

            // Identity fp conversion is safe if this is a read from a fp field/array
            if (IsFieldOrArrayElement(semanticModel, castedExpressionNode, cancellationToken))
                return false;

            castNode = castNode.WalkUpParentheses();
            if (castNode.Parent is AssignmentExpressionSyntax assignmentExpression &&
                assignmentExpression.Right == castNode)
            {
                // Identity fp conversion is safe if this is a write to a fp field/array
                if (IsFieldOrArrayElement(semanticModel, assignmentExpression.Left, cancellationToken))
                    return false;
            }
            else if (castNode.Parent.IsKind(SyntaxKind.ArrayInitializerExpression, out InitializerExpressionSyntax arrayInitializer))
            {
                // Identity fp conversion is safe if this is in an array initializer.
                var typeInfo = semanticModel.GetTypeInfo(arrayInitializer, cancellationToken);
                return typeInfo.Type?.Kind == SymbolKind.ArrayType;
            }
            else if (castNode.Parent is EqualsValueClauseSyntax equalsValue &&
                     equalsValue.Value == castNode &&
                     equalsValue.Parent is VariableDeclaratorSyntax variableDeclarator)
            {
                // Identity fp conversion is safe if this is in a field initializer.
                var symbol = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);
                if (symbol?.Kind == SymbolKind.Field)
                    return false;
            }

            // We have to preserve this cast.
            return true;
        }

        private static bool IsFieldOrArrayElement(
            SemanticModel semanticModel, ExpressionSyntax expr, CancellationToken cancellationToken)
        {
            expr = expr.WalkDownParentheses();
            var castedExpresionSymbol = semanticModel.GetSymbolInfo(expr, cancellationToken).Symbol;

            // we're reading from a field of the same size.  it's safe to remove this case.
            if (castedExpresionSymbol?.Kind == SymbolKind.Field)
                return true;

            if (expr is ElementAccessExpressionSyntax elementAccess)
            {
                var locationType = semanticModel.GetTypeInfo(elementAccess.Expression, cancellationToken);
                return locationType.Type?.Kind == SymbolKind.ArrayType;
            }

            return false;
        }

        private static bool HaveSameUserDefinedConversion(Conversion conversion1, Conversion conversion2)
        {
            return conversion1.IsUserDefined
                && conversion2.IsUserDefined
                && Equals(conversion1.MethodSymbol, conversion2.MethodSymbol);
        }

        private static bool IsInDelegateCreationExpression(
            ExpressionSyntax castNode, SemanticModel semanticModel)
        {
            if (!(castNode.WalkUpParentheses().Parent is ArgumentSyntax argument))
            {
                return false;
            }

            if (!(argument.Parent is ArgumentListSyntax argumentList))
            {
                return false;
            }

            if (!(argumentList.Parent is ObjectCreationExpressionSyntax objectCreation))
            {
                return false;
            }

            var typeSymbol = semanticModel.GetSymbolInfo(objectCreation.Type).Symbol;

            return typeSymbol != null
                && typeSymbol.IsDelegateType();
        }

        private static bool IsDynamicInvocation(
            ExpressionSyntax castExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (castExpression.WalkUpParentheses().IsParentKind(SyntaxKind.Argument, out ArgumentSyntax argument) &&
                argument.IsParentKind(SyntaxKind.ArgumentList, SyntaxKind.BracketedArgumentList) &&
                argument.Parent.IsParentKind(SyntaxKind.InvocationExpression, SyntaxKind.ElementAccessExpression))
            {
                var typeInfo = semanticModel.GetTypeInfo(argument.Parent.Parent, cancellationToken);
                return typeInfo.Type?.Kind == SymbolKind.DynamicType;
            }

            return false;
        }

        private static bool IsDynamicAssignment(ExpressionSyntax castExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            castExpression = castExpression.WalkUpParentheses();
            if (castExpression.IsRightSideOfAnyAssignExpression())
            {
                var assignmentExpression = (AssignmentExpressionSyntax)castExpression.Parent;
                var assignmentType = semanticModel.GetTypeInfo(assignmentExpression.Left, cancellationToken).Type;

                return assignmentType?.Kind == SymbolKind.DynamicType;
            }

            return false;
        }

        private static bool IsRequiredImplicitNumericConversion(ITypeSymbol sourceType, ITypeSymbol destinationType)
        {
            // C# Language Specification: Section 6.1.2 Implicit numeric conversions

            // Conversions from int, uint, long, or ulong to float and from long or ulong to double may cause a loss of precision,
            // but will never cause a loss of magnitude. The other implicit numeric conversions never lose any information.

            switch (destinationType.SpecialType)
            {
                case SpecialType.System_Single:
                    switch (sourceType.SpecialType)
                    {
                        case SpecialType.System_Int32:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt64:
                            return true;

                        default:
                            return false;
                    }

                case SpecialType.System_Double:
                    switch (sourceType.SpecialType)
                    {
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt64:
                            return true;

                        default:
                            return false;
                    }

                default:
                    return false;
            }
        }

        private static bool CastIsRequiredToPreventUnintendedComparisonWarning(
            ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode, ITypeSymbol castType,
            SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // Based on the check in DiagnosticPass.CheckRelationals.

            // (object)"" == someObj
            //
            // This cast can be removed with no runtime or static-semantics change.  However, the compiler warns here
            // that this could be confusing (since it's not clear it's calling `==(object,object)` instead of
            // `==(string,string)`), so we have to preserve this.

            // compiler: if (node.Left.Type.SpecialType == SpecialType.System_Object
            if (castType?.SpecialType != SpecialType.System_Object)
                return false;

            // compiler: node.OperatorKind == BinaryOperatorKind.ObjectEqual || node.OperatorKind == BinaryOperatorKind.ObjectNotEqual
            castNode = castNode.WalkUpParentheses();
            var parent = castNode.Parent;
            if (!(parent is BinaryExpressionSyntax binaryExpression))
                return false;

            if (!binaryExpression.IsKind(SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression))
                return false;

            var binaryMethod = semanticModel.GetSymbolInfo(binaryExpression, cancellationToken).Symbol as IMethodSymbol;
            if (binaryMethod == null)
                return false;

            if (binaryMethod.ContainingType?.SpecialType != SpecialType.System_Object)
                return false;

            var operatorName = binaryMethod.Name;
            if (operatorName != WellKnownMemberNames.EqualityOperatorName && operatorName != WellKnownMemberNames.InequalityOperatorName)
                return false;

            // compiler: && ConvertedHasEqual(node.OperatorKind, node.Right, out t))
            var otherSide = castNode == binaryExpression.Left ? binaryExpression.Right : binaryExpression.Left;
            otherSide = otherSide.WalkDownParentheses();

            return CastIsRequiredToPreventUnintendedComparisonWarning(castedExpressionNode, otherSide, operatorName, semanticModel, cancellationToken) ||
                   CastIsRequiredToPreventUnintendedComparisonWarning(otherSide, castedExpressionNode, operatorName, semanticModel, cancellationToken);
        }

        private static bool CastIsRequiredToPreventUnintendedComparisonWarning(
            ExpressionSyntax left, ExpressionSyntax right, string operatorName,
            SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // compiler: node.Left.Type.SpecialType == SpecialType.System_Object
            var leftType = semanticModel.GetTypeInfo(left, cancellationToken).Type;
            if (leftType?.SpecialType != SpecialType.System_Object)
                return false;

            // compiler: && !IsExplicitCast(node.Left)
            if (left.IsKind(SyntaxKind.CastExpression, SyntaxKind.AsExpression))
                return false;

            // compiler: && !(node.Left.ConstantValue != null && node.Left.ConstantValue.IsNull)
            var constantValue = semanticModel.GetConstantValue(left, cancellationToken);
            if (constantValue.HasValue && constantValue.Value is null)
                return false;

            // compiler: && ConvertedHasEqual(node.OperatorKind, node.Right, out t))

            // Code for: ConvertedHasEqual

            // compiler: if (conv.ExplicitCastInCode) return false;
            if (right.IsKind(SyntaxKind.CastExpression, SyntaxKind.AsExpression))
                return false;

            // compiler: NamedTypeSymbol nt = conv.Operand.Type as NamedTypeSymbol;
            //           if ((object)nt == null || !nt.IsReferenceType || nt.IsInterface)
            var otherSideType = semanticModel.GetTypeInfo(right, cancellationToken).Type as INamedTypeSymbol;
            if (otherSideType == null)
                return false;

            if (!otherSideType.IsReferenceType || otherSideType.TypeKind == TypeKind.Interface)
                return false;

            // compiler: for (var t = nt; (object)t != null; t = t.BaseTypeNoUseSiteDiagnostics)
            for (var currentType = otherSideType; currentType != null; currentType = currentType.BaseType)
            {
                // compiler: foreach (var sym in t.GetMembers(opName))
                foreach (var opMember in currentType.GetMembers(operatorName))
                {
                    // compiler: MethodSymbol op = sym as MethodSymbol;
                    var opMethod = opMember as IMethodSymbol;

                    // compiler: if ((object)op == null || op.MethodKind != MethodKind.UserDefinedOperator) continue;
                    if (opMethod == null || opMethod.MethodKind != MethodKind.UserDefinedOperator)
                        continue;

                    // compiler: var parameters = op.GetParameters();
                    //           if (parameters.Length == 2 && TypeSymbol.Equals(parameters[0].Type, t, TypeCompareKind.ConsiderEverything2) && TypeSymbol.Equals(parameters[1].Type, t, TypeCompareKind.ConsiderEverything2))
                    //               return true
                    var parameters = opMethod.Parameters;
                    if (parameters.Length == 2 && Equals(parameters[0].Type, currentType) && Equals(parameters[1].Type, currentType))
                        return true;
                }
            }

            return false;
        }

        private static Conversion GetSpeculatedExpressionToOuterTypeConversion(ExpressionSyntax speculatedExpression, SpeculationAnalyzer speculationAnalyzer, CancellationToken cancellationToken)
        {
            var typeInfo = speculationAnalyzer.SpeculativeSemanticModel.GetTypeInfo(speculatedExpression, cancellationToken);
            var conversion = speculationAnalyzer.SpeculativeSemanticModel.GetConversion(speculatedExpression, cancellationToken);

            if (!conversion.IsIdentity)
            {
                return conversion;
            }

            var speculatedExpressionOuterType = GetOuterCastType(speculatedExpression, speculationAnalyzer.SpeculativeSemanticModel, out _) ?? typeInfo.ConvertedType;
            if (speculatedExpressionOuterType == null)
            {
                return default;
            }

            return speculationAnalyzer.SpeculativeSemanticModel.ClassifyConversion(speculatedExpression, speculatedExpressionOuterType);
        }

        private static bool UserDefinedConversionIsAllowed(ExpressionSyntax expression)
        {
            expression = expression.WalkUpParentheses();

            var parentNode = expression.Parent;
            if (parentNode == null)
            {
                return false;
            }

            if (parentNode.IsKind(SyntaxKind.ThrowStatement))
            {
                return false;
            }

            return true;
        }

        private static bool ParamsArgumentCastMustBePreserved(
            ExpressionSyntax cast,
            ITypeSymbol castType,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // When a casted value is passed as the single argument to a params parameter,
            // we can only remove the cast if it is implicitly convertible to the parameter's type,
            // but not the parameter's element type. Otherwise, we could end up changing the invocation
            // to pass an array rather than an array with a single element.
            //
            // IOW, given the following method...
            //
            // static void Goo(params object[] x) { }
            //
            // ...we should remove this cast...
            //
            // Goo((object[])null);
            //
            // ...but not this cast...
            //
            // Goo((object)null);
            var parent = cast.WalkUpParentheses().Parent;
            if (parent is ArgumentSyntax argument)
            {
                // If there are any arguments to the right (and the argument is not named), we can assume that this is
                // not a *single* argument passed to a params parameter.
                if (argument.NameColon == null && argument.Parent is BaseArgumentListSyntax argumentList)
                {
                    var argumentIndex = argumentList.Arguments.IndexOf(argument);
                    if (argumentIndex < argumentList.Arguments.Count - 1)
                    {
                        return false;
                    }
                }

                var parameter = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                return ParameterTypeMatchesParamsElementType(parameter, castType, semanticModel);
            }

            if (parent is AttributeArgumentSyntax attributeArgument)
            {
                if (attributeArgument.Parent is AttributeArgumentListSyntax)
                {
                    // We don't check the position of the argument because in attributes it is allowed that 
                    // params parameter are positioned in between if named arguments are used.
                    var parameter = attributeArgument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                    return ParameterTypeMatchesParamsElementType(parameter, castType, semanticModel);
                }
            }

            return false;
        }

        private static bool ParameterTypeMatchesParamsElementType(IParameterSymbol parameter, ITypeSymbol castType, SemanticModel semanticModel)
        {
            if (parameter?.IsParams == true)
            {
                // if the method is defined with errors: void M(params int wrongDefined), parameter.IsParams == true but parameter.Type is not an array.
                // In such cases is better to be conservative and opt out.
                if (!(parameter.Type is IArrayTypeSymbol parameterType))
                {
                    return true;
                }

                var conversion = semanticModel.Compilation.ClassifyConversion(castType, parameterType);
                if (conversion.Exists &&
                    conversion.IsImplicit)
                {
                    return false;
                }

                var conversionElementType = semanticModel.Compilation.ClassifyConversion(castType, parameterType.ElementType);
                if (conversionElementType.Exists &&
                    conversionElementType.IsImplicit)
                {
                    return true;
                }
            }

            return false;
        }

        private static ITypeSymbol GetOuterCastType(
            ExpressionSyntax expression, SemanticModel semanticModel, out bool parentIsIsOrAsExpression)
        {
            expression = expression.WalkUpParentheses();
            parentIsIsOrAsExpression = false;

            var parentNode = expression.Parent;
            if (parentNode == null)
            {
                return null;
            }

            if (parentNode.IsKind(SyntaxKind.CastExpression, out CastExpressionSyntax castExpression))
            {
                return semanticModel.GetTypeInfo(castExpression).Type;
            }

            if (parentNode.IsKind(SyntaxKind.PointerIndirectionExpression))
            {
                return semanticModel.GetTypeInfo(expression).Type;
            }

            if (parentNode.IsKind(SyntaxKind.IsExpression) ||
                parentNode.IsKind(SyntaxKind.AsExpression))
            {
                parentIsIsOrAsExpression = true;
                return null;
            }

            if (parentNode.IsKind(SyntaxKind.ArrayRankSpecifier))
            {
                return semanticModel.Compilation.GetSpecialType(SpecialType.System_Int32);
            }

            if (parentNode.IsKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax memberAccess))
            {
                if (memberAccess.Expression == expression)
                {
                    var memberSymbol = semanticModel.GetSymbolInfo(memberAccess).Symbol;
                    if (memberSymbol != null)
                    {
                        return memberSymbol.ContainingType;
                    }
                }
            }

            if (parentNode.IsKind(SyntaxKind.ConditionalExpression) &&
                ((ConditionalExpressionSyntax)parentNode).Condition == expression)
            {
                return semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean);
            }

            if ((parentNode is PrefixUnaryExpressionSyntax || parentNode is PostfixUnaryExpressionSyntax) &&
                !semanticModel.GetConversion(expression).IsUserDefined)
            {
                var parentExpression = (ExpressionSyntax)parentNode;
                return GetOuterCastType(parentExpression, semanticModel, out parentIsIsOrAsExpression) ?? semanticModel.GetTypeInfo(parentExpression).ConvertedType;
            }

            if (parentNode is InterpolationSyntax)
            {
                // $"{(x)y}"
                //
                // Regardless of the cast to 'x', being in an interpolation automatically casts the result to object
                // since this becomes a call to: FormattableStringFactory.Create(string, params object[]).
                return semanticModel.Compilation.ObjectType;
            }

            return null;
        }
    }
}
