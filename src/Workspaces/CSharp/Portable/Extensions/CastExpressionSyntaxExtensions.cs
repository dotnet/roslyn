// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class CastExpressionSyntaxExtensions
    {
        private static ITypeSymbol GetOuterCastType(ExpressionSyntax expression, SemanticModel semanticModel, out bool parentIsOrAsExpression)
        {
            expression = expression.WalkUpParentheses();
            parentIsOrAsExpression = false;

            var parentNode = expression.Parent;
            if (parentNode == null)
            {
                return null;
            }

            if (parentNode.IsKind(SyntaxKind.CastExpression))
            {
                var castExpression = (CastExpressionSyntax)parentNode;
                return semanticModel.GetTypeInfo(castExpression).Type;
            }

            if (parentNode.IsKind(SyntaxKind.PointerIndirectionExpression))
            {
                return semanticModel.GetTypeInfo(expression).Type;
            }

            if (parentNode.IsKind(SyntaxKind.IsExpression) ||
                parentNode.IsKind(SyntaxKind.AsExpression))
            {
                parentIsOrAsExpression = true;
                return null;
            }

            if (parentNode.IsKind(SyntaxKind.ArrayRankSpecifier))
            {
                return semanticModel.Compilation.GetSpecialType(SpecialType.System_Int32);
            }

            if (parentNode.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var memberAccess = (MemberAccessExpressionSyntax)parentNode;
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
                return GetOuterCastType(parentExpression, semanticModel, out parentIsOrAsExpression) ?? semanticModel.GetTypeInfo(parentExpression).ConvertedType;
            }

            return null;
        }

        private static bool IsRequiredCastForReferenceEqualityComparison(ITypeSymbol outerType, CastExpressionSyntax castExpression, SemanticModel semanticModel, out ExpressionSyntax other)
        {
            if (outerType.SpecialType == SpecialType.System_Object)
            {
                var expression = castExpression.WalkUpParentheses();
                var parentNode = expression.Parent;
                if (parentNode.IsKind(SyntaxKind.EqualsExpression) || parentNode.IsKind(SyntaxKind.NotEqualsExpression))
                {
                    // Reference comparison.
                    var binaryExpression = (BinaryExpressionSyntax)parentNode;
                    other = binaryExpression.Left == expression ?
                        binaryExpression.Right :
                        binaryExpression.Left;

                    // Explicit cast not required if we are comparing with type parameter with a class constraint.
                    var otherType = semanticModel.GetTypeInfo(other).Type;
                    if (otherType != null && otherType.TypeKind != TypeKind.TypeParameter)
                    {
                        return !other.WalkDownParentheses().IsKind(SyntaxKind.CastExpression);
                    }
                }
            }

            other = null;
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

            bool discarded;
            var speculatedExpressionOuterType = GetOuterCastType(speculatedExpression, speculationAnalyzer.SpeculativeSemanticModel, out discarded) ?? typeInfo.ConvertedType;
            if (speculatedExpressionOuterType == null)
            {
                return default(Conversion);
            }

            return speculationAnalyzer.SpeculativeSemanticModel.ClassifyConversion(speculatedExpression, speculatedExpressionOuterType);
        }

        private static bool UserDefinedConversionIsAllowed(ExpressionSyntax expression, SemanticModel semanticModel)
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

        private static bool CastPassedToParamsArrayDefinitelyCantBeRemoved(
            CastExpressionSyntax cast,
            ITypeSymbol castType,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // Special case: When a null literal is cast and passed as the single argument to a params parameter,
            // we can only remove the cast if it is implicitly convertible to the parameter's type,
            // but not the parameter's element type. Otherwise, we could end up changing the invocation
            // to pass a null array rather than an array with a null single element.
            //
            // IOW, given the following method...
            //
            // static void Foo(params object[] x) { }
            //
            // ...we should remove this cast...
            //
            // Foo((object[])null);
            //
            // ...but not this cast...
            //
            // Foo((object)null);

            if (cast.Expression.WalkDownParentheses().IsKind(SyntaxKind.NullLiteralExpression))
            {
                var argument = cast.WalkUpParentheses().Parent as ArgumentSyntax;
                if (argument != null)
                {
                    // If there are any arguments to the right, we can assume that this is not a
                    // *single* argument passed to a params parameter.
                    var argumentList = argument.Parent as BaseArgumentListSyntax;
                    if (argumentList != null)
                    {
                        var argumentIndex = argumentList.Arguments.IndexOf(argument);
                        if (argumentIndex < argumentList.Arguments.Count - 1)
                        {
                            return false;
                        }
                    }

                    var parameter = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                    if (parameter != null && parameter.IsParams)
                    {
                        Debug.Assert(parameter.Type is IArrayTypeSymbol);

                        var parameterType = (IArrayTypeSymbol)parameter.Type;

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
                }
            }

            return false;
        }

        private static bool PointerCastDefinitelyCantBeRemoved(CastExpressionSyntax cast)
        {
            if (cast.WalkUpParentheses().IsParentKind(SyntaxKind.PointerIndirectionExpression) &&
                cast.Expression.WalkDownParentheses().IsKind(SyntaxKind.NullLiteralExpression))
            {
                return true;
            }

            return false;
        }

        private static bool HaveSameUserDefinedConversion(Conversion conversion1, Conversion conversion2)
        {
            return conversion1.IsUserDefined
                && conversion2.IsUserDefined
                && conversion1.MethodSymbol == conversion2.MethodSymbol;
        }

        private static bool IsInDelegateCreationExpression(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var argument = expression.WalkUpParentheses().Parent as ArgumentSyntax;
            if (argument == null)
            {
                return false;
            }

            var argumentList = argument.Parent as ArgumentListSyntax;
            if (argumentList == null)
            {
                return false;
            }

            var objectCreation = argumentList.Parent as ObjectCreationExpressionSyntax;
            if (objectCreation == null)
            {
                return false;
            }

            var typeSymbol = semanticModel.GetSymbolInfo(objectCreation.Type).Symbol;

            return typeSymbol != null
                && typeSymbol.IsDelegateType();
        }

        private static bool IsDynamicInvocation(ExpressionSyntax castExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (castExpression.IsParentKind(SyntaxKind.Argument) &&
                castExpression.Parent.Parent.IsKind(SyntaxKind.ArgumentList, SyntaxKind.BracketedArgumentList) &&
                castExpression.Parent.Parent.Parent.IsKind(SyntaxKind.InvocationExpression, SyntaxKind.ElementAccessExpression))
            {
                var typeInfo = default(TypeInfo);

                if (castExpression.Parent.Parent.IsParentKind(SyntaxKind.InvocationExpression))
                {
                    typeInfo = semanticModel.GetTypeInfo((InvocationExpressionSyntax)castExpression.Parent.Parent.Parent, cancellationToken);
                }

                if (castExpression.Parent.Parent.IsParentKind(SyntaxKind.ElementAccessExpression))
                {
                    typeInfo = semanticModel.GetTypeInfo((ElementAccessExpressionSyntax)castExpression.Parent.Parent.Parent, cancellationToken);
                }

                if (typeInfo.Type != null && typeInfo.Type.Kind == SymbolKind.DynamicType)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDynamicAssignment(ExpressionSyntax castExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (castExpression.IsRightSideOfAnyAssignExpression())
            {
                var assignmentExpression = (AssignmentExpressionSyntax)castExpression.Parent;
                var assignmentType = semanticModel.GetTypeInfo(assignmentExpression.Left, cancellationToken).Type;

                return assignmentType?.Kind == SymbolKind.DynamicType;
            }

            return false;
        }

        public static bool IsUnnecessaryCast(this CastExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var speculationAnalyzer = new SpeculationAnalyzer(cast,
                cast.Expression, semanticModel, cancellationToken,
                skipVerificationForReplacedNode: true, failOnOverloadResolutionFailuresInOriginalCode: true);

            // First, check to see if the node ultimately parenting this cast has any
            // syntax errors. If so, we bail.
            if (speculationAnalyzer.SemanticRootOfOriginalExpression.ContainsDiagnostics)
            {
                return false;
            }

            var castTypeInfo = semanticModel.GetTypeInfo(cast, cancellationToken);
            var castType = castTypeInfo.Type;

            // Case:
            // 1 . Console.WriteLine(await (dynamic)task); Any Dynamic Cast will not be removed.
            if (castType == null || castType.Kind == SymbolKind.DynamicType || castType.IsErrorType())
            {
                return false;
            }

            var expressionTypeInfo = semanticModel.GetTypeInfo(cast.Expression, cancellationToken);
            var expressionType = expressionTypeInfo.Type;

            // We do not remove any cast on 
            // 1. Dynamic Expressions
            // 2. If there is any other argument which is dynamic
            // 3. Dynamic Invocation
            // 4. Assignment to dynamic
            if ((expressionType != null &&
                (expressionType.IsErrorType() ||
                 expressionType.Kind == SymbolKind.DynamicType)) ||
                IsDynamicInvocation(cast, semanticModel, cancellationToken) ||
                IsDynamicAssignment(cast, semanticModel, cancellationToken))
            {
                return false;
            }

            if (PointerCastDefinitelyCantBeRemoved(cast))
            {
                return false;
            }

            if (CastPassedToParamsArrayDefinitelyCantBeRemoved(cast, castType, semanticModel, cancellationToken))
            {
                return false;
            }

            // A casts to object can always be removed from an expression inside of an interpolation, since it'll be converted to object
            // in order to call string.Format(...) anyway.
            if (castType?.SpecialType == SpecialType.System_Object &&
                cast.WalkUpParentheses().IsParentKind(SyntaxKind.Interpolation))
            {
                return true;
            }

            if (speculationAnalyzer.ReplacementChangesSemantics())
            {
                return false;
            }

            var expressionToCastType = semanticModel.ClassifyConversion(cast.SpanStart, cast.Expression, castType, isExplicitInSource: true);

            bool parentIsOrAsExpression;
            var outerType = GetOuterCastType(cast, semanticModel, out parentIsOrAsExpression) ?? castTypeInfo.ConvertedType;

            // Simple case: If the conversion from the inner expression to the cast type is identity,
            // the cast can be removed.
            if (expressionToCastType.IsIdentity)
            {
                // Required explicit cast for reference comparison.
                // Cast removal causes warning CS0252 (Possible unintended reference comparison).
                //      object x = string.Intern("Hi!");
                //      (object)x == "Hi!"
                ExpressionSyntax other;
                if (IsRequiredCastForReferenceEqualityComparison(outerType, cast, semanticModel, out other))
                {
                    var otherToOuterType = semanticModel.ClassifyConversion(other, outerType);
                    if (otherToOuterType.IsImplicit && otherToOuterType.IsReference)
                    {
                        return false;
                    }
                }

                return true;
            }
            else if (expressionToCastType.IsExplicit && expressionToCastType.IsReference)
            {
                // Explicit reference conversions can cause an exception or data loss, hence can never be removed.
                return false;
            }
            else if (expressionToCastType.IsExplicit && expressionToCastType.IsNumeric)
            {
                // Don't remove any explicit numeric casts.
                // https://github.com/dotnet/roslyn/issues/2987 tracks improving on this conservative approach.
                return false;
            }
            else if (expressionToCastType.IsPointer)
            {
                // Don't remove any non-identity pointer conversions.
                // https://github.com/dotnet/roslyn/issues/2987 tracks improving on this conservative approach.
                return expressionType != null && expressionType.Equals(outerType);
            }

            if (parentIsOrAsExpression)
            {
                // Note: speculationAnalyzer.ReplacementChangesSemantics() ensures that the parenting is or as expression are not broken.
                // Here we just need to ensure that the original cast expression doesn't invoke a user defined operator.
                return !expressionToCastType.IsUserDefined;
            }

            if (outerType != null)
            {
                var castToOuterType = semanticModel.ClassifyConversion(cast.SpanStart, cast, outerType);
                var expressionToOuterType = GetSpeculatedExpressionToOuterTypeConversion(speculationAnalyzer.ReplacedExpression, speculationAnalyzer, cancellationToken);

                // CONSIDER: Anonymous function conversions cannot be compared from different semantic models as lambda symbol comparison requires syntax tree equality. Should this be a compiler bug?
                // For now, just revert back to computing expressionToOuterType using the original semantic model.
                if (expressionToOuterType.IsAnonymousFunction)
                {
                    expressionToOuterType = semanticModel.ClassifyConversion(cast.SpanStart, cast.Expression, outerType);
                }

                // If there is an user-defined conversion from the expression to the cast type or the cast
                // to the outer type, we need to make sure that the same user-defined conversion will be 
                // called if the cast is removed.
                if (castToOuterType.IsUserDefined || expressionToCastType.IsUserDefined)
                {
                    return !expressionToOuterType.IsExplicit &&
                        (HaveSameUserDefinedConversion(expressionToCastType, expressionToOuterType) ||
                         HaveSameUserDefinedConversion(castToOuterType, expressionToOuterType)) &&
                         UserDefinedConversionIsAllowed(cast, semanticModel);
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

                // Required explicit cast for reference comparison.
                // Cast removal causes warning CS0252 (Possible unintended reference comparison).
                //      object x = string.Intern("Hi!");
                //      x == (object)"Hi!"
                ExpressionSyntax other;
                if (expressionToCastType.IsImplicit && expressionToCastType.IsReference &&
                    castToOuterType.IsIdentity &&
                    IsRequiredCastForReferenceEqualityComparison(outerType, cast, semanticModel, out other))
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
                    return true;
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
                if (IsInDelegateCreationExpression(cast, semanticModel))
                {
                    if (expressionToCastType.IsAnonymousFunction && expressionToOuterType.IsAnonymousFunction)
                    {
                        return !speculationAnalyzer.ReplacementChangesSemanticsOfUnchangedLambda(cast.Expression, speculationAnalyzer.ReplacedExpression);
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
                            !speculationAnalyzer.ReplacementChangesSemanticsOfUnchangedLambda(cast.Expression, speculationAnalyzer.ReplacedExpression);
                    }

                    return true;
                }
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
    }
}
