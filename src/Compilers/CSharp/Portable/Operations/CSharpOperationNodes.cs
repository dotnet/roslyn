// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed class CSharpLazyAddressOfOperation : LazyAddressOfOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _reference;

        internal CSharpLazyAddressOfOperation(CSharpOperationFactory operationFactory, BoundNode reference, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _reference = reference;
        }

        protected override IOperation CreateReference()
        {
            return SetParentOperation(_operationFactory.Create(_reference), this);
        }
    }

    internal sealed class CSharpLazyNameOfOperation : LazyNameOfOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _argument;

        internal CSharpLazyNameOfOperation(CSharpOperationFactory operationFactory, BoundNode argument, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _argument = argument;
        }

        protected override IOperation CreateArgument()
        {
            return SetParentOperation(_operationFactory.Create(_argument), this);
        }
    }

    internal sealed class CSharpLazyThrowOperation : LazyThrowOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _exception;

        internal CSharpLazyThrowOperation(CSharpOperationFactory operationFactory, BoundNode exception, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _exception = exception;
        }

        protected override IOperation CreateException()
        {
            return SetParentOperation(_operationFactory.Create(_exception), this);
        }
    }

    internal sealed class CSharpLazyArgumentOperation : LazyArgumentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyArgumentOperation(CSharpOperationFactory operationFactory, BoundNode value, ArgumentKind argumentKind, IConvertibleConversion inConversionOpt, IConvertibleConversion outConversionOpt, IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit) : base(argumentKind, inConversionOpt, outConversionOpt, parameter, semanticModel, syntax, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyArrayCreationOperation : LazyArrayCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _dimensionSizes;
        private readonly BoundNode _initializer;

        internal CSharpLazyArrayCreationOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> dimensionSizes, BoundNode initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _dimensionSizes = dimensionSizes;
            _initializer = initializer;
        }

        protected override ImmutableArray<IOperation> CreateDimensionSizes()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundExpression, IOperation>(_dimensionSizes), this);
        }

        protected override IArrayInitializerOperation CreateInitializer()
        {
            return (IArrayInitializerOperation)SetParentOperation(_operationFactory.Create(_initializer), this);
        }
    }

    internal sealed class CSharpLazyArrayElementReferenceOperation : LazyArrayElementReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _arrayReference;
        private readonly ImmutableArray<BoundExpression> _indices;

        internal CSharpLazyArrayElementReferenceOperation(CSharpOperationFactory operationFactory, BoundNode arrayReference, ImmutableArray<BoundExpression> indices, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _arrayReference = arrayReference;
            _indices = indices;
        }

        protected override IOperation CreateArrayReference()
        {
            return SetParentOperation(_operationFactory.Create(_arrayReference), this);
        }

        protected override ImmutableArray<IOperation> CreateIndices()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundExpression, IOperation>(_indices), this);
        }
    }

    internal sealed class CSharpLazyArrayInitializerOperation : LazyArrayInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _elementValues;

        internal CSharpLazyArrayInitializerOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> elementValues, SemanticModel semanticModel, SyntaxNode syntax, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _elementValues = elementValues;
        }

        protected override ImmutableArray<IOperation> CreateElementValues()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundExpression, IOperation>(_elementValues), this);
        }
    }

    internal sealed class CSharpLazySimpleAssignmentOperation : LazySimpleAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _target;
        private readonly BoundNode _value;

        internal CSharpLazySimpleAssignmentOperation(CSharpOperationFactory operationFactory, BoundNode target, BoundNode value, bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _target = target;
            _value = value;
        }

        protected override IOperation CreateTarget()
        {
            return SetParentOperation(_operationFactory.Create(_target), this);
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyDeconstructionAssignmentOperation : LazyDeconstructionAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _target;
        private readonly BoundNode _value;

        internal CSharpLazyDeconstructionAssignmentOperation(CSharpOperationFactory operationFactory, BoundNode target, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _target = target;
            _value = value;
        }

        protected override IOperation CreateTarget()
        {
            return SetParentOperation(_operationFactory.Create(_target), this);
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyDeclarationExpressionOperation : LazyDeclarationExpressionOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _expression;

        internal CSharpLazyDeclarationExpressionOperation(CSharpOperationFactory operationFactory, BoundNode expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _expression = expression;
        }

        protected override IOperation CreateExpression()
        {
            return SetParentOperation(_operationFactory.Create(_expression), this);
        }
    }

    internal sealed class CSharpLazyAwaitOperation : LazyAwaitOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operation;

        internal CSharpLazyAwaitOperation(CSharpOperationFactory operationFactory, BoundNode operation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
        }

        protected override IOperation CreateOperation()
        {
            return SetParentOperation(_operationFactory.Create(_operation), this);
        }
    }

    internal sealed class CSharpLazyBinaryOperation : LazyBinaryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _leftOperand;
        private readonly BoundNode _rightOperand;

        internal CSharpLazyBinaryOperation(CSharpOperationFactory operationFactory, BoundNode leftOperand, BoundNode rightOperand, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, bool isCompareText, IMethodSymbol operatorMethod, IMethodSymbol unaryOperatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(operatorKind, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _leftOperand = leftOperand;
            _rightOperand = rightOperand;
        }

        protected override IOperation CreateLeftOperand()
        {
            return SetParentOperation(_operationFactory.Create(_leftOperand), this);
        }

        protected override IOperation CreateRightOperand()
        {
            return SetParentOperation(_operationFactory.Create(_rightOperand), this);
        }
    }

    internal sealed class CSharpLazyTupleBinaryOperation : LazyTupleBinaryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _leftOperand;
        private readonly BoundNode _rightOperand;

        internal CSharpLazyTupleBinaryOperation(CSharpOperationFactory operationFactory, BoundNode leftOperand, BoundNode rightOperand, BinaryOperatorKind operatorKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(operatorKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _leftOperand = leftOperand;
            _rightOperand = rightOperand;
        }

        protected override IOperation CreateLeftOperand()
        {
            return SetParentOperation(_operationFactory.Create(_leftOperand), this);
        }

        protected override IOperation CreateRightOperand()
        {
            return SetParentOperation(_operationFactory.Create(_rightOperand), this);
        }
    }

    internal sealed class CSharpLazyBlockOperation : LazyBlockOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundStatement> _operations;

        internal CSharpLazyBlockOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundStatement> operations, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operations = operations;
        }

        protected override ImmutableArray<IOperation> CreateOperations()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundStatement, IOperation>(_operations), this);
        }
    }

    internal sealed class CSharpLazyCatchClauseOperation : LazyCatchClauseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly IOperation _exceptionDeclarationOrExpression;
        private readonly BoundNode _filter;
        private readonly BoundNode _handler;

        internal CSharpLazyCatchClauseOperation(CSharpOperationFactory operationFactory, IOperation exceptionDeclarationOrExpression, BoundNode filter, BoundNode handler, ITypeSymbol exceptionType, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(exceptionType, locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _exceptionDeclarationOrExpression = SetParentOperation(exceptionDeclarationOrExpression, this);
            _filter = filter;
            _handler = handler;
        }

        protected override IOperation CreateExceptionDeclarationOrExpression()
        {
            return _exceptionDeclarationOrExpression;
        }

        protected override IOperation CreateFilter()
        {
            return SetParentOperation(_operationFactory.Create(_filter), this);
        }

        protected override IBlockOperation CreateHandler()
        {
            return (IBlockOperation)SetParentOperation(_operationFactory.Create(_handler), this);
        }
    }

    internal sealed class CSharpLazyCompoundAssignmentOperation : LazyCompoundAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _target;
        private readonly BoundNode _value;

        internal CSharpLazyCompoundAssignmentOperation(CSharpOperationFactory operationFactory, BoundNode target, BoundNode value, IConvertibleConversion inConversionConvertible, IConvertibleConversion outConversionConvertible, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(inConversionConvertible, outConversionConvertible, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _target = target;
            _value = value;
        }

        protected override IOperation CreateTarget()
        {
            return SetParentOperation(_operationFactory.Create(_target), this);
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyConditionalAccessOperation : LazyConditionalAccessOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operation;
        private readonly BoundNode _whenNotNull;

        internal CSharpLazyConditionalAccessOperation(CSharpOperationFactory operationFactory, BoundNode operation, BoundNode whenNotNull, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
            _whenNotNull = whenNotNull;
        }

        protected override IOperation CreateOperation()
        {
            return SetParentOperation(_operationFactory.Create(_operation), this);
        }

        protected override IOperation CreateWhenNotNull()
        {
            return SetParentOperation(_operationFactory.Create(_whenNotNull), this);
        }
    }

    internal sealed class CSharpLazyConditionalOperation : LazyConditionalOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _condition;
        private readonly BoundNode _whenTrue;
        private readonly BoundNode _whenFalse;

        internal CSharpLazyConditionalOperation(CSharpOperationFactory operationFactory, BoundNode condition, BoundNode whenTrue, BoundNode whenFalse, bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _condition = condition;
            _whenTrue = whenTrue;
            _whenFalse = whenFalse;
        }

        protected override IOperation CreateCondition()
        {
            return SetParentOperation(_operationFactory.Create(_condition), this);
        }

        protected override IOperation CreateWhenTrue()
        {
            return SetParentOperation(_operationFactory.Create(_whenTrue), this);
        }

        protected override IOperation CreateWhenFalse()
        {
            return SetParentOperation(_operationFactory.Create(_whenFalse), this);
        }
    }

    internal sealed class CSharpLazyConversionOperation : LazyConversionOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operand;

        internal CSharpLazyConversionOperation(CSharpOperationFactory operationFactory, BoundNode operand, IConvertibleConversion convertibleConversion, bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(convertibleConversion, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operand = operand;
        }

        protected override IOperation CreateOperand()
        {
            return SetParentOperation(_operationFactory.Create(_operand), this);
        }
    }

    internal sealed class CSharpLazyEventAssignmentOperation : LazyEventAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundEventAssignmentOperator _eventAssignmentOperator;

        internal CSharpLazyEventAssignmentOperation(CSharpOperationFactory operationFactory, BoundEventAssignmentOperator eventAssignmentOperator, bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(adds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _eventAssignmentOperator = eventAssignmentOperator;
        }

        protected override IOperation CreateEventReference()
        {
            return SetParentOperation(_operationFactory.CreateBoundEventAccessOperation(_eventAssignmentOperator), this);
        }

        protected override IOperation CreateHandlerValue()
        {
            return SetParentOperation(_operationFactory.Create(_eventAssignmentOperator.Argument), this);
        }
    }

    internal sealed class CSharpLazyEventReferenceOperation : LazyEventReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _instance;

        internal CSharpLazyEventReferenceOperation(CSharpOperationFactory operationFactory, BoundNode instance, IEventSymbol @event, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(@event, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
        }

        protected override IOperation CreateInstance()
        {
            return SetParentOperation(_operationFactory.CreateReceiverOperation(_instance, Event), this);
        }
    }

    internal sealed class CSharpLazyExpressionStatementOperation : LazyExpressionStatementOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operation;

        internal CSharpLazyExpressionStatementOperation(CSharpOperationFactory operationFactory, BoundNode operation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
        }

        protected override IOperation CreateOperation()
        {
            return SetParentOperation(_operationFactory.Create(_operation), this);
        }
    }

    internal sealed class CSharpLazyVariableInitializerOperation : LazyVariableInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyVariableInitializerOperation(CSharpOperationFactory operationFactory, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyFieldInitializerOperation : LazyFieldInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyFieldInitializerOperation(CSharpOperationFactory operationFactory, BoundNode value, ImmutableArray<ILocalSymbol> locals, ImmutableArray<IFieldSymbol> initializedFields, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, initializedFields, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyFieldReferenceOperation : LazyFieldReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _instance;

        internal CSharpLazyFieldReferenceOperation(CSharpOperationFactory operationFactory, BoundNode instance, IFieldSymbol field, bool isDeclaration, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(field, isDeclaration, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
        }

        protected override IOperation CreateInstance()
        {
            return SetParentOperation(_operationFactory.CreateReceiverOperation(_instance, Field), this);
        }
    }

    internal sealed class CSharpLazyFixedOperation : LazyFixedOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _variables;
        private readonly BoundNode _body;

        internal CSharpLazyFixedOperation(CSharpOperationFactory operationFactory, BoundNode variables, BoundNode body, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _variables = variables;
            _body = body;
        }

        protected override IVariableDeclarationGroupOperation CreateVariables()
        {
            return (IVariableDeclarationGroupOperation)SetParentOperation(_operationFactory.Create(_variables), this);
        }

        protected override IOperation CreateBody()
        {
            return SetParentOperation(_operationFactory.Create(_body), this);
        }
    }

    internal sealed class CSharpLazyForEachLoopOperation : LazyForEachLoopOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _loopControlVariable;
        private readonly IOperation _nonLazyLoopControlVariable;
        private readonly BoundNode _collection;
        private readonly BoundNode _body;

        internal CSharpLazyForEachLoopOperation(CSharpOperationFactory operationFactory, BoundNode loopControlVariable, IOperation nonLazyLoopControlVariable, BoundNode collection, BoundNode body, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, ForEachLoopOperationInfo info, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, continueLabel, exitLabel, info, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _loopControlVariable = loopControlVariable;
            _nonLazyLoopControlVariable = SetParentOperation(nonLazyLoopControlVariable, this);
            _collection = collection;
            _body = body;
        }

        protected override IOperation CreateLoopControlVariable()
        {
            return _nonLazyLoopControlVariable ?? SetParentOperation(_operationFactory.Create(_loopControlVariable), this);
        }

        protected override IOperation CreateCollection()
        {
            return SetParentOperation(_operationFactory.Create(_collection), this);
        }

        protected override ImmutableArray<IOperation> CreateNextVariables()
        {
            return ImmutableArray<IOperation>.Empty;
        }

        protected override IOperation CreateBody()
        {
            return SetParentOperation(_operationFactory.Create(_body), this);
        }
    }

    internal sealed class CSharpLazyForLoopOperation : LazyForLoopOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundStatement> _before;
        private readonly BoundNode _condition;
        private readonly ImmutableArray<BoundStatement> _atLoopBottom;
        private readonly BoundNode _body;

        internal CSharpLazyForLoopOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundStatement> before, BoundNode condition, ImmutableArray<BoundStatement> atLoopBottom, BoundNode body, ImmutableArray<ILocalSymbol> locals, ImmutableArray<ILocalSymbol> conditionLocals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, conditionLocals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _before = before;
            _condition = condition;
            _atLoopBottom = atLoopBottom;
            _body = body;
        }

        protected override ImmutableArray<IOperation> CreateBefore()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundStatement, IOperation>(_before), this);
        }

        protected override IOperation CreateCondition()
        {
            return SetParentOperation(_operationFactory.Create(_condition), this);
        }

        protected override ImmutableArray<IOperation> CreateAtLoopBottom()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundStatement, IOperation>(_atLoopBottom), this);
        }

        protected override IOperation CreateBody()
        {
            return SetParentOperation(_operationFactory.Create(_body), this);
        }
    }

    internal sealed class CSharpLazyForToLoopOperation : LazyForToLoopOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _loopControlVariable;
        private readonly BoundNode _initialValue;
        private readonly BoundNode _limitValue;
        private readonly BoundNode _stepValue;
        private readonly BoundNode _body;
        private readonly ImmutableArray<BoundNode> _nextVariables;

        internal CSharpLazyForToLoopOperation(CSharpOperationFactory operationFactory, BoundNode loopControlVariable, BoundNode initialValue, BoundNode limitValue, BoundNode stepValue, BoundNode body, ImmutableArray<BoundNode> nextVariables, ImmutableArray<ILocalSymbol> locals, bool isChecked, (ILocalSymbol LoopObject, ForToLoopOperationUserDefinedInfo UserDefinedInfo) info, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, isChecked, info, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _loopControlVariable = loopControlVariable;
            _initialValue = initialValue;
            _limitValue = limitValue;
            _stepValue = stepValue;
            _body = body;
            _nextVariables = nextVariables;
        }

        protected override IOperation CreateLoopControlVariable()
        {
            return SetParentOperation(_operationFactory.Create(_loopControlVariable), this);
        }

        protected override IOperation CreateInitialValue()
        {
            return SetParentOperation(_operationFactory.Create(_initialValue), this);
        }

        protected override IOperation CreateLimitValue()
        {
            return SetParentOperation(_operationFactory.Create(_limitValue), this);
        }

        protected override IOperation CreateStepValue()
        {
            return SetParentOperation(_operationFactory.Create(_stepValue), this);
        }

        protected override IOperation CreateBody()
        {
            return SetParentOperation(_operationFactory.Create(_body), this);
        }

        protected override ImmutableArray<IOperation> CreateNextVariables()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundNode, IOperation>(_nextVariables), this);
        }
    }

    internal sealed class CSharpLazyIncrementOrDecrementOperation : LazyIncrementOrDecrementOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _target;

        internal CSharpLazyIncrementOrDecrementOperation(CSharpOperationFactory operationFactory, BoundNode target, bool isDecrement, bool isPostfix, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(isDecrement, isPostfix, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _target = target;
        }

        protected override IOperation CreateTarget()
        {
            return SetParentOperation(_operationFactory.Create(_target), this);
        }
    }

    internal sealed class CSharpLazyInterpolatedStringOperation : LazyInterpolatedStringOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _parts;

        internal CSharpLazyInterpolatedStringOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> parts, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _parts = parts;
        }

        protected override ImmutableArray<IInterpolatedStringContentOperation> CreateParts()
        {
            return SetParentOperation(_operationFactory.CreateBoundInterpolatedStringContentOperation(_parts), this);
        }
    }

    internal sealed class CSharpLazyInterpolatedStringTextOperation : LazyInterpolatedStringTextOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _text;

        internal CSharpLazyInterpolatedStringTextOperation(CSharpOperationFactory operationFactory, BoundNode text, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _text = text;
        }

        protected override IOperation CreateText()
        {
            return SetParentOperation(_operationFactory.Create(_text), this);
        }
    }

    internal sealed class CSharpLazyInterpolationOperation : LazyInterpolationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _expression;
        private readonly BoundNode _alignment;
        private readonly BoundNode _formatString;

        internal CSharpLazyInterpolationOperation(CSharpOperationFactory operationFactory, BoundNode expression, BoundNode alignment, BoundNode formatString, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _expression = expression;
            _alignment = alignment;
            _formatString = formatString;
        }

        protected override IOperation CreateExpression()
        {
            return SetParentOperation(_operationFactory.Create(_expression), this);
        }

        protected override IOperation CreateAlignment()
        {
            return SetParentOperation(_operationFactory.Create(_alignment), this);
        }

        protected override IOperation CreateFormatString()
        {
            return SetParentOperation(_operationFactory.Create(_formatString), this);
        }
    }

    internal sealed class CSharpLazyInvalidOperation : LazyInvalidOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundNode> _children;

        internal CSharpLazyInvalidOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundNode> children, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _children = children;
        }

        protected override ImmutableArray<IOperation> CreateChildren()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundNode, IOperation>(_children), this);
        }
    }

    internal sealed class CSharpLazyInvocationOperation : LazyInvocationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundExpression _instance;
        private readonly BoundNode _boundCall;

        internal CSharpLazyInvocationOperation(CSharpOperationFactory operationFactory, BoundExpression instance, BoundNode boundNode, IMethodSymbol targetMethod, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
            _boundCall = boundNode;
        }

        protected override IOperation CreateInstance()
        {
            return SetParentOperation(_operationFactory.CreateReceiverOperation(_instance, TargetMethod), this);
        }

        protected override ImmutableArray<IArgumentOperation> CreateArguments()
        {
            return SetParentOperation(_operationFactory.DeriveArguments(_boundCall), this);
        }
    }

    internal sealed class CSharpLazyRaiseEventOperation : LazyRaiseEventOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _eventReference;
        private readonly ImmutableArray<BoundNode> _arguments;

        internal CSharpLazyRaiseEventOperation(CSharpOperationFactory operationFactory, BoundNode eventReference, ImmutableArray<BoundNode> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _eventReference = eventReference;
            _arguments = arguments;
        }

        protected override IEventReferenceOperation CreateEventReference()
        {
            return (IEventReferenceOperation)SetParentOperation(_operationFactory.Create(_eventReference), this);
        }

        protected override ImmutableArray<IArgumentOperation> CreateArguments()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundNode, IArgumentOperation>(_arguments), this);
        }
    }

    internal sealed class CSharpLazyIsTypeOperation : LazyIsTypeOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _valueOperand;

        internal CSharpLazyIsTypeOperation(CSharpOperationFactory operationFactory, BoundNode valueOperand, ITypeSymbol isType, bool isNotTypeExpression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(isType, isNotTypeExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _valueOperand = valueOperand;
        }

        protected override IOperation CreateValueOperand()
        {
            return SetParentOperation(_operationFactory.Create(_valueOperand), this);
        }
    }

    internal sealed class CSharpLazyLabeledOperation : LazyLabeledOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operation;

        internal CSharpLazyLabeledOperation(CSharpOperationFactory operationFactory, BoundNode operation, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
        }

        protected override IOperation CreateOperation()
        {
            return SetParentOperation(_operationFactory.Create(_operation), this);
        }
    }

    internal sealed class CSharpLazyAnonymousFunctionOperation : LazyAnonymousFunctionOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _body;

        internal CSharpLazyAnonymousFunctionOperation(CSharpOperationFactory operationFactory, BoundNode body, IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _body = body;
        }

        protected override IBlockOperation CreateBody()
        {
            return (IBlockOperation)SetParentOperation(_operationFactory.Create(_body), this);
        }
    }

    internal sealed class CSharpLazyDelegateCreationOperation : LazyDelegateCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _delegateNode;

        internal CSharpLazyDelegateCreationOperation(CSharpOperationFactory operationFactory, BoundNode delegateNode, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _delegateNode = delegateNode;
        }

        protected override IOperation CreateTarget()
        {
            return SetParentOperation(_operationFactory.CreateDelegateTargetOperation(_delegateNode), this);
        }
    }

    internal sealed class CSharpLazyDynamicMemberReferenceOperation : LazyDynamicMemberReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _instance;

        internal CSharpLazyDynamicMemberReferenceOperation(CSharpOperationFactory operationFactory, BoundNode instance, string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol containingType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
        }

        protected override IOperation CreateInstance()
        {
            return SetParentOperation(_operationFactory.Create(_instance), this);
        }
    }

    internal sealed class CSharpLazyLockOperation : LazyLockOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _lockedValue;
        private readonly BoundNode _body;

        internal CSharpLazyLockOperation(CSharpOperationFactory operationFactory, BoundNode lockedValue, BoundNode body, ILocalSymbol lockTakenSymbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(lockTakenSymbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _lockedValue = lockedValue;
            _body = body;
        }

        protected override IOperation CreateLockedValue()
        {
            return SetParentOperation(_operationFactory.Create(_lockedValue), this);
        }

        protected override IOperation CreateBody()
        {
            return SetParentOperation(_operationFactory.Create(_body), this);
        }
    }

    internal sealed class CSharpLazyMethodReferenceOperation : LazyMethodReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _instance;

        internal CSharpLazyMethodReferenceOperation(CSharpOperationFactory operationFactory, BoundNode instance, IMethodSymbol method, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(method, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
        }

        protected override IOperation CreateInstance()
        {
            return SetParentOperation(_operationFactory.CreateReceiverOperation(_instance, Method), this);
        }
    }

    internal sealed class CSharpLazyCoalesceOperation : LazyCoalesceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;
        private readonly BoundNode _whenNull;

        internal CSharpLazyCoalesceOperation(CSharpOperationFactory operationFactory, BoundNode value, BoundNode whenNull, IConvertibleConversion convertibleValueConversion, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(convertibleValueConversion, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
            _whenNull = whenNull;
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }

        protected override IOperation CreateWhenNull()
        {
            return SetParentOperation(_operationFactory.Create(_whenNull), this);
        }
    }

    internal sealed class CSharpLazyCoalesceAssignmentOperation : LazyCoalesceAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _target;
        private readonly BoundNode _value;

        internal CSharpLazyCoalesceAssignmentOperation(CSharpOperationFactory operationFactory, BoundNode target, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _target = target;
            _value = value;
        }

        protected override IOperation CreateTarget()
        {
            return SetParentOperation(_operationFactory.Create(_target), this);
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyObjectCreationOperation : LazyObjectCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundObjectCreationExpression _objectCreation;

        internal CSharpLazyObjectCreationOperation(CSharpOperationFactory operationFactory, BoundObjectCreationExpression objectCreation, IMethodSymbol constructor, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(constructor, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _objectCreation = objectCreation;
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)SetParentOperation(_operationFactory.Create(_objectCreation.InitializerExpressionOpt), this);
        }

        protected override ImmutableArray<IArgumentOperation> CreateArguments()
        {
            return SetParentOperation(_operationFactory.DeriveArguments(_objectCreation), this);
        }
    }

    internal sealed class CSharpLazyAnonymousObjectCreationOperation : LazyAnonymousObjectCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _arguments;
        private readonly ImmutableArray<BoundAnonymousPropertyDeclaration> _declarations;

        internal CSharpLazyAnonymousObjectCreationOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> arguments, ImmutableArray<BoundAnonymousPropertyDeclaration> declarations, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _arguments = arguments;
            _declarations = declarations;
        }

        protected override ImmutableArray<IOperation> CreateInitializers()
        {
            return SetParentOperation(_operationFactory.GetAnonymousObjectCreationInitializers(_arguments, _declarations, Syntax, Type, IsImplicit), this);
        }
    }

    internal sealed class CSharpLazyParameterInitializerOperation : LazyParameterInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyParameterInitializerOperation(CSharpOperationFactory operationFactory, BoundNode value, ImmutableArray<ILocalSymbol> locals, IParameterSymbol parameter, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, parameter, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyParenthesizedOperation : LazyParenthesizedOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operand;

        internal CSharpLazyParenthesizedOperation(CSharpOperationFactory operationFactory, BoundNode operand, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operand = operand;
        }

        protected override IOperation CreateOperand()
        {
            return SetParentOperation(_operationFactory.Create(_operand), this);
        }
    }

    internal sealed class CSharpLazyPropertyInitializerOperation : LazyPropertyInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyPropertyInitializerOperation(CSharpOperationFactory operationFactory, BoundNode value, ImmutableArray<ILocalSymbol> locals, ImmutableArray<IPropertySymbol> initializedProperties, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, initializedProperties, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyPropertyReferenceOperation : LazyPropertyReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly IOperation _instanceNonLazy;
        private readonly BoundNode _instance;
        private readonly BoundNode _argumentsInstance;
        private readonly bool _isObjectOrCollectionInitializer = false;

        internal CSharpLazyPropertyReferenceOperation(CSharpOperationFactory operationFactory, BoundNode instance, IPropertySymbol property, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : this(operationFactory, instance, argumentsInstance: null, property, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        internal CSharpLazyPropertyReferenceOperation(CSharpOperationFactory operationFactory, IOperation instance, BoundNode argumentsInstance, IPropertySymbol property, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit, bool isObjectOrCollectionInitializer = false) : base(property, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instanceNonLazy = SetParentOperation(instance, this);
            _argumentsInstance = argumentsInstance;
            _isObjectOrCollectionInitializer = isObjectOrCollectionInitializer;
        }

        internal CSharpLazyPropertyReferenceOperation(CSharpOperationFactory operationFactory, BoundNode instance, BoundNode argumentsInstance, IPropertySymbol property, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(property, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
            _argumentsInstance = argumentsInstance;
        }

        protected override IOperation CreateInstance()
        {
            return _instanceNonLazy ?? SetParentOperation(_operationFactory.CreateReceiverOperation(_instance, Property), this);
        }

        protected override ImmutableArray<IArgumentOperation> CreateArguments()
        {
            return _argumentsInstance is null ? ImmutableArray<IArgumentOperation>.Empty : SetParentOperation(_operationFactory.DeriveArguments(_argumentsInstance, _isObjectOrCollectionInitializer), this);
        }
    }

    internal sealed class CSharpLazyRangeCaseClauseOperation : LazyRangeCaseClauseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _minimumValue;
        private readonly BoundNode _maximumValue;

        internal CSharpLazyRangeCaseClauseOperation(CSharpOperationFactory operationFactory, BoundNode minimumValue, BoundNode maximumValue, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _minimumValue = minimumValue;
            _maximumValue = maximumValue;
        }

        protected override IOperation CreateMinimumValue()
        {
            return SetParentOperation(_operationFactory.Create(_minimumValue), this);
        }

        protected override IOperation CreateMaximumValue()
        {
            return SetParentOperation(_operationFactory.Create(_maximumValue), this);
        }
    }

    internal sealed class CSharpLazyRelationalCaseClauseOperation : LazyRelationalCaseClauseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyRelationalCaseClauseOperation(CSharpOperationFactory operationFactory, BoundNode value, BinaryOperatorKind relation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(relation, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyReturnOperation : LazyReturnOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _returnedValue;

        internal CSharpLazyReturnOperation(CSharpOperationFactory operationFactory, BoundNode returnedValue, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _returnedValue = returnedValue;
        }

        protected override IOperation CreateReturnedValue()
        {
            return SetParentOperation(_operationFactory.Create(_returnedValue), this);
        }
    }

    internal sealed class CSharpLazySingleValueCaseClauseOperation : LazySingleValueCaseClauseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazySingleValueCaseClauseOperation(CSharpOperationFactory operationFactory, BoundNode value, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazySwitchCaseOperation : LazySwitchCaseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundNode> _clauses;
        private readonly BoundNode _condition;
        private readonly ImmutableArray<BoundStatement> _body;

        internal CSharpLazySwitchCaseOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundNode> clauses, BoundNode condition, ImmutableArray<BoundStatement> body, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _clauses = clauses;
            _condition = condition;
            _body = body;
        }

        protected override ImmutableArray<ICaseClauseOperation> CreateClauses()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundNode, ICaseClauseOperation>(_clauses), this);
        }

        protected override IOperation CreateCondition()
        {
            return SetParentOperation(_operationFactory.Create(_condition), null);
        }

        protected override ImmutableArray<IOperation> CreateBody()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundStatement, IOperation>(_body), this);
        }
    }

    internal sealed class CSharpLazySwitchOperation : LazySwitchOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;
        private readonly ImmutableArray<BoundStatementList> _cases;

        internal CSharpLazySwitchOperation(CSharpOperationFactory operationFactory, BoundNode value, ImmutableArray<BoundStatementList> cases, ImmutableArray<ILocalSymbol> locals, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
            _cases = cases;
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }

        protected override ImmutableArray<ISwitchCaseOperation> CreateCases()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundStatementList, ISwitchCaseOperation>(_cases), this);
        }
    }

    internal sealed class CSharpLazyTryOperation : LazyTryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _body;
        private readonly ImmutableArray<BoundCatchBlock> _catches;
        private readonly BoundNode _finally;

        internal CSharpLazyTryOperation(CSharpOperationFactory operationFactory, BoundNode body, ImmutableArray<BoundCatchBlock> catches, BoundNode @finally, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _body = body;
            _catches = catches;
            _finally = @finally;
        }

        protected override IBlockOperation CreateBody()
        {
            return (IBlockOperation)SetParentOperation(_operationFactory.Create(_body), this);
        }

        protected override ImmutableArray<ICatchClauseOperation> CreateCatches()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundCatchBlock, ICatchClauseOperation>(_catches), this);
        }

        protected override IBlockOperation CreateFinally()
        {
            return (IBlockOperation)SetParentOperation(_operationFactory.Create(_finally), this);
        }
    }

    internal sealed class CSharpLazyTupleOperation : LazyTupleOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _elements;

        internal CSharpLazyTupleOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> elements, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ITypeSymbol naturalType, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, naturalType, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _elements = elements;
        }

        protected override ImmutableArray<IOperation> CreateElements()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundExpression, IOperation>(_elements), this);
        }
    }

    internal sealed class CSharpLazyTypeParameterObjectCreationOperation : LazyTypeParameterObjectCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _initializer;

        internal CSharpLazyTypeParameterObjectCreationOperation(CSharpOperationFactory operationFactory, BoundNode initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _initializer = initializer;
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)SetParentOperation(_operationFactory.Create(_initializer), this);
        }
    }

    internal sealed class CSharpLazyDynamicObjectCreationOperation : LazyDynamicObjectCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _arguments;
        private readonly BoundNode _initializer;

        internal CSharpLazyDynamicObjectCreationOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> arguments, BoundNode initializer, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _arguments = arguments;
            _initializer = initializer;
        }

        protected override ImmutableArray<IOperation> CreateArguments()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundExpression, IOperation>(_arguments), this);
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)SetParentOperation(_operationFactory.Create(_initializer), this);
        }
    }

    internal sealed class CSharpLazyDynamicInvocationOperation : LazyDynamicInvocationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly IOperation _operationNonLazy;
        private readonly BoundNode _operation;
        private readonly ImmutableArray<BoundExpression> _arguments;

        internal CSharpLazyDynamicInvocationOperation(CSharpOperationFactory operationFactory, BoundNode operation, ImmutableArray<BoundExpression> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
            _arguments = arguments;
        }

        internal CSharpLazyDynamicInvocationOperation(CSharpOperationFactory operationFactory, IOperation operation, ImmutableArray<BoundExpression> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(operationFactory, operation: (BoundNode)null, arguments, argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationNonLazy = SetParentOperation(operation, this);
        }

        protected override IOperation CreateOperation()
        {
            if (_operationNonLazy != null)
            {
                return _operationNonLazy;
            }
            IOperation operation;
            if (_operation.Kind == BoundKind.MethodGroup)
            {
                var methodGroup = (BoundMethodGroup)_operation;
                operation = _operationFactory.CreateBoundDynamicMemberAccessOperation(methodGroup.ReceiverOpt, TypeMap.AsTypeSymbols(methodGroup.TypeArgumentsOpt), methodGroup.Name, methodGroup.Syntax, methodGroup.Type, methodGroup.ConstantValue, methodGroup.WasCompilerGenerated);
            }
            else
            {
                operation = _operationFactory.Create(_operation);
            }
            return SetParentOperation(operation, this);
        }

        protected override ImmutableArray<IOperation> CreateArguments()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundExpression, IOperation>(_arguments), this);
        }
    }

    internal sealed class CSharpLazyDynamicIndexerAccessOperation : LazyDynamicIndexerAccessOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly IOperation _operationNonLazy;
        private readonly BoundNode _operation;
        private readonly ImmutableArray<BoundExpression> _arguments;

        internal CSharpLazyDynamicIndexerAccessOperation(CSharpOperationFactory operationFactory, BoundNode operation, ImmutableArray<BoundExpression> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
            _arguments = arguments;
        }

        internal CSharpLazyDynamicIndexerAccessOperation(CSharpOperationFactory operationFactory, IOperation operation, ImmutableArray<BoundExpression> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(operationFactory, operation: (BoundNode)null, arguments, argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationNonLazy = SetParentOperation(operation, this);
        }

        protected override IOperation CreateOperation()
        {
            return _operationNonLazy ?? SetParentOperation(_operationFactory.Create(_operation), this);
        }

        protected override ImmutableArray<IOperation> CreateArguments()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundExpression, IOperation>(_arguments), this);
        }
    }

    internal sealed class CSharpLazyUnaryOperation : LazyUnaryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operand;

        internal CSharpLazyUnaryOperation(CSharpOperationFactory operationFactory, BoundNode operand, UnaryOperatorKind unaryOperationKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(unaryOperationKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operand = operand;
        }

        protected override IOperation CreateOperand()
        {
            return SetParentOperation(_operationFactory.Create(_operand), this);
        }
    }

    internal sealed class CSharpLazyUsingOperation : LazyUsingOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _resources;
        private readonly BoundNode _body;

        internal CSharpLazyUsingOperation(CSharpOperationFactory operationFactory, BoundNode resources, BoundNode body, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _resources = resources;
            _body = body;
        }

        protected override IOperation CreateResources()
        {
            return SetParentOperation(_operationFactory.Create(_resources), this);
        }

        protected override IOperation CreateBody()
        {
            return SetParentOperation(_operationFactory.Create(_body), this);
        }
    }

    internal sealed class CSharpLazyVariableDeclaratorOperation : LazyVariableDeclaratorOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _initializer;
        private readonly ImmutableArray<BoundNode> _ignoredArguments;

        internal CSharpLazyVariableDeclaratorOperation(CSharpOperationFactory operationFactory, BoundNode initializer, ImmutableArray<BoundNode> ignoredArguments, ILocalSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _initializer = initializer;
            _ignoredArguments = ignoredArguments;
        }

        protected override IVariableInitializerOperation CreateInitializer()
        {
            return (IVariableInitializerOperation)SetParentOperation(_operationFactory.Create(_initializer), this);
        }

        protected override ImmutableArray<IOperation> CreateIgnoredArguments()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundNode, IOperation>(_ignoredArguments), this);
        }
    }

    internal sealed class CSharpLazyVariableDeclarationOperation : LazyVariableDeclarationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _localDeclaration;
        private readonly BoundNode _initializer;

        internal CSharpLazyVariableDeclarationOperation(CSharpOperationFactory operationFactory, BoundNode localDeclaration, BoundNode initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _localDeclaration = localDeclaration;
            _initializer = initializer;
        }

        protected override ImmutableArray<IVariableDeclaratorOperation> CreateDeclarators()
        {
            return SetParentOperation(_operationFactory.CreateVariableDeclarator(_localDeclaration, Syntax), this);
        }

        protected override IVariableInitializerOperation CreateInitializer()
        {
            return (IVariableInitializerOperation)SetParentOperation(_operationFactory.Create(_initializer), this);
        }
    }

    internal sealed class CSharpLazyWhileLoopOperation : LazyWhileLoopOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _condition;
        private readonly BoundNode _body;
        private readonly BoundNode _ignoredCondition;

        internal CSharpLazyWhileLoopOperation(CSharpOperationFactory operationFactory, BoundNode condition, BoundNode body, BoundNode ignoredCondition, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, bool conditionIsTop, bool conditionIsUntil, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, continueLabel, exitLabel, conditionIsTop, conditionIsUntil, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _condition = condition;
            _body = body;
            _ignoredCondition = ignoredCondition;
        }

        protected override IOperation CreateCondition()
        {
            return SetParentOperation(_operationFactory.Create(_condition), this);
        }

        protected override IOperation CreateBody()
        {
            return SetParentOperation(_operationFactory.Create(_body), this);
        }

        protected override IOperation CreateIgnoredCondition()
        {
            return SetParentOperation(_operationFactory.Create(_ignoredCondition), this);
        }
    }

    internal sealed class CSharpLazyWithOperation : LazyWithOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _body;
        private readonly BoundNode _value;

        internal CSharpLazyWithOperation(CSharpOperationFactory operationFactory, BoundNode body, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _body = body;
            _value = value;
        }

        protected override IOperation CreateBody()
        {
            return SetParentOperation(_operationFactory.Create(_body), this);
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyLocalFunctionOperation : LazyLocalFunctionOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _body;
        private readonly BoundNode _ignoredBody;

        internal CSharpLazyLocalFunctionOperation(CSharpOperationFactory operationFactory, BoundNode body, BoundNode ignoredBody, IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _body = body;
            _ignoredBody = ignoredBody;
        }

        protected override IBlockOperation CreateBody()
        {
            return (IBlockOperation)SetParentOperation(_operationFactory.Create(_body), this);
        }

        protected override IBlockOperation CreateIgnoredBody()
        {
            return (IBlockOperation)SetParentOperation(_operationFactory.Create(_ignoredBody), this);
        }
    }

    internal sealed class CSharpLazyConstantPatternOperation : LazyConstantPatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyConstantPatternOperation(CSharpOperationFactory operationFactory, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }
    }

    internal sealed class CSharpLazyPatternCaseClauseOperation : LazyPatternCaseClauseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _pattern;
        private readonly BoundNode _guard;

        internal CSharpLazyPatternCaseClauseOperation(CSharpOperationFactory operationFactory, BoundNode pattern, BoundNode guard, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _pattern = pattern;
            _guard = guard;
        }

        protected override IPatternOperation CreatePattern()
        {
            return (IPatternOperation)SetParentOperation(_operationFactory.Create(_pattern), this);
        }

        protected override IOperation CreateGuard()
        {
            return SetParentOperation(_operationFactory.Create(_guard), this);
        }
    }

    internal sealed class CSharpLazyIsPatternOperation : LazyIsPatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;
        private readonly BoundNode _pattern;

        internal CSharpLazyIsPatternOperation(CSharpOperationFactory operationFactory, BoundNode value, BoundNode pattern, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
            _pattern = pattern;
        }

        protected override IOperation CreateValue()
        {
            return SetParentOperation(_operationFactory.Create(_value), this);
        }

        protected override IPatternOperation CreatePattern()
        {
            return (IPatternOperation)SetParentOperation(_operationFactory.Create(_pattern), this);
        }
    }

    internal sealed class CSharpLazyObjectOrCollectionInitializerOperation : LazyObjectOrCollectionInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _initializers;

        internal CSharpLazyObjectOrCollectionInitializerOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> initializers, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _initializers = initializers;
        }

        protected override ImmutableArray<IOperation> CreateInitializers()
        {
            return SetParentOperation(_operationFactory.CreateFromArray<BoundExpression, IOperation>(_initializers), this);
        }
    }

    internal sealed class CSharpLazyMemberInitializerOperation : LazyMemberInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _initializedMember;
        private readonly BoundNode _initializer;

        internal CSharpLazyMemberInitializerOperation(CSharpOperationFactory operationFactory, BoundNode initializedMember, BoundNode initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _initializedMember = initializedMember;
            _initializer = initializer;
        }

        protected override IOperation CreateInitializedMember()
        {
            return SetParentOperation(_operationFactory.CreateMemberInitializerInitializedMember(_initializedMember), this);
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)SetParentOperation(_operationFactory.Create(_initializer), this);
        }
    }

    internal sealed class CSharpLazyTranslatedQueryOperation : LazyTranslatedQueryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operation;

        internal CSharpLazyTranslatedQueryOperation(CSharpOperationFactory operationFactory, BoundNode operation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
        }

        protected override IOperation CreateOperation()
        {
            return SetParentOperation(_operationFactory.Create(_operation), this);
        }
    }

    internal sealed class CSharpLazyMethodBodyOperation : LazyMethodBodyOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _blockBody;
        private readonly BoundNode _expressionBody;

        internal CSharpLazyMethodBodyOperation(CSharpOperationFactory operationFactory, BoundNode blockBody, BoundNode expressionBody, SemanticModel semanticModel, SyntaxNode syntax) : base(semanticModel, syntax)
        {
            _operationFactory = operationFactory;
            _blockBody = blockBody;
            _expressionBody = expressionBody;
        }

        protected override IBlockOperation CreateBlockBody()
        {
            return (IBlockOperation)SetParentOperation(_operationFactory.Create(_blockBody), this);
        }

        protected override IBlockOperation CreateExpressionBody()
        {
            return (IBlockOperation)SetParentOperation(_operationFactory.Create(_expressionBody), this);
        }
    }

    internal sealed class CSharpLazyConstructorBodyOperation : LazyConstructorBodyOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _initializer;
        private readonly BoundNode _blockBody;
        private readonly BoundNode _expressionBody;

        internal CSharpLazyConstructorBodyOperation(CSharpOperationFactory operationFactory, BoundNode initializer, BoundNode blockBody, BoundNode expressionBody, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax) : base(locals, semanticModel, syntax)
        {
            _operationFactory = operationFactory;
            _initializer = initializer;
            _blockBody = blockBody;
            _expressionBody = expressionBody;
        }

        protected override IOperation CreateInitializer()
        {
            return SetParentOperation(_operationFactory.Create(_initializer), this);
        }

        protected override IBlockOperation CreateBlockBody()
        {
            return (IBlockOperation)SetParentOperation(_operationFactory.Create(_blockBody), this);
        }

        protected override IBlockOperation CreateExpressionBody()
        {
            return (IBlockOperation)SetParentOperation(_operationFactory.Create(_expressionBody), this);
        }
    }

    internal sealed class CSharpLazyAggregateQueryOperation : LazyAggregateQueryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _group;
        private readonly BoundNode _aggregation;

        internal CSharpLazyAggregateQueryOperation(CSharpOperationFactory operationFactory, BoundNode group, BoundNode aggregation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _group = group;
            _aggregation = aggregation;
        }

        protected override IOperation CreateGroup()
        {
            return SetParentOperation(_operationFactory.Create(_group), this);
        }

        protected override IOperation CreateAggregation()
        {
            return SetParentOperation(_operationFactory.Create(_aggregation), this);
        }
    }

    internal sealed class CSharpLazyNoPiaObjectCreationOperation : LazyNoPiaObjectCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _initializer;

        internal CSharpLazyNoPiaObjectCreationOperation(CSharpOperationFactory operationFactory, BoundNode initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _initializer = initializer;
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)SetParentOperation(_operationFactory.Create(_initializer), this);
        }
    }

    internal sealed class CSharpLazyFromEndIndexOperation : LazyFromEndIndexOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operand;

        internal CSharpLazyFromEndIndexOperation(CSharpOperationFactory operationFactory, BoundNode operand, bool isLifted, bool isImplicit, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, IMethodSymbol symbol) : base(isLifted, isImplicit, semanticModel, syntax, type, symbol)
        {
            _operationFactory = operationFactory;
            _operand = operand;
        }

        protected override IOperation CreateOperand()
        {
            return SetParentOperation(_operationFactory.Create(_operand), this);
        }
    }

    internal sealed class CSharpLazyRangeOperation : LazyRangeOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _leftOperand;
        private readonly BoundNode _rightOperand;

        internal CSharpLazyRangeOperation(CSharpOperationFactory operationFactory, BoundNode leftOperand, BoundNode rightOperand, bool isLifted, bool isImplicit, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, IMethodSymbol symbol) : base(isLifted, isImplicit, semanticModel, syntax, type, symbol)
        {
            _operationFactory = operationFactory;
            _leftOperand = leftOperand;
            _rightOperand = rightOperand;
        }

        protected override IOperation CreateLeftOperand()
        {
            return SetParentOperation(_operationFactory.Create(_leftOperand), this);
        }

        protected override IOperation CreateRightOperand()
        {
            return SetParentOperation(_operationFactory.Create(_rightOperand), this);
        }
    }
}
