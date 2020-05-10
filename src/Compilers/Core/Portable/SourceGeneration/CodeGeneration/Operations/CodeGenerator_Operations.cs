// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal partial class CodeGenerator
    {
        public static IBlockOperation Block(
            ImmutableArray<IOperation> operations = default)
        {
            return new BlockOperation(operations.NullToEmpty(), locals: default, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
        }

        public static IVariableDeclarationGroupOperation VariableDeclarationGroup(
            ImmutableArray<IVariableDeclarationOperation> declarations,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new VariableDeclarationGroupOperation(declarations, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ISwitchOperation Switch(
            IOperation value,
            ImmutableArray<ISwitchCaseOperation> cases,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new SwitchOperation(locals: default, value, cases, exitLabel: null, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IForEachLoopOperation ForEachLoop(
            IOperation loopControlVariable,
            IOperation collection,
            ImmutableArray<IOperation> nextVariables,
            bool isAsynchronous,
            IOperation body,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ForEachLoopOperation(loopControlVariable, collection, nextVariables, isAsynchronous, LoopKind.ForEach, body, locals: default, continueLabel: null, exitLabel: null, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IForLoopOperation ForLoop(
            ImmutableArray<IOperation> before,
            ImmutableArray<ILocalSymbol> conditionLocals,
            IOperation condition,
            ImmutableArray<IOperation> atLoopBottom,
            LoopKind loopKind,
            IOperation body,
            ImmutableArray<ILocalSymbol> locals,
            ILabelSymbol continueLabel,
            ILabelSymbol exitLabel,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ForLoopOperation(before, conditionLocals, condition, atLoopBottom, loopKind, body, locals, continueLabel, exitLabel, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IForToLoopOperation ForToLoop(
            IOperation loopControlVariable,
            IOperation initialValue,
            IOperation limitValue,
            IOperation stepValue,
            bool isChecked,
            ImmutableArray<IOperation> nextVariables,
            (ILocalSymbol LoopObject, ForToLoopOperationUserDefinedInfo UserDefinedInfo) info,
            LoopKind loopKind,
            IOperation body,
            ImmutableArray<ILocalSymbol> locals,
            ILabelSymbol continueLabel,
            ILabelSymbol exitLabel,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ForToLoopOperation(loopControlVariable, initialValue, limitValue, stepValue, isChecked, nextVariables, info, loopKind, body, locals, continueLabel, exitLabel, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IWhileLoopOperation WhileLoop(
            IOperation condition,
            bool conditionIsTop,
            bool conditionIsUntil,
            IOperation ignoredCondition,
            LoopKind loopKind,
            IOperation body,
            ImmutableArray<ILocalSymbol> locals,
            ILabelSymbol continueLabel,
            ILabelSymbol exitLabel,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new WhileLoopOperation(condition, conditionIsTop, conditionIsUntil, ignoredCondition, loopKind, body, locals, continueLabel, exitLabel, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ILabeledOperation Labeled(
            string label,
            IOperation operation,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return Labeled(Label(label), operation, type, constantValue, isImplicit);
        }

        public static ILabeledOperation Labeled(
            ILabelSymbol label,
            IOperation operation,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new LabeledOperation(label, operation, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IBranchOperation Break(
            ILabelSymbol target,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new BranchOperation(target, BranchKind.Break, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IBranchOperation Continue(
            ILabelSymbol target,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new BranchOperation(target, BranchKind.Continue, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IBranchOperation GoTo(
            ILabelSymbol target,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new BranchOperation(target, BranchKind.GoTo, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IEmptyOperation Empty()
        {
            return new EmptyOperation(semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
        }

        public static IReturnOperation Return(IOperation returnedValue = null)
        {
            return new ReturnOperation(returnedValue, OperationKind.Return, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
        }

        public static IReturnOperation YieldBreak()
        {
            return new ReturnOperation(returnedValue: null, OperationKind.YieldBreak, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
        }

        public static IReturnOperation YieldReturn(IOperation returnedValue)
        {
            return new ReturnOperation(returnedValue: null, OperationKind.YieldReturn, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
        }

        public static ILockOperation Lock(
            IOperation lockedValue,
            IOperation body,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new LockOperation(lockedValue, body, lockTakenSymbol: null, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ITryOperation Try(
            IBlockOperation body,
            ImmutableArray<ICatchClauseOperation> catches,
            IBlockOperation @finally,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new TryOperation(body, catches, @finally, exitLabel: null, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IUsingOperation Using(
            IOperation resources,
            IOperation body,
            bool isAsynchronous = false,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new UsingOperation(resources, body, locals: default, isAsynchronous, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IExpressionStatementOperation ExpressionStatement(
            IOperation operation,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ExpressionStatementOperation(operation, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ILocalFunctionOperation LocalFunction(
            IMethodSymbol symbol,
            IBlockOperation body,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new LocalFunctionOperation(symbol, body, ignoredBody: null, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IStopOperation Stop(
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new StopOperation(semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IEndOperation End(
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new EndOperation(semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IRaiseEventOperation RaiseEvent(
            IEventReferenceOperation eventReference,
            ImmutableArray<IArgumentOperation> arguments = default,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new RaiseEventOperation(eventReference, arguments, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ILiteralOperation Literal(
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new LiteralOperation(semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ILiteralOperation Literal(object value)
        {
            return new LiteralOperation(semanticModel: null, syntax: null, type: null, new Optional<object>(value), isImplicit: false);
        }

        public static IConversionOperation Conversion(
            IOperation operand,
            IConvertibleConversion conversion,
            bool isTryCast = false,
            bool isChecked = false,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ConversionOperation(operand, conversion, isTryCast, isChecked, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IInvocationOperation Invocation(
            IMethodSymbol targetMethod,
            IOperation instance = null,
            ImmutableArray<IArgumentOperation> arguments = default,
            bool isVirtual = false,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new InvocationOperation(targetMethod, instance, isVirtual, arguments, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IArrayElementReferenceOperation ArrayElementReference(
            IOperation arrayReference,
            ImmutableArray<IOperation> indices = default,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ArrayElementReferenceOperation(arrayReference, indices, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }


        public static ILocalReferenceOperation LocalReference(string local)
            => LocalReference(Local(null, local));

        public static ILocalReferenceOperation LocalReference(ILocalSymbol local)
        {
            return new LocalReferenceOperation(local, isDeclaration: false, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
        }

        public static IParameterReferenceOperation ParameterReference(string parameter)
            => ParameterReference(Parameter(null, "x"));

        public static IParameterReferenceOperation ParameterReference(IParameterSymbol parameter)
        {
            return new ParameterReferenceOperation(parameter, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
        }

        public static IFieldReferenceOperation FieldReference(
            IFieldSymbol field,
            bool isDeclaration,
            IOperation instance,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new FieldReferenceOperation(field, isDeclaration, instance, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IMethodReferenceOperation MethodReference(
            IMethodSymbol method,
            IOperation instance,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new MethodReferenceOperation(method, isVirtual: false, instance, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IPropertyReferenceOperation PropertyReference(
            IPropertySymbol property,
            ImmutableArray<IArgumentOperation> arguments,
            IOperation instance,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new PropertyReferenceOperation(property, arguments, instance, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IEventReferenceOperation EventReference(
            IEventSymbol @event,
            IOperation instance,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new EventReferenceOperation(@event, instance, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IUnaryOperation Unary(
            UnaryOperatorKind operatorKind,
            IOperation operand,
            bool isLifted = false,
            bool isChecked = false,
            IMethodSymbol operatorMethod = null,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new UnaryOperation(operatorKind, operand, isLifted, isChecked, operatorMethod, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IBinaryOperation Binary(
            BinaryOperatorKind operatorKind,
            IOperation leftOperand,
            IOperation rightOperand,
            bool isLifted = false,
            bool isChecked = false,
            bool isCompareText = false,
            IMethodSymbol operatorMethod = null,
            IMethodSymbol unaryOperatorMethod = null,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new BinaryOperation(operatorKind, leftOperand, rightOperand, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IConditionalOperation Conditional(
            IOperation condition,
            IOperation whenTrue,
            IOperation whenFalse,
            bool isRef = false,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ConditionalOperation(condition, whenTrue, whenFalse, isRef, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ICoalesceOperation Coalesce(
            IOperation value,
            IOperation whenNull,
            IConvertibleConversion valueConversion = null,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new CoalesceOperation(value, whenNull, valueConversion, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IAnonymousFunctionOperation AnonymousFunction(
            IMethodSymbol symbol,
            IBlockOperation body,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new AnonymousFunctionOperation(symbol, body, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IObjectCreationOperation ObjectCreation(
            IMethodSymbol constructor,
            ImmutableArray<IArgumentOperation> arguments,
            IObjectOrCollectionInitializerOperation initializer,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ObjectCreationOperation(constructor, initializer, arguments, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ITypeParameterObjectCreationOperation TypeParameterObjectCreation(
            IObjectOrCollectionInitializerOperation initializer,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new TypeParameterObjectCreationOperation(initializer, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IArrayCreationOperation ArrayCreation(
            ImmutableArray<IOperation> dimensionSizes,
            IArrayInitializerOperation initializer,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ArrayCreationOperation(dimensionSizes, initializer, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IInstanceReferenceOperation InstanceReference(
            InstanceReferenceKind referenceKind,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new InstanceReferenceOperation(referenceKind, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IIsTypeOperation IsType(
            IOperation valueOperand,
            ITypeSymbol typeOperand,
            bool isNegated = false,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new IsTypeOperation(valueOperand, typeOperand, isNegated, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IAwaitOperation Await(
            IOperation operation,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new AwaitOperation(operation, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ISimpleAssignmentOperation SimpleAssignment(
            IOperation target,
            IOperation value,
            bool isRef = false,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new SimpleAssignmentOperation(isRef, target, value, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ICompoundAssignmentOperation CompoundAssignment(
            IConvertibleConversion inConversion,
            IConvertibleConversion outConversion,
            BinaryOperatorKind operatorKind,
            bool isLifted,
            bool isChecked,
            IMethodSymbol operatorMethod,
            IOperation target,
            IOperation value,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new CompoundAssignmentOperation(inConversion, outConversion, operatorKind, isLifted, isChecked, operatorMethod, target, value, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IParenthesizedOperation Parenthesized(
            IOperation operand,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ParenthesizedOperation(operand, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IEventAssignmentOperation EventAssignment(
            IOperation eventReference,
            IOperation handlerValue,
            bool adds,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new EventAssignmentOperation(eventReference, handlerValue, adds, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IConditionalAccessOperation ConditionalAccess(
            IOperation operation,
            IOperation whenNotNull,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ConditionalAccessOperation(operation, whenNotNull, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IConditionalAccessInstanceOperation ConditionalAccessInstance(
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ConditionalAccessInstanceOperation(semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IInterpolatedStringOperation InterpolatedString(
            ImmutableArray<IInterpolatedStringContentOperation> parts,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new InterpolatedStringOperation(parts, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IAnonymousObjectCreationOperation AnonymousObjectCreation(
            ImmutableArray<IOperation> initializers,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new AnonymousObjectCreationOperation(initializers, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IObjectOrCollectionInitializerOperation ObjectOrCollectionInitializer(
            ImmutableArray<IOperation> initializers,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ObjectOrCollectionInitializerOperation(initializers, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IMemberInitializerOperation MemberInitializer(
            IOperation initializedMember,
            IObjectOrCollectionInitializerOperation initializer,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new MemberInitializerOperation(initializedMember, initializer, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static INameOfOperation NameOf(
            IOperation argument,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new NameOfOperation(argument, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ITupleOperation Tuple(
            ImmutableArray<IOperation> elements,
            ITypeSymbol naturalType,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new TupleOperation(elements, naturalType, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IDynamicMemberReferenceOperation DynamicMemberReference(
            IOperation instance,
            string memberName,
            ImmutableArray<ITypeSymbol> typeArguments,
            ITypeSymbol containingType,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new DynamicMemberReferenceOperation(instance, memberName, typeArguments, containingType, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ITranslatedQueryOperation TranslatedQuery(
            IOperation operation,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new TranslatedQueryOperation(operation, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IDelegateCreationOperation DelegateCreation(
            IOperation target,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new DelegateCreationOperation(target, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IDefaultValueOperation DefaultValue(
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new DefaultValueOperation(semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ITypeOfOperation TypeOf(
            ITypeSymbol typeOperand,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new TypeOfOperation(typeOperand, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ISizeOfOperation SizeOf(
            ITypeSymbol typeOperand,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new SizeOfOperation(typeOperand, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IAddressOfOperation AddressOf(
            IOperation reference,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new AddressOfOperation(reference, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IIsPatternOperation IsPattern(
            IOperation value,
            IPatternOperation pattern,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new IsPatternOperation(value, pattern, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IIncrementOrDecrementOperation Increment(
            bool isPostfix,
            bool isLifted,
            bool isChecked,
            IOperation target,
            IMethodSymbol operatorMethod,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new IncrementOrDecrementOperation(isPostfix, isLifted, isChecked, target, operatorMethod, OperationKind.Increment, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IIncrementOrDecrementOperation Decrement(
            bool isPostfix,
            bool isLifted,
            bool isChecked,
            IOperation target,
            IMethodSymbol operatorMethod,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new IncrementOrDecrementOperation(isPostfix, isLifted, isChecked, target, operatorMethod, OperationKind.Decrement, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IThrowOperation Throw(
            IOperation exception,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ThrowOperation(exception, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IDeconstructionAssignmentOperation DeconstructionAssignment(
            IOperation target,
            IOperation value,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new DeconstructionAssignmentOperation(target, value, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IDeclarationExpressionOperation DeclarationExpression(
            IOperation expression,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new DeclarationExpressionOperation(expression, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IOmittedArgumentOperation OmittedArgument(
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new OmittedArgumentOperation(semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IFieldInitializerOperation FieldInitializer(
            ImmutableArray<IFieldSymbol> initializedFields,
            ImmutableArray<ILocalSymbol> locals,
            IOperation value,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new FieldInitializerOperation(initializedFields, locals, value, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IVariableInitializerOperation VariableInitializer(
            ImmutableArray<ILocalSymbol> locals,
            IOperation value,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new VariableInitializerOperation(locals, value, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IPropertyInitializerOperation PropertyInitializer(
            ImmutableArray<IPropertySymbol> initializedProperties,
            ImmutableArray<ILocalSymbol> locals,
            IOperation value,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new PropertyInitializerOperation(initializedProperties, locals, value, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IParameterInitializerOperation ParameterInitializer(
            IParameterSymbol parameter,
            ImmutableArray<ILocalSymbol> locals,
            IOperation value,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ParameterInitializerOperation(parameter, locals, value, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IArrayInitializerOperation ArrayInitializer(
            ImmutableArray<IOperation> elementValues,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ArrayInitializerOperation(elementValues, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IVariableDeclaratorOperation VariableDeclarator(
            ILocalSymbol symbol,
            IVariableInitializerOperation initializer,
            ImmutableArray<IOperation> ignoredArguments,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new VariableDeclaratorOperation(symbol, initializer, ignoredArguments, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IVariableDeclarationOperation VariableDeclaration(
            ImmutableArray<IVariableDeclaratorOperation> declarators,
            IVariableInitializerOperation initializer,
            ImmutableArray<IOperation> ignoredDimensions,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new VariableDeclarationOperation(declarators, initializer, ignoredDimensions, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IArgumentOperation Argument(
            ArgumentKind argumentKind,
            IParameterSymbol parameter,
            IOperation value,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ArgumentOperation(argumentKind, parameter, value, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ICatchClauseOperation CatchClause(
            IOperation exceptionDeclarationOrExpression,
            ITypeSymbol exceptionType,
            ImmutableArray<ILocalSymbol> locals,
            IOperation filter,
            IBlockOperation handler,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new CatchClauseOperation(exceptionDeclarationOrExpression, exceptionType, locals, filter, handler, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ISwitchCaseOperation SwitchCase(
            ImmutableArray<ICaseClauseOperation> clauses,
            ImmutableArray<IOperation> body,
            ImmutableArray<ILocalSymbol> locals,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new SwitchCaseOperation(clauses, body, locals, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IDefaultCaseClauseOperation DefaultCaseClause(
            CaseKind caseKind,
            ILabelSymbol label,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new DefaultCaseClauseOperation(caseKind, label, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IRangeCaseClauseOperation RangeCaseClause(
            IOperation minimumValue,
            IOperation maximumValue,
            CaseKind caseKind,
            ILabelSymbol label,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new RangeCaseClauseOperation(minimumValue, maximumValue, caseKind, label, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IRelationalCaseClauseOperation RelationalCaseClause(
            IOperation value,
            BinaryOperatorKind relation,
            CaseKind caseKind,
            ILabelSymbol label,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new RelationalCaseClauseOperation(value, relation, caseKind, label, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ISingleValueCaseClauseOperation SingleValueCaseClause(
            IOperation value,
            CaseKind caseKind,
            ILabelSymbol label,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new SingleValueCaseClauseOperation(value, caseKind, label, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IInterpolatedStringTextOperation InterpolatedStringText(
            IOperation text,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new InterpolatedStringTextOperation(text, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IInterpolationOperation Interpolation(
            IOperation expression,
            IOperation alignment,
            IOperation formatString,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new InterpolationOperation(expression, alignment, formatString, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IConstantPatternOperation ConstantPattern(
            IOperation value,
            ITypeSymbol inputType,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ConstantPatternOperation(value, inputType, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IDeclarationPatternOperation DeclarationPattern(
            ITypeSymbol matchedType,
            bool matchesNull,
            ISymbol declaredSymbol,
            ITypeSymbol inputType,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new DeclarationPatternOperation(matchedType, matchesNull, declaredSymbol, inputType, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ITupleBinaryOperation TupleBinary(
            BinaryOperatorKind operatorKind,
            IOperation leftOperand,
            IOperation rightOperand,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new TupleBinaryOperation(operatorKind, leftOperand, rightOperand, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IMethodBodyOperation MethodBody(
            IBlockOperation blockBody,
            IBlockOperation expressionBody,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new MethodBodyOperation(blockBody, expressionBody, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IConstructorBodyOperation ConstructorBody(
            ImmutableArray<ILocalSymbol> locals,
            IOperation initializer,
            IBlockOperation blockBody,
            IBlockOperation expressionBody,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ConstructorBodyOperation(locals, initializer, blockBody, expressionBody, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IDiscardOperation Discard(
            IDiscardSymbol discardSymbol,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new DiscardOperation(discardSymbol, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ICoalesceAssignmentOperation CoalesceAssignment(
            IOperation target,
            IOperation value,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new CoalesceAssignmentOperation(target, value, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IRangeOperation Range(
            IOperation leftOperand,
            IOperation rightOperand,
            bool isLifted,
            IMethodSymbol method,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new RangeOperation(leftOperand, rightOperand, isLifted, method, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IReDimOperation ReDim(
            ImmutableArray<IReDimClauseOperation> clauses,
            bool preserve,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ReDimOperation(clauses, preserve, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IReDimClauseOperation ReDimClause(
            IOperation operand,
            ImmutableArray<IOperation> dimensionSizes,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new ReDimClauseOperation(operand, dimensionSizes, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IRecursivePatternOperation RecursivePattern(
            ITypeSymbol matchedType,
            ISymbol deconstructSymbol,
            ImmutableArray<IPatternOperation> deconstructionSubpatterns,
            ImmutableArray<IPropertySubpatternOperation> propertySubpatterns,
            ISymbol declaredSymbol,
            ITypeSymbol inputType,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new RecursivePatternOperation(matchedType, deconstructSymbol, deconstructionSubpatterns, propertySubpatterns, declaredSymbol, inputType, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IDiscardPatternOperation DiscardPattern(
            ITypeSymbol inputType,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new DiscardPatternOperation(inputType, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ISwitchExpressionOperation SwitchExpression(
            IOperation value,
            ImmutableArray<ISwitchExpressionArmOperation> arms,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new SwitchExpressionOperation(value, arms, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ISwitchExpressionArmOperation SwitchExpressionArm(
            IPatternOperation pattern,
            IOperation guard,
            IOperation value,
            ImmutableArray<ILocalSymbol> locals,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new SwitchExpressionArmOperation(pattern, guard, value, locals, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IPropertySubpatternOperation PropertySubpattern(
            IOperation member,
            IPatternOperation pattern,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new PropertySubpatternOperation(member, pattern, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IAggregateQueryOperation AggregateQuery(
            IOperation group,
            IOperation aggregation,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new AggregateQueryOperation(group, aggregation, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IFixedOperation Fixed(
            ImmutableArray<ILocalSymbol> locals,
            IVariableDeclarationGroupOperation variables,
            IOperation body,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new FixedOperation(locals, variables, body, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static INoPiaObjectCreationOperation NoPiaObjectCreation(
            IObjectOrCollectionInitializerOperation initializer,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new NoPiaObjectCreationOperation(initializer, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IPlaceholderOperation Placeholder(
            PlaceholderKind placeholderKind,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new PlaceholderOperation(placeholderKind, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IPointerIndirectionReferenceOperation PointerIndirectionReference(
            IOperation pointer,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new PointerIndirectionReferenceOperation(pointer, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IWithOperation With(
            IOperation body,
            IOperation value,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new WithOperation(body, value, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IUsingDeclarationOperation UsingDeclaration(
            IVariableDeclarationGroupOperation declarationGroup,
            bool isAsynchronous,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new UsingDeclarationOperation(declarationGroup, isAsynchronous, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static INegatedPatternOperation NegatedPattern(
            IPatternOperation negatedPattern,
            ITypeSymbol inputType,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new NegatedPatternOperation(negatedPattern, inputType, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IBinaryPatternOperation BinaryPattern(
            BinaryOperatorKind operatorKind,
            IPatternOperation leftPattern,
            IPatternOperation rightPattern,
            ITypeSymbol inputType,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new BinaryPatternOperation(operatorKind, leftPattern, rightPattern, inputType, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static ITypePatternOperation TypePattern(
            ITypeSymbol matchedType,
            ITypeSymbol inputType,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new TypePatternOperation(matchedType, inputType, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

        public static IRelationalPatternOperation RelationalPattern(
            BinaryOperatorKind operatorKind,
            IOperation value,
            ITypeSymbol inputType,
            ITypeSymbol type = null, Optional<object> constantValue = default, bool isImplicit = false)
        {
            return new RelationalPatternOperation(operatorKind, value, inputType, semanticModel: null, syntax: null, type, constantValue, isImplicit);
        }

    }
}
