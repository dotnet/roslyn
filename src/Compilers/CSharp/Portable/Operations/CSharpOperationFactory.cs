// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal static partial class CSharpOperationFactory
    {
        private static readonly ConditionalWeakTable<BoundNode, IOperation> s_cache = new ConditionalWeakTable<BoundNode, IOperation>();

        public static IOperation Create(BoundNode boundNode)
        {
            return s_cache.GetValue(boundNode, n => CreateInternal(n));
        }

        private static IOperation CreateInternal(BoundNode boundNode)
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
            Lazy<IOperation> instance = new Lazy<IOperation>(() => (IOperation)Create(((object)boundCall.Method == null || boundCall.Method.IsStatic) ? null : boundCall.ReceiverOpt));
            bool isVirtual = (object)boundCall.Method != null &&
                        boundCall.ReceiverOpt != null &&
                        (boundCall.Method.IsVirtual || boundCall.Method.IsAbstract || boundCall.Method.IsOverride) &&
                        !boundCall.ReceiverOpt.SuppressVirtualCalls;
            Lazy<ImmutableArray<IArgument>> argumentsInSourceOrder = new Lazy<ImmutableArray<IArgument>>(() => GetArgumentsInSourceOrder(boundCall));
            Lazy<ImmutableArray<IArgument>> argumentsInParameterOrder = new Lazy<ImmutableArray<IArgument>>(() => DeriveArguments(boundCall.Arguments, boundCall.ArgumentNamesOpt, boundCall.ArgsToParamsOpt, boundCall.ArgumentRefKindsOpt, boundCall.Method.Parameters, boundCall.Syntax));
            bool isInvalid = boundCall.HasErrors;
            SyntaxNode syntax = boundCall.Syntax;
            ITypeSymbol type = boundCall.Type;
            Optional<object> constantValue = ConvertToOptional(boundCall.ConstantValue);
            return new LazyInvocationExpression(targetMethod, instance, isVirtual, argumentsInSourceOrder, argumentsInParameterOrder, isInvalid, syntax, type, constantValue);
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
            Lazy<IOperation> instance = new Lazy<IOperation>(() => (IOperation)Create(boundFieldAccess.FieldSymbol.IsStatic ? null : boundFieldAccess.ReceiverOpt));
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
            Lazy<IOperation> instance = new Lazy<IOperation>(() => (IOperation)Create(boundPropertyAccess.PropertySymbol.IsStatic ? null : boundPropertyAccess.ReceiverOpt));
            ISymbol member = boundPropertyAccess.PropertySymbol;
            bool isInvalid = boundPropertyAccess.HasErrors;
            SyntaxNode syntax = boundPropertyAccess.Syntax;
            ITypeSymbol type = boundPropertyAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundPropertyAccess.ConstantValue);
            return new LazyPropertyReferenceExpression(property, instance, member, isInvalid, syntax, type, constantValue);
        }
        private static IIndexedPropertyReferenceExpression CreateBoundIndexerAccessOperation(BoundIndexerAccess boundIndexerAccess)
        {
            IPropertySymbol property = boundIndexerAccess.Indexer;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => (IOperation)Create(boundIndexerAccess.Indexer.IsStatic ? null : boundIndexerAccess.ReceiverOpt));
            ISymbol member = boundIndexerAccess.Indexer;
            Lazy<ImmutableArray<IArgument>> argumentsInParameterOrder = new Lazy<ImmutableArray<IArgument>>(() => DeriveArguments(boundIndexerAccess.Arguments, boundIndexerAccess.ArgumentNamesOpt, boundIndexerAccess.ArgsToParamsOpt, boundIndexerAccess.ArgumentRefKindsOpt, boundIndexerAccess.Indexer.Parameters, boundIndexerAccess.Syntax));
            bool isInvalid = boundIndexerAccess.HasErrors;
            SyntaxNode syntax = boundIndexerAccess.Syntax;
            ITypeSymbol type = boundIndexerAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundIndexerAccess.ConstantValue);
            return new LazyIndexedPropertyReferenceExpression(property, instance, member, argumentsInParameterOrder, isInvalid, syntax, type, constantValue);
        }
        private static IEventReferenceExpression CreateBoundEventAccessOperation(BoundEventAccess boundEventAccess)
        {
            IEventSymbol @event = boundEventAccess.EventSymbol;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => (IOperation)Create(boundEventAccess.EventSymbol.IsStatic ? null : boundEventAccess.ReceiverOpt));
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
            Lazy<IOperation> eventInstance = new Lazy<IOperation>(() => (IOperation)Create(boundEventAssignmentOperator.Event.IsStatic ? null : boundEventAssignmentOperator.ReceiverOpt));
            Lazy<IOperation> handlerValue = new Lazy<IOperation>(() => (IOperation)Create(boundEventAssignmentOperator.Argument));
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
            Lazy<IOperation> instance = new Lazy<IOperation>(() => GetDelegateCreationInstance(boundDelegateCreationExpression));
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
            Lazy<ImmutableArray<ISymbolInitializer>> memberInitializers = new Lazy<ImmutableArray<ISymbolInitializer>>(() => GetObjectCreationMemberInitializers(boundObjectCreationExpression));
            Lazy<ImmutableArray<IArgument>> argumentsInParameterOrder = new Lazy<ImmutableArray<IArgument>>(() => DeriveArguments(boundObjectCreationExpression.Arguments, boundObjectCreationExpression.ArgumentNamesOpt, boundObjectCreationExpression.ArgsToParamsOpt, boundObjectCreationExpression.ArgumentRefKindsOpt, boundObjectCreationExpression.Constructor.Parameters, boundObjectCreationExpression.Syntax));
            bool isInvalid = boundObjectCreationExpression.HasErrors;
            SyntaxNode syntax = boundObjectCreationExpression.Syntax;
            ITypeSymbol type = boundObjectCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundObjectCreationExpression.ConstantValue);
            return new LazyObjectCreationExpression(constructor, memberInitializers, argumentsInParameterOrder, isInvalid, syntax, type, constantValue);
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
            Lazy<IBlockStatement> body = new Lazy<IBlockStatement>(() => (IBlockStatement)Create(boundLambda.Body));
            bool isInvalid = boundLambda.HasErrors;
            SyntaxNode syntax = boundLambda.Syntax;
            ITypeSymbol type = boundLambda.Type;
            Optional<object> constantValue = ConvertToOptional(boundLambda.ConstantValue);
            return new LazyLambdaExpression(signature, body, isInvalid, syntax, type, constantValue);
        }
        private static IConversionExpression CreateBoundConversionOperation(BoundConversion boundConversion)
        {
            Lazy<IOperation> operand = new Lazy<IOperation>(() => (IOperation)Create(boundConversion.Operand));
            ConversionKind conversionKind = GetConversionKind(boundConversion.ConversionKind);
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
            Lazy<IOperation> operand = new Lazy<IOperation>(() => (IOperation)Create(boundAsOperator.Operand));
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
            Lazy<IOperation> operand = new Lazy<IOperation>(() => (IOperation)Create(boundIsOperator.Operand));
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
            ITypeSymbol elementType = GetArrayCreationElementType(boundArrayCreation);
            Lazy<ImmutableArray<IOperation>> dimensionSizes = new Lazy<ImmutableArray<IOperation>>(() => boundArrayCreation.Bounds.SelectAsArray(n => (IOperation)Create(n)));
            Lazy<IArrayInitializer> initializer = new Lazy<IArrayInitializer>(() => (IArrayInitializer)Create(boundArrayCreation.InitializerOpt));
            bool isInvalid = boundArrayCreation.HasErrors;
            SyntaxNode syntax = boundArrayCreation.Syntax;
            ITypeSymbol type = boundArrayCreation.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayCreation.ConstantValue);
            return new LazyArrayCreationExpression(elementType, dimensionSizes, initializer, isInvalid, syntax, type, constantValue);
        }
        private static IArrayInitializer CreateBoundArrayInitializationOperation(BoundArrayInitialization boundArrayInitialization)
        {
            Lazy<ImmutableArray<IOperation>> elementValues = new Lazy<ImmutableArray<IOperation>>(() => boundArrayInitialization.Initializers.SelectAsArray(n => (IOperation)Create(n)));
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
            Lazy<IOperation> target = new Lazy<IOperation>(() => (IOperation)Create(boundAssignmentOperator.Left));
            Lazy<IOperation> value = new Lazy<IOperation>(() => (IOperation)Create(boundAssignmentOperator.Right));
            bool isInvalid = boundAssignmentOperator.HasErrors;
            SyntaxNode syntax = boundAssignmentOperator.Syntax;
            ITypeSymbol type = boundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAssignmentOperator.ConstantValue);
            return new LazyAssignmentExpression(target, value, isInvalid, syntax, type, constantValue);
        }
        private static ICompoundAssignmentExpression CreateBoundCompoundAssignmentOperatorOperation(BoundCompoundAssignmentOperator boundCompoundAssignmentOperator)
        {
            BinaryOperationKind binaryOperationKind = CSharp.Expression.DeriveBinaryOperationKind(boundCompoundAssignmentOperator.Operator.Kind);
            Lazy<IOperation> target = new Lazy<IOperation>(() => (IOperation)Create(boundCompoundAssignmentOperator.Left));
            Lazy<IOperation> value = new Lazy<IOperation>(() => (IOperation)Create(boundCompoundAssignmentOperator.Right));
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
            Lazy<IOperation> target = new Lazy<IOperation>(() => (IOperation)Create(boundIncrementOperator.Operand));
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
            Lazy<IOperation> operand = new Lazy<IOperation>(() => (IOperation)Create(boundUnaryOperator.Operand));
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
            Lazy<IOperation> leftOperand = new Lazy<IOperation>(() => (IOperation)Create(boundBinaryOperator.Left));
            Lazy<IOperation> rightOperand = new Lazy<IOperation>(() => (IOperation)Create(boundBinaryOperator.Right));
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
            Lazy<IOperation> condition = new Lazy<IOperation>(() => (IOperation)Create(boundConditionalOperator.Condition));
            Lazy<IOperation> ifTrueValue = new Lazy<IOperation>(() => (IOperation)Create(boundConditionalOperator.Consequence));
            Lazy<IOperation> ifFalseValue = new Lazy<IOperation>(() => (IOperation)Create(boundConditionalOperator.Alternative));
            bool isInvalid = boundConditionalOperator.HasErrors;
            SyntaxNode syntax = boundConditionalOperator.Syntax;
            ITypeSymbol type = boundConditionalOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalOperator.ConstantValue);
            return new LazyConditionalChoiceExpression(condition, ifTrueValue, ifFalseValue, isInvalid, syntax, type, constantValue);
        }
        private static INullCoalescingExpression CreateBoundNullCoalescingOperatorOperation(BoundNullCoalescingOperator boundNullCoalescingOperator)
        {
            Lazy<IOperation> primaryOperand = new Lazy<IOperation>(() => (IOperation)Create(boundNullCoalescingOperator.LeftOperand));
            Lazy<IOperation> secondaryOperand = new Lazy<IOperation>(() => (IOperation)Create(boundNullCoalescingOperator.RightOperand));
            bool isInvalid = boundNullCoalescingOperator.HasErrors;
            SyntaxNode syntax = boundNullCoalescingOperator.Syntax;
            ITypeSymbol type = boundNullCoalescingOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundNullCoalescingOperator.ConstantValue);
            return new LazyNullCoalescingExpression(primaryOperand, secondaryOperand, isInvalid, syntax, type, constantValue);
        }
        private static IAwaitExpression CreateBoundAwaitExpressionOperation(BoundAwaitExpression boundAwaitExpression)
        {
            Lazy<IOperation> awaitedValue = new Lazy<IOperation>(() => (IOperation)Create(boundAwaitExpression.Expression));
            bool isInvalid = boundAwaitExpression.HasErrors;
            SyntaxNode syntax = boundAwaitExpression.Syntax;
            ITypeSymbol type = boundAwaitExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundAwaitExpression.ConstantValue);
            return new LazyAwaitExpression(awaitedValue, isInvalid, syntax, type, constantValue);
        }
        private static IArrayElementReferenceExpression CreateBoundArrayAccessOperation(BoundArrayAccess boundArrayAccess)
        {
            Lazy<IOperation> arrayReference = new Lazy<IOperation>(() => (IOperation)Create(boundArrayAccess.Expression));
            Lazy<ImmutableArray<IOperation>> indices = new Lazy<ImmutableArray<IOperation>>(() => boundArrayAccess.Indices.SelectAsArray(n => (IOperation)Create(n)));
            bool isInvalid = boundArrayAccess.HasErrors;
            SyntaxNode syntax = boundArrayAccess.Syntax;
            ITypeSymbol type = boundArrayAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayAccess.ConstantValue);
            return new LazyArrayElementReferenceExpression(arrayReference, indices, isInvalid, syntax, type, constantValue);
        }
        private static IPointerIndirectionReferenceExpression CreateBoundPointerIndirectionOperatorOperation(BoundPointerIndirectionOperator boundPointerIndirectionOperator)
        {
            Lazy<IOperation> pointer = new Lazy<IOperation>(() => (IOperation)Create(boundPointerIndirectionOperator.Operand));
            bool isInvalid = boundPointerIndirectionOperator.HasErrors;
            SyntaxNode syntax = boundPointerIndirectionOperator.Syntax;
            ITypeSymbol type = boundPointerIndirectionOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundPointerIndirectionOperator.ConstantValue);
            return new LazyPointerIndirectionReferenceExpression(pointer, isInvalid, syntax, type, constantValue);
        }
        private static IAddressOfExpression CreateBoundAddressOfOperatorOperation(BoundAddressOfOperator boundAddressOfOperator)
        {
            Lazy<IOperation> reference = new Lazy<IOperation>(() => (IOperation)Create(boundAddressOfOperator.Operand));
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
            Lazy<IOperation> conditionalValue = new Lazy<IOperation>(() => (IOperation)Create(boundConditionalAccess.AccessExpression));
            Lazy<IOperation> conditionalInstance = new Lazy<IOperation>(() => (IOperation)Create(boundConditionalAccess.Receiver));
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
            Lazy<IOperation> value = new Lazy<IOperation>(() => (IOperation)Create(boundFieldEqualsValue.Value));
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
            Lazy<IOperation> value = new Lazy<IOperation>(() => (IOperation)Create(boundPropertyEqualsValue.Value));
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
            Lazy<IOperation> value = new Lazy<IOperation>(() => (IOperation)Create(boundParameterEqualsValue.Value));
            OperationKind kind = OperationKind.ParameterInitializerAtDeclaration;
            bool isInvalid = ((IOperation)boundParameterEqualsValue.Value).IsInvalid;
            SyntaxNode syntax = boundParameterEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyParameterInitializer(parameter, value, kind, isInvalid, syntax, type, constantValue);
        }
        private static IBlockStatement CreateBoundBlockOperation(BoundBlock boundBlock)
        {
            Lazy<ImmutableArray<IOperation>> statements = new Lazy<ImmutableArray<IOperation>>(() => GetBlockStatement(boundBlock));
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
            Lazy<IOperation> returnedValue = new Lazy<IOperation>(() => (IOperation)Create(null));
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
            Lazy<IOperation> condition = new Lazy<IOperation>(() => (IOperation)Create(boundIfStatement.Condition));
            Lazy<IOperation> ifTrueStatement = new Lazy<IOperation>(() => (IOperation)Create(boundIfStatement.Consequence));
            Lazy<IOperation> ifFalseStatement = new Lazy<IOperation>(() => (IOperation)Create(boundIfStatement.AlternativeOpt));
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
            Lazy<IOperation> condition = new Lazy<IOperation>(() => (IOperation)Create(boundWhileStatement.Condition));
            LoopKind loopKind = LoopKind.WhileUntil;
            Lazy<IOperation> body = new Lazy<IOperation>(() => (IOperation)Create(boundWhileStatement.Body));
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
            Lazy<IOperation> condition = new Lazy<IOperation>(() => (IOperation)Create(boundDoStatement.Condition));
            LoopKind loopKind = LoopKind.WhileUntil;
            Lazy<IOperation> body = new Lazy<IOperation>(() => (IOperation)Create(boundDoStatement.Body));
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
            Lazy<IOperation> condition = new Lazy<IOperation>(() => (IOperation)Create(boundForStatement.Condition));
            LoopKind loopKind = LoopKind.For;
            Lazy<IOperation> body = new Lazy<IOperation>(() => (IOperation)Create(boundForStatement.Body));
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
            Lazy<IOperation> collection = new Lazy<IOperation>(() => (IOperation)Create(boundForEachStatement.Expression));
            LoopKind loopKind = LoopKind.ForEach;
            Lazy<IOperation> body = new Lazy<IOperation>(() => (IOperation)Create(boundForEachStatement.Body));
            bool isInvalid = boundForEachStatement.HasErrors;
            SyntaxNode syntax = boundForEachStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyForEachLoopStatement(iterationVariable, collection, loopKind, body, isInvalid, syntax, type, constantValue);
        }
        private static ISwitchStatement CreateBoundSwitchStatementOperation(BoundSwitchStatement boundSwitchStatement)
        {
            Lazy<IOperation> value = new Lazy<IOperation>(() => (IOperation)Create(boundSwitchStatement.Expression));
            Lazy<ImmutableArray<ISwitchCase>> cases = new Lazy<ImmutableArray<ISwitchCase>>(() => GetSwitchStatementCases(boundSwitchStatement));
            bool isInvalid = boundSwitchStatement.HasErrors;
            SyntaxNode syntax = boundSwitchStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazySwitchStatement(value, cases, isInvalid, syntax, type, constantValue);
        }
        private static ISingleValueCaseClause CreateBoundSwitchLabelOperation(BoundSwitchLabel boundSwitchLabel)
        {
            Lazy<IOperation> value = new Lazy<IOperation>(() => (IOperation)Create(boundSwitchLabel.ExpressionOpt));
            BinaryOperationKind equality = GetLabelEqualityKind(boundSwitchLabel);
            CaseKind caseKind = boundSwitchLabel.ExpressionOpt != null ? CaseKind.SingleValue : CaseKind.Default;
            bool isInvalid = boundSwitchLabel.HasErrors;
            SyntaxNode syntax = boundSwitchLabel.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazySingleValueCaseClause(value, equality, caseKind, isInvalid, syntax, type, constantValue);
        }
        private static ITryStatement CreateBoundTryStatementOperation(BoundTryStatement boundTryStatement)
        {
            Lazy<IBlockStatement> body = new Lazy<IBlockStatement>(() => (IBlockStatement)Create(boundTryStatement.TryBlock));
            Lazy<ImmutableArray<ICatchClause>> catches = new Lazy<ImmutableArray<ICatchClause>>(() => boundTryStatement.CatchBlocks.SelectAsArray(n => (ICatchClause)Create(n)));
            Lazy<IBlockStatement> finallyHandler = new Lazy<IBlockStatement>(() => (IBlockStatement)Create(boundTryStatement.FinallyBlockOpt));
            bool isInvalid = boundTryStatement.HasErrors;
            SyntaxNode syntax = boundTryStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyTryStatement(body, catches, finallyHandler, isInvalid, syntax, type, constantValue);
        }
        private static ICatchClause CreateBoundCatchBlockOperation(BoundCatchBlock boundCatchBlock)
        {
            Lazy<IBlockStatement> handler = new Lazy<IBlockStatement>(() => (IBlockStatement)Create(boundCatchBlock.Body));
            ITypeSymbol caughtType = boundCatchBlock.ExceptionTypeOpt;
            Lazy<IOperation> filter = new Lazy<IOperation>(() => (IOperation)Create(boundCatchBlock.ExceptionFilterOpt));
            ILocalSymbol exceptionLocal = (boundCatchBlock.Locals.FirstOrDefault()?.DeclarationKind == CSharp.Symbols.LocalDeclarationKind.CatchVariable) ? boundCatchBlock.Locals.FirstOrDefault() : null;
            bool isInvalid = boundCatchBlock.Body.HasErrors || (boundCatchBlock.ExceptionFilterOpt != null && boundCatchBlock.ExceptionFilterOpt.HasErrors);
            SyntaxNode syntax = boundCatchBlock.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyCatchClause(handler, caughtType, filter, exceptionLocal, isInvalid, syntax, type, constantValue);
        }
        private static IFixedStatement CreateBoundFixedStatementOperation(BoundFixedStatement boundFixedStatement)
        {
            Lazy<IVariableDeclarationStatement> variables = new Lazy<IVariableDeclarationStatement>(() => (IVariableDeclarationStatement)Create(boundFixedStatement.Declarations));
            Lazy<IOperation> body = new Lazy<IOperation>(() => (IOperation)Create(boundFixedStatement.Body));
            bool isInvalid = boundFixedStatement.HasErrors;
            SyntaxNode syntax = boundFixedStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyFixedStatement(variables, body, isInvalid, syntax, type, constantValue);
        }
        private static IUsingStatement CreateBoundUsingStatementOperation(BoundUsingStatement boundUsingStatement)
        {
            Lazy<IOperation> body = new Lazy<IOperation>(() => (IOperation)Create(boundUsingStatement.Body));
            Lazy<IVariableDeclarationStatement> declaration = new Lazy<IVariableDeclarationStatement>(() => (IVariableDeclarationStatement)Create(boundUsingStatement.DeclarationsOpt));
            Lazy<IOperation> value = new Lazy<IOperation>(() => (IOperation)Create(boundUsingStatement.ExpressionOpt));
            bool isInvalid = boundUsingStatement.HasErrors;
            SyntaxNode syntax = boundUsingStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyUsingStatement(body, declaration, value, isInvalid, syntax, type, constantValue);
        }
        private static IThrowStatement CreateBoundThrowStatementOperation(BoundThrowStatement boundThrowStatement)
        {
            Lazy<IOperation> thrownObject = new Lazy<IOperation>(() => (IOperation)Create(boundThrowStatement.ExpressionOpt));
            bool isInvalid = boundThrowStatement.HasErrors;
            SyntaxNode syntax = boundThrowStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyThrowStatement(thrownObject, isInvalid, syntax, type, constantValue);
        }
        private static IReturnStatement CreateBoundReturnStatementOperation(BoundReturnStatement boundReturnStatement)
        {
            Lazy<IOperation> returnedValue = new Lazy<IOperation>(() => (IOperation)Create(boundReturnStatement.ExpressionOpt));
            bool isInvalid = boundReturnStatement.HasErrors;
            SyntaxNode syntax = boundReturnStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyReturnStatement(returnedValue, isInvalid, syntax, type, constantValue);
        }
        private static IReturnStatement CreateBoundYieldReturnStatementOperation(BoundYieldReturnStatement boundYieldReturnStatement)
        {
            Lazy<IOperation> returnedValue = new Lazy<IOperation>(() => (IOperation)Create(boundYieldReturnStatement.Expression));
            bool isInvalid = boundYieldReturnStatement.HasErrors;
            SyntaxNode syntax = boundYieldReturnStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyReturnStatement(returnedValue, isInvalid, syntax, type, constantValue);
        }
        private static ILockStatement CreateBoundLockStatementOperation(BoundLockStatement boundLockStatement)
        {
            Lazy<IOperation> lockedObject = new Lazy<IOperation>(() => (IOperation)Create(boundLockStatement.Argument));
            Lazy<IOperation> body = new Lazy<IOperation>(() => (IOperation)Create(boundLockStatement.Body));
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
            Lazy<ImmutableArray<IVariableDeclaration>> variables = new Lazy<ImmutableArray<IVariableDeclaration>>(() => GetVariableDeclarationStatementVariables(boundLocalDeclaration));
            bool isInvalid = boundLocalDeclaration.HasErrors;
            SyntaxNode syntax = boundLocalDeclaration.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyVariableDeclarationStatement(variables, isInvalid, syntax, type, constantValue);
        }
        private static IVariableDeclarationStatement CreateBoundMultipleLocalDeclarationsOperation(BoundMultipleLocalDeclarations boundMultipleLocalDeclarations)
        {
            Lazy<ImmutableArray<IVariableDeclaration>> variables = new Lazy<ImmutableArray<IVariableDeclaration>>(() => GetVariableDeclarationStatementVariables(boundMultipleLocalDeclarations));
            bool isInvalid = boundMultipleLocalDeclarations.HasErrors;
            SyntaxNode syntax = boundMultipleLocalDeclarations.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyVariableDeclarationStatement(variables, isInvalid, syntax, type, constantValue);
        }
        private static ILabelStatement CreateBoundLabelStatementOperation(BoundLabelStatement boundLabelStatement)
        {
            ILabelSymbol label = boundLabelStatement.Label;
            Lazy<IOperation> labeledStatement = new Lazy<IOperation>(() => (IOperation)Create(null));
            bool isInvalid = boundLabelStatement.HasErrors;
            SyntaxNode syntax = boundLabelStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyLabelStatement(label, labeledStatement, isInvalid, syntax, type, constantValue);
        }
        private static ILabelStatement CreateBoundLabeledStatementOperation(BoundLabeledStatement boundLabeledStatement)
        {
            ILabelSymbol label = boundLabeledStatement.Label;
            Lazy<IOperation> labeledStatement = new Lazy<IOperation>(() => (IOperation)Create(boundLabeledStatement.Body));
            bool isInvalid = boundLabeledStatement.HasErrors;
            SyntaxNode syntax = boundLabeledStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            return new LazyLabelStatement(label, labeledStatement, isInvalid, syntax, type, constantValue);
        }
        private static IExpressionStatement CreateBoundExpressionStatementOperation(BoundExpressionStatement boundExpressionStatement)
        {
            Lazy<IOperation> expression = new Lazy<IOperation>(() => (IOperation)Create(boundExpressionStatement.Expression));
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
                return statementList.Statements.Select(n => Create(n)).ToImmutableArray();
            }
            else if (statement == null)
            {
                return ImmutableArray<IOperation>.Empty;
            }

            return ImmutableArray.Create<IOperation>(Create(statement));
        }

        private static readonly ConditionalWeakTable<BoundIncrementOperator, ILiteralExpression> s_incrementValueMappings = new ConditionalWeakTable<BoundIncrementOperator, ILiteralExpression>();

        private static ILiteralExpression CreateIncrementOneLiteralExpression(BoundIncrementOperator boundIncrementOperator)
        {
            return s_incrementValueMappings.GetValue(boundIncrementOperator, (increment) =>
            {
                string text = increment.Syntax.ToString();
                bool isInvalid = false;
                SyntaxNode syntax = increment.Syntax;
                ITypeSymbol type = increment.Type;
                Optional<object> constantValue = ConvertToOptional(Semantics.Expression.SynthesizeNumeric(increment.Type, 1));
                return new LiteralExpression(text, isInvalid, syntax, type, constantValue);
            });
        }

        private static ImmutableArray<IArgument> GetArgumentsInSourceOrder(BoundCall call)
        {
            ArrayBuilder<IArgument> sourceOrderArguments = ArrayBuilder<IArgument>.GetInstance(call.Arguments.Length);
            for (int argumentIndex = 0; argumentIndex < call.Arguments.Length; argumentIndex++)
            {
                IArgument argument = DeriveArgument(
                    call.ArgsToParamsOpt.IsDefault ? argumentIndex : call.ArgsToParamsOpt[argumentIndex],
                    argumentIndex,
                    call.Arguments,
                    call.ArgumentNamesOpt,
                    call.ArgumentRefKindsOpt,
                    call.Method.Parameters,
                    call.Syntax);

                sourceOrderArguments.Add(argument);
                if (argument.ArgumentKind == ArgumentKind.ParamArray)
                {
                    break;
                }
            }

            return sourceOrderArguments.ToImmutableAndFree();
        }

        private static readonly ConditionalWeakTable<BoundExpression, IArgument> s_argumentMappings = new ConditionalWeakTable<BoundExpression, IArgument>();

        private static IArgument DeriveArgument(
            int parameterIndex,
            int argumentIndex,
            ImmutableArray<BoundExpression> boundArguments,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            ImmutableArray<CSharp.Symbols.ParameterSymbol> parameters,
            SyntaxNode invocationSyntax)
        {
            if ((uint)argumentIndex >= (uint)boundArguments.Length)
            {
                // Check for an omitted argument that becomes an empty params array.
                if (parameters.Length > 0)
                {
                    IParameterSymbol lastParameter = parameters[parameters.Length - 1];
                    if (lastParameter.IsParams)
                    {
                        var value = CreateParamArray(lastParameter, boundArguments, argumentIndex, invocationSyntax);
                        return new Argument(
                            argumentKind: ArgumentKind.ParamArray,
                            parameter: lastParameter,
                            value: value,
                            inConversion: null,
                            outConversion: null,
                            isInvalid: lastParameter == null || value.IsInvalid,
                            syntax: value.Syntax,
                            type: null,
                            constantValue: default(Optional<object>));
                    }
                }

                // There is no supplied argument and there is no params parameter. Any action is suspect at this point.
                var invalid = OperationFactory.CreateInvalidExpression(invocationSyntax);
                return new Argument(
                            argumentKind: ArgumentKind.Positional,
                            parameter: null,
                            value: invalid,
                            inConversion: null,
                            outConversion: null,
                            isInvalid: true,
                            syntax: null,
                            type: null,
                            constantValue: default(Optional<object>));
            }

            return s_argumentMappings.GetValue(
                boundArguments[argumentIndex],
                (argument) =>
                {
                    string nameOpt = !argumentNamesOpt.IsDefaultOrEmpty ? argumentNamesOpt[argumentIndex] : null;
                    IParameterSymbol parameterOpt = (uint)parameterIndex < (uint)parameters.Length ? parameters[parameterIndex] : null;

                    if ((object)nameOpt == null)
                    {
                        RefKind refMode = argumentRefKindsOpt.IsDefaultOrEmpty ? RefKind.None : argumentRefKindsOpt[argumentIndex];

                        if (refMode != RefKind.None)
                        {
                            var value = Create(argument);
                            return new Argument(
                                argumentKind: ArgumentKind.Positional,
                                parameter: parameterOpt,
                                value: value,
                                inConversion: null,
                                outConversion: null,
                                isInvalid: parameterOpt == null || value.IsInvalid,
                                syntax: argument.Syntax,
                                type: null,
                                constantValue: default(Optional<object>));
                        }

                        if (argumentIndex >= parameters.Length - 1 &&
                            parameters.Length > 0 &&
                            parameters[parameters.Length - 1].IsParams &&
                            // An argument that is an array of the appropriate type is not a params argument.
                            (boundArguments.Length > argumentIndex + 1 ||
                             ((object)argument.Type != null && // If argument type is null, we are in an error scenario and cannot tell if it is a param array, or not. 
                              (argument.Type.TypeKind != TypeKind.Array ||
                              !argument.Type.Equals((CSharp.Symbols.TypeSymbol)parameters[parameters.Length - 1].Type, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds)))))
                        {
                            var parameter = parameters[parameters.Length - 1];
                            var value = CreateParamArray(parameter, boundArguments, argumentIndex, invocationSyntax);

                            return new Argument(
                                argumentKind: ArgumentKind.ParamArray,
                                parameter: parameter,
                                value: value,
                                inConversion: null,
                                outConversion: null,
                                isInvalid: parameter == null || value.IsInvalid,
                                syntax: value.Syntax,
                                type: null,
                                constantValue: default(Optional<object>));
                        }
                        else
                        {
                            var value = Create(argument);
                            return new Argument(
                                argumentKind: ArgumentKind.Positional,
                                parameter: parameterOpt,
                                value: value,
                                inConversion: null,
                                outConversion: null,
                                isInvalid: parameterOpt == null || value.IsInvalid,
                                syntax: value.Syntax,
                                type: null,
                                constantValue: default(Optional<object>));
                        }
                    }

                    var operation = Create(argument);
                    return new Argument(
                        argumentKind: ArgumentKind.Named,
                        parameter: parameterOpt,
                        value: operation,
                        inConversion: null,
                        outConversion: null,
                        isInvalid: parameterOpt == null || operation.IsInvalid,
                        syntax: operation.Syntax,
                        type: null,
                        constantValue: default(Optional<object>));
                });
        }

        private static IOperation CreateParamArray(IParameterSymbol parameter, ImmutableArray<BoundExpression> boundArguments, int firstArgumentElementIndex, SyntaxNode invocationSyntax)
        {
            if (parameter.Type.TypeKind == TypeKind.Array)
            {
                IArrayTypeSymbol arrayType = (IArrayTypeSymbol)parameter.Type;
                ArrayBuilder<IOperation> builder = ArrayBuilder<IOperation>.GetInstance(boundArguments.Length - firstArgumentElementIndex);

                for (int index = firstArgumentElementIndex; index < boundArguments.Length; index++)
                {
                    builder.Add(Create(boundArguments[index]));
                }

                var paramArrayArguments = builder.ToImmutableAndFree();

                // Use the invocation syntax node if there is no actual syntax available for the argument (because the paramarray is empty.)
                return OperationFactory.CreateArrayCreationExpression(arrayType, paramArrayArguments, paramArrayArguments.Length > 0 ? paramArrayArguments[0].Syntax : invocationSyntax);
            }

            return OperationFactory.CreateInvalidExpression(invocationSyntax);
        }

        private static IOperation GetDelegateCreationInstance(BoundDelegateCreationExpression expression)
        {
            BoundMethodGroup methodGroup = expression.Argument as BoundMethodGroup;
            if (methodGroup != null)
            {
                return Create(methodGroup.InstanceOpt);
            }

            return null;
        }

        private static readonly ConditionalWeakTable<BoundObjectCreationExpression, object> s_memberInitializersMappings =
            new ConditionalWeakTable<BoundObjectCreationExpression, object>();

        private static ImmutableArray<ISymbolInitializer> GetObjectCreationMemberInitializers(BoundObjectCreationExpression expression)
        {
            return (ImmutableArray<ISymbolInitializer>)s_memberInitializersMappings.GetValue(expression,
                objectCreationExpression =>
                {
                    var objectInitializerExpression = expression.InitializerExpressionOpt as BoundObjectInitializerExpression;
                    if (objectInitializerExpression != null)
                    {
                        var builder = ArrayBuilder<ISymbolInitializer>.GetInstance(objectInitializerExpression.Initializers.Length);
                        foreach (var memberAssignment in objectInitializerExpression.Initializers)
                        {
                            var assignment = memberAssignment as BoundAssignmentOperator;
                            var leftSymbol = (assignment?.Left as BoundObjectInitializerMember)?.MemberSymbol;

                            if ((object)leftSymbol == null)
                            {
                                continue;
                            }

                            switch (leftSymbol.Kind)
                            {
                                case SymbolKind.Field:
                                    {
                                        var value = Create(assignment.Right);
                                        builder.Add(new FieldInitializer(
                                            ImmutableArray.Create((IFieldSymbol)leftSymbol),
                                            value,
                                            OperationKind.FieldInitializerInCreation,
                                            value.IsInvalid || leftSymbol == null,
                                            assignment.Syntax,
                                            type: null,
                                            constantValue: default(Optional<object>)));
                                        break;
                                    }
                                case SymbolKind.Property:
                                    {
                                        var value = Create(assignment.Right);
                                        builder.Add(new PropertyInitializer(
                                            (IPropertySymbol)leftSymbol,
                                            value,
                                            OperationKind.PropertyInitializerInCreation,
                                            value.IsInvalid || leftSymbol == null,
                                            assignment.Syntax,
                                            type: null,
                                            constantValue: default(Optional<object>)));
                                        break;
                                    }
                            }
                        }

                        return builder.ToImmutableAndFree();
                    }

                    return ImmutableArray<ISymbolInitializer>.Empty;
                });
        }

        private static ConversionKind GetConversionKind(CSharp.ConversionKind kind)
        {
            switch (kind)
            {
                case CSharp.ConversionKind.ExplicitUserDefined:
                case CSharp.ConversionKind.ImplicitUserDefined:
                    return Semantics.ConversionKind.OperatorMethod;

                case CSharp.ConversionKind.ExplicitReference:
                case CSharp.ConversionKind.ImplicitReference:
                case CSharp.ConversionKind.Boxing:
                case CSharp.ConversionKind.Unboxing:
                case CSharp.ConversionKind.Identity:
                    return Semantics.ConversionKind.Cast;

                case CSharp.ConversionKind.AnonymousFunction:
                case CSharp.ConversionKind.ExplicitDynamic:
                case CSharp.ConversionKind.ImplicitDynamic:
                case CSharp.ConversionKind.ExplicitEnumeration:
                case CSharp.ConversionKind.ImplicitEnumeration:
                case CSharp.ConversionKind.ImplicitThrow:
                case CSharp.ConversionKind.ImplicitTupleLiteral:
                case CSharp.ConversionKind.ImplicitTuple:
                case CSharp.ConversionKind.ExplicitTupleLiteral:
                case CSharp.ConversionKind.ExplicitTuple:
                case CSharp.ConversionKind.ExplicitNullable:
                case CSharp.ConversionKind.ImplicitNullable:
                case CSharp.ConversionKind.ExplicitNumeric:
                case CSharp.ConversionKind.ImplicitNumeric:
                case CSharp.ConversionKind.ImplicitConstant:
                case CSharp.ConversionKind.IntegerToPointer:
                case CSharp.ConversionKind.IntPtr:
                case CSharp.ConversionKind.NullLiteral:
                case CSharp.ConversionKind.NullToPointer:
                case CSharp.ConversionKind.PointerToInteger:
                case CSharp.ConversionKind.PointerToPointer:
                case CSharp.ConversionKind.PointerToVoid:
                    return Semantics.ConversionKind.CSharp;

                default:
                    return Semantics.ConversionKind.Invalid;
            }
        }

        private static ITypeSymbol GetArrayCreationElementType(BoundArrayCreation creation)
        {
            IArrayTypeSymbol arrayType = creation.Type as IArrayTypeSymbol;
            if ((object)arrayType != null)
            {
                return arrayType.ElementType;
            }

            return null;
        }

        private static readonly ConditionalWeakTable<BoundBlock, object> s_blockStatementsMappings =
            new ConditionalWeakTable<BoundBlock, object>();

        private static ImmutableArray<IOperation> GetBlockStatement(BoundBlock block)
        {
            // This is to filter out operations of kind None.
            return (ImmutableArray<IOperation>)s_blockStatementsMappings.GetValue(block,
                blockStatement =>
                {
                    return blockStatement.Statements.Select(s => Create(s)).Where(s => s.Kind != OperationKind.None).ToImmutableArray();
                });
        }

        private static readonly ConditionalWeakTable<BoundSwitchStatement, object> s_switchSectionsMappings =
            new ConditionalWeakTable<BoundSwitchStatement, object>();

        private static ImmutableArray<ISwitchCase> GetSwitchStatementCases(BoundSwitchStatement statement)
        {
            return (ImmutableArray<ISwitchCase>)s_switchSectionsMappings.GetValue(statement,
                switchStatement =>
                {
                    return switchStatement.SwitchSections.SelectAsArray(switchSection =>
                    {
                        var clauses = switchSection.SwitchLabels.Select(s => (ICaseClause)Create(s)).ToImmutableArray();
                        var body = switchSection.Statements.Select(s => Create(s)).ToImmutableArray();

                        return new SwitchCase(clauses, body, switchSection.HasErrors, switchSection.Syntax, type: null, constantValue: default(Optional<object>));
                    });
                });
        }

        private static BinaryOperationKind GetLabelEqualityKind(BoundSwitchLabel label)
        {
            BoundExpression caseValue = label.ExpressionOpt;
            if (caseValue != null)
            {
                switch (caseValue.Type.SpecialType)
                {
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int16:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Char:
                        return BinaryOperationKind.IntegerEquals;

                    case SpecialType.System_Boolean:
                        return BinaryOperationKind.BooleanEquals;

                    case SpecialType.System_String:
                        return BinaryOperationKind.StringEquals;
                }

                if (caseValue.Type.TypeKind == TypeKind.Enum)
                {
                    return BinaryOperationKind.EnumEquals;
                }

                return BinaryOperationKind.Invalid;
            }

            // Return None for `default` case.
            return BinaryOperationKind.None;
        }

        private static readonly ConditionalWeakTable<BoundLocalDeclaration, object> s_variablesMappings =
            new ConditionalWeakTable<BoundLocalDeclaration, object>();

        private static ImmutableArray<IVariableDeclaration> GetVariableDeclarationStatementVariables(BoundLocalDeclaration decl)
        {
            return (ImmutableArray<IVariableDeclaration>)s_variablesMappings.GetValue(decl,
                declaration => ImmutableArray.Create<IVariableDeclaration>(
                    OperationFactory.CreateVariableDeclaration(declaration.LocalSymbol, Create(declaration.InitializerOpt), declaration.Syntax)));
        }

        private static readonly ConditionalWeakTable<BoundMultipleLocalDeclarations, object> s_multiVariablesMappings =
            new ConditionalWeakTable<BoundMultipleLocalDeclarations, object>();

        private static ImmutableArray<IVariableDeclaration> GetVariableDeclarationStatementVariables(BoundMultipleLocalDeclarations decl)
        {
            return (ImmutableArray<IVariableDeclaration>)s_multiVariablesMappings.GetValue(decl,
                multipleDeclarations =>
                    multipleDeclarations.LocalDeclarations.SelectAsArray(declaration =>
                        OperationFactory.CreateVariableDeclaration(declaration.LocalSymbol, Create(declaration.InitializerOpt), declaration.Syntax)));
        }

        internal static ImmutableArray<IArgument> DeriveArguments(
            ImmutableArray<BoundExpression> boundArguments,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<int> argumentsToParametersOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            ImmutableArray<CSharp.Symbols.ParameterSymbol> parameters,
            SyntaxNode invocationSyntax)
        {
            ArrayBuilder<IArgument> arguments = ArrayBuilder<IArgument>.GetInstance(boundArguments.Length);
            for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                int argumentIndex = -1;
                if (argumentsToParametersOpt.IsDefault)
                {
                    argumentIndex = parameterIndex;
                }
                else
                {
                    argumentIndex = argumentsToParametersOpt.IndexOf(parameterIndex);
                }

                if ((uint)argumentIndex >= (uint)boundArguments.Length)
                {
                    // No argument has been supplied for the parameter at `parameterIndex`:
                    // 1. `argumentIndex == -1' when the arguments are specified out of parameter order, and no argument is provided for parameter corresponding to `parameters[parameterIndex]`.
                    // 2. `argumentIndex >= boundArguments.Length` when the arguments are specified in parameter order, and no argument is provided at `parameterIndex`.

                    var parameter = parameters[parameterIndex];
                    if (parameter.HasExplicitDefaultValue)
                    {
                        // The parameter is optional with a default value.
                        arguments.Add(new Argument(
                            ArgumentKind.DefaultValue,
                            parameter,
                            OperationFactory.CreateLiteralExpression(parameter.ExplicitDefaultConstantValue, parameter.Type, invocationSyntax),
                            inConversion: null,
                            outConversion: null,
                            isInvalid: parameter.ExplicitDefaultConstantValue.IsBad,
                            syntax: invocationSyntax,
                            type: null,
                            constantValue: default(Optional<object>)));
                    }
                    else
                    {
                        // If the invocation is semantically valid, the parameter will be a params array and an empty array will be provided.
                        // If the argument is otherwise omitted for a parameter with no default value, the invocation is not valid and a null argument will be provided.
                        arguments.Add(DeriveArgument(parameterIndex, boundArguments.Length, boundArguments, argumentNamesOpt, argumentRefKindsOpt, parameters, invocationSyntax));
                    }
                }
                else
                {
                    arguments.Add(DeriveArgument(parameterIndex, argumentIndex, boundArguments, argumentNamesOpt, argumentRefKindsOpt, parameters, invocationSyntax));
                }
            }

            return arguments.ToImmutableAndFree();
        }
    }
}
