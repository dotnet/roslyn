// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class ExpressionLambdaRewriter // this is like a bound tree rewriter, but only handles a small subset of node kinds
    {
        private readonly SyntheticBoundNodeFactory _bound;
        private readonly TypeMap _typeMap;
        private readonly Dictionary<ParameterSymbol, BoundExpression> _parameterMap = new Dictionary<ParameterSymbol, BoundExpression>();
        private readonly bool _ignoreAccessibility;
        private int _recursionDepth;

        private NamedTypeSymbol _ExpressionType;
        private NamedTypeSymbol ExpressionType
        {
            get
            {
                if ((object)_ExpressionType == null)
                {
                    _ExpressionType = _bound.WellKnownType(WellKnownType.System_Linq_Expressions_Expression);
                }
                return _ExpressionType;
            }
        }

        private NamedTypeSymbol _ParameterExpressionType;
        private NamedTypeSymbol ParameterExpressionType
        {
            get
            {
                if ((object)_ParameterExpressionType == null)
                {
                    _ParameterExpressionType = _bound.WellKnownType(WellKnownType.System_Linq_Expressions_ParameterExpression);
                }
                return _ParameterExpressionType;
            }
        }

        private NamedTypeSymbol _ElementInitType;
        private NamedTypeSymbol ElementInitType
        {
            get
            {
                if ((object)_ElementInitType == null)
                {
                    _ElementInitType = _bound.WellKnownType(WellKnownType.System_Linq_Expressions_ElementInit);
                }
                return _ElementInitType;
            }
        }

        private NamedTypeSymbol _MemberBindingType;

        public NamedTypeSymbol MemberBindingType
        {
            get
            {
                if ((object)_MemberBindingType == null)
                {
                    _MemberBindingType = _bound.WellKnownType(WellKnownType.System_Linq_Expressions_MemberBinding);
                }
                return _MemberBindingType;
            }
        }

        private readonly NamedTypeSymbol _int32Type;

        private readonly NamedTypeSymbol _objectType;

        private readonly NamedTypeSymbol _nullableType;

        private NamedTypeSymbol _MemberInfoType;
        private NamedTypeSymbol MemberInfoType
        {
            get
            {
                if ((object)_MemberInfoType == null)
                {
                    _MemberInfoType = _bound.WellKnownType(WellKnownType.System_Reflection_MemberInfo);
                }
                return _MemberInfoType;
            }
        }

        private readonly NamedTypeSymbol _IEnumerableType;

        private DiagnosticBag Diagnostics { get { return _bound.Diagnostics; } }

        private ExpressionLambdaRewriter(TypeCompilationState compilationState, TypeMap typeMap, CSharpSyntaxNode node, int recursionDepth, DiagnosticBag diagnostics)
        {
            _bound = new SyntheticBoundNodeFactory(null, compilationState.Type, node, compilationState, diagnostics);
            _ignoreAccessibility = compilationState.ModuleBuilderOpt.IgnoreAccessibility;
            _int32Type = _bound.SpecialType(SpecialType.System_Int32);
            _objectType = _bound.SpecialType(SpecialType.System_Object);
            _nullableType = _bound.SpecialType(SpecialType.System_Nullable_T);
            _IEnumerableType = _bound.SpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);

            _typeMap = typeMap;
            _recursionDepth = recursionDepth;
        }

        internal static BoundNode RewriteLambda(BoundLambda node, TypeCompilationState compilationState, TypeMap typeMap, int recursionDepth, DiagnosticBag diagnostics)
        {
            try
            {
                var r = new ExpressionLambdaRewriter(compilationState, typeMap, node.Syntax, recursionDepth, diagnostics);
                var result = r.VisitLambdaInternal(node);
                if (node.Type != result.Type)
                {
                    diagnostics.Add(ErrorCode.ERR_MissingPredefinedMember, node.Syntax.Location, r.ExpressionType, "Lambda");
                }
                return result;
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                return node;
            }
        }

        private BoundExpression TranslateLambdaBody(BoundBlock block)
        {
            Debug.Assert(block.Locals.IsEmpty);
            foreach (var s in block.Statements)
            {
                for (var stmt = s; stmt != null;)
                {
                    switch (stmt.Kind)
                    {
                        case BoundKind.ReturnStatement:
                            var result = Visit(((BoundReturnStatement)stmt).ExpressionOpt);
                            if (result != null)
                            {
                                return result;
                            }
                            stmt = null;
                            break;
                        case BoundKind.ExpressionStatement:
                            return Visit(((BoundExpressionStatement)stmt).Expression);
                        case BoundKind.SequencePoint:
                            stmt = ((BoundSequencePoint)stmt).StatementOpt;
                            break;
                        case BoundKind.SequencePointWithSpan:
                            stmt = ((BoundSequencePointWithSpan)stmt).StatementOpt;
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(stmt.Kind);
                    }
                }
            }

            return null;
        }

        private BoundExpression Visit(BoundExpression node)
        {
            if (node == null)
            {
                return null;
            }

            CSharpSyntaxNode old = _bound.Syntax;
            _bound.Syntax = node.Syntax;
            var result = VisitInternal(node);
            _bound.Syntax = old;
            return _bound.Convert(ExpressionType, result);
        }

        private BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundKind.ArrayAccess:
                    return VisitArrayAccess((BoundArrayAccess)node);
                case BoundKind.ArrayCreation:
                    return VisitArrayCreation((BoundArrayCreation)node);
                case BoundKind.ArrayLength:
                    return VisitArrayLength((BoundArrayLength)node);
                case BoundKind.AsOperator:
                    return VisitAsOperator((BoundAsOperator)node);
                case BoundKind.BaseReference:
                    return VisitBaseReference((BoundBaseReference)node);
                case BoundKind.BinaryOperator:
                    var binOp = (BoundBinaryOperator)node;
                    return VisitBinaryOperator(binOp.OperatorKind, binOp.MethodOpt, binOp.Type, binOp.Left, binOp.Right);
                case BoundKind.UserDefinedConditionalLogicalOperator:
                    var userDefCondLogOp = (BoundUserDefinedConditionalLogicalOperator)node;
                    return VisitBinaryOperator(userDefCondLogOp.OperatorKind, userDefCondLogOp.LogicalOperator, userDefCondLogOp.Type, userDefCondLogOp.Left, userDefCondLogOp.Right);
                case BoundKind.Call:
                    return VisitCall((BoundCall)node);
                case BoundKind.ConditionalOperator:
                    return VisitConditionalOperator((BoundConditionalOperator)node);
                case BoundKind.Conversion:
                    return VisitConversion((BoundConversion)node);
                case BoundKind.DelegateCreationExpression:
                    return VisitDelegateCreationExpression((BoundDelegateCreationExpression)node);
                case BoundKind.FieldAccess:
                    return VisitFieldAccess((BoundFieldAccess)node);
                case BoundKind.IsOperator:
                    return VisitIsOperator((BoundIsOperator)node);
                case BoundKind.Lambda:
                    return VisitLambda((BoundLambda)node);
                case BoundKind.NewT:
                    return VisitNewT((BoundNewT)node);
                case BoundKind.NullCoalescingOperator:
                    return VisitNullCoalescingOperator((BoundNullCoalescingOperator)node);
                case BoundKind.ObjectCreationExpression:
                    return VisitObjectCreationExpression((BoundObjectCreationExpression)node);
                case BoundKind.Parameter:
                    return VisitParameter((BoundParameter)node);
                case BoundKind.PointerIndirectionOperator:
                    return VisitPointerIndirectionOperator((BoundPointerIndirectionOperator)node);
                case BoundKind.PointerElementAccess:
                    return VisitPointerElementAccess((BoundPointerElementAccess)node);
                case BoundKind.PropertyAccess:
                    return VisitPropertyAccess((BoundPropertyAccess)node);
                case BoundKind.SizeOfOperator:
                    return VisitSizeOfOperator((BoundSizeOfOperator)node);
                case BoundKind.UnaryOperator:
                    return VisitUnaryOperator((BoundUnaryOperator)node);

                case BoundKind.DefaultOperator:
                case BoundKind.HostObjectMemberReference:
                case BoundKind.Literal:
                case BoundKind.Local:
                case BoundKind.MethodInfo:
                case BoundKind.PreviousSubmissionReference:
                case BoundKind.ThisReference:
                case BoundKind.TypeOfOperator:
                    return Constant(node);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind);
            }
        }

        private BoundExpression VisitInternal(BoundExpression node)
        {
            BoundExpression result;
            _recursionDepth++;
#if DEBUG
            int saveRecursionDepth = _recursionDepth;
#endif

            if (_recursionDepth > 1)
            {
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

                result = VisitExpressionWithoutStackGuard(node);
            }
            else
            {
                result = VisitExpressionWithStackGuard(node);
            }

#if DEBUG
            Debug.Assert(saveRecursionDepth == _recursionDepth);
#endif
            _recursionDepth--;
            return result;
        }

        private BoundExpression VisitExpressionWithStackGuard(BoundExpression node)
        {
            try
            {
                return VisitExpressionWithoutStackGuard(node);
            }
            catch (Exception ex) when (StackGuard.IsInsufficientExecutionStackException(ex))
            {
                throw new BoundTreeVisitor.CancelledByStackGuardException(ex, node);
            }
        }

        private BoundExpression VisitArrayAccess(BoundArrayAccess node)
        {
            var array = Visit(node.Expression);
            if (node.Indices.Length == 1)
            {
                var arg = node.Indices[0];
                var index = Visit(arg);
                if (node.Type != _int32Type)
                {
                    index = ConvertIndex(index, arg.Type, _int32Type);
                }
                return ExprFactory("ArrayIndex", array, index);
            }
            else
            {
                return ExprFactory("ArrayIndex", array, Indices(node.Indices));
            }
        }

        private BoundExpression Indices(ImmutableArray<BoundExpression> expressions)
        {
            var builder = ArrayBuilder<BoundExpression>.GetInstance();
            foreach (var arg in expressions)
            {
                var index = Visit(arg);
                if (arg.Type != _int32Type)
                {
                    index = ConvertIndex(index, arg.Type, _int32Type);
                }
                builder.Add(index);
            }

            return _bound.Array(ExpressionType, builder.ToImmutableAndFree());
        }

        private BoundExpression Expressions(ImmutableArray<BoundExpression> expressions)
        {
            var builder = ArrayBuilder<BoundExpression>.GetInstance();
            foreach (var arg in expressions)
            {
                builder.Add(Visit(arg));
            }

            return _bound.Array(ExpressionType, builder.ToImmutableAndFree());
        }

        private BoundExpression VisitArrayCreation(BoundArrayCreation node)
        {
            var arrayType = (ArrayTypeSymbol)node.Type;
            var boundType = _bound.Typeof(arrayType.ElementType);
            if (node.InitializerOpt != null)
            {
                if (arrayType.IsSZArray)
                {
                    return ExprFactory("NewArrayInit", boundType, Expressions(node.InitializerOpt.Initializers));
                }
                else
                {
                    // error should have been reported earlier
                    // Bound.Diagnostics.Add(ErrorCode.ERR_ExpressionTreeContainsMultiDimensionalArrayInitializer, node.Syntax.Location);
                    return new BoundBadExpression(node.Syntax, default(LookupResultKind), ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(node), ExpressionType);
                }
            }
            else
            {
                return ExprFactory("NewArrayBounds", boundType, Expressions(node.Bounds));
            }
        }

        private BoundExpression VisitArrayLength(BoundArrayLength node)
        {
            return ExprFactory("ArrayLength", Visit(node.Expression));
        }

        private BoundExpression VisitAsOperator(BoundAsOperator node)
        {
            if (node.Operand.IsLiteralNull() && (object)node.Operand.Type == null)
            {
                var operand = _bound.Null(_bound.SpecialType(SpecialType.System_Object));
                node = node.Update(operand, node.TargetType, node.Conversion, node.Type);
            }

            return ExprFactory("TypeAs", Visit(node.Operand), _bound.Typeof(node.Type));
        }

        private BoundExpression VisitBaseReference(BoundBaseReference node)
        {
            // should have been reported earlier.
            // Diagnostics.Add(ErrorCode.ERR_ExpressionTreeContainsBaseAccess, node.Syntax.Location);
            return new BoundBadExpression(node.Syntax, 0, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(node), ExpressionType);
        }

        private string GetBinaryOperatorName(BinaryOperatorKind opKind, out bool isChecked, out bool isLifted, out bool requiresLifted)
        {
            isChecked = opKind.IsChecked();
            isLifted = opKind.IsLifted();
            requiresLifted = opKind.IsComparison();

            switch (opKind.Operator())
            {
                case BinaryOperatorKind.Addition: return isChecked ? "AddChecked" : "Add";
                case BinaryOperatorKind.Multiplication: return isChecked ? "MultiplyChecked" : "Multiply";
                case BinaryOperatorKind.Subtraction: return isChecked ? "SubtractChecked" : "Subtract";
                case BinaryOperatorKind.Division: return "Divide";
                case BinaryOperatorKind.Remainder: return "Modulo";
                case BinaryOperatorKind.And: return opKind.IsLogical() ? "AndAlso" : "And";
                case BinaryOperatorKind.Xor: return "ExclusiveOr";
                case BinaryOperatorKind.Or: return opKind.IsLogical() ? "OrElse" : "Or";
                case BinaryOperatorKind.LeftShift: return "LeftShift";
                case BinaryOperatorKind.RightShift: return "RightShift";
                case BinaryOperatorKind.Equal: return "Equal";
                case BinaryOperatorKind.NotEqual: return "NotEqual";
                case BinaryOperatorKind.LessThan: return "LessThan";
                case BinaryOperatorKind.LessThanOrEqual: return "LessThanOrEqual";
                case BinaryOperatorKind.GreaterThan: return "GreaterThan";
                case BinaryOperatorKind.GreaterThanOrEqual: return "GreaterThanOrEqual";
                default:
                    throw ExceptionUtilities.UnexpectedValue(opKind.Operator());
            }
        }

        private BoundExpression VisitBinaryOperator(BinaryOperatorKind opKind, MethodSymbol methodOpt, TypeSymbol type, BoundExpression left, BoundExpression right)
        {
            bool isChecked, isLifted, requiresLifted;
            string opName = GetBinaryOperatorName(opKind, out isChecked, out isLifted, out requiresLifted);

            // Fix up the null value for a nullable comparison vs null
            if ((object)left.Type == null && left.IsLiteralNull())
            {
                left = _bound.Default(right.Type);
            }
            if ((object)right.Type == null && right.IsLiteralNull())
            {
                right = _bound.Default(left.Type);
            }


            // Enums are handled as per their promoted underlying type
            switch (opKind.OperandTypes())
            {
                case BinaryOperatorKind.EnumAndUnderlying:
                case BinaryOperatorKind.UnderlyingAndEnum:
                case BinaryOperatorKind.Enum:
                    {
                        var enumOperand = (opKind.OperandTypes() == BinaryOperatorKind.UnderlyingAndEnum) ? right : left;
                        var promotedType = PromotedType(enumOperand.Type.StrippedType().GetEnumUnderlyingType());
                        if (opKind.IsLifted())
                        {
                            promotedType = _nullableType.Construct(promotedType);
                        }

                        var loweredLeft = VisitAndPromoteEnumOperand(left, promotedType, isChecked);
                        var loweredRight = VisitAndPromoteEnumOperand(right, promotedType, isChecked);

                        var result = MakeBinary(methodOpt, type, isLifted, requiresLifted, opName, loweredLeft, loweredRight);
                        return Demote(result, type, isChecked);
                    }
                default:
                    {
                        var loweredLeft = Visit(left);
                        var loweredRight = Visit(right);
                        return MakeBinary(methodOpt, type, isLifted, requiresLifted, opName, loweredLeft, loweredRight);
                    }
            }
        }

        private static BoundExpression DemoteEnumOperand(BoundExpression operand)
        {
            if (operand.Kind == BoundKind.Conversion)
            {
                var conversion = (BoundConversion)operand;
                if (!conversion.ConversionKind.IsUserDefinedConversion() &&
                    conversion.ConversionKind.IsImplicitConversion() && 
                    conversion.Type.StrippedType().IsEnumType())
                {
                    operand = conversion.Operand;
                }
            }

            return operand;
        }

        private BoundExpression VisitAndPromoteEnumOperand(BoundExpression operand, TypeSymbol promotedType, bool isChecked)
        {
            var literal = operand as BoundLiteral;
            if (literal != null)
            {
                // for compat reasons enum literals are directly promoted into underlying values
                return Constant(literal.Update(literal.ConstantValue, promotedType));
            }
            else
            {
                // COMPAT: if we have an operand converted to enum, we should unconvert it first
                //         Otherwise we will have an extra conversion in the tree: op -> enum -> underlying
                //         where native compiler would just directly convert to underlying
                var demotedOperand = DemoteEnumOperand(operand);
                var loweredOperand = Visit(demotedOperand);
                return Convert(loweredOperand, operand.Type, promotedType, isChecked, false);
            }
        }

        private BoundExpression MakeBinary(MethodSymbol methodOpt, TypeSymbol type, bool isLifted, bool requiresLifted, string opName, BoundExpression loweredLeft, BoundExpression loweredRight)
        {
            return
                ((object)methodOpt == null) ? ExprFactory(opName, loweredLeft, loweredRight) :
                    requiresLifted ? ExprFactory(opName, loweredLeft, loweredRight, _bound.Literal(isLifted && methodOpt.ReturnType != type), _bound.MethodInfo(methodOpt)) :
                        ExprFactory(opName, loweredLeft, loweredRight, _bound.MethodInfo(methodOpt));
        }

        private TypeSymbol PromotedType(TypeSymbol underlying)
        {
            if (underlying.SpecialType == SpecialType.System_Boolean)
            {
                return underlying;
            }

            var possiblePromote = Binder.GetEnumPromotedType(underlying.SpecialType);

            if (possiblePromote == underlying.SpecialType)
            {
                return underlying;
            }
            else
            {
                return _bound.SpecialType(possiblePromote);
            }
        }

        private BoundExpression Demote(BoundExpression node, TypeSymbol type, bool isChecked)
        {
            var e = type as NamedTypeSymbol;
            if ((object)e != null)
            {
                if (e.TypeKind == TypeKind.Enum || e.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && e.TypeArgumentsNoUseSiteDiagnostics[0].TypeKind == TypeKind.Enum)
                {
                    return Convert(node, type, isChecked);
                }

                var promotedType = e.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ? _nullableType.Construct(PromotedType((NamedTypeSymbol)e.TypeArgumentsNoUseSiteDiagnostics[0])) : PromotedType(e);
                if (promotedType != type)
                {
                    return Convert(node, type, isChecked);
                }
            }

            return node;
        }

        private BoundExpression ConvertIndex(BoundExpression expr, TypeSymbol oldType, TypeSymbol newType)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var kind = _bound.Compilation.Conversions.ClassifyConversion(oldType, newType, ref useSiteDiagnostics).Kind;
            Debug.Assert(useSiteDiagnostics.IsNullOrEmpty());
            switch (kind)
            {
                case ConversionKind.Identity:
                    return expr;
                case ConversionKind.ExplicitNumeric:
                    return Convert(expr, newType, true);
                default:
                    return Convert(expr, _int32Type, false);
            }
        }

        private BoundExpression VisitCall(BoundCall node)
        {
            if (node.IsDelegateCall)
            {
                // Generate Expression.Invoke(Receiver, arguments)
                return ExprFactory(WellKnownMemberNames.DelegateInvokeName, Visit(node.ReceiverOpt), Expressions(node.Arguments));
            }
            else
            {
                // Generate Expression.Call(Receiver, Method, [typeArguments,] arguments)
                var method = node.Method;
                return ExprFactory(
                    "Call",
                    method.IsStatic ? _bound.Null(ExpressionType) : Visit(node.ReceiverOpt),
                    _bound.MethodInfo(method),
                    Expressions(node.Arguments));
            }
        }

        private BoundExpression VisitConditionalOperator(BoundConditionalOperator node)
        {
            var condition = Visit(node.Condition);
            var consequence = VisitExactType(node.Consequence);
            var alternative = VisitExactType(node.Alternative);
            return ExprFactory("Condition", condition, consequence, alternative);
        }

        /// <summary>
        /// Visit the expression, but do so in a way that ensures that its type is precise.  That means that any
        /// sometimes-unnecessary conversions (such as an implicit reference conversion) are retained.
        /// </summary>
        private BoundExpression VisitExactType(BoundExpression e)
        {
            var conversion = e as BoundConversion;
            if (conversion != null && !conversion.ExplicitCastInCode)
            {
                e = conversion.Update(
                    conversion.Operand,
                    conversion.ConversionKind,
                    conversion.ResultKind,
                    isBaseConversion: conversion.IsBaseConversion,
                    symbolOpt: conversion.SymbolOpt,
                    @checked: conversion.Checked,
                    explicitCastInCode: true,
                    isExtensionMethod: conversion.IsExtensionMethod,
                    isArrayIndex: conversion.IsArrayIndex,
                    constantValueOpt: conversion.ConstantValueOpt,
                    type: conversion.Type);
            }

            return Visit(e);
        }

        private BoundExpression VisitConversion(BoundConversion node)
        {
            switch (node.ConversionKind)
            {
                case ConversionKind.MethodGroup:
                    {
                        var mg = (BoundMethodGroup)node.Operand;
                        return DelegateCreation(mg.ReceiverOpt, node.SymbolOpt, node.Type, node.SymbolOpt.IsStatic && !node.IsExtensionMethod);
                    }
                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                case ConversionKind.IntPtr:
                    {
                        var method = node.SymbolOpt;
                        var operandType = node.Operand.Type;
                        var strippedOperandType = operandType.StrippedType();
                        var conversionInputType = method.Parameters[0].Type;
                        var isLifted = operandType != conversionInputType && strippedOperandType == conversionInputType;
                        bool requireAdditionalCast =
                            strippedOperandType != ((node.ConversionKind == ConversionKind.ExplicitUserDefined) ? conversionInputType : conversionInputType.StrippedType());
                        var resultType = (isLifted && method.ReturnType.IsNonNullableValueType() && node.Type.IsNullableType()) ? _nullableType.Construct(method.ReturnType) : method.ReturnType;
                        var e1 = requireAdditionalCast
                            ? Convert(Visit(node.Operand), node.Operand.Type, method.Parameters[0].Type, node.Checked, false)
                            : Visit(node.Operand);
                        var e2 = ExprFactory("Convert", e1, _bound.Typeof(resultType), _bound.MethodInfo(method));
                        return Convert(e2, resultType, node.Type, node.Checked, false);
                    }
                case ConversionKind.ImplicitReference:
                case ConversionKind.Identity:
                    {
                        var operand = Visit(node.Operand);
                        return node.ExplicitCastInCode ? Convert(operand, node.Type, false) : operand;
                    }
                case ConversionKind.ImplicitNullable:
                    if (node.Operand.Type.IsNullableType())
                    {
                        return Convert(Visit(node.Operand), node.Operand.Type, node.Type, node.Checked, node.ExplicitCastInCode);
                    }
                    else
                    {
                        // the native compiler performs this conversion in two steps, so we follow suit
                        var nullable = (NamedTypeSymbol)node.Type;
                        var intermediate = nullable.TypeArgumentsNoUseSiteDiagnostics[0];
                        var e1 = Convert(Visit(node.Operand), node.Operand.Type, intermediate, node.Checked, false);
                        return Convert(e1, intermediate, node.Type, node.Checked, false);
                    }
                case ConversionKind.NullLiteral:
                    return Convert(Constant(_bound.Null(_objectType)), _objectType, node.Type, false, node.ExplicitCastInCode);
                default:
                    return Convert(Visit(node.Operand), node.Operand.Type, node.Type, node.Checked, node.ExplicitCastInCode);
            }
        }

        private BoundExpression Convert(BoundExpression operand, TypeSymbol oldType, TypeSymbol newType, bool isChecked, bool isExplicit)
        {
            return (oldType == newType && !isExplicit) ? operand : Convert(operand, newType, isChecked);
        }

        private BoundExpression Convert(BoundExpression expr, TypeSymbol type, bool isChecked)
        {
            return ExprFactory(isChecked ? "ConvertChecked" : "Convert", expr, _bound.Typeof(type));
        }

        private BoundExpression DelegateCreation(BoundExpression receiver, MethodSymbol method, TypeSymbol delegateType, bool staticMember)
        {
            var nullObject = _bound.Null(_objectType);
            receiver = staticMember ? nullObject : receiver.Type.IsReferenceType ? receiver : _bound.Convert(_objectType, receiver);

            var createDelegate = _bound.WellKnownMethod(WellKnownMember.System_Reflection_MethodInfo__CreateDelegate, isOptional: true);
            BoundExpression unquoted;
            if ((object)createDelegate != null)
            {
                // beginning in 4.5, we do it this way
                unquoted = _bound.Call(_bound.MethodInfo(method), createDelegate, _bound.Typeof(delegateType), receiver);
            }
            else
            {
                // 4.0 and earlier we do it this way
                //createDelegate = (MethodSymbol)Bound.WellKnownMember(WellKnownMember.System_Delegate__CreateDelegate);
                //operand = Bound.Call(nullObject, createDelegate, Bound.Typeof(node.Type), receiver, Bound.MethodInfo(method));
                unquoted = _bound.StaticCall(_bound.SpecialType(SpecialType.System_Delegate), "CreateDelegate", _bound.Typeof(delegateType), receiver, _bound.MethodInfo(method));
            }

            // NOTE: we visit the just-built node, which has not yet been visited.  This is not the usual order
            // of operations.  The above code represents Dev10's pre-expression-tree lowering, and producing
            // the expanded lowering by hand is very cumbersome.
            return Convert(Visit(unquoted), delegateType, false);
        }

        private BoundExpression VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            var mg = node.Argument as BoundMethodGroup;
            if (mg != null)
            {
                return DelegateCreation(mg.ReceiverOpt, node.MethodOpt, node.Type, node.MethodOpt.IsStatic && !mg.SearchExtensionMethods);
            }

            var d = node.Argument.Type as NamedTypeSymbol;
            if ((object)d != null && d.TypeKind == TypeKind.Delegate)
            {
                return DelegateCreation(node.Argument, d.DelegateInvokeMethod, node.Type, false);
            }

            // there should be no other cases.  Have we missed one?
            throw ExceptionUtilities.UnexpectedValue(node.Argument);
        }

        private BoundExpression VisitFieldAccess(BoundFieldAccess node)
        {
            var receiver = node.FieldSymbol.IsStatic ? _bound.Null(ExpressionType) : Visit(node.ReceiverOpt);
            return ExprFactory(
                "Field",
                receiver, _bound.FieldInfo(node.FieldSymbol));
        }

        private BoundExpression VisitIsOperator(BoundIsOperator node)
        {
            var operand = node.Operand;
            if ((object)operand.Type == null && operand.ConstantValue != null && operand.ConstantValue.IsNull)
            {
                operand = _bound.Null(_objectType);
            }

            return ExprFactory("TypeIs", Visit(operand), _bound.Typeof(node.TargetType.Type));
        }

        private BoundExpression VisitLambda(BoundLambda node)
        {
            var result = VisitLambdaInternal(node);
            return node.Type.IsExpressionTree() ? ExprFactory("Quote", result) : result;
        }

        private BoundExpression VisitLambdaInternal(BoundLambda node)
        {
            // prepare parameters so that they can be seen later
            var locals = ArrayBuilder<LocalSymbol>.GetInstance();
            var initializers = ArrayBuilder<BoundExpression>.GetInstance();
            var parameters = ArrayBuilder<BoundExpression>.GetInstance();
            foreach (var p in node.Symbol.Parameters)
            {
                var param = _bound.SynthesizedLocal(ParameterExpressionType);
                locals.Add(param);
                var parameterReference = _bound.Local(param);
                parameters.Add(parameterReference);
                var parameter = ExprFactory(
                    "Parameter",
                    _bound.Typeof(_typeMap.SubstituteType(p.Type).Type), _bound.Literal(p.Name));
                initializers.Add(_bound.AssignmentExpression(parameterReference, parameter));
                _parameterMap[p] = parameterReference;
            }

            var underlyingDelegateType = node.Type.GetDelegateType();
            var result = _bound.Sequence(locals.ToImmutableAndFree(), initializers.ToImmutableAndFree(),
                ExprFactory(
                    "Lambda",
                    ImmutableArray.Create<TypeSymbol>(underlyingDelegateType),
                    TranslateLambdaBody(node.Body),
                    _bound.Array(ParameterExpressionType, parameters.ToImmutableAndFree())));

            foreach (var p in node.Symbol.Parameters)
            {
                _parameterMap.Remove(p);
            }

            return result;
        }

        private BoundExpression VisitNewT(BoundNewT node)
        {
            return VisitObjectCreationContinued(ExprFactory("New", _bound.Typeof(node.Type)), node.InitializerExpressionOpt);
        }

        private BoundExpression VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            var left = Visit(node.LeftOperand);
            var right = Visit(node.RightOperand);
            if (node.LeftConversion.IsUserDefined)
            {
                TypeSymbol lambdaParamType = node.LeftOperand.Type.StrippedType();
                return ExprFactory("Coalesce", left, right, MakeConversionLambda(node.LeftConversion, lambdaParamType, node.Type));
            }
            else
            {
                return ExprFactory("Coalesce", left, right);
            }
        }

        private BoundExpression MakeConversionLambda(Conversion conversion, TypeSymbol fromType, TypeSymbol toType)
        {
            string parameterName = "p";
            ParameterSymbol lambdaParameter = _bound.SynthesizedParameter(fromType, parameterName);
            var param = _bound.SynthesizedLocal(ParameterExpressionType);
            var parameterReference = _bound.Local(param);
            var parameter = ExprFactory("Parameter", _bound.Typeof(fromType), _bound.Literal(parameterName));
            _parameterMap[lambdaParameter] = parameterReference;
            var convertedValue = Visit(_bound.Convert(toType, _bound.Parameter(lambdaParameter), conversion));
            _parameterMap.Remove(lambdaParameter);
            var result = _bound.Sequence(
                ImmutableArray.Create(param),
                ImmutableArray.Create<BoundExpression>(_bound.AssignmentExpression(parameterReference, parameter)),
                ExprFactory(
                    "Lambda",
                    convertedValue,
                    _bound.Array(ParameterExpressionType, ImmutableArray.Create<BoundExpression>(parameterReference))));
            return result;
        }

        private BoundExpression InitializerMemberSetter(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    return _bound.Convert(MemberInfoType, _bound.FieldInfo((FieldSymbol)symbol));
                case SymbolKind.Property:
                    return _bound.MethodInfo(((PropertySymbol)symbol).GetOwnOrInheritedSetMethod());
                case SymbolKind.Event:
                    return _bound.Convert(MemberInfoType, _bound.FieldInfo(((EventSymbol)symbol).AssociatedField));
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        private BoundExpression InitializerMemberGetter(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    return _bound.Convert(MemberInfoType, _bound.FieldInfo((FieldSymbol)symbol));
                case SymbolKind.Property:
                    return _bound.MethodInfo(((PropertySymbol)symbol).GetOwnOrInheritedGetMethod());
                case SymbolKind.Event:
                    return _bound.Convert(MemberInfoType, _bound.FieldInfo(((EventSymbol)symbol).AssociatedField));
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        private enum InitializerKind { Expression, MemberInitializer, CollectionInitializer };

        private BoundExpression VisitInitializer(BoundExpression node, out InitializerKind kind)
        {
            switch (node.Kind)
            {
                case BoundKind.ObjectInitializerExpression:
                    {
                        var oi = (BoundObjectInitializerExpression)node;
                        var builder = ArrayBuilder<BoundExpression>.GetInstance();
                        foreach (BoundAssignmentOperator a in oi.Initializers)
                        {
                            var sym = ((BoundObjectInitializerMember)a.Left).MemberSymbol;

                            // An error is reported in diagnostics pass when a dynamic object initializer is encountered in an ET:
                            Debug.Assert((object)sym != null);

                            InitializerKind elementKind;
                            var value = VisitInitializer(a.Right, out elementKind);
                            switch (elementKind)
                            {
                                case InitializerKind.CollectionInitializer:
                                    {
                                        var left = InitializerMemberGetter(sym);
                                        builder.Add(ExprFactory("ListBind", left, value));
                                        break;
                                    }
                                case InitializerKind.Expression:
                                    {
                                        var left = InitializerMemberSetter(sym);
                                        builder.Add(ExprFactory("Bind", left, value));
                                        break;
                                    }
                                case InitializerKind.MemberInitializer:
                                    {
                                        var left = InitializerMemberGetter(sym);
                                        builder.Add(ExprFactory("MemberBind", left, value));
                                        break;
                                    }
                                default:
                                    throw ExceptionUtilities.UnexpectedValue(elementKind);
                            }
                        }

                        kind = InitializerKind.MemberInitializer;
                        return _bound.Array(MemberBindingType, builder.ToImmutableAndFree());
                    }

                case BoundKind.CollectionInitializerExpression:
                    {
                        var ci = (BoundCollectionInitializerExpression)node;
                        Debug.Assert(ci.Initializers.Length != 0);
                        kind = InitializerKind.CollectionInitializer;

                        var builder = ArrayBuilder<BoundExpression>.GetInstance();

                        // The method invocation must be a static call. 
                        // Dynamic calls are not allowed in ETs, an error is reported in diagnostics pass.
                        foreach (BoundCollectionElementInitializer i in ci.Initializers)
                        {
                            BoundExpression elementInit = ExprFactory("ElementInit", _bound.MethodInfo(i.AddMethod), Expressions(i.Arguments));
                            builder.Add(elementInit);
                        }

                        return _bound.Array(ElementInitType, builder.ToImmutableAndFree());
                    }

                default:
                    {
                        kind = InitializerKind.Expression;
                        return Visit(node);
                    }
            }
        }

        private BoundExpression VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            return VisitObjectCreationContinued(VisitObjectCreationExpressionInternal(node), node.InitializerExpressionOpt);
        }

        private BoundExpression VisitObjectCreationContinued(BoundExpression creation, BoundExpression initializerExpressionOpt)
        {
            var result = creation;
            if (initializerExpressionOpt == null) return result;
            InitializerKind initializerKind;
            var init = VisitInitializer(initializerExpressionOpt, out initializerKind);
            switch (initializerKind)
            {
                case InitializerKind.CollectionInitializer:
                    return ExprFactory("ListInit", result, init);
                case InitializerKind.MemberInitializer:
                    return ExprFactory("MemberInit", result, init);
                default:
                    throw ExceptionUtilities.UnexpectedValue(initializerKind); // no other options at the top level of an initializer
            }
        }

        private BoundExpression VisitObjectCreationExpressionInternal(BoundObjectCreationExpression node)
        {
            if (node.ConstantValue != null)
            {
                // typically a decimal constant.
                return Constant(node);
            }

            if ((object)node.Constructor == null ||
                (node.Arguments.Length == 0 && !node.Type.IsStructType()) ||
                node.Constructor.IsDefaultValueTypeConstructor())
            {
                return ExprFactory("New", _bound.Typeof(node.Type));
            }

            var ctor = _bound.ConstructorInfo(node.Constructor);
            var args = _bound.Convert(_IEnumerableType.Construct(ExpressionType), Expressions(node.Arguments));
            if (node.Type.IsAnonymousType && node.Arguments.Length != 0)
            {
                var anonType = (NamedTypeSymbol)node.Type;
                var membersBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                for (int i = 0; i < node.Arguments.Length; i++)
                {
                    membersBuilder.Add(_bound.MethodInfo(AnonymousTypeManager.GetAnonymousTypeProperty(anonType, i).GetMethod));
                }

                return ExprFactory("New", ctor, args, _bound.Array(MemberInfoType, membersBuilder.ToImmutableAndFree()));
            }
            else
            {
                return ExprFactory("New", ctor, args);
            }
        }

        private BoundExpression VisitParameter(BoundParameter node)
        {
            return _parameterMap[node.ParameterSymbol];
        }

        private static BoundExpression VisitPointerIndirectionOperator(BoundPointerIndirectionOperator node)
        {
            // error should have been reported earlier
            // Diagnostics.Add(ErrorCode.ERR_ExpressionTreeContainsPointerOp, node.Syntax.Location);
            return new BoundBadExpression(node.Syntax, default(LookupResultKind), ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(node), node.Type);
        }

        private static BoundExpression VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            // error should have been reported earlier
            // Diagnostics.Add(ErrorCode.ERR_ExpressionTreeContainsPointerOp, node.Syntax.Location);
            return new BoundBadExpression(node.Syntax, default(LookupResultKind), ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(node), node.Type);
        }

        private BoundExpression VisitPropertyAccess(BoundPropertyAccess node)
        {
            var receiver = node.PropertySymbol.IsStatic ? _bound.Null(ExpressionType) : Visit(node.ReceiverOpt);
            var getMethod = node.PropertySymbol.GetOwnOrInheritedGetMethod();

            // COMPAT: see https://github.com/dotnet/roslyn/issues/4471
            //         old compiler used to insert casts like this and 
            //         there are known dependencies on this kind of tree shape.
            //
            //         While the casts are semantically incorrect, the conditions
            //         under which they are observable are extremely narrow:
            //         We would have to deal with a generic T receiver which is actually a struct
            //         that implements a property form an interface and 
            //         the implementation of the getter must make observable mutations to the instance.
            //
            //         At this point it seems more appropriate to continue adding these casts.
            if (node.ReceiverOpt?.Type.IsTypeParameter() == true &&
                !node.ReceiverOpt.Type.IsReferenceType)
            {
                receiver = this.Convert(receiver, getMethod.ReceiverType, isChecked: false);
            }

            return ExprFactory("Property", receiver, _bound.MethodInfo(getMethod));
        }

        private static BoundExpression VisitSizeOfOperator(BoundSizeOfOperator node)
        {
            // error should have been reported earlier
            // Diagnostics.Add(ErrorCode.ERR_ExpressionTreeContainsPointerOp, node.Syntax.Location);
            return new BoundBadExpression(node.Syntax, default(LookupResultKind), ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(node), node.Type);
        }

        private BoundExpression VisitUnaryOperator(BoundUnaryOperator node)
        {
            var arg = node.Operand;
            var loweredArg = Visit(arg);
            var opKind = node.OperatorKind;
            var op = opKind & UnaryOperatorKind.OpMask;
            var isChecked = (opKind & UnaryOperatorKind.Checked) != 0;

            string opname;
            switch (op)
            {
                case UnaryOperatorKind.UnaryPlus:
                    if ((object)node.MethodOpt == null)
                    {
                        return loweredArg;
                    }
                    opname = "UnaryPlus";
                    break;
                case UnaryOperatorKind.UnaryMinus:
                    opname = isChecked ? "NegateChecked" : "Negate";
                    break;
                case UnaryOperatorKind.BitwiseComplement:
                case UnaryOperatorKind.LogicalNegation:
                    opname = "Not";
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(op);
            }

            if (node.OperatorKind.OperandTypes() == UnaryOperatorKind.Enum && (opKind & UnaryOperatorKind.Lifted) != 0)
            {
                Debug.Assert((object)node.MethodOpt == null);
                var promotedType = PromotedType(arg.Type.StrippedType().GetEnumUnderlyingType());
                promotedType = _nullableType.Construct(promotedType);
                loweredArg = Convert(loweredArg, arg.Type, promotedType, isChecked, false);
                var result = ExprFactory(opname, loweredArg);
                return Demote(result, node.Type, isChecked);
            }

            return ((object)node.MethodOpt == null)
                ? ExprFactory(opname, loweredArg)
                : ExprFactory(opname, loweredArg, _bound.MethodInfo(node.MethodOpt));
        }

        // ======================================================

        private BoundExpression ExprFactory(string name, params BoundExpression[] arguments)
        {
            return _bound.StaticCall(ExpressionType, name, arguments);
        }

        private BoundExpression ExprFactory(string name, ImmutableArray<TypeSymbol> typeArgs, params BoundExpression[] arguments)
        {
            return _bound.StaticCall(_ignoreAccessibility ? BinderFlags.IgnoreAccessibility : BinderFlags.None, ExpressionType, name, typeArgs, arguments);
        }

        private BoundExpression ExprFactory(WellKnownMember method, ImmutableArray<TypeSymbol> typeArgs, params BoundExpression[] arguments)
        {
            var m0 = _bound.WellKnownMethod(method);
            Debug.Assert((object)m0 != null);
            Debug.Assert(m0.ParameterCount == arguments.Length);
            var m1 = m0.Construct(typeArgs);
            return _bound.Call(null, m1, arguments);
        }

        private BoundExpression Constant(BoundExpression node)
        {
            return ExprFactory(
                "Constant",
                _bound.Convert(_objectType, node),
                _bound.Typeof(node.Type));
        }
    }
}
