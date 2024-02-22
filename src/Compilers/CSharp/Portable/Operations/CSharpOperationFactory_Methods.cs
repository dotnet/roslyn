// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed partial class CSharpOperationFactory
    {
        internal ImmutableArray<BoundStatement> ToStatements(BoundStatement? statement)
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

        private IInstanceReferenceOperation CreateImplicitReceiver(SyntaxNode syntax, TypeSymbol type) =>
            new InstanceReferenceOperation(InstanceReferenceKind.ImplicitReceiver, _semanticModel, syntax, type.GetPublicSymbol(), isImplicit: true);

        internal IArgumentOperation CreateArgumentOperation(ArgumentKind kind, IParameterSymbol? parameter, BoundExpression expression)
        {
            // put argument syntax to argument operation
            IOperation value = Create(expression is BoundConversion { IsParamsArrayOrCollection: true } conversion ? conversion.Operand : expression);
            (SyntaxNode syntax, bool isImplicit) = expression.Syntax is { Parent: ArgumentSyntax or AttributeArgumentSyntax } ? (expression.Syntax.Parent, expression.WasCompilerGenerated) : (value.Syntax, true);
            return new ArgumentOperation(
                kind,
                parameter,
                value,
                OperationFactory.IdentityConversion,
                OperationFactory.IdentityConversion,
                _semanticModel,
                syntax,
                isImplicit);
        }

        internal IVariableInitializerOperation? CreateVariableDeclaratorInitializer(BoundLocalDeclaration boundLocalDeclaration, SyntaxNode syntax)
        {
            if (boundLocalDeclaration.InitializerOpt != null)
            {
                SyntaxNode? initializerSyntax = null;
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

                IOperation value = Create(boundLocalDeclaration.InitializerOpt);
                return new VariableInitializerOperation(locals: ImmutableArray<ILocalSymbol>.Empty, value, _semanticModel, initializerSyntax, initializerIsImplicit);
            }

            return null;
        }

        private IVariableDeclaratorOperation CreateVariableDeclaratorInternal(BoundLocalDeclaration boundLocalDeclaration, SyntaxNode syntax)
        {
            ILocalSymbol symbol = boundLocalDeclaration.LocalSymbol.GetPublicSymbol();
            bool isImplicit = false;

            IVariableInitializerOperation? initializer = CreateVariableDeclaratorInitializer(boundLocalDeclaration, syntax);
            ImmutableArray<IOperation> ignoredDimensions = CreateFromArray<BoundExpression, IOperation>(boundLocalDeclaration.ArgumentsOpt);

            return new VariableDeclaratorOperation(symbol, initializer, ignoredDimensions, _semanticModel, syntax, isImplicit);
        }

        [return: NotNullIfNotNull(nameof(boundLocal))]
        internal IVariableDeclaratorOperation? CreateVariableDeclarator(BoundLocal? boundLocal)
        {
            return boundLocal == null ? null : new VariableDeclaratorOperation(boundLocal.LocalSymbol.GetPublicSymbol(), initializer: null, ignoredArguments: ImmutableArray<IOperation>.Empty, semanticModel: _semanticModel, syntax: boundLocal.Syntax, isImplicit: false);
        }

        internal IOperation? CreateReceiverOperation(BoundNode? instance, Symbol? symbol)
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

        private bool IsCallVirtual(MethodSymbol? targetMethod, BoundExpression? receiver)
        {
            return (object?)targetMethod != null && receiver != null &&
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
            IEventSymbol @event = boundEventAssignmentOperator.Event.GetPublicSymbol();
            IOperation? instance = CreateReceiverOperation(boundEventAssignmentOperator.ReceiverOpt, boundEventAssignmentOperator.Event);
            SyntaxNode eventAccessSyntax = ((AssignmentExpressionSyntax)syntax).Left;
            bool isImplicit = boundEventAssignmentOperator.WasCompilerGenerated;
            TypeParameterSymbol? constrainedToType = GetConstrainedToType(boundEventAssignmentOperator.Event, boundEventAssignmentOperator.ReceiverOpt);

            return new EventReferenceOperation(@event, constrainedToType.GetPublicSymbol(), instance, _semanticModel, eventAccessSyntax, @event.Type, isImplicit);
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
                    Debug.Assert(boundConversion.SymbolOpt is not null);
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

            switch (initializedMember)
            {
                case BoundObjectInitializerMember objectInitializer:
                    return CreateBoundObjectInitializerMemberOperation(objectInitializer, isObjectOrCollectionInitializer: true);
                case BoundDynamicObjectInitializerMember dynamicInitializer:
                    return CreateBoundDynamicObjectInitializerMemberOperation(dynamicInitializer);
                default:
                    return Create(initializedMember);
            }
        }

        internal ImmutableArray<IArgumentOperation> DeriveArguments(BoundNode containingExpression)
        {
            switch (containingExpression.Kind)
            {
                case BoundKind.ObjectInitializerMember:
                    {
                        var boundObjectInitializerMember = (BoundObjectInitializerMember)containingExpression;
                        var property = (PropertySymbol?)boundObjectInitializerMember.MemberSymbol;
                        Debug.Assert(property is not null);
                        return DeriveArguments(
                                    property,
                                    boundObjectInitializerMember.Arguments,
                                    boundObjectInitializerMember.ArgsToParamsOpt,
                                    boundObjectInitializerMember.DefaultArguments);
                    }
                case BoundKind.IndexerAccess:
                    {
                        var boundIndexer = (BoundIndexerAccess)containingExpression;
                        return DeriveArguments(boundIndexer.Indexer,
                                               boundIndexer.Arguments,
                                               boundIndexer.ArgsToParamsOpt,
                                               boundIndexer.DefaultArguments);
                    }
                case BoundKind.ObjectCreationExpression:
                    {
                        var objectCreation = (BoundObjectCreationExpression)containingExpression;
                        return DeriveArguments(objectCreation.Constructor,
                                               objectCreation.Arguments,
                                               objectCreation.ArgsToParamsOpt,
                                               objectCreation.DefaultArguments);
                    }
                case BoundKind.Attribute:
                    var attribute = (BoundAttribute)containingExpression;
                    Debug.Assert(attribute.Constructor is not null);
                    return DeriveArguments(attribute.Constructor,
                                           attribute.ConstructorArguments,
                                           attribute.ConstructorArgumentsToParamsOpt,
                                           attribute.ConstructorDefaultArguments);
                case BoundKind.Call:
                    {
                        var boundCall = (BoundCall)containingExpression;
                        return DeriveArguments(boundCall.Method,
                                               boundCall.Arguments,
                                               boundCall.ArgsToParamsOpt,
                                               boundCall.DefaultArguments,
                                               boundCall.InvokedAsExtensionMethod);
                    }
                case BoundKind.CollectionElementInitializer:
                    {
                        var boundCollectionElementInitializer = (BoundCollectionElementInitializer)containingExpression;
                        return DeriveArguments(boundCollectionElementInitializer.AddMethod,
                                               boundCollectionElementInitializer.Arguments,
                                               boundCollectionElementInitializer.ArgsToParamsOpt,
                                               boundCollectionElementInitializer.DefaultArguments,
                                               boundCollectionElementInitializer.InvokedAsExtensionMethod);
                    }
                case BoundKind.FunctionPointerInvocation:
                    {
                        var boundFunctionPointerInvocation = (BoundFunctionPointerInvocation)containingExpression;
                        return DeriveArguments(boundFunctionPointerInvocation.FunctionPointer.Signature,
                                               boundFunctionPointerInvocation.Arguments,
                                               default,
                                               BitVector.Empty,
                                               false);
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(containingExpression.Kind);
            }
        }

        private ImmutableArray<IArgumentOperation> DeriveArguments(
            Symbol methodOrIndexer,
            ImmutableArray<BoundExpression> boundArguments,
            ImmutableArray<int> argumentsToParametersOpt,
            BitVector defaultArguments,
            bool invokedAsExtensionMethod = false)
        {
            // We can simply return empty array only if both parameters and boundArguments are empty, because:
            // - if only parameters is empty, there's error in code but we still need to return provided expression.
            // - if boundArguments is empty, then either there's error or we need to provide values for optional/param-array parameters.
            if (methodOrIndexer.GetParameters().IsDefaultOrEmpty && boundArguments.IsDefaultOrEmpty)
            {
                return ImmutableArray<IArgumentOperation>.Empty;
            }

            return MakeArgumentsInEvaluationOrder(
                 operationFactory: this,
                 arguments: boundArguments,
                 methodOrIndexer: methodOrIndexer,
                 argsToParamsOpt: argumentsToParametersOpt,
                 defaultArguments: defaultArguments,
                 invokedAsExtensionMethod: invokedAsExtensionMethod);
        }

        private static ImmutableArray<IArgumentOperation> MakeArgumentsInEvaluationOrder(
            CSharpOperationFactory operationFactory,
            ImmutableArray<BoundExpression> arguments,
            Symbol methodOrIndexer,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            bool invokedAsExtensionMethod)
        {
            // We need to do a fancy rewrite under the following circumstances:
            // (1) a params array is being used; we need to generate the array. 
            // (2) named arguments were provided out-of-order of the parameters.
            //
            // If neither of those are the case then we can just take an early out.

            if (LocalRewriter.CanSkipRewriting(arguments, methodOrIndexer, argsToParamsOpt, invokedAsExtensionMethod, true, out _))
            {
                // In this case, there's no named argument provided.
                // So we just return list of arguments as is.

                ImmutableArray<ParameterSymbol> parameters = methodOrIndexer.GetParameters();
                ArrayBuilder<IArgumentOperation> argumentsBuilder = ArrayBuilder<IArgumentOperation>.GetInstance(arguments.Length);

                int i = 0;
                for (; i < parameters.Length; ++i)
                {
                    var argumentKind = GetArgumentKind(arguments[i], ref defaultArguments, i);
                    argumentsBuilder.Add(operationFactory.CreateArgumentOperation(argumentKind, parameters[i].GetPublicSymbol(), arguments[i]));
                }

                // TODO: In case of __arglist, we will have more arguments than parameters, 
                //       set the parameter to null for __arglist argument for now.
                //       https://github.com/dotnet/roslyn/issues/19673
                for (; i < arguments.Length; ++i)
                {
                    var argumentKind = defaultArguments[i] ? ArgumentKind.DefaultValue : ArgumentKind.Explicit;
                    argumentsBuilder.Add(operationFactory.CreateArgumentOperation(argumentKind, null, arguments[i]));
                }

                Debug.Assert(methodOrIndexer.GetIsVararg() ^ parameters.Length == arguments.Length);

                return argumentsBuilder.ToImmutableAndFree();
            }

            return BuildArgumentsInEvaluationOrder(
                operationFactory,
                methodOrIndexer,
                argsToParamsOpt,
                defaultArguments,
                arguments);
        }

        private static ArgumentKind GetArgumentKind(BoundExpression argument, ref BitVector defaultArguments, int i)
        {
            ArgumentKind argumentKind;
            if (defaultArguments[i])
            {
                argumentKind = ArgumentKind.DefaultValue;
            }
            else if (argument.IsParamsArrayOrCollection)
            {
                argumentKind = argument.Type?.IsSZArray() == true ? ArgumentKind.ParamArray : ArgumentKind.ParamCollection;
            }
            else
            {
                argumentKind = ArgumentKind.Explicit;
            }

            return argumentKind;
        }

        // This fills in the arguments in evaluation order.
        private static ImmutableArray<IArgumentOperation> BuildArgumentsInEvaluationOrder(
            CSharpOperationFactory operationFactory,
            Symbol methodOrIndexer,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            ImmutableArray<BoundExpression> arguments)
        {
            ImmutableArray<ParameterSymbol> parameters = methodOrIndexer.GetParameters();

            ArrayBuilder<IArgumentOperation> argumentsInEvaluationBuilder = ArrayBuilder<IArgumentOperation>.GetInstance(parameters.Length);

            // First, fill in all the explicitly provided arguments.
            for (int a = 0; a < arguments.Length; ++a)
            {
                BoundExpression argument = arguments[a];

                int p = (!argsToParamsOpt.IsDefault) ? argsToParamsOpt[a] : a;
                var parameter = parameters[p];

                ArgumentKind kind = GetArgumentKind(argument, ref defaultArguments, a);

                argumentsInEvaluationBuilder.Add(operationFactory.CreateArgumentOperation(kind, parameter.GetPublicSymbol(), argument));
            }

            Debug.Assert(argumentsInEvaluationBuilder.All(static arg => arg is not null));
            return argumentsInEvaluationBuilder.ToImmutableAndFree();
        }

        internal static ImmutableArray<BoundNode> CreateInvalidChildrenFromArgumentsExpression(BoundNode? receiverOpt, ImmutableArray<BoundExpression> arguments, BoundExpression? additionalNodeOpt = null)
        {
            var builder = ArrayBuilder<BoundNode>.GetInstance();

            if (receiverOpt != null
               && (!receiverOpt.WasCompilerGenerated
                   || (receiverOpt.Kind != BoundKind.ThisReference
                      && receiverOpt.Kind != BoundKind.BaseReference
                      && receiverOpt.Kind != BoundKind.ObjectOrCollectionValuePlaceholder)))
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
                        isImplicit: true);

                // Find matching declaration for the current argument.
                PropertySymbol property = AnonymousTypeManager.GetAnonymousTypeProperty(type.GetSymbol<NamedTypeSymbol>(), i);
                BoundAnonymousPropertyDeclaration? anonymousProperty = getDeclaration(declarations, property, ref currentDeclarationIndex);
                if (anonymousProperty is null)
                {
                    // No matching declaration, synthesize a property reference to be assigned.
                    target = new PropertyReferenceOperation(
                        property.GetPublicSymbol(),
                        constrainedToType: null,
                        arguments: ImmutableArray<IArgumentOperation>.Empty,
                        instance,
                        semanticModel: _semanticModel,
                        syntax: value.Syntax,
                        type: property.Type.GetPublicSymbol(),
                        isImplicit: true);
                    isImplicitAssignment = true;
                }
                else
                {
                    target = new PropertyReferenceOperation(anonymousProperty.Property.GetPublicSymbol(),
                                                            constrainedToType: null,
                                                            ImmutableArray<IArgumentOperation>.Empty,
                                                            instance,
                                                            _semanticModel,
                                                            anonymousProperty.Syntax,
                                                            anonymousProperty.GetPublicTypeSymbol(),
                                                            anonymousProperty.WasCompilerGenerated);
                    isImplicitAssignment = isImplicit;
                }

                var assignmentSyntax = value.Syntax?.Parent ?? syntax;
                ITypeSymbol? assignmentType = target.Type;
                bool isRef = false;
                var assignment = new SimpleAssignmentOperation(isRef, target, value, _semanticModel, assignmentSyntax, assignmentType, value.GetConstantValue(), isImplicitAssignment);
                builder.Add(assignment);
            }

            Debug.Assert(currentDeclarationIndex == declarations.Length);
            return builder.ToImmutableAndFree();

            static BoundAnonymousPropertyDeclaration? getDeclaration(ImmutableArray<BoundAnonymousPropertyDeclaration> declarations, PropertySymbol currentProperty, ref int currentDeclarationIndex)
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

                    case CSharp.BinaryOperatorKind.UnsignedRightShift:
                        return BinaryOperatorKind.UnsignedRightShift;

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
