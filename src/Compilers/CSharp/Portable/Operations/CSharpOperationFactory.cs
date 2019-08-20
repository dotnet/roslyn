// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed partial class CSharpOperationFactory
    {
        private readonly ConcurrentDictionary<BoundNode, IOperation> _nodeMap =
            new ConcurrentDictionary<BoundNode, IOperation>(concurrencyLevel: 2, capacity: 10);

        private readonly Func<BoundNode, IOperation> _cachedCreateInternal;

        private readonly SemanticModel _semanticModel;

        public CSharpOperationFactory(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
            _cachedCreateInternal = CreateInternal;
        }

        public IOperation Create(BoundNode boundNode)
        {
            if (boundNode == null)
            {
                return null;
            }

            // implicit receiver can be shared between multiple bound nodes.
            // always return cloned one
            if (boundNode.Kind == BoundKind.ImplicitReceiver)
            {
                return OperationCloner.CloneOperation(CreateInternal(boundNode));
            }

            return _nodeMap.GetOrAdd(boundNode, _cachedCreateInternal);
        }

        public ImmutableArray<TOperation> CreateFromArray<TBoundNode, TOperation>(ImmutableArray<TBoundNode> boundNodes) where TBoundNode : BoundNode where TOperation : class, IOperation
        {
            if (boundNodes.IsDefault)
            {
                return ImmutableArray<TOperation>.Empty;
            }
            var builder = ArrayBuilder<TOperation>.GetInstance(boundNodes.Length);
            foreach (var node in boundNodes)
            {
                builder.AddIfNotNull((TOperation)Create(node));
            }
            return builder.ToImmutableAndFree();
        }

        internal IOperation CreateInternal(BoundNode boundNode)
        {
            switch (boundNode.Kind)
            {
                case BoundKind.DeconstructValuePlaceholder:
                    return CreateBoundDeconstructValuePlaceholderOperation((BoundDeconstructValuePlaceholder)boundNode);
                case BoundKind.DeconstructionAssignmentOperator:
                    return CreateBoundDeconstructionAssignmentOperator((BoundDeconstructionAssignmentOperator)boundNode);
                case BoundKind.Call:
                    return CreateBoundCallOperation((BoundCall)boundNode);
                case BoundKind.Local:
                    return CreateBoundLocalOperation((BoundLocal)boundNode);
                case BoundKind.FieldAccess:
                    return CreateBoundFieldAccessOperation((BoundFieldAccess)boundNode);
                case BoundKind.PropertyAccess:
                    return CreateBoundPropertyAccessOperation((BoundPropertyAccess)boundNode);
                case BoundKind.IndexerAccess:
                    return CreateBoundIndexerAccessOperation((BoundIndexerAccess)boundNode);
                case BoundKind.EventAccess:
                    return CreateBoundEventAccessOperation((BoundEventAccess)boundNode);
                case BoundKind.EventAssignmentOperator:
                    return CreateBoundEventAssignmentOperatorOperation((BoundEventAssignmentOperator)boundNode);
                case BoundKind.Parameter:
                    return CreateBoundParameterOperation((BoundParameter)boundNode);
                case BoundKind.Literal:
                    return CreateBoundLiteralOperation((BoundLiteral)boundNode);
                case BoundKind.DynamicInvocation:
                    return CreateBoundDynamicInvocationExpressionOperation((BoundDynamicInvocation)boundNode);
                case BoundKind.DynamicIndexerAccess:
                    return CreateBoundDynamicIndexerAccessExpressionOperation((BoundDynamicIndexerAccess)boundNode);
                case BoundKind.ObjectCreationExpression:
                    return CreateBoundObjectCreationExpressionOperation((BoundObjectCreationExpression)boundNode);
                case BoundKind.DynamicObjectCreationExpression:
                    return CreateBoundDynamicObjectCreationExpressionOperation((BoundDynamicObjectCreationExpression)boundNode);
                case BoundKind.ObjectInitializerExpression:
                    return CreateBoundObjectInitializerExpressionOperation((BoundObjectInitializerExpression)boundNode);
                case BoundKind.CollectionInitializerExpression:
                    return CreateBoundCollectionInitializerExpressionOperation((BoundCollectionInitializerExpression)boundNode);
                case BoundKind.ObjectInitializerMember:
                    return CreateBoundObjectInitializerMemberOperation((BoundObjectInitializerMember)boundNode);
                case BoundKind.CollectionElementInitializer:
                    return CreateBoundCollectionElementInitializerOperation((BoundCollectionElementInitializer)boundNode);
                case BoundKind.DynamicObjectInitializerMember:
                    return CreateBoundDynamicObjectInitializerMemberOperation((BoundDynamicObjectInitializerMember)boundNode);
                case BoundKind.DynamicMemberAccess:
                    return CreateBoundDynamicMemberAccessOperation((BoundDynamicMemberAccess)boundNode);
                case BoundKind.DynamicCollectionElementInitializer:
                    return CreateBoundDynamicCollectionElementInitializerOperation((BoundDynamicCollectionElementInitializer)boundNode);
                case BoundKind.UnboundLambda:
                    return CreateUnboundLambdaOperation((UnboundLambda)boundNode);
                case BoundKind.Lambda:
                    return CreateBoundLambdaOperation((BoundLambda)boundNode);
                case BoundKind.Conversion:
                    return CreateBoundConversionOperation((BoundConversion)boundNode);
                case BoundKind.AsOperator:
                    return CreateBoundAsOperatorOperation((BoundAsOperator)boundNode);
                case BoundKind.IsOperator:
                    return CreateBoundIsOperatorOperation((BoundIsOperator)boundNode);
                case BoundKind.SizeOfOperator:
                    return CreateBoundSizeOfOperatorOperation((BoundSizeOfOperator)boundNode);
                case BoundKind.TypeOfOperator:
                    return CreateBoundTypeOfOperatorOperation((BoundTypeOfOperator)boundNode);
                case BoundKind.ArrayCreation:
                    return CreateBoundArrayCreationOperation((BoundArrayCreation)boundNode);
                case BoundKind.ArrayInitialization:
                    return CreateBoundArrayInitializationOperation((BoundArrayInitialization)boundNode);
                case BoundKind.DefaultExpression:
                    return CreateBoundDefaultExpressionOperation((BoundDefaultExpression)boundNode);
                case BoundKind.BaseReference:
                    return CreateBoundBaseReferenceOperation((BoundBaseReference)boundNode);
                case BoundKind.ThisReference:
                    return CreateBoundThisReferenceOperation((BoundThisReference)boundNode);
                case BoundKind.AssignmentOperator:
                    return CreateBoundAssignmentOperatorOrMemberInitializerOperation((BoundAssignmentOperator)boundNode);
                case BoundKind.CompoundAssignmentOperator:
                    return CreateBoundCompoundAssignmentOperatorOperation((BoundCompoundAssignmentOperator)boundNode);
                case BoundKind.IncrementOperator:
                    return CreateBoundIncrementOperatorOperation((BoundIncrementOperator)boundNode);
                case BoundKind.BadExpression:
                    return CreateBoundBadExpressionOperation((BoundBadExpression)boundNode);
                case BoundKind.NewT:
                    return CreateBoundNewTOperation((BoundNewT)boundNode);
                case BoundKind.NoPiaObjectCreationExpression:
                    return CreateNoPiaObjectCreationExpressionOperation((BoundNoPiaObjectCreationExpression)boundNode);
                case BoundKind.UnaryOperator:
                    return CreateBoundUnaryOperatorOperation((BoundUnaryOperator)boundNode);
                case BoundKind.BinaryOperator:
                    return CreateBoundBinaryOperatorOperation((BoundBinaryOperator)boundNode);
                case BoundKind.UserDefinedConditionalLogicalOperator:
                    return CreateBoundUserDefinedConditionalLogicalOperator((BoundUserDefinedConditionalLogicalOperator)boundNode);
                case BoundKind.TupleBinaryOperator:
                    return CreateBoundTupleBinaryOperatorOperation((BoundTupleBinaryOperator)boundNode);
                case BoundKind.ConditionalOperator:
                    return CreateBoundConditionalOperatorOperation((BoundConditionalOperator)boundNode);
                case BoundKind.NullCoalescingOperator:
                    return CreateBoundNullCoalescingOperatorOperation((BoundNullCoalescingOperator)boundNode);
                case BoundKind.AwaitExpression:
                    return CreateBoundAwaitExpressionOperation((BoundAwaitExpression)boundNode);
                case BoundKind.ArrayAccess:
                    return CreateBoundArrayAccessOperation((BoundArrayAccess)boundNode);
                case BoundKind.NameOfOperator:
                    return CreateBoundNameOfOperatorOperation((BoundNameOfOperator)boundNode);
                case BoundKind.ThrowExpression:
                    return CreateBoundThrowExpressionOperation((BoundThrowExpression)boundNode);
                case BoundKind.AddressOfOperator:
                    return CreateBoundAddressOfOperatorOperation((BoundAddressOfOperator)boundNode);
                case BoundKind.ImplicitReceiver:
                    return CreateBoundImplicitReceiverOperation((BoundImplicitReceiver)boundNode);
                case BoundKind.ConditionalAccess:
                    return CreateBoundConditionalAccessOperation((BoundConditionalAccess)boundNode);
                case BoundKind.ConditionalReceiver:
                    return CreateBoundConditionalReceiverOperation((BoundConditionalReceiver)boundNode);
                case BoundKind.FieldEqualsValue:
                    return CreateBoundFieldEqualsValueOperation((BoundFieldEqualsValue)boundNode);
                case BoundKind.PropertyEqualsValue:
                    return CreateBoundPropertyEqualsValueOperation((BoundPropertyEqualsValue)boundNode);
                case BoundKind.ParameterEqualsValue:
                    return CreateBoundParameterEqualsValueOperation((BoundParameterEqualsValue)boundNode);
                case BoundKind.Block:
                    return CreateBoundBlockOperation((BoundBlock)boundNode);
                case BoundKind.ContinueStatement:
                    return CreateBoundContinueStatementOperation((BoundContinueStatement)boundNode);
                case BoundKind.BreakStatement:
                    return CreateBoundBreakStatementOperation((BoundBreakStatement)boundNode);
                case BoundKind.YieldBreakStatement:
                    return CreateBoundYieldBreakStatementOperation((BoundYieldBreakStatement)boundNode);
                case BoundKind.GotoStatement:
                    return CreateBoundGotoStatementOperation((BoundGotoStatement)boundNode);
                case BoundKind.NoOpStatement:
                    return CreateBoundNoOpStatementOperation((BoundNoOpStatement)boundNode);
                case BoundKind.IfStatement:
                    return CreateBoundIfStatementOperation((BoundIfStatement)boundNode);
                case BoundKind.WhileStatement:
                    return CreateBoundWhileStatementOperation((BoundWhileStatement)boundNode);
                case BoundKind.DoStatement:
                    return CreateBoundDoStatementOperation((BoundDoStatement)boundNode);
                case BoundKind.ForStatement:
                    return CreateBoundForStatementOperation((BoundForStatement)boundNode);
                case BoundKind.ForEachStatement:
                    return CreateBoundForEachStatementOperation((BoundForEachStatement)boundNode);
                case BoundKind.TryStatement:
                    return CreateBoundTryStatementOperation((BoundTryStatement)boundNode);
                case BoundKind.CatchBlock:
                    return CreateBoundCatchBlockOperation((BoundCatchBlock)boundNode);
                case BoundKind.FixedStatement:
                    return CreateBoundFixedStatementOperation((BoundFixedStatement)boundNode);
                case BoundKind.UsingStatement:
                    return CreateBoundUsingStatementOperation((BoundUsingStatement)boundNode);
                case BoundKind.ThrowStatement:
                    return CreateBoundThrowStatementOperation((BoundThrowStatement)boundNode);
                case BoundKind.ReturnStatement:
                    return CreateBoundReturnStatementOperation((BoundReturnStatement)boundNode);
                case BoundKind.YieldReturnStatement:
                    return CreateBoundYieldReturnStatementOperation((BoundYieldReturnStatement)boundNode);
                case BoundKind.LockStatement:
                    return CreateBoundLockStatementOperation((BoundLockStatement)boundNode);
                case BoundKind.BadStatement:
                    return CreateBoundBadStatementOperation((BoundBadStatement)boundNode);
                case BoundKind.LocalDeclaration:
                    return CreateBoundLocalDeclarationOperation((BoundLocalDeclaration)boundNode);
                case BoundKind.MultipleLocalDeclarations:
                    return CreateBoundMultipleLocalDeclarationsOperation((BoundMultipleLocalDeclarations)boundNode);
                case BoundKind.LabelStatement:
                    return CreateBoundLabelStatementOperation((BoundLabelStatement)boundNode);
                case BoundKind.LabeledStatement:
                    return CreateBoundLabeledStatementOperation((BoundLabeledStatement)boundNode);
                case BoundKind.ExpressionStatement:
                    return CreateBoundExpressionStatementOperation((BoundExpressionStatement)boundNode);
                case BoundKind.TupleLiteral:
                    return CreateBoundTupleLiteralOperation((BoundTupleLiteral)boundNode);
                case BoundKind.ConvertedTupleLiteral:
                    return CreateBoundConvertedTupleLiteralOperation((BoundConvertedTupleLiteral)boundNode);
                case BoundKind.InterpolatedString:
                    return CreateBoundInterpolatedStringExpressionOperation((BoundInterpolatedString)boundNode);
                case BoundKind.StringInsert:
                    return CreateBoundInterpolationOperation((BoundStringInsert)boundNode);
                case BoundKind.LocalFunctionStatement:
                    return CreateBoundLocalFunctionStatementOperation((BoundLocalFunctionStatement)boundNode);
                case BoundKind.AnonymousObjectCreationExpression:
                    return CreateBoundAnonymousObjectCreationExpressionOperation((BoundAnonymousObjectCreationExpression)boundNode);
                case BoundKind.AnonymousPropertyDeclaration:
                    throw ExceptionUtilities.Unreachable;
                case BoundKind.ConstantPattern:
                    return CreateBoundConstantPatternOperation((BoundConstantPattern)boundNode);
                case BoundKind.DeclarationPattern:
                    return CreateBoundDeclarationPatternOperation((BoundDeclarationPattern)boundNode);
                case BoundKind.RecursivePattern:
                    return CreateBoundRecursivePatternOperation((BoundRecursivePattern)boundNode);
                case BoundKind.ITuplePattern:
                    return CreateBoundRecursivePatternOperation((BoundITuplePattern)boundNode);
                case BoundKind.DiscardPattern:
                    return CreateBoundDiscardPatternOperation((BoundDiscardPattern)boundNode);
                case BoundKind.SwitchStatement:
                    return CreateBoundSwitchStatementOperation((BoundSwitchStatement)boundNode);
                case BoundKind.SwitchLabel:
                    return CreateBoundSwitchLabelOperation((BoundSwitchLabel)boundNode);
                case BoundKind.IsPatternExpression:
                    return CreateBoundIsPatternExpressionOperation((BoundIsPatternExpression)boundNode);
                case BoundKind.QueryClause:
                    return CreateBoundQueryClauseOperation((BoundQueryClause)boundNode);
                case BoundKind.DelegateCreationExpression:
                    return CreateBoundDelegateCreationExpressionOperation((BoundDelegateCreationExpression)boundNode);
                case BoundKind.RangeVariable:
                    return CreateBoundRangeVariableOperation((BoundRangeVariable)boundNode);
                case BoundKind.ConstructorMethodBody:
                    return CreateConstructorBodyOperation((BoundConstructorMethodBody)boundNode);
                case BoundKind.NonConstructorMethodBody:
                    return CreateMethodBodyOperation((BoundNonConstructorMethodBody)boundNode);
                case BoundKind.DiscardExpression:
                    return CreateBoundDiscardExpressionOperation((BoundDiscardExpression)boundNode);
                case BoundKind.NullCoalescingAssignmentOperator:
                    return CreateBoundNullCoalescingAssignmentOperatorOperation((BoundNullCoalescingAssignmentOperator)boundNode);
                case BoundKind.FromEndIndexExpression:
                    return CreateFromEndIndexExpressionOperation((BoundFromEndIndexExpression)boundNode);
                case BoundKind.RangeExpression:
                    return CreateRangeExpressionOperation((BoundRangeExpression)boundNode);
                case BoundKind.SwitchSection:
                    return CreateBoundSwitchSectionOperation((BoundSwitchSection)boundNode);
                case BoundKind.UnconvertedSwitchExpression:
                case BoundKind.ConvertedSwitchExpression:
                    return CreateBoundSwitchExpressionOperation((BoundSwitchExpression)boundNode);
                case BoundKind.SwitchExpressionArm:
                    return CreateBoundSwitchExpressionArmOperation((BoundSwitchExpressionArm)boundNode);
                case BoundKind.UsingLocalDeclarations:
                    return CreateUsingLocalDeclarationsOperation((BoundUsingLocalDeclarations)boundNode);

                case BoundKind.Attribute:
                case BoundKind.ArgList:
                case BoundKind.ArgListOperator:
                case BoundKind.ConvertedStackAllocExpression:
                case BoundKind.FixedLocalCollectionInitializer:
                case BoundKind.GlobalStatementInitializer:
                case BoundKind.HostObjectMemberReference:
                case BoundKind.MakeRefOperator:
                case BoundKind.MethodGroup:
                case BoundKind.NamespaceExpression:
                case BoundKind.PointerElementAccess:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.PreviousSubmissionReference:
                case BoundKind.RefTypeOperator:
                case BoundKind.RefValueOperator:
                case BoundKind.Sequence:
                case BoundKind.StackAllocArrayCreation:
                case BoundKind.TypeExpression:
                case BoundKind.TypeOrValueExpression:
                case BoundKind.IndexOrRangePatternIndexerAccess:

                    Optional<object> constantValue = ConvertToOptional((boundNode as BoundExpression)?.ConstantValue);
                    bool isImplicit = boundNode.WasCompilerGenerated;

                    if (!isImplicit)
                    {
                        switch (boundNode.Kind)
                        {
                            case BoundKind.FixedLocalCollectionInitializer:
                                isImplicit = true;
                                break;
                        }
                    }

                    return new CSharpLazyNoneOperation(this, boundNode, _semanticModel, boundNode.Syntax, constantValue, isImplicit: isImplicit);

                default:
                    throw ExceptionUtilities.UnexpectedValue(boundNode.Kind);
            }
        }

        private IMethodBodyOperation CreateMethodBodyOperation(BoundNonConstructorMethodBody boundNode)
        {
            return new CSharpLazyMethodBodyOperation(this, boundNode, _semanticModel, boundNode.Syntax);
        }

        private IConstructorBodyOperation CreateConstructorBodyOperation(BoundConstructorMethodBody boundNode)
        {
            return new CSharpLazyConstructorBodyOperation(this, boundNode, boundNode.Locals.As<ILocalSymbol>(), _semanticModel, boundNode.Syntax);
        }

        internal ImmutableArray<IOperation> GetIOperationChildren(BoundNode boundNode)
        {
            //TODO: We can get rid of this once we implement UsingLocalDeclaration operations correctly, instead of just using an operationNone.
            //For now we return a single child consisting of the using declaration parsed as if it were a standard variable declaration.
            //See: https://github.com/dotnet/roslyn/issues/32100
            if (boundNode is BoundUsingLocalDeclarations boundUsingLocalDeclarations)
            {
                return ImmutableArray.Create<IOperation>(CreateBoundMultipleLocalDeclarationsOperation(boundUsingLocalDeclarations));
            }

            var boundNodeWithChildren = (IBoundNodeWithIOperationChildren)boundNode;
            var children = boundNodeWithChildren.Children;
            if (children.IsDefaultOrEmpty)
            {
                return ImmutableArray<IOperation>.Empty;
            }

            var builder = ArrayBuilder<IOperation>.GetInstance(children.Length);
            foreach (BoundNode childNode in children)
            {
                IOperation operation = Create(childNode);
                if (operation == null)
                {
                    continue;
                }

                builder.Add(operation);
            }

            return builder.ToImmutableAndFree();
        }

        internal ImmutableArray<IVariableDeclaratorOperation> CreateVariableDeclarator(BoundNode declaration, SyntaxNode declarationSyntax)
        {
            switch (declaration.Kind)
            {
                case BoundKind.LocalDeclaration:
                    {
                        return ImmutableArray.Create(CreateVariableDeclaratorInternal((BoundLocalDeclaration)declaration, (declarationSyntax as VariableDeclarationSyntax)?.Variables[0] ?? declarationSyntax));
                    }
                case BoundKind.MultipleLocalDeclarations:
                case BoundKind.UsingLocalDeclarations:
                    {
                        var multipleDeclaration = (BoundMultipleLocalDeclarations)declaration;
                        var builder = ArrayBuilder<IVariableDeclaratorOperation>.GetInstance(multipleDeclaration.LocalDeclarations.Length);
                        foreach (var decl in multipleDeclaration.LocalDeclarations)
                        {
                            builder.Add((IVariableDeclaratorOperation)_nodeMap.GetOrAdd(decl, CreateVariableDeclaratorInternal(decl, decl.Syntax)));
                        }
                        return builder.ToImmutableAndFree();
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(declaration.Kind);
            }
        }

        private IPlaceholderOperation CreateBoundDeconstructValuePlaceholderOperation(BoundDeconstructValuePlaceholder boundDeconstructValuePlaceholder)
        {
            SyntaxNode syntax = boundDeconstructValuePlaceholder.Syntax;
            ITypeSymbol type = boundDeconstructValuePlaceholder.Type;
            Optional<object> constantValue = ConvertToOptional(boundDeconstructValuePlaceholder.ConstantValue);
            bool isImplicit = boundDeconstructValuePlaceholder.WasCompilerGenerated;
            return new PlaceholderOperation(PlaceholderKind.Unspecified, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDeconstructionAssignmentOperation CreateBoundDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator boundDeconstructionAssignmentOperator)
        {
            SyntaxNode syntax = boundDeconstructionAssignmentOperator.Syntax;
            ITypeSymbol type = boundDeconstructionAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundDeconstructionAssignmentOperator.ConstantValue);
            bool isImplicit = boundDeconstructionAssignmentOperator.WasCompilerGenerated;
            return new CSharpLazyDeconstructionAssignmentOperation(this, boundDeconstructionAssignmentOperator, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundCallOperation(BoundCall boundCall)
        {
            MethodSymbol targetMethod = boundCall.Method;
            SyntaxNode syntax = boundCall.Syntax;
            ITypeSymbol type = boundCall.Type;
            Optional<object> constantValue = ConvertToOptional(boundCall.ConstantValue);
            bool isImplicit = boundCall.WasCompilerGenerated;

            if (!boundCall.OriginalMethodsOpt.IsDefault || IsMethodInvalid(boundCall.ResultKind, targetMethod))
            {
                return new CSharpLazyInvalidOperation(this, boundCall, _semanticModel, syntax, type, constantValue, isImplicit);
            }

            bool isVirtual = IsCallVirtual(targetMethod, boundCall.ReceiverOpt);
            return new CSharpLazyInvocationOperation(this, boundCall, targetMethod, isVirtual, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal ImmutableArray<IOperation> CreateIgnoredDimensions(BoundNode declaration, SyntaxNode declarationSyntax)
        {
            switch (declaration.Kind)
            {
                case BoundKind.LocalDeclaration:
                    {
                        return CreateFromArray<BoundExpression, IOperation>(((BoundLocalDeclaration)declaration).DeclaredTypeOpt.BoundDimensionsOpt);
                    }
                case BoundKind.MultipleLocalDeclarations:
                case BoundKind.UsingLocalDeclarations:
                    {
                        var declarations = ((BoundMultipleLocalDeclarations)declaration).LocalDeclarations;
                        var dimensions = declarations.Length > 0
                            ? declarations[0].DeclaredTypeOpt.BoundDimensionsOpt
                            : ImmutableArray<BoundExpression>.Empty;
                        return CreateFromArray<BoundExpression, IOperation>(dimensions);
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(declaration.Kind);
            }
        }

        internal IOperation CreateBoundLocalOperation(BoundLocal boundLocal, bool createDeclaration = true)
        {
            ILocalSymbol local = boundLocal.LocalSymbol;
            bool isDeclaration = boundLocal.DeclarationKind != BoundLocalDeclarationKind.None;
            SyntaxNode syntax = boundLocal.Syntax;
            ITypeSymbol type = boundLocal.Type;
            Optional<object> constantValue = ConvertToOptional(boundLocal.ConstantValue);
            bool isImplicit = boundLocal.WasCompilerGenerated;
            if (isDeclaration && syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                syntax = declarationExpressionSyntax.Designation;
                if (createDeclaration)
                {
                    return new CSharpLazyDeclarationExpressionOperation(this, boundLocal, _semanticModel, declarationExpressionSyntax, type, constantValue: default, isImplicit: false);
                }
            }
            return new LocalReferenceOperation(local, isDeclaration, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal IOperation CreateBoundFieldAccessOperation(BoundFieldAccess boundFieldAccess, bool createDeclaration = true)
        {
            IFieldSymbol field = boundFieldAccess.FieldSymbol;
            bool isDeclaration = boundFieldAccess.IsDeclaration;
            BoundNode instance = boundFieldAccess.ReceiverOpt;
            SyntaxNode syntax = boundFieldAccess.Syntax;
            ITypeSymbol type = boundFieldAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundFieldAccess.ConstantValue);
            bool isImplicit = boundFieldAccess.WasCompilerGenerated;
            if (isDeclaration && syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                syntax = declarationExpressionSyntax.Designation;

                if (createDeclaration)
                {
                    return new CSharpLazyDeclarationExpressionOperation(this, boundFieldAccess, _semanticModel, declarationExpressionSyntax, type, constantValue: default, isImplicit: false);
                }
            }
            return new CSharpLazyFieldReferenceOperation(this, instance, field, isDeclaration, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal IOperation CreateBoundPropertyReferenceInstance(BoundNode boundNode)
        {
            switch (boundNode)
            {
                case BoundPropertyAccess boundPropertyAccess:
                    return CreateReceiverOperation(boundPropertyAccess.ReceiverOpt, boundPropertyAccess.PropertySymbol);
                case BoundObjectInitializerMember boundObjectInitializerMember:
                    return boundObjectInitializerMember.MemberSymbol?.IsStatic == true ?
                        null :
                        CreateImplicitReceiver(boundObjectInitializerMember.Syntax, boundObjectInitializerMember.ReceiverType);
                case BoundIndexerAccess boundIndexerAccess:
                    return CreateReceiverOperation(boundIndexerAccess.ReceiverOpt, boundIndexerAccess.ExpressionSymbol);
                default:
                    throw ExceptionUtilities.UnexpectedValue(boundNode.Kind);
            }
        }

        internal ImmutableArray<IArgumentOperation> CreateBoundPropertyReferenceArguments(BoundNode boundNode, bool isObjectOrCollectionInitializerMember)
        {
            switch (boundNode)
            {
                case BoundObjectInitializerMember boundObjectInitializerMember:
                    if (boundObjectInitializerMember.Arguments.IsEmpty)
                    {
                        return ImmutableArray<IArgumentOperation>.Empty;
                    }
                    else
                    {
                        return DeriveArguments(boundObjectInitializerMember, isObjectOrCollectionInitializerMember);
                    }

                case BoundIndexerAccess boundIndexerAccess:
                    return DeriveArguments(boundIndexerAccess);

                case BoundPropertyAccess _:
                    return ImmutableArray<IArgumentOperation>.Empty;

                default:
                    throw ExceptionUtilities.UnexpectedValue(boundNode.Kind);
            }
        }

        private IPropertyReferenceOperation CreateBoundPropertyAccessOperation(BoundPropertyAccess boundPropertyAccess)
        {
            bool isObjectOrCollectionInitializer = false;
            IPropertySymbol property = boundPropertyAccess.PropertySymbol;
            SyntaxNode syntax = boundPropertyAccess.Syntax;
            ITypeSymbol type = boundPropertyAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundPropertyAccess.ConstantValue);
            bool isImplicit = boundPropertyAccess.WasCompilerGenerated;
            return new CSharpLazyPropertyReferenceOperation(this, boundPropertyAccess, isObjectOrCollectionInitializer, property, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundIndexerAccessOperation(BoundIndexerAccess boundIndexerAccess)
        {
            bool isObjectOrCollectionInitializer = false;
            PropertySymbol property = boundIndexerAccess.Indexer;
            SyntaxNode syntax = boundIndexerAccess.Syntax;
            ITypeSymbol type = boundIndexerAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundIndexerAccess.ConstantValue);
            bool isImplicit = boundIndexerAccess.WasCompilerGenerated;

            MethodSymbol accessor = boundIndexerAccess.UseSetterForDefaultArgumentGeneration
                ? property.GetOwnOrInheritedSetMethod()
                : property.GetOwnOrInheritedGetMethod();

            if (!boundIndexerAccess.OriginalIndexersOpt.IsDefault || boundIndexerAccess.ResultKind == LookupResultKind.OverloadResolutionFailure || accessor == null || accessor.OriginalDefinition is ErrorMethodSymbol)
            {
                return new CSharpLazyInvalidOperation(this, boundIndexerAccess, _semanticModel, syntax, type, constantValue, isImplicit);
            }

            return new CSharpLazyPropertyReferenceOperation(this, boundIndexerAccess, isObjectOrCollectionInitializer, property, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IEventReferenceOperation CreateBoundEventAccessOperation(BoundEventAccess boundEventAccess)
        {
            IEventSymbol @event = boundEventAccess.EventSymbol;
            BoundNode instance = boundEventAccess.ReceiverOpt;
            SyntaxNode syntax = boundEventAccess.Syntax;
            ITypeSymbol type = boundEventAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundEventAccess.ConstantValue);
            bool isImplicit = boundEventAccess.WasCompilerGenerated;
            return new CSharpLazyEventReferenceOperation(this, instance, @event, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IEventAssignmentOperation CreateBoundEventAssignmentOperatorOperation(BoundEventAssignmentOperator boundEventAssignmentOperator)
        {
            SyntaxNode syntax = boundEventAssignmentOperator.Syntax;
            bool adds = boundEventAssignmentOperator.IsAddition;
            ITypeSymbol type = boundEventAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundEventAssignmentOperator.ConstantValue);
            bool isImplicit = boundEventAssignmentOperator.WasCompilerGenerated;
            return new CSharpLazyEventAssignmentOperation(this, boundEventAssignmentOperator, adds, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IParameterReferenceOperation CreateBoundParameterOperation(BoundParameter boundParameter)
        {
            IParameterSymbol parameter = boundParameter.ParameterSymbol;
            SyntaxNode syntax = boundParameter.Syntax;
            ITypeSymbol type = boundParameter.Type;
            Optional<object> constantValue = ConvertToOptional(boundParameter.ConstantValue);
            bool isImplicit = boundParameter.WasCompilerGenerated;
            return new ParameterReferenceOperation(parameter, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal ILiteralOperation CreateBoundLiteralOperation(BoundLiteral boundLiteral, bool @implicit = false)
        {
            SyntaxNode syntax = boundLiteral.Syntax;
            ITypeSymbol type = boundLiteral.Type;
            Optional<object> constantValue = ConvertToOptional(boundLiteral.ConstantValue);
            bool isImplicit = boundLiteral.WasCompilerGenerated || @implicit;
            return new LiteralOperation(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IAnonymousObjectCreationOperation CreateBoundAnonymousObjectCreationExpressionOperation(BoundAnonymousObjectCreationExpression boundAnonymousObjectCreationExpression)
        {
            SyntaxNode syntax = boundAnonymousObjectCreationExpression.Syntax;
            ITypeSymbol type = boundAnonymousObjectCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundAnonymousObjectCreationExpression.ConstantValue);
            bool isImplicit = boundAnonymousObjectCreationExpression.WasCompilerGenerated;
            return new CSharpLazyAnonymousObjectCreationOperation(this, boundAnonymousObjectCreationExpression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundObjectCreationExpressionOperation(BoundObjectCreationExpression boundObjectCreationExpression)
        {
            MethodSymbol constructor = boundObjectCreationExpression.Constructor;
            SyntaxNode syntax = boundObjectCreationExpression.Syntax;
            ITypeSymbol type = boundObjectCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundObjectCreationExpression.ConstantValue);
            bool isImplicit = boundObjectCreationExpression.WasCompilerGenerated;

            if (boundObjectCreationExpression.ResultKind == LookupResultKind.OverloadResolutionFailure || constructor == null || constructor.OriginalDefinition is ErrorMethodSymbol)
            {
                return new CSharpLazyInvalidOperation(this, boundObjectCreationExpression, _semanticModel, syntax, type, constantValue, isImplicit);
            }
            else if (boundObjectCreationExpression.Type.IsAnonymousType)
            {
                // Workaround for https://github.com/dotnet/roslyn/issues/28157
                Debug.Assert(isImplicit);
                return new CSharpLazyAnonymousObjectCreationOperation(this, boundObjectCreationExpression, _semanticModel, syntax, type, constantValue, isImplicit);
            }

            return new CSharpLazyObjectCreationOperation(this, boundObjectCreationExpression, constructor, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDynamicObjectCreationOperation CreateBoundDynamicObjectCreationExpressionOperation(BoundDynamicObjectCreationExpression boundDynamicObjectCreationExpression)
        {
            ImmutableArray<string> argumentNames = boundDynamicObjectCreationExpression.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicObjectCreationExpression.ArgumentRefKindsOpt.NullToEmpty();
            SyntaxNode syntax = boundDynamicObjectCreationExpression.Syntax;
            ITypeSymbol type = boundDynamicObjectCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundDynamicObjectCreationExpression.ConstantValue);
            bool isImplicit = boundDynamicObjectCreationExpression.WasCompilerGenerated;
            return new CSharpLazyDynamicObjectCreationOperation(this, boundDynamicObjectCreationExpression, argumentNames, argumentRefKinds, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal IOperation CreateBoundDynamicInvocationExpressionReceiver(BoundNode receiver)
        {
            switch (receiver)
            {
                case BoundImplicitReceiver implicitReceiver:
                    return CreateBoundDynamicMemberAccessOperation(implicitReceiver, typeArgumentsOpt: ImmutableArray<TypeSymbol>.Empty, memberName: "Add",
                                                                   implicitReceiver.Syntax, type: null, value: default, isImplicit: true);

                case BoundMethodGroup methodGroup:
                    return CreateBoundDynamicMemberAccessOperation(methodGroup.ReceiverOpt, TypeMap.AsTypeSymbols(methodGroup.TypeArgumentsOpt), methodGroup.Name,
                                                                   methodGroup.Syntax, methodGroup.Type, methodGroup.ConstantValue, methodGroup.WasCompilerGenerated);

                default:
                    return Create(receiver);
            }
        }

        private IDynamicInvocationOperation CreateBoundDynamicInvocationExpressionOperation(BoundDynamicInvocation boundDynamicInvocation)
        {
            ImmutableArray<string> argumentNames = boundDynamicInvocation.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicInvocation.ArgumentRefKindsOpt.NullToEmpty();
            SyntaxNode syntax = boundDynamicInvocation.Syntax;
            ITypeSymbol type = boundDynamicInvocation.Type;
            Optional<object> constantValue = ConvertToOptional(boundDynamicInvocation.ConstantValue);
            bool isImplicit = boundDynamicInvocation.WasCompilerGenerated;
            return new CSharpLazyDynamicInvocationOperation(this, boundDynamicInvocation, argumentNames, argumentRefKinds, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal IOperation CreateBoundDynamicIndexerAccessExpressionReceiver(BoundExpression indexer)
        {
            switch (indexer)
            {
                case BoundDynamicIndexerAccess boundDynamicIndexerAccess:
                    return Create(boundDynamicIndexerAccess.ReceiverOpt);

                case BoundObjectInitializerMember boundObjectInitializerMember:
                    return CreateImplicitReceiver(boundObjectInitializerMember.Syntax, boundObjectInitializerMember.ReceiverType);

                default:
                    throw ExceptionUtilities.UnexpectedValue(indexer.Kind);
            }
        }

        internal ImmutableArray<IOperation> CreateBoundDynamicIndexerAccessArguments(BoundExpression indexer)
        {
            switch (indexer)
            {
                case BoundDynamicIndexerAccess boundDynamicAccess:
                    return CreateFromArray<BoundExpression, IOperation>(boundDynamicAccess.Arguments);

                case BoundObjectInitializerMember boundObjectInitializerMember:
                    return CreateFromArray<BoundExpression, IOperation>(boundObjectInitializerMember.Arguments);

                default:
                    throw ExceptionUtilities.UnexpectedValue(indexer.Kind);
            }
        }

        private IDynamicIndexerAccessOperation CreateBoundDynamicIndexerAccessExpressionOperation(BoundDynamicIndexerAccess boundDynamicIndexerAccess)
        {
            ImmutableArray<string> argumentNames = boundDynamicIndexerAccess.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicIndexerAccess.ArgumentRefKindsOpt.NullToEmpty();
            SyntaxNode syntax = boundDynamicIndexerAccess.Syntax;
            ITypeSymbol type = boundDynamicIndexerAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundDynamicIndexerAccess.ConstantValue);
            bool isImplicit = boundDynamicIndexerAccess.WasCompilerGenerated;
            return new CSharpLazyDynamicIndexerAccessOperation(this, boundDynamicIndexerAccess, argumentNames, argumentRefKinds, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IObjectOrCollectionInitializerOperation CreateBoundObjectInitializerExpressionOperation(BoundObjectInitializerExpression boundObjectInitializerExpression)
        {
            SyntaxNode syntax = boundObjectInitializerExpression.Syntax;
            ITypeSymbol type = boundObjectInitializerExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundObjectInitializerExpression.ConstantValue);
            bool isImplicit = boundObjectInitializerExpression.WasCompilerGenerated;
            return new CSharpLazyObjectOrCollectionInitializerOperation(this, boundObjectInitializerExpression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IObjectOrCollectionInitializerOperation CreateBoundCollectionInitializerExpressionOperation(BoundCollectionInitializerExpression boundCollectionInitializerExpression)
        {
            SyntaxNode syntax = boundCollectionInitializerExpression.Syntax;
            ITypeSymbol type = boundCollectionInitializerExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundCollectionInitializerExpression.ConstantValue);
            bool isImplicit = boundCollectionInitializerExpression.WasCompilerGenerated;
            return new CSharpLazyObjectOrCollectionInitializerOperation(this, boundCollectionInitializerExpression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundObjectInitializerMemberOperation(BoundObjectInitializerMember boundObjectInitializerMember, bool isObjectOrCollectionInitializer = false)
        {
            Symbol memberSymbol = boundObjectInitializerMember.MemberSymbol;
            SyntaxNode syntax = boundObjectInitializerMember.Syntax;
            ITypeSymbol type = boundObjectInitializerMember.Type;
            Optional<object> constantValue = ConvertToOptional(boundObjectInitializerMember.ConstantValue);
            bool isImplicit = boundObjectInitializerMember.WasCompilerGenerated;

            if ((object)memberSymbol == null)
            {
                Debug.Assert(boundObjectInitializerMember.Type.IsDynamic());

                ImmutableArray<string> argumentNames = boundObjectInitializerMember.ArgumentNamesOpt.NullToEmpty();
                ImmutableArray<RefKind> argumentRefKinds = boundObjectInitializerMember.ArgumentRefKindsOpt.NullToEmpty();
                return new CSharpLazyDynamicIndexerAccessOperation(this, boundObjectInitializerMember, argumentNames, argumentRefKinds, _semanticModel, syntax, type, constantValue, isImplicit);
            }

            switch (memberSymbol.Kind)
            {
                case SymbolKind.Field:
                    var field = (FieldSymbol)memberSymbol;
                    bool isDeclaration = false;
                    return new FieldReferenceOperation(field, isDeclaration, createReceiver(), _semanticModel, syntax, type, constantValue, isImplicit);
                case SymbolKind.Event:
                    var eventSymbol = (EventSymbol)memberSymbol;
                    return new EventReferenceOperation(eventSymbol, createReceiver(), _semanticModel, syntax, type, constantValue, isImplicit);
                case SymbolKind.Property:
                    var property = (PropertySymbol)memberSymbol;
                    if (boundObjectInitializerMember.Arguments.Any())
                    {
                        // In nested member initializers, the property is not actually set. Instead, it is retrieved for a series of Add method calls or nested property setter calls,
                        // so we need to use the getter for this property
                        MethodSymbol accessor = isObjectOrCollectionInitializer ? property.GetOwnOrInheritedGetMethod() : property.GetOwnOrInheritedSetMethod();
                        if (accessor == null || boundObjectInitializerMember.ResultKind == LookupResultKind.OverloadResolutionFailure || accessor.OriginalDefinition is ErrorMethodSymbol)
                        {
                            return new CSharpLazyInvalidOperation(this, boundObjectInitializerMember, _semanticModel, syntax, type, constantValue, isImplicit);
                        }
                    }

                    return new CSharpLazyPropertyReferenceOperation(this, boundObjectInitializerMember, isObjectOrCollectionInitializer: true, property, _semanticModel, syntax, type, constantValue, isImplicit);
                default:
                    throw ExceptionUtilities.Unreachable;
            }

            IOperation createReceiver() => memberSymbol?.IsStatic == true ?
                    null :
                    CreateImplicitReceiver(boundObjectInitializerMember.Syntax, boundObjectInitializerMember.ReceiverType);
        }

        private IOperation CreateBoundDynamicObjectInitializerMemberOperation(BoundDynamicObjectInitializerMember boundDynamicObjectInitializerMember)
        {
            IOperation instanceReceiver = CreateImplicitReceiver(boundDynamicObjectInitializerMember.Syntax, boundDynamicObjectInitializerMember.ReceiverType);
            string memberName = boundDynamicObjectInitializerMember.MemberName;
            ImmutableArray<ITypeSymbol> typeArguments = ImmutableArray<ITypeSymbol>.Empty;
            ITypeSymbol containingType = boundDynamicObjectInitializerMember.ReceiverType;
            SyntaxNode syntax = boundDynamicObjectInitializerMember.Syntax;
            ITypeSymbol type = boundDynamicObjectInitializerMember.Type;
            Optional<object> constantValue = ConvertToOptional(boundDynamicObjectInitializerMember.ConstantValue);
            bool isImplicit = boundDynamicObjectInitializerMember.WasCompilerGenerated;

            return new DynamicMemberReferenceOperation(instanceReceiver, memberName, typeArguments, containingType, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundCollectionElementInitializerOperation(BoundCollectionElementInitializer boundCollectionElementInitializer)
        {
            MethodSymbol addMethod = boundCollectionElementInitializer.AddMethod;
            SyntaxNode syntax = boundCollectionElementInitializer.Syntax;
            ITypeSymbol type = boundCollectionElementInitializer.Type;
            Optional<object> constantValue = ConvertToOptional(boundCollectionElementInitializer.ConstantValue);
            bool isImplicit = boundCollectionElementInitializer.WasCompilerGenerated;

            if (IsMethodInvalid(boundCollectionElementInitializer.ResultKind, addMethod))
            {
                return new CSharpLazyInvalidOperation(this, boundCollectionElementInitializer, _semanticModel, syntax, type, constantValue, isImplicit);
            }

            bool isVirtual = IsCallVirtual(addMethod, boundCollectionElementInitializer.ImplicitReceiverOpt);
            return new CSharpLazyInvocationOperation(this, boundCollectionElementInitializer, addMethod, isVirtual, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDynamicMemberReferenceOperation CreateBoundDynamicMemberAccessOperation(BoundDynamicMemberAccess boundDynamicMemberAccess)
        {
            return CreateBoundDynamicMemberAccessOperation(boundDynamicMemberAccess.Receiver, TypeMap.AsTypeSymbols(boundDynamicMemberAccess.TypeArgumentsOpt), boundDynamicMemberAccess.Name,
                boundDynamicMemberAccess.Syntax, boundDynamicMemberAccess.Type, boundDynamicMemberAccess.ConstantValue, boundDynamicMemberAccess.WasCompilerGenerated);
        }

        private IDynamicMemberReferenceOperation CreateBoundDynamicMemberAccessOperation(
            BoundExpression receiverOpt,
            ImmutableArray<TypeSymbol> typeArgumentsOpt,
            string memberName,
            SyntaxNode syntaxNode,
            ITypeSymbol type,
            ConstantValue value,
            bool isImplicit)
        {
            ITypeSymbol containingType = null;
            if (receiverOpt?.Kind == BoundKind.TypeExpression)
            {
                containingType = receiverOpt.Type;
                receiverOpt = null;
            }

            ImmutableArray<ITypeSymbol> typeArguments = ImmutableArray<ITypeSymbol>.Empty;
            if (!typeArgumentsOpt.IsDefault)
            {
                typeArguments = ImmutableArray<ITypeSymbol>.CastUp(typeArgumentsOpt);
            }
            Optional<object> constantValue = ConvertToOptional(value);
            return new CSharpLazyDynamicMemberReferenceOperation(this, receiverOpt, memberName, typeArguments, containingType, _semanticModel, syntaxNode, type, constantValue, isImplicit);
        }

        private IDynamicInvocationOperation CreateBoundDynamicCollectionElementInitializerOperation(BoundDynamicCollectionElementInitializer boundCollectionElementInitializer)
        {
            SyntaxNode syntax = boundCollectionElementInitializer.Syntax;
            ITypeSymbol type = boundCollectionElementInitializer.Type;
            Optional<object> constantValue = ConvertToOptional(boundCollectionElementInitializer.ConstantValue);
            bool isImplicit = boundCollectionElementInitializer.WasCompilerGenerated;
            return new CSharpLazyDynamicInvocationOperation(this, boundCollectionElementInitializer, argumentNames: ImmutableArray<string>.Empty, argumentRefKinds: ImmutableArray<RefKind>.Empty, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateUnboundLambdaOperation(UnboundLambda unboundLambda)
        {
            // We want to ensure that we never see the UnboundLambda node, and that we don't end up having two different IOperation
            // nodes for the lambda expression. So, we ask the semantic model for the IOperation node for the unbound lambda syntax.
            // We are counting on the fact that will do the error recovery and actually create the BoundLambda node appropriate for
            // this syntax node.
            BoundLambda boundLambda = unboundLambda.BindForErrorRecovery();
            return CreateInternal(boundLambda);
        }

        private IAnonymousFunctionOperation CreateBoundLambdaOperation(BoundLambda boundLambda)
        {
            IMethodSymbol symbol = boundLambda.Symbol;
            BoundNode body = boundLambda.Body;
            SyntaxNode syntax = boundLambda.Syntax;
            // This matches the SemanticModel implementation. This is because in VB, lambdas by themselves
            // do not have a type. To get the type of a lambda expression in the SemanticModel, you need to look at
            // TypeInfo.ConvertedType, rather than TypeInfo.Type. We replicate that behavior here. To get the type of
            // an IAnonymousFunctionExpression, you need to look at the parent IConversionExpression.
            ITypeSymbol type = null;
            Optional<object> constantValue = ConvertToOptional(boundLambda.ConstantValue);
            bool isImplicit = boundLambda.WasCompilerGenerated;
            return new CSharpLazyAnonymousFunctionOperation(this, body, symbol, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILocalFunctionOperation CreateBoundLocalFunctionStatementOperation(BoundLocalFunctionStatement boundLocalFunctionStatement)
        {
            IMethodSymbol symbol = boundLocalFunctionStatement.Symbol;
            SyntaxNode syntax = boundLocalFunctionStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLocalFunctionStatement.WasCompilerGenerated;
            return new CSharpLazyLocalFunctionOperation(this, boundLocalFunctionStatement, symbol, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundConversionOperation(BoundConversion boundConversion)
        {
            bool isImplicit = boundConversion.WasCompilerGenerated || !boundConversion.ExplicitCastInCode;
            BoundExpression boundOperand = boundConversion.Operand;
            if (boundConversion.ConversionKind == CSharp.ConversionKind.MethodGroup)
            {
                // We don't check HasErrors on the conversion here because if we actually have a MethodGroup conversion,
                // overload resolution succeeded. The resulting method could be invalid for other reasons, but we don't
                // hide the resolved method.
                SyntaxNode syntax = boundConversion.Syntax;
                ITypeSymbol type = boundConversion.Type;
                Optional<object> constantValue = ConvertToOptional(boundConversion.ConstantValue);
                return new CSharpLazyDelegateCreationOperation(this, boundConversion, _semanticModel, syntax, type, constantValue, isImplicit);
            }
            else
            {
                SyntaxNode syntax = boundConversion.Syntax;

                if (syntax.IsMissing)
                {
                    // If the underlying syntax IsMissing, then that means we're in case where the compiler generated a piece of syntax to fill in for
                    // an error, such as this case:
                    //
                    //  int i = ;
                    //
                    // Semantic model has a special case here that we match: if the underlying syntax is missing, don't create a conversion expression,
                    // and instead directly return the operand, which will be a BoundBadExpression. When we generate a node for the BoundBadExpression,
                    // the resulting IOperation will also have a null Type.
                    Debug.Assert(boundOperand.Kind == BoundKind.BadExpression ||
                                 ((boundOperand as BoundLambda)?.Body.Statements.SingleOrDefault() as BoundReturnStatement)?.
                                     ExpressionOpt?.Kind == BoundKind.BadExpression);
                    return Create(boundOperand);
                }

                BoundConversion correctedConversionNode = boundConversion;
                Conversion conversion = boundConversion.Conversion;

                if (boundOperand.Syntax == boundConversion.Syntax)
                {
                    if (boundOperand.Kind == BoundKind.ConvertedTupleLiteral && TypeSymbol.Equals(boundOperand.Type, boundConversion.Type, TypeCompareKind.ConsiderEverything2))
                    {
                        // Erase this conversion, this is an artificial conversion added on top of BoundConvertedTupleLiteral
                        // in Binder.CreateTupleLiteralConversion
                        return Create(boundOperand);
                    }
                    else
                    {
                        // Make this conversion implicit
                        isImplicit = true;
                    }
                }

                if (boundConversion.ExplicitCastInCode && conversion.IsIdentity && boundOperand.Kind == BoundKind.Conversion)
                {
                    var nestedConversion = (BoundConversion)boundOperand;
                    BoundExpression nestedOperand = nestedConversion.Operand;

                    if (nestedConversion.Syntax == nestedOperand.Syntax && nestedConversion.ExplicitCastInCode &&
                        nestedOperand.Kind == BoundKind.ConvertedTupleLiteral &&
                        !TypeSymbol.Equals(nestedConversion.Type, nestedOperand.Type, TypeCompareKind.ConsiderEverything2))
                    {
                        // Let's erase the nested conversion, this is an artificial conversion added on top of BoundConvertedTupleLiteral
                        // in Binder.CreateTupleLiteralConversion.
                        // We need to use conversion information from the nested conversion because that is where the real conversion
                        // information is stored.
                        conversion = nestedConversion.Conversion;
                        correctedConversionNode = nestedConversion;
                    }
                }

                ITypeSymbol type = boundConversion.Type;
                Optional<object> constantValue = ConvertToOptional(boundConversion.ConstantValue);

                // If this is a lambda or method group conversion to a delegate type, we return a delegate creation instead of a conversion
                if ((boundOperand.Kind == BoundKind.Lambda ||
                     boundOperand.Kind == BoundKind.UnboundLambda ||
                     boundOperand.Kind == BoundKind.MethodGroup) &&
                    boundConversion.Type.IsDelegateType())
                {
                    return new CSharpLazyDelegateCreationOperation(this, correctedConversionNode, _semanticModel, syntax, type, constantValue, isImplicit);
                }
                else
                {
                    bool isTryCast = false;
                    // Checked conversions only matter if the conversion is a Numeric conversion. Don't have true unless the conversion is actually numeric.
                    bool isChecked = conversion.IsNumeric && boundConversion.Checked;
                    return new CSharpLazyConversionOperation(this, correctedConversionNode.Operand, conversion, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit);
                }
            }
        }

        private IConversionOperation CreateBoundAsOperatorOperation(BoundAsOperator boundAsOperator)
        {
            BoundNode operand = boundAsOperator.Operand;
            SyntaxNode syntax = boundAsOperator.Syntax;
            Conversion conversion = boundAsOperator.Conversion;
            bool isTryCast = true;
            bool isChecked = false;
            ITypeSymbol type = boundAsOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAsOperator.ConstantValue);
            bool isImplicit = boundAsOperator.WasCompilerGenerated;
            return new CSharpLazyConversionOperation(this, operand, conversion, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDelegateCreationOperation CreateBoundDelegateCreationExpressionOperation(BoundDelegateCreationExpression boundDelegateCreationExpression)
        {
            SyntaxNode syntax = boundDelegateCreationExpression.Syntax;
            ITypeSymbol type = boundDelegateCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundDelegateCreationExpression.ConstantValue);
            bool isImplicit = boundDelegateCreationExpression.WasCompilerGenerated;
            return new CSharpLazyDelegateCreationOperation(this, boundDelegateCreationExpression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IMethodReferenceOperation CreateBoundMethodGroupSingleMethodOperation(BoundMethodGroup boundMethodGroup, IMethodSymbol methodSymbol, bool suppressVirtualCalls)
        {
            bool isVirtual = (methodSymbol.IsAbstract || methodSymbol.IsOverride || methodSymbol.IsVirtual) && !suppressVirtualCalls;
            BoundNode instance = boundMethodGroup.ReceiverOpt;
            SyntaxNode bindingSyntax = boundMethodGroup.Syntax;
            ITypeSymbol bindingType = null;
            Optional<object> bindingConstantValue = ConvertToOptional(boundMethodGroup.ConstantValue);
            bool isImplicit = boundMethodGroup.WasCompilerGenerated;
            return new CSharpLazyMethodReferenceOperation(this, instance, methodSymbol, isVirtual, _semanticModel, bindingSyntax, bindingType, bindingConstantValue, boundMethodGroup.WasCompilerGenerated);
        }

        private IIsTypeOperation CreateBoundIsOperatorOperation(BoundIsOperator boundIsOperator)
        {
            BoundNode valueOperand = boundIsOperator.Operand;
            ITypeSymbol typeOperand = boundIsOperator.TargetType.Type;
            SyntaxNode syntax = boundIsOperator.Syntax;
            ITypeSymbol type = boundIsOperator.Type;
            bool isNegated = false;
            Optional<object> constantValue = ConvertToOptional(boundIsOperator.ConstantValue);
            bool isImplicit = boundIsOperator.WasCompilerGenerated;
            return new CSharpLazyIsTypeOperation(this, valueOperand, typeOperand, isNegated, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ISizeOfOperation CreateBoundSizeOfOperatorOperation(BoundSizeOfOperator boundSizeOfOperator)
        {
            ITypeSymbol typeOperand = boundSizeOfOperator.SourceType.Type;
            SyntaxNode syntax = boundSizeOfOperator.Syntax;
            ITypeSymbol type = boundSizeOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundSizeOfOperator.ConstantValue);
            bool isImplicit = boundSizeOfOperator.WasCompilerGenerated;
            return new SizeOfOperation(typeOperand, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ITypeOfOperation CreateBoundTypeOfOperatorOperation(BoundTypeOfOperator boundTypeOfOperator)
        {
            ITypeSymbol typeOperand = boundTypeOfOperator.SourceType.Type;
            SyntaxNode syntax = boundTypeOfOperator.Syntax;
            ITypeSymbol type = boundTypeOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundTypeOfOperator.ConstantValue);
            bool isImplicit = boundTypeOfOperator.WasCompilerGenerated;
            return new TypeOfOperation(typeOperand, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IArrayCreationOperation CreateBoundArrayCreationOperation(BoundArrayCreation boundArrayCreation)
        {
            SyntaxNode syntax = boundArrayCreation.Syntax;
            ITypeSymbol type = boundArrayCreation.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayCreation.ConstantValue);
            bool isImplicit = boundArrayCreation.WasCompilerGenerated ||
                              (boundArrayCreation.InitializerOpt?.Syntax == syntax && !boundArrayCreation.InitializerOpt.WasCompilerGenerated);
            return new CSharpLazyArrayCreationOperation(this, boundArrayCreation, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IArrayInitializerOperation CreateBoundArrayInitializationOperation(BoundArrayInitialization boundArrayInitialization)
        {
            SyntaxNode syntax = boundArrayInitialization.Syntax;
            Optional<object> constantValue = ConvertToOptional(boundArrayInitialization.ConstantValue);
            bool isImplicit = boundArrayInitialization.WasCompilerGenerated;
            return new CSharpLazyArrayInitializerOperation(this, boundArrayInitialization, _semanticModel, syntax, constantValue, isImplicit);
        }

        private IDefaultValueOperation CreateBoundDefaultExpressionOperation(BoundDefaultExpression boundDefaultExpression)
        {
            SyntaxNode syntax = boundDefaultExpression.Syntax;
            ITypeSymbol type = boundDefaultExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundDefaultExpression.ConstantValue);
            bool isImplicit = boundDefaultExpression.WasCompilerGenerated;
            return new DefaultValueOperation(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundBaseReferenceOperation(BoundBaseReference boundBaseReference)
        {
            InstanceReferenceKind referenceKind = InstanceReferenceKind.ContainingTypeInstance;
            SyntaxNode syntax = boundBaseReference.Syntax;
            ITypeSymbol type = boundBaseReference.Type;
            Optional<object> constantValue = ConvertToOptional(boundBaseReference.ConstantValue);
            bool isImplicit = boundBaseReference.WasCompilerGenerated;
            return new InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundThisReferenceOperation(BoundThisReference boundThisReference)
        {
            InstanceReferenceKind referenceKind = InstanceReferenceKind.ContainingTypeInstance;
            SyntaxNode syntax = boundThisReference.Syntax;
            ITypeSymbol type = boundThisReference.Type;
            Optional<object> constantValue = ConvertToOptional(boundThisReference.ConstantValue);
            bool isImplicit = boundThisReference.WasCompilerGenerated;
            return new InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundAssignmentOperatorOrMemberInitializerOperation(BoundAssignmentOperator boundAssignmentOperator)
        {
            return IsMemberInitializer(boundAssignmentOperator) ?
                (IOperation)CreateBoundMemberInitializerOperation(boundAssignmentOperator) :
                CreateBoundAssignmentOperatorOperation(boundAssignmentOperator);
        }

        private static bool IsMemberInitializer(BoundAssignmentOperator boundAssignmentOperator) =>
            boundAssignmentOperator.Right?.Kind == BoundKind.ObjectInitializerExpression ||
            boundAssignmentOperator.Right?.Kind == BoundKind.CollectionInitializerExpression;

        private ISimpleAssignmentOperation CreateBoundAssignmentOperatorOperation(BoundAssignmentOperator boundAssignmentOperator)
        {
            Debug.Assert(!IsMemberInitializer(boundAssignmentOperator));

            bool isRef = boundAssignmentOperator.IsRef;
            SyntaxNode syntax = boundAssignmentOperator.Syntax;
            ITypeSymbol type = boundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAssignmentOperator.ConstantValue);
            bool isImplicit = boundAssignmentOperator.WasCompilerGenerated;
            return new CSharpLazySimpleAssignmentOperation(this, boundAssignmentOperator, isRef, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IMemberInitializerOperation CreateBoundMemberInitializerOperation(BoundAssignmentOperator boundAssignmentOperator)
        {
            Debug.Assert(IsMemberInitializer(boundAssignmentOperator));

            SyntaxNode syntax = boundAssignmentOperator.Syntax;
            ITypeSymbol type = boundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAssignmentOperator.ConstantValue);
            bool isImplicit = boundAssignmentOperator.WasCompilerGenerated;
            return new CSharpLazyMemberInitializerOperation(this, boundAssignmentOperator, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICompoundAssignmentOperation CreateBoundCompoundAssignmentOperatorOperation(BoundCompoundAssignmentOperator boundCompoundAssignmentOperator)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundCompoundAssignmentOperator.Operator.Kind);
            Conversion inConversion = boundCompoundAssignmentOperator.LeftConversion;
            Conversion outConversion = boundCompoundAssignmentOperator.FinalConversion;
            bool isLifted = boundCompoundAssignmentOperator.Operator.Kind.IsLifted();
            bool isChecked = boundCompoundAssignmentOperator.Operator.Kind.IsChecked();
            IMethodSymbol operatorMethod = boundCompoundAssignmentOperator.Operator.Method;
            SyntaxNode syntax = boundCompoundAssignmentOperator.Syntax;
            ITypeSymbol type = boundCompoundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundCompoundAssignmentOperator.ConstantValue);
            bool isImplicit = boundCompoundAssignmentOperator.WasCompilerGenerated;
            return new CSharpLazyCompoundAssignmentOperation(this, boundCompoundAssignmentOperator, inConversion, outConversion, operatorKind, isLifted, isChecked, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IIncrementOrDecrementOperation CreateBoundIncrementOperatorOperation(BoundIncrementOperator boundIncrementOperator)
        {
            bool isDecrement = Helper.IsDecrement(boundIncrementOperator.OperatorKind);
            bool isPostfix = Helper.IsPostfixIncrementOrDecrement(boundIncrementOperator.OperatorKind);
            bool isLifted = boundIncrementOperator.OperatorKind.IsLifted();
            bool isChecked = boundIncrementOperator.OperatorKind.IsChecked();
            BoundNode target = boundIncrementOperator.Operand;
            IMethodSymbol operatorMethod = boundIncrementOperator.MethodOpt;
            SyntaxNode syntax = boundIncrementOperator.Syntax;
            ITypeSymbol type = boundIncrementOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundIncrementOperator.ConstantValue);
            bool isImplicit = boundIncrementOperator.WasCompilerGenerated;
            return new CSharpLazyIncrementOrDecrementOperation(this, target, isDecrement, isPostfix, isLifted, isChecked, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInvalidOperation CreateBoundBadExpressionOperation(BoundBadExpression boundBadExpression)
        {
            SyntaxNode syntax = boundBadExpression.Syntax;
            // We match semantic model here: if the expression IsMissing, we have a null type, rather than the ErrorType of the bound node.
            ITypeSymbol type = syntax.IsMissing ? null : boundBadExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundBadExpression.ConstantValue);

            // if child has syntax node point to same syntax node as bad expression, then this invalid expression is implicit
            bool isImplicit = boundBadExpression.WasCompilerGenerated || boundBadExpression.ChildBoundNodes.Any(e => e?.Syntax == boundBadExpression.Syntax);
            return new CSharpLazyInvalidOperation(this, boundBadExpression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ITypeParameterObjectCreationOperation CreateBoundNewTOperation(BoundNewT boundNewT)
        {
            BoundNode initializer = boundNewT.InitializerExpressionOpt;
            SyntaxNode syntax = boundNewT.Syntax;
            ITypeSymbol type = boundNewT.Type;
            Optional<object> constantValue = ConvertToOptional(boundNewT.ConstantValue);
            bool isImplicit = boundNewT.WasCompilerGenerated;
            return new CSharpLazyTypeParameterObjectCreationOperation(this, initializer, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private INoPiaObjectCreationOperation CreateNoPiaObjectCreationExpressionOperation(BoundNoPiaObjectCreationExpression creation)
        {
            BoundNode initializer = creation.InitializerExpressionOpt;
            SyntaxNode syntax = creation.Syntax;
            ITypeSymbol type = creation.Type;
            Optional<object> constantValue = ConvertToOptional(creation.ConstantValue);
            bool isImplicit = creation.WasCompilerGenerated;
            return new CSharpLazyNoPiaObjectCreationOperation(this, initializer, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IUnaryOperation CreateBoundUnaryOperatorOperation(BoundUnaryOperator boundUnaryOperator)
        {
            UnaryOperatorKind unaryOperatorKind = Helper.DeriveUnaryOperatorKind(boundUnaryOperator.OperatorKind);
            BoundNode operand = boundUnaryOperator.Operand;
            IMethodSymbol operatorMethod = boundUnaryOperator.MethodOpt;
            SyntaxNode syntax = boundUnaryOperator.Syntax;
            ITypeSymbol type = boundUnaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundUnaryOperator.ConstantValue);
            bool isLifted = boundUnaryOperator.OperatorKind.IsLifted();
            bool isChecked = boundUnaryOperator.OperatorKind.IsChecked();
            bool isImplicit = boundUnaryOperator.WasCompilerGenerated;
            return new CSharpLazyUnaryOperation(this, operand, unaryOperatorKind, isLifted, isChecked, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBinaryOperation CreateBoundBinaryOperatorOperation(BoundBinaryOperator boundBinaryOperator)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundBinaryOperator.OperatorKind);
            IMethodSymbol operatorMethod = boundBinaryOperator.MethodOpt;
            IMethodSymbol unaryOperatorMethod = null;

            // For dynamic logical operator MethodOpt is actually the unary true/false operator
            if (boundBinaryOperator.Type.IsDynamic() &&
                (operatorKind == BinaryOperatorKind.ConditionalAnd || operatorKind == BinaryOperatorKind.ConditionalOr) &&
                operatorMethod?.Parameters.Length == 1)
            {
                unaryOperatorMethod = operatorMethod;
                operatorMethod = null;
            }

            SyntaxNode syntax = boundBinaryOperator.Syntax;
            ITypeSymbol type = boundBinaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundBinaryOperator.ConstantValue);
            bool isLifted = boundBinaryOperator.OperatorKind.IsLifted();
            bool isChecked = boundBinaryOperator.OperatorKind.IsChecked();
            bool isCompareText = false;
            bool isImplicit = boundBinaryOperator.WasCompilerGenerated;
            return new CSharpLazyBinaryOperation(this, boundBinaryOperator, operatorKind, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod,
                                                 _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBinaryOperation CreateBoundUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator boundBinaryOperator)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundBinaryOperator.OperatorKind);
            IMethodSymbol operatorMethod = boundBinaryOperator.LogicalOperator;
            IMethodSymbol unaryOperatorMethod = boundBinaryOperator.OperatorKind.Operator() == CSharp.BinaryOperatorKind.And ?
                                                    boundBinaryOperator.FalseOperator :
                                                    boundBinaryOperator.TrueOperator;
            SyntaxNode syntax = boundBinaryOperator.Syntax;
            ITypeSymbol type = boundBinaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundBinaryOperator.ConstantValue);
            bool isLifted = boundBinaryOperator.OperatorKind.IsLifted();
            bool isChecked = boundBinaryOperator.OperatorKind.IsChecked();
            bool isCompareText = false;
            bool isImplicit = boundBinaryOperator.WasCompilerGenerated;
            return new CSharpLazyBinaryOperation(this, boundBinaryOperator, operatorKind, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod,
                                                 _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ITupleBinaryOperation CreateBoundTupleBinaryOperatorOperation(BoundTupleBinaryOperator boundTupleBinaryOperator)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundTupleBinaryOperator.OperatorKind);
            SyntaxNode syntax = boundTupleBinaryOperator.Syntax;
            ITypeSymbol type = boundTupleBinaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundTupleBinaryOperator.ConstantValue);
            bool isImplicit = boundTupleBinaryOperator.WasCompilerGenerated;
            return new CSharpLazyTupleBinaryOperation(this, boundTupleBinaryOperator, operatorKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalOperation CreateBoundConditionalOperatorOperation(BoundConditionalOperator boundConditionalOperator)
        {
            bool isRef = boundConditionalOperator.IsRef;
            SyntaxNode syntax = boundConditionalOperator.Syntax;
            ITypeSymbol type = boundConditionalOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalOperator.ConstantValue);
            bool isImplicit = boundConditionalOperator.WasCompilerGenerated;
            return new CSharpLazyConditionalOperation(this, boundConditionalOperator, isRef, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICoalesceOperation CreateBoundNullCoalescingOperatorOperation(BoundNullCoalescingOperator boundNullCoalescingOperator)
        {
            SyntaxNode syntax = boundNullCoalescingOperator.Syntax;
            ITypeSymbol type = boundNullCoalescingOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundNullCoalescingOperator.ConstantValue);
            bool isImplicit = boundNullCoalescingOperator.WasCompilerGenerated;
            Conversion valueConversion = boundNullCoalescingOperator.LeftConversion;

            if (valueConversion.Exists && !valueConversion.IsIdentity &&
                boundNullCoalescingOperator.Type.Equals(boundNullCoalescingOperator.LeftOperand.Type?.StrippedType(), TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
            {
                valueConversion = Conversion.Identity;
            }

            return new CSharpLazyCoalesceOperation(this, boundNullCoalescingOperator, valueConversion, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundNullCoalescingAssignmentOperatorOperation(BoundNullCoalescingAssignmentOperator boundNode)
        {
            SyntaxNode syntax = boundNode.Syntax;
            ITypeSymbol type = boundNode.Type;
            Optional<object> constantValue = ConvertToOptional(boundNode.ConstantValue);
            bool isImplicit = boundNode.WasCompilerGenerated;

            return new CSharpLazyCoalesceAssignmentOperation(this, boundNode, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IAwaitOperation CreateBoundAwaitExpressionOperation(BoundAwaitExpression boundAwaitExpression)
        {
            BoundNode awaitedValue = boundAwaitExpression.Expression;
            SyntaxNode syntax = boundAwaitExpression.Syntax;
            ITypeSymbol type = boundAwaitExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundAwaitExpression.ConstantValue);
            bool isImplicit = boundAwaitExpression.WasCompilerGenerated;
            return new CSharpLazyAwaitOperation(this, awaitedValue, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IArrayElementReferenceOperation CreateBoundArrayAccessOperation(BoundArrayAccess boundArrayAccess)
        {
            SyntaxNode syntax = boundArrayAccess.Syntax;
            ITypeSymbol type = boundArrayAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayAccess.ConstantValue);
            bool isImplicit = boundArrayAccess.WasCompilerGenerated;

            return new CSharpLazyArrayElementReferenceOperation(this, boundArrayAccess, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private INameOfOperation CreateBoundNameOfOperatorOperation(BoundNameOfOperator boundNameOfOperator)
        {
            BoundExpression argument = boundNameOfOperator.Argument;
            SyntaxNode syntax = boundNameOfOperator.Syntax;
            ITypeSymbol type = boundNameOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundNameOfOperator.ConstantValue);
            bool isImplicit = boundNameOfOperator.WasCompilerGenerated;
            return new CSharpLazyNameOfOperation(this, argument, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IThrowOperation CreateBoundThrowExpressionOperation(BoundThrowExpression boundThrowExpression)
        {
            BoundNode expression = boundThrowExpression.Expression;
            SyntaxNode syntax = boundThrowExpression.Syntax;
            ITypeSymbol type = boundThrowExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundThrowExpression.ConstantValue);
            bool isImplicit = boundThrowExpression.WasCompilerGenerated;
            return new CSharpLazyThrowOperation(this, expression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IAddressOfOperation CreateBoundAddressOfOperatorOperation(BoundAddressOfOperator boundAddressOfOperator)
        {
            BoundExpression reference = boundAddressOfOperator.Operand;
            SyntaxNode syntax = boundAddressOfOperator.Syntax;
            ITypeSymbol type = boundAddressOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAddressOfOperator.ConstantValue);
            bool isImplicit = boundAddressOfOperator.WasCompilerGenerated;
            return new CSharpLazyAddressOfOperation(this, reference, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundImplicitReceiverOperation(BoundImplicitReceiver boundImplicitReceiver)
        {
            InstanceReferenceKind referenceKind = InstanceReferenceKind.ImplicitReceiver;
            SyntaxNode syntax = boundImplicitReceiver.Syntax;
            ITypeSymbol type = boundImplicitReceiver.Type;
            Optional<object> constantValue = ConvertToOptional(boundImplicitReceiver.ConstantValue);
            bool isImplicit = boundImplicitReceiver.WasCompilerGenerated;
            return new InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalAccessOperation CreateBoundConditionalAccessOperation(BoundConditionalAccess boundConditionalAccess)
        {
            SyntaxNode syntax = boundConditionalAccess.Syntax;
            ITypeSymbol type = boundConditionalAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalAccess.ConstantValue);
            bool isImplicit = boundConditionalAccess.WasCompilerGenerated;

            return new CSharpLazyConditionalAccessOperation(this, boundConditionalAccess, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalAccessInstanceOperation CreateBoundConditionalReceiverOperation(BoundConditionalReceiver boundConditionalReceiver)
        {
            SyntaxNode syntax = boundConditionalReceiver.Syntax;
            ITypeSymbol type = boundConditionalReceiver.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalReceiver.ConstantValue);
            bool isImplicit = boundConditionalReceiver.WasCompilerGenerated;
            return new ConditionalAccessInstanceOperation(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IFieldInitializerOperation CreateBoundFieldEqualsValueOperation(BoundFieldEqualsValue boundFieldEqualsValue)
        {
            ImmutableArray<IFieldSymbol> initializedFields = ImmutableArray.Create<IFieldSymbol>(boundFieldEqualsValue.Field);
            BoundNode value = boundFieldEqualsValue.Value;
            OperationKind kind = OperationKind.FieldInitializer;
            SyntaxNode syntax = boundFieldEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundFieldEqualsValue.WasCompilerGenerated;
            return new CSharpLazyFieldInitializerOperation(this, value, boundFieldEqualsValue.Locals.As<ILocalSymbol>(), initializedFields, kind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IPropertyInitializerOperation CreateBoundPropertyEqualsValueOperation(BoundPropertyEqualsValue boundPropertyEqualsValue)
        {
            ImmutableArray<IPropertySymbol> initializedProperties = ImmutableArray.Create<IPropertySymbol>(boundPropertyEqualsValue.Property);
            BoundNode value = boundPropertyEqualsValue.Value;
            SyntaxNode syntax = boundPropertyEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundPropertyEqualsValue.WasCompilerGenerated;
            return new CSharpLazyPropertyInitializerOperation(this, value, boundPropertyEqualsValue.Locals.As<ILocalSymbol>(), initializedProperties, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IParameterInitializerOperation CreateBoundParameterEqualsValueOperation(BoundParameterEqualsValue boundParameterEqualsValue)
        {
            IParameterSymbol parameter = boundParameterEqualsValue.Parameter;
            BoundNode value = boundParameterEqualsValue.Value;
            OperationKind kind = OperationKind.ParameterInitializer;
            SyntaxNode syntax = boundParameterEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundParameterEqualsValue.WasCompilerGenerated;
            return new CSharpLazyParameterInitializerOperation(this, value, boundParameterEqualsValue.Locals.As<ILocalSymbol>(), parameter, kind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBlockOperation CreateBoundBlockOperation(BoundBlock boundBlock)
        {
            ImmutableArray<ILocalSymbol> locals = boundBlock.Locals.As<ILocalSymbol>();
            SyntaxNode syntax = boundBlock.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundBlock.WasCompilerGenerated;
            return new CSharpLazyBlockOperation(this, boundBlock, locals, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBranchOperation CreateBoundContinueStatementOperation(BoundContinueStatement boundContinueStatement)
        {
            ILabelSymbol target = boundContinueStatement.Label;
            BranchKind branchKind = BranchKind.Continue;
            SyntaxNode syntax = boundContinueStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundContinueStatement.WasCompilerGenerated;
            return new BranchOperation(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBranchOperation CreateBoundBreakStatementOperation(BoundBreakStatement boundBreakStatement)
        {
            ILabelSymbol target = boundBreakStatement.Label;
            BranchKind branchKind = BranchKind.Break;
            SyntaxNode syntax = boundBreakStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundBreakStatement.WasCompilerGenerated;
            return new BranchOperation(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IReturnOperation CreateBoundYieldBreakStatementOperation(BoundYieldBreakStatement boundYieldBreakStatement)
        {
            BoundNode returnedValue = null;
            SyntaxNode syntax = boundYieldBreakStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundYieldBreakStatement.WasCompilerGenerated;
            return new CSharpLazyReturnOperation(this, returnedValue, OperationKind.YieldBreak, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBranchOperation CreateBoundGotoStatementOperation(BoundGotoStatement boundGotoStatement)
        {
            ILabelSymbol target = boundGotoStatement.Label;
            BranchKind branchKind = BranchKind.GoTo;
            SyntaxNode syntax = boundGotoStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundGotoStatement.WasCompilerGenerated;
            return new BranchOperation(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IEmptyOperation CreateBoundNoOpStatementOperation(BoundNoOpStatement boundNoOpStatement)
        {
            SyntaxNode syntax = boundNoOpStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundNoOpStatement.WasCompilerGenerated;
            return new EmptyOperation(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalOperation CreateBoundIfStatementOperation(BoundIfStatement boundIfStatement)
        {
            bool isRef = false;
            SyntaxNode syntax = boundIfStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundIfStatement.WasCompilerGenerated;
            return new CSharpLazyConditionalOperation(this, boundIfStatement, isRef, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IWhileLoopOperation CreateBoundWhileStatementOperation(BoundWhileStatement boundWhileStatement)
        {
            ImmutableArray<ILocalSymbol> locals = boundWhileStatement.Locals.As<ILocalSymbol>();
            ILabelSymbol continueLabel = boundWhileStatement.ContinueLabel;
            ILabelSymbol exitLabel = boundWhileStatement.BreakLabel;
            bool conditionIsTop = true;
            bool conditionIsUntil = false;
            SyntaxNode syntax = boundWhileStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundWhileStatement.WasCompilerGenerated;
            return new CSharpLazyWhileLoopOperation(this, boundWhileStatement, locals, continueLabel, exitLabel, conditionIsTop, conditionIsUntil, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IWhileLoopOperation CreateBoundDoStatementOperation(BoundDoStatement boundDoStatement)
        {
            ILabelSymbol continueLabel = boundDoStatement.ContinueLabel;
            ILabelSymbol exitLabel = boundDoStatement.BreakLabel;
            bool conditionIsTop = false;
            bool conditionIsUntil = false;
            ImmutableArray<ILocalSymbol> locals = boundDoStatement.Locals.As<ILocalSymbol>();
            SyntaxNode syntax = boundDoStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundDoStatement.WasCompilerGenerated;
            return new CSharpLazyWhileLoopOperation(this, boundDoStatement, locals, continueLabel, exitLabel, conditionIsTop, conditionIsUntil, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IForLoopOperation CreateBoundForStatementOperation(BoundForStatement boundForStatement)
        {
            ImmutableArray<ILocalSymbol> locals = boundForStatement.OuterLocals.As<ILocalSymbol>();
            ImmutableArray<ILocalSymbol> conditionLocals = boundForStatement.InnerLocals.As<ILocalSymbol>();
            ILabelSymbol continueLabel = boundForStatement.ContinueLabel;
            ILabelSymbol exitLabel = boundForStatement.BreakLabel;
            SyntaxNode syntax = boundForStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundForStatement.WasCompilerGenerated;
            return new CSharpLazyForLoopOperation(this, boundForStatement, locals, conditionLocals, continueLabel, exitLabel, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal ForEachLoopOperationInfo GetForEachLoopOperatorInfo(BoundForEachStatement boundForEachStatement)
        {
            ForEachEnumeratorInfo enumeratorInfoOpt = boundForEachStatement.EnumeratorInfoOpt;
            ForEachLoopOperationInfo info;

            if (enumeratorInfoOpt != null)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var compilation = (CSharpCompilation)_semanticModel.Compilation;

                info = new ForEachLoopOperationInfo(enumeratorInfoOpt.ElementType,
                                                    enumeratorInfoOpt.GetEnumeratorMethod,
                                                    (PropertySymbol)enumeratorInfoOpt.CurrentPropertyGetter.AssociatedSymbol,
                                                    enumeratorInfoOpt.MoveNextMethod,
                                                    enumeratorInfoOpt.NeedsDisposal,
                                                    knownToImplementIDisposable: enumeratorInfoOpt.NeedsDisposal && (object)enumeratorInfoOpt.GetEnumeratorMethod != null ?
                                                                                     compilation.Conversions.
                                                                                         ClassifyImplicitConversionFromType(enumeratorInfoOpt.GetEnumeratorMethod.ReturnType,
                                                                                                                            compilation.GetSpecialType(SpecialType.System_IDisposable),
                                                                                                                            ref useSiteDiagnostics).IsImplicit :
                                                                                     false,
                                                    enumeratorInfoOpt.CurrentConversion,
                                                    boundForEachStatement.ElementConversion);
            }
            else
            {
                info = default;
            }

            return info;
        }

        internal IOperation CreateBoundForEachStatementLoopControlVariable(BoundForEachStatement boundForEachStatement)
        {
            if (boundForEachStatement.DeconstructionOpt != null)
            {
                return Create(boundForEachStatement.DeconstructionOpt.DeconstructionAssignment.Left);
            }
            else if (boundForEachStatement.IterationErrorExpressionOpt != null)
            {
                return Create(boundForEachStatement.IterationErrorExpressionOpt);
            }
            else
            {
                Debug.Assert(boundForEachStatement.IterationVariables.Length == 1);
                var local = boundForEachStatement.IterationVariables[0];
                // We use iteration variable type syntax as the underlying syntax node as there is no variable declarator syntax in the syntax tree.
                var declaratorSyntax = boundForEachStatement.IterationVariableType.Syntax;
                return new VariableDeclaratorOperation(local, initializer: null, ignoredArguments: ImmutableArray<IOperation>.Empty, semanticModel: _semanticModel, syntax: declaratorSyntax, type: null, constantValue: default, isImplicit: false);
            }
        }

        private IForEachLoopOperation CreateBoundForEachStatementOperation(BoundForEachStatement boundForEachStatement)
        {
            ImmutableArray<ILocalSymbol> locals = boundForEachStatement.IterationVariables.As<ILocalSymbol>();

            ILabelSymbol continueLabel = boundForEachStatement.ContinueLabel;
            ILabelSymbol exitLabel = boundForEachStatement.BreakLabel;
            SyntaxNode syntax = boundForEachStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundForEachStatement.WasCompilerGenerated;
            return new CSharpLazyForEachLoopOperation(this, boundForEachStatement, locals, continueLabel, exitLabel, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ITryOperation CreateBoundTryStatementOperation(BoundTryStatement boundTryStatement)
        {
            SyntaxNode syntax = boundTryStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundTryStatement.WasCompilerGenerated;
            return new CSharpLazyTryOperation(this, boundTryStatement, exitLabel: null, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICatchClauseOperation CreateBoundCatchBlockOperation(BoundCatchBlock boundCatchBlock)
        {
            ITypeSymbol exceptionType = boundCatchBlock.ExceptionTypeOpt ?? (ITypeSymbol)_semanticModel.Compilation.ObjectType;
            ImmutableArray<ILocalSymbol> locals = boundCatchBlock.Locals.As<ILocalSymbol>();
            SyntaxNode syntax = boundCatchBlock.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundCatchBlock.WasCompilerGenerated;
            return new CSharpLazyCatchClauseOperation(this, boundCatchBlock, exceptionType, locals, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IFixedOperation CreateBoundFixedStatementOperation(BoundFixedStatement boundFixedStatement)
        {
            ImmutableArray<ILocalSymbol> locals = boundFixedStatement.Locals.As<ILocalSymbol>();
            SyntaxNode syntax = boundFixedStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundFixedStatement.WasCompilerGenerated;
            return new CSharpLazyFixedOperation(this, boundFixedStatement, locals, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IUsingOperation CreateBoundUsingStatementOperation(BoundUsingStatement boundUsingStatement)
        {
            ImmutableArray<ILocalSymbol> locals = ImmutableArray<ILocalSymbol>.CastUp(boundUsingStatement.Locals);
            SyntaxNode syntax = boundUsingStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundUsingStatement.WasCompilerGenerated;
            return new CSharpLazyUsingOperation(this, boundUsingStatement, locals, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IThrowOperation CreateBoundThrowStatementOperation(BoundThrowStatement boundThrowStatement)
        {
            BoundNode thrownObject = boundThrowStatement.ExpressionOpt;
            SyntaxNode syntax = boundThrowStatement.Syntax;
            ITypeSymbol statementType = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundThrowStatement.WasCompilerGenerated;
            return new CSharpLazyThrowOperation(this, thrownObject, _semanticModel, syntax, statementType, constantValue, isImplicit);
        }

        private IReturnOperation CreateBoundReturnStatementOperation(BoundReturnStatement boundReturnStatement)
        {
            BoundNode returnedValue = boundReturnStatement.ExpressionOpt;
            SyntaxNode syntax = boundReturnStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundReturnStatement.WasCompilerGenerated;
            return new CSharpLazyReturnOperation(this, returnedValue, OperationKind.Return, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IReturnOperation CreateBoundYieldReturnStatementOperation(BoundYieldReturnStatement boundYieldReturnStatement)
        {
            BoundNode returnedValue = boundYieldReturnStatement.Expression;
            SyntaxNode syntax = boundYieldReturnStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundYieldReturnStatement.WasCompilerGenerated;
            return new CSharpLazyReturnOperation(this, returnedValue, OperationKind.YieldReturn, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILockOperation CreateBoundLockStatementOperation(BoundLockStatement boundLockStatement)
        {
            // If there is no Enter2 method, then there will be no lock taken reference
            bool legacyMode = _semanticModel.Compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter2) == null;
            ILocalSymbol lockTakenSymbol =
                legacyMode ? null : new SynthesizedLocal(_semanticModel.GetEnclosingSymbol(boundLockStatement.Syntax.SpanStart) as MethodSymbol,
                                                         TypeWithAnnotations.Create((TypeSymbol)_semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean)),
                                                         SynthesizedLocalKind.LockTaken,
                                                         syntaxOpt: boundLockStatement.Argument.Syntax);
            SyntaxNode syntax = boundLockStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLockStatement.WasCompilerGenerated;

            return new CSharpLazyLockOperation(this, boundLockStatement, lockTakenSymbol, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInvalidOperation CreateBoundBadStatementOperation(BoundBadStatement boundBadStatement)
        {
            SyntaxNode syntax = boundBadStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);

            // if child has syntax node point to same syntax node as bad statement, then this invalid statement is implicit
            bool isImplicit = boundBadStatement.WasCompilerGenerated || boundBadStatement.ChildBoundNodes.Any(e => e?.Syntax == boundBadStatement.Syntax);
            return new CSharpLazyInvalidOperation(this, boundBadStatement, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundLocalDeclarationOperation(BoundLocalDeclaration boundLocalDeclaration)
        {
            var node = boundLocalDeclaration.Syntax;
            var kind = node.Kind();

            SyntaxNode varStatement;
            SyntaxNode varDeclaration;
            switch (kind)
            {
                case SyntaxKind.LocalDeclarationStatement:
                    {
                        var statement = (LocalDeclarationStatementSyntax)node;

                        // this happen for simple int i = 0;
                        // var statement points to LocalDeclarationStatementSyntax
                        varStatement = statement;

                        varDeclaration = statement.Declaration;
                        break;
                    }
                case SyntaxKind.VariableDeclarator:
                    {
                        // this happen for 'for loop' initializer
                        // We generate a DeclarationGroup for this scenario to maintain tree shape consistency across IOperation.
                        // var statement points to VariableDeclarationSyntax
                        varStatement = node.Parent;

                        varDeclaration = node.Parent;
                        break;
                    }
                default:
                    {
                        Debug.Fail($"Unexpected syntax: {kind}");

                        // otherwise, they points to whatever bound nodes are pointing to.
                        varStatement = varDeclaration = node;
                        break;
                    }
            }

            bool multiVariableImplicit = boundLocalDeclaration.WasCompilerGenerated;
            IVariableDeclarationOperation multiVariableDeclaration = new CSharpLazyVariableDeclarationOperation(this, boundLocalDeclaration, _semanticModel, varDeclaration, null, default, multiVariableImplicit);
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            // In the case of a for loop, varStatement and varDeclaration will be the same syntax node.
            // We can only have one explicit operation, so make sure this node is implicit in that scenario.
            bool isImplicit = (varStatement == varDeclaration) || boundLocalDeclaration.WasCompilerGenerated;
            return new VariableDeclarationGroupOperation(ImmutableArray.Create(multiVariableDeclaration), _semanticModel, varStatement, type, constantValue, isImplicit);
        }

        private IVariableDeclarationGroupOperation CreateBoundMultipleLocalDeclarationsOperation(BoundMultipleLocalDeclarations boundMultipleLocalDeclarations)
        {
            // The syntax for the boundMultipleLocalDeclarations can either be a LocalDeclarationStatement or a VariableDeclaration, depending on the context
            // (using/fixed statements vs variable declaration)
            // We generate a DeclarationGroup for these scenarios (using/fixed) to maintain tree shape consistency across IOperation.
            SyntaxNode declarationGroupSyntax = boundMultipleLocalDeclarations.Syntax;
            SyntaxNode declarationSyntax = declarationGroupSyntax.IsKind(SyntaxKind.LocalDeclarationStatement) ?
                    ((LocalDeclarationStatementSyntax)declarationGroupSyntax).Declaration :
                    declarationGroupSyntax;
            bool declarationIsImplicit = boundMultipleLocalDeclarations.WasCompilerGenerated;
            IVariableDeclarationOperation multiVariableDeclaration = new CSharpLazyVariableDeclarationOperation(this, boundMultipleLocalDeclarations, _semanticModel, declarationSyntax, null, default, declarationIsImplicit);

            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            // If the syntax was the same, we're in a fixed statement or using statement. We make the Group operation implicit in this scenario, as the
            // syntax itself is a VariableDeclaration
            // We do the same if the declarationSyntax was a using declaration, as it's bound as if it were a using statement
            bool isUsing = declarationGroupSyntax.IsKind(SyntaxKind.LocalDeclarationStatement) && ((LocalDeclarationStatementSyntax)declarationGroupSyntax).UsingKeyword != default;
            bool isImplicit = declarationGroupSyntax == declarationSyntax || boundMultipleLocalDeclarations.WasCompilerGenerated || isUsing;
            return new VariableDeclarationGroupOperation(ImmutableArray.Create(multiVariableDeclaration), _semanticModel, declarationGroupSyntax, type, constantValue, isImplicit);
        }

        private ILabeledOperation CreateBoundLabelStatementOperation(BoundLabelStatement boundLabelStatement)
        {
            ILabelSymbol label = boundLabelStatement.Label;
            BoundNode statement = null;
            SyntaxNode syntax = boundLabelStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLabelStatement.WasCompilerGenerated;
            return new CSharpLazyLabeledOperation(this, statement, label, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILabeledOperation CreateBoundLabeledStatementOperation(BoundLabeledStatement boundLabeledStatement)
        {
            ILabelSymbol label = boundLabeledStatement.Label;
            BoundNode labeledStatement = boundLabeledStatement.Body;
            SyntaxNode syntax = boundLabeledStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLabeledStatement.WasCompilerGenerated;
            return new CSharpLazyLabeledOperation(this, labeledStatement, label, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IExpressionStatementOperation CreateBoundExpressionStatementOperation(BoundExpressionStatement boundExpressionStatement)
        {
            BoundNode expression = boundExpressionStatement.Expression;
            SyntaxNode syntax = boundExpressionStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);

            // lambda body can point to expression directly and binder can insert expression statement there. and end up statement pointing to
            // expression syntax node since there is no statement syntax node to point to. this will mark such one as implicit since it doesn't
            // actually exist in code
            bool isImplicit = boundExpressionStatement.WasCompilerGenerated || boundExpressionStatement.Syntax == boundExpressionStatement.Expression.Syntax;
            return new CSharpLazyExpressionStatementOperation(this, expression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal IOperation CreateBoundTupleLiteralOperation(BoundTupleLiteral boundTupleLiteral, bool createDeclaration = true)
        {
            return CreateTupleOperation(boundTupleLiteral, boundTupleLiteral.Type, createDeclaration);
        }

        internal IOperation CreateBoundConvertedTupleLiteralOperation(BoundConvertedTupleLiteral boundConvertedTupleLiteral, bool createDeclaration = true)
        {
            return CreateTupleOperation(boundConvertedTupleLiteral, boundConvertedTupleLiteral.SourceTuple.Type, createDeclaration);
        }

        internal IOperation CreateTupleOperation(BoundTupleExpression boundTupleExpression, ITypeSymbol naturalType, bool createDeclaration)
        {
            SyntaxNode syntax = boundTupleExpression.Syntax;
            bool isImplicit = boundTupleExpression.WasCompilerGenerated;
            ITypeSymbol type = boundTupleExpression.Type;
            Optional<object> constantValue = default;
            if (syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                syntax = declarationExpressionSyntax.Designation;
                if (createDeclaration)
                {
                    return new CSharpLazyDeclarationExpressionOperation(this, boundTupleExpression, _semanticModel, declarationExpressionSyntax, type, constantValue: default, isImplicit: false);
                }
            }

            return new CSharpLazyTupleOperation(this, boundTupleExpression, _semanticModel, syntax, type, naturalType, constantValue, isImplicit);
        }

        private IInterpolatedStringOperation CreateBoundInterpolatedStringExpressionOperation(BoundInterpolatedString boundInterpolatedString)
        {
            SyntaxNode syntax = boundInterpolatedString.Syntax;
            ITypeSymbol type = boundInterpolatedString.Type;
            Optional<object> constantValue = ConvertToOptional(boundInterpolatedString.ConstantValue);
            bool isImplicit = boundInterpolatedString.WasCompilerGenerated;
            return new CSharpLazyInterpolatedStringOperation(this, boundInterpolatedString, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal ImmutableArray<IInterpolatedStringContentOperation> CreateBoundInterpolatedStringContentOperation(ImmutableArray<BoundExpression> parts)
        {
            var builder = ArrayBuilder<IInterpolatedStringContentOperation>.GetInstance(parts.Length);
            foreach (var part in parts)
            {
                if (part.Kind == BoundKind.StringInsert)
                {
                    builder.Add((IInterpolatedStringContentOperation)Create(part));
                }
                else
                {
                    builder.Add(CreateBoundInterpolatedStringTextOperation((BoundLiteral)part));
                }
            }
            return builder.ToImmutableAndFree();
        }

        private IInterpolationOperation CreateBoundInterpolationOperation(BoundStringInsert boundStringInsert)
        {
            SyntaxNode syntax = boundStringInsert.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundStringInsert.WasCompilerGenerated;
            return new CSharpLazyInterpolationOperation(this, boundStringInsert, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInterpolatedStringTextOperation CreateBoundInterpolatedStringTextOperation(BoundLiteral boundNode)
        {
            SyntaxNode syntax = boundNode.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundNode.WasCompilerGenerated;
            return new CSharpLazyInterpolatedStringTextOperation(this, boundNode, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConstantPatternOperation CreateBoundConstantPatternOperation(BoundConstantPattern boundConstantPattern)
        {
            BoundNode value = boundConstantPattern.Value;
            SyntaxNode syntax = boundConstantPattern.Syntax;
            bool isImplicit = boundConstantPattern.WasCompilerGenerated;
            TypeSymbol inputType = boundConstantPattern.InputType;
            return new CSharpLazyConstantPatternOperation(inputType, this, value, _semanticModel, syntax, isImplicit);
        }

        private IDeclarationPatternOperation CreateBoundDeclarationPatternOperation(BoundDeclarationPattern boundDeclarationPattern)
        {
            ISymbol variable = boundDeclarationPattern.Variable;
            if (variable == null && boundDeclarationPattern.VariableAccess?.Kind == BoundKind.DiscardExpression)
            {
                variable = ((BoundDiscardExpression)boundDeclarationPattern.VariableAccess).ExpressionSymbol;
            }

            ITypeSymbol inputType = boundDeclarationPattern.InputType;
            bool acceptsNull = boundDeclarationPattern.IsVar;
            ITypeSymbol matchedType = acceptsNull ? null : boundDeclarationPattern.DeclaredType.Type;
            SyntaxNode syntax = boundDeclarationPattern.Syntax;
            bool isImplicit = boundDeclarationPattern.WasCompilerGenerated;
            return new DeclarationPatternOperation(inputType, matchedType, variable, acceptsNull, _semanticModel, syntax, isImplicit);
        }

        private IRecursivePatternOperation CreateBoundRecursivePatternOperation(BoundRecursivePattern boundRecursivePattern)
        {
            return new CSharpLazyRecursivePatternOperation(this, boundRecursivePattern, _semanticModel);
        }

        private IRecursivePatternOperation CreateBoundRecursivePatternOperation(BoundITuplePattern boundITuplePattern)
        {
            return new CSharpLazyITuplePatternOperation(this, boundITuplePattern, _semanticModel);
        }

        private ISwitchOperation CreateBoundSwitchStatementOperation(BoundSwitchStatement boundSwitchStatement)
        {
            ImmutableArray<ILocalSymbol> locals = boundSwitchStatement.InnerLocals.As<ILocalSymbol>();
            ILabelSymbol exitLabel = boundSwitchStatement.BreakLabel;
            SyntaxNode syntax = boundSwitchStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundSwitchStatement.WasCompilerGenerated;
            return new CSharpLazySwitchOperation(this, boundSwitchStatement, locals, exitLabel, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ISwitchCaseOperation CreateBoundSwitchSectionOperation(BoundSwitchSection boundSwitchSection)
        {
            ImmutableArray<ILocalSymbol> locals = StaticCast<ILocalSymbol>.From(boundSwitchSection.Locals);

            return new CSharpLazySwitchCaseOperation(this, boundSwitchSection, locals, _semanticModel, boundSwitchSection.Syntax, type: null, constantValue: default, isImplicit: boundSwitchSection.WasCompilerGenerated);
        }

        private ISwitchExpressionOperation CreateBoundSwitchExpressionOperation(BoundSwitchExpression boundSwitchExpression)
        {
            return new CSharpLazySwitchExpressionOperation(this, boundSwitchExpression, _semanticModel);
        }

        private ISwitchExpressionArmOperation CreateBoundSwitchExpressionArmOperation(BoundSwitchExpressionArm boundSwitchExpressionArm)
        {
            return new CSharpLazySwitchExpressionArmOperation(this, boundSwitchExpressionArm, _semanticModel);
        }

        private ICaseClauseOperation CreateBoundSwitchLabelOperation(BoundSwitchLabel boundSwitchLabel)
        {
            SyntaxNode syntax = boundSwitchLabel.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundSwitchLabel.WasCompilerGenerated;
            LabelSymbol label = boundSwitchLabel.Label;

            if (boundSwitchLabel.Syntax.Kind() == SyntaxKind.DefaultSwitchLabel)
            {
                Debug.Assert(boundSwitchLabel.Pattern.Kind == BoundKind.DiscardPattern);
                return new DefaultCaseClauseOperation(label, _semanticModel, syntax, type, constantValue, isImplicit);
            }
            else if (boundSwitchLabel.WhenClause == null &&
                     boundSwitchLabel.Pattern.Kind == BoundKind.ConstantPattern &&
                     boundSwitchLabel.Pattern is BoundConstantPattern cp &&
                     cp.InputType.IsValidV6SwitchGoverningType())
            {
                return new CSharpLazySingleValueCaseClauseOperation(this, cp.Value, label, _semanticModel, syntax, type, constantValue, isImplicit);
            }
            else
            {
                return new CSharpLazyPatternCaseClauseOperation(this, boundSwitchLabel, label, _semanticModel, syntax, type, constantValue, isImplicit);
            }
        }

        private IIsPatternOperation CreateBoundIsPatternExpressionOperation(BoundIsPatternExpression boundIsPatternExpression)
        {
            SyntaxNode syntax = boundIsPatternExpression.Syntax;
            ITypeSymbol type = boundIsPatternExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundIsPatternExpression.ConstantValue);
            bool isImplicit = boundIsPatternExpression.WasCompilerGenerated;
            return new CSharpLazyIsPatternOperation(this, boundIsPatternExpression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundQueryClauseOperation(BoundQueryClause boundQueryClause)
        {
            if (boundQueryClause.Syntax.Kind() != SyntaxKind.QueryExpression)
            {
                // Currently we have no IOperation APIs for different query clauses or continuation.
                return Create(boundQueryClause.Value);
            }

            BoundNode expression = boundQueryClause.Value;
            SyntaxNode syntax = boundQueryClause.Syntax;
            ITypeSymbol type = boundQueryClause.Type;
            Optional<object> constantValue = ConvertToOptional(boundQueryClause.ConstantValue);
            bool isImplicit = boundQueryClause.WasCompilerGenerated;
            return new CSharpLazyTranslatedQueryOperation(this, expression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundRangeVariableOperation(BoundRangeVariable boundRangeVariable)
        {
            // We do not have operation nodes for the bound range variables, just it's value.
            return Create(boundRangeVariable.Value);
        }

        private IOperation CreateBoundDiscardExpressionOperation(BoundDiscardExpression boundNode)
        {
            return new DiscardOperation(
                (IDiscardSymbol)boundNode.ExpressionSymbol,
                _semanticModel,
                boundNode.Syntax,
                boundNode.Type,
                ConvertToOptional(boundNode.ConstantValue),
                isImplicit: boundNode.WasCompilerGenerated);
        }

        private IOperation CreateFromEndIndexExpressionOperation(BoundFromEndIndexExpression boundIndex)
        {
            return new CSharpLazyUnaryOperation(
                operationFactory: this,
                boundIndex.Operand,
                UnaryOperatorKind.Hat,
                isLifted: boundIndex.Type.IsNullableType(),
                isChecked: false,
                operatorMethod: null,
                _semanticModel,
                boundIndex.Syntax,
                boundIndex.Type,
                constantValue: default,
                isImplicit: boundIndex.WasCompilerGenerated);
        }

        private IOperation CreateRangeExpressionOperation(BoundRangeExpression boundRange)
        {
            return new CSharpLazyRangeOperation(
                operationFactory: this,
                boundRange,
                isLifted: boundRange.Type.IsNullableType(),
                _semanticModel,
                boundRange.Syntax,
                boundRange.Type,
                boundRange.MethodOpt,
                isImplicit: boundRange.WasCompilerGenerated);
        }

        private IOperation CreateBoundDiscardPatternOperation(BoundDiscardPattern boundNode)
        {
            return new DiscardPatternOperation(
                boundNode.InputType,
                _semanticModel,
                boundNode.Syntax,
                isImplicit: boundNode.WasCompilerGenerated);
        }

        private IOperation CreateUsingLocalDeclarationsOperation(BoundUsingLocalDeclarations boundNode)
        {
            //TODO: Implement UsingLocalDeclaration operations correctly.
            //      For now we return an implicit operationNone,
            //      and GetIOperationChildren will return a single child
            //      consisting of the using declaration parsed as if it were a standard variable declaration.
            //      See: https://github.com/dotnet/roslyn/issues/32100
            return new CSharpLazyNoneOperation(
                this,
                boundNode,
                _semanticModel,
                boundNode.Syntax,
                constantValue: default,
                isImplicit: false);
        }

        internal IPropertySubpatternOperation CreatePropertySubpattern(BoundSubpattern subpattern, ITypeSymbol matchedType)
        {
            SyntaxNode syntax = subpattern.Syntax;
            return new CSharpLazyPropertySubpatternOperation(this, subpattern, matchedType, syntax, _semanticModel);
        }

        internal IOperation CreatePropertySubpatternMember(Symbol symbol, ITypeSymbol matchedType, SyntaxNode syntax)
        {
            var nameSyntax = (syntax is SubpatternSyntax subpatSyntax ? subpatSyntax.NameColon?.Name : null) ?? syntax;
            bool isImplicit = nameSyntax == syntax;
            switch (symbol)
            {
                case FieldSymbol field:
                    {
                        var constantValue = field.ConstantValue is null ? default(Optional<object>) : new Optional<object>(field.ConstantValue);
                        var receiver = new InstanceReferenceOperation(
                            InstanceReferenceKind.PatternInput, _semanticModel, nameSyntax, matchedType, constantValue, isImplicit: true);
                        return new FieldReferenceOperation(
                            field, isDeclaration: false, receiver, _semanticModel, nameSyntax, field.Type, constantValue, isImplicit: isImplicit);
                    }
                case PropertySymbol property:
                    {
                        var receiver = new InstanceReferenceOperation(
                            InstanceReferenceKind.PatternInput, _semanticModel, nameSyntax, matchedType, constantValue: default, isImplicit: true);
                        return new PropertyReferenceOperation(
                            property, ImmutableArray<IArgumentOperation>.Empty, receiver, _semanticModel, nameSyntax, property.Type,
                            constantValue: default, isImplicit: isImplicit);
                    }
                default:
                    // We should expose the symbol in this case somehow:
                    // https://github.com/dotnet/roslyn/issues/33175
                    return OperationFactory.CreateInvalidOperation(_semanticModel, nameSyntax, ImmutableArray<IOperation>.Empty, isImplicit);
            }
        }
    }
}
