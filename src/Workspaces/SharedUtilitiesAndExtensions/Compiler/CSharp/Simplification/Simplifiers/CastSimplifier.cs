// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    internal static class CastSimplifier
    {
        public static bool IsUnnecessaryCast(ExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
            => cast is CastExpressionSyntax castExpression ? IsUnnecessaryCast(castExpression, semanticModel, cancellationToken) :
               cast is BinaryExpressionSyntax binaryExpression ? IsUnnecessaryAsCast(binaryExpression, semanticModel, cancellationToken) : false;

        public static bool IsUnnecessaryCast(CastExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
            => IsUnnecessaryCast(cast, cast.Expression, semanticModel, cancellationToken);

        public static bool IsUnnecessaryAsCast(BinaryExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
            => cast.Kind() == SyntaxKind.AsExpression &&
               IsUnnecessaryCast(cast, cast.Left, semanticModel, cancellationToken);

        private static bool IsUnnecessaryCast(
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

            var castTypeInfo = semanticModel.GetTypeInfo(castNode, cancellationToken);
            var castType = castTypeInfo.Type;

            // Case:
            // 1 . Console.WriteLine(await (dynamic)task); Any Dynamic Cast will not be removed.
            if (castType == null || castType.Kind == SymbolKind.DynamicType || castType.IsErrorType())
                return false;

            var expressionTypeInfo = semanticModel.GetTypeInfo(castedExpressionNode, cancellationToken);
            var expressionType = expressionTypeInfo.Type;

            if (EnumCastDefinitelyCantBeRemoved(castNode, expressionType, castType))
                return false;

            // We do not remove any cast on 
            // 1. Dynamic Expressions
            // 2. If there is any other argument which is dynamic
            // 3. Dynamic Invocation
            // 4. Assignment to dynamic
            if ((expressionType != null &&
                (expressionType.IsErrorType() ||
                 expressionType.Kind == SymbolKind.DynamicType)) ||
                IsDynamicInvocation(castNode, semanticModel, cancellationToken) ||
                IsDynamicAssignment(castNode, semanticModel, cancellationToken))
            {
                return false;
            }

            if (PointerCastDefinitelyCantBeRemoved(castNode, castedExpressionNode))
                return false;

            if (CastPassedToParamsArrayDefinitelyCantBeRemoved(castNode, castType, semanticModel, cancellationToken))
                return false;

            // A casts to object can always be removed from an expression inside of an interpolation, since it'll be converted to object
            // in order to call string.Format(...) anyway.
            if (castType?.SpecialType == SpecialType.System_Object &&
                castNode.WalkUpParentheses().IsParentKind(SyntaxKind.Interpolation))
            {
                return true;
            }

            if (speculationAnalyzer.ReplacementChangesSemantics())
                return false;

            var expressionToCastType = semanticModel.ClassifyConversion(castNode.SpanStart, castedExpressionNode, castType, isExplicitInSource: true);
            var outerType = GetOuterCastType(castNode, semanticModel, out var parentIsOrAsExpression) ?? castTypeInfo.ConvertedType;

            // Simple case: If the conversion from the inner expression to the cast type is identity,
            // the cast can be removed.
            if (expressionToCastType.IsIdentity)
            {
                // Simple case: Is this an identity cast to another cast? If so, we're safe to remove it.
                if (castedExpressionNode.WalkDownParentheses().IsKind(SyntaxKind.CastExpression))
                {
                    return true;
                }

                // Required explicit cast for reference comparison.
                // Cast removal causes warning CS0252 (Possible unintended reference comparison).
                //      object x = string.Intern("Hi!");
                //      (object)x == "Hi!"
                if (IsRequiredCastForReferenceEqualityComparison(outerType, castNode, semanticModel, out var other))
                {
                    var otherToOuterType = semanticModel.ClassifyConversion(other, outerType);
                    if (otherToOuterType.IsImplicit && otherToOuterType.IsReference)
                    {
                        return false;
                    }
                }

                if (SameSizedFloatingPointCastMustBePreserved(
                        semanticModel, castNode, castedExpressionNode,
                        expressionType, castType, cancellationToken))
                {
                    return false;
                }

                return true;
            }

            Debug.Assert(!expressionToCastType.IsIdentity);
            if (expressionToCastType.IsExplicit)
            {
                // Explicit reference conversions can cause an exception or data loss, hence can never be removed.
                if (expressionToCastType.IsReference)
                    return false;

                // Unboxing conversions can cause a null ref exception, hence can never be removed.
                if (expressionToCastType.IsUnboxing)
                    return false;

                // Don't remove any explicit numeric casts.
                // https://github.com/dotnet/roslyn/issues/2987 tracks improving on this conservative approach.
                if (expressionToCastType.IsNumeric)
                    return false;
            }

            if (expressionToCastType.IsPointer || expressionToCastType.IsIntPtr)
            {
                // Don't remove any non-identity pointer or IntPtr conversions.
                // https://github.com/dotnet/roslyn/issues/2987 tracks improving on this conservative approach.
                return expressionType != null && expressionType.Equals(outerType);
            }

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
                         UserDefinedConversionIsAllowed(castNode, semanticModel);
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
                if (expressionToCastType.IsImplicit && expressionToCastType.IsReference &&
                    castToOuterType.IsIdentity &&
                    IsRequiredCastForReferenceEqualityComparison(outerType, castNode, semanticModel, out var other))
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

        private static bool SameSizedFloatingPointCastMustBePreserved(
            SemanticModel semanticModel, ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode,
            ITypeSymbol expressionType, ITypeSymbol castType, CancellationToken cancellationToken)
        {
            // Floating point casts can have subtle runtime behavior, even between the same fp types. For example, a
            // cast from float-to-float can still change behavior because it may take a higher precision computation and
            // truncate it to 32bits.
            //
            // Because of this we keep floating point conversions unless we can prove that it's safe.  The only safe
            // times are when we're loading or storing into a location we know has the same size as the cast size
            // (i.e. reading/writing into a field).
            if (expressionType.SpecialType != SpecialType.System_Double &&
                expressionType.SpecialType != SpecialType.System_Single &&
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
                var typeInfo = semanticModel.GetTypeInfo(arrayInitializer);
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

        private static bool PointerCastDefinitelyCantBeRemoved(
            ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode)
        {
            if (castNode.WalkUpParentheses().IsParentKind(SyntaxKind.PointerIndirectionExpression) &&
                castedExpressionNode.WalkDownParentheses().IsKind(SyntaxKind.NullLiteralExpression))
            {
                return true;
            }

            return false;
        }

        private static bool EnumCastDefinitelyCantBeRemoved(
            ExpressionSyntax castNode, ITypeSymbol expressionType, ITypeSymbol castType)
        {
            if (expressionType is null || !expressionType.IsEnumType())
            {
                return false;
            }

            var outerExpression = castNode.WalkUpParentheses();
            if (outerExpression.IsParentKind(SyntaxKind.UnaryMinusExpression, SyntaxKind.UnaryPlusExpression))
            {
                // -(NumericType)value
                // +(NumericType)value
                return true;
            }

            if (castType.IsNumericType() && !outerExpression.IsParentKind(SyntaxKind.CastExpression))
            {
                if (outerExpression.Parent is BinaryExpressionSyntax
                    || outerExpression.Parent is PrefixUnaryExpressionSyntax)
                {
                    // Let the parent code handle this, since it could be something like this:
                    //
                    //   (int)enumValue > 0
                    //   ~(int)enumValue
                    return false;
                }

                // Explicit enum cast to numeric type, but not part of a chained cast or binary expression
                return true;
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

        private static bool IsRequiredCastForReferenceEqualityComparison(
            ITypeSymbol outerType, ExpressionSyntax castNode,
            SemanticModel semanticModel, out ExpressionSyntax other)
        {
            if (outerType.SpecialType == SpecialType.System_Object)
            {
                var expression = castNode.WalkUpParentheses();
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

            var speculatedExpressionOuterType = GetOuterCastType(speculatedExpression, speculationAnalyzer.SpeculativeSemanticModel, out var discarded) ?? typeInfo.ConvertedType;
            if (speculatedExpressionOuterType == null)
            {
                return default;
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
                // If there are any arguments to the right, we can assume that this is not a
                // *single* argument passed to a params parameter.
                if (argument.Parent is BaseArgumentListSyntax argumentList)
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
                if (attributeArgument.Parent is AttributeArgumentListSyntax attributeArgumentList)
                {
                    // We don't check the position of the argument because in attributes it is allowed that 
                    // params parameter are positioned in between if named arguments are used.
                    // The *single* argument check above is also broken: https://github.com/dotnet/roslyn/issues/20742
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

            return null;
        }
    }
}
