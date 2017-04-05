// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal static partial class CSharpOperationFactory
    {
        public static IOperation Create(BoundNode boundNode)
        {
            switch (boundNode)
            {
                case BoundDeconstructValuePlaceholder boundDeconstructValuePlaceholder:
                    return CreateBoundDeconstructValuePlaceholderOperation(boundDeconstructValuePlaceholder);
                case BoundCall boundCall:
                    return CreateBoundCallOperation(boundCall);
                case BoundLocal boundLocal:
                    return CreateBoundLocalOperation(boundLocal);
                case BoundFieldAccess boundFieldAccess:
                    return CreateBoundFieldAccessOperation(boundFieldAccess);
                case BoundPropertyAccess boundPropertyAccess:
                    return CreateBoundPropertyAccessOperation(boundPropertyAccess);
                case BoundIndexerAccess boundIndexerAccess:
                    return CreateBoundIndexerAccessOperation(boundIndexerAccess);
                case BoundEventAccess boundEventAccess:
                    return CreateBoundEventAccessOperation(boundEventAccess);
                case BoundEventAssignmentOperator boundEventAssignmentOperator:
                    return CreateBoundEventAssignmentOperatorOperation(boundEventAssignmentOperator);
                case BoundDelegateCreationExpression boundDelegateCreationExpression:
                    return CreateBoundDelegateCreationExpressionOperation(boundDelegateCreationExpression);
                case BoundParameter boundParameter:
                    return CreateBoundParameterOperation(boundParameter);
                case BoundLiteral boundLiteral:
                    return CreateBoundLiteralOperation(boundLiteral);
                case BoundObjectCreationExpression boundObjectCreationExpression:
                    return CreateBoundObjectCreationExpressionOperation(boundObjectCreationExpression);
                case UnboundLambda unboundLambda:
                    return CreateUnboundLambdaOperation(unboundLambda);
                case BoundLambda boundLambda:
                    return CreateBoundLambdaOperation(boundLambda);
                case BoundConversion boundConversion:
                    return CreateBoundConversionOperation(boundConversion);
                case BoundAsOperator boundAsOperator:
                    return CreateBoundAsOperatorOperation(boundAsOperator);
                case BoundIsOperator boundIsOperator:
                    return CreateBoundIsOperatorOperation(boundIsOperator);
                case BoundSizeOfOperator boundSizeOfOperator:
                    return CreateBoundSizeOfOperatorOperation(boundSizeOfOperator);
                case BoundTypeOfOperator boundTypeOfOperator:
                    return CreateBoundTypeOfOperatorOperation(boundTypeOfOperator);
                case BoundArrayCreation boundArrayCreation:
                    return CreateBoundArrayCreationOperation(boundArrayCreation);
                case BoundArrayInitialization boundArrayInitialization:
                    return CreateBoundArrayInitializationOperation(boundArrayInitialization);
                case BoundDefaultOperator boundDefaultOperator:
                    return CreateBoundDefaultOperatorOperation(boundDefaultOperator);
                case BoundBaseReference boundBaseReference:
                    return CreateBoundBaseReferenceOperation(boundBaseReference);
                case BoundThisReference boundThisReference:
                    return CreateBoundThisReferenceOperation(boundThisReference);
                case BoundAssignmentOperator boundAssignmentOperator:
                    return CreateBoundAssignmentOperatorOperation(boundAssignmentOperator);
                case BoundCompoundAssignmentOperator boundCompoundAssignmentOperator:
                    return CreateBoundCompoundAssignmentOperatorOperation(boundCompoundAssignmentOperator);
                case BoundIncrementOperator boundIncrementOperator:
                    return CreateBoundIncrementOperatorOperation(boundIncrementOperator);
                case BoundBadExpression boundBadExpression:
                    return CreateBoundBadExpressionOperation(boundBadExpression);
                case BoundNewT boundNewT:
                    return CreateBoundNewTOperation(boundNewT);
                case BoundUnaryOperator boundUnaryOperator:
                    return CreateBoundUnaryOperatorOperation(boundUnaryOperator);
                case BoundBinaryOperator boundBinaryOperator:
                    return CreateBoundBinaryOperatorOperation(boundBinaryOperator);
                case BoundConditionalOperator boundConditionalOperator:
                    return CreateBoundConditionalOperatorOperation(boundConditionalOperator);
                case BoundNullCoalescingOperator boundNullCoalescingOperator:
                    return CreateBoundNullCoalescingOperatorOperation(boundNullCoalescingOperator);
                case BoundAwaitExpression boundAwaitExpression:
                    return CreateBoundAwaitExpressionOperation(boundAwaitExpression);
                case BoundArrayAccess boundArrayAccess:
                    return CreateBoundArrayAccessOperation(boundArrayAccess);
                case BoundPointerIndirectionOperator boundPointerIndirectionOperator:
                    return CreateBoundPointerIndirectionOperatorOperation(boundPointerIndirectionOperator);
                case BoundAddressOfOperator boundAddressOfOperator:
                    return CreateBoundAddressOfOperatorOperation(boundAddressOfOperator);
                case BoundImplicitReceiver boundImplicitReceiver:
                    return CreateBoundImplicitReceiverOperation(boundImplicitReceiver);
                case BoundConditionalAccess boundConditionalAccess:
                    return CreateBoundConditionalAccessOperation(boundConditionalAccess);
                case BoundConditionalReceiver boundConditionalReceiver:
                    return CreateBoundConditionalReceiverOperation(boundConditionalReceiver);
                case BoundFieldEqualsValue boundFieldEqualsValue:
                    return CreateBoundFieldEqualsValueOperation(boundFieldEqualsValue);
                case BoundPropertyEqualsValue boundPropertyEqualsValue:
                    return CreateBoundPropertyEqualsValueOperation(boundPropertyEqualsValue);
                case BoundParameterEqualsValue boundParameterEqualsValue:
                    return CreateBoundParameterEqualsValueOperation(boundParameterEqualsValue);
                case BoundBlock boundBlock:
                    return CreateBoundBlockOperation(boundBlock);
                case BoundContinueStatement boundContinueStatement:
                    return CreateBoundContinueStatementOperation(boundContinueStatement);
                case BoundBreakStatement boundBreakStatement:
                    return CreateBoundBreakStatementOperation(boundBreakStatement);
                case BoundYieldBreakStatement boundYieldBreakStatement:
                    return CreateBoundYieldBreakStatementOperation(boundYieldBreakStatement);
                case BoundGotoStatement boundGotoStatement:
                    return CreateBoundGotoStatementOperation(boundGotoStatement);
                case BoundNoOpStatement boundNoOpStatement:
                    return CreateBoundNoOpStatementOperation(boundNoOpStatement);
                case BoundIfStatement boundIfStatement:
                    return CreateBoundIfStatementOperation(boundIfStatement);
                case BoundWhileStatement boundWhileStatement:
                    return CreateBoundWhileStatementOperation(boundWhileStatement);
                case BoundDoStatement boundDoStatement:
                    return CreateBoundDoStatementOperation(boundDoStatement);
                case BoundForStatement boundForStatement:
                    return CreateBoundForStatementOperation(boundForStatement);
                case BoundForEachStatement boundForEachStatement:
                    return CreateBoundForEachStatementOperation(boundForEachStatement);
                case BoundSwitchStatement boundSwitchStatement:
                    return CreateBoundSwitchStatementOperation(boundSwitchStatement);
                case BoundSwitchLabel boundSwitchLabel:
                    return CreateBoundSwitchLabelOperation(boundSwitchLabel);
                case BoundTryStatement boundTryStatement:
                    return CreateBoundTryStatementOperation(boundTryStatement);
                case BoundCatchBlock boundCatchBlock:
                    return CreateBoundCatchBlockOperation(boundCatchBlock);
                case BoundFixedStatement boundFixedStatement:
                    return CreateBoundFixedStatementOperation(boundFixedStatement);
                case BoundUsingStatement boundUsingStatement:
                    return CreateBoundUsingStatementOperation(boundUsingStatement);
                case BoundThrowStatement boundThrowStatement:
                    return CreateBoundThrowStatementOperation(boundThrowStatement);
                case BoundReturnStatement boundReturnStatement:
                    return CreateBoundReturnStatementOperation(boundReturnStatement);
                case BoundYieldReturnStatement boundYieldReturnStatement:
                    return CreateBoundYieldReturnStatementOperation(boundYieldReturnStatement);
                case BoundLockStatement boundLockStatement:
                    return CreateBoundLockStatementOperation(boundLockStatement);
                case BoundBadStatement boundBadStatement:
                    return CreateBoundBadStatementOperation(boundBadStatement);
                case BoundLocalDeclaration boundLocalDeclaration:
                    return CreateBoundLocalDeclarationOperation(boundLocalDeclaration);
                case BoundMultipleLocalDeclarations boundMultipleLocalDeclarations:
                    return CreateBoundMultipleLocalDeclarationsOperation(boundMultipleLocalDeclarations);
                case BoundLabelStatement boundLabelStatement:
                    return CreateBoundLabelStatementOperation(boundLabelStatement);
                case BoundLabeledStatement boundLabeledStatement:
                    return CreateBoundLabeledStatementOperation(boundLabeledStatement);
                case BoundExpressionStatement boundExpressionStatement:
                    return CreateBoundExpressionStatementOperation(boundExpressionStatement);
                default:
                    throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(boundNode);
            }
        }
        private static IPlaceholderExpression CreateBoundDeconstructValuePlaceholderOperation(BoundDeconstructValuePlaceholder boundDeconstructValuePlaceholder)
        {
            bool isInvalid = boundDeconstructValuePlaceholder.HasErrors;
            SyntaxNode syntax = boundDeconstructValuePlaceholder.Syntax;
            ITypeSymbol type = boundDeconstructValuePlaceholder.Type;
            Optional<object> constantValue = ConvertToOptional(boundDeconstructValuePlaceholder.ConstantValue);
            return new PlaceholderExpression(isInvalid, syntax, type, constantValue);
        }
        private static IInvocationExpression CreateBoundCallOperation(BoundCall boundCall)
        {
            IMethodSymbol targetMethod = boundCall.Method;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => ((object)boundCall.Method == null || boundCall.Method.IsStatic) ? null : boundCall.ReceiverOpt);
            bool isVirtual = (object)boundCall.Method != null &&
                        boundCall.ReceiverOpt != null &&
                        (boundCall.Method.IsVirtual || boundCall.Method.IsAbstract || boundCall.Method.IsOverride) &&
                        !boundCall.ReceiverOpt.SuppressVirtualCalls;
            Lazy<ImmutableArray<IArgument>> argumentsInSourceOrder = new Lazy<ImmutableArray<IArgument>>(() => ((IInvocationExpression)boundCall).ArgumentsInSourceOrder /* MANUAL */);
            bool isInvalid = boundCall.HasErrors;
            SyntaxNode syntax = boundCall.Syntax;
            ITypeSymbol type = boundCall.Type;
            Optional<object> constantValue = ConvertToOptional(boundCall.ConstantValue);
            return new LazyInvocationExpression(targetMethod, instance, isVirtual, argumentsInSourceOrder, isInvalid, syntax, type, constantValue);
        }
        private static ILocalReferenceExpression CreateBoundLocalOperation(BoundLocal boundLocal)
        {
            ILocalSymbol local = boundLocal.LocalSymbol;
            bool isInvalid = boundLocal.HasErrors;
            SyntaxNode syntax = boundLocal.Syntax;
            ITypeSymbol type = boundLocal.Type;
            Optional<object> constantValue = ConvertToOptional(boundLocal.ConstantValue);
            return new LocalReferenceExpression(local, isInvalid, syntax, type, constantValue);
        }
        private static IFieldReferenceExpression CreateBoundFieldAccessOperation(BoundFieldAccess boundFieldAccess)
        {
            IFieldSymbol field = boundFieldAccess.FieldSymbol;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => boundFieldAccess.FieldSymbol.IsStatic ? null : boundFieldAccess.ReceiverOpt);
            ISymbol member = boundFieldAccess.FieldSymbol;
            bool isInvalid = boundFieldAccess.HasErrors;
            SyntaxNode syntax = boundFieldAccess.Syntax;
            ITypeSymbol type = boundFieldAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundFieldAccess.ConstantValue);
            return new LazyFieldReferenceExpression(field, instance, member, isInvalid, syntax, type, constantValue);
        }
        private static IPropertyReferenceExpression CreateBoundPropertyAccessOperation(BoundPropertyAccess boundPropertyAccess)
        {
            IPropertySymbol property = boundPropertyAccess.PropertySymbol;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => boundPropertyAccess.PropertySymbol.IsStatic ? null : boundPropertyAccess.ReceiverOpt);
            ISymbol member = boundPropertyAccess.PropertySymbol;
            bool isInvalid = boundPropertyAccess.HasErrors;
            SyntaxNode syntax = boundPropertyAccess.Syntax;
            ITypeSymbol type = boundPropertyAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundPropertyAccess.ConstantValue);
            return new LazyIndexedPropertyReferenceExpression(property, instance, member, isInvalid, syntax, type, constantValue);
        }
        private static IIndexedPropertyReferenceExpression CreateBoundIndexerAccessOperation(BoundIndexerAccess boundIndexerAccess)
        {
            IPropertySymbol property = boundIndexerAccess.Indexer;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => boundIndexerAccess.Indexer.IsStatic ? null : boundIndexerAccess.ReceiverOpt);
            ISymbol member = boundIndexerAccess.Indexer;
            bool isInvalid = boundIndexerAccess.HasErrors;
            SyntaxNode syntax = boundIndexerAccess.Syntax;
            ITypeSymbol type = boundIndexerAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundIndexerAccess.ConstantValue);
            return new LazyIndexedPropertyReferenceExpression(property, instance, member, isInvalid, syntax, type, constantValue);
        }
        private static IEventReferenceExpression CreateBoundEventAccessOperation(BoundEventAccess boundEventAccess)
        {
            IEventSymbol @event = boundEventAccess.EventSymbol;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => boundEventAccess.EventSymbol.IsStatic ? null : boundEventAccess.ReceiverOpt);
            ISymbol member = boundEventAccess.EventSymbol;
            bool isInvalid = boundEventAccess.HasErrors;
            SyntaxNode syntax = boundEventAccess.Syntax;
            ITypeSymbol type = boundEventAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundEventAccess.ConstantValue);
            return new LazyEventReferenceExpression(@event, instance, member, isInvalid, syntax, type, constantValue);
        }
        private static IEventAssignmentExpression CreateBoundEventAssignmentOperatorOperation(BoundEventAssignmentOperator boundEventAssignmentOperator)
        {
            IEventSymbol @event = boundEventAssignmentOperator.Event;
            Lazy<IOperation> eventInstance = new Lazy<IOperation>(() => boundEventAssignmentOperator.Event.IsStatic ? null : boundEventAssignmentOperator.ReceiverOpt);
            Lazy<IOperation> handlerValue = new Lazy<IOperation>(() => boundEventAssignmentOperator.Argument);
            bool adds = boundEventAssignmentOperator.IsAddition;
            bool isInvalid = boundEventAssignmentOperator.HasErrors;
            SyntaxNode syntax = boundEventAssignmentOperator.Syntax;
            ITypeSymbol type = boundEventAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundEventAssignmentOperator.ConstantValue);
            return new LazyEventAssignmentExpression(@event, eventInstance, handlerValue, adds, isInvalid, syntax, type, constantValue);
        }
        private static IMethodBindingExpression CreateBoundDelegateCreationExpressionOperation(BoundDelegateCreationExpression boundDelegateCreationExpression)
        {
            IMethodSymbol method = boundDelegateCreationExpression.MethodOpt;
            bool isVirtual = (object)boundDelegateCreationExpression.MethodOpt != null &&
                        (boundDelegateCreationExpression.MethodOpt.IsVirtual || boundDelegateCreationExpression.MethodOpt.IsAbstract || boundDelegateCreationExpression.MethodOpt.IsOverride) &&
                        !boundDelegateCreationExpression.SuppressVirtualCalls;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => ((IMemberReferenceExpression)boundDelegateCreationExpression).Instance /* MANUAL */);
            ISymbol member = boundDelegateCreationExpression.MethodOpt;
            bool isInvalid = boundDelegateCreationExpression.HasErrors;
            SyntaxNode syntax = boundDelegateCreationExpression.Argument.Syntax;
            ITypeSymbol type = boundDelegateCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundDelegateCreationExpression.ConstantValue);
            return new LazyMethodBindingExpression(method, isVirtual, instance, member, isInvalid, syntax, type, constantValue);
        }
        private static IParameterReferenceExpression CreateBoundParameterOperation(BoundParameter boundParameter)
        {
            IParameterSymbol parameter = boundParameter.ParameterSymbol;
            bool isInvalid = boundParameter.HasErrors;
            SyntaxNode syntax = boundParameter.Syntax;
            ITypeSymbol type = boundParameter.Type;
            Optional<object> constantValue = ConvertToOptional(boundParameter.ConstantValue);
            return new ParameterReferenceExpression(parameter, isInvalid, syntax, type, constantValue);
        }
        private static ILiteralExpression CreateBoundLiteralOperation(BoundLiteral boundLiteral)
        {
            string text = boundLiteral.Syntax.ToString();
            bool isInvalid = boundLiteral.HasErrors;
            SyntaxNode syntax = boundLiteral.Syntax;
            ITypeSymbol type = boundLiteral.Type;
            Optional<object> constantValue = ConvertToOptional(boundLiteral.ConstantValue);
            return new LiteralExpression(text, isInvalid, syntax, type, constantValue);
        }
        private static IObjectCreationExpression CreateBoundObjectCreationExpressionOperation(BoundObjectCreationExpression boundObjectCreationExpression)
        {
            IMethodSymbol constructor = boundObjectCreationExpression.Constructor;
            Lazy<ImmutableArray<ISymbolInitializer>> memberInitializers = new Lazy<ImmutableArray<ISymbolInitializer>>(() => ((IObjectCreationExpression)boundObjectCreationExpression).MemberInitializers /* MANUAL */);
            bool isInvalid = boundObjectCreationExpression.HasErrors;
            SyntaxNode syntax = boundObjectCreationExpression.Syntax;
            ITypeSymbol type = boundObjectCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundObjectCreationExpression.ConstantValue);
            return new LazyObjectCreationExpression(constructor, memberInitializers, isInvalid, syntax, type, constantValue);
        }
        private static IUnboundLambdaExpression CreateUnboundLambdaOperation(UnboundLambda unboundLambda)
        {
            bool isInvalid = unboundLambda.HasErrors;
            SyntaxNode syntax = unboundLambda.Syntax;
            ITypeSymbol type = unboundLambda.Type;
            Optional<object> constantValue = ConvertToOptional(unboundLambda.ConstantValue);
            return new UnboundLambdaExpression(isInvalid, syntax, type, constantValue);
        }
        private static ILambdaExpression CreateBoundLambdaOperation(BoundLambda boundLambda)
        {
            IMethodSymbol signature = boundLambda.Symbol;
            Lazy<IBlockStatement> body = new Lazy<IBlockStatement>(() => boundLambda.Body);
            bool isInvalid = boundLambda.HasErrors;
            SyntaxNode syntax = boundLambda.Syntax;
            ITypeSymbol type = boundLambda.Type;
            Optional<object> constantValue = ConvertToOptional(boundLambda.ConstantValue);
            return new LazyLambdaExpression(signature, body, isInvalid, syntax, type, constantValue);
        }
        private static IConversionExpression CreateBoundConversionOperation(BoundConversion boundConversion)
        {
            Lazy<IOperation> operand = new Lazy<IOperation>(() => boundConversion.Operand);
            ConversionKind conversionKind = ((IConversionExpression)boundConversion).ConversionKind /* MANUAL */;
            bool isExplicit = boundConversion.ExplicitCastInCode;
            bool usesOperatorMethod = boundConversion.ConversionKind == CSharp.ConversionKind.ExplicitUserDefined || boundConversion.ConversionKind == CSharp.ConversionKind.ImplicitUserDefined;
            IMethodSymbol operatorMethod = boundConversion.SymbolOpt;
            bool isInvalid = boundConversion.HasErrors;
            SyntaxNode syntax = boundConversion.Syntax;
            ITypeSymbol type = boundConversion.Type;
            Optional<object> constantValue = ConvertToOptional(boundConversion.ConstantValue);
            return new LazyConversionExpression(operand, conversionKind, isExplicit, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue);
        }
        private static IConversionExpression CreateBoundAsOperatorOperation(BoundAsOperator boundAsOperator)
        {
            Lazy<IOperation> operand = new Lazy<IOperation>(() => boundAsOperator.Operand);
            ConversionKind conversionKind = Semantics.ConversionKind.TryCast;
            bool isExplicit = true;
            bool usesOperatorMethod = false;
            IMethodSymbol operatorMethod = null;
            bool isInvalid = boundAsOperator.HasErrors;
            SyntaxNode syntax = boundAsOperator.Syntax;
            ITypeSymbol type = boundAsOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAsOperator.ConstantValue);
            return new LazyConversionExpression(operand, conversionKind, isExplicit, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue);
        }
        private static IIsTypeExpression CreateBoundIsOperatorOperation(BoundIsOperator boundIsOperator)
        {
            Lazy<IOperation> operand = new Lazy<IOperation>(() => boundIsOperator.Operand);
            ITypeSymbol isType = boundIsOperator.TargetType.Type;
            bool isInvalid = boundIsOperator.HasErrors;
            SyntaxNode syntax = boundIsOperator.Syntax;
            ITypeSymbol type = boundIsOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundIsOperator.ConstantValue);
            return new LazyIsTypeExpression(operand, isType, isInvalid, syntax, type, constantValue);
        }
        private static ISizeOfExpression CreateBoundSizeOfOperatorOperation(BoundSizeOfOperator boundSizeOfOperator)
        {
            ITypeSymbol typeOperand = boundSizeOfOperator.SourceType.Type;
            bool isInvalid = boundSizeOfOperator.HasErrors;
            SyntaxNode syntax = boundSizeOfOperator.Syntax;
            ITypeSymbol type = boundSizeOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundSizeOfOperator.ConstantValue);
            return new SizeOfExpression(typeOperand, isInvalid, syntax, type, constantValue);
        }
        private static ITypeOfExpression CreateBoundTypeOfOperatorOperation(BoundTypeOfOperator boundTypeOfOperator)
        {
            ITypeSymbol typeOperand = boundTypeOfOperator.SourceType.Type;
            bool isInvalid = boundTypeOfOperator.HasErrors;
            SyntaxNode syntax = boundTypeOfOperator.Syntax;
            ITypeSymbol type = boundTypeOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundTypeOfOperator.ConstantValue);
            return new TypeOfExpression(typeOperand, isInvalid, syntax, type, constantValue);
        }
        private static IArrayCreationExpression CreateBoundArrayCreationOperation(BoundArrayCreation boundArrayCreation)
        {
            ITypeSymbol elementType = ((IArrayCreationExpression)boundArrayCreation).ElementType /* MANUAL */;
            Lazy<ImmutableArray<IOperation>> dimensionSizes = new Lazy<ImmutableArray<IOperation>>(() => boundArrayCreation.Bounds.As<IOperation>());
            Lazy<IArrayInitializer> initializer = new Lazy<IArrayInitializer>(() => boundArrayCreation.InitializerOpt);
            bool isInvalid = boundArrayCreation.HasErrors;
            SyntaxNode syntax = boundArrayCreation.Syntax;
            ITypeSymbol type = boundArrayCreation.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayCreation.ConstantValue);
            return new LazyArrayCreationExpression(elementType, dimensionSizes, initializer, isInvalid, syntax, type, constantValue);
        }
        private static IArrayInitializer CreateBoundArrayInitializationOperation(BoundArrayInitialization boundArrayInitialization)
        {
            Lazy<ImmutableArray<IOperation>> elementValues = new Lazy<ImmutableArray<IOperation>>(() => boundArrayInitialization.Initializers.As<IOperation>());
            bool isInvalid = boundArrayInitialization.HasErrors;
            SyntaxNode syntax = boundArrayInitialization.Syntax;
            ITypeSymbol type = boundArrayInitialization.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayInitialization.ConstantValue);
            return new LazyArrayInitializer(elementValues, isInvalid, syntax, type, constantValue);
        }
        private static IDefaultValueExpression CreateBoundDefaultOperatorOperation(BoundDefaultOperator boundDefaultOperator)
        {
            bool isInvalid = boundDefaultOperator.HasErrors;
            SyntaxNode syntax = boundDefaultOperator.Syntax;
            ITypeSymbol type = boundDefaultOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundDefaultOperator.ConstantValue);
            return new DefaultValueExpression(isInvalid, syntax, type, constantValue);
        }
        private static IInstanceReferenceExpression CreateBoundBaseReferenceOperation(BoundBaseReference boundBaseReference)
        {
            InstanceReferenceKind instanceReferenceKind = InstanceReferenceKind.BaseClass;
            bool isInvalid = boundBaseReference.HasErrors;
            SyntaxNode syntax = boundBaseReference.Syntax;
            ITypeSymbol type = boundBaseReference.Type;
            Optional<object> constantValue = ConvertToOptional(boundBaseReference.ConstantValue);
            return new InstanceReferenceExpression(instanceReferenceKind, isInvalid, syntax, type, constantValue);
        }
        private static IInstanceReferenceExpression CreateBoundThisReferenceOperation(BoundThisReference boundThisReference)
        {
            InstanceReferenceKind instanceReferenceKind = boundThisReference.Syntax.Kind() == SyntaxKind.ThisExpression ? InstanceReferenceKind.Explicit : InstanceReferenceKind.Implicit;
            bool isInvalid = boundThisReference.HasErrors;
            SyntaxNode syntax = boundThisReference.Syntax;
            ITypeSymbol type = boundThisReference.Type;
            Optional<object> constantValue = ConvertToOptional(boundThisReference.ConstantValue);
            return new InstanceReferenceExpression(instanceReferenceKind, isInvalid, syntax, type, constantValue);
        }
        private static IAssignmentExpression CreateBoundAssignmentOperatorOperation(BoundAssignmentOperator boundAssignmentOperator)
        {
            Lazy<IOperation> target = new Lazy<IOperation>(() => boundAssignmentOperator.Left);
            Lazy<IOperation> value = new Lazy<IOperation>(() => boundAssignmentOperator.Right);
            bool isInvalid = boundAssignmentOperator.HasErrors;
            SyntaxNode syntax = boundAssignmentOperator.Syntax;
            ITypeSymbol type = boundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAssignmentOperator.ConstantValue);
            return new LazyAssignmentExpression(target, value, isInvalid, syntax, type, constantValue);
        }
        private static ICompoundAssignmentExpression CreateBoundCompoundAssignmentOperatorOperation(BoundCompoundAssignmentOperator boundCompoundAssignmentOperator)
        {
            BinaryOperationKind binaryOperationKind = CSharp.Expression.DeriveBinaryOperationKind(boundCompoundAssignmentOperator.Operator.Kind);
            Lazy<IOperation> target = new Lazy<IOperation>(() => boundCompoundAssignmentOperator.Left);
            Lazy<IOperation> value = new Lazy<IOperation>(() => boundCompoundAssignmentOperator.Right);
            bool usesOperatorMethod = (boundCompoundAssignmentOperator.Operator.Kind & BinaryOperatorKind.TypeMask) == BinaryOperatorKind.UserDefined;
            IMethodSymbol operatorMethod = boundCompoundAssignmentOperator.Operator.Method;
            bool isInvalid = boundCompoundAssignmentOperator.HasErrors;
            SyntaxNode syntax = boundCompoundAssignmentOperator.Syntax;
            ITypeSymbol type = boundCompoundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundCompoundAssignmentOperator.ConstantValue);
            return new LazyCompoundAssignmentExpression(binaryOperationKind, target, value, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue);
        }
        private static IIncrementExpression CreateBoundIncrementOperatorOperation(BoundIncrementOperator boundIncrementOperator)
        {
            UnaryOperationKind incrementOperationKind = CSharp.Expression.DeriveUnaryOperationKind(boundIncrementOperator.OperatorKind);
            BinaryOperationKind binaryOperationKind = CSharp.Expression.DeriveBinaryOperationKind(incrementOperationKind);
            Lazy<IOperation> target = new Lazy<IOperation>(() => boundIncrementOperator.Operand);
            Lazy<IOperation> value = new Lazy<IOperation>(() => CreateIncrementOneLiteralExpression(boundIncrementOperator));
            bool usesOperatorMethod = (boundIncrementOperator.OperatorKind & UnaryOperatorKind.TypeMask) == UnaryOperatorKind.UserDefined;
            IMethodSymbol operatorMethod = boundIncrementOperator.MethodOpt;
            bool isInvalid = boundIncrementOperator.HasErrors;
            SyntaxNode syntax = boundIncrementOperator.Syntax;
            ITypeSymbol type = boundIncrementOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundIncrementOperator.ConstantValue);
            return new LazyIncrementExpression(incrementOperationKind, binaryOperationKind, target, value, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue);
        }
        private static IInvalidExpression CreateBoundBadExpressionOperation(BoundBadExpression boundBadExpression)
        {
            bool isInvalid = boundBadExpression.HasErrors;
            SyntaxNode syntax = boundBadExpression.Syntax;
            ITypeSymbol type = boundBadExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundBadExpression.ConstantValue);
            return new InvalidExpression(isInvalid, syntax, type, constantValue);
        }
        private static ITypeParameterObjectCreationExpression CreateBoundNewTOperation(BoundNewT boundNewT)
        {
            bool isInvalid = boundNewT.HasErrors;
            SyntaxNode syntax = boundNewT.Syntax;
            ITypeSymbol type = boundNewT.Type;
            Optional<object> constantValue = ConvertToOptional(boundNewT.ConstantValue);
            return new TypeParameterObjectCreationExpression(isInvalid, syntax, type, constantValue);
        }
        private static IUnaryOperatorExpression CreateBoundUnaryOperatorOperation(BoundUnaryOperator boundUnaryOperator)
        {
            UnaryOperationKind unaryOperationKind = CSharp.Expression.DeriveUnaryOperationKind(boundUnaryOperator.OperatorKind);
            Lazy<IOperation> operand = new Lazy<IOperation>(() => boundUnaryOperator.Operand);
            bool usesOperatorMethod = (boundUnaryOperator.OperatorKind & UnaryOperatorKind.TypeMask) == UnaryOperatorKind.UserDefined;
            IMethodSymbol operatorMethod = boundUnaryOperator.MethodOpt;
            bool isInvalid = boundUnaryOperator.HasErrors;
            SyntaxNode syntax = boundUnaryOperator.Syntax;
            ITypeSymbol type = boundUnaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundUnaryOperator.ConstantValue);
            return new LazyUnaryOperatorExpression(unaryOperationKind, operand, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue);
        }
        private static IBinaryOperatorExpression CreateBoundBinaryOperatorOperation(BoundBinaryOperator boundBinaryOperator)
        {
            BinaryOperationKind binaryOperationKind = CSharp.Expression.DeriveBinaryOperationKind(boundBinaryOperator.OperatorKind);
            Lazy<IOperation> leftOperand = new Lazy<IOperation>(() => boundBinaryOperator.Left);
            Lazy<IOperation> rightOperand = new Lazy<IOperation>(() => boundBinaryOperator.Right);
            bool usesOperatorMethod = (boundBinaryOperator.OperatorKind & BinaryOperatorKind.TypeMask) == BinaryOperatorKind.UserDefined;
            IMethodSymbol operatorMethod = boundBinaryOperator.MethodOpt;
            bool isInvalid = boundBinaryOperator.HasErrors;
            SyntaxNode syntax = boundBinaryOperator.Syntax;
            ITypeSymbol type = boundBinaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundBinaryOperator.ConstantValue);
            return new LazyBinaryOperatorExpression(binaryOperationKind, leftOperand, rightOperand, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue);
        }
        private static IConditionalChoiceExpression CreateBoundConditionalOperatorOperation(BoundConditionalOperator boundConditionalOperator)
        {
            Lazy<IOperation> condition = new Lazy<IOperation>(() => boundConditionalOperator.Condition);
            Lazy<IOperation> ifTrueValue = new Lazy<IOperation>(() => boundConditionalOperator.Consequence);
            Lazy<IOperation> ifFalseValue = new Lazy<IOperation>(() => boundConditionalOperator.Alternative);
            bool isInvalid = boundConditionalOperator.HasErrors;
            SyntaxNode syntax = boundConditionalOperator.Syntax;
            ITypeSymbol type = boundConditionalOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalOperator.ConstantValue);
            return new LazyConditionalChoiceExpression(condition, ifTrueValue, ifFalseValue, isInvalid, syntax, type, constantValue);
        }
        private static INullCoalescingExpression CreateBoundNullCoalescingOperatorOperation(BoundNullCoalescingOperator boundNullCoalescingOperator)
        {
            Lazy<IOperation> primaryOperand = new Lazy<IOperation>(() => boundNullCoalescingOperator.LeftOperand);
            Lazy<IOperation> secondaryOperand = new Lazy<IOperation>(() => boundNullCoalescingOperator.RightOperand);
            bool isInvalid = boundNullCoalescingOperator.HasErrors;
            SyntaxNode syntax = boundNullCoalescingOperator.Syntax;
            ITypeSymbol type = boundNullCoalescingOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundNullCoalescingOperator.ConstantValue);
            return new LazyNullCoalescingExpression(primaryOperand, secondaryOperand, isInvalid, syntax, type, constantValue);
        }
        private static IAwaitExpression CreateBoundAwaitExpressionOperation(BoundAwaitExpression boundAwaitExpression)
        {
            Lazy<IOperation> awaitedValue = new Lazy<IOperation>(() => boundAwaitExpression.Expression);
            bool isInvalid = boundAwaitExpression.HasErrors;
            SyntaxNode syntax = boundAwaitExpression.Syntax;
            ITypeSymbol type = boundAwaitExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundAwaitExpression.ConstantValue);
            return new LazyAwaitExpression(awaitedValue, isInvalid, syntax, type, constantValue);
        }
        private static IArrayElementReferenceExpression CreateBoundArrayAccessOperation(BoundArrayAccess boundArrayAccess)
        {
            Lazy<IOperation> arrayReference = new Lazy<IOperation>(() => boundArrayAccess.Expression);
            Lazy<ImmutableArray<IOperation>> indices = new Lazy<ImmutableArray<IOperation>>(() => boundArrayAccess.Indices.As<IOperation>());
            bool isInvalid = boundArrayAccess.HasErrors;
            SyntaxNode syntax = boundArrayAccess.Syntax;
            ITypeSymbol type = boundArrayAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayAccess.ConstantValue);
            return new LazyArrayElementReferenceExpression(arrayReference, indices, isInvalid, syntax, type, constantValue);
        }
        private static IPointerIndirectionReferenceExpression CreateBoundPointerIndirectionOperatorOperation(BoundPointerIndirectionOperator boundPointerIndirectionOperator)
        {
            Lazy<IOperation> pointer = new Lazy<IOperation>(() => boundPointerIndirectionOperator.Operand);
            bool isInvalid = boundPointerIndirectionOperator.HasErrors;
            SyntaxNode syntax = boundPointerIndirectionOperator.Syntax;
            ITypeSymbol type = boundPointerIndirectionOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundPointerIndirectionOperator.ConstantValue);
            return new LazyPointerIndirectionReferenceExpression(pointer, isInvalid, syntax, type, constantValue);
        }
        private static IAddressOfExpression CreateBoundAddressOfOperatorOperation(BoundAddressOfOperator boundAddressOfOperator)
        {
            Lazy<IOperation> reference = new Lazy<IOperation>(() => boundAddressOfOperator.Operand);
            bool isInvalid = boundAddressOfOperator.HasErrors;
            SyntaxNode syntax = boundAddressOfOperator.Syntax;
            ITypeSymbol type = boundAddressOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAddressOfOperator.ConstantValue);
            return new LazyAddressOfExpression(reference, isInvalid, syntax, type, constantValue);
        }
        private static IInstanceReferenceExpression CreateBoundImplicitReceiverOperation(BoundImplicitReceiver boundImplicitReceiver)
        {
            InstanceReferenceKind instanceReferenceKind = InstanceReferenceKind.Implicit;
            bool isInvalid = boundImplicitReceiver.HasErrors;
            SyntaxNode syntax = boundImplicitReceiver.Syntax;
            ITypeSymbol type = boundImplicitReceiver.Type;
            Optional<object> constantValue = ConvertToOptional(boundImplicitReceiver.ConstantValue);
            return new InstanceReferenceExpression(instanceReferenceKind, isInvalid, syntax, type, constantValue);
        }
        private static IConditionalAccessExpression CreateBoundConditionalAccessOperation(BoundConditionalAccess boundConditionalAccess)
        {
            Lazy<IOperation> conditionalValue = new Lazy<IOperation>(() => boundConditionalAccess.AccessExpression);
            Lazy<IOperation> conditionalInstance = new Lazy<IOperation>(() => boundConditionalAccess.Receiver);
            bool isInvalid = boundConditionalAccess.HasErrors;
            SyntaxNode syntax = boundConditionalAccess.Syntax;
            ITypeSymbol type = boundConditionalAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalAccess.ConstantValue);
            return new LazyConditionalAccessExpression(conditionalValue, conditionalInstance, isInvalid, syntax, type, constantValue);
        }
        private static IConditionalAccessInstanceExpression CreateBoundConditionalReceiverOperation(BoundConditionalReceiver boundConditionalReceiver)
        {
            bool isInvalid = boundConditionalReceiver.HasErrors;
            SyntaxNode syntax = boundConditionalReceiver.Syntax;
            ITypeSymbol type = boundConditionalReceiver.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalReceiver.ConstantValue);
            return new ConditionalAccessInstanceExpression(isInvalid, syntax, type, constantValue);
        }
        private static IFieldInitializer CreateBoundFieldEqualsValueOperation(BoundFieldEqualsValue boundFieldEqualsValue)
        {
            ImmutableArray<IFieldSymbol> initializedFields = ImmutableArray.Create<IFieldSymbol>(boundFieldEqualsValue.Field);
            Lazy<IOperation> value = new Lazy<IOperation>(() => boundFieldEqualsValue.Value);
            OperationKind kind = OperationKind.FieldInitializerAtDeclaration;
            bool isInvalid = ((IOperation)boundFieldEqualsValue.Value).IsInvalid;
            SyntaxNode syntax = boundFieldEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyFieldInitializer(initializedFields, value, kind, isInvalid, syntax, type, constantValue);
        }
        private static IPropertyInitializer CreateBoundPropertyEqualsValueOperation(BoundPropertyEqualsValue boundPropertyEqualsValue)
        {
            IPropertySymbol initializedProperty = boundPropertyEqualsValue.Property;
            Lazy<IOperation> value = new Lazy<IOperation>(() => boundPropertyEqualsValue.Value);
            OperationKind kind = OperationKind.PropertyInitializerAtDeclaration;
            bool isInvalid = ((IOperation)boundPropertyEqualsValue.Value).IsInvalid;
            SyntaxNode syntax = boundPropertyEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyPropertyInitializer(initializedProperty, value, kind, isInvalid, syntax, type, constantValue);
        }
        private static IParameterInitializer CreateBoundParameterEqualsValueOperation(BoundParameterEqualsValue boundParameterEqualsValue)
        {
            IParameterSymbol parameter = boundParameterEqualsValue.Parameter;
            Lazy<IOperation> value = new Lazy<IOperation>(() => boundParameterEqualsValue.Value);
            OperationKind kind = OperationKind.ParameterInitializerAtDeclaration;
            bool isInvalid = ((IOperation)boundParameterEqualsValue.Value).IsInvalid;
            SyntaxNode syntax = boundParameterEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyParameterInitializer(parameter, value, kind, isInvalid, syntax, type, constantValue);
        }
        private static IBlockStatement CreateBoundBlockOperation(BoundBlock boundBlock)
        {
            Lazy<ImmutableArray<IOperation>> statements = new Lazy<ImmutableArray<IOperation>>(() => ((IBlockStatement)boundBlock).Statements /* MANUAL */);
            ImmutableArray<ILocalSymbol> locals = boundBlock.Locals.As<ILocalSymbol>();
            bool isInvalid = boundBlock.HasErrors;
            SyntaxNode syntax = boundBlock.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyBlockStatement(statements, locals, isInvalid, syntax, type, constantValue);
        }
        private static IBranchStatement CreateBoundContinueStatementOperation(BoundContinueStatement boundContinueStatement)
        {
            ILabelSymbol target = boundContinueStatement.Label;
            BranchKind branchKind = BranchKind.Continue;
            bool isInvalid = boundContinueStatement.HasErrors;
            SyntaxNode syntax = boundContinueStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new BranchStatement(target, branchKind, isInvalid, syntax, type, constantValue);
        }
        private static IBranchStatement CreateBoundBreakStatementOperation(BoundBreakStatement boundBreakStatement)
        {
            ILabelSymbol target = boundBreakStatement.Label;
            BranchKind branchKind = BranchKind.Break;
            bool isInvalid = boundBreakStatement.HasErrors;
            SyntaxNode syntax = boundBreakStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new BranchStatement(target, branchKind, isInvalid, syntax, type, constantValue);
        }
        private static IReturnStatement CreateBoundYieldBreakStatementOperation(BoundYieldBreakStatement boundYieldBreakStatement)
        {
            Lazy<IOperation> returnedValue = new Lazy<IOperation>(() => null);
            bool isInvalid = boundYieldBreakStatement.HasErrors;
            SyntaxNode syntax = boundYieldBreakStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyReturnStatement(returnedValue, isInvalid, syntax, type, constantValue);
        }
        private static IBranchStatement CreateBoundGotoStatementOperation(BoundGotoStatement boundGotoStatement)
        {
            ILabelSymbol target = boundGotoStatement.Label;
            BranchKind branchKind = BranchKind.GoTo;
            bool isInvalid = boundGotoStatement.HasErrors;
            SyntaxNode syntax = boundGotoStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new BranchStatement(target, branchKind, isInvalid, syntax, type, constantValue);
        }
        private static IEmptyStatement CreateBoundNoOpStatementOperation(BoundNoOpStatement boundNoOpStatement)
        {
            bool isInvalid = boundNoOpStatement.HasErrors;
            SyntaxNode syntax = boundNoOpStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new EmptyStatement(isInvalid, syntax, type, constantValue);
        }
        private static IIfStatement CreateBoundIfStatementOperation(BoundIfStatement boundIfStatement)
        {
            Lazy<IOperation> condition = new Lazy<IOperation>(() => boundIfStatement.Condition);
            Lazy<IOperation> ifTrueStatement = new Lazy<IOperation>(() => boundIfStatement.Consequence);
            Lazy<IOperation> ifFalseStatement = new Lazy<IOperation>(() => boundIfStatement.AlternativeOpt);
            bool isInvalid = boundIfStatement.HasErrors;
            SyntaxNode syntax = boundIfStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyIfStatement(condition, ifTrueStatement, ifFalseStatement, isInvalid, syntax, type, constantValue);
        }
        private static IWhileUntilLoopStatement CreateBoundWhileStatementOperation(BoundWhileStatement boundWhileStatement)
        {
            bool isTopTest = true;
            bool isWhile = true;
            Lazy<IOperation> condition = new Lazy<IOperation>(() => boundWhileStatement.Condition);
            LoopKind loopKind = LoopKind.WhileUntil;
            Lazy<IOperation> body = new Lazy<IOperation>(() => boundWhileStatement.Body);
            bool isInvalid = boundWhileStatement.HasErrors;
            SyntaxNode syntax = boundWhileStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyWhileUntilLoopStatement(isTopTest, isWhile, condition, loopKind, body, isInvalid, syntax, type, constantValue);
        }
        private static IWhileUntilLoopStatement CreateBoundDoStatementOperation(BoundDoStatement boundDoStatement)
        {
            bool isTopTest = false;
            bool isWhile = true;
            Lazy<IOperation> condition = new Lazy<IOperation>(() => boundDoStatement.Condition);
            LoopKind loopKind = LoopKind.WhileUntil;
            Lazy<IOperation> body = new Lazy<IOperation>(() => boundDoStatement.Body);
            bool isInvalid = boundDoStatement.HasErrors;
            SyntaxNode syntax = boundDoStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyWhileUntilLoopStatement(isTopTest, isWhile, condition, loopKind, body, isInvalid, syntax, type, constantValue);
        }
        private static IForLoopStatement CreateBoundForStatementOperation(BoundForStatement boundForStatement)
        {
            Lazy<ImmutableArray<IOperation>> before = new Lazy<ImmutableArray<IOperation>>(() => ToStatements(boundForStatement.Initializer));
            Lazy<ImmutableArray<IOperation>> atLoopBottom = new Lazy<ImmutableArray<IOperation>>(() => ToStatements(boundForStatement.Increment));
            ImmutableArray<ILocalSymbol> locals = boundForStatement.OuterLocals.As<ILocalSymbol>();
            Lazy<IOperation> condition = new Lazy<IOperation>(() => boundForStatement.Condition);
            LoopKind loopKind = LoopKind.For;
            Lazy<IOperation> body = new Lazy<IOperation>(() => boundForStatement.Body);
            bool isInvalid = boundForStatement.HasErrors;
            SyntaxNode syntax = boundForStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyForLoopStatement(before, atLoopBottom, locals, condition, loopKind, body, isInvalid, syntax, type, constantValue);
        }
        private static IForEachLoopStatement CreateBoundForEachStatementOperation(BoundForEachStatement boundForEachStatement)
        {
            ILocalSymbol iterationVariable = boundForEachStatement.IterationVariables.Length == 1 ?
                                                                                    boundForEachStatement.IterationVariables.FirstOrDefault() :
                                                                                    null;
            Lazy<IOperation> collection = new Lazy<IOperation>(() => boundForEachStatement.Expression);
            LoopKind loopKind = LoopKind.ForEach;
            Lazy<IOperation> body = new Lazy<IOperation>(() => boundForEachStatement.Body);
            bool isInvalid = boundForEachStatement.HasErrors;
            SyntaxNode syntax = boundForEachStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyForEachLoopStatement(iterationVariable, collection, loopKind, body, isInvalid, syntax, type, constantValue);
        }
        private static ISwitchStatement CreateBoundSwitchStatementOperation(BoundSwitchStatement boundSwitchStatement)
        {
            Lazy<IOperation> value = new Lazy<IOperation>(() => boundSwitchStatement.Expression);
            Lazy<ImmutableArray<ISwitchCase>> cases = new Lazy<ImmutableArray<ISwitchCase>>(() => ((ISwitchStatement)boundSwitchStatement).Cases /* MANUAL */);
            bool isInvalid = boundSwitchStatement.HasErrors;
            SyntaxNode syntax = boundSwitchStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazySwitchStatement(value, cases, isInvalid, syntax, type, constantValue);
        }
        private static ISingleValueCaseClause CreateBoundSwitchLabelOperation(BoundSwitchLabel boundSwitchLabel)
        {
            Lazy<IOperation> value = new Lazy<IOperation>(() => boundSwitchLabel.ExpressionOpt);
            BinaryOperationKind equality = ((ISingleValueCaseClause)boundSwitchLabel).Equality /* MANUAL */;
            CaseKind caseKind = boundSwitchLabel.ExpressionOpt != null ? CaseKind.SingleValue : CaseKind.Default;
            bool isInvalid = boundSwitchLabel.HasErrors;
            SyntaxNode syntax = boundSwitchLabel.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazySingleValueCaseClause(value, equality, caseKind, isInvalid, syntax, type, constantValue);
        }
        private static ITryStatement CreateBoundTryStatementOperation(BoundTryStatement boundTryStatement)
        {
            Lazy<IBlockStatement> body = new Lazy<IBlockStatement>(() => boundTryStatement.TryBlock);
            Lazy<ImmutableArray<ICatchClause>> catches = new Lazy<ImmutableArray<ICatchClause>>(() => boundTryStatement.CatchBlocks.As<ICatchClause>());
            Lazy<IBlockStatement> finallyHandler = new Lazy<IBlockStatement>(() => boundTryStatement.FinallyBlockOpt);
            bool isInvalid = boundTryStatement.HasErrors;
            SyntaxNode syntax = boundTryStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyTryStatement(body, catches, finallyHandler, isInvalid, syntax, type, constantValue);
        }
        private static ICatchClause CreateBoundCatchBlockOperation(BoundCatchBlock boundCatchBlock)
        {
            Lazy<IBlockStatement> handler = new Lazy<IBlockStatement>(() => boundCatchBlock.Body);
            ITypeSymbol caughtType = boundCatchBlock.ExceptionTypeOpt;
            Lazy<IOperation> filter = new Lazy<IOperation>(() => boundCatchBlock.ExceptionFilterOpt);
            ILocalSymbol exceptionLocal = ((ICatchClause)boundCatchBlock).ExceptionLocal /* MANUAL */;
            bool isInvalid = boundCatchBlock.Body.HasErrors || (boundCatchBlock.ExceptionFilterOpt != null && boundCatchBlock.ExceptionFilterOpt.HasErrors);
            SyntaxNode syntax = boundCatchBlock.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyCatchClause(handler, caughtType, filter, exceptionLocal, isInvalid, syntax, type, constantValue);
        }
        private static IFixedStatement CreateBoundFixedStatementOperation(BoundFixedStatement boundFixedStatement)
        {
            Lazy<IVariableDeclarationStatement> variables = new Lazy<IVariableDeclarationStatement>(() => boundFixedStatement.Declarations);
            Lazy<IOperation> body = new Lazy<IOperation>(() => boundFixedStatement.Body);
            bool isInvalid = boundFixedStatement.HasErrors;
            SyntaxNode syntax = boundFixedStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyFixedStatement(variables, body, isInvalid, syntax, type, constantValue);
        }
        private static IUsingStatement CreateBoundUsingStatementOperation(BoundUsingStatement boundUsingStatement)
        {
            Lazy<IOperation> body = new Lazy<IOperation>(() => boundUsingStatement.Body);
            Lazy<IVariableDeclarationStatement> declaration = new Lazy<IVariableDeclarationStatement>(() => boundUsingStatement.DeclarationsOpt);
            Lazy<IOperation> value = new Lazy<IOperation>(() => boundUsingStatement.ExpressionOpt);
            bool isInvalid = boundUsingStatement.HasErrors;
            SyntaxNode syntax = boundUsingStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyUsingStatement(body, declaration, value, isInvalid, syntax, type, constantValue);
        }
        private static IThrowStatement CreateBoundThrowStatementOperation(BoundThrowStatement boundThrowStatement)
        {
            Lazy<IOperation> thrownObject = new Lazy<IOperation>(() => boundThrowStatement.ExpressionOpt);
            bool isInvalid = boundThrowStatement.HasErrors;
            SyntaxNode syntax = boundThrowStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyThrowStatement(thrownObject, isInvalid, syntax, type, constantValue);
        }
        private static IReturnStatement CreateBoundReturnStatementOperation(BoundReturnStatement boundReturnStatement)
        {
            Lazy<IOperation> returnedValue = new Lazy<IOperation>(() => boundReturnStatement.ExpressionOpt);
            bool isInvalid = boundReturnStatement.HasErrors;
            SyntaxNode syntax = boundReturnStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyReturnStatement(returnedValue, isInvalid, syntax, type, constantValue);
        }
        private static IReturnStatement CreateBoundYieldReturnStatementOperation(BoundYieldReturnStatement boundYieldReturnStatement)
        {
            Lazy<IOperation> returnedValue = new Lazy<IOperation>(() => boundYieldReturnStatement.Expression);
            bool isInvalid = boundYieldReturnStatement.HasErrors;
            SyntaxNode syntax = boundYieldReturnStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyReturnStatement(returnedValue, isInvalid, syntax, type, constantValue);
        }
        private static ILockStatement CreateBoundLockStatementOperation(BoundLockStatement boundLockStatement)
        {
            Lazy<IOperation> lockedObject = new Lazy<IOperation>(() => boundLockStatement.Argument);
            Lazy<IOperation> body = new Lazy<IOperation>(() => boundLockStatement.Body);
            bool isInvalid = boundLockStatement.HasErrors;
            SyntaxNode syntax = boundLockStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyLockStatement(lockedObject, body, isInvalid, syntax, type, constantValue);
        }
        private static IInvalidStatement CreateBoundBadStatementOperation(BoundBadStatement boundBadStatement)
        {
            bool isInvalid = boundBadStatement.HasErrors;
            SyntaxNode syntax = boundBadStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new InvalidStatement(isInvalid, syntax, type, constantValue);
        }
        private static IVariableDeclarationStatement CreateBoundLocalDeclarationOperation(BoundLocalDeclaration boundLocalDeclaration)
        {
            Lazy<ImmutableArray<IVariableDeclaration>> variables = new Lazy<ImmutableArray<IVariableDeclaration>>(() => ((IVariableDeclarationStatement)boundLocalDeclaration).Variables /* MANUAL */);
            bool isInvalid = boundLocalDeclaration.HasErrors;
            SyntaxNode syntax = boundLocalDeclaration.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyVariableDeclarationStatement(variables, isInvalid, syntax, type, constantValue);
        }
        private static IVariableDeclarationStatement CreateBoundMultipleLocalDeclarationsOperation(BoundMultipleLocalDeclarations boundMultipleLocalDeclarations)
        {
            Lazy<ImmutableArray<IVariableDeclaration>> variables = new Lazy<ImmutableArray<IVariableDeclaration>>(() => ((IVariableDeclarationStatement)boundMultipleLocalDeclarations).Variables /* MANUAL */);
            bool isInvalid = boundMultipleLocalDeclarations.HasErrors;
            SyntaxNode syntax = boundMultipleLocalDeclarations.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyVariableDeclarationStatement(variables, isInvalid, syntax, type, constantValue);
        }
        private static ILabelStatement CreateBoundLabelStatementOperation(BoundLabelStatement boundLabelStatement)
        {
            ILabelSymbol label = boundLabelStatement.Label;
            Lazy<IOperation> labeledStatement = new Lazy<IOperation>(() => null);
            bool isInvalid = boundLabelStatement.HasErrors;
            SyntaxNode syntax = boundLabelStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyLabelStatement(label, labeledStatement, isInvalid, syntax, type, constantValue);
        }
        private static ILabelStatement CreateBoundLabeledStatementOperation(BoundLabeledStatement boundLabeledStatement)
        {
            ILabelSymbol label = boundLabeledStatement.Label;
            Lazy<IOperation> labeledStatement = new Lazy<IOperation>(() => boundLabeledStatement.Body);
            bool isInvalid = boundLabeledStatement.HasErrors;
            SyntaxNode syntax = boundLabeledStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyLabelStatement(label, labeledStatement, isInvalid, syntax, type, constantValue);
        }
        private static IExpressionStatement CreateBoundExpressionStatementOperation(BoundExpressionStatement boundExpressionStatement)
        {
            Lazy<IOperation> expression = new Lazy<IOperation>(() => boundExpressionStatement.Expression);
            bool isInvalid = boundExpressionStatement.HasErrors;
            SyntaxNode syntax = boundExpressionStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyExpressionStatement(expression, isInvalid, syntax, type, constantValue);
        }


        private static Optional<object> ConvertToOptional(ConstantValue value)
        {
            return value != null ? new Optional<object>(value.Value) : default(Optional<object>);
        }

        private static ImmutableArray<IOperation> ToStatements(BoundStatement statement)
        {
            BoundStatementList statementList = statement as BoundStatementList;
            if (statementList != null)
            {
                return statementList.Statements.As<IOperation>();
            }
            else if (statement == null)
            {
                return ImmutableArray<IOperation>.Empty;
            }

            return ImmutableArray.Create<IOperation>(statement);
        }

        private static ILiteralExpression CreateIncrementOneLiteralExpression(BoundIncrementOperator boundIncrementOperator)
        {
            string text = boundIncrementOperator.Syntax.ToString();
            bool isInvalid = false;
            SyntaxNode syntax = boundIncrementOperator.Syntax;
            ITypeSymbol type = boundIncrementOperator.Type;
            Optional<object> constantValue = ConvertToOptional(Semantics.Expression.SynthesizeNumeric(boundIncrementOperator.Type, 1));
            return new LiteralExpression(text, isInvalid, syntax, type, constantValue);
        }
    }
}
