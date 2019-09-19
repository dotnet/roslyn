// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed partial class CSharpOperationFactory
    {
        private static readonly IConvertibleConversion s_boxedIdentityConversion = Conversion.Identity;

        internal static Optional<object> ConvertToOptional(ConstantValue value)
        {
            return value != null && !value.IsBad ? new Optional<object>(value.Value) : default(Optional<object>);
        }

        internal ImmutableArray<BoundStatement> ToStatements(BoundStatement statement)
        {
            if (statement == null)
            {
                return ImmutableArray<BoundStatement>.Empty;
            }

            if (statement.Kind == BoundKind.StatementList)
            {
                return ((BoundStatementList)statement).Statements;
            }

            return ImmutableArray.Create(statement);
        }

        private IInstanceReferenceOperation CreateImplicitReceiver(SyntaxNode syntax, ITypeSymbol type) =>
            new InstanceReferenceOperation(InstanceReferenceKind.ImplicitReceiver, _semanticModel, syntax, type, constantValue: default, isImplicit: true);

        internal IArgumentOperation CreateArgumentOperation(ArgumentKind kind, IParameterSymbol parameter, BoundExpression expression)
        {
            // put argument syntax to argument operation

            if (expression.Syntax?.Parent is ArgumentSyntax argument)
            {
                // if argument syntax doesn't exist, this operation is implicit
                return new CSharpLazyArgumentOperation(this,
                    expression,
                    kind,
                    s_boxedIdentityConversion,
                    s_boxedIdentityConversion,
                    parameter,
                    semanticModel: _semanticModel,
                    syntax: argument,
                    isImplicit: expression.WasCompilerGenerated);
            }
            else
            {
                // We have to create the argument child eagerly here, as we need to use its syntax for this node, but the BoundExpression
                // syntax may not be the correct syntax in certain scenarios (such as query clauses that need to be skipped).
                IOperation value = Create(expression);
                return new ArgumentOperation(
                    value,
                    kind,
                    parameter,
                    s_boxedIdentityConversion,
                    s_boxedIdentityConversion,
                    _semanticModel,
                    value.Syntax,
                    isImplicit: true);
            }
        }

        internal IVariableInitializerOperation CreateVariableDeclaratorInitializer(BoundLocalDeclaration boundLocalDeclaration, SyntaxNode syntax)
        {
            if (boundLocalDeclaration.InitializerOpt != null)
            {
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
                    initializerSyntax = boundLocalDeclaration.InitializerOpt.Syntax;
                    initializerIsImplicit = true;
                }

                return new CSharpLazyVariableInitializerOperation(this, boundLocalDeclaration.InitializerOpt, _semanticModel, initializerSyntax, type: null, constantValue: default, initializerIsImplicit);
            }

            return null;
        }

        private IVariableDeclaratorOperation CreateVariableDeclaratorInternal(BoundLocalDeclaration boundLocalDeclaration, SyntaxNode syntax)
        {
            ILocalSymbol symbol = boundLocalDeclaration.LocalSymbol;
            SyntaxNode syntaxNode = boundLocalDeclaration.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default;
            bool isImplicit = false;

            return new CSharpLazyVariableDeclaratorOperation(this, boundLocalDeclaration, symbol, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal IVariableDeclaratorOperation CreateVariableDeclarator(BoundLocal boundLocal)
        {
            return boundLocal == null ? null : new VariableDeclaratorOperation(boundLocal.LocalSymbol, initializer: null, ignoredArguments: ImmutableArray<IOperation>.Empty, semanticModel: _semanticModel, syntax: boundLocal.Syntax, type: null, constantValue: default, isImplicit: false);
        }

        internal IOperation CreateReceiverOperation(BoundNode instance, ISymbol symbol)
        {
            if (instance == null || instance.Kind == BoundKind.TypeExpression)
            {
                return null;
            }

            // Static members cannot have an implicit this receiver
            if (symbol != null && symbol.IsStatic && instance.WasCompilerGenerated && instance.Kind == BoundKind.ThisReference)
            {
                return null;
            }

            return Create(instance);
        }

        private bool IsCallVirtual(MethodSymbol targetMethod, BoundExpression receiver)
        {
            return (object)targetMethod != null && receiver != null &&
                   (targetMethod.IsVirtual || targetMethod.IsAbstract || targetMethod.IsOverride) &&
                   !receiver.SuppressVirtualCalls;
        }

        private bool IsMethodInvalid(LookupResultKind resultKind, MethodSymbol targetMethod) =>
            resultKind == LookupResultKind.OverloadResolutionFailure || targetMethod?.OriginalDefinition is ErrorMethodSymbol;

        internal IEventReferenceOperation CreateBoundEventAccessOperation(BoundEventAssignmentOperator boundEventAssignmentOperator)
        {
            SyntaxNode syntax = boundEventAssignmentOperator.Syntax;
            // BoundEventAssignmentOperator doesn't hold on to BoundEventAccess provided during binding.
            // Based on the implementation of those two bound node types, the following data can be retrieved w/o changing BoundEventAssignmentOperator:
            //  1. the type of BoundEventAccess is the type of the event symbol.
            //  2. the constant value of BoundEventAccess is always null.
            //  3. the syntax of the boundEventAssignmentOperator is always AssignmentExpressionSyntax, so the syntax for the event reference would be the LHS of the assignment.
            IEventSymbol @event = boundEventAssignmentOperator.Event;
            BoundNode instance = boundEventAssignmentOperator.ReceiverOpt;
            SyntaxNode eventAccessSyntax = ((AssignmentExpressionSyntax)syntax).Left;
            bool isImplicit = boundEventAssignmentOperator.WasCompilerGenerated;

            return new CSharpLazyEventReferenceOperation(this, instance, @event, _semanticModel, eventAccessSyntax, @event.Type, ConvertToOptional(null), isImplicit);
        }

        internal IOperation CreateDelegateTargetOperation(BoundNode delegateNode)
        {
            if (delegateNode is BoundConversion boundConversion)
            {
                if (boundConversion.ConversionKind == ConversionKind.MethodGroup)
                {
                    // We don't check HasErrors on the conversion here because if we actually have a MethodGroup conversion,
                    // overload resolution succeeded. The resulting method could be invalid for other reasons, but we don't
                    // hide the resolved method.
                    return CreateBoundMethodGroupSingleMethodOperation((BoundMethodGroup)boundConversion.Operand,
                                                                       boundConversion.SymbolOpt,
                                                                       boundConversion.SuppressVirtualCalls);
                }
                else
                {
                    return Create(boundConversion.Operand);
                }
            }
            else
            {
                var boundDelegateCreationExpression = (BoundDelegateCreationExpression)delegateNode;
                if (boundDelegateCreationExpression.Argument.Kind == BoundKind.MethodGroup &&
                    boundDelegateCreationExpression.MethodOpt != null)
                {
                    // If this is a method binding, and a valid candidate method was found, then we want to expose
                    // this child as an IMethodBindingReference. Otherwise, we want to just delegate to the standard
                    // CSharpOperationFactory behavior. Note we don't check HasErrors here because if we have a method group,
                    // overload resolution succeeded, even if the resulting method isn't valid for some other reason.
                    BoundMethodGroup boundMethodGroup = (BoundMethodGroup)boundDelegateCreationExpression.Argument;
                    return CreateBoundMethodGroupSingleMethodOperation(boundMethodGroup, boundDelegateCreationExpression.MethodOpt, boundMethodGroup.SuppressVirtualCalls);
                }
                else
                {
                    return Create(boundDelegateCreationExpression.Argument);
                }
            }
        }

        internal IOperation CreateMemberInitializerInitializedMember(BoundNode initializedMember)
        {

            switch (initializedMember.Kind)
            {
                case BoundKind.ObjectInitializerMember:
                    return _nodeMap.GetOrAdd(initializedMember, key =>
                        CreateBoundObjectInitializerMemberOperation((BoundObjectInitializerMember)key, isObjectOrCollectionInitializer: true));
                case BoundKind.DynamicObjectInitializerMember:
                    return _nodeMap.GetOrAdd(initializedMember, key =>
                        CreateBoundDynamicObjectInitializerMemberOperation((BoundDynamicObjectInitializerMember)key));
                default:
                    return Create(initializedMember);
            }
        }

        internal ImmutableArray<IArgumentOperation> DeriveArguments(BoundNode containingExpression, bool isObjectOrCollectionInitializer)
        {
            switch (containingExpression.Kind)
            {
                case BoundKind.ObjectInitializerMember:
                    {
                        var boundObjectInitializerMember = (BoundObjectInitializerMember)containingExpression;
                        var property = (PropertySymbol)boundObjectInitializerMember.MemberSymbol;
                        MethodSymbol accessor = isObjectOrCollectionInitializer ? property.GetOwnOrInheritedGetMethod() : property.GetOwnOrInheritedSetMethod();
                        return DeriveArguments(
                                    boundObjectInitializerMember,
                                    boundObjectInitializerMember.BinderOpt,
                                    property,
                                    accessor,
                                    boundObjectInitializerMember.Arguments,
                                    boundObjectInitializerMember.ArgumentNamesOpt,
                                    boundObjectInitializerMember.ArgsToParamsOpt,
                                    boundObjectInitializerMember.ArgumentRefKindsOpt,
                                    property.Parameters,
                                    boundObjectInitializerMember.Expanded,
                                    boundObjectInitializerMember.Syntax);
                    }

                default:
                    return DeriveArguments(containingExpression);
            }
        }

        internal ImmutableArray<IArgumentOperation> DeriveArguments(BoundNode containingExpression)
        {
            switch (containingExpression.Kind)
            {
                case BoundKind.IndexerAccess:
                    {
                        var boundIndexer = (BoundIndexerAccess)containingExpression;
                        return DeriveArguments(boundIndexer,
                                               boundIndexer.BinderOpt,
                                               boundIndexer.Indexer,
                                               boundIndexer.UseSetterForDefaultArgumentGeneration ? boundIndexer.Indexer.GetOwnOrInheritedSetMethod() :
                                                                                                    boundIndexer.Indexer.GetOwnOrInheritedGetMethod(),
                                               boundIndexer.Arguments,
                                               boundIndexer.ArgumentNamesOpt,
                                               boundIndexer.ArgsToParamsOpt,
                                               boundIndexer.ArgumentRefKindsOpt,
                                               boundIndexer.Indexer.Parameters,
                                               boundIndexer.Expanded,
                                               boundIndexer.Syntax);
                    }
                case BoundKind.ObjectCreationExpression:
                    {
                        var objectCreation = (BoundObjectCreationExpression)containingExpression;
                        return DeriveArguments(objectCreation,
                                               objectCreation.BinderOpt,
                                               objectCreation.Constructor,
                                               objectCreation.Constructor,
                                               objectCreation.Arguments,
                                               objectCreation.ArgumentNamesOpt,
                                               objectCreation.ArgsToParamsOpt,
                                               objectCreation.ArgumentRefKindsOpt,
                                               objectCreation.Constructor.Parameters,
                                               objectCreation.Expanded,
                                               objectCreation.Syntax);
                    }
                case BoundKind.Call:
                    {
                        var boundCall = (BoundCall)containingExpression;
                        return DeriveArguments(boundCall,
                                               boundCall.BinderOpt,
                                               boundCall.Method,
                                               boundCall.Method,
                                               boundCall.Arguments,
                                               boundCall.ArgumentNamesOpt,
                                               boundCall.ArgsToParamsOpt,
                                               boundCall.ArgumentRefKindsOpt,
                                               boundCall.Method.Parameters,
                                               boundCall.Expanded,
                                               boundCall.Syntax,
                                               boundCall.InvokedAsExtensionMethod);
                    }
                case BoundKind.CollectionElementInitializer:
                    {
                        var boundCollectionElementInitializer = (BoundCollectionElementInitializer)containingExpression;
                        return DeriveArguments(boundCollectionElementInitializer,
                                               boundCollectionElementInitializer.BinderOpt,
                                               boundCollectionElementInitializer.AddMethod,
                                               boundCollectionElementInitializer.AddMethod,
                                               boundCollectionElementInitializer.Arguments,
                                               argumentNamesOpt: default,
                                               boundCollectionElementInitializer.ArgsToParamsOpt,
                                               argumentRefKindsOpt: default,
                                               boundCollectionElementInitializer.AddMethod.Parameters,
                                               boundCollectionElementInitializer.Expanded,
                                               boundCollectionElementInitializer.Syntax,
                                               boundCollectionElementInitializer.InvokedAsExtensionMethod);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(containingExpression.Kind);
            }
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

        internal static ImmutableArray<BoundNode> CreateInvalidChildrenFromArgumentsExpression(BoundNode receiverOpt, ImmutableArray<BoundExpression> arguments, BoundExpression additionalNodeOpt = null)
        {
            var builder = ArrayBuilder<BoundNode>.GetInstance();

            if (receiverOpt != null
               && (!receiverOpt.WasCompilerGenerated
                   || (receiverOpt.Kind != BoundKind.ThisReference
                      && receiverOpt.Kind != BoundKind.BaseReference
                      && receiverOpt.Kind != BoundKind.CollectionValuePlaceholder)))
            {
                builder.Add(receiverOpt);
            }

            builder.AddRange(StaticCast<BoundNode>.From(arguments));

            builder.AddIfNotNull(additionalNodeOpt);

            return builder.ToImmutableAndFree();
        }

        internal ImmutableArray<IOperation> GetAnonymousObjectCreationInitializers(
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<BoundAnonymousPropertyDeclaration> declarations,
            SyntaxNode syntax,
            ITypeSymbol type,
            bool isImplicit)
        {
            // For error cases and non-assignment initializers, the binder generates only the argument.
            Debug.Assert(arguments.Length >= declarations.Length);

            var builder = ArrayBuilder<IOperation>.GetInstance(arguments.Length);
            var currentDeclarationIndex = 0;
            for (int i = 0; i < arguments.Length; i++)
            {
                IOperation value = Create(arguments[i]);

                IOperation target;
                bool isImplicitAssignment;

                // Synthesize an implicit receiver for property reference being assigned.
                var instance = new InstanceReferenceOperation(
                        referenceKind: InstanceReferenceKind.ImplicitReceiver,
                        semanticModel: _semanticModel,
                        syntax: syntax,
                        type: type,
                        constantValue: default,
                        isImplicit: true);

                // Find matching declaration for the current argument.
                PropertySymbol property = AnonymousTypeManager.GetAnonymousTypeProperty((NamedTypeSymbol)type, i);
                BoundAnonymousPropertyDeclaration anonymousProperty = getDeclaration(declarations, property, ref currentDeclarationIndex);
                if (anonymousProperty is null)
                {
                    // No matching declaration, synthesize a property reference to be assigned.
                    target = new PropertyReferenceOperation(
                        property,
                        arguments: ImmutableArray<IArgumentOperation>.Empty,
                        instance,
                        semanticModel: _semanticModel,
                        syntax: value.Syntax,
                        type: property.Type,
                        constantValue: default,
                        isImplicit: true);
                    isImplicitAssignment = true;
                }
                else
                {
                    target = new PropertyReferenceOperation(anonymousProperty.Property,
                                                            ImmutableArray<IArgumentOperation>.Empty,
                                                            instance,
                                                            _semanticModel,
                                                            anonymousProperty.Syntax,
                                                            anonymousProperty.Type,
                                                            ConvertToOptional(anonymousProperty.ConstantValue),
                                                            anonymousProperty.WasCompilerGenerated);
                    isImplicitAssignment = isImplicit;
                }

                var assignmentSyntax = value.Syntax?.Parent ?? syntax;
                ITypeSymbol assignmentType = target.Type;
                Optional<object> constantValue = value.ConstantValue;
                bool isRef = false;
                var assignment = new SimpleAssignmentOperation(isRef, target, value, _semanticModel, assignmentSyntax, assignmentType, constantValue, isImplicitAssignment);
                builder.Add(assignment);
            }

            Debug.Assert(currentDeclarationIndex == declarations.Length);
            return builder.ToImmutableAndFree();

            static BoundAnonymousPropertyDeclaration getDeclaration(ImmutableArray<BoundAnonymousPropertyDeclaration> declarations, PropertySymbol currentProperty, ref int currentDeclarationIndex)
            {
                if (currentDeclarationIndex >= declarations.Length)
                {
                    return null;
                }

                var currentDeclaration = declarations[currentDeclarationIndex];

                if (currentProperty.MemberIndexOpt == currentDeclaration.Property.MemberIndexOpt)
                {
                    currentDeclarationIndex++;
                    return currentDeclaration;
                }

                return null;
            }
        }

        internal class Helper
        {
            internal static bool IsPostfixIncrementOrDecrement(CSharp.UnaryOperatorKind operatorKind)
            {
                switch (operatorKind.Operator())
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
                switch (operatorKind.Operator())
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
                switch (operatorKind.Operator())
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
