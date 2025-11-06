// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        private readonly SemanticModel _semanticModel;

        public CSharpOperationFactory(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        [return: NotNullIfNotNull(nameof(boundNode))]
        public IOperation? Create(BoundNode? boundNode)
        {
            if (boundNode == null)
            {
                return null;
            }

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
                case BoundKind.Utf8String:
                    return CreateBoundUtf8StringOperation((BoundUtf8String)boundNode);
                case BoundKind.DynamicInvocation:
                    return CreateBoundDynamicInvocationExpressionOperation((BoundDynamicInvocation)boundNode);
                case BoundKind.DynamicIndexerAccess:
                    return CreateBoundDynamicIndexerAccessExpressionOperation((BoundDynamicIndexerAccess)boundNode);
                case BoundKind.ObjectCreationExpression:
                    return CreateBoundObjectCreationExpressionOperation((BoundObjectCreationExpression)boundNode);
                case BoundKind.WithExpression:
                    return CreateBoundWithExpressionOperation((BoundWithExpression)boundNode);
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
                case BoundKind.CollectionExpression:
                    return CreateBoundCollectionExpression((BoundCollectionExpression)boundNode);
                case BoundKind.DefaultLiteral:
                    return CreateBoundDefaultLiteralOperation((BoundDefaultLiteral)boundNode);
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
                case BoundKind.UserDefinedConditionalLogicalOperator:
                    return CreateBoundBinaryOperatorBase((BoundBinaryOperatorBase)boundNode);
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
                case BoundKind.ImplicitIndexerAccess:
                    return CreateBoundImplicitIndexerAccessOperation((BoundImplicitIndexerAccess)boundNode);
                case BoundKind.InlineArrayAccess:
                    return CreateBoundInlineArrayAccessOperation((BoundInlineArrayAccess)boundNode);
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
                case BoundKind.UsingLocalDeclarations:
                    return CreateBoundMultipleLocalDeclarationsBaseOperation((BoundMultipleLocalDeclarationsBase)boundNode);
                case BoundKind.LabelStatement:
                    return CreateBoundLabelStatementOperation((BoundLabelStatement)boundNode);
                case BoundKind.LabeledStatement:
                    return CreateBoundLabeledStatementOperation((BoundLabeledStatement)boundNode);
                case BoundKind.ExpressionStatement:
                    return CreateBoundExpressionStatementOperation((BoundExpressionStatement)boundNode);
                case BoundKind.TupleLiteral:
                case BoundKind.ConvertedTupleLiteral:
                    return CreateBoundTupleOperation((BoundTupleExpression)boundNode);
                case BoundKind.InterpolatedString:
                    return CreateBoundInterpolatedStringExpressionOperation((BoundInterpolatedString)boundNode);
                case BoundKind.StringInsert:
                    return CreateBoundInterpolationOperation((BoundStringInsert)boundNode);
                case BoundKind.LocalFunctionStatement:
                    return CreateBoundLocalFunctionStatementOperation((BoundLocalFunctionStatement)boundNode);
                case BoundKind.AnonymousObjectCreationExpression:
                    return CreateBoundAnonymousObjectCreationExpressionOperation((BoundAnonymousObjectCreationExpression)boundNode);
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
                case BoundKind.BinaryPattern:
                    return CreateBoundBinaryPatternOperation((BoundBinaryPattern)boundNode);
                case BoundKind.NegatedPattern:
                    return CreateBoundNegatedPatternOperation((BoundNegatedPattern)boundNode);
                case BoundKind.RelationalPattern:
                    return CreateBoundRelationalPatternOperation((BoundRelationalPattern)boundNode);
                case BoundKind.TypePattern:
                    return CreateBoundTypePatternOperation((BoundTypePattern)boundNode);
                case BoundKind.SlicePattern:
                    return CreateBoundSlicePatternOperation((BoundSlicePattern)boundNode);
                case BoundKind.ListPattern:
                    return CreateBoundListPatternOperation((BoundListPattern)boundNode);
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
                case BoundKind.ConvertedSwitchExpression:
                    return CreateBoundSwitchExpressionOperation((BoundConvertedSwitchExpression)boundNode);
                case BoundKind.SwitchExpressionArm:
                    return CreateBoundSwitchExpressionArmOperation((BoundSwitchExpressionArm)boundNode);
                case BoundKind.ObjectOrCollectionValuePlaceholder:
                    return CreateCollectionValuePlaceholderOperation((BoundObjectOrCollectionValuePlaceholder)boundNode);
                case BoundKind.FunctionPointerInvocation:
                    return CreateBoundFunctionPointerInvocationOperation((BoundFunctionPointerInvocation)boundNode);
                case BoundKind.UnconvertedAddressOfOperator:
                    return CreateBoundUnconvertedAddressOfOperatorOperation((BoundUnconvertedAddressOfOperator)boundNode);
                case BoundKind.InterpolatedStringArgumentPlaceholder:
                    return CreateBoundInterpolatedStringArgumentPlaceholder((BoundInterpolatedStringArgumentPlaceholder)boundNode);
                case BoundKind.InterpolatedStringHandlerPlaceholder:
                    return CreateBoundInterpolatedStringHandlerPlaceholder((BoundInterpolatedStringHandlerPlaceholder)boundNode);
                case BoundKind.Attribute:
                    return CreateBoundAttributeOperation((BoundAttribute)boundNode);

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
                    ConstantValue? constantValue = (boundNode as BoundExpression)?.ConstantValueOpt;
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
                    ImmutableArray<IOperation> children = GetIOperationChildren(boundNode);
                    ITypeSymbol? type = boundNode switch
                    {
                        BoundExpression boundExpr => boundExpr.GetPublicTypeSymbol(),
                        _ => null
                    };
                    return new NoneOperation(children, _semanticModel, boundNode.Syntax, type: type, constantValue, isImplicit: isImplicit);
                case BoundKind.ValuePlaceholder:
                    // The only supported use of BoundValuePlaceholder is within a collection expression as we use it
                    // to represents the elements passed to the collection builder creation methods.  We can hit these
                    // creation methods when producing the .CreationArguments for the ICollectionExpressionOperation.
                    // Note: the caller will end up stripping this off when producing the CreationArguments, so it will
                    // not actually leak to the user.  But this ends up keeping the logic simple between that callsite
                    // and this code which actually hits all the arguments passed along.
                    Debug.Assert(boundNode.Syntax is CollectionExpressionSyntax or WithElementSyntax);

                    // Because we never actually expose this placeholder in the IOp tree, it's fine to use .Unspecified
                    // as its kind here.
                    return new PlaceholderOperation(
                        PlaceholderKind.Unspecified, _semanticModel, boundNode.Syntax,
                        boundNode switch
                        {
                            BoundExpression boundExpr => boundExpr.GetPublicTypeSymbol(),
                            _ => null
                        }, boundNode.WasCompilerGenerated);
                case BoundKind.UnconvertedInterpolatedString:
                case BoundKind.UnconvertedConditionalOperator:
                case BoundKind.UnconvertedSwitchExpression:
                case BoundKind.AnonymousPropertyDeclaration:
                default:
                    // If you're hitting this because the IOperation test hook has failed, see
                    // <roslyn-root>/docs/Compilers/IOperation Test Hook.md for instructions on how to fix.
                    throw ExceptionUtilities.UnexpectedValue(boundNode.Kind);
            }
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

        private IMethodBodyOperation CreateMethodBodyOperation(BoundNonConstructorMethodBody boundNode)
        {
            return new MethodBodyOperation(
                (IBlockOperation?)Create(boundNode.BlockBody),
                (IBlockOperation?)Create(boundNode.ExpressionBody),
                 _semanticModel,
                 boundNode.Syntax,
                 isImplicit: boundNode.WasCompilerGenerated);
        }

        private IConstructorBodyOperation CreateConstructorBodyOperation(BoundConstructorMethodBody boundNode)
        {
            return new ConstructorBodyOperation(
                boundNode.Locals.GetPublicSymbols(),
                Create(boundNode.Initializer),
                (IBlockOperation?)Create(boundNode.BlockBody),
                (IBlockOperation?)Create(boundNode.ExpressionBody),
                _semanticModel,
                boundNode.Syntax,
                isImplicit: boundNode.WasCompilerGenerated);
        }

        internal ImmutableArray<IOperation> GetIOperationChildren(IBoundNodeWithIOperationChildren boundNodeWithChildren)
        {
            var children = boundNodeWithChildren.Children;
            if (children.IsDefaultOrEmpty)
            {
                return ImmutableArray<IOperation>.Empty;
            }

            var builder = ArrayBuilder<IOperation>.GetInstance(children.Length);
            foreach (BoundNode? childNode in children)
            {
                if (childNode == null)
                {
                    continue;
                }

                IOperation operation = Create(childNode);
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
                        var multipleDeclaration = (BoundMultipleLocalDeclarationsBase)declaration;
                        var builder = ArrayBuilder<IVariableDeclaratorOperation>.GetInstance(multipleDeclaration.LocalDeclarations.Length);
                        foreach (var decl in multipleDeclaration.LocalDeclarations)
                        {
                            builder.Add((IVariableDeclaratorOperation)CreateVariableDeclaratorInternal(decl, decl.Syntax));
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
            ITypeSymbol? type = boundDeconstructValuePlaceholder.GetPublicTypeSymbol();
            bool isImplicit = boundDeconstructValuePlaceholder.WasCompilerGenerated;
            return new PlaceholderOperation(PlaceholderKind.Unspecified, _semanticModel, syntax, type, isImplicit);
        }

        private IDeconstructionAssignmentOperation CreateBoundDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator boundDeconstructionAssignmentOperator)
        {
            IOperation target = Create(boundDeconstructionAssignmentOperator.Left);
            // Skip the synthetic deconstruction conversion wrapping the right operand. This is a compiler-generated conversion that we don't want to reflect
            // in the public API because it's an implementation detail.
            IOperation value = Create(boundDeconstructionAssignmentOperator.Right.Operand);
            SyntaxNode syntax = boundDeconstructionAssignmentOperator.Syntax;
            ITypeSymbol? type = boundDeconstructionAssignmentOperator.GetPublicTypeSymbol();
            bool isImplicit = boundDeconstructionAssignmentOperator.WasCompilerGenerated;
            return new DeconstructionAssignmentOperation(target, value, _semanticModel, syntax, type, isImplicit);
        }

        private IOperation CreateBoundCallOperation(BoundCall boundCall)
        {
            MethodSymbol targetMethod = boundCall.Method;
            SyntaxNode syntax = boundCall.Syntax;
            ITypeSymbol? type = boundCall.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundCall.ConstantValueOpt;
            bool isImplicit = boundCall.WasCompilerGenerated;

            if (boundCall.IsErroneousNode)
            {
                ImmutableArray<IOperation> children = CreateFromArray<BoundNode, IOperation>(((IBoundInvalidNode)boundCall).InvalidNodeChildren);
                return new InvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit);
            }

            TypeParameterSymbol? constrainedToType = GetConstrainedToType(targetMethod, boundCall.ReceiverOpt);
            bool isVirtual = constrainedToType is not null || IsCallVirtual(targetMethod, boundCall.ReceiverOpt);
            IOperation? receiver = CreateReceiverOperation(boundCall.ReceiverOpt, targetMethod);
            ImmutableArray<IArgumentOperation> arguments = DeriveArguments(boundCall);
            return new InvocationOperation(targetMethod.GetPublicSymbol(), constrainedToType.GetPublicSymbol(), receiver, isVirtual, arguments, _semanticModel, syntax, type, isImplicit);
        }

        private static TypeParameterSymbol? GetConstrainedToType(Symbol targetMember, BoundExpression? receiverOpt)
        {
            if (targetMember.IsStatic && (targetMember.IsAbstract || targetMember.IsVirtual) &&
                receiverOpt is BoundTypeExpression { Type: TypeParameterSymbol typeParameter })
            {
                return typeParameter;
            }

            return null;
        }

        private IOperation CreateBoundFunctionPointerInvocationOperation(BoundFunctionPointerInvocation boundFunctionPointerInvocation)
        {
            ITypeSymbol? type = boundFunctionPointerInvocation.GetPublicTypeSymbol();
            SyntaxNode syntax = boundFunctionPointerInvocation.Syntax;
            bool isImplicit = boundFunctionPointerInvocation.WasCompilerGenerated;

            if (boundFunctionPointerInvocation.ResultKind != LookupResultKind.Viable)
            {
                ImmutableArray<IOperation> children = CreateFromArray<BoundNode, IOperation>(((IBoundInvalidNode)boundFunctionPointerInvocation).InvalidNodeChildren);
                return new InvalidOperation(children, _semanticModel, syntax, type, constantValue: null, isImplicit);
            }

            var pointer = Create(boundFunctionPointerInvocation.InvokedExpression);
            var arguments = DeriveArguments(boundFunctionPointerInvocation);
            return new FunctionPointerInvocationOperation(pointer, arguments, _semanticModel, syntax, type, isImplicit);
        }

        private IOperation CreateBoundUnconvertedAddressOfOperatorOperation(BoundUnconvertedAddressOfOperator boundUnconvertedAddressOf)
        {
            return new AddressOfOperation(
                Create(boundUnconvertedAddressOf.Operand),
                _semanticModel,
                boundUnconvertedAddressOf.Syntax,
                boundUnconvertedAddressOf.GetPublicTypeSymbol(),
                boundUnconvertedAddressOf.WasCompilerGenerated);
        }

        private IOperation CreateBoundAttributeOperation(BoundAttribute boundAttribute)
        {
            var isAttributeImplicit = boundAttribute.WasCompilerGenerated;
            if (boundAttribute.Constructor is null)
            {
                var invalidOperation = OperationFactory.CreateInvalidOperation(_semanticModel, boundAttribute.Syntax, GetIOperationChildren(boundAttribute), isImplicit: true);
                return new AttributeOperation(invalidOperation, _semanticModel, boundAttribute.Syntax, isAttributeImplicit);
            }

            ObjectOrCollectionInitializerOperation? initializer = null;
            if (!boundAttribute.NamedArguments.IsEmpty)
            {
                var namedArguments = CreateFromArray<BoundAssignmentOperator, IOperation>(boundAttribute.NamedArguments);
                initializer = new ObjectOrCollectionInitializerOperation(namedArguments, _semanticModel, boundAttribute.Syntax, boundAttribute.GetPublicTypeSymbol(), isImplicit: true);
                Debug.Assert(initializer.Initializers.All(i => i is ISimpleAssignmentOperation));
            }

            var objectCreationOperation = new ObjectCreationOperation(boundAttribute.Constructor.GetPublicSymbol(), initializer, DeriveArguments(boundAttribute), _semanticModel, boundAttribute.Syntax, boundAttribute.GetPublicTypeSymbol(), boundAttribute.ConstantValueOpt, isImplicit: true);
            return new AttributeOperation(objectCreationOperation, _semanticModel, boundAttribute.Syntax, isAttributeImplicit);
        }

        internal ImmutableArray<IOperation> CreateIgnoredDimensions(BoundNode declaration)
        {
            switch (declaration.Kind)
            {
                case BoundKind.LocalDeclaration:
                    {
                        BoundTypeExpression? declaredTypeOpt = ((BoundLocalDeclaration)declaration).DeclaredTypeOpt;
                        Debug.Assert(declaredTypeOpt != null);
                        return CreateFromArray<BoundExpression, IOperation>(declaredTypeOpt.BoundDimensionsOpt);
                    }
                case BoundKind.MultipleLocalDeclarations:
                case BoundKind.UsingLocalDeclarations:
                    {
                        var declarations = ((BoundMultipleLocalDeclarationsBase)declaration).LocalDeclarations;
                        ImmutableArray<BoundExpression> dimensions;
                        if (declarations.Length > 0)
                        {
                            BoundTypeExpression? declaredTypeOpt = declarations[0].DeclaredTypeOpt;
                            Debug.Assert(declaredTypeOpt != null);
                            dimensions = declaredTypeOpt.BoundDimensionsOpt;
                        }
                        else
                        {
                            dimensions = ImmutableArray<BoundExpression>.Empty;
                        }
                        return CreateFromArray<BoundExpression, IOperation>(dimensions);
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(declaration.Kind);
            }
        }

        internal IOperation CreateBoundLocalOperation(BoundLocal boundLocal, bool createDeclaration = true)
        {
            ILocalSymbol local = boundLocal.LocalSymbol.GetPublicSymbol();
            bool isDeclaration = boundLocal.DeclarationKind != BoundLocalDeclarationKind.None;
            SyntaxNode syntax = boundLocal.Syntax;
            ITypeSymbol? type = boundLocal.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundLocal.ConstantValueOpt;
            bool isImplicit = boundLocal.WasCompilerGenerated;
            if (isDeclaration && syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                syntax = declarationExpressionSyntax.Designation;
                if (createDeclaration)
                {
                    IOperation localReference = CreateBoundLocalOperation(boundLocal, createDeclaration: false);
                    return new DeclarationExpressionOperation(localReference, _semanticModel, declarationExpressionSyntax, type, isImplicit: false);
                }
            }
            return new LocalReferenceOperation(local, isDeclaration, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal IOperation CreateBoundFieldAccessOperation(BoundFieldAccess boundFieldAccess, bool createDeclaration = true)
        {
            IFieldSymbol field = boundFieldAccess.FieldSymbol.GetPublicSymbol();
            bool isDeclaration = boundFieldAccess.IsDeclaration;
            SyntaxNode syntax = boundFieldAccess.Syntax;
            ITypeSymbol? type = boundFieldAccess.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundFieldAccess.ConstantValueOpt;
            bool isImplicit = boundFieldAccess.WasCompilerGenerated;
            if (isDeclaration && syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                syntax = declarationExpressionSyntax.Designation;

                if (createDeclaration)
                {
                    IOperation fieldAccess = CreateBoundFieldAccessOperation(boundFieldAccess, createDeclaration: false);
                    return new DeclarationExpressionOperation(fieldAccess, _semanticModel, declarationExpressionSyntax, type, isImplicit: false);
                }
            }

            IOperation? instance = CreateReceiverOperation(boundFieldAccess.ReceiverOpt, boundFieldAccess.FieldSymbol);
            return new FieldReferenceOperation(field, isDeclaration, instance, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal IOperation? CreateBoundPropertyReferenceInstance(BoundNode boundNode)
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

        private IPropertyReferenceOperation CreateBoundPropertyAccessOperation(BoundPropertyAccess boundPropertyAccess)
        {
            IOperation? instance = CreateReceiverOperation(boundPropertyAccess.ReceiverOpt, boundPropertyAccess.PropertySymbol);
            var arguments = ImmutableArray<IArgumentOperation>.Empty;
            IPropertySymbol property = boundPropertyAccess.PropertySymbol.GetPublicSymbol();
            SyntaxNode syntax = boundPropertyAccess.Syntax;
            ITypeSymbol? type = boundPropertyAccess.GetPublicTypeSymbol();
            bool isImplicit = boundPropertyAccess.WasCompilerGenerated;
            TypeParameterSymbol? constrainedToType = GetConstrainedToType(boundPropertyAccess.PropertySymbol, boundPropertyAccess.ReceiverOpt);
            return new PropertyReferenceOperation(property, constrainedToType.GetPublicSymbol(), arguments, instance, _semanticModel, syntax, type, isImplicit);
        }

        private IOperation CreateBoundIndexerAccessOperation(BoundIndexerAccess boundIndexerAccess)
        {
            PropertySymbol property = boundIndexerAccess.Indexer;
            SyntaxNode syntax = boundIndexerAccess.Syntax;
            ITypeSymbol? type = boundIndexerAccess.GetPublicTypeSymbol();
            bool isImplicit = boundIndexerAccess.WasCompilerGenerated;

            if (!boundIndexerAccess.OriginalIndexersOpt.IsDefault || boundIndexerAccess.ResultKind == LookupResultKind.OverloadResolutionFailure)
            {
                var children = CreateFromArray<BoundNode, IOperation>(((IBoundInvalidNode)boundIndexerAccess).InvalidNodeChildren);
                return new InvalidOperation(children, _semanticModel, syntax, type, constantValue: null, isImplicit);
            }

            ImmutableArray<IArgumentOperation> arguments = DeriveArguments(boundIndexerAccess);
            IOperation? instance = CreateReceiverOperation(boundIndexerAccess.ReceiverOpt, boundIndexerAccess.ExpressionSymbol);
            TypeParameterSymbol? constrainedToType = GetConstrainedToType(property, boundIndexerAccess.ReceiverOpt);
            return new PropertyReferenceOperation(property.GetPublicSymbol(), constrainedToType.GetPublicSymbol(), arguments, instance, _semanticModel, syntax, type, isImplicit);
        }

        private IEventReferenceOperation CreateBoundEventAccessOperation(BoundEventAccess boundEventAccess)
        {
            IEventSymbol @event = boundEventAccess.EventSymbol.GetPublicSymbol();
            IOperation? instance = CreateReceiverOperation(boundEventAccess.ReceiverOpt, boundEventAccess.EventSymbol);
            SyntaxNode syntax = boundEventAccess.Syntax;
            ITypeSymbol? type = boundEventAccess.GetPublicTypeSymbol();
            bool isImplicit = boundEventAccess.WasCompilerGenerated;
            TypeParameterSymbol? constrainedToType = GetConstrainedToType(boundEventAccess.EventSymbol, boundEventAccess.ReceiverOpt);
            return new EventReferenceOperation(@event, constrainedToType.GetPublicSymbol(), instance, _semanticModel, syntax, type, isImplicit);
        }

        private IEventAssignmentOperation CreateBoundEventAssignmentOperatorOperation(BoundEventAssignmentOperator boundEventAssignmentOperator)
        {
            IOperation eventReference = CreateBoundEventAccessOperation(boundEventAssignmentOperator);
            IOperation handlerValue = Create(boundEventAssignmentOperator.Argument);
            SyntaxNode syntax = boundEventAssignmentOperator.Syntax;
            bool adds = boundEventAssignmentOperator.IsAddition;
            ITypeSymbol? type = boundEventAssignmentOperator.GetPublicTypeSymbol();
            bool isImplicit = boundEventAssignmentOperator.WasCompilerGenerated;
            return new EventAssignmentOperation(eventReference, handlerValue, adds, _semanticModel, syntax, type, isImplicit);
        }

        private IParameterReferenceOperation CreateBoundParameterOperation(BoundParameter boundParameter)
        {
            IParameterSymbol parameter = boundParameter.ParameterSymbol.GetPublicSymbol();
            SyntaxNode syntax = boundParameter.Syntax;
            ITypeSymbol? type = boundParameter.GetPublicTypeSymbol();
            bool isImplicit = boundParameter.WasCompilerGenerated;
            return new ParameterReferenceOperation(parameter, _semanticModel, syntax, type, isImplicit);
        }

        internal ILiteralOperation CreateBoundLiteralOperation(BoundLiteral boundLiteral, bool @implicit = false)
        {
            SyntaxNode syntax = boundLiteral.Syntax;
            ITypeSymbol? type = boundLiteral.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundLiteral.ConstantValueOpt;
            bool isImplicit = boundLiteral.WasCompilerGenerated || @implicit;
            return new LiteralOperation(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IUtf8StringOperation CreateBoundUtf8StringOperation(BoundUtf8String boundNode)
        {
            SyntaxNode syntax = boundNode.Syntax;
            ITypeSymbol? type = boundNode.GetPublicTypeSymbol();
            bool isImplicit = boundNode.WasCompilerGenerated;
            return new Utf8StringOperation(boundNode.Value, _semanticModel, syntax, type, isImplicit);
        }

        private IAnonymousObjectCreationOperation CreateBoundAnonymousObjectCreationExpressionOperation(BoundAnonymousObjectCreationExpression boundAnonymousObjectCreationExpression)
        {
            SyntaxNode syntax = boundAnonymousObjectCreationExpression.Syntax;
            ITypeSymbol? type = boundAnonymousObjectCreationExpression.GetPublicTypeSymbol();
            Debug.Assert(type is not null);
            bool isImplicit = boundAnonymousObjectCreationExpression.WasCompilerGenerated;
            ImmutableArray<IOperation> initializers = GetAnonymousObjectCreationInitializers(boundAnonymousObjectCreationExpression.Arguments, boundAnonymousObjectCreationExpression.Declarations, syntax, type, isImplicit);
            return new AnonymousObjectCreationOperation(initializers, _semanticModel, syntax, type, isImplicit);
        }

        private IOperation CreateBoundObjectCreationExpressionOperation(BoundObjectCreationExpression boundObjectCreationExpression)
        {
            MethodSymbol constructor = boundObjectCreationExpression.Constructor;
            SyntaxNode syntax = boundObjectCreationExpression.Syntax;
            ITypeSymbol? type = boundObjectCreationExpression.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundObjectCreationExpression.ConstantValueOpt;
            bool isImplicit = boundObjectCreationExpression.WasCompilerGenerated;

            Debug.Assert(constructor is not null);

            if (boundObjectCreationExpression.ResultKind == LookupResultKind.OverloadResolutionFailure || constructor.OriginalDefinition is ErrorMethodSymbol)
            {
                var children = CreateFromArray<BoundNode, IOperation>(((IBoundInvalidNode)boundObjectCreationExpression).InvalidNodeChildren);
                return new InvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit);
            }
            else if (boundObjectCreationExpression.Type.IsAnonymousType)
            {
                // Workaround for https://github.com/dotnet/roslyn/issues/28157
                Debug.Assert(isImplicit);
                Debug.Assert(type is not null);
                ImmutableArray<IOperation> initializers = GetAnonymousObjectCreationInitializers(
                    boundObjectCreationExpression.Arguments,
                    declarations: ImmutableArray<BoundAnonymousPropertyDeclaration>.Empty,
                    syntax,
                    type,
                    isImplicit);
                return new AnonymousObjectCreationOperation(initializers, _semanticModel, syntax, type, isImplicit);
            }

            ImmutableArray<IArgumentOperation> arguments = DeriveArguments(boundObjectCreationExpression);
            IObjectOrCollectionInitializerOperation? initializer = (IObjectOrCollectionInitializerOperation?)Create(boundObjectCreationExpression.InitializerExpressionOpt);

            return new ObjectCreationOperation(constructor.GetPublicSymbol(), initializer, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundWithExpressionOperation(BoundWithExpression boundWithExpression)
        {
            IOperation operand = Create(boundWithExpression.Receiver);
            IObjectOrCollectionInitializerOperation initializer = (IObjectOrCollectionInitializerOperation)Create(boundWithExpression.InitializerExpression);
            MethodSymbol? constructor = boundWithExpression.CloneMethod;
            SyntaxNode syntax = boundWithExpression.Syntax;
            ITypeSymbol? type = boundWithExpression.GetPublicTypeSymbol();
            bool isImplicit = boundWithExpression.WasCompilerGenerated;
            return new WithOperation(operand, constructor.GetPublicSymbol(), initializer, _semanticModel, syntax, type, isImplicit);
        }

        private IDynamicObjectCreationOperation CreateBoundDynamicObjectCreationExpressionOperation(BoundDynamicObjectCreationExpression boundDynamicObjectCreationExpression)
        {
            IObjectOrCollectionInitializerOperation? initializer = (IObjectOrCollectionInitializerOperation?)Create(boundDynamicObjectCreationExpression.InitializerExpressionOpt);
            ImmutableArray<IOperation> arguments = CreateFromArray<BoundExpression, IOperation>(boundDynamicObjectCreationExpression.Arguments);
            ImmutableArray<string?> argumentNames = boundDynamicObjectCreationExpression.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicObjectCreationExpression.ArgumentRefKindsOpt.NullToEmpty();
            SyntaxNode syntax = boundDynamicObjectCreationExpression.Syntax;
            ITypeSymbol? type = boundDynamicObjectCreationExpression.GetPublicTypeSymbol();
            bool isImplicit = boundDynamicObjectCreationExpression.WasCompilerGenerated;
            return new DynamicObjectCreationOperation(initializer, arguments, argumentNames, argumentRefKinds, _semanticModel, syntax, type, isImplicit);
        }

        internal IOperation CreateBoundDynamicInvocationExpressionReceiver(BoundNode receiver)
        {
            switch (receiver)
            {
                case BoundObjectOrCollectionValuePlaceholder implicitReceiver:
                    return CreateBoundDynamicMemberAccessOperation(implicitReceiver, typeArgumentsOpt: ImmutableArray<TypeSymbol>.Empty, memberName: "Add",
                                                                   implicitReceiver.Syntax, type: null, isImplicit: true);

                case BoundMethodGroup methodGroup:
                    return CreateBoundDynamicMemberAccessOperation(methodGroup.ReceiverOpt, TypeMap.AsTypeSymbols(methodGroup.TypeArgumentsOpt), methodGroup.Name,
                                                                   methodGroup.Syntax, methodGroup.GetPublicTypeSymbol(), methodGroup.WasCompilerGenerated);

                default:
                    return Create(receiver);
            }
        }

        private IDynamicInvocationOperation CreateBoundDynamicInvocationExpressionOperation(BoundDynamicInvocation boundDynamicInvocation)
        {
            IOperation operation = CreateBoundDynamicInvocationExpressionReceiver(boundDynamicInvocation.Expression);
            ImmutableArray<IOperation> arguments = CreateFromArray<BoundExpression, IOperation>(boundDynamicInvocation.Arguments);
            ImmutableArray<string?> argumentNames = boundDynamicInvocation.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicInvocation.ArgumentRefKindsOpt.NullToEmpty();
            SyntaxNode syntax = boundDynamicInvocation.Syntax;
            ITypeSymbol? type = boundDynamicInvocation.GetPublicTypeSymbol();
            bool isImplicit = boundDynamicInvocation.WasCompilerGenerated;
            return new DynamicInvocationOperation(operation, arguments, argumentNames, argumentRefKinds, _semanticModel, syntax, type, isImplicit);
        }

        internal IOperation CreateBoundDynamicIndexerAccessExpressionReceiver(BoundExpression indexer)
        {
            switch (indexer)
            {
                case BoundDynamicIndexerAccess boundDynamicIndexerAccess:
                    return Create(boundDynamicIndexerAccess.Receiver);

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
                    Debug.Assert(!boundObjectInitializerMember.Expanded);
                    return CreateFromArray<BoundExpression, IOperation>(boundObjectInitializerMember.Arguments);

                default:
                    throw ExceptionUtilities.UnexpectedValue(indexer.Kind);
            }
        }

        private IDynamicIndexerAccessOperation CreateBoundDynamicIndexerAccessExpressionOperation(BoundDynamicIndexerAccess boundDynamicIndexerAccess)
        {
            IOperation operation = CreateBoundDynamicIndexerAccessExpressionReceiver(boundDynamicIndexerAccess);
            ImmutableArray<IOperation> arguments = CreateBoundDynamicIndexerAccessArguments(boundDynamicIndexerAccess);
            ImmutableArray<string?> argumentNames = boundDynamicIndexerAccess.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicIndexerAccess.ArgumentRefKindsOpt.NullToEmpty();
            SyntaxNode syntax = boundDynamicIndexerAccess.Syntax;
            ITypeSymbol? type = boundDynamicIndexerAccess.GetPublicTypeSymbol();
            bool isImplicit = boundDynamicIndexerAccess.WasCompilerGenerated;
            return new DynamicIndexerAccessOperation(operation, arguments, argumentNames, argumentRefKinds, _semanticModel, syntax, type, isImplicit);
        }

        private IObjectOrCollectionInitializerOperation CreateBoundObjectInitializerExpressionOperation(BoundObjectInitializerExpression boundObjectInitializerExpression)
        {
            ImmutableArray<IOperation> initializers = CreateFromArray<BoundExpression, IOperation>(BoundObjectCreationExpression.GetChildInitializers(boundObjectInitializerExpression));
            SyntaxNode syntax = boundObjectInitializerExpression.Syntax;
            ITypeSymbol? type = boundObjectInitializerExpression.GetPublicTypeSymbol();
            bool isImplicit = boundObjectInitializerExpression.WasCompilerGenerated;
            return new ObjectOrCollectionInitializerOperation(initializers, _semanticModel, syntax, type, isImplicit);
        }

        private IObjectOrCollectionInitializerOperation CreateBoundCollectionInitializerExpressionOperation(BoundCollectionInitializerExpression boundCollectionInitializerExpression)
        {
            ImmutableArray<IOperation> initializers = CreateFromArray<BoundExpression, IOperation>(BoundObjectCreationExpression.GetChildInitializers(boundCollectionInitializerExpression));
            SyntaxNode syntax = boundCollectionInitializerExpression.Syntax;
            ITypeSymbol? type = boundCollectionInitializerExpression.GetPublicTypeSymbol();
            bool isImplicit = boundCollectionInitializerExpression.WasCompilerGenerated;
            return new ObjectOrCollectionInitializerOperation(initializers, _semanticModel, syntax, type, isImplicit);
        }

        private IOperation CreateBoundObjectInitializerMemberOperation(BoundObjectInitializerMember boundObjectInitializerMember, bool isObjectOrCollectionInitializer = false)
        {
            Symbol? memberSymbol = boundObjectInitializerMember.MemberSymbol;
            SyntaxNode syntax = boundObjectInitializerMember.Syntax;
            ITypeSymbol? type = boundObjectInitializerMember.GetPublicTypeSymbol();
            bool isImplicit = boundObjectInitializerMember.WasCompilerGenerated;

            if ((object?)memberSymbol == null)
            {
                Debug.Assert(boundObjectInitializerMember.Type.IsDynamic());

                IOperation operation = CreateBoundDynamicIndexerAccessExpressionReceiver(boundObjectInitializerMember);
                ImmutableArray<IOperation> arguments = CreateBoundDynamicIndexerAccessArguments(boundObjectInitializerMember);
                ImmutableArray<string?> argumentNames = boundObjectInitializerMember.ArgumentNamesOpt.NullToEmpty();
                ImmutableArray<RefKind> argumentRefKinds = boundObjectInitializerMember.ArgumentRefKindsOpt.NullToEmpty();
                return new DynamicIndexerAccessOperation(operation, arguments, argumentNames, argumentRefKinds, _semanticModel, syntax, type, isImplicit);
            }

            switch (memberSymbol.Kind)
            {
                case SymbolKind.Field:
                    var field = (FieldSymbol)memberSymbol;
                    bool isDeclaration = false;
                    return new FieldReferenceOperation(field.GetPublicSymbol(), isDeclaration, createReceiver(), _semanticModel, syntax, type, constantValue: null, isImplicit);
                case SymbolKind.Event:
                    var eventSymbol = (EventSymbol)memberSymbol;
                    return new EventReferenceOperation(eventSymbol.GetPublicSymbol(), constrainedToType: null, createReceiver(), _semanticModel, syntax, type, isImplicit);
                case SymbolKind.Property:
                    var property = (PropertySymbol)memberSymbol;

                    ImmutableArray<IArgumentOperation> arguments;
                    if (!boundObjectInitializerMember.Arguments.IsEmpty)
                    {
                        // In nested member initializers, the property is not actually set. Instead, it is retrieved for a series of Add method calls or nested property setter calls,
                        // so we need to use the getter for this property
                        MethodSymbol? accessor = isObjectOrCollectionInitializer || property.RefKind != RefKind.None
                            ? property.GetOwnOrInheritedGetMethod()
                            : property.GetOwnOrInheritedSetMethod();
                        if (accessor == null || boundObjectInitializerMember.ResultKind == LookupResultKind.OverloadResolutionFailure || accessor.OriginalDefinition is ErrorMethodSymbol)
                        {
                            var children = CreateFromArray<BoundNode, IOperation>(((IBoundInvalidNode)boundObjectInitializerMember).InvalidNodeChildren);
                            return new InvalidOperation(children, _semanticModel, syntax, type, constantValue: null, isImplicit);
                        }

                        arguments = DeriveArguments(boundObjectInitializerMember);
                    }
                    else
                    {
                        arguments = ImmutableArray<IArgumentOperation>.Empty;
                    }

                    return new PropertyReferenceOperation(property.GetPublicSymbol(), constrainedToType: null, arguments, createReceiver(), _semanticModel, syntax, type, isImplicit);
                default:
                    throw ExceptionUtilities.UnexpectedValue(memberSymbol.Kind);
            }

            IOperation? createReceiver() => memberSymbol?.IsStatic == true ?
                    null :
                    CreateImplicitReceiver(boundObjectInitializerMember.Syntax, boundObjectInitializerMember.ReceiverType);
        }

        private IOperation CreateBoundDynamicObjectInitializerMemberOperation(BoundDynamicObjectInitializerMember boundDynamicObjectInitializerMember)
        {
            IOperation instanceReceiver = CreateImplicitReceiver(boundDynamicObjectInitializerMember.Syntax, boundDynamicObjectInitializerMember.ReceiverType);
            string memberName = boundDynamicObjectInitializerMember.MemberName;
            ImmutableArray<ITypeSymbol> typeArguments = ImmutableArray<ITypeSymbol>.Empty;
            ITypeSymbol containingType = boundDynamicObjectInitializerMember.ReceiverType.GetPublicSymbol();
            SyntaxNode syntax = boundDynamicObjectInitializerMember.Syntax;
            ITypeSymbol? type = boundDynamicObjectInitializerMember.GetPublicTypeSymbol();
            bool isImplicit = boundDynamicObjectInitializerMember.WasCompilerGenerated;

            return new DynamicMemberReferenceOperation(instanceReceiver, memberName, typeArguments, containingType, _semanticModel, syntax, type, isImplicit);
        }

        private IOperation CreateBoundCollectionElementInitializerOperation(BoundCollectionElementInitializer boundCollectionElementInitializer)
        {
            MethodSymbol addMethod = boundCollectionElementInitializer.AddMethod;
            IOperation? receiver = CreateReceiverOperation(boundCollectionElementInitializer.ImplicitReceiverOpt, addMethod);
            ImmutableArray<IArgumentOperation> arguments = DeriveArguments(boundCollectionElementInitializer);
            SyntaxNode syntax = boundCollectionElementInitializer.Syntax;
            ITypeSymbol? type = boundCollectionElementInitializer.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundCollectionElementInitializer.ConstantValueOpt;
            bool isImplicit = boundCollectionElementInitializer.WasCompilerGenerated;

            if (IsMethodInvalid(boundCollectionElementInitializer.ResultKind, addMethod))
            {
                var children = CreateFromArray<BoundNode, IOperation>(((IBoundInvalidNode)boundCollectionElementInitializer).InvalidNodeChildren);
                return new InvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit);
            }

            bool isVirtual = IsCallVirtual(addMethod, boundCollectionElementInitializer.ImplicitReceiverOpt);
            return new InvocationOperation(addMethod.GetPublicSymbol(), constrainedToType: null, receiver, isVirtual, arguments, _semanticModel, syntax, type, isImplicit);
        }

        private IDynamicMemberReferenceOperation CreateBoundDynamicMemberAccessOperation(BoundDynamicMemberAccess boundDynamicMemberAccess)
        {
            return CreateBoundDynamicMemberAccessOperation(boundDynamicMemberAccess.Receiver, TypeMap.AsTypeSymbols(boundDynamicMemberAccess.TypeArgumentsOpt), boundDynamicMemberAccess.Name,
                boundDynamicMemberAccess.Syntax, boundDynamicMemberAccess.GetPublicTypeSymbol(), boundDynamicMemberAccess.WasCompilerGenerated);
        }

        private IDynamicMemberReferenceOperation CreateBoundDynamicMemberAccessOperation(
            BoundExpression? receiver,
            ImmutableArray<TypeSymbol> typeArgumentsOpt,
            string memberName,
            SyntaxNode syntaxNode,
            ITypeSymbol? type,
            bool isImplicit)
        {
            ITypeSymbol? containingType = null;
            if (receiver?.Kind == BoundKind.TypeExpression)
            {
                containingType = receiver.GetPublicTypeSymbol();
                receiver = null;
            }

            ImmutableArray<ITypeSymbol> typeArguments = ImmutableArray<ITypeSymbol>.Empty;
            if (!typeArgumentsOpt.IsDefault)
            {
                typeArguments = typeArgumentsOpt.GetPublicSymbols();
            }

            IOperation? instance = Create(receiver);
            return new DynamicMemberReferenceOperation(instance, memberName, typeArguments, containingType, _semanticModel, syntaxNode, type, isImplicit);
        }

        private IDynamicInvocationOperation CreateBoundDynamicCollectionElementInitializerOperation(BoundDynamicCollectionElementInitializer boundCollectionElementInitializer)
        {
            IOperation operation = CreateBoundDynamicInvocationExpressionReceiver(boundCollectionElementInitializer.Expression);
            ImmutableArray<IOperation> arguments = CreateFromArray<BoundExpression, IOperation>(boundCollectionElementInitializer.Arguments);
            SyntaxNode syntax = boundCollectionElementInitializer.Syntax;
            ITypeSymbol? type = boundCollectionElementInitializer.GetPublicTypeSymbol();
            bool isImplicit = boundCollectionElementInitializer.WasCompilerGenerated;
            return new DynamicInvocationOperation(operation, arguments, argumentNames: ImmutableArray<string?>.Empty, argumentRefKinds: ImmutableArray<RefKind>.Empty, _semanticModel, syntax, type, isImplicit);
        }

        private IOperation CreateUnboundLambdaOperation(UnboundLambda unboundLambda)
        {
            // We want to ensure that we never see the UnboundLambda node, and that we don't end up having two different IOperation
            // nodes for the lambda expression. So, we ask the semantic model for the IOperation node for the unbound lambda syntax.
            // We are counting on the fact that will do the error recovery and actually create the BoundLambda node appropriate for
            // this syntax node.
            BoundLambda boundLambda = unboundLambda.BindForErrorRecovery();
            return Create(boundLambda);
        }

        private IAnonymousFunctionOperation CreateBoundLambdaOperation(BoundLambda boundLambda)
        {
            IMethodSymbol symbol = boundLambda.Symbol.GetPublicSymbol();
            IBlockOperation body = (IBlockOperation)Create(boundLambda.Body);
            SyntaxNode syntax = boundLambda.Syntax;
            bool isImplicit = boundLambda.WasCompilerGenerated;
            return new AnonymousFunctionOperation(symbol, body, _semanticModel, syntax, isImplicit);
        }

        private ILocalFunctionOperation CreateBoundLocalFunctionStatementOperation(BoundLocalFunctionStatement boundLocalFunctionStatement)
        {
            IBlockOperation? body = (IBlockOperation?)Create(boundLocalFunctionStatement.Body);
            IBlockOperation? ignoredBody = boundLocalFunctionStatement is { BlockBody: { }, ExpressionBody: { } exprBody }
                ? (IBlockOperation?)Create(exprBody)
                : null;
            IMethodSymbol symbol = boundLocalFunctionStatement.Symbol.GetPublicSymbol();
            SyntaxNode syntax = boundLocalFunctionStatement.Syntax;
            bool isImplicit = boundLocalFunctionStatement.WasCompilerGenerated;
            return new LocalFunctionOperation(symbol, body, ignoredBody, _semanticModel, syntax, isImplicit);
        }

        private IOperation CreateBoundConversionOperation(BoundConversion boundConversion, bool forceOperandImplicitLiteral = false)
        {
            Debug.Assert(!forceOperandImplicitLiteral || boundConversion.Operand is BoundLiteral);
            bool isImplicit = boundConversion.WasCompilerGenerated || !boundConversion.ExplicitCastInCode || forceOperandImplicitLiteral;
            BoundExpression boundOperand = boundConversion.Operand;

            if (boundConversion.ConversionKind == ConversionKind.InterpolatedStringHandler)
            {
                Debug.Assert(!forceOperandImplicitLiteral);
                Debug.Assert(!boundOperand.GetInterpolatedStringHandlerData().IsDefault);
                return CreateInterpolatedStringHandler(boundConversion);
            }

            if (boundConversion.ConversionKind == CSharp.ConversionKind.MethodGroup)
            {
                SyntaxNode syntax = boundConversion.Syntax;
                ITypeSymbol? type = boundConversion.GetPublicTypeSymbol();
                Debug.Assert(!forceOperandImplicitLiteral);

                if (boundConversion.Type is FunctionPointerTypeSymbol)
                {
                    Debug.Assert(boundConversion.SymbolOpt is object);
                    return new AddressOfOperation(
                        CreateBoundMethodGroupSingleMethodOperation((BoundMethodGroup)boundConversion.Operand, boundConversion.SymbolOpt, suppressVirtualCalls: false),
                        _semanticModel, syntax, type, boundConversion.WasCompilerGenerated);
                }

                // We don't check HasErrors on the conversion here because if we actually have a MethodGroup conversion,
                // overload resolution succeeded. The resulting method could be invalid for other reasons, but we don't
                // hide the resolved method.
                IOperation target = CreateDelegateTargetOperation(boundConversion);

                // If this was an explicit tuple expression conversion, such as ((Action, int))(M, 1), we will be "explicit", because the
                // original conversion was explicit in code, but the syntax node for this delegate creation and the nested method group will
                // be the same. We therefore need to mark this node as implicit to ensure we don't have two explicit nodes for the same syntax.
                Debug.Assert(isImplicit || target.Syntax != syntax || target.IsImplicit || boundConversion.ConversionGroupOpt != null);

                isImplicit = isImplicit || (target.Syntax == syntax && !target.IsImplicit);

                return new DelegateCreationOperation(target, _semanticModel, syntax, type, isImplicit);
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
                    Debug.Assert(!forceOperandImplicitLiteral);
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
                        Debug.Assert(!forceOperandImplicitLiteral);
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

                ITypeSymbol? type = boundConversion.GetPublicTypeSymbol();
                ConstantValue? constantValue = boundConversion.ConstantValueOpt;

                // If this is a lambda or method group conversion to a delegate type, we return a delegate creation instead of a conversion
                if ((boundOperand.Kind == BoundKind.Lambda ||
                     boundOperand.Kind == BoundKind.UnboundLambda ||
                     boundOperand.Kind == BoundKind.MethodGroup) &&
                    boundConversion.Type.IsDelegateType())
                {
                    IOperation target = CreateDelegateTargetOperation(correctedConversionNode);
                    return new DelegateCreationOperation(target, _semanticModel, syntax, type, isImplicit);
                }
                else
                {
                    bool isTryCast = false;
                    // Checked conversions only matter if the conversion is a Numeric conversion. Don't have true unless the conversion is actually numeric.
                    bool isChecked = boundConversion.Checked && (conversion.IsNumeric || (boundConversion.SymbolOpt is not null && SyntaxFacts.IsCheckedOperator(boundConversion.SymbolOpt.Name)));
                    IOperation operand = forceOperandImplicitLiteral
                        ? CreateBoundLiteralOperation((BoundLiteral)correctedConversionNode.Operand, @implicit: true)
                        : Create(correctedConversionNode.Operand);
                    return new ConversionOperation(operand, conversion, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit);
                }
            }
        }

        private IConversionOperation CreateBoundAsOperatorOperation(BoundAsOperator boundAsOperator)
        {
            IOperation operand = Create(boundAsOperator.Operand);
            SyntaxNode syntax = boundAsOperator.Syntax;
            Conversion conversion = BoundNode.GetConversion(boundAsOperator.OperandConversion, boundAsOperator.OperandPlaceholder);
            bool isTryCast = true;
            bool isChecked = false;
            ITypeSymbol? type = boundAsOperator.GetPublicTypeSymbol();
            bool isImplicit = boundAsOperator.WasCompilerGenerated;
            return new ConversionOperation(operand, conversion, isTryCast, isChecked, _semanticModel, syntax, type, constantValue: null, isImplicit);
        }

        private IDelegateCreationOperation CreateBoundDelegateCreationExpressionOperation(BoundDelegateCreationExpression boundDelegateCreationExpression)
        {
            IOperation target = CreateDelegateTargetOperation(boundDelegateCreationExpression);
            SyntaxNode syntax = boundDelegateCreationExpression.Syntax;
            ITypeSymbol? type = boundDelegateCreationExpression.GetPublicTypeSymbol();
            bool isImplicit = boundDelegateCreationExpression.WasCompilerGenerated;
            return new DelegateCreationOperation(target, _semanticModel, syntax, type, isImplicit);
        }

        private IMethodReferenceOperation CreateBoundMethodGroupSingleMethodOperation(BoundMethodGroup boundMethodGroup, MethodSymbol methodSymbol, bool suppressVirtualCalls)
        {
            TypeParameterSymbol? constrainedToType = GetConstrainedToType(methodSymbol, boundMethodGroup.ReceiverOpt);
            bool isVirtual = constrainedToType is not null || ((methodSymbol.IsAbstract || methodSymbol.IsOverride || methodSymbol.IsVirtual) && !suppressVirtualCalls);
            IOperation? instance = CreateReceiverOperation(boundMethodGroup.ReceiverOpt, methodSymbol);
            SyntaxNode bindingSyntax = boundMethodGroup.Syntax;
            ITypeSymbol? bindingType = null;
            bool isImplicit = boundMethodGroup.WasCompilerGenerated;
            return new MethodReferenceOperation(methodSymbol.GetPublicSymbol(), constrainedToType.GetPublicSymbol(), isVirtual, instance, _semanticModel, bindingSyntax, bindingType, isImplicit);
        }

        private IIsTypeOperation CreateBoundIsOperatorOperation(BoundIsOperator boundIsOperator)
        {
            IOperation value = Create(boundIsOperator.Operand);
            ITypeSymbol? typeOperand = boundIsOperator.TargetType.GetPublicTypeSymbol();
            Debug.Assert(typeOperand is not null);
            SyntaxNode syntax = boundIsOperator.Syntax;
            ITypeSymbol? type = boundIsOperator.GetPublicTypeSymbol();
            bool isNegated = false;
            bool isImplicit = boundIsOperator.WasCompilerGenerated;
            return new IsTypeOperation(value, typeOperand, isNegated, _semanticModel, syntax, type, isImplicit);
        }

        private ISizeOfOperation CreateBoundSizeOfOperatorOperation(BoundSizeOfOperator boundSizeOfOperator)
        {
            ITypeSymbol? typeOperand = boundSizeOfOperator.SourceType.GetPublicTypeSymbol();
            Debug.Assert(typeOperand is not null);
            SyntaxNode syntax = boundSizeOfOperator.Syntax;
            ITypeSymbol? type = boundSizeOfOperator.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundSizeOfOperator.ConstantValueOpt;
            bool isImplicit = boundSizeOfOperator.WasCompilerGenerated;
            return new SizeOfOperation(typeOperand, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ITypeOfOperation CreateBoundTypeOfOperatorOperation(BoundTypeOfOperator boundTypeOfOperator)
        {
            ITypeSymbol? typeOperand = boundTypeOfOperator.SourceType.GetPublicTypeSymbol();
            Debug.Assert(typeOperand is not null);
            SyntaxNode syntax = boundTypeOfOperator.Syntax;
            ITypeSymbol? type = boundTypeOfOperator.GetPublicTypeSymbol();
            bool isImplicit = boundTypeOfOperator.WasCompilerGenerated;
            return new TypeOfOperation(typeOperand, _semanticModel, syntax, type, isImplicit);
        }

        private IArrayCreationOperation CreateBoundArrayCreationOperation(BoundArrayCreation boundArrayCreation)
        {
            ImmutableArray<IOperation> dimensionSizes = CreateFromArray<BoundExpression, IOperation>(boundArrayCreation.Bounds);
            IArrayInitializerOperation? arrayInitializer = (IArrayInitializerOperation?)Create(boundArrayCreation.InitializerOpt);
            SyntaxNode syntax = boundArrayCreation.Syntax;
            ITypeSymbol? type = boundArrayCreation.GetPublicTypeSymbol();
            bool isImplicit = boundArrayCreation.WasCompilerGenerated ||
                              (boundArrayCreation.InitializerOpt?.Syntax == syntax && !boundArrayCreation.InitializerOpt.WasCompilerGenerated);
            return new ArrayCreationOperation(dimensionSizes, arrayInitializer, _semanticModel, syntax, type, isImplicit);
        }

        private IArrayInitializerOperation CreateBoundArrayInitializationOperation(BoundArrayInitialization boundArrayInitialization)
        {
            ImmutableArray<IOperation> elementValues = CreateFromArray<BoundExpression, IOperation>(boundArrayInitialization.Initializers);
            SyntaxNode syntax = boundArrayInitialization.Syntax;
            bool isImplicit = boundArrayInitialization.WasCompilerGenerated;
            return new ArrayInitializerOperation(elementValues, _semanticModel, syntax, isImplicit);
        }

        private ICollectionExpressionOperation CreateBoundCollectionExpression(BoundCollectionExpression expr)
        {
            var compilation = (CSharpCompilation)_semanticModel.Compilation;
            SyntaxNode syntax = expr.Syntax;
            ITypeSymbol? collectionType = expr.GetPublicTypeSymbol();
            bool isImplicit = expr.WasCompilerGenerated;
            IMethodSymbol? constructMethod = getConstructMethod(compilation, expr).GetPublicSymbol();
            ImmutableArray<IOperation> elements = expr.Elements.SelectAsArray((element, expr) => CreateBoundCollectionExpressionElement(expr, element), expr);
            return new CollectionExpressionOperation(
                constructMethod,
                getCreationArguments(this, expr),
                elements,
                _semanticModel,
                syntax,
                collectionType,
                isImplicit);

            static MethodSymbol? getConstructMethod(CSharpCompilation compilation, BoundCollectionExpression expr)
            {
                switch (expr.CollectionTypeKind)
                {
                    case CollectionExpressionTypeKind.None:
                    case CollectionExpressionTypeKind.Array:
                    case CollectionExpressionTypeKind.ReadOnlySpan:
                    case CollectionExpressionTypeKind.Span:
                        return null;
                    case CollectionExpressionTypeKind.ArrayInterface:
                    case CollectionExpressionTypeKind.ImplementsIEnumerable:
                        return (expr.CollectionCreation as BoundObjectCreationExpression)?.Constructor;
                    case CollectionExpressionTypeKind.CollectionBuilder:
                        return expr.CollectionBuilderMethod;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(expr.CollectionTypeKind);
                }
            }

            static ImmutableArray<IArgumentOperation> getCreationArguments(
                CSharpOperationFactory @this, BoundCollectionExpression expr)
            {
                var collectionCreation = expr.CollectionCreation;
                if (collectionCreation is { HasAnyErrors: true })
                    return [];

                while (collectionCreation is BoundConversion conversion)
                    collectionCreation = conversion.Operand;

                if (collectionCreation is BoundObjectCreationExpression)
                    return @this.DeriveArguments(collectionCreation);

                if (collectionCreation is BoundCall)
                {
                    // With a CollectionBuilder, the last argument will be a placeholder where the .Elements will go.
                    // We do *not* want to include that information in the Arguments we return.
                    var arguments = @this.DeriveArguments(collectionCreation);

                    Debug.Assert(arguments.Length > 0, "We should always have at least one argument (the placeholder elements).");
                    return arguments is [.. var normalArguments, _]
                        ? normalArguments
                        : arguments;
                }

                return [];
            }
        }

        private IOperation CreateBoundCollectionExpressionElement(BoundCollectionExpression expr, BoundNode element)
        {
            return element is BoundCollectionExpressionSpreadElement spreadElement ?
                CreateBoundCollectionExpressionSpreadElement(expr, spreadElement) :
                Create(Binder.GetUnderlyingCollectionExpressionElement(expr, (BoundExpression)element, throwOnErrors: false));
        }

        private ISpreadOperation CreateBoundCollectionExpressionSpreadElement(BoundCollectionExpression expr, BoundCollectionExpressionSpreadElement element)
        {
            var iteratorItem = element.IteratorBody is { } iteratorBody ?
                Binder.GetUnderlyingCollectionExpressionElement(expr, ((BoundExpressionStatement)iteratorBody).Expression, throwOnErrors: false) :
                null;
            var collection = Create(element.Expression);
            SyntaxNode syntax = element.Syntax;
            bool isImplicit = element.WasCompilerGenerated;
            var elementType = element.EnumeratorInfoOpt?.ElementType.GetPublicSymbol();
            var elementConversion = BoundNode.GetConversion(iteratorItem, element.ElementPlaceholder);
            return new SpreadOperation(
                collection,
                elementType: elementType,
                elementConversion,
                _semanticModel,
                syntax,
                isImplicit);
        }

        private IDefaultValueOperation CreateBoundDefaultLiteralOperation(BoundDefaultLiteral boundDefaultLiteral)
        {
            SyntaxNode syntax = boundDefaultLiteral.Syntax;
            ConstantValue? constantValue = boundDefaultLiteral.ConstantValueOpt;
            bool isImplicit = boundDefaultLiteral.WasCompilerGenerated;
            return new DefaultValueOperation(_semanticModel, syntax, type: null, constantValue, isImplicit);
        }

        private IDefaultValueOperation CreateBoundDefaultExpressionOperation(BoundDefaultExpression boundDefaultExpression)
        {
            SyntaxNode syntax = boundDefaultExpression.Syntax;
            ITypeSymbol? type = boundDefaultExpression.GetPublicTypeSymbol();
            ConstantValue? constantValue = ((BoundExpression)boundDefaultExpression).ConstantValueOpt;
            bool isImplicit = boundDefaultExpression.WasCompilerGenerated;
            return new DefaultValueOperation(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundBaseReferenceOperation(BoundBaseReference boundBaseReference)
        {
            InstanceReferenceKind referenceKind = InstanceReferenceKind.ContainingTypeInstance;
            SyntaxNode syntax = boundBaseReference.Syntax;
            ITypeSymbol? type = boundBaseReference.GetPublicTypeSymbol();
            bool isImplicit = boundBaseReference.WasCompilerGenerated;
            return new InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundThisReferenceOperation(BoundThisReference boundThisReference)
        {
            InstanceReferenceKind referenceKind = InstanceReferenceKind.ContainingTypeInstance;
            SyntaxNode syntax = boundThisReference.Syntax;
            ITypeSymbol? type = boundThisReference.GetPublicTypeSymbol();
            bool isImplicit = boundThisReference.WasCompilerGenerated;
            return new InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, isImplicit);
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

            IOperation target = Create(boundAssignmentOperator.Left);
            IOperation value = Create(boundAssignmentOperator.Right);
            bool isRef = boundAssignmentOperator.IsRef;
            SyntaxNode syntax = boundAssignmentOperator.Syntax;
            ITypeSymbol? type = boundAssignmentOperator.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundAssignmentOperator.ConstantValueOpt;
            bool isImplicit = boundAssignmentOperator.WasCompilerGenerated;
            return new SimpleAssignmentOperation(isRef, target, value, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IMemberInitializerOperation CreateBoundMemberInitializerOperation(BoundAssignmentOperator boundAssignmentOperator)
        {
            Debug.Assert(IsMemberInitializer(boundAssignmentOperator));
            IOperation initializedMember = CreateMemberInitializerInitializedMember(boundAssignmentOperator.Left);
            IObjectOrCollectionInitializerOperation initializer = (IObjectOrCollectionInitializerOperation)Create(boundAssignmentOperator.Right);
            SyntaxNode syntax = boundAssignmentOperator.Syntax;
            ITypeSymbol? type = boundAssignmentOperator.GetPublicTypeSymbol();
            bool isImplicit = boundAssignmentOperator.WasCompilerGenerated;
            return new MemberInitializerOperation(initializedMember, initializer, _semanticModel, syntax, type, isImplicit);
        }

        private ICompoundAssignmentOperation CreateBoundCompoundAssignmentOperatorOperation(BoundCompoundAssignmentOperator boundCompoundAssignmentOperator)
        {
            IOperation target = Create(boundCompoundAssignmentOperator.Left);
            IOperation value = Create(boundCompoundAssignmentOperator.Right);
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundCompoundAssignmentOperator.Operator.Kind);
            Conversion inConversion = BoundNode.GetConversion(boundCompoundAssignmentOperator.LeftConversion, boundCompoundAssignmentOperator.LeftPlaceholder);
            Conversion outConversion = BoundNode.GetConversion(boundCompoundAssignmentOperator.FinalConversion, boundCompoundAssignmentOperator.FinalPlaceholder);
            bool isLifted = boundCompoundAssignmentOperator.Operator.Kind.IsLifted();
            var method = boundCompoundAssignmentOperator.Operator.Method;
            bool isChecked = boundCompoundAssignmentOperator.Operator.Kind.IsChecked() || (method is not null && SyntaxFacts.IsCheckedOperator(method.Name));
            IMethodSymbol? operatorMethod = method.GetPublicSymbol();
            SyntaxNode syntax = boundCompoundAssignmentOperator.Syntax;
            ITypeSymbol? type = boundCompoundAssignmentOperator.GetPublicTypeSymbol();
            bool isImplicit = boundCompoundAssignmentOperator.WasCompilerGenerated;
            return new CompoundAssignmentOperation(inConversion, outConversion, operatorKind, isLifted, isChecked, operatorMethod,
                                                   GetConstrainedToTypeForOperator(method, boundCompoundAssignmentOperator.Operator.ConstrainedToTypeOpt).GetPublicSymbol(),
                                                   target, value, _semanticModel, syntax, type, isImplicit);
        }

        private static TypeParameterSymbol? GetConstrainedToTypeForOperator(MethodSymbol? operatorMethod, TypeSymbol? constrainedToTypeOpt)
        {
            if (operatorMethod is not null && operatorMethod.IsStatic && (operatorMethod.IsAbstract || operatorMethod.IsVirtual) &&
                constrainedToTypeOpt is TypeParameterSymbol typeParameter)
            {
                return typeParameter;
            }

            return null;
        }

        private IIncrementOrDecrementOperation CreateBoundIncrementOperatorOperation(BoundIncrementOperator boundIncrementOperator)
        {
            OperationKind operationKind = Helper.IsDecrement(boundIncrementOperator.OperatorKind) ? OperationKind.Decrement : OperationKind.Increment;
            bool isPostfix = Helper.IsPostfixIncrementOrDecrement(boundIncrementOperator.OperatorKind);
            bool isLifted = boundIncrementOperator.OperatorKind.IsLifted();
            bool isChecked = boundIncrementOperator.OperatorKind.IsChecked() || (boundIncrementOperator.MethodOpt is not null && SyntaxFacts.IsCheckedOperator(boundIncrementOperator.MethodOpt.Name));
            IOperation target = Create(boundIncrementOperator.Operand);
            IMethodSymbol? operatorMethod = boundIncrementOperator.MethodOpt.GetPublicSymbol();
            SyntaxNode syntax = boundIncrementOperator.Syntax;
            ITypeSymbol? type = boundIncrementOperator.GetPublicTypeSymbol();
            bool isImplicit = boundIncrementOperator.WasCompilerGenerated;
            return new IncrementOrDecrementOperation(isPostfix, isLifted, isChecked, target, operatorMethod,
                                                     GetConstrainedToTypeForOperator(boundIncrementOperator.MethodOpt, boundIncrementOperator.ConstrainedToTypeOpt).GetPublicSymbol(),
                                                     operationKind, _semanticModel, syntax, type, isImplicit);
        }

        private IInvalidOperation CreateBoundBadExpressionOperation(BoundBadExpression boundBadExpression)
        {
            SyntaxNode syntax = boundBadExpression.Syntax;
            // We match semantic model here: if the expression IsMissing, we have a null type, rather than the ErrorType of the bound node.
            ITypeSymbol? type = syntax.IsMissing ? null : boundBadExpression.GetPublicTypeSymbol();

            // if child has syntax node point to same syntax node as bad expression, then this invalid expression is implicit
            bool isImplicit = boundBadExpression.WasCompilerGenerated || boundBadExpression.ChildBoundNodes.Any(static (e, boundBadExpression) => e?.Syntax == boundBadExpression.Syntax, boundBadExpression);
            var children = CreateFromArray<BoundExpression, IOperation>(boundBadExpression.ChildBoundNodes);
            return new InvalidOperation(children, _semanticModel, syntax, type, constantValue: null, isImplicit);
        }

        private ITypeParameterObjectCreationOperation CreateBoundNewTOperation(BoundNewT boundNewT)
        {
            IObjectOrCollectionInitializerOperation? initializer = (IObjectOrCollectionInitializerOperation?)Create(boundNewT.InitializerExpressionOpt);
            SyntaxNode syntax = boundNewT.Syntax;
            ITypeSymbol? type = boundNewT.GetPublicTypeSymbol();
            bool isImplicit = boundNewT.WasCompilerGenerated;
            return new TypeParameterObjectCreationOperation(initializer, _semanticModel, syntax, type, isImplicit);
        }

        private INoPiaObjectCreationOperation CreateNoPiaObjectCreationExpressionOperation(BoundNoPiaObjectCreationExpression creation)
        {
            IObjectOrCollectionInitializerOperation? initializer = (IObjectOrCollectionInitializerOperation?)Create(creation.InitializerExpressionOpt);
            SyntaxNode syntax = creation.Syntax;
            ITypeSymbol? type = creation.GetPublicTypeSymbol();
            bool isImplicit = creation.WasCompilerGenerated;
            return new NoPiaObjectCreationOperation(initializer, _semanticModel, syntax, type, isImplicit);
        }

        private IUnaryOperation CreateBoundUnaryOperatorOperation(BoundUnaryOperator boundUnaryOperator)
        {
            UnaryOperatorKind unaryOperatorKind = Helper.DeriveUnaryOperatorKind(boundUnaryOperator.OperatorKind);
            IOperation operand = Create(boundUnaryOperator.Operand);
            IMethodSymbol? operatorMethod = boundUnaryOperator.MethodOpt.GetPublicSymbol();
            SyntaxNode syntax = boundUnaryOperator.Syntax;
            ITypeSymbol? type = boundUnaryOperator.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundUnaryOperator.ConstantValueOpt;
            bool isLifted = boundUnaryOperator.OperatorKind.IsLifted();
            bool isChecked = boundUnaryOperator.OperatorKind.IsChecked() || (boundUnaryOperator.MethodOpt is not null && SyntaxFacts.IsCheckedOperator(boundUnaryOperator.MethodOpt.Name));
            bool isImplicit = boundUnaryOperator.WasCompilerGenerated;
            return new UnaryOperation(unaryOperatorKind, operand, isLifted, isChecked, operatorMethod,
                                      GetConstrainedToTypeForOperator(boundUnaryOperator.MethodOpt, boundUnaryOperator.ConstrainedToTypeOpt).GetPublicSymbol(),
                                      _semanticModel, syntax, type, constantValue, isImplicit);
        }
        private IOperation CreateBoundBinaryOperatorBase(BoundBinaryOperatorBase boundBinaryOperatorBase)
        {
            if (boundBinaryOperatorBase is BoundBinaryOperator { InterpolatedStringHandlerData: not null } binary)
            {
                return CreateBoundInterpolatedStringBinaryOperator(binary);
            }

            // Binary operators can be nested _many_ levels deep, and cause a stack overflow if we manually recurse.
            // To solve this, we use a manual stack for the left side.
            var stack = ArrayBuilder<BoundBinaryOperatorBase>.GetInstance();
            BoundBinaryOperatorBase? currentBinary = boundBinaryOperatorBase;

            do
            {
                stack.Push(currentBinary);
                currentBinary = currentBinary.Left as BoundBinaryOperatorBase;
            } while (currentBinary is not null and not BoundBinaryOperator { InterpolatedStringHandlerData: not null });

            Debug.Assert(stack.Count > 0);
            IOperation? left = null;

            while (stack.TryPop(out currentBinary))
            {
                left ??= Create(currentBinary.Left);
                IOperation right = Create(currentBinary.Right);
                left = currentBinary switch
                {
                    BoundBinaryOperator binaryOp => CreateBoundBinaryOperatorOperation(binaryOp, left, right),
                    BoundUserDefinedConditionalLogicalOperator logicalOp => createBoundUserDefinedConditionalLogicalOperator(logicalOp, left, right),
                    { Kind: var kind } => throw ExceptionUtilities.UnexpectedValue(kind)
                };
            }

            Debug.Assert(left is not null && stack.Count == 0);
            stack.Free();
            return left;

            IBinaryOperation createBoundUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator boundBinaryOperator, IOperation left, IOperation right)
            {
                BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundBinaryOperator.OperatorKind);
                IMethodSymbol operatorMethod = boundBinaryOperator.LogicalOperator.GetPublicSymbol();
                IMethodSymbol unaryOperatorMethod = boundBinaryOperator.OperatorKind.Operator() == CSharp.BinaryOperatorKind.And ?
                                                        boundBinaryOperator.FalseOperator.GetPublicSymbol() :
                                                        boundBinaryOperator.TrueOperator.GetPublicSymbol();
                SyntaxNode syntax = boundBinaryOperator.Syntax;
                ITypeSymbol? type = boundBinaryOperator.GetPublicTypeSymbol();
                ConstantValue? constantValue = boundBinaryOperator.ConstantValueOpt;
                bool isLifted = boundBinaryOperator.OperatorKind.IsLifted();
                bool isChecked = boundBinaryOperator.OperatorKind.IsChecked();
                bool isCompareText = false;
                bool isImplicit = boundBinaryOperator.WasCompilerGenerated;
                TypeSymbol? constrainedToTypeOpt = GetConstrainedToTypeForOperator(boundBinaryOperator.LogicalOperator, boundBinaryOperator.ConstrainedToTypeOpt) ??
                                                   GetConstrainedToTypeForOperator(boundBinaryOperator.OperatorKind.Operator() == CSharp.BinaryOperatorKind.And ?
                                                                                       boundBinaryOperator.FalseOperator :
                                                                                       boundBinaryOperator.TrueOperator,
                                                                                   boundBinaryOperator.ConstrainedToTypeOpt);
                return new BinaryOperation(operatorKind, left, right, isLifted, isChecked, isCompareText, operatorMethod, constrainedToTypeOpt.GetPublicSymbol(), unaryOperatorMethod,
                                           _semanticModel, syntax, type, constantValue, isImplicit);
            }
        }

        private IBinaryOperation CreateBoundBinaryOperatorOperation(BoundBinaryOperator boundBinaryOperator, IOperation left, IOperation right)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundBinaryOperator.OperatorKind);
            MethodSymbol? binaryOperatorMethod = boundBinaryOperator.BinaryOperatorMethod;
            MethodSymbol? unaryOperatorMethod = boundBinaryOperator.LeftTruthOperatorMethod;

            SyntaxNode syntax = boundBinaryOperator.Syntax;
            ITypeSymbol? type = boundBinaryOperator.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundBinaryOperator.ConstantValueOpt;
            bool isLifted = boundBinaryOperator.OperatorKind.IsLifted();
            bool isChecked = boundBinaryOperator.OperatorKind.IsChecked() || (binaryOperatorMethod is not null && SyntaxFacts.IsCheckedOperator(binaryOperatorMethod.Name));
            bool isCompareText = false;
            bool isImplicit = boundBinaryOperator.WasCompilerGenerated;
            return new BinaryOperation(operatorKind, left, right, isLifted, isChecked, isCompareText,
                                       binaryOperatorMethod.GetPublicSymbol(),
                                       GetConstrainedToTypeForOperator(binaryOperatorMethod, boundBinaryOperator.ConstrainedToType).GetPublicSymbol(),
                                       unaryOperatorMethod.GetPublicSymbol(),
                                       _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundInterpolatedStringBinaryOperator(BoundBinaryOperator boundBinaryOperator)
        {
            Debug.Assert(boundBinaryOperator.InterpolatedStringHandlerData is not null);
            Func<BoundInterpolatedString, int, (CSharpOperationFactory, InterpolatedStringHandlerData), IOperation> createInterpolatedString
                = createInterpolatedStringOperand;

            Func<BoundBinaryOperator, IOperation, IOperation, (CSharpOperationFactory, InterpolatedStringHandlerData), IOperation> createBinaryOperator
                = createBoundBinaryOperatorOperation;

            return boundBinaryOperator.RewriteInterpolatedStringAddition((this, boundBinaryOperator.InterpolatedStringHandlerData.GetValueOrDefault()), createInterpolatedString, createBinaryOperator);

            static IInterpolatedStringOperation createInterpolatedStringOperand(
                BoundInterpolatedString boundInterpolatedString,
                int i,
                (CSharpOperationFactory @this, InterpolatedStringHandlerData Data) arg)
                => arg.@this.CreateBoundInterpolatedStringExpressionOperation(boundInterpolatedString, arg.Data.PositionInfo[i]);

            static IBinaryOperation createBoundBinaryOperatorOperation(
                BoundBinaryOperator boundBinaryOperator,
                IOperation left,
                IOperation right,
                (CSharpOperationFactory @this, InterpolatedStringHandlerData _) arg)
                => arg.@this.CreateBoundBinaryOperatorOperation(boundBinaryOperator, left, right);
        }

        private ITupleBinaryOperation CreateBoundTupleBinaryOperatorOperation(BoundTupleBinaryOperator boundTupleBinaryOperator)
        {
            IOperation left = Create(boundTupleBinaryOperator.Left);
            IOperation right = Create(boundTupleBinaryOperator.Right);
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundTupleBinaryOperator.OperatorKind);
            SyntaxNode syntax = boundTupleBinaryOperator.Syntax;
            ITypeSymbol? type = boundTupleBinaryOperator.GetPublicTypeSymbol();
            bool isImplicit = boundTupleBinaryOperator.WasCompilerGenerated;
            return new TupleBinaryOperation(operatorKind, left, right, _semanticModel, syntax, type, isImplicit);
        }

        private IConditionalOperation CreateBoundConditionalOperatorOperation(BoundConditionalOperator boundConditionalOperator)
        {
            IOperation condition = Create(boundConditionalOperator.Condition);
            IOperation whenTrue = Create(boundConditionalOperator.Consequence);
            IOperation whenFalse = Create(boundConditionalOperator.Alternative);
            bool isRef = boundConditionalOperator.IsRef;
            SyntaxNode syntax = boundConditionalOperator.Syntax;
            ITypeSymbol? type = boundConditionalOperator.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundConditionalOperator.ConstantValueOpt;
            bool isImplicit = boundConditionalOperator.WasCompilerGenerated;
            return new ConditionalOperation(condition, whenTrue, whenFalse, isRef, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICoalesceOperation CreateBoundNullCoalescingOperatorOperation(BoundNullCoalescingOperator boundNullCoalescingOperator)
        {
            IOperation value = Create(boundNullCoalescingOperator.LeftOperand);
            IOperation whenNull = Create(boundNullCoalescingOperator.RightOperand);
            SyntaxNode syntax = boundNullCoalescingOperator.Syntax;
            ITypeSymbol? type = boundNullCoalescingOperator.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundNullCoalescingOperator.ConstantValueOpt;
            bool isImplicit = boundNullCoalescingOperator.WasCompilerGenerated;
            Conversion valueConversion = BoundNode.GetConversion(boundNullCoalescingOperator.LeftConversion, boundNullCoalescingOperator.LeftPlaceholder);

            if (valueConversion.Exists && !valueConversion.IsIdentity &&
                boundNullCoalescingOperator.Type.Equals(boundNullCoalescingOperator.LeftOperand.Type?.StrippedType(), TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
            {
                valueConversion = Conversion.Identity;
            }

            return new CoalesceOperation(value, whenNull, valueConversion, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundNullCoalescingAssignmentOperatorOperation(BoundNullCoalescingAssignmentOperator boundNode)
        {
            IOperation target = Create(boundNode.LeftOperand);
            IOperation value = Create(boundNode.RightOperand);
            SyntaxNode syntax = boundNode.Syntax;
            ITypeSymbol? type = boundNode.GetPublicTypeSymbol();
            bool isImplicit = boundNode.WasCompilerGenerated;

            return new CoalesceAssignmentOperation(target, value, _semanticModel, syntax, type, isImplicit);
        }

        private IAwaitOperation CreateBoundAwaitExpressionOperation(BoundAwaitExpression boundAwaitExpression)
        {
            IOperation awaitedValue = Create(boundAwaitExpression.Expression);
            SyntaxNode syntax = boundAwaitExpression.Syntax;
            ITypeSymbol? type = boundAwaitExpression.GetPublicTypeSymbol();
            bool isImplicit = boundAwaitExpression.WasCompilerGenerated;
            return new AwaitOperation(awaitedValue, _semanticModel, syntax, type, isImplicit);
        }

        private IArrayElementReferenceOperation CreateBoundArrayAccessOperation(BoundArrayAccess boundArrayAccess)
        {
            IOperation arrayReference = Create(boundArrayAccess.Expression);
            ImmutableArray<IOperation> indices = CreateFromArray<BoundExpression, IOperation>(boundArrayAccess.Indices);
            SyntaxNode syntax = boundArrayAccess.Syntax;
            ITypeSymbol? type = boundArrayAccess.GetPublicTypeSymbol();
            bool isImplicit = boundArrayAccess.WasCompilerGenerated;

            return new ArrayElementReferenceOperation(arrayReference, indices, _semanticModel, syntax, type, isImplicit);
        }

        private IOperation CreateBoundImplicitIndexerAccessOperation(BoundImplicitIndexerAccess boundIndexerAccess)
        {
            IOperation instance = Create(boundIndexerAccess.Receiver);
            IOperation argument = Create(boundIndexerAccess.Argument);
            SyntaxNode syntax = boundIndexerAccess.Syntax;
            ITypeSymbol? type = boundIndexerAccess.GetPublicTypeSymbol();
            bool isImplicit = boundIndexerAccess.WasCompilerGenerated;

            if (boundIndexerAccess.LengthOrCountAccess.Kind == BoundKind.ArrayLength)
            {
                return new ArrayElementReferenceOperation(instance, ImmutableArray.Create(argument), _semanticModel, syntax, type, isImplicit);
            }

            var lengthSymbol = Binder.GetPropertySymbol(boundIndexerAccess.LengthOrCountAccess, out _, out _).GetPublicSymbol();
            var indexerSymbol = Binder.GetIndexerOrImplicitIndexerSymbol(boundIndexerAccess).GetPublicSymbol();

            Debug.Assert(lengthSymbol is not null);
            Debug.Assert(indexerSymbol is not null);

            return new ImplicitIndexerReferenceOperation(instance, argument, lengthSymbol, indexerSymbol, _semanticModel, syntax, type, isImplicit);
        }

        private IInlineArrayAccessOperation CreateBoundInlineArrayAccessOperation(BoundInlineArrayAccess boundInlineArrayAccess)
        {
            IOperation arrayReference = Create(boundInlineArrayAccess.Expression);
            IOperation argument = Create(boundInlineArrayAccess.Argument);
            SyntaxNode syntax = boundInlineArrayAccess.Syntax;
            ITypeSymbol? type = boundInlineArrayAccess.GetPublicTypeSymbol();
            bool isImplicit = boundInlineArrayAccess.WasCompilerGenerated;

            return new InlineArrayAccessOperation(arrayReference, argument, _semanticModel, syntax, type, isImplicit);
        }

        private INameOfOperation CreateBoundNameOfOperatorOperation(BoundNameOfOperator boundNameOfOperator)
        {
            IOperation argument = Create(boundNameOfOperator.Argument);
            SyntaxNode syntax = boundNameOfOperator.Syntax;
            ITypeSymbol? type = boundNameOfOperator.GetPublicTypeSymbol();
            ConstantValue constantValue = boundNameOfOperator.ConstantValueOpt;
            bool isImplicit = boundNameOfOperator.WasCompilerGenerated;
            return new NameOfOperation(argument, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IThrowOperation CreateBoundThrowExpressionOperation(BoundThrowExpression boundThrowExpression)
        {
            IOperation expression = Create(boundThrowExpression.Expression);
            SyntaxNode syntax = boundThrowExpression.Syntax;
            ITypeSymbol? type = boundThrowExpression.GetPublicTypeSymbol();
            bool isImplicit = boundThrowExpression.WasCompilerGenerated;
            return new ThrowOperation(expression, _semanticModel, syntax, type, isImplicit);
        }

        private IAddressOfOperation CreateBoundAddressOfOperatorOperation(BoundAddressOfOperator boundAddressOfOperator)
        {
            IOperation reference = Create(boundAddressOfOperator.Operand);
            SyntaxNode syntax = boundAddressOfOperator.Syntax;
            ITypeSymbol? type = boundAddressOfOperator.GetPublicTypeSymbol();
            bool isImplicit = boundAddressOfOperator.WasCompilerGenerated;
            return new AddressOfOperation(reference, _semanticModel, syntax, type, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundImplicitReceiverOperation(BoundImplicitReceiver boundImplicitReceiver)
        {
            InstanceReferenceKind referenceKind = InstanceReferenceKind.ImplicitReceiver;
            SyntaxNode syntax = boundImplicitReceiver.Syntax;
            ITypeSymbol? type = boundImplicitReceiver.GetPublicTypeSymbol();
            bool isImplicit = boundImplicitReceiver.WasCompilerGenerated;
            return new InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, isImplicit);
        }

        private IConditionalAccessOperation CreateBoundConditionalAccessOperation(BoundConditionalAccess boundConditionalAccess)
        {
            IOperation operation = Create(boundConditionalAccess.Receiver);
            IOperation whenNotNull = Create(boundConditionalAccess.AccessExpression);
            SyntaxNode syntax = boundConditionalAccess.Syntax;
            ITypeSymbol? type = boundConditionalAccess.GetPublicTypeSymbol();
            bool isImplicit = boundConditionalAccess.WasCompilerGenerated;

            return new ConditionalAccessOperation(operation, whenNotNull, _semanticModel, syntax, type, isImplicit);
        }

        private IConditionalAccessInstanceOperation CreateBoundConditionalReceiverOperation(BoundConditionalReceiver boundConditionalReceiver)
        {
            SyntaxNode syntax = boundConditionalReceiver.Syntax;
            ITypeSymbol? type = boundConditionalReceiver.GetPublicTypeSymbol();
            bool isImplicit = boundConditionalReceiver.WasCompilerGenerated;
            return new ConditionalAccessInstanceOperation(_semanticModel, syntax, type, isImplicit);
        }

        private IFieldInitializerOperation CreateBoundFieldEqualsValueOperation(BoundFieldEqualsValue boundFieldEqualsValue)
        {
            ImmutableArray<IFieldSymbol> initializedFields = ImmutableArray.Create<IFieldSymbol>(boundFieldEqualsValue.Field.GetPublicSymbol());
            IOperation value = Create(boundFieldEqualsValue.Value);
            SyntaxNode syntax = boundFieldEqualsValue.Syntax;
            bool isImplicit = boundFieldEqualsValue.WasCompilerGenerated;
            return new FieldInitializerOperation(initializedFields, boundFieldEqualsValue.Locals.GetPublicSymbols(), value, _semanticModel, syntax, isImplicit);
        }

        private IPropertyInitializerOperation CreateBoundPropertyEqualsValueOperation(BoundPropertyEqualsValue boundPropertyEqualsValue)
        {
            ImmutableArray<IPropertySymbol> initializedProperties = ImmutableArray.Create<IPropertySymbol>(boundPropertyEqualsValue.Property.GetPublicSymbol());
            IOperation value = Create(boundPropertyEqualsValue.Value);
            SyntaxNode syntax = boundPropertyEqualsValue.Syntax;
            bool isImplicit = boundPropertyEqualsValue.WasCompilerGenerated;
            return new PropertyInitializerOperation(initializedProperties, boundPropertyEqualsValue.Locals.GetPublicSymbols(), value, _semanticModel, syntax, isImplicit);
        }

        private IParameterInitializerOperation CreateBoundParameterEqualsValueOperation(BoundParameterEqualsValue boundParameterEqualsValue)
        {
            IParameterSymbol parameter = boundParameterEqualsValue.Parameter.GetPublicSymbol();
            IOperation value = Create(boundParameterEqualsValue.Value);
            SyntaxNode syntax = boundParameterEqualsValue.Syntax;
            bool isImplicit = boundParameterEqualsValue.WasCompilerGenerated;
            return new ParameterInitializerOperation(parameter, boundParameterEqualsValue.Locals.GetPublicSymbols(), value, _semanticModel, syntax, isImplicit);
        }

        private IBlockOperation CreateBoundBlockOperation(BoundBlock boundBlock)
        {
            ImmutableArray<IOperation> operations = CreateFromArray<BoundStatement, IOperation>(boundBlock.Statements);
            ImmutableArray<ILocalSymbol> locals = boundBlock.Locals.GetPublicSymbols();
            SyntaxNode syntax = boundBlock.Syntax;
            bool isImplicit = boundBlock.WasCompilerGenerated;
            return new BlockOperation(operations, locals, _semanticModel, syntax, isImplicit);
        }

        private IBranchOperation CreateBoundContinueStatementOperation(BoundContinueStatement boundContinueStatement)
        {
            ILabelSymbol target = boundContinueStatement.Label.GetPublicSymbol();
            BranchKind branchKind = BranchKind.Continue;
            SyntaxNode syntax = boundContinueStatement.Syntax;
            bool isImplicit = boundContinueStatement.WasCompilerGenerated;
            return new BranchOperation(target, branchKind, _semanticModel, syntax, isImplicit);
        }

        private IBranchOperation CreateBoundBreakStatementOperation(BoundBreakStatement boundBreakStatement)
        {
            ILabelSymbol target = boundBreakStatement.Label.GetPublicSymbol();
            BranchKind branchKind = BranchKind.Break;
            SyntaxNode syntax = boundBreakStatement.Syntax;
            bool isImplicit = boundBreakStatement.WasCompilerGenerated;
            return new BranchOperation(target, branchKind, _semanticModel, syntax, isImplicit);
        }

        private IReturnOperation CreateBoundYieldBreakStatementOperation(BoundYieldBreakStatement boundYieldBreakStatement)
        {
            IOperation? returnedValue = null;
            SyntaxNode syntax = boundYieldBreakStatement.Syntax;
            bool isImplicit = boundYieldBreakStatement.WasCompilerGenerated;
            return new ReturnOperation(returnedValue, OperationKind.YieldBreak, _semanticModel, syntax, isImplicit);
        }

        private IBranchOperation CreateBoundGotoStatementOperation(BoundGotoStatement boundGotoStatement)
        {
            ILabelSymbol target = boundGotoStatement.Label.GetPublicSymbol();
            BranchKind branchKind = BranchKind.GoTo;
            SyntaxNode syntax = boundGotoStatement.Syntax;
            bool isImplicit = boundGotoStatement.WasCompilerGenerated;
            return new BranchOperation(target, branchKind, _semanticModel, syntax, isImplicit);
        }

        private IEmptyOperation CreateBoundNoOpStatementOperation(BoundNoOpStatement boundNoOpStatement)
        {
            SyntaxNode syntax = boundNoOpStatement.Syntax;
            bool isImplicit = boundNoOpStatement.WasCompilerGenerated;
            return new EmptyOperation(_semanticModel, syntax, isImplicit);
        }

        private IConditionalOperation CreateBoundIfStatementOperation(BoundIfStatement boundIfStatement)
        {
            var stack = ArrayBuilder<BoundIfStatement>.GetInstance();

            IOperation? whenFalse;
            while (true)
            {
                stack.Push(boundIfStatement);

                var alternative = boundIfStatement.AlternativeOpt;

                if (alternative is BoundIfStatement elseIfStatement)
                {
                    boundIfStatement = elseIfStatement;
                }
                else
                {
                    whenFalse = Create(alternative);
                    break;
                }
            }

            ConditionalOperation result;
            do
            {
                boundIfStatement = stack.Pop();
                IOperation condition = Create(boundIfStatement.Condition);
                IOperation whenTrue = Create(boundIfStatement.Consequence);
                bool isRef = false;
                SyntaxNode syntax = boundIfStatement.Syntax;
                ITypeSymbol? type = null;
                ConstantValue? constantValue = null;
                bool isImplicit = boundIfStatement.WasCompilerGenerated;
                result = new ConditionalOperation(condition, whenTrue, whenFalse, isRef, _semanticModel, syntax, type, constantValue, isImplicit);
                whenFalse = result;
            }
            while (stack.Any());

            stack.Free();
            return result;
        }

        private IWhileLoopOperation CreateBoundWhileStatementOperation(BoundWhileStatement boundWhileStatement)
        {
            IOperation condition = Create(boundWhileStatement.Condition);
            IOperation body = Create(boundWhileStatement.Body);
            ImmutableArray<ILocalSymbol> locals = boundWhileStatement.Locals.GetPublicSymbols();
            ILabelSymbol continueLabel = boundWhileStatement.ContinueLabel.GetPublicSymbol();
            ILabelSymbol exitLabel = boundWhileStatement.BreakLabel.GetPublicSymbol();
            bool conditionIsTop = true;
            bool conditionIsUntil = false;
            SyntaxNode syntax = boundWhileStatement.Syntax;
            bool isImplicit = boundWhileStatement.WasCompilerGenerated;
            return new WhileLoopOperation(condition, conditionIsTop, conditionIsUntil, ignoredCondition: null, body, locals, continueLabel, exitLabel, _semanticModel, syntax, isImplicit);
        }

        private IWhileLoopOperation CreateBoundDoStatementOperation(BoundDoStatement boundDoStatement)
        {
            IOperation condition = Create(boundDoStatement.Condition);
            IOperation body = Create(boundDoStatement.Body);
            ILabelSymbol continueLabel = boundDoStatement.ContinueLabel.GetPublicSymbol();
            ILabelSymbol exitLabel = boundDoStatement.BreakLabel.GetPublicSymbol();
            bool conditionIsTop = false;
            bool conditionIsUntil = false;
            ImmutableArray<ILocalSymbol> locals = boundDoStatement.Locals.GetPublicSymbols();
            SyntaxNode syntax = boundDoStatement.Syntax;
            bool isImplicit = boundDoStatement.WasCompilerGenerated;
            return new WhileLoopOperation(condition, conditionIsTop, conditionIsUntil, ignoredCondition: null, body, locals, continueLabel, exitLabel, _semanticModel, syntax, isImplicit);
        }

        private IForLoopOperation CreateBoundForStatementOperation(BoundForStatement boundForStatement)
        {
            ImmutableArray<IOperation> before = CreateFromArray<BoundStatement, IOperation>(ToStatements(boundForStatement.Initializer));
            IOperation? condition = Create(boundForStatement.Condition);
            ImmutableArray<IOperation> atLoopBottom = CreateFromArray<BoundStatement, IOperation>(ToStatements(boundForStatement.Increment));
            IOperation body = Create(boundForStatement.Body);
            ImmutableArray<ILocalSymbol> locals = boundForStatement.OuterLocals.GetPublicSymbols();
            ImmutableArray<ILocalSymbol> conditionLocals = boundForStatement.InnerLocals.GetPublicSymbols();
            ILabelSymbol continueLabel = boundForStatement.ContinueLabel.GetPublicSymbol();
            ILabelSymbol exitLabel = boundForStatement.BreakLabel.GetPublicSymbol();
            SyntaxNode syntax = boundForStatement.Syntax;
            bool isImplicit = boundForStatement.WasCompilerGenerated;
            return new ForLoopOperation(before, conditionLocals, condition, atLoopBottom, body, locals, continueLabel, exitLabel, _semanticModel, syntax, isImplicit);
        }

        internal ForEachLoopOperationInfo? GetForEachLoopOperatorInfo(BoundForEachStatement boundForEachStatement)
        {
            ForEachEnumeratorInfo? enumeratorInfoOpt = boundForEachStatement.EnumeratorInfoOpt;
            ForEachLoopOperationInfo? info;

            if (enumeratorInfoOpt != null)
            {
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                var compilation = (CSharpCompilation)_semanticModel.Compilation;

                var iDisposable = enumeratorInfoOpt.IsAsync
                                    ? compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable)
                                    : compilation.GetSpecialType(SpecialType.System_IDisposable);

                info = new ForEachLoopOperationInfo(enumeratorInfoOpt.ElementType.GetPublicSymbol(),
                                                    enumeratorInfoOpt.GetEnumeratorInfo.Method.GetPublicSymbol(),
                                                    ((PropertySymbol)enumeratorInfoOpt.CurrentPropertyGetter.AssociatedSymbol).GetPublicSymbol(),
                                                    enumeratorInfoOpt.MoveNextInfo.Method.GetPublicSymbol(),
                                                    isAsynchronous: enumeratorInfoOpt.IsAsync,
                                                    inlineArrayConversion: enumeratorInfoOpt.InlineArraySpanType is WellKnownType.Unknown ? null : Conversion.InlineArray,
                                                    collectionIsInlineArrayValue: enumeratorInfoOpt.InlineArrayUsedAsValue,
                                                    needsDispose: enumeratorInfoOpt.NeedsDisposal,
                                                    knownToImplementIDisposable: enumeratorInfoOpt.NeedsDisposal ?
                                                                                     compilation.Conversions.
                                                                                         HasImplicitConversionToOrImplementsVarianceCompatibleInterface(enumeratorInfoOpt.GetEnumeratorInfo.Method.ReturnType,
                                                                                                                            iDisposable,
                                                                                                                            ref discardedUseSiteInfo,
                                                                                                                            needSupportForRefStructInterfaces: out _) :
                                                                                     false,
                                                    enumeratorInfoOpt.PatternDisposeInfo?.Method.GetPublicSymbol(),
                                                    BoundNode.GetConversion(enumeratorInfoOpt.CurrentConversion, enumeratorInfoOpt.CurrentPlaceholder),
                                                    BoundNode.GetConversion(boundForEachStatement.ElementConversion, boundForEachStatement.ElementPlaceholder),
                                                    getEnumeratorArguments: createArgumentOperations(enumeratorInfoOpt.GetEnumeratorInfo),
                                                    moveNextArguments: createArgumentOperations(enumeratorInfoOpt.MoveNextInfo),
                                                    disposeArguments: enumeratorInfoOpt.PatternDisposeInfo is object
                                                        ? CreateDisposeArguments(enumeratorInfoOpt.PatternDisposeInfo)
                                                        : default);
            }
            else
            {
                info = null;
            }

            return info;

            ImmutableArray<IArgumentOperation> createArgumentOperations(MethodArgumentInfo? info)
            {
                if (info == null)
                {
                    return default;
                }
                if (info.Arguments.Length == 0)
                {
                    return ImmutableArray<IArgumentOperation>.Empty;
                }
                var args = DeriveArguments(
                    info.Method,
                    info.Arguments,
                    argumentsToParametersOpt: default,
                    info.DefaultArguments,
                    invokedAsExtensionMethod: info.Method.IsExtensionMethod
                );
                return Operation.SetParentOperation(args, null);
            }
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
                return new VariableDeclaratorOperation(local.GetPublicSymbol(), initializer: null, ignoredArguments: ImmutableArray<IOperation>.Empty, semanticModel: _semanticModel, syntax: declaratorSyntax, isImplicit: false);
            }
        }

        private IForEachLoopOperation CreateBoundForEachStatementOperation(BoundForEachStatement boundForEachStatement)
        {
            IOperation loopControlVariable = CreateBoundForEachStatementLoopControlVariable(boundForEachStatement);
            // Strip identity conversion added by compiler on top of inline array. Conversion produces an rvalue, but we need the IOperation tree to preserve lvalue-ness of the original collection. 
            IOperation collection = Create(boundForEachStatement.EnumeratorInfoOpt?.InlineArraySpanType is null or WellKnownType.Unknown ||
                                           boundForEachStatement.Expression is not BoundConversion { Conversion.IsIdentity: true, ExplicitCastInCode: false, Operand: BoundExpression operand } ?
                                               boundForEachStatement.Expression :
                                               operand);
            var nextVariables = ImmutableArray<IOperation>.Empty;
            IOperation body = Create(boundForEachStatement.Body);
            ForEachLoopOperationInfo? info = GetForEachLoopOperatorInfo(boundForEachStatement);

            ImmutableArray<ILocalSymbol> locals = boundForEachStatement.IterationVariables.GetPublicSymbols();

            ILabelSymbol continueLabel = boundForEachStatement.ContinueLabel.GetPublicSymbol();
            ILabelSymbol exitLabel = boundForEachStatement.BreakLabel.GetPublicSymbol();
            SyntaxNode syntax = boundForEachStatement.Syntax;
            bool isImplicit = boundForEachStatement.WasCompilerGenerated;
            bool isAsynchronous = boundForEachStatement.EnumeratorInfoOpt is { MoveNextAwaitableInfo: not null };
            return new ForEachLoopOperation(loopControlVariable, collection, nextVariables, info, isAsynchronous, body, locals, continueLabel, exitLabel, _semanticModel, syntax, isImplicit);
        }

        private ITryOperation CreateBoundTryStatementOperation(BoundTryStatement boundTryStatement)
        {
            var body = (IBlockOperation)Create(boundTryStatement.TryBlock);
            ImmutableArray<ICatchClauseOperation> catches = CreateFromArray<BoundCatchBlock, ICatchClauseOperation>(boundTryStatement.CatchBlocks);
            var @finally = (IBlockOperation?)Create(boundTryStatement.FinallyBlockOpt);
            SyntaxNode syntax = boundTryStatement.Syntax;
            bool isImplicit = boundTryStatement.WasCompilerGenerated;
            return new TryOperation(body, catches, @finally, exitLabel: null, _semanticModel, syntax, isImplicit);
        }

        private ICatchClauseOperation CreateBoundCatchBlockOperation(BoundCatchBlock boundCatchBlock)
        {
            IOperation? exceptionDeclarationOrExpression = CreateVariableDeclarator((BoundLocal?)boundCatchBlock.ExceptionSourceOpt);
            // The exception filter prologue is introduced during lowering, so should be null here.
            Debug.Assert(boundCatchBlock.ExceptionFilterPrologueOpt is null);
            IOperation? filter = Create(boundCatchBlock.ExceptionFilterOpt);
            IBlockOperation handler = (IBlockOperation)Create(boundCatchBlock.Body);
            ITypeSymbol exceptionType = boundCatchBlock.ExceptionTypeOpt.GetPublicSymbol() ?? _semanticModel.Compilation.ObjectType;
            ImmutableArray<ILocalSymbol> locals = boundCatchBlock.Locals.GetPublicSymbols();
            SyntaxNode syntax = boundCatchBlock.Syntax;
            bool isImplicit = boundCatchBlock.WasCompilerGenerated;
            return new CatchClauseOperation(exceptionDeclarationOrExpression, exceptionType, locals, filter, handler, _semanticModel, syntax, isImplicit);
        }

        private IFixedOperation CreateBoundFixedStatementOperation(BoundFixedStatement boundFixedStatement)
        {
            IVariableDeclarationGroupOperation variables = (IVariableDeclarationGroupOperation)Create(boundFixedStatement.Declarations);
            IOperation body = Create(boundFixedStatement.Body);
            ImmutableArray<ILocalSymbol> locals = boundFixedStatement.Locals.GetPublicSymbols();
            SyntaxNode syntax = boundFixedStatement.Syntax;
            bool isImplicit = boundFixedStatement.WasCompilerGenerated;
            return new FixedOperation(locals, variables, body, _semanticModel, syntax, isImplicit);
        }

        private IUsingOperation CreateBoundUsingStatementOperation(BoundUsingStatement boundUsingStatement)
        {
            Debug.Assert((boundUsingStatement.DeclarationsOpt == null) != (boundUsingStatement.ExpressionOpt == null));
            Debug.Assert(boundUsingStatement.ExpressionOpt is object || boundUsingStatement.Locals.Length > 0);
            IOperation resources = Create(boundUsingStatement.DeclarationsOpt ?? (BoundNode)boundUsingStatement.ExpressionOpt!);
            IOperation body = Create(boundUsingStatement.Body);
            ImmutableArray<ILocalSymbol> locals = boundUsingStatement.Locals.GetPublicSymbols();
            bool isAsynchronous = boundUsingStatement.AwaitOpt != null;
            DisposeOperationInfo disposeOperationInfo = boundUsingStatement.PatternDisposeInfoOpt is object
                                                         ? new DisposeOperationInfo(
                                                                 disposeMethod: boundUsingStatement.PatternDisposeInfoOpt.Method.GetPublicSymbol(),
                                                                 disposeArguments: CreateDisposeArguments(boundUsingStatement.PatternDisposeInfoOpt))
                                                         : default;
            SyntaxNode syntax = boundUsingStatement.Syntax;
            bool isImplicit = boundUsingStatement.WasCompilerGenerated;
            return new UsingOperation(resources, body, locals, isAsynchronous, disposeOperationInfo, _semanticModel, syntax, isImplicit);
        }

        private IThrowOperation CreateBoundThrowStatementOperation(BoundThrowStatement boundThrowStatement)
        {
            IOperation? thrownObject = Create(boundThrowStatement.ExpressionOpt);
            SyntaxNode syntax = boundThrowStatement.Syntax;
            ITypeSymbol? statementType = null;
            bool isImplicit = boundThrowStatement.WasCompilerGenerated;
            return new ThrowOperation(thrownObject, _semanticModel, syntax, statementType, isImplicit);
        }

        private IReturnOperation CreateBoundReturnStatementOperation(BoundReturnStatement boundReturnStatement)
        {
            IOperation? returnedValue = Create(boundReturnStatement.ExpressionOpt);
            SyntaxNode syntax = boundReturnStatement.Syntax;
            bool isImplicit = boundReturnStatement.WasCompilerGenerated;
            return new ReturnOperation(returnedValue, OperationKind.Return, _semanticModel, syntax, isImplicit);
        }

        private IReturnOperation CreateBoundYieldReturnStatementOperation(BoundYieldReturnStatement boundYieldReturnStatement)
        {
            IOperation returnedValue = Create(boundYieldReturnStatement.Expression);
            SyntaxNode syntax = boundYieldReturnStatement.Syntax;
            bool isImplicit = boundYieldReturnStatement.WasCompilerGenerated;
            return new ReturnOperation(returnedValue, OperationKind.YieldReturn, _semanticModel, syntax, isImplicit);
        }

        private ILockOperation CreateBoundLockStatementOperation(BoundLockStatement boundLockStatement)
        {
            // If there is no Enter2 method, then there will be no lock taken reference
            bool legacyMode = _semanticModel.Compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter2) == null;
            ILocalSymbol? lockTakenSymbol =
                legacyMode ? null : new SynthesizedLocal((_semanticModel.GetEnclosingSymbol(boundLockStatement.Syntax.SpanStart) as IMethodSymbol).GetSymbol(),
                                                         TypeWithAnnotations.Create(((CSharpCompilation)_semanticModel.Compilation).GetSpecialType(SpecialType.System_Boolean)),
                                                         SynthesizedLocalKind.LockTaken,
                                                         syntaxOpt: boundLockStatement.Argument.Syntax).GetPublicSymbol();
            IOperation lockedValue = Create(boundLockStatement.Argument);
            IOperation body = Create(boundLockStatement.Body);
            SyntaxNode syntax = boundLockStatement.Syntax;
            bool isImplicit = boundLockStatement.WasCompilerGenerated;

            return new LockOperation(lockedValue, body, lockTakenSymbol, _semanticModel, syntax, isImplicit);
        }

        private IInvalidOperation CreateBoundBadStatementOperation(BoundBadStatement boundBadStatement)
        {
            SyntaxNode syntax = boundBadStatement.Syntax;

            // if child has syntax node point to same syntax node as bad statement, then this invalid statement is implicit
            bool isImplicit = boundBadStatement.WasCompilerGenerated || boundBadStatement.ChildBoundNodes.Any(static (e, boundBadStatement) => e?.Syntax == boundBadStatement.Syntax, boundBadStatement);
            var children = CreateFromArray<BoundNode, IOperation>(boundBadStatement.ChildBoundNodes);
            return new InvalidOperation(children, _semanticModel, syntax, type: null, constantValue: null, isImplicit);
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
                        Debug.Assert(node.Parent != null);
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
            ImmutableArray<IVariableDeclaratorOperation> declarators = CreateVariableDeclarator(boundLocalDeclaration, varDeclaration);
            ImmutableArray<IOperation> ignoredDimensions = CreateIgnoredDimensions(boundLocalDeclaration);
            IVariableDeclarationOperation multiVariableDeclaration = new VariableDeclarationOperation(declarators, initializer: null, ignoredDimensions, _semanticModel, varDeclaration, multiVariableImplicit);
            // In the case of a for loop, varStatement and varDeclaration will be the same syntax node.
            // We can only have one explicit operation, so make sure this node is implicit in that scenario.
            bool isImplicit = (varStatement == varDeclaration) || boundLocalDeclaration.WasCompilerGenerated;
            return new VariableDeclarationGroupOperation(ImmutableArray.Create(multiVariableDeclaration), _semanticModel, varStatement, isImplicit);
        }

        private IOperation CreateBoundMultipleLocalDeclarationsBaseOperation(BoundMultipleLocalDeclarationsBase boundMultipleLocalDeclarations)
        {
            // The syntax for the boundMultipleLocalDeclarations can either be a LocalDeclarationStatement or a VariableDeclaration, depending on the context
            // (using/fixed statements vs variable declaration)
            // We generate a DeclarationGroup for these scenarios (using/fixed) to maintain tree shape consistency across IOperation.
            SyntaxNode declarationGroupSyntax = boundMultipleLocalDeclarations.Syntax;
            SyntaxNode declarationSyntax = declarationGroupSyntax.IsKind(SyntaxKind.LocalDeclarationStatement) ?
                    ((LocalDeclarationStatementSyntax)declarationGroupSyntax).Declaration :
                    declarationGroupSyntax;

            bool declarationIsImplicit = boundMultipleLocalDeclarations.WasCompilerGenerated;
            ImmutableArray<IVariableDeclaratorOperation> declarators = CreateVariableDeclarator(boundMultipleLocalDeclarations, declarationSyntax);
            ImmutableArray<IOperation> ignoredDimensions = CreateIgnoredDimensions(boundMultipleLocalDeclarations);
            IVariableDeclarationOperation multiVariableDeclaration = new VariableDeclarationOperation(declarators, initializer: null, ignoredDimensions, _semanticModel, declarationSyntax, declarationIsImplicit);

            // If the syntax was the same, we're in a fixed statement or using statement. We make the Group operation implicit in this scenario, as the
            // syntax itself is a VariableDeclaration. We do this for using declarations as well, but since that doesn't have a separate parent bound
            // node, we need to check the current node for that explicitly.
            bool isImplicit = declarationGroupSyntax == declarationSyntax || boundMultipleLocalDeclarations.WasCompilerGenerated || boundMultipleLocalDeclarations is BoundUsingLocalDeclarations;
            var variableDeclaration = new VariableDeclarationGroupOperation(ImmutableArray.Create(multiVariableDeclaration), _semanticModel, declarationGroupSyntax, isImplicit);

            if (boundMultipleLocalDeclarations is BoundUsingLocalDeclarations usingDecl)
            {
                return new UsingDeclarationOperation(
                    variableDeclaration,
                    isAsynchronous: usingDecl.AwaitOpt is object,
                    disposeInfo: usingDecl.PatternDisposeInfoOpt is object
                                   ? new DisposeOperationInfo(
                                           disposeMethod: usingDecl.PatternDisposeInfoOpt.Method.GetPublicSymbol(),
                                           disposeArguments: CreateDisposeArguments(usingDecl.PatternDisposeInfoOpt))
                                   : default,
                     _semanticModel,
                    declarationGroupSyntax,
                    isImplicit: boundMultipleLocalDeclarations.WasCompilerGenerated);
            }

            return variableDeclaration;
        }

        private ILabeledOperation CreateBoundLabelStatementOperation(BoundLabelStatement boundLabelStatement)
        {
            ILabelSymbol label = boundLabelStatement.Label.GetPublicSymbol();
            SyntaxNode syntax = boundLabelStatement.Syntax;
            bool isImplicit = boundLabelStatement.WasCompilerGenerated;
            return new LabeledOperation(label, operation: null, _semanticModel, syntax, isImplicit);
        }

        private ILabeledOperation CreateBoundLabeledStatementOperation(BoundLabeledStatement boundLabeledStatement)
        {
            ILabelSymbol label = boundLabeledStatement.Label.GetPublicSymbol();
            IOperation labeledStatement = Create(boundLabeledStatement.Body);
            SyntaxNode syntax = boundLabeledStatement.Syntax;
            bool isImplicit = boundLabeledStatement.WasCompilerGenerated;
            return new LabeledOperation(label, labeledStatement, _semanticModel, syntax, isImplicit);
        }

        private IExpressionStatementOperation CreateBoundExpressionStatementOperation(BoundExpressionStatement boundExpressionStatement)
        {
            // lambda body can point to expression directly and binder can insert expression statement there. and end up statement pointing to
            // expression syntax node since there is no statement syntax node to point to. this will mark such one as implicit since it doesn't
            // actually exist in code
            bool isImplicit = boundExpressionStatement.WasCompilerGenerated || boundExpressionStatement.Syntax == boundExpressionStatement.Expression.Syntax;
            SyntaxNode syntax = boundExpressionStatement.Syntax;

            // If we're creating the tree for a speculatively-bound constructor initializer, there can be a bound sequence as the child node here
            // that corresponds to the lifetime of any declared variables.
            IOperation expression = Create(boundExpressionStatement.Expression);
            if (boundExpressionStatement.Expression is BoundSequence sequence)
            {
                Debug.Assert(boundExpressionStatement.Syntax == sequence.Value.Syntax);
                isImplicit = true;
            }

            return new ExpressionStatementOperation(expression, _semanticModel, syntax, isImplicit);
        }

        internal IOperation CreateBoundTupleOperation(BoundTupleExpression boundTupleExpression, bool createDeclaration = true)
        {
            SyntaxNode syntax = boundTupleExpression.Syntax;
            bool isImplicit = boundTupleExpression.WasCompilerGenerated;
            ITypeSymbol? type = boundTupleExpression.GetPublicTypeSymbol();

            if (syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                syntax = declarationExpressionSyntax.Designation;
                if (createDeclaration)
                {
                    var tupleOperation = CreateBoundTupleOperation(boundTupleExpression, createDeclaration: false);
                    return new DeclarationExpressionOperation(tupleOperation, _semanticModel, declarationExpressionSyntax, type, isImplicit: false);
                }
            }

            TypeSymbol? naturalType = boundTupleExpression switch
            {
                BoundTupleLiteral { Type: var t } => t,
                BoundConvertedTupleLiteral { SourceTuple: { Type: var t } } => t,
                BoundConvertedTupleLiteral => null,
                { Kind: var kind } => throw ExceptionUtilities.UnexpectedValue(kind)
            };

            ImmutableArray<IOperation> elements = CreateFromArray<BoundExpression, IOperation>(boundTupleExpression.Arguments);
            return new TupleOperation(elements, naturalType.GetPublicSymbol(), _semanticModel, syntax, type, isImplicit);
        }

        private IInterpolatedStringOperation CreateBoundInterpolatedStringExpressionOperation(BoundInterpolatedString boundInterpolatedString, ImmutableArray<(bool IsLiteral, bool HasAlignment, bool HasFormat)>? positionInfo = null)
        {
            Debug.Assert(positionInfo == null || boundInterpolatedString.InterpolationData is null or { BuilderType: null });

            if (positionInfo is null && boundInterpolatedString.InterpolationData is { BuilderType: not null, PositionInfo: var info })
            {
                positionInfo = info[0];
            }

            ImmutableArray<IInterpolatedStringContentOperation> parts = CreateBoundInterpolatedStringContentOperation(boundInterpolatedString.Parts, positionInfo);
            SyntaxNode syntax = boundInterpolatedString.Syntax;
            ITypeSymbol? type = boundInterpolatedString.GetPublicTypeSymbol();
            ConstantValue? constantValue = boundInterpolatedString.ConstantValueOpt;
            bool isImplicit = boundInterpolatedString.WasCompilerGenerated;
            return new InterpolatedStringOperation(parts, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        internal ImmutableArray<IInterpolatedStringContentOperation> CreateBoundInterpolatedStringContentOperation(ImmutableArray<BoundExpression> parts, ImmutableArray<(bool IsLiteral, bool HasAlignment, bool HasFormat)>? positionInfo)
        {
            return positionInfo is { } info ? createHandlerInterpolatedStringContent(info) : createNonHandlerInterpolatedStringContent();

            ImmutableArray<IInterpolatedStringContentOperation> createNonHandlerInterpolatedStringContent()
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

            ImmutableArray<IInterpolatedStringContentOperation> createHandlerInterpolatedStringContent(ImmutableArray<(bool IsLiteral, bool HasAlignment, bool HasFormat)> positionInfo)
            {
                // For interpolated string handlers, we want to deconstruct the `AppendLiteral`/`AppendFormatted` calls into
                // their relevant components.
                // https://github.com/dotnet/roslyn/issues/54505 we need to handle interpolated strings used as handler conversions.

                Debug.Assert(parts.Length == positionInfo.Length);
                var builder = ArrayBuilder<IInterpolatedStringContentOperation>.GetInstance(parts.Length);

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    var currentPosition = positionInfo[i];

                    BoundExpression value;
                    BoundExpression? alignment;
                    BoundExpression? format;

                    switch (part)
                    {
                        case BoundCall call:
                            (value, alignment, format) = getCallInfo(call.Arguments, call.ArgumentNamesOpt, currentPosition);
                            break;

                        case BoundDynamicInvocation dynamicInvocation:
                            (value, alignment, format) = getCallInfo(dynamicInvocation.Arguments, dynamicInvocation.ArgumentNamesOpt, currentPosition);
                            break;

                        case BoundBadExpression bad:
                            Debug.Assert(bad.ChildBoundNodes.Length ==
                                2 + // initial value + receiver is added to the end
                                (currentPosition.HasAlignment ? 1 : 0) +
                                (currentPosition.HasFormat ? 1 : 0));

                            value = bad.ChildBoundNodes[0];
                            if (currentPosition.IsLiteral)
                            {
                                alignment = format = null;
                            }
                            else
                            {
                                alignment = currentPosition.HasAlignment ? bad.ChildBoundNodes[1] : null;
                                format = currentPosition.HasFormat ? bad.ChildBoundNodes[^2] : null;
                            }
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(part.Kind);
                    }

                    // We are intentionally not checking the part for implicitness here. The part is a generated AppendLiteral or AppendFormatted call,
                    // and will always be marked as CompilerGenerated. However, our existing behavior for non-builder interpolated strings does not mark
                    // the BoundLiteral or BoundStringInsert components as compiler generated. This generates a non-implicit IInterpolatedStringTextOperation
                    // with an implicit literal underneath, and a non-implicit IInterpolationOperation with non-implicit underlying components.
                    bool isImplicit = false;
                    if (currentPosition.IsLiteral)
                    {
                        Debug.Assert(alignment is null);
                        Debug.Assert(format is null);
                        IOperation valueOperation = value switch
                        {
                            BoundLiteral l => CreateBoundLiteralOperation(l, @implicit: true),
                            BoundConversion { Operand: BoundLiteral } c => CreateBoundConversionOperation(c, forceOperandImplicitLiteral: true),
                            _ => throw ExceptionUtilities.UnexpectedValue(value.Kind),
                        };

                        Debug.Assert(valueOperation.IsImplicit);

                        builder.Add(new InterpolatedStringTextOperation(valueOperation, _semanticModel, part.Syntax, isImplicit));
                    }
                    else
                    {
                        IOperation valueOperation = Create(value);
                        IOperation? alignmentOperation = Create(alignment);
                        IOperation? formatOperation = Create(format);

                        Debug.Assert(valueOperation.Syntax != part.Syntax);

                        builder.Add(new InterpolationOperation(valueOperation, alignmentOperation, formatOperation, _semanticModel, part.Syntax, isImplicit));
                    }
                }

                return builder.ToImmutableAndFree();

                static (BoundExpression Value, BoundExpression? Alignment, BoundExpression? Format) getCallInfo(ImmutableArray<BoundExpression> arguments, ImmutableArray<string?> argumentNamesOpt, (bool IsLiteral, bool HasAlignment, bool HasFormat) currentPosition)
                {
                    BoundExpression value = arguments[0];

                    if (currentPosition.IsLiteral || argumentNamesOpt.IsDefault)
                    {
                        // There was no alignment/format component, as binding will qualify those parameters by name
                        return (value, null, null);
                    }
                    else
                    {
                        var alignmentIndex = argumentNamesOpt.IndexOf("alignment");
                        BoundExpression? alignment = alignmentIndex == -1 ? null : arguments[alignmentIndex];
                        var formatIndex = argumentNamesOpt.IndexOf("format");
                        BoundExpression? format = formatIndex == -1 ? null : arguments[formatIndex];

                        return (value, alignment, format);
                    }
                }
            }
        }

        private IInterpolationOperation CreateBoundInterpolationOperation(BoundStringInsert boundStringInsert)
        {
            IOperation expression = Create(boundStringInsert.Value);
            IOperation? alignment = Create(boundStringInsert.Alignment);
            IOperation? formatString = Create(boundStringInsert.Format);
            SyntaxNode syntax = boundStringInsert.Syntax;
            bool isImplicit = boundStringInsert.WasCompilerGenerated;
            return new InterpolationOperation(expression, alignment, formatString, _semanticModel, syntax, isImplicit);
        }

        private IInterpolatedStringTextOperation CreateBoundInterpolatedStringTextOperation(BoundLiteral boundNode)
        {
            IOperation text = CreateBoundLiteralOperation(boundNode, @implicit: true);
            SyntaxNode syntax = boundNode.Syntax;
            bool isImplicit = boundNode.WasCompilerGenerated;
            return new InterpolatedStringTextOperation(text, _semanticModel, syntax, isImplicit);
        }

        private IInterpolatedStringHandlerCreationOperation CreateInterpolatedStringHandler(BoundConversion conversion)
        {
            Debug.Assert(conversion.Conversion.IsInterpolatedStringHandler);

            InterpolatedStringHandlerData interpolationData = conversion.Operand.GetInterpolatedStringHandlerData();
            var construction = Create(interpolationData.Construction);
            var content = createContent(conversion.Operand);
            var isImplicit = conversion.WasCompilerGenerated || !conversion.ExplicitCastInCode;
            return new InterpolatedStringHandlerCreationOperation(
                construction,
                interpolationData.HasTrailingHandlerValidityParameter,
                interpolationData.UsesBoolReturns,
                content,
                _semanticModel,
                conversion.Syntax,
                conversion.GetPublicTypeSymbol(),
                isImplicit);

            IOperation createContent(BoundExpression current)
            {
                switch (current)
                {
                    case BoundBinaryOperator binaryOperator:
                        var left = createContent(binaryOperator.Left);
                        var right = createContent(binaryOperator.Right);
                        return new InterpolatedStringAdditionOperation(left, right, _semanticModel, current.Syntax, current.WasCompilerGenerated);

                    case BoundInterpolatedString interpolatedString:
                        var parts = interpolatedString.Parts.SelectAsArray(
                            static IInterpolatedStringContentOperation (part, @this) =>
                            {
                                var methodName = part switch
                                {
                                    BoundCall { Method.Name: var name } => name,
                                    BoundDynamicInvocation { Expression: BoundMethodGroup { Name: var name } } => name,
                                    { HasErrors: true } => "",
                                    _ => throw ExceptionUtilities.UnexpectedValue(part.Kind)
                                };

                                var operationKind = methodName switch
                                {
                                    "" => OperationKind.InterpolatedStringAppendInvalid,
                                    BoundInterpolatedString.AppendLiteralMethod => OperationKind.InterpolatedStringAppendLiteral,
                                    BoundInterpolatedString.AppendFormattedMethod => OperationKind.InterpolatedStringAppendFormatted,
                                    _ => throw ExceptionUtilities.UnexpectedValue(methodName)
                                };

                                return new InterpolatedStringAppendOperation(@this.Create(part), operationKind, @this._semanticModel, part.Syntax, isImplicit: true);
                            }, this);

                        return new InterpolatedStringOperation(
                            parts,
                            _semanticModel,
                            interpolatedString.Syntax,
                            interpolatedString.GetPublicTypeSymbol(),
                            interpolatedString.ConstantValueOpt,
                            isImplicit: interpolatedString.WasCompilerGenerated);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(current.Kind);
                }
            }
        }

        private IOperation CreateBoundInterpolatedStringArgumentPlaceholder(BoundInterpolatedStringArgumentPlaceholder placeholder)
        {
            SyntaxNode syntax = placeholder.Syntax;
            bool isImplicit = true;
            ITypeSymbol? type = placeholder.GetPublicTypeSymbol();

            if (placeholder.ArgumentIndex == BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter)
            {
                return new InvalidOperation(ImmutableArray<IOperation>.Empty, _semanticModel, syntax, type, placeholder.ConstantValueOpt, isImplicit);
            }

            const int NonArgumentIndex = -1;

            var (placeholderKind, argumentIndex) = placeholder.ArgumentIndex switch
            {
                >= 0 and var index => (InterpolatedStringArgumentPlaceholderKind.CallsiteArgument, index),
                BoundInterpolatedStringArgumentPlaceholder.InstanceParameter or BoundInterpolatedStringArgumentPlaceholder.ExtensionReceiver => (InterpolatedStringArgumentPlaceholderKind.CallsiteReceiver, NonArgumentIndex),
                BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter => (InterpolatedStringArgumentPlaceholderKind.TrailingValidityArgument, NonArgumentIndex),
                _ => throw ExceptionUtilities.UnexpectedValue(placeholder.ArgumentIndex)
            };

            return new InterpolatedStringHandlerArgumentPlaceholderOperation(argumentIndex, placeholderKind, _semanticModel, syntax, isImplicit);
        }

        private IOperation CreateBoundInterpolatedStringHandlerPlaceholder(BoundInterpolatedStringHandlerPlaceholder placeholder)
        {
            return new InstanceReferenceOperation(
                InstanceReferenceKind.InterpolatedStringHandler,
                _semanticModel,
                placeholder.Syntax,
                placeholder.GetPublicTypeSymbol(),
                isImplicit: placeholder.WasCompilerGenerated);
        }

        private IConstantPatternOperation CreateBoundConstantPatternOperation(BoundConstantPattern boundConstantPattern)
        {
            IOperation value = Create(boundConstantPattern.Value);
            SyntaxNode syntax = boundConstantPattern.Syntax;
            bool isImplicit = boundConstantPattern.WasCompilerGenerated;
            TypeSymbol inputType = boundConstantPattern.InputType;
            TypeSymbol narrowedType = boundConstantPattern.NarrowedType;
            return new ConstantPatternOperation(value, inputType.GetPublicSymbol(), narrowedType.GetPublicSymbol(), _semanticModel, syntax, isImplicit);
        }

        private IOperation CreateBoundRelationalPatternOperation(BoundRelationalPattern boundRelationalPattern)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundRelationalPattern.Relation);
            IOperation value = Create(boundRelationalPattern.Value);
            SyntaxNode syntax = boundRelationalPattern.Syntax;
            bool isImplicit = boundRelationalPattern.WasCompilerGenerated;
            TypeSymbol inputType = boundRelationalPattern.InputType;
            TypeSymbol narrowedType = boundRelationalPattern.NarrowedType;
            return new RelationalPatternOperation(operatorKind, value, inputType.GetPublicSymbol(), narrowedType.GetPublicSymbol(), _semanticModel, syntax, isImplicit);
        }

        private IDeclarationPatternOperation CreateBoundDeclarationPatternOperation(BoundDeclarationPattern boundDeclarationPattern)
        {
            ISymbol? variable = boundDeclarationPattern.Variable.GetPublicSymbol();
            if (variable == null && boundDeclarationPattern.VariableAccess?.Kind == BoundKind.DiscardExpression)
            {
                variable = ((BoundDiscardExpression)boundDeclarationPattern.VariableAccess).ExpressionSymbol.GetPublicSymbol();
            }

            ITypeSymbol inputType = boundDeclarationPattern.InputType.GetPublicSymbol();
            ITypeSymbol narrowedType = boundDeclarationPattern.NarrowedType.GetPublicSymbol();
            bool acceptsNull = boundDeclarationPattern.IsVar;
            ITypeSymbol? matchedType = acceptsNull ? null : boundDeclarationPattern.DeclaredType.GetPublicTypeSymbol();
            SyntaxNode syntax = boundDeclarationPattern.Syntax;
            bool isImplicit = boundDeclarationPattern.WasCompilerGenerated;
            return new DeclarationPatternOperation(matchedType, acceptsNull, variable, inputType, narrowedType, _semanticModel, syntax, isImplicit);
        }

        private IRecursivePatternOperation CreateBoundRecursivePatternOperation(BoundRecursivePattern boundRecursivePattern)
        {
            ITypeSymbol matchedType = (boundRecursivePattern.DeclaredType?.Type ?? boundRecursivePattern.InputType.StrippedType()).GetPublicSymbol();
            ImmutableArray<IPatternOperation> deconstructionSubpatterns = boundRecursivePattern.Deconstruction is { IsDefault: false } deconstructions
                ? deconstructions.SelectAsArray((p, fac) => (IPatternOperation)fac.Create(p.Pattern), this)
                : ImmutableArray<IPatternOperation>.Empty;
            ImmutableArray<IPropertySubpatternOperation> propertySubpatterns = boundRecursivePattern.Properties is { IsDefault: false } properties
                ? properties.SelectAsArray((p, arg) => arg.Fac.CreatePropertySubpattern(p, arg.MatchedType), (Fac: this, MatchedType: matchedType))
                : ImmutableArray<IPropertySubpatternOperation>.Empty;
            return new RecursivePatternOperation(
                matchedType,
                boundRecursivePattern.DeconstructMethod.GetPublicSymbol(),
                deconstructionSubpatterns,
                propertySubpatterns,
                boundRecursivePattern.Variable.GetPublicSymbol(),
                boundRecursivePattern.InputType.GetPublicSymbol(),
                boundRecursivePattern.NarrowedType.GetPublicSymbol(),
                _semanticModel,
                boundRecursivePattern.Syntax,
                isImplicit: boundRecursivePattern.WasCompilerGenerated);
        }

        private IRecursivePatternOperation CreateBoundRecursivePatternOperation(BoundITuplePattern boundITuplePattern)
        {
            ImmutableArray<IPatternOperation> deconstructionSubpatterns = boundITuplePattern.Subpatterns is { IsDefault: false } subpatterns
                ? subpatterns.SelectAsArray((p, fac) => (IPatternOperation)fac.Create(p.Pattern), this)
                : ImmutableArray<IPatternOperation>.Empty;

            return new RecursivePatternOperation(
                boundITuplePattern.InputType.StrippedType().GetPublicSymbol(),
                boundITuplePattern.GetLengthMethod.ContainingType.GetPublicSymbol(),
                deconstructionSubpatterns,
                propertySubpatterns: ImmutableArray<IPropertySubpatternOperation>.Empty,
                declaredSymbol: null,
                boundITuplePattern.InputType.GetPublicSymbol(),
                boundITuplePattern.NarrowedType.GetPublicSymbol(),
                _semanticModel,
                boundITuplePattern.Syntax,
                isImplicit: boundITuplePattern.WasCompilerGenerated);
        }

        private IOperation CreateBoundTypePatternOperation(BoundTypePattern boundTypePattern)
        {
            return new TypePatternOperation(
                matchedType: boundTypePattern.NarrowedType.GetPublicSymbol(),
                inputType: boundTypePattern.InputType.GetPublicSymbol(),
                narrowedType: boundTypePattern.NarrowedType.GetPublicSymbol(),
                semanticModel: _semanticModel,
                syntax: boundTypePattern.Syntax,
                isImplicit: boundTypePattern.WasCompilerGenerated);
        }

        private IOperation CreateBoundSlicePatternOperation(BoundSlicePattern boundNode)
        {
            return new SlicePatternOperation(
                sliceSymbol: boundNode.Pattern is null ? null :
                    Binder.GetIndexerOrImplicitIndexerSymbol(boundNode.IndexerAccess).GetPublicSymbol(),
                pattern: (IPatternOperation?)Create(boundNode.Pattern),
                inputType: boundNode.InputType.GetPublicSymbol(),
                narrowedType: boundNode.NarrowedType.GetPublicSymbol(),
                _semanticModel,
                boundNode.Syntax,
                isImplicit: boundNode.WasCompilerGenerated);
        }

        private IOperation CreateBoundListPatternOperation(BoundListPattern boundNode)
        {
            return new ListPatternOperation(
                lengthSymbol: Binder.GetPropertySymbol(boundNode.LengthAccess, out _, out _).GetPublicSymbol(),
                indexerSymbol: Binder.GetIndexerOrImplicitIndexerSymbol(boundNode.IndexerAccess).GetPublicSymbol(),
                patterns: boundNode.Subpatterns.SelectAsArray((p, fac) => (IPatternOperation)fac.Create(p), this),
                declaredSymbol: boundNode.Variable.GetPublicSymbol(),
                inputType: boundNode.InputType.GetPublicSymbol(),
                narrowedType: boundNode.NarrowedType.GetPublicSymbol(),
                _semanticModel,
                boundNode.Syntax,
                isImplicit: boundNode.WasCompilerGenerated);
        }

        private IOperation CreateBoundNegatedPatternOperation(BoundNegatedPattern boundNegatedPattern)
        {
            return new NegatedPatternOperation(
                (IPatternOperation)Create(boundNegatedPattern.Negated),
                boundNegatedPattern.InputType.GetPublicSymbol(),
                boundNegatedPattern.NarrowedType.GetPublicSymbol(),
                _semanticModel,
                boundNegatedPattern.Syntax,
                isImplicit: boundNegatedPattern.WasCompilerGenerated);
        }

        private IOperation CreateBoundBinaryPatternOperation(BoundBinaryPattern boundBinaryPattern)
        {
            if (boundBinaryPattern.Left is not BoundBinaryPattern)
            {
                return createOperation(this, boundBinaryPattern, left: (IPatternOperation)Create(boundBinaryPattern.Left));
            }

            // Use a manual stack to avoid overflowing on deeply-nested binary patterns
            var stack = ArrayBuilder<BoundBinaryPattern>.GetInstance();
            BoundBinaryPattern? current = boundBinaryPattern;

            do
            {
                stack.Push(current);
                current = current.Left as BoundBinaryPattern;
            } while (current != null);

            current = stack.Pop();
            var result = (IPatternOperation)Create(current.Left);
            do
            {
                result = createOperation(this, current, result);
            } while (stack.TryPop(out current));

            stack.Free();
            return result;

            static BinaryPatternOperation createOperation(CSharpOperationFactory @this, BoundBinaryPattern boundBinaryPattern, IPatternOperation left)
            {
                return new BinaryPatternOperation(
                            boundBinaryPattern.Disjunction ? BinaryOperatorKind.Or : BinaryOperatorKind.And,
                            left,
                            (IPatternOperation)@this.Create(boundBinaryPattern.Right),
                            boundBinaryPattern.InputType.GetPublicSymbol(),
                            boundBinaryPattern.NarrowedType.GetPublicSymbol(),
                            @this._semanticModel,
                            boundBinaryPattern.Syntax,
                            isImplicit: boundBinaryPattern.WasCompilerGenerated);
            }
        }

        private ISwitchOperation CreateBoundSwitchStatementOperation(BoundSwitchStatement boundSwitchStatement)
        {
            IOperation value = Create(boundSwitchStatement.Expression);
            ImmutableArray<ISwitchCaseOperation> cases = CreateFromArray<BoundSwitchSection, ISwitchCaseOperation>(boundSwitchStatement.SwitchSections);
            ImmutableArray<ILocalSymbol> locals = boundSwitchStatement.InnerLocals.GetPublicSymbols();
            ILabelSymbol exitLabel = boundSwitchStatement.BreakLabel.GetPublicSymbol();
            SyntaxNode syntax = boundSwitchStatement.Syntax;
            bool isImplicit = boundSwitchStatement.WasCompilerGenerated;
            return new SwitchOperation(locals, value, cases, exitLabel, _semanticModel, syntax, isImplicit);
        }

        private ISwitchCaseOperation CreateBoundSwitchSectionOperation(BoundSwitchSection boundSwitchSection)
        {
            ImmutableArray<ICaseClauseOperation> clauses = CreateFromArray<BoundSwitchLabel, ICaseClauseOperation>(boundSwitchSection.SwitchLabels);
            ImmutableArray<IOperation> body = CreateFromArray<BoundStatement, IOperation>(boundSwitchSection.Statements);
            ImmutableArray<ILocalSymbol> locals = boundSwitchSection.Locals.GetPublicSymbols();

            return new SwitchCaseOperation(clauses, body, locals, condition: null, _semanticModel, boundSwitchSection.Syntax, isImplicit: boundSwitchSection.WasCompilerGenerated);
        }

        private ISwitchExpressionOperation CreateBoundSwitchExpressionOperation(BoundConvertedSwitchExpression boundSwitchExpression)
        {
            IOperation value = Create(boundSwitchExpression.Expression);
            ImmutableArray<ISwitchExpressionArmOperation> arms = CreateFromArray<BoundSwitchExpressionArm, ISwitchExpressionArmOperation>(boundSwitchExpression.SwitchArms);

            bool isExhaustive;
            if (boundSwitchExpression.DefaultLabel != null)
            {
                Debug.Assert(boundSwitchExpression.DefaultLabel is GeneratedLabelSymbol);
                isExhaustive = false;
            }
            else
            {
                isExhaustive = true;
            }

            return new SwitchExpressionOperation(
                value,
                arms,
                isExhaustive,
                _semanticModel,
                boundSwitchExpression.Syntax,
                boundSwitchExpression.GetPublicTypeSymbol(),
                boundSwitchExpression.WasCompilerGenerated);
        }

        private ISwitchExpressionArmOperation CreateBoundSwitchExpressionArmOperation(BoundSwitchExpressionArm boundSwitchExpressionArm)
        {
            IPatternOperation pattern = (IPatternOperation)Create(boundSwitchExpressionArm.Pattern);
            IOperation? guard = Create(boundSwitchExpressionArm.WhenClause);
            IOperation value = Create(boundSwitchExpressionArm.Value);
            return new SwitchExpressionArmOperation(
                pattern,
                guard,
                value,
                boundSwitchExpressionArm.Locals.GetPublicSymbols(),
                _semanticModel,
                boundSwitchExpressionArm.Syntax,
                boundSwitchExpressionArm.WasCompilerGenerated);
        }

        private ICaseClauseOperation CreateBoundSwitchLabelOperation(BoundSwitchLabel boundSwitchLabel)
        {
            SyntaxNode syntax = boundSwitchLabel.Syntax;
            bool isImplicit = boundSwitchLabel.WasCompilerGenerated;
            LabelSymbol label = boundSwitchLabel.Label;

            if (boundSwitchLabel.Syntax.Kind() == SyntaxKind.DefaultSwitchLabel)
            {
                Debug.Assert(boundSwitchLabel.Pattern.Kind == BoundKind.DiscardPattern);
                return new DefaultCaseClauseOperation(label.GetPublicSymbol(), _semanticModel, syntax, isImplicit);
            }
            else if (boundSwitchLabel.WhenClause == null &&
                     boundSwitchLabel.Pattern.Kind == BoundKind.ConstantPattern &&
                     boundSwitchLabel.Pattern is BoundConstantPattern cp &&
                     cp.InputType.IsValidV6SwitchGoverningType())
            {
                return new SingleValueCaseClauseOperation(Create(cp.Value), label.GetPublicSymbol(), _semanticModel, syntax, isImplicit);
            }
            else
            {
                IPatternOperation pattern = (IPatternOperation)Create(boundSwitchLabel.Pattern);
                IOperation? guard = Create(boundSwitchLabel.WhenClause);
                return new PatternCaseClauseOperation(label.GetPublicSymbol(), pattern, guard, _semanticModel, syntax, isImplicit);
            }
        }

        private IIsPatternOperation CreateBoundIsPatternExpressionOperation(BoundIsPatternExpression boundIsPatternExpression)
        {
            IOperation value = Create(boundIsPatternExpression.Expression);
            IPatternOperation pattern = (IPatternOperation)Create(boundIsPatternExpression.Pattern);
            SyntaxNode syntax = boundIsPatternExpression.Syntax;
            ITypeSymbol? type = boundIsPatternExpression.GetPublicTypeSymbol();
            bool isImplicit = boundIsPatternExpression.WasCompilerGenerated;
            return new IsPatternOperation(value, pattern, _semanticModel, syntax, type, isImplicit);
        }

        private IOperation CreateBoundQueryClauseOperation(BoundQueryClause boundQueryClause)
        {
            if (boundQueryClause.Syntax.Kind() != SyntaxKind.QueryExpression)
            {
                // Currently we have no IOperation APIs for different query clauses or continuation.
                return Create(boundQueryClause.Value);
            }

            IOperation operation = Create(boundQueryClause.Value);
            SyntaxNode syntax = boundQueryClause.Syntax;
            ITypeSymbol? type = boundQueryClause.GetPublicTypeSymbol();
            bool isImplicit = boundQueryClause.WasCompilerGenerated;
            return new TranslatedQueryOperation(operation, _semanticModel, syntax, type, isImplicit);
        }

        private IOperation CreateBoundRangeVariableOperation(BoundRangeVariable boundRangeVariable)
        {
            // We do not have operation nodes for the bound range variables, just it's value.
            return Create(boundRangeVariable.Value);
        }

        private IOperation CreateBoundDiscardExpressionOperation(BoundDiscardExpression boundNode)
        {
            return new DiscardOperation(
                ((DiscardSymbol)boundNode.ExpressionSymbol).GetPublicSymbol(),
                _semanticModel,
                boundNode.Syntax,
                boundNode.GetPublicTypeSymbol(),
                isImplicit: boundNode.WasCompilerGenerated);
        }

        private IOperation CreateFromEndIndexExpressionOperation(BoundFromEndIndexExpression boundIndex)
        {
            return new UnaryOperation(
                UnaryOperatorKind.Hat,
                Create(boundIndex.Operand),
                isLifted: boundIndex.Type.IsNullableType(),
                isChecked: false,
                operatorMethod: null,
                constrainedToType: null,
                _semanticModel,
                boundIndex.Syntax,
                boundIndex.GetPublicTypeSymbol(),
                constantValue: null,
                isImplicit: boundIndex.WasCompilerGenerated);
        }

        private IOperation CreateRangeExpressionOperation(BoundRangeExpression boundRange)
        {
            IOperation? left = Create(boundRange.LeftOperandOpt);
            IOperation? right = Create(boundRange.RightOperandOpt);
            return new RangeOperation(
                left, right,
                isLifted: boundRange.Type.IsNullableType(),
                boundRange.MethodOpt.GetPublicSymbol(),
                _semanticModel,
                boundRange.Syntax,
                boundRange.GetPublicTypeSymbol(),
                isImplicit: boundRange.WasCompilerGenerated);
        }

        private IOperation CreateBoundDiscardPatternOperation(BoundDiscardPattern boundNode)
        {
            return new DiscardPatternOperation(
                inputType: boundNode.InputType.GetPublicSymbol(),
                narrowedType: boundNode.NarrowedType.GetPublicSymbol(),
                _semanticModel,
                boundNode.Syntax,
                isImplicit: boundNode.WasCompilerGenerated);
        }

        internal IPropertySubpatternOperation CreatePropertySubpattern(BoundPropertySubpattern subpattern, ITypeSymbol matchedType)
        {
            // We treat `c is { ... .Prop: <pattern> }` as `c is { ...: { Prop: <pattern> } }`

            SyntaxNode subpatternSyntax = subpattern.Syntax;
            BoundPropertySubpatternMember? member = subpattern.Member;
            IPatternOperation pattern = (IPatternOperation)Create(subpattern.Pattern);
            if (member is null)
            {
                var reference = OperationFactory.CreateInvalidOperation(_semanticModel, subpatternSyntax, ImmutableArray<IOperation>.Empty, isImplicit: true);
                return new PropertySubpatternOperation(reference, pattern, _semanticModel, subpatternSyntax, isImplicit: false);
            }

            // Create an operation for last property access:
            // `{ SingleProp: <pattern operation> }`
            // or
            // `.LastProp: <pattern operation>` portion (treated as `{ LastProp: <pattern operation> }`)
            var nameSyntax = member.Syntax;
            var inputType = getInputType(member, matchedType);
            IPropertySubpatternOperation? result = createPropertySubpattern(member.Symbol, pattern, inputType, nameSyntax, isSingle: member.Receiver is null);

            while (member.Receiver is not null)
            {
                member = member.Receiver;
                nameSyntax = member.Syntax;
                ITypeSymbol previousType = inputType;
                inputType = getInputType(member, matchedType);

                // Create an operation for a preceding property access:
                // { PrecedingProp: <previous pattern operation> }
                IPatternOperation nestedPattern = new RecursivePatternOperation(
                    matchedType: previousType, deconstructSymbol: null, deconstructionSubpatterns: ImmutableArray<IPatternOperation>.Empty,
                    propertySubpatterns: ImmutableArray.Create(result), declaredSymbol: null,
                    previousType, narrowedType: previousType, semanticModel: _semanticModel, nameSyntax, isImplicit: true);

                result = createPropertySubpattern(member.Symbol, nestedPattern, inputType, nameSyntax, isSingle: false);
            }

            return result;

            IPropertySubpatternOperation createPropertySubpattern(Symbol? symbol, IPatternOperation pattern, ITypeSymbol receiverType, SyntaxNode nameSyntax, bool isSingle)
            {
                Debug.Assert(nameSyntax is not null);
                IOperation reference;
                switch (symbol)
                {
                    case FieldSymbol field:
                        {
                            var constantValue = field.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);
                            reference = new FieldReferenceOperation(field.GetPublicSymbol(), isDeclaration: false,
                                createReceiver(), _semanticModel, nameSyntax, type: field.Type.GetPublicSymbol(), constantValue, isImplicit: false);
                            break;
                        }
                    case PropertySymbol property:
                        {
                            reference = new PropertyReferenceOperation(property.GetPublicSymbol(), constrainedToType: null, ImmutableArray<IArgumentOperation>.Empty,
                                createReceiver(), _semanticModel, nameSyntax, type: property.Type.GetPublicSymbol(), isImplicit: false);
                            break;
                        }
                    default:
                        {
                            // We should expose the symbol in this case somehow:
                            // https://github.com/dotnet/roslyn/issues/33175
                            reference = OperationFactory.CreateInvalidOperation(_semanticModel, nameSyntax, ImmutableArray<IOperation>.Empty, isImplicit: false);
                            break;
                        }
                }

                var syntaxForPropertySubpattern = isSingle ? subpatternSyntax : nameSyntax;
                return new PropertySubpatternOperation(reference, pattern, _semanticModel, syntaxForPropertySubpattern, isImplicit: !isSingle);

                IOperation? createReceiver()
                    => symbol?.IsStatic == false ? new InstanceReferenceOperation(InstanceReferenceKind.PatternInput, _semanticModel, nameSyntax!, receiverType, isImplicit: true) : null;
            }

            static ITypeSymbol getInputType(BoundPropertySubpatternMember member, ITypeSymbol matchedType)
                => member.Receiver?.Type.StrippedType().GetPublicSymbol() ?? matchedType;
        }

        private IInstanceReferenceOperation CreateCollectionValuePlaceholderOperation(BoundObjectOrCollectionValuePlaceholder placeholder)
        {
            InstanceReferenceKind referenceKind = InstanceReferenceKind.ImplicitReceiver;
            SyntaxNode syntax = placeholder.Syntax;
            ITypeSymbol? type = placeholder.GetPublicTypeSymbol();
            bool isImplicit = placeholder.WasCompilerGenerated;
            return new InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, isImplicit);
        }

        private ImmutableArray<IArgumentOperation> CreateDisposeArguments(MethodArgumentInfo patternDisposeInfo)
        {
            // can't be an extension method for dispose
            Debug.Assert(!patternDisposeInfo.Method.IsStatic);

            if (patternDisposeInfo.Method.ParameterCount == 0)
            {
                return ImmutableArray<IArgumentOperation>.Empty;
            }

            var args = DeriveArguments(
                            patternDisposeInfo.Method,
                            patternDisposeInfo.Arguments,
                            argumentsToParametersOpt: default,
                            patternDisposeInfo.DefaultArguments,
                            invokedAsExtensionMethod: false);

            return Operation.SetParentOperation(args, null);
        }
    }
}
