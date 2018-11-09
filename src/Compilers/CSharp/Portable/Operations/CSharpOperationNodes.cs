// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed class CSharpLazyAddressOfOperation : LazyAddressOfOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _reference;

        internal CSharpLazyAddressOfOperation(CSharpOperationFactory operationFactory, BoundNode reference, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _reference = reference;
        }

        protected override IOperation CreateReference()
        {
            return _operationFactory.Create(_reference);
        }
    }

    internal sealed class CSharpLazyNameOfOperation : LazyNameOfOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _argument;

        internal CSharpLazyNameOfOperation(CSharpOperationFactory operationFactory, BoundNode argument, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _argument = argument;
        }

        protected override IOperation CreateArgument()
        {
            return _operationFactory.Create(_argument);
        }
    }

    internal sealed class CSharpLazyThrowOperation : LazyThrowOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _exception;

        internal CSharpLazyThrowOperation(CSharpOperationFactory operationFactory, BoundNode exception, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _exception = exception;
        }

        protected override IOperation CreateException()
        {
            return _operationFactory.Create(_exception);
        }
    }

    internal sealed class CSharpLazyArgumentOperation : LazyArgumentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyArgumentOperation(CSharpOperationFactory operationFactory, BoundNode value, ArgumentKind argumentKind, IConvertibleConversion inConversionOpt, IConvertibleConversion outConversionOpt, IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit) :
			base(argumentKind, inConversionOpt, outConversionOpt, parameter, semanticModel, syntax, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazyArrayCreationOperation : LazyArrayCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _dimensionSizes;
        private readonly BoundNode _initializer;

        internal CSharpLazyArrayCreationOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> dimensionSizes, BoundNode initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _dimensionSizes = dimensionSizes;
            _initializer = initializer;
        }

        protected override ImmutableArray<IOperation> CreateDimensionSizes()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_dimensionSizes);
        }

        protected override IArrayInitializerOperation CreateInitializer()
        {
            return (IArrayInitializerOperation)_operationFactory.Create(_initializer);
        }
    }

    internal sealed class CSharpLazyArrayElementReferenceOperation : LazyArrayElementReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _arrayReference;
        private readonly ImmutableArray<BoundExpression> _indices;

        internal CSharpLazyArrayElementReferenceOperation(CSharpOperationFactory operationFactory, BoundNode arrayReference, ImmutableArray<BoundExpression> indices, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _arrayReference = arrayReference;
            _indices = indices;
        }

        protected override IOperation CreateArrayReference()
        {
            return _operationFactory.Create(_arrayReference);
        }

        protected override ImmutableArray<IOperation> CreateIndices()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_indices);
        }
    }

    internal sealed class CSharpLazyArrayInitializerOperation : LazyArrayInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _elementValues;

        internal CSharpLazyArrayInitializerOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> elementValues, SemanticModel semanticModel, SyntaxNode syntax, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _elementValues = elementValues;
        }

        protected override ImmutableArray<IOperation> CreateElementValues()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_elementValues);
        }
    }

    internal sealed class CSharpLazySimpleAssignmentOperation : LazySimpleAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _target;
        private readonly BoundNode _value;

        internal CSharpLazySimpleAssignmentOperation(CSharpOperationFactory operationFactory, BoundNode target, BoundNode value, bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _target = target;
            _value = value;
        }

        protected override IOperation CreateTarget()
        {
            return _operationFactory.Create(_target);
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazyDeconstructionAssignmentOperation : LazyDeconstructionAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _target;
        private readonly BoundNode _value;

        internal CSharpLazyDeconstructionAssignmentOperation(CSharpOperationFactory operationFactory, BoundNode target, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _target = target;
            _value = value;
        }

        protected override IOperation CreateTarget()
        {
            return _operationFactory.Create(_target);
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazyAwaitOperation : LazyAwaitOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operation;

        internal CSharpLazyAwaitOperation(CSharpOperationFactory operationFactory, BoundNode operation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
        }

        protected override IOperation CreateOperation()
        {
            return _operationFactory.Create(_operation);
        }
    }

    internal sealed class CSharpLazyBinaryOperation : LazyBinaryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _leftOperand;
        private readonly BoundNode _rightOperand;

        internal CSharpLazyBinaryOperation(CSharpOperationFactory operationFactory, BoundNode leftOperand, BoundNode rightOperand, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, bool isCompareText, IMethodSymbol operatorMethod, IMethodSymbol unaryOperatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(operatorKind, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _leftOperand = leftOperand;
            _rightOperand = rightOperand;
        }

        protected override IOperation CreateLeftOperand()
        {
            return _operationFactory.Create(_leftOperand);
        }

        protected override IOperation CreateRightOperand()
        {
            return _operationFactory.Create(_rightOperand);
        }
    }

    internal sealed class CSharpLazyTupleBinaryOperation : LazyTupleBinaryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _leftOperand;
        private readonly BoundNode _rightOperand;

        internal CSharpLazyTupleBinaryOperation(CSharpOperationFactory operationFactory, BoundNode leftOperand, BoundNode rightOperand, BinaryOperatorKind operatorKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(operatorKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _leftOperand = leftOperand;
            _rightOperand = rightOperand;
        }

        protected override IOperation CreateLeftOperand()
        {
            return _operationFactory.Create(_leftOperand);
        }

        protected override IOperation CreateRightOperand()
        {
            return _operationFactory.Create(_rightOperand);
        }
    }

    internal sealed class CSharpLazyBlockOperation : LazyBlockOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundStatement> _operations;

        internal CSharpLazyBlockOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundStatement> operations, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operations = operations;
        }

        protected override ImmutableArray<IOperation> CreateOperations()
        {
            return _operationFactory.CreateFromArray<BoundStatement, IOperation>(_operations);
        }
    }

    internal sealed class CSharpLazyCatchClauseOperation : LazyCatchClauseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundLocal _exceptionDeclarationOrExpression;
        private readonly BoundNode _filter;
        private readonly BoundNode _handler;

        internal CSharpLazyCatchClauseOperation(CSharpOperationFactory operationFactory, BoundLocal exceptionDeclarationOrExpression, BoundNode filter, BoundNode handler, ITypeSymbol exceptionType, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(exceptionType, locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _exceptionDeclarationOrExpression = exceptionDeclarationOrExpression;
            _filter = filter;
            _handler = handler;
        }

        protected override IOperation CreateExceptionDeclarationOrExpression()
        {
            return _operationFactory.CreateVariableDeclarator(_exceptionDeclarationOrExpression);
        }

        protected override IOperation CreateFilter()
        {
            return _operationFactory.Create(_filter);
        }

        protected override IBlockOperation CreateHandler()
        {
            return (IBlockOperation)_operationFactory.Create(_handler);
        }
    }

    internal sealed class CSharpLazyCompoundAssignmentOperation : LazyCompoundAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _target;
        private readonly BoundNode _value;

        internal CSharpLazyCompoundAssignmentOperation(CSharpOperationFactory operationFactory, BoundNode target, BoundNode value, IConvertibleConversion inConversionConvertible, IConvertibleConversion outConversionConvertible, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(inConversionConvertible, outConversionConvertible, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _target = target;
            _value = value;
        }

        protected override IOperation CreateTarget()
        {
            return _operationFactory.Create(_target);
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazyConditionalAccessOperation : LazyConditionalAccessOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operation;
        private readonly BoundNode _whenNotNull;

        internal CSharpLazyConditionalAccessOperation(CSharpOperationFactory operationFactory, BoundNode operation, BoundNode whenNotNull, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
            _whenNotNull = whenNotNull;
        }

        protected override IOperation CreateOperation()
        {
            return _operationFactory.Create(_operation);
        }

        protected override IOperation CreateWhenNotNull()
        {
            return _operationFactory.Create(_whenNotNull);
        }
    }

    internal sealed class CSharpLazyConditionalOperation : LazyConditionalOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _condition;
        private readonly BoundNode _whenTrue;
        private readonly BoundNode _whenFalse;

        internal CSharpLazyConditionalOperation(CSharpOperationFactory operationFactory, BoundNode condition, BoundNode whenTrue, BoundNode whenFalse, bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _condition = condition;
            _whenTrue = whenTrue;
            _whenFalse = whenFalse;
        }

        protected override IOperation CreateCondition()
        {
            return _operationFactory.Create(_condition);
        }

        protected override IOperation CreateWhenTrue()
        {
            return _operationFactory.Create(_whenTrue);
        }

        protected override IOperation CreateWhenFalse()
        {
            return _operationFactory.Create(_whenFalse);
        }
    }

    internal sealed class CSharpLazyConversionOperation : LazyConversionOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operand;

        internal CSharpLazyConversionOperation(CSharpOperationFactory operationFactory, BoundNode operand, IConvertibleConversion convertibleConversion, bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(convertibleConversion, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operand = operand;
        }

        protected override IOperation CreateOperand()
        {
            return _operationFactory.Create(_operand);
        }
    }

    internal sealed class CSharpLazyEventAssignmentOperation : LazyEventAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundEventAssignmentOperator _eventAssignmentOperator;

        internal CSharpLazyEventAssignmentOperation(CSharpOperationFactory operationFactory, BoundEventAssignmentOperator eventAssignmentOperator, bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(adds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _eventAssignmentOperator = eventAssignmentOperator;
        }

        protected override IOperation CreateEventReference()
        {
            return _operationFactory.CreateBoundEventAccessOperation(_eventAssignmentOperator);
        }

        protected override IOperation CreateHandlerValue()
        {
            return _operationFactory.Create(_eventAssignmentOperator.Argument);
        }
    }

    internal sealed class CSharpLazyEventReferenceOperation : LazyEventReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _instance;

        internal CSharpLazyEventReferenceOperation(CSharpOperationFactory operationFactory, BoundNode instance, IEventSymbol @event, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(@event, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
        }

        protected override IOperation CreateInstance()
        {
            return _operationFactory.CreateReceiverOperation(_instance, Event);
        }
    }

    internal sealed class CSharpLazyExpressionStatementOperation : LazyExpressionStatementOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operation;

        internal CSharpLazyExpressionStatementOperation(CSharpOperationFactory operationFactory, BoundNode operation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
        }

        protected override IOperation CreateOperation()
        {
            return _operationFactory.Create(_operation);
        }
    }

    internal sealed class CSharpLazyVariableInitializerOperation : LazyVariableInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyVariableInitializerOperation(CSharpOperationFactory operationFactory, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazyFieldInitializerOperation : LazyFieldInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyFieldInitializerOperation(CSharpOperationFactory operationFactory, BoundNode value, ImmutableArray<ILocalSymbol> locals, ImmutableArray<IFieldSymbol> initializedFields, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(locals, initializedFields, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazyFieldReferenceOperation : LazyFieldReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _instance;

        internal CSharpLazyFieldReferenceOperation(CSharpOperationFactory operationFactory, BoundNode instance, IFieldSymbol field, bool isDeclaration, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(field, isDeclaration, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
        }

        protected override IOperation CreateInstance()
        {
            return _operationFactory.CreateReceiverOperation(_instance, Field);
        }
    }

    internal sealed class CSharpLazyFixedOperation : LazyFixedOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _variables;
        private readonly BoundNode _body;

        internal CSharpLazyFixedOperation(CSharpOperationFactory operationFactory, BoundNode variables, BoundNode body, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _variables = variables;
            _body = body;
        }

        protected override IVariableDeclarationGroupOperation CreateVariables()
        {
            return (IVariableDeclarationGroupOperation)_operationFactory.Create(_variables);
        }

        protected override IOperation CreateBody()
        {
            return _operationFactory.Create(_body);
        }
    }

    internal sealed class CSharpLazyForEachLoopOperation : LazyForEachLoopOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundForEachStatement _forEachStatement;

        internal CSharpLazyForEachLoopOperation(CSharpOperationFactory operationFactory, BoundForEachStatement forEachStatement, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _forEachStatement = forEachStatement;
        }

        protected override IOperation CreateLoopControlVariable()
        {
            return _operationFactory.CreateBoundForEachStatementLoopControlVariable(_forEachStatement);
        }

        protected override IOperation CreateCollection()
        {
            return _operationFactory.Create(_forEachStatement.Expression);
        }

        protected override ImmutableArray<IOperation> CreateNextVariables()
        {
            return ImmutableArray<IOperation>.Empty;
        }

        protected override IOperation CreateBody()
        {
            return _operationFactory.Create(_forEachStatement.Body);
        }

        protected override ForEachLoopOperationInfo CreateLoopInfo()
        {
            return _operationFactory.GetForEachLoopOperatorInfo(_forEachStatement);
        }
    }

    internal sealed class CSharpLazyForLoopOperation : LazyForLoopOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundStatement> _before;
        private readonly BoundNode _condition;
        private readonly ImmutableArray<BoundStatement> _atLoopBottom;
        private readonly BoundNode _body;

        internal CSharpLazyForLoopOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundStatement> before, BoundNode condition, ImmutableArray<BoundStatement> atLoopBottom, BoundNode body, ImmutableArray<ILocalSymbol> locals, ImmutableArray<ILocalSymbol> conditionLocals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(locals, conditionLocals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _before = before;
            _condition = condition;
            _atLoopBottom = atLoopBottom;
            _body = body;
        }

        protected override ImmutableArray<IOperation> CreateBefore()
        {
            return _operationFactory.CreateFromArray<BoundStatement, IOperation>(_before);
        }

        protected override IOperation CreateCondition()
        {
            return _operationFactory.Create(_condition);
        }

        protected override ImmutableArray<IOperation> CreateAtLoopBottom()
        {
            return _operationFactory.CreateFromArray<BoundStatement, IOperation>(_atLoopBottom);
        }

        protected override IOperation CreateBody()
        {
            return _operationFactory.Create(_body);
        }
    }

    internal sealed class CSharpLazyIncrementOrDecrementOperation : LazyIncrementOrDecrementOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _target;

        internal CSharpLazyIncrementOrDecrementOperation(CSharpOperationFactory operationFactory, BoundNode target, bool isDecrement, bool isPostfix, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(isDecrement, isPostfix, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _target = target;
        }

        protected override IOperation CreateTarget()
        {
            return _operationFactory.Create(_target);
        }
    }

    internal sealed class CSharpLazyInterpolatedStringOperation : LazyInterpolatedStringOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _parts;

        internal CSharpLazyInterpolatedStringOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> parts, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _parts = parts;
        }

        protected override ImmutableArray<IInterpolatedStringContentOperation> CreateParts()
        {
            return _operationFactory.CreateBoundInterpolatedStringContentOperation(_parts);
        }
    }

    internal sealed class CSharpLazyInterpolatedStringTextOperation : LazyInterpolatedStringTextOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundLiteral _text;

        internal CSharpLazyInterpolatedStringTextOperation(CSharpOperationFactory operationFactory, BoundLiteral text, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _text = text;
        }

        protected override IOperation CreateText()
        {
            return _operationFactory.CreateBoundLiteralOperation(_text, @implicit: true);
        }
    }

    internal sealed class CSharpLazyInterpolationOperation : LazyInterpolationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _expression;
        private readonly BoundNode _alignment;
        private readonly BoundNode _formatString;

        internal CSharpLazyInterpolationOperation(CSharpOperationFactory operationFactory, BoundNode expression, BoundNode alignment, BoundNode formatString, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _expression = expression;
            _alignment = alignment;
            _formatString = formatString;
        }

        protected override IOperation CreateExpression()
        {
            return _operationFactory.Create(_expression);
        }

        protected override IOperation CreateAlignment()
        {
            return _operationFactory.Create(_alignment);
        }

        protected override IOperation CreateFormatString()
        {
            return _operationFactory.Create(_formatString);
        }
    }

    internal sealed class CSharpLazyInvalidOperation : LazyInvalidOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundNode> _children;

        internal CSharpLazyInvalidOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundNode> children, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _children = children;
        }

        protected override ImmutableArray<IOperation> CreateChildren()
        {
            return _operationFactory.CreateFromArray<BoundNode, IOperation>(_children);
        }
    }

    internal sealed class CSharpLazyInvocationOperation : LazyInvocationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundExpression _instance;
        private readonly BoundNode _boundCall;

        internal CSharpLazyInvocationOperation(CSharpOperationFactory operationFactory, BoundExpression instance, BoundNode boundNode, IMethodSymbol targetMethod, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
            _boundCall = boundNode;
        }

        protected override IOperation CreateInstance()
        {
            return _operationFactory.CreateReceiverOperation(_instance, TargetMethod);
        }

        protected override ImmutableArray<IArgumentOperation> CreateArguments()
        {
            return _operationFactory.DeriveArguments(_boundCall);
        }
    }

    internal sealed class CSharpLazyIsTypeOperation : LazyIsTypeOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _valueOperand;

        internal CSharpLazyIsTypeOperation(CSharpOperationFactory operationFactory, BoundNode valueOperand, ITypeSymbol isType, bool isNotTypeExpression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(isType, isNotTypeExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _valueOperand = valueOperand;
        }

        protected override IOperation CreateValueOperand()
        {
            return _operationFactory.Create(_valueOperand);
        }
    }

    internal sealed class CSharpLazyLabeledOperation : LazyLabeledOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operation;

        internal CSharpLazyLabeledOperation(CSharpOperationFactory operationFactory, BoundNode operation, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
        }

        protected override IOperation CreateOperation()
        {
            return _operationFactory.Create(_operation);
        }
    }

    internal sealed class CSharpLazyAnonymousFunctionOperation : LazyAnonymousFunctionOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _body;

        internal CSharpLazyAnonymousFunctionOperation(CSharpOperationFactory operationFactory, BoundNode body, IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _body = body;
        }

        protected override IBlockOperation CreateBody()
        {
            return (IBlockOperation)_operationFactory.Create(_body);
        }
    }

    internal sealed class CSharpLazyDelegateCreationOperation : LazyDelegateCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _delegateNode;

        internal CSharpLazyDelegateCreationOperation(CSharpOperationFactory operationFactory, BoundNode delegateNode, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _delegateNode = delegateNode;
        }

        protected override IOperation CreateTarget()
        {
            return _operationFactory.CreateDelegateTargetOperation(_delegateNode);
        }
    }

    internal sealed class CSharpLazyDynamicMemberReferenceOperation : LazyDynamicMemberReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _instance;

        internal CSharpLazyDynamicMemberReferenceOperation(CSharpOperationFactory operationFactory, BoundNode instance, string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol containingType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
        }

        protected override IOperation CreateInstance()
        {
            return _operationFactory.Create(_instance);
        }
    }

    internal sealed class CSharpLazyLockOperation : LazyLockOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _lockedValue;
        private readonly BoundNode _body;

        internal CSharpLazyLockOperation(CSharpOperationFactory operationFactory, BoundNode lockedValue, BoundNode body, ILocalSymbol lockTakenSymbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(lockTakenSymbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _lockedValue = lockedValue;
            _body = body;
        }

        protected override IOperation CreateLockedValue()
        {
            return _operationFactory.Create(_lockedValue);
        }

        protected override IOperation CreateBody()
        {
            return _operationFactory.Create(_body);
        }
    }

    internal sealed class CSharpLazyMethodReferenceOperation : LazyMethodReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _instance;

        internal CSharpLazyMethodReferenceOperation(CSharpOperationFactory operationFactory, BoundNode instance, IMethodSymbol method, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(method, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
        }

        protected override IOperation CreateInstance()
        {
            return _operationFactory.CreateReceiverOperation(_instance, Method);
        }
    }

    internal sealed class CSharpLazyCoalesceOperation : LazyCoalesceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;
        private readonly BoundNode _whenNull;

        internal CSharpLazyCoalesceOperation(CSharpOperationFactory operationFactory, BoundNode value, BoundNode whenNull, IConvertibleConversion convertibleValueConversion, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(convertibleValueConversion, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
            _whenNull = whenNull;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }

        protected override IOperation CreateWhenNull()
        {
            return _operationFactory.Create(_whenNull);
        }
    }

    internal sealed class CSharpLazyCoalesceAssignmentOperation : LazyCoalesceAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _target;
        private readonly BoundNode _value;

        internal CSharpLazyCoalesceAssignmentOperation(CSharpOperationFactory operationFactory, BoundNode target, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _target = target;
            _value = value;
        }

        protected override IOperation CreateTarget()
        {
            return _operationFactory.Create(_target);
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazyObjectCreationOperation : LazyObjectCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundObjectCreationExpression _objectCreation;

        internal CSharpLazyObjectCreationOperation(CSharpOperationFactory operationFactory, BoundObjectCreationExpression objectCreation, IMethodSymbol constructor, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(constructor, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _objectCreation = objectCreation;
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)_operationFactory.Create(_objectCreation.InitializerExpressionOpt);
        }

        protected override ImmutableArray<IArgumentOperation> CreateArguments()
        {
            return _operationFactory.DeriveArguments(_objectCreation);
        }
    }

    internal sealed class CSharpLazyAnonymousObjectCreationOperation : LazyAnonymousObjectCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _arguments;
        private readonly ImmutableArray<BoundAnonymousPropertyDeclaration> _declarations;

        internal CSharpLazyAnonymousObjectCreationOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> arguments, ImmutableArray<BoundAnonymousPropertyDeclaration> declarations, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _arguments = arguments;
            _declarations = declarations;
        }

        protected override ImmutableArray<IOperation> CreateInitializers()
        {
            return _operationFactory.GetAnonymousObjectCreationInitializers(_arguments, _declarations, Syntax, Type, IsImplicit);
        }
    }

    internal sealed class CSharpLazyParameterInitializerOperation : LazyParameterInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyParameterInitializerOperation(CSharpOperationFactory operationFactory, BoundNode value, ImmutableArray<ILocalSymbol> locals, IParameterSymbol parameter, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(locals, parameter, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazyPropertyInitializerOperation : LazyPropertyInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyPropertyInitializerOperation(CSharpOperationFactory operationFactory, BoundNode value, ImmutableArray<ILocalSymbol> locals, ImmutableArray<IPropertySymbol> initializedProperties, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(locals, initializedProperties, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazyPropertyReferenceOperation : LazyPropertyReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _propertyReference;
        private readonly bool _isObjectOrCollectionInitializer;

        internal CSharpLazyPropertyReferenceOperation(CSharpOperationFactory operationFactory, BoundNode propertyReference, bool isObjectOrCollectionInitializer, IPropertySymbol property, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(property, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _propertyReference = propertyReference;
            _isObjectOrCollectionInitializer = isObjectOrCollectionInitializer;
        }

        protected override IOperation CreateInstance()
        {
            return _operationFactory.CreateBoundPropertyReferenceInstance(_propertyReference);
        }

        protected override ImmutableArray<IArgumentOperation> CreateArguments()
        {
            return _propertyReference is null || _propertyReference.Kind == BoundKind.PropertyAccess ? ImmutableArray<IArgumentOperation>.Empty : _operationFactory.DeriveArguments(_propertyReference, _isObjectOrCollectionInitializer);
        }
    }

    internal sealed class CSharpLazyReturnOperation : LazyReturnOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _returnedValue;

        internal CSharpLazyReturnOperation(CSharpOperationFactory operationFactory, BoundNode returnedValue, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _returnedValue = returnedValue;
        }

        protected override IOperation CreateReturnedValue()
        {
            return _operationFactory.Create(_returnedValue);
        }
    }

    internal sealed class CSharpLazySingleValueCaseClauseOperation : LazySingleValueCaseClauseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazySingleValueCaseClauseOperation(CSharpOperationFactory operationFactory, BoundNode value, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazySwitchCaseOperation : LazySwitchCaseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundNode> _clauses;
        private readonly BoundNode _condition;
        private readonly ImmutableArray<BoundStatement> _body;

        internal CSharpLazySwitchCaseOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundNode> clauses, BoundNode condition, ImmutableArray<BoundStatement> body, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _clauses = clauses;
            _condition = condition;
            _body = body;
        }

        protected override ImmutableArray<ICaseClauseOperation> CreateClauses()
        {
            return _operationFactory.CreateFromArray<BoundNode, ICaseClauseOperation>(_clauses);
        }

        protected override IOperation CreateCondition()
        {
            return _operationFactory.Create(_condition);
        }

        protected override ImmutableArray<IOperation> CreateBody()
        {
            return _operationFactory.CreateFromArray<BoundStatement, IOperation>(_body);
        }
    }

    internal sealed class CSharpLazySwitchOperation : LazySwitchOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;
        private readonly ImmutableArray<BoundStatementList> _cases;

        internal CSharpLazySwitchOperation(CSharpOperationFactory operationFactory, BoundNode value, ImmutableArray<BoundStatementList> cases, ImmutableArray<ILocalSymbol> locals, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(locals, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
            _cases = cases;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }

        protected override ImmutableArray<ISwitchCaseOperation> CreateCases()
        {
            return _operationFactory.CreateFromArray<BoundStatementList, ISwitchCaseOperation>(_cases);
        }
    }

    internal sealed class CSharpLazyTryOperation : LazyTryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _body;
        private readonly ImmutableArray<BoundCatchBlock> _catches;
        private readonly BoundNode _finally;

        internal CSharpLazyTryOperation(CSharpOperationFactory operationFactory, BoundNode body, ImmutableArray<BoundCatchBlock> catches, BoundNode @finally, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _body = body;
            _catches = catches;
            _finally = @finally;
        }

        protected override IBlockOperation CreateBody()
        {
            return (IBlockOperation)_operationFactory.Create(_body);
        }

        protected override ImmutableArray<ICatchClauseOperation> CreateCatches()
        {
            return _operationFactory.CreateFromArray<BoundCatchBlock, ICatchClauseOperation>(_catches);
        }

        protected override IBlockOperation CreateFinally()
        {
            return (IBlockOperation)_operationFactory.Create(_finally);
        }
    }

    internal sealed class CSharpLazyTupleOperation : LazyTupleOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _elements;

        internal CSharpLazyTupleOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> elements, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ITypeSymbol naturalType, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, naturalType, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _elements = elements;
        }

        protected override ImmutableArray<IOperation> CreateElements()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_elements);
        }
    }

    internal sealed class CSharpLazyTypeParameterObjectCreationOperation : LazyTypeParameterObjectCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _initializer;

        internal CSharpLazyTypeParameterObjectCreationOperation(CSharpOperationFactory operationFactory, BoundNode initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _initializer = initializer;
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)_operationFactory.Create(_initializer);
        }
    }

    internal sealed class CSharpLazyDynamicObjectCreationOperation : LazyDynamicObjectCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _arguments;
        private readonly BoundNode _initializer;

        internal CSharpLazyDynamicObjectCreationOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> arguments, BoundNode initializer, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _arguments = arguments;
            _initializer = initializer;
        }

        protected override ImmutableArray<IOperation> CreateArguments()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_arguments);
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)_operationFactory.Create(_initializer);
        }
    }

    internal sealed class CSharpLazyDynamicInvocationOperation : LazyDynamicInvocationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operation;
        private readonly ImmutableArray<BoundExpression> _arguments;

        internal CSharpLazyDynamicInvocationOperation(CSharpOperationFactory operationFactory, BoundNode operation, ImmutableArray<BoundExpression> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
            _arguments = arguments;
        }

        protected override IOperation CreateOperation()
        {
            return _operationFactory.CreateBoundDynamicInvocationExpressionReceiver(_operation);
        }

        protected override ImmutableArray<IOperation> CreateArguments()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_arguments);
        }
    }

    internal sealed class CSharpLazyDynamicIndexerAccessOperation : LazyDynamicIndexerAccessOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundExpression _indexer;

        internal CSharpLazyDynamicIndexerAccessOperation(CSharpOperationFactory operationFactory, BoundExpression indexer, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _indexer = indexer;
        }

        protected override IOperation CreateOperation()
        {
            return _operationFactory.CreateBoundDynamicIndexerAccessExpressionReceiver(_indexer);
        }

        protected override ImmutableArray<IOperation> CreateArguments()
        {
            return _operationFactory.CreateBoundDynamicIndexerAccessArguments(_indexer);
        }
    }

    internal sealed class CSharpLazyUnaryOperation : LazyUnaryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operand;

        internal CSharpLazyUnaryOperation(CSharpOperationFactory operationFactory, BoundNode operand, UnaryOperatorKind unaryOperationKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(unaryOperationKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operand = operand;
        }

        protected override IOperation CreateOperand()
        {
            return _operationFactory.Create(_operand);
        }
    }

    internal sealed class CSharpLazyUsingOperation : LazyUsingOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _resources;
        private readonly BoundNode _body;

        internal CSharpLazyUsingOperation(CSharpOperationFactory operationFactory, BoundNode resources, BoundNode body, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _resources = resources;
            _body = body;
        }

        protected override IOperation CreateResources()
        {
            return _operationFactory.Create(_resources);
        }

        protected override IOperation CreateBody()
        {
            return _operationFactory.Create(_body);
        }
    }

    internal sealed class CSharpLazyVariableDeclaratorOperation : LazyVariableDeclaratorOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundLocalDeclaration _localDeclaration;

        internal CSharpLazyVariableDeclaratorOperation(CSharpOperationFactory operationFactory, BoundLocalDeclaration localDeclaration, ILocalSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _localDeclaration = localDeclaration;
        }

        protected override IVariableInitializerOperation CreateInitializer()
        {
            return _operationFactory.CreateVariableDeclaratorInitializer(_localDeclaration, Syntax);
        }

        protected override ImmutableArray<IOperation> CreateIgnoredArguments()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_localDeclaration.ArgumentsOpt);
        }
    }

    internal sealed class CSharpLazyVariableDeclarationOperation : LazyVariableDeclarationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _localDeclaration;
        private readonly BoundNode _initializer;

        internal CSharpLazyVariableDeclarationOperation(CSharpOperationFactory operationFactory, BoundNode localDeclaration, BoundNode initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _localDeclaration = localDeclaration;
            _initializer = initializer;
        }

        protected override ImmutableArray<IVariableDeclaratorOperation> CreateDeclarators()
        {
            return _operationFactory.CreateVariableDeclarator(_localDeclaration, Syntax);
        }

        protected override IVariableInitializerOperation CreateInitializer()
        {
            return (IVariableInitializerOperation)_operationFactory.Create(_initializer);
        }
    }

    internal sealed class CSharpLazyWhileLoopOperation : LazyWhileLoopOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _condition;
        private readonly BoundNode _body;
        private readonly BoundNode _ignoredCondition;

        internal CSharpLazyWhileLoopOperation(CSharpOperationFactory operationFactory, BoundNode condition, BoundNode body, BoundNode ignoredCondition, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, bool conditionIsTop, bool conditionIsUntil, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(locals, continueLabel, exitLabel, conditionIsTop, conditionIsUntil, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _condition = condition;
            _body = body;
            _ignoredCondition = ignoredCondition;
        }

        protected override IOperation CreateCondition()
        {
            return _operationFactory.Create(_condition);
        }

        protected override IOperation CreateBody()
        {
            return _operationFactory.Create(_body);
        }

        protected override IOperation CreateIgnoredCondition()
        {
            return _operationFactory.Create(_ignoredCondition);
        }
    }

    internal sealed class CSharpLazyLocalFunctionOperation : LazyLocalFunctionOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _body;
        private readonly BoundNode _ignoredBody;

        internal CSharpLazyLocalFunctionOperation(CSharpOperationFactory operationFactory, BoundNode body, BoundNode ignoredBody, IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _body = body;
            _ignoredBody = ignoredBody;
        }

        protected override IBlockOperation CreateBody()
        {
            return (IBlockOperation)_operationFactory.Create(_body);
        }

        protected override IBlockOperation CreateIgnoredBody()
        {
            return (IBlockOperation)_operationFactory.Create(_ignoredBody);
        }
    }

    internal sealed class CSharpLazyConstantPatternOperation : LazyConstantPatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyConstantPatternOperation(CSharpOperationFactory operationFactory, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazyPatternCaseClauseOperation : LazyPatternCaseClauseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _pattern;
        private readonly BoundNode _guard;

        internal CSharpLazyPatternCaseClauseOperation(CSharpOperationFactory operationFactory, BoundNode pattern, BoundNode guard, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _pattern = pattern;
            _guard = guard;
        }

        protected override IPatternOperation CreatePattern()
        {
            return (IPatternOperation)_operationFactory.Create(_pattern);
        }

        protected override IOperation CreateGuard()
        {
            return _operationFactory.Create(_guard);
        }
    }

    internal sealed class CSharpLazyIsPatternOperation : LazyIsPatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;
        private readonly BoundNode _pattern;

        internal CSharpLazyIsPatternOperation(CSharpOperationFactory operationFactory, BoundNode value, BoundNode pattern, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
            _pattern = pattern;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }

        protected override IPatternOperation CreatePattern()
        {
            return (IPatternOperation)_operationFactory.Create(_pattern);
        }
    }

    internal sealed class CSharpLazyObjectOrCollectionInitializerOperation : LazyObjectOrCollectionInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ImmutableArray<BoundExpression> _initializers;

        internal CSharpLazyObjectOrCollectionInitializerOperation(CSharpOperationFactory operationFactory, ImmutableArray<BoundExpression> initializers, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _initializers = initializers;
        }

        protected override ImmutableArray<IOperation> CreateInitializers()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_initializers);
        }
    }

    internal sealed class CSharpLazyMemberInitializerOperation : LazyMemberInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _initializedMember;
        private readonly BoundNode _initializer;

        internal CSharpLazyMemberInitializerOperation(CSharpOperationFactory operationFactory, BoundNode initializedMember, BoundNode initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _initializedMember = initializedMember;
            _initializer = initializer;
        }

        protected override IOperation CreateInitializedMember()
        {
            return _operationFactory.CreateMemberInitializerInitializedMember(_initializedMember);
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)_operationFactory.Create(_initializer);
        }
    }

    internal sealed class CSharpLazyTranslatedQueryOperation : LazyTranslatedQueryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operation;

        internal CSharpLazyTranslatedQueryOperation(CSharpOperationFactory operationFactory, BoundNode operation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _operation = operation;
        }

        protected override IOperation CreateOperation()
        {
            return _operationFactory.Create(_operation);
        }
    }

    internal sealed class CSharpLazyMethodBodyOperation : LazyMethodBodyOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _blockBody;
        private readonly BoundNode _expressionBody;

        internal CSharpLazyMethodBodyOperation(CSharpOperationFactory operationFactory, BoundNode blockBody, BoundNode expressionBody, SemanticModel semanticModel, SyntaxNode syntax) :
			base(semanticModel, syntax)
        {
            _operationFactory = operationFactory;
            _blockBody = blockBody;
            _expressionBody = expressionBody;
        }

        protected override IBlockOperation CreateBlockBody()
        {
            return (IBlockOperation)_operationFactory.Create(_blockBody);
        }

        protected override IBlockOperation CreateExpressionBody()
        {
            return (IBlockOperation)_operationFactory.Create(_expressionBody);
        }
    }

    internal sealed class CSharpLazyConstructorBodyOperation : LazyConstructorBodyOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _initializer;
        private readonly BoundNode _blockBody;
        private readonly BoundNode _expressionBody;

        internal CSharpLazyConstructorBodyOperation(CSharpOperationFactory operationFactory, BoundNode initializer, BoundNode blockBody, BoundNode expressionBody, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax) :
			base(locals, semanticModel, syntax)
        {
            _operationFactory = operationFactory;
            _initializer = initializer;
            _blockBody = blockBody;
            _expressionBody = expressionBody;
        }

        protected override IOperation CreateInitializer()
        {
            return _operationFactory.Create(_initializer);
        }

        protected override IBlockOperation CreateBlockBody()
        {
            return (IBlockOperation)_operationFactory.Create(_blockBody);
        }

        protected override IBlockOperation CreateExpressionBody()
        {
            return (IBlockOperation)_operationFactory.Create(_expressionBody);
        }
    }

    internal sealed class CSharpLazyNoPiaObjectCreationOperation : LazyNoPiaObjectCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _initializer;

        internal CSharpLazyNoPiaObjectCreationOperation(CSharpOperationFactory operationFactory, BoundNode initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
			base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _initializer = initializer;
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)_operationFactory.Create(_initializer);
        }
    }

    internal sealed class CSharpLazyFromEndIndexOperation : LazyFromEndIndexOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _operand;

        internal CSharpLazyFromEndIndexOperation(CSharpOperationFactory operationFactory, BoundNode operand, bool isLifted, bool isImplicit, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, IMethodSymbol symbol) :
			base(isLifted, isImplicit, semanticModel, syntax, type, symbol)
        {
            _operationFactory = operationFactory;
            _operand = operand;
        }

        protected override IOperation CreateOperand()
        {
            return _operationFactory.Create(_operand);
        }
    }

    internal sealed class CSharpLazyRangeOperation : LazyRangeOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _leftOperand;
        private readonly BoundNode _rightOperand;

        internal CSharpLazyRangeOperation(CSharpOperationFactory operationFactory, BoundNode leftOperand, BoundNode rightOperand, bool isLifted, bool isImplicit, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, IMethodSymbol symbol) :
			base(isLifted, isImplicit, semanticModel, syntax, type, symbol)
        {
            _operationFactory = operationFactory;
            _leftOperand = leftOperand;
            _rightOperand = rightOperand;
        }

        protected override IOperation CreateLeftOperand()
        {
            return _operationFactory.Create(_leftOperand);
        }

        protected override IOperation CreateRightOperand()
        {
            return _operationFactory.Create(_rightOperand);
        }
    }
}
