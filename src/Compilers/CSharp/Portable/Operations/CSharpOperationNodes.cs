// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{

    internal sealed class CSharpLazyNoneOperation : LazyNoneOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _boundNode;

        public CSharpLazyNoneOperation(CSharpOperationFactory operationFactory, BoundNode boundNode, SemanticModel semanticModel, SyntaxNode node, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, node, constantValue: constantValue, isImplicit: isImplicit)
        {
            _operationFactory = operationFactory;
            _boundNode = boundNode;
        }

        protected override ImmutableArray<IOperation> GetChildren() => _operationFactory.GetIOperationChildren(_boundNode);
    }


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
        private readonly BoundArrayCreation _arrayCreation;

        internal CSharpLazyArrayCreationOperation(CSharpOperationFactory operationFactory, BoundArrayCreation arrayCreation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _arrayCreation = arrayCreation;
        }

        protected override ImmutableArray<IOperation> CreateDimensionSizes()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_arrayCreation.Bounds);
        }

        protected override IArrayInitializerOperation CreateInitializer()
        {
            return (IArrayInitializerOperation)_operationFactory.Create(_arrayCreation.InitializerOpt);
        }
    }

    internal sealed class CSharpLazyArrayElementReferenceOperation : LazyArrayElementReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundArrayAccess _arrayAccess;

        internal CSharpLazyArrayElementReferenceOperation(CSharpOperationFactory operationFactory, BoundArrayAccess arrayAccess, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _arrayAccess = arrayAccess;
        }

        protected override IOperation CreateArrayReference()
        {
            // The compiler will dedupe the _arrayAccess.Expression between different array references. Some example code:
            //
            // class C
            // {
            //     int[] a;

            //     static void Main()
            //     {
            //         // Compiler dedupes the array access receiver for [0] and [1]
            //         var a = new C { a = { [0] = 1, [1] = 2 } };
            //     }
            // }
            //
            // In order to prevent parent pointer from having an issue with this, we intentionally create a new IOperation node every time
            // we encounter an array access. Since we create from the top down, it should be impossible for us to see the node in
            // boundArrayAccess.Expression before seeing the boundArrayAccess itself, so this should not create any other parent pointer
            // issues.
            return _operationFactory.CreateInternal(_arrayAccess.Expression);
        }

        protected override ImmutableArray<IOperation> CreateIndices()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_arrayAccess.Indices);
        }
    }

    internal sealed class CSharpLazyArrayInitializerOperation : LazyArrayInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundArrayInitialization _arrayInitialization;

        internal CSharpLazyArrayInitializerOperation(CSharpOperationFactory operationFactory, BoundArrayInitialization arrayInitialization, SemanticModel semanticModel, SyntaxNode syntax, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type: null, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _arrayInitialization = arrayInitialization;
        }

        protected override ImmutableArray<IOperation> CreateElementValues()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_arrayInitialization.Initializers);
        }
    }

    internal sealed class CSharpLazySimpleAssignmentOperation : LazySimpleAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundAssignmentOperator _assignmentOperator;

        internal CSharpLazySimpleAssignmentOperation(CSharpOperationFactory operationFactory, BoundAssignmentOperator assignment, bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _assignmentOperator = assignment;
        }

        protected override IOperation CreateTarget()
        {
            return _operationFactory.Create(_assignmentOperator.Left);
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_assignmentOperator.Right);
        }
    }

    internal sealed class CSharpLazyDeconstructionAssignmentOperation : LazyDeconstructionAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundDeconstructionAssignmentOperator _deconstructionAssignment;

        internal CSharpLazyDeconstructionAssignmentOperation(CSharpOperationFactory operationFactory, BoundDeconstructionAssignmentOperator deconstructionAssignment, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _deconstructionAssignment = deconstructionAssignment;
        }

        protected override IOperation CreateTarget()
        {
            return _operationFactory.Create(_deconstructionAssignment.Left);
        }

        protected override IOperation CreateValue()
        {
            // Skip the synthetic deconstruction conversion wrapping the right operand.
            return _operationFactory.Create(_deconstructionAssignment.Right.Operand);
        }
    }

    internal sealed class CSharpLazyDeclarationExpressionOperation : LazyDeclarationExpressionOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundExpression _underlyingReference;

        public CSharpLazyDeclarationExpressionOperation(CSharpOperationFactory operationFactory, BoundExpression underlyingReference, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Debug.Assert(underlyingReference.Kind == BoundKind.Local ||
                         underlyingReference.Kind == BoundKind.FieldAccess ||
                         underlyingReference is BoundTupleExpression);

            _operationFactory = operationFactory;
            _underlyingReference = underlyingReference;
        }

        protected override IOperation CreateExpression()
        {
            SyntaxNode underlyingSyntax = ((DeclarationExpressionSyntax)_underlyingReference.Syntax).Designation;

            switch (_underlyingReference)
            {
                case BoundLocal local:
                    return _operationFactory.CreateBoundLocalOperation(local, createDeclaration: false);
                case BoundTupleLiteral tupleLiteral:
                    return _operationFactory.CreateBoundTupleLiteralOperation(tupleLiteral, createDeclaration: false);
                case BoundConvertedTupleLiteral convertedTupleLiteral:
                    return _operationFactory.CreateBoundConvertedTupleLiteralOperation(convertedTupleLiteral, createDeclaration: false);
                case BoundFieldAccess fieldAccess:
                    return _operationFactory.CreateBoundFieldAccessOperation(fieldAccess, createDeclaration: false);
                default:
                    throw ExceptionUtilities.UnexpectedValue(_underlyingReference.Kind);
            }
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
        private readonly BoundBinaryOperatorBase _binaryOperator;

        internal CSharpLazyBinaryOperation(CSharpOperationFactory operationFactory, BoundBinaryOperatorBase binaryOperator, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, bool isCompareText, IMethodSymbol operatorMethod, IMethodSymbol unaryOperatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(operatorKind, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _binaryOperator = binaryOperator;
        }

        protected override IOperation CreateLeftOperand()
        {
            return _operationFactory.Create(_binaryOperator.Left);
        }

        protected override IOperation CreateRightOperand()
        {
            return _operationFactory.Create(_binaryOperator.Right);
        }
    }

    internal sealed class CSharpLazyTupleBinaryOperation : LazyTupleBinaryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundTupleBinaryOperator _tupleBinaryOperator;

        internal CSharpLazyTupleBinaryOperation(CSharpOperationFactory operationFactory, BoundTupleBinaryOperator tupleBinaryOperator, BinaryOperatorKind operatorKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(operatorKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _tupleBinaryOperator = tupleBinaryOperator;
        }

        protected override IOperation CreateLeftOperand()
        {
            return _operationFactory.Create(_tupleBinaryOperator.Left);
        }

        protected override IOperation CreateRightOperand()
        {
            return _operationFactory.Create(_tupleBinaryOperator.Right);
        }
    }

    internal sealed class CSharpLazyBlockOperation : LazyBlockOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundBlock _block;

        internal CSharpLazyBlockOperation(CSharpOperationFactory operationFactory, BoundBlock block, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _block = block;
        }

        protected override ImmutableArray<IOperation> CreateOperations()
        {
            return _operationFactory.CreateFromArray<BoundStatement, IOperation>(_block.Statements);
        }
    }

    internal sealed class CSharpLazyCatchClauseOperation : LazyCatchClauseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundCatchBlock _catchBlock;

        internal CSharpLazyCatchClauseOperation(CSharpOperationFactory operationFactory, BoundCatchBlock catchBlock, ITypeSymbol exceptionType, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(exceptionType, locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _catchBlock = catchBlock;
        }

        protected override IOperation CreateExceptionDeclarationOrExpression()
        {
            return _operationFactory.CreateVariableDeclarator((BoundLocal)_catchBlock.ExceptionSourceOpt);
        }

        protected override IOperation CreateFilter()
        {
            return _operationFactory.Create(_catchBlock.ExceptionFilterOpt);
        }

        protected override IBlockOperation CreateHandler()
        {
            return (IBlockOperation)_operationFactory.Create(_catchBlock.Body);
        }
    }

    internal sealed class CSharpLazyCompoundAssignmentOperation : LazyCompoundAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundCompoundAssignmentOperator _compoundAssignmentOperator;

        internal CSharpLazyCompoundAssignmentOperation(CSharpOperationFactory operationFactory, BoundCompoundAssignmentOperator compoundAssignmentOperator, IConvertibleConversion inConversionConvertible, IConvertibleConversion outConversionConvertible, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(inConversionConvertible, outConversionConvertible, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _compoundAssignmentOperator = compoundAssignmentOperator;
        }

        protected override IOperation CreateTarget()
        {
            return _operationFactory.Create(_compoundAssignmentOperator.Left);
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_compoundAssignmentOperator.Right);
        }
    }

    internal sealed class CSharpLazyConditionalAccessOperation : LazyConditionalAccessOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundConditionalAccess _conditionalAccess;

        internal CSharpLazyConditionalAccessOperation(CSharpOperationFactory operationFactory, BoundConditionalAccess conditionalAccess, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _conditionalAccess = conditionalAccess;
        }

        protected override IOperation CreateOperation()
        {
            return _operationFactory.Create(_conditionalAccess.Receiver);
        }

        protected override IOperation CreateWhenNotNull()
        {
            return _operationFactory.Create(_conditionalAccess.AccessExpression);
        }
    }

    internal sealed class CSharpLazyConditionalOperation : LazyConditionalOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly IBoundConditional _boundConditional;

        internal CSharpLazyConditionalOperation(CSharpOperationFactory operationFactory, IBoundConditional boundConditional, bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _boundConditional = boundConditional;
        }

        protected override IOperation CreateCondition()
        {
            return _operationFactory.Create(_boundConditional.Condition);
        }

        protected override IOperation CreateWhenTrue()
        {
            return _operationFactory.Create(_boundConditional.Consequence);
        }

        protected override IOperation CreateWhenFalse()
        {
            return _operationFactory.Create(_boundConditional.AlternativeOpt);
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
            base(locals: ImmutableArray<ILocalSymbol>.Empty, semanticModel, syntax, type, constantValue, isImplicit)
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
            base(initializedFields, locals, semanticModel, syntax, type, constantValue, isImplicit)
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
        private readonly BoundFixedStatement _fixedStatement;

        internal CSharpLazyFixedOperation(CSharpOperationFactory operationFactory, BoundFixedStatement fixedStatement, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _fixedStatement = fixedStatement;
        }

        protected override IVariableDeclarationGroupOperation CreateVariables()
        {
            return (IVariableDeclarationGroupOperation)_operationFactory.Create(_fixedStatement.Declarations);
        }

        protected override IOperation CreateBody()
        {
            return _operationFactory.Create(_fixedStatement.Body);
        }
    }

    internal sealed class CSharpLazyForEachLoopOperation : LazyForEachLoopOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundForEachStatement _forEachStatement;

        internal CSharpLazyForEachLoopOperation(CSharpOperationFactory operationFactory, BoundForEachStatement forEachStatement, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(forEachStatement.AwaitOpt != null, LoopKind.ForEach, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
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
        private readonly BoundForStatement _forStatement;

        internal CSharpLazyForLoopOperation(CSharpOperationFactory operationFactory, BoundForStatement forStatement, ImmutableArray<ILocalSymbol> locals, ImmutableArray<ILocalSymbol> conditionLocals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, conditionLocals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _forStatement = forStatement;
        }

        protected override ImmutableArray<IOperation> CreateBefore()
        {
            return _operationFactory.CreateFromArray<BoundStatement, IOperation>(_operationFactory.ToStatements(_forStatement.Initializer));
        }

        protected override IOperation CreateCondition()
        {
            return _operationFactory.Create(_forStatement.Condition);
        }

        protected override ImmutableArray<IOperation> CreateAtLoopBottom()
        {
            return _operationFactory.CreateFromArray<BoundStatement, IOperation>(_operationFactory.ToStatements(_forStatement.Increment));
        }

        protected override IOperation CreateBody()
        {
            return _operationFactory.Create(_forStatement.Body);
        }
    }

    internal sealed class CSharpLazyIncrementOrDecrementOperation : LazyIncrementOrDecrementOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _target;

        internal CSharpLazyIncrementOrDecrementOperation(CSharpOperationFactory operationFactory, BoundNode target, bool isDecrement, bool isPostfix, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(isPostfix, isLifted, isChecked, operatorMethod, isDecrement ? OperationKind.Decrement : OperationKind.Increment, semanticModel, syntax, type, constantValue, isImplicit)
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
        private readonly BoundInterpolatedString _interpolatedString;

        internal CSharpLazyInterpolatedStringOperation(CSharpOperationFactory operationFactory, BoundInterpolatedString interpolatedString, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _interpolatedString = interpolatedString;
        }

        protected override ImmutableArray<IInterpolatedStringContentOperation> CreateParts()
        {
            return _operationFactory.CreateBoundInterpolatedStringContentOperation(_interpolatedString.Parts);
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
        private readonly BoundStringInsert _stringInsert;

        internal CSharpLazyInterpolationOperation(CSharpOperationFactory operationFactory, BoundStringInsert stringInsert, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _stringInsert = stringInsert;
        }

        protected override IOperation CreateExpression()
        {
            return _operationFactory.Create(_stringInsert.Value);
        }

        protected override IOperation CreateAlignment()
        {
            return _operationFactory.Create(_stringInsert.Alignment);
        }

        protected override IOperation CreateFormatString()
        {
            return _operationFactory.Create(_stringInsert.Format);
        }
    }

    internal sealed class CSharpLazyInvalidOperation : LazyInvalidOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly IBoundInvalidNode _node;

        internal CSharpLazyInvalidOperation(CSharpOperationFactory operationFactory, IBoundInvalidNode node, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _node = node;
        }

        protected override ImmutableArray<IOperation> CreateChildren()
        {
            return _operationFactory.CreateFromArray<BoundNode, IOperation>(_node.InvalidNodeChildren);
        }
    }

    internal sealed class CSharpLazyInvocationOperation : LazyInvocationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundExpression _invocableExpression;

        internal CSharpLazyInvocationOperation(CSharpOperationFactory operationFactory, BoundCall invocableExpression, IMethodSymbol targetMethod, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(operationFactory, (BoundExpression)invocableExpression, targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        internal CSharpLazyInvocationOperation(CSharpOperationFactory operationFactory, BoundCollectionElementInitializer invocableExpression, IMethodSymbol targetMethod, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(operationFactory, (BoundExpression)invocableExpression, targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        private CSharpLazyInvocationOperation(CSharpOperationFactory operationFactory, BoundExpression invocableExpression, IMethodSymbol targetMethod, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _invocableExpression = invocableExpression;
        }

        protected override IOperation CreateInstance()
        {
            BoundExpression receiver;
            switch (_invocableExpression)
            {
                case BoundCall call:
                    receiver = call.ReceiverOpt;
                    break;
                case BoundCollectionElementInitializer initializer:
                    receiver = initializer.ImplicitReceiverOpt;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(_invocableExpression.Kind);
            }
            return _operationFactory.CreateReceiverOperation(receiver, TargetMethod);
        }

        protected override ImmutableArray<IArgumentOperation> CreateArguments()
        {
            return _operationFactory.DeriveArguments(_invocableExpression);
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
        private readonly BoundLockStatement _lockStatement;

        internal CSharpLazyLockOperation(CSharpOperationFactory operationFactory, BoundLockStatement lockStatement, ILocalSymbol lockTakenSymbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(lockTakenSymbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _lockStatement = lockStatement;
        }

        protected override IOperation CreateLockedValue()
        {
            return _operationFactory.Create(_lockStatement.Argument);
        }

        protected override IOperation CreateBody()
        {
            return _operationFactory.Create(_lockStatement.Body);
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
        private readonly BoundNullCoalescingOperator _nullCoalescingOperator;

        internal CSharpLazyCoalesceOperation(CSharpOperationFactory operationFactory, BoundNullCoalescingOperator nullCoalescingOperator, IConvertibleConversion convertibleValueConversion, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(convertibleValueConversion, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _nullCoalescingOperator = nullCoalescingOperator;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_nullCoalescingOperator.LeftOperand);
        }

        protected override IOperation CreateWhenNull()
        {
            return _operationFactory.Create(_nullCoalescingOperator.RightOperand);
        }
    }

    internal sealed class CSharpLazyCoalesceAssignmentOperation : LazyCoalesceAssignmentOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNullCoalescingAssignmentOperator _nullCoalescingAssignmentOperator;

        internal CSharpLazyCoalesceAssignmentOperation(CSharpOperationFactory operationFactory, BoundNullCoalescingAssignmentOperator nullCoalescingAssignmentOperator, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _nullCoalescingAssignmentOperator = nullCoalescingAssignmentOperator;
        }

        protected override IOperation CreateTarget()
        {
            return _operationFactory.Create(_nullCoalescingAssignmentOperator.LeftOperand);
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_nullCoalescingAssignmentOperator.RightOperand);
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
        private readonly BoundExpression _anonymousObjectCreation;

        internal CSharpLazyAnonymousObjectCreationOperation(CSharpOperationFactory operationFactory, BoundAnonymousObjectCreationExpression anonymousObjectCreation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(operationFactory, (BoundExpression)anonymousObjectCreation, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        internal CSharpLazyAnonymousObjectCreationOperation(CSharpOperationFactory operationFactory, BoundObjectCreationExpression anonymousObjectCreation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(operationFactory, (BoundExpression)anonymousObjectCreation, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        private CSharpLazyAnonymousObjectCreationOperation(CSharpOperationFactory operationFactory, BoundExpression anonymousObjectCreation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _anonymousObjectCreation = anonymousObjectCreation;
        }

        protected override ImmutableArray<IOperation> CreateInitializers()
        {
            ImmutableArray<BoundExpression> arguments;
            ImmutableArray<BoundAnonymousPropertyDeclaration> declarations;

            switch (_anonymousObjectCreation)
            {
                case BoundAnonymousObjectCreationExpression anonymousObjectCreationExpression:
                    arguments = anonymousObjectCreationExpression.Arguments;
                    declarations = anonymousObjectCreationExpression.Declarations;
                    break;
                case BoundObjectCreationExpression objectCreationExpression:
                    arguments = objectCreationExpression.Arguments;
                    declarations = ImmutableArray<BoundAnonymousPropertyDeclaration>.Empty;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(_anonymousObjectCreation.Kind);
            }

            return _operationFactory.GetAnonymousObjectCreationInitializers(arguments, declarations, Syntax, Type, IsImplicit);
        }
    }

    internal sealed class CSharpLazyParameterInitializerOperation : LazyParameterInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyParameterInitializerOperation(CSharpOperationFactory operationFactory, BoundNode value, ImmutableArray<ILocalSymbol> locals, IParameterSymbol parameter, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(parameter, locals, semanticModel, syntax, type, constantValue, isImplicit)
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

        internal CSharpLazyPropertyInitializerOperation(CSharpOperationFactory operationFactory, BoundNode value, ImmutableArray<ILocalSymbol> locals, ImmutableArray<IPropertySymbol> initializedProperties, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(initializedProperties, locals, semanticModel, syntax, type, constantValue, isImplicit)
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
            base(CaseKind.SingleValue, label, semanticModel, syntax, type, constantValue, isImplicit)
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
        private readonly IBoundSwitchSection _switchSection;

        internal CSharpLazySwitchCaseOperation(CSharpOperationFactory operationFactory, IBoundSwitchSection switchSection, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _switchSection = switchSection;
        }

        protected override ImmutableArray<ICaseClauseOperation> CreateClauses()
        {
            return _operationFactory.CreateFromArray<BoundNode, ICaseClauseOperation>(_switchSection.SwitchLabels);
        }

        protected override IOperation CreateCondition()
        {
            return null;
        }

        protected override ImmutableArray<IOperation> CreateBody()
        {
            return _operationFactory.CreateFromArray<BoundStatement, IOperation>(_switchSection.Statements);
        }
    }

    internal sealed class CSharpLazySwitchOperation : LazySwitchOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly IBoundSwitchStatement _switchStatement;

        internal CSharpLazySwitchOperation(CSharpOperationFactory operationFactory, IBoundSwitchStatement switchStatement, ImmutableArray<ILocalSymbol> locals, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _switchStatement = switchStatement;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_switchStatement.Value);
        }

        protected override ImmutableArray<ISwitchCaseOperation> CreateCases()
        {
            return _operationFactory.CreateFromArray<BoundStatementList, ISwitchCaseOperation>(_switchStatement.Cases);
        }
    }

    internal sealed class CSharpLazyTryOperation : LazyTryOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundTryStatement _tryStatement;

        internal CSharpLazyTryOperation(CSharpOperationFactory operationFactory, BoundTryStatement tryStatement, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _tryStatement = tryStatement;
        }

        protected override IBlockOperation CreateBody()
        {
            return (IBlockOperation)_operationFactory.Create(_tryStatement.TryBlock);
        }

        protected override ImmutableArray<ICatchClauseOperation> CreateCatches()
        {
            return _operationFactory.CreateFromArray<BoundCatchBlock, ICatchClauseOperation>(_tryStatement.CatchBlocks);
        }

        protected override IBlockOperation CreateFinally()
        {
            return (IBlockOperation)_operationFactory.Create(_tryStatement.FinallyBlockOpt);
        }
    }

    internal sealed class CSharpLazyTupleOperation : LazyTupleOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundTupleExpression _tupleExpression;

        internal CSharpLazyTupleOperation(CSharpOperationFactory operationFactory, BoundTupleExpression tupleExpression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ITypeSymbol naturalType, Optional<object> constantValue, bool isImplicit) :
            base(naturalType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _tupleExpression = tupleExpression;
        }

        protected override ImmutableArray<IOperation> CreateElements()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_tupleExpression.Arguments);
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
        private readonly BoundDynamicObjectCreationExpression _dynamicObjectCreationExpression;

        internal CSharpLazyDynamicObjectCreationOperation(CSharpOperationFactory operationFactory, BoundDynamicObjectCreationExpression dynamicObjectCreationExpression, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _dynamicObjectCreationExpression = dynamicObjectCreationExpression;
        }

        protected override ImmutableArray<IOperation> CreateArguments()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_dynamicObjectCreationExpression.Arguments);
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)_operationFactory.Create(_dynamicObjectCreationExpression.InitializerExpressionOpt);
        }
    }

    internal sealed class CSharpLazyDynamicInvocationOperation : LazyDynamicInvocationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundDynamicInvocableBase _dynamicInvocable;

        internal CSharpLazyDynamicInvocationOperation(CSharpOperationFactory operationFactory, BoundDynamicInvocableBase dynamicInvocable, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _dynamicInvocable = dynamicInvocable;
        }

        protected override IOperation CreateOperation()
        {
            return _operationFactory.CreateBoundDynamicInvocationExpressionReceiver(_dynamicInvocable.Expression);
        }

        protected override ImmutableArray<IOperation> CreateArguments()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_dynamicInvocable.Arguments);
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
        private readonly BoundUsingStatement _usingStatement;

        internal CSharpLazyUsingOperation(CSharpOperationFactory operationFactory, BoundUsingStatement usingStatement, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(locals, usingStatement.AwaitOpt != null, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _usingStatement = usingStatement;
        }

        protected override IOperation CreateResources()
        {
            return _operationFactory.Create((BoundNode)_usingStatement.DeclarationsOpt ?? _usingStatement.ExpressionOpt);
        }

        protected override IOperation CreateBody()
        {
            return _operationFactory.Create(_usingStatement.Body);
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

        internal CSharpLazyVariableDeclarationOperation(CSharpOperationFactory operationFactory, BoundNode localDeclaration, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _localDeclaration = localDeclaration;
        }

        protected override ImmutableArray<IVariableDeclaratorOperation> CreateDeclarators()
        {
            return _operationFactory.CreateVariableDeclarator(_localDeclaration, Syntax);
        }

        protected override IVariableInitializerOperation CreateInitializer()
        {
            return null;
        }

        protected override ImmutableArray<IOperation> CreateIgnoredDimensions()
        {
            return _operationFactory.CreateIgnoredDimensions(_localDeclaration, Syntax);
        }
    }

    internal sealed class CSharpLazyWhileLoopOperation : LazyWhileLoopOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundConditionalLoopStatement _conditionalLoopStatement;

        internal CSharpLazyWhileLoopOperation(CSharpOperationFactory operationFactory, BoundConditionalLoopStatement conditionalLoopStatement, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, bool conditionIsTop, bool conditionIsUntil, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(conditionIsTop, conditionIsUntil, LoopKind.While, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _conditionalLoopStatement = conditionalLoopStatement;
        }

        protected override IOperation CreateCondition()
        {
            return _operationFactory.Create(_conditionalLoopStatement.Condition);
        }

        protected override IOperation CreateBody()
        {
            return _operationFactory.Create(_conditionalLoopStatement.Body);
        }

        protected override IOperation CreateIgnoredCondition()
        {
            return null;
        }
    }

    internal sealed class CSharpLazyLocalFunctionOperation : LazyLocalFunctionOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundLocalFunctionStatement _localFunctionStatement;

        internal CSharpLazyLocalFunctionOperation(CSharpOperationFactory operationFactory, BoundLocalFunctionStatement localFunctionStatement, IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _localFunctionStatement = localFunctionStatement;
        }

        protected override IBlockOperation CreateBody()
        {
            return (IBlockOperation)_operationFactory.Create(_localFunctionStatement.Body);
        }

        protected override IBlockOperation CreateIgnoredBody()
        {
            return _localFunctionStatement is {
                BlockBody: {
                }, ExpressionBody: {
                }
            } ?
                        (IBlockOperation)_operationFactory.Create(_localFunctionStatement.ExpressionBody) :
                        null;
        }
    }

    internal sealed class CSharpLazyConstantPatternOperation : LazyConstantPatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyConstantPatternOperation(ITypeSymbol inputType, CSharpOperationFactory operationFactory, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit) :
            base(inputType, semanticModel, syntax, type: null, constantValue: default, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    /// <summary>
    /// Represents a C# recursive pattern.
    /// </summary>
    internal sealed partial class CSharpLazyRecursivePatternOperation : LazyRecursivePatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundRecursivePattern _boundRecursivePattern;

        public CSharpLazyRecursivePatternOperation(
            CSharpOperationFactory operationFactory,
            BoundRecursivePattern boundRecursivePattern,
            SemanticModel semanticModel)
            : base(inputType: boundRecursivePattern.InputType,
                   matchedType: boundRecursivePattern.DeclaredType?.Type ?? boundRecursivePattern.InputType.StrippedType(),
                   deconstructSymbol: boundRecursivePattern.DeconstructMethod,
                   declaredSymbol: boundRecursivePattern.Variable,
                   semanticModel: semanticModel,
                   syntax: boundRecursivePattern.Syntax,
                   type: null,
                   constantValue: default,
                   isImplicit: boundRecursivePattern.WasCompilerGenerated)
        {
            _operationFactory = operationFactory;
            _boundRecursivePattern = boundRecursivePattern;

        }
        protected override ImmutableArray<IPatternOperation> CreateDeconstructionSubpatterns()
        {
            return _boundRecursivePattern.Deconstruction.IsDefault ? ImmutableArray<IPatternOperation>.Empty :
                _boundRecursivePattern.Deconstruction.SelectAsArray((p, fac) => (IPatternOperation)fac.Create(p.Pattern), _operationFactory);
        }
        protected override ImmutableArray<IPropertySubpatternOperation> CreatePropertySubpatterns()
        {
            return _boundRecursivePattern.Properties.IsDefault ? ImmutableArray<IPropertySubpatternOperation>.Empty :
                _boundRecursivePattern.Properties.SelectAsArray((p, recursivePattern) => recursivePattern._operationFactory.CreatePropertySubpattern(p, recursivePattern.MatchedType), this);
        }
    }

    internal sealed partial class CSharpLazyPropertySubpatternOperation : LazyPropertySubpatternOperation
    {
        private readonly BoundSubpattern _subpattern;
        private readonly CSharpOperationFactory _operationFactory;
        private readonly ITypeSymbol _matchedType;

        public CSharpLazyPropertySubpatternOperation(
            CSharpOperationFactory operationFactory,
            BoundSubpattern subpattern,
            ITypeSymbol matchedType,
            SyntaxNode syntax,
            SemanticModel semanticModel)
            : base(semanticModel, syntax, type: null, constantValue: default, isImplicit: false)
        {
            _subpattern = subpattern;
            _operationFactory = operationFactory;
            _matchedType = matchedType;
        }
        protected override IOperation CreateMember()
        {
            return _operationFactory.CreatePropertySubpatternMember(_subpattern.Symbol, _matchedType, Syntax);
        }

        protected override IPatternOperation CreatePattern()
        {
            return (IPatternOperation)_operationFactory.Create(_subpattern.Pattern);
        }
    }

    /// <summary>
    /// Represents a C# recursive pattern using ITuple.
    /// </summary>
    internal sealed partial class CSharpLazyITuplePatternOperation : LazyRecursivePatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundITuplePattern _boundITuplePattern;

        public CSharpLazyITuplePatternOperation(CSharpOperationFactory operationFactory, BoundITuplePattern boundITuplePattern, SemanticModel semanticModel)
            : base(inputType: boundITuplePattern.InputType,
                   matchedType: boundITuplePattern.InputType.StrippedType(),
                   deconstructSymbol: boundITuplePattern.GetLengthMethod.ContainingType,
                   declaredSymbol: null,
                   semanticModel: semanticModel,
                   syntax: boundITuplePattern.Syntax,
                   type: null,
                   constantValue: default,
                   isImplicit: boundITuplePattern.WasCompilerGenerated)
        {
            _operationFactory = operationFactory;
            _boundITuplePattern = boundITuplePattern;

        }
        protected override ImmutableArray<IPatternOperation> CreateDeconstructionSubpatterns()
        {
            return _boundITuplePattern.Subpatterns.IsDefault ? ImmutableArray<IPatternOperation>.Empty :
                _boundITuplePattern.Subpatterns.SelectAsArray((p, fac) => (IPatternOperation)fac.Create(p.Pattern), _operationFactory);
        }
        protected override ImmutableArray<IPropertySubpatternOperation> CreatePropertySubpatterns()
        {
            return ImmutableArray<IPropertySubpatternOperation>.Empty;
        }
    }

    internal sealed class CSharpLazyPatternCaseClauseOperation : LazyPatternCaseClauseOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundSwitchLabel _patternSwitchLabel;

        internal CSharpLazyPatternCaseClauseOperation(CSharpOperationFactory operationFactory, BoundSwitchLabel patternSwitchLabel, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _patternSwitchLabel = patternSwitchLabel;
        }

        protected override IPatternOperation CreatePattern()
        {
            return (IPatternOperation)_operationFactory.Create(_patternSwitchLabel.Pattern);
        }

        protected override IOperation CreateGuard()
        {
            return _operationFactory.Create(_patternSwitchLabel.WhenClause);
        }
    }

    internal sealed class CSharpLazyIsPatternOperation : LazyIsPatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundIsPatternExpression _isPatternExpression;

        internal CSharpLazyIsPatternOperation(CSharpOperationFactory operationFactory, BoundIsPatternExpression isPatternExpression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _isPatternExpression = isPatternExpression;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_isPatternExpression.Expression);
        }

        protected override IPatternOperation CreatePattern()
        {
            return (IPatternOperation)_operationFactory.Create(_isPatternExpression.Pattern);
        }
    }

    internal sealed class CSharpLazySwitchExpressionOperation : LazySwitchExpressionOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundSwitchExpression _switchExpression;

        public CSharpLazySwitchExpressionOperation(CSharpOperationFactory operationFactory, BoundSwitchExpression boundSwitchExpression, SemanticModel semanticModel)
            : base(semanticModel, boundSwitchExpression.Syntax, boundSwitchExpression.Type, constantValue: default, boundSwitchExpression.WasCompilerGenerated)
        {
            _operationFactory = operationFactory;
            _switchExpression = boundSwitchExpression;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_switchExpression.Expression);
        }
        protected override ImmutableArray<ISwitchExpressionArmOperation> CreateArms()
        {
            return _operationFactory.CreateFromArray<BoundSwitchExpressionArm, ISwitchExpressionArmOperation>(_switchExpression.SwitchArms);
        }
    }

    internal sealed class CSharpLazySwitchExpressionArmOperation : LazySwitchExpressionArmOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundSwitchExpressionArm _switchExpressionArm;

        public CSharpLazySwitchExpressionArmOperation(CSharpOperationFactory operationFactory, BoundSwitchExpressionArm boundSwitchExpressionArm, SemanticModel semanticModel)
            : base(boundSwitchExpressionArm.Locals.Cast<CSharp.Symbols.LocalSymbol, ILocalSymbol>(), semanticModel, boundSwitchExpressionArm.Syntax, type: null, constantValue: default, boundSwitchExpressionArm.WasCompilerGenerated)
        {
            _operationFactory = operationFactory;
            _switchExpressionArm = boundSwitchExpressionArm;
        }

        protected override IOperation CreateGuard()
        {
            return _operationFactory.Create(_switchExpressionArm.WhenClause);
        }

        protected override IPatternOperation CreatePattern()
        {
            return (IPatternOperation)_operationFactory.Create(_switchExpressionArm.Pattern);
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_switchExpressionArm.Value);
        }
    }

    internal sealed class CSharpLazyObjectOrCollectionInitializerOperation : LazyObjectOrCollectionInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundExpression _initializer;

        internal CSharpLazyObjectOrCollectionInitializerOperation(CSharpOperationFactory operationFactory, BoundExpression initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _initializer = initializer;
        }

        protected override ImmutableArray<IOperation> CreateInitializers()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(BoundObjectCreationExpression.GetChildInitializers(_initializer));
        }
    }

    internal sealed class CSharpLazyMemberInitializerOperation : LazyMemberInitializerOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundAssignmentOperator _assignmentOperator;

        internal CSharpLazyMemberInitializerOperation(CSharpOperationFactory operationFactory, BoundAssignmentOperator assignmentOperator, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _assignmentOperator = assignmentOperator;
        }

        protected override IOperation CreateInitializedMember()
        {
            return _operationFactory.CreateMemberInitializerInitializedMember(_assignmentOperator.Left);
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)_operationFactory.Create(_assignmentOperator.Right);
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
        private readonly BoundNonConstructorMethodBody _methodBody;

        internal CSharpLazyMethodBodyOperation(CSharpOperationFactory operationFactory, BoundNonConstructorMethodBody methodBody, SemanticModel semanticModel, SyntaxNode syntax) :
            base(semanticModel, syntax, type: null, constantValue: default, isImplicit: false)
        {
            _operationFactory = operationFactory;
            _methodBody = methodBody;
        }

        protected override IBlockOperation CreateBlockBody()
        {
            return (IBlockOperation)_operationFactory.Create(_methodBody.BlockBody);
        }

        protected override IBlockOperation CreateExpressionBody()
        {
            return (IBlockOperation)_operationFactory.Create(_methodBody.ExpressionBody);
        }
    }

    internal sealed class CSharpLazyConstructorBodyOperation : LazyConstructorBodyOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundConstructorMethodBody _constructorMethodBody;

        internal CSharpLazyConstructorBodyOperation(CSharpOperationFactory operationFactory, BoundConstructorMethodBody constructorMethodBody, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax) :
            base(locals, semanticModel, syntax, type: null, constantValue: default, isImplicit: false)
        {
            _operationFactory = operationFactory;
            _constructorMethodBody = constructorMethodBody;
        }

        protected override IOperation CreateInitializer()
        {
            return _operationFactory.Create(_constructorMethodBody.Initializer);
        }

        protected override IBlockOperation CreateBlockBody()
        {
            return (IBlockOperation)_operationFactory.Create(_constructorMethodBody.BlockBody);
        }

        protected override IBlockOperation CreateExpressionBody()
        {
            return (IBlockOperation)_operationFactory.Create(_constructorMethodBody.ExpressionBody);
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

    internal sealed class CSharpLazyRangeOperation : LazyRangeOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundRangeExpression _rangeExpression;

        internal CSharpLazyRangeOperation(CSharpOperationFactory operationFactory, BoundRangeExpression rangeExpression, bool isLifted, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, IMethodSymbol symbol, bool isImplicit) :
            base(isLifted, symbol, semanticModel, syntax, type, constantValue: default, isImplicit)
        {
            _operationFactory = operationFactory;
            _rangeExpression = rangeExpression;
        }

        protected override IOperation CreateLeftOperand()
        {
            return _operationFactory.Create(_rangeExpression.LeftOperandOpt);
        }

        protected override IOperation CreateRightOperand()
        {
            return _operationFactory.Create(_rangeExpression.RightOperandOpt);
        }
    }
}
