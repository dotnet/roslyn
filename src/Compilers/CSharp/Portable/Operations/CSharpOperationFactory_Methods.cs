// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal sealed partial class CSharpOperationFactory
    {
        private static Optional<object> ConvertToOptional(ConstantValue value)
        {
            return value != null ? new Optional<object>(value.Value) : default(Optional<object>);
        }

        private ImmutableArray<IOperation> ToStatements(BoundStatement statement)
        {
            var statementList = statement as BoundStatementList;
            if (statementList != null)
            {
                return statementList.Statements.SelectAsArray(n => Create(n));
            }
            else if (statement == null)
            {
                return ImmutableArray<IOperation>.Empty;
            }

            return ImmutableArray.Create(Create(statement));
        }

        internal IArgument CreateArgumentOperation(ArgumentKind kind, IParameterSymbol parameter, BoundExpression expression)
        {
            var value = Create(expression);

            // put argument syntax to argument operation
            var argument = value.Syntax?.Parent as ArgumentSyntax;

            // if argument syntax doesn't exist, this operation is implicit
            return new CSharpArgument(kind,
                parameter,
                value,
                semanticModel: _semanticModel,
                syntax: argument ?? value.Syntax,
                type: value.Type,
                constantValue: default,
                isImplicit: expression.WasCompilerGenerated || argument == null);
        }

        private IVariableDeclaration CreateVariableDeclarationInternal(BoundLocalDeclaration boundLocalDeclaration, SyntaxNode syntax)
        {
            IVariableInitializer initializer = null;
            if (boundLocalDeclaration.InitializerOpt != null)
            {
                IOperation initializerValue = Create(boundLocalDeclaration.InitializerOpt);
                SyntaxNode initializerSyntax;
                bool isImplicit;
                if (syntax is VariableDeclaratorSyntax variableDeclarator)
                {
                    initializerSyntax = variableDeclarator.Initializer;
                    isImplicit = false;
                }
                else
                {
                    initializerSyntax = initializer.Syntax;
                    isImplicit = true;
                }

                initializer = OperationFactory.CreateVariableInitializer(initializerSyntax, initializerValue, _semanticModel, isImplicit);
            }

            return OperationFactory.CreateVariableDeclaration(boundLocalDeclaration.LocalSymbol, initializer, _semanticModel, syntax);
        }

        private IVariableDeclaration CreateVariableDeclaration(BoundLocal boundLocal)
        {
            return OperationFactory.CreateVariableDeclaration(boundLocal.LocalSymbol, initializer: null, semanticModel: _semanticModel, syntax: boundLocal.Syntax);
        }

        private IOperation CreateBoundCallInstanceOperation(BoundCall boundCall)
        {
            if (boundCall.Method == null || boundCall.Method.IsStatic)
            {
                return null;
            }

            return Create(boundCall.ReceiverOpt);
        }

        private IEventReferenceExpression CreateBoundEventAccessOperation(BoundEventAssignmentOperator boundEventAssignmentOperator)
        {
            SyntaxNode syntax = boundEventAssignmentOperator.Syntax;
            // BoundEventAssignmentOperator doesn't hold on to BoundEventAccess provided during binding.
            // Based on the implementation of those two bound node types, the following data can be retrieved w/o changing BoundEventAssignmentOperator:
            //  1. the type of BoundEventAccess is the type of the event symbol.
            //  2. the constant value of BoundEventAccess is always null.
            //  3. the syntax of the boundEventAssignmentOperator is always AssignmentExpressionSyntax, so the syntax for the event reference would be the LHS of the assignment.
            IEventSymbol @event = boundEventAssignmentOperator.Event;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => Create(boundEventAssignmentOperator.Event.IsStatic ? null : boundEventAssignmentOperator.ReceiverOpt));
            SyntaxNode eventAccessSyntax = ((AssignmentExpressionSyntax)syntax).Left;
            bool isImplicit = boundEventAssignmentOperator.WasCompilerGenerated;

            return new LazyEventReferenceExpression(@event, instance, @event, _semanticModel, eventAccessSyntax, @event.Type, ConvertToOptional(null), isImplicit);
        }

        private ImmutableArray<IArgument> DeriveArguments(
            BoundNode boundNode,
            Binder binder,
            Symbol methodOrIndexer,
            MethodSymbol optionalParametersMethod,
            ImmutableArray<BoundExpression> boundArguments,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<int> argumentsToParametersOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            ImmutableArray<ParameterSymbol> parameters,
            bool expanded,
            SyntaxNode invocationSyntax,
            bool invokedAsExtensionMethod = false)
        {
            // We can simply return empty array only if both parameters and boundArguments are empty, because:
            // - if only parameters is empty, there's error in code but we still need to return provided expression.
            // - if boundArguments is empty, then either there's error or we need to provide values for optional/param-array parameters. 
            if (parameters.IsDefaultOrEmpty && boundArguments.IsDefaultOrEmpty)
            {
                return ImmutableArray<IArgument>.Empty;
            }

            //TODO: https://github.com/dotnet/roslyn/issues/18722
            //      Right now, for erroneous code, we exposes all expression in place of arguments as IArgument with Parameter set to null,
            //      so user needs to check IsInvalid first before using anything we returned. Need to implement a new interface for invalid 
            //      invocation instead.
            //      Note this check doesn't cover all scenarios. For example, when a parameter is a generic type but the type of the type argument 
            //      is undefined.
            if ((object)optionalParametersMethod == null
                || boundNode.HasAnyErrors
                || parameters.Any(p => p.Type.IsErrorType())
                || optionalParametersMethod.GetUseSiteDiagnostic()?.DefaultSeverity == DiagnosticSeverity.Error)
            {
                // optionalParametersMethod can be null if we are writing to a readonly indexer or reading from an writeonly indexer,
                // in which case HasErrors property would be true, but we still want to treat this as invalid invocation.
                return boundArguments.SelectAsArray(arg => CreateArgumentOperation(ArgumentKind.Explicit, null, arg));
            }

            return LocalRewriter.MakeArgumentsInEvaluationOrder(
                 operationFactory: this,
                 binder: binder,
                 syntax: invocationSyntax,
                 arguments: boundArguments,
                 methodOrIndexer: methodOrIndexer,
                 optionalParametersMethod: optionalParametersMethod,
                 expanded: expanded,
                 argsToParamsOpt: argumentsToParametersOpt,
                 invokedAsExtensionMethod: invokedAsExtensionMethod);
        }

        private ImmutableArray<IOperation> GetAnonymousObjectCreationInitializers(BoundAnonymousObjectCreationExpression expression)
        {
            // For error cases, the binder generates only the argument.
            Debug.Assert(expression.Arguments.Length >= expression.Declarations.Length);

            var builder = ArrayBuilder<IOperation>.GetInstance(expression.Arguments.Length);
            for (int i = 0; i < expression.Arguments.Length; i++)
            {
                IOperation value = Create(expression.Arguments[i]);
                if (i >= expression.Declarations.Length)
                {
                    builder.Add(value);
                    continue;
                }

                IOperation target = Create(expression.Declarations[i]);
                SyntaxNode syntax = value.Syntax?.Parent ?? expression.Syntax;
                ITypeSymbol type = target.Type;
                Optional<object> constantValue = value.ConstantValue;
                var assignment = new SimpleAssignmentExpression(target, value, _semanticModel, syntax, type, constantValue, isImplicit: value.IsImplicit);
                builder.Add(assignment);
            }

            return builder.ToImmutableAndFree();
        }

        private ImmutableArray<ISwitchCase> GetSwitchStatementCases(BoundSwitchStatement statement)
        {
            return statement.SwitchSections.SelectAsArray(switchSection =>
            {
                var clauses = switchSection.SwitchLabels.SelectAsArray(s => (ICaseClause)Create(s));
                var body = switchSection.Statements.SelectAsArray(s => Create(s));

                return (ISwitchCase)new SwitchCase(clauses, body, _semanticModel, switchSection.Syntax, type: null, constantValue: default(Optional<object>), isImplicit: switchSection.WasCompilerGenerated);
            });
        }

        private ImmutableArray<ISwitchCase> GetPatternSwitchStatementCases(BoundPatternSwitchStatement statement)
        {
            return statement.SwitchSections.SelectAsArray(switchSection =>
            {
                var clauses = switchSection.SwitchLabels.SelectAsArray(s => (ICaseClause)Create(s));
                var body = switchSection.Statements.SelectAsArray(s => Create(s));

                return (ISwitchCase)new SwitchCase(clauses, body, _semanticModel, switchSection.Syntax, type: null, constantValue: default(Optional<object>), isImplicit: switchSection.WasCompilerGenerated);
            });
        }

        internal class Helper
        {
            internal static bool IsPostfixIncrementOrDecrement(CSharp.UnaryOperatorKind operatorKind)
            {
                switch (operatorKind & CSharp.UnaryOperatorKind.OpMask)
                {
                    case CSharp.UnaryOperatorKind.PostfixIncrement:
                    case CSharp.UnaryOperatorKind.PostfixDecrement:
                        return true;

                    default:
                        return false;
                }
            }

            internal static bool IsDecrement(CSharp.UnaryOperatorKind operatorKind)
            {
                switch (operatorKind & CSharp.UnaryOperatorKind.OpMask)
                {
                    case CSharp.UnaryOperatorKind.PrefixDecrement:
                    case CSharp.UnaryOperatorKind.PostfixDecrement:
                        return true;

                    default:
                        return false;
                }
            }

            internal static UnaryOperatorKind DeriveUnaryOperatorKind(CSharp.UnaryOperatorKind operatorKind)
            {
                switch (operatorKind & CSharp.UnaryOperatorKind.OpMask)
                {
                    case CSharp.UnaryOperatorKind.UnaryPlus:
                        return UnaryOperatorKind.Plus;

                    case CSharp.UnaryOperatorKind.UnaryMinus:
                        return UnaryOperatorKind.Minus;

                    case CSharp.UnaryOperatorKind.LogicalNegation:
                        return UnaryOperatorKind.Not;

                    case CSharp.UnaryOperatorKind.BitwiseComplement:
                        return UnaryOperatorKind.BitwiseNegation;

                    case CSharp.UnaryOperatorKind.True:
                        return UnaryOperatorKind.True;

                    case CSharp.UnaryOperatorKind.False:
                        return UnaryOperatorKind.False;
                }

                return UnaryOperatorKind.Invalid;
            }

            internal static BinaryOperatorKind DeriveBinaryOperatorKind(CSharp.BinaryOperatorKind operatorKind)
            {
                switch (operatorKind & CSharp.BinaryOperatorKind.OpMask)
                {
                    case CSharp.BinaryOperatorKind.Addition:
                        return BinaryOperatorKind.Add;

                    case CSharp.BinaryOperatorKind.Subtraction:
                        return BinaryOperatorKind.Subtract;

                    case CSharp.BinaryOperatorKind.Multiplication:
                        return BinaryOperatorKind.Multiply;

                    case CSharp.BinaryOperatorKind.Division:
                        return BinaryOperatorKind.Divide;

                    case CSharp.BinaryOperatorKind.Remainder:
                        return BinaryOperatorKind.Remainder;

                    case CSharp.BinaryOperatorKind.LeftShift:
                        return BinaryOperatorKind.LeftShift;

                    case CSharp.BinaryOperatorKind.RightShift:
                        return BinaryOperatorKind.RightShift;

                    case CSharp.BinaryOperatorKind.And:
                        return BinaryOperatorKind.And;

                    case CSharp.BinaryOperatorKind.Or:
                        return BinaryOperatorKind.Or;

                    case CSharp.BinaryOperatorKind.Xor:
                        return BinaryOperatorKind.ExclusiveOr;

                    case CSharp.BinaryOperatorKind.LessThan:
                        return BinaryOperatorKind.LessThan;

                    case CSharp.BinaryOperatorKind.LessThanOrEqual:
                        return BinaryOperatorKind.LessThanOrEqual;

                    case CSharp.BinaryOperatorKind.Equal:
                        return BinaryOperatorKind.Equals;

                    case CSharp.BinaryOperatorKind.NotEqual:
                        return BinaryOperatorKind.NotEquals;

                    case CSharp.BinaryOperatorKind.GreaterThanOrEqual:
                        return BinaryOperatorKind.GreaterThanOrEqual;

                    case CSharp.BinaryOperatorKind.GreaterThan:
                        return BinaryOperatorKind.GreaterThan;
                }

                return BinaryOperatorKind.Invalid;
            }
        }
    }
}
