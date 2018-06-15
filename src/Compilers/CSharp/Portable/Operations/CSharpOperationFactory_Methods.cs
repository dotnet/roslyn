// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed partial class CSharpOperationFactory
    {
        private static readonly IConvertibleConversion s_boxedIdentityConversion = Conversion.Identity;

        private static Optional<object> ConvertToOptional(ConstantValue value)
        {
            return value != null && !value.IsBad ? new Optional<object>(value.Value) : default(Optional<object>);
        }

        private ImmutableArray<IOperation> ToStatements(BoundStatement statement)
        {
            if (statement == null)
            {
                return ImmutableArray<IOperation>.Empty;
            }

            if (statement.Kind == BoundKind.StatementList)
            {
                return ((BoundStatementList)statement).Statements.SelectAsArray(n => Create(n));
            }

            return ImmutableArray.Create(Create(statement));
        }

        private IInstanceReferenceOperation CreateImplicitReciever(SyntaxNode syntax, ITypeSymbol type) =>
            new InstanceReferenceExpression(InstanceReferenceKind.ImplicitReceiver, _semanticModel, syntax, type, constantValue: default, isImplicit: true);

        internal IArgumentOperation CreateArgumentOperation(ArgumentKind kind, IParameterSymbol parameter, BoundExpression expression)
        {
            var value = Create(expression);

            // put argument syntax to argument operation
            var argument = value.Syntax?.Parent as ArgumentSyntax;

            // if argument syntax doesn't exist, this operation is implicit
            return new ArgumentOperation(value,
                kind,
                parameter,
                s_boxedIdentityConversion,
                s_boxedIdentityConversion,
                semanticModel: _semanticModel,
                syntax: argument ?? value.Syntax,
                isImplicit: expression.WasCompilerGenerated || argument == null);
        }

        private IVariableDeclaratorOperation CreateVariableDeclaratorInternal(BoundLocalDeclaration boundLocalDeclaration, SyntaxNode syntax)
        {
            IVariableInitializerOperation initializer = null;
            if (boundLocalDeclaration.InitializerOpt != null)
            {
                IOperation initializerValue = Create(boundLocalDeclaration.InitializerOpt);
                SyntaxNode initializerSyntax = null;
                bool initializerIsImplicit = false;
                if (syntax is VariableDeclaratorSyntax variableDeclarator)
                {
                    initializerSyntax = variableDeclarator.Initializer;
                }
                else
                {
                    Debug.Fail($"Unexpected syntax kind: {syntax.Kind()}");
                }

                if (initializerSyntax == null)
                {
                    // There is no explicit syntax for the initializer, so we use the initializerValue's syntax and mark the operation as implicit.
                    initializerSyntax = initializerValue.Syntax;
                    initializerIsImplicit = true;
                }

                initializer = OperationFactory.CreateVariableInitializer(initializerSyntax, initializerValue, _semanticModel, initializerIsImplicit);
            }

            ImmutableArray<IOperation> ignoredArguments = boundLocalDeclaration.ArgumentsOpt.IsDefault ?
                                                            ImmutableArray<IOperation>.Empty :
                                                            boundLocalDeclaration.ArgumentsOpt.SelectAsArray(arg => Create(arg));
            ILocalSymbol symbol = boundLocalDeclaration.LocalSymbol;
            SyntaxNode syntaxNode = boundLocalDeclaration.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default;
            bool isImplicit = false;

            return new VariableDeclarator(symbol, initializer, ignoredArguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IVariableDeclaratorOperation CreateVariableDeclarator(BoundLocal boundLocal)
        {
            return new VariableDeclarator(boundLocal.LocalSymbol, initializer: null, ignoredArguments: ImmutableArray<IOperation>.Empty, semanticModel: _semanticModel, syntax: boundLocal.Syntax, type: null, constantValue: default, isImplicit: false);
        }

        private Lazy<IOperation> CreateReceiverOperation(BoundNode instance, ISymbol symbol)
        {
            if (instance == null || instance.Kind == BoundKind.TypeExpression)
            {
                return OperationFactory.NullOperation;
            }

            // Static members cannot have an implicit this receiver
            if (symbol != null && symbol.IsStatic && instance.WasCompilerGenerated && instance.Kind == BoundKind.ThisReference)
            {
                return OperationFactory.NullOperation;
            }

            return new Lazy<IOperation>(() => Create(instance));
        }

        private bool IsCallVirtual(MethodSymbol targetMethod, BoundExpression receiver)
        {
            return (object)targetMethod != null && receiver != null &&
                   (targetMethod.IsVirtual || targetMethod.IsAbstract || targetMethod.IsOverride) &&
                   !receiver.SuppressVirtualCalls;
        }

        private bool IsMethodInvalid(LookupResultKind resultKind, MethodSymbol targetMethod) =>
            resultKind == LookupResultKind.OverloadResolutionFailure || targetMethod?.OriginalDefinition is ErrorMethodSymbol;

        private IEventReferenceOperation CreateBoundEventAccessOperation(BoundEventAssignmentOperator boundEventAssignmentOperator)
        {
            SyntaxNode syntax = boundEventAssignmentOperator.Syntax;
            // BoundEventAssignmentOperator doesn't hold on to BoundEventAccess provided during binding.
            // Based on the implementation of those two bound node types, the following data can be retrieved w/o changing BoundEventAssignmentOperator:
            //  1. the type of BoundEventAccess is the type of the event symbol.
            //  2. the constant value of BoundEventAccess is always null.
            //  3. the syntax of the boundEventAssignmentOperator is always AssignmentExpressionSyntax, so the syntax for the event reference would be the LHS of the assignment.
            IEventSymbol @event = boundEventAssignmentOperator.Event;
            Lazy<IOperation> instance = CreateReceiverOperation(boundEventAssignmentOperator.ReceiverOpt, @event);
            SyntaxNode eventAccessSyntax = ((AssignmentExpressionSyntax)syntax).Left;
            bool isImplicit = boundEventAssignmentOperator.WasCompilerGenerated;

            return new LazyEventReferenceExpression(@event, instance, _semanticModel, eventAccessSyntax, @event.Type, ConvertToOptional(null), isImplicit);
        }

        private ImmutableArray<IArgumentOperation> DeriveArguments(
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
                return ImmutableArray<IArgumentOperation>.Empty;
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

        private IInvalidOperation CreateInvalidExpressionForHasArgumentsExpression(BoundNode receiverOpt, ImmutableArray<BoundExpression> arguments, BoundExpression additionalNodeOpt, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
        {
            Lazy<ImmutableArray<IOperation>> children = new Lazy<ImmutableArray<IOperation>>(
                      () =>
                      {
                          ArrayBuilder<IOperation> builder = ArrayBuilder<IOperation>.GetInstance();

                          if (receiverOpt != null
                             && (!receiverOpt.WasCompilerGenerated
                                 || (receiverOpt.Kind != BoundKind.ThisReference
                                    && receiverOpt.Kind != BoundKind.BaseReference
                                    && receiverOpt.Kind != BoundKind.ImplicitReceiver)))
                          {
                              builder.Add(Create(receiverOpt));
                          }

                          builder.AddRange(arguments.Select(a => Create(a)));

                          if (additionalNodeOpt != null)
                          {
                              builder.Add(Create(additionalNodeOpt));
                          }

                          return builder.ToImmutableAndFree();
                      });
            return new LazyInvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit);
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
                bool isRef = false;
                var assignment = new SimpleAssignmentExpression(target, isRef, value, _semanticModel, syntax, type, constantValue, isImplicit: expression.WasCompilerGenerated);
                builder.Add(assignment);
            }

            return builder.ToImmutableAndFree();
        }

        private ImmutableArray<ISwitchCaseOperation> GetSwitchStatementCases(BoundSwitchStatement statement)
        {
            return statement.SwitchSections.SelectAsArray(switchSection =>
            {
                var clauses = switchSection.SwitchLabels.SelectAsArray(s => (ICaseClauseOperation)Create(s));
                var body = switchSection.Statements.SelectAsArray(s => Create(s));
                var locals = switchSection.Locals.CastArray<ILocalSymbol>();

                return (ISwitchCaseOperation)new SwitchCase(locals, condition: null, clauses, body, _semanticModel, switchSection.Syntax,
                                                            type: null, constantValue: default(Optional<object>), isImplicit: switchSection.WasCompilerGenerated);
            });
        }

        private ImmutableArray<ISwitchCaseOperation> GetPatternSwitchStatementCases(BoundPatternSwitchStatement statement)
        {
            return statement.SwitchSections.SelectAsArray(switchSection =>
            {
                var clauses = switchSection.SwitchLabels.SelectAsArray(s => (ICaseClauseOperation)Create(s));
                var body = switchSection.Statements.SelectAsArray(s => Create(s));
                ImmutableArray<ILocalSymbol> locals = switchSection.Locals.CastArray<ILocalSymbol>();

                return (ISwitchCaseOperation)new SwitchCase(locals, condition: null, clauses, body, _semanticModel, switchSection.Syntax,
                                                            type: null, constantValue: default(Optional<object>), isImplicit: switchSection.WasCompilerGenerated);
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

                return UnaryOperatorKind.None;
            }

            internal static BinaryOperatorKind DeriveBinaryOperatorKind(CSharp.BinaryOperatorKind operatorKind)
            {
                switch (operatorKind.OperatorWithLogical())
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

                    case CSharp.BinaryOperatorKind.LogicalAnd:
                        return BinaryOperatorKind.ConditionalAnd;

                    case CSharp.BinaryOperatorKind.LogicalOr:
                        return BinaryOperatorKind.ConditionalOr;
                }

                return BinaryOperatorKind.None;
            }
        }
    }
}
