// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Use this to create IOperation when we don't have proper specific IOperation yet for given language construct
    /// </summary>
    internal abstract class BaseNoneOperation : Operation
    {
        protected BaseNoneOperation(SemanticModel semanticModel, SyntaxNode syntax, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.None, semanticModel, syntax, type: null, constantValue, isImplicit)
        {
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal class NoneOperation : BaseNoneOperation
    {
        public NoneOperation(ImmutableArray<IOperation> children, SemanticModel semanticModel, SyntaxNode syntax, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, constantValue, isImplicit)
        {
            Children = SetParentOperation(children, this);
        }

        public override IEnumerable<IOperation> Children { get; }
    }

    internal abstract class LazyNoneOperation : BaseNoneOperation
    {
        private ImmutableArray<IOperation> _lazyChildrenInterlocked;

        public LazyNoneOperation(SemanticModel semanticModel, SyntaxNode node, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, node, constantValue: constantValue, isImplicit: isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> GetChildren();

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (_lazyChildrenInterlocked.IsDefault)
                {
                    ImmutableArray<IOperation> children = GetChildren();
                    SetParentOperation(children, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyChildrenInterlocked, children);
                }

                return _lazyChildrenInterlocked;
            }

        }
    }

    internal abstract partial class BaseArgumentOperation
    {
        internal BaseArgumentOperation(ArgumentKind argumentKind, IParameterSymbol parameter, IConvertibleConversion inConversion, IConvertibleConversion outConversion, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(argumentKind, parameter, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InConversionConvertibleOpt = inConversion;
            OutConversionConvertibleOpt = outConversion;
        }

        internal IConvertibleConversion InConversionConvertibleOpt { get; }
        internal IConvertibleConversion OutConversionConvertibleOpt { get; }
        public CommonConversion InConversion => InConversionConvertibleOpt?.ToCommonConversion() ?? Identity();
        public CommonConversion OutConversion => OutConversionConvertibleOpt?.ToCommonConversion() ?? Identity();

        private static CommonConversion Identity()
        {
            return new CommonConversion(exists: true, isIdentity: true, isNumeric: false, isReference: false, methodSymbol: null, isImplicit: true);
        }
    }

    internal sealed partial class ArgumentOperation
    {
        public ArgumentOperation(IOperation value, ArgumentKind argumentKind, IParameterSymbol parameter, IConvertibleConversion inConversionOpt, IConvertibleConversion outConversionOpt, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit) :
            base(argumentKind, parameter, inConversionOpt, outConversionOpt, semanticModel, syntax, type: null, constantValue: default, isImplicit)
        {
            Value = SetParentOperation(value, this);
        }
    }


    internal abstract partial class LazyArgumentOperation
    {
        public LazyArgumentOperation(ArgumentKind argumentKind, IConvertibleConversion inConversionOpt, IConvertibleConversion outConversionOpt, IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit) :
            base(argumentKind, parameter, inConversionOpt, outConversionOpt, semanticModel, syntax, type: null, constantValue: default, isImplicit)
        { }
    }

    internal sealed partial class ArrayInitializerOperation : BaseArrayInitializerOperation, IArrayInitializerOperation
    {
        public ArrayInitializerOperation(ImmutableArray<IOperation> elementValues, SemanticModel semanticModel, SyntaxNode syntax, Optional<object> constantValue, bool isImplicit) :
            this(elementValues, semanticModel, syntax, type: null, constantValue, isImplicit)
        { }

    }

    internal abstract partial class BaseConversionOperation : Operation, IConversionOperation
    {
        public IMethodSymbol OperatorMethod => Conversion.MethodSymbol;
    }

    internal abstract partial class BaseEventReferenceOperation
    {
        public override ISymbol Member => Event;
    }

    internal sealed partial class VariableInitializerOperation : BaseVariableInitializerOperation, IVariableInitializerOperation
    {
        public VariableInitializerOperation(IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(ImmutableArray<ILocalSymbol>.Empty, value, semanticModel, syntax, type, constantValue, isImplicit)
        { }
    }

    internal abstract partial class BaseFieldReferenceOperation
    {
        public override ISymbol Member => Field;
    }

    internal abstract partial class BaseForEachLoopOperation : BaseLoopOperation, IForEachLoopOperation
    {
        public abstract ForEachLoopOperationInfo Info { get; }
    }

    internal sealed partial class ForEachLoopOperation : BaseForEachLoopOperation, IForEachLoopOperation
    {
        public ForEachLoopOperation(ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, IOperation loopControlVariable,
                                    IOperation collection, ImmutableArray<IOperation> nextVariables, IOperation body, ForEachLoopOperationInfo info,
                                    SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(loopControlVariable, collection, nextVariables, LoopKind.ForEach, body, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Info = info;
        }

        public override ForEachLoopOperationInfo Info { get; }
    }

    internal abstract partial class LazyForEachLoopOperation : BaseForEachLoopOperation, IForEachLoopOperation
    {
        private ForEachLoopOperationInfo _lazyForEachLoopInfoInterlocked = null;
        protected abstract ForEachLoopOperationInfo CreateLoopInfo();
        public override ForEachLoopOperationInfo Info
        {
            get
            {
                if (_lazyForEachLoopInfoInterlocked == null)
                {
                    Interlocked.CompareExchange(ref _lazyForEachLoopInfoInterlocked, CreateLoopInfo(), null);
                }

                return _lazyForEachLoopInfoInterlocked;
            }
        }
    }

    internal sealed partial class ForLoopOperation : BaseForLoopOperation, IForLoopOperation
    {
        public ForLoopOperation(ImmutableArray<IOperation> before, IOperation condition, ImmutableArray<IOperation> atLoopBottom, ImmutableArray<ILocalSymbol> locals, ImmutableArray<ILocalSymbol> conditionLocals,
            ILabelSymbol continueLabel, ILabelSymbol exitLabel, IOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(before, conditionLocals, condition, atLoopBottom, LoopKind.For, body, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        { }
    }

    internal abstract partial class LazyForLoopOperation : BaseForLoopOperation, IForLoopOperation
    {
        public LazyForLoopOperation(ImmutableArray<ILocalSymbol> locals, ImmutableArray<ILocalSymbol> conditionLocals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(conditionLocals, LoopKind.For, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        { }
    }

    internal sealed partial class ForToLoopOperation : BaseForToLoopOperation, IForToLoopOperation
    {
        public ForToLoopOperation(ImmutableArray<ILocalSymbol> locals, bool isChecked,
                                  (ILocalSymbol LoopObject, ForToLoopOperationUserDefinedInfo UserDefinedInfo) info,
                                  ILabelSymbol continueLabel, ILabelSymbol exitLabel, IOperation loopControlVariable,
                                  IOperation initialValue, IOperation limitValue, IOperation stepValue, IOperation body,
                                  ImmutableArray<IOperation> nextVariables, SemanticModel semanticModel, SyntaxNode syntax,
                                  ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(loopControlVariable, initialValue, limitValue, stepValue, isChecked, nextVariables, info, LoopKind.ForTo, body, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        { }
    }

    internal abstract partial class BaseInvalidOperation : Operation, IInvalidOperation
    {
        protected BaseInvalidOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.Invalid, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvalid(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvalid(this, argument);
        }
    }

    internal sealed partial class InvalidOperation : BaseInvalidOperation, IInvalidOperation
    {
        public InvalidOperation(ImmutableArray<IOperation> children, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            // we don't allow null children.
            Debug.Assert(children.All(o => o != null));
            Children = SetParentOperation(children, this);
        }
        public override IEnumerable<IOperation> Children { get; }
    }

    internal abstract class LazyInvalidOperation : BaseInvalidOperation, IInvalidOperation
    {
        private ImmutableArray<IOperation> _lazyChildrenInterlocked;

        public LazyInvalidOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> CreateChildren();

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (_lazyChildrenInterlocked.IsDefault)
                {
                    ImmutableArray<IOperation> children = CreateChildren();
                    SetParentOperation(children, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyChildrenInterlocked, children);
                }

                return _lazyChildrenInterlocked;
            }
        }
    }

    internal sealed partial class SingleValueCaseClauseOperation : BaseSingleValueCaseClauseOperation, ISingleValueCaseClauseOperation
    {
        public SingleValueCaseClauseOperation(ILabelSymbol label, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(value, CaseKind.SingleValue, label, semanticModel, syntax, type, constantValue, isImplicit)
        { }
    }

    internal sealed class FlowAnonymousFunctionOperation : Operation, IFlowAnonymousFunctionOperation
    {
        public readonly ControlFlowGraphBuilder.Context Context;
        public readonly IAnonymousFunctionOperation Original;

        public FlowAnonymousFunctionOperation(in ControlFlowGraphBuilder.Context context, IAnonymousFunctionOperation original, bool isImplicit) :
            base(OperationKind.FlowAnonymousFunction, semanticModel: null, original.Syntax, original.Type, original.ConstantValue, isImplicit)
        {
            Context = context;
            Original = original;
        }
        public IMethodSymbol Symbol => Original.Symbol;
        public override IEnumerable<IOperation> Children
        {
            get
            {
                return ImmutableArray<IOperation>.Empty;
            }
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFlowAnonymousFunction(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFlowAnonymousFunction(this, argument);
        }
    }

    internal abstract partial class BaseMemberReferenceOperation : Operation, IMemberReferenceOperation
    {
        public abstract ISymbol Member { get; }
    }

    internal abstract partial class BaseMethodReferenceOperation
    {
        public override ISymbol Member => Method;
    }

    internal abstract partial class BasePropertyReferenceOperation
    {
        public override ISymbol Member => Property;
    }

    internal sealed partial class RangeCaseClauseOperation : BaseRangeCaseClauseOperation, IRangeCaseClauseOperation
    {
        public RangeCaseClauseOperation(IOperation minimumValue, IOperation maximumValue, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(minimumValue, maximumValue, CaseKind.Range, label: null, semanticModel, syntax, type, constantValue, isImplicit)
        { }
    }

    internal sealed partial class RelationalCaseClauseOperation : BaseRelationalCaseClauseOperation, IRelationalCaseClauseOperation
    {
        public RelationalCaseClauseOperation(IOperation value, BinaryOperatorKind relation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(value, relation, CaseKind.Relational, label: null, semanticModel, syntax, type, constantValue, isImplicit)
        { }
    }

    internal sealed partial class DefaultCaseClauseOperation : BaseCaseClauseOperation
    {
        public DefaultCaseClauseOperation(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(CaseKind.Default, label, semanticModel, syntax, type, constantValue, isImplicit)
        { }
    }

    internal abstract partial class BaseSwitchCaseOperation
    {
        /// <summary>
        /// Optional combined logical condition that accounts for all <see cref="Clauses"/>.
        /// An instance of <see cref="IPlaceholderOperation"/> with kind <see cref="PlaceholderKind.SwitchOperationExpression"/>
        /// is used to refer to the <see cref="ISwitchOperation.Value"/> in context of this expression.
        /// It is not part of <see cref="Children"/> list and likely contains duplicate nodes for
        /// nodes exposed by <see cref="Clauses"/>, like <see cref="ISingleValueCaseClauseOperation.Value"/>,
        /// etc.
        /// Never set for C# at the moment.
        /// </summary>
        public abstract IOperation Condition { get; }
    }

    internal sealed partial class SwitchCaseOperation : BaseSwitchCaseOperation, ISwitchCaseOperation
    {
        public SwitchCaseOperation(ImmutableArray<ILocalSymbol> locals, IOperation condition, ImmutableArray<ICaseClauseOperation> clauses, ImmutableArray<IOperation> body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(clauses, body, locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Condition = SetParentOperation(condition, null);
        }

        public override IOperation Condition { get; }
    }

    internal abstract partial class LazySwitchCaseOperation
    {
        private IOperation _lazyConditionInterlocked = s_unset;
        protected abstract IOperation CreateCondition();
        public override IOperation Condition
        {
            get
            {
                if (_lazyConditionInterlocked == s_unset)
                {
                    IOperation condition = CreateCondition();
                    SetParentOperation(condition, null);
                    Interlocked.CompareExchange(ref _lazyConditionInterlocked, condition, s_unset);
                }

                return _lazyConditionInterlocked;
            }
        }
    }

    internal abstract partial class HasDynamicArgumentsExpression : Operation
    {
        protected HasDynamicArgumentsExpression(OperationKind operationKind, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(operationKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ArgumentNames = argumentNames;
            ArgumentRefKinds = argumentRefKinds;
        }

        public ImmutableArray<string> ArgumentNames { get; }
        public ImmutableArray<RefKind> ArgumentRefKinds { get; }
        public abstract ImmutableArray<IOperation> Arguments { get; }
    }

    internal abstract partial class BaseDynamicObjectCreationOperation : HasDynamicArgumentsExpression, IDynamicObjectCreationOperation
    {
        public BaseDynamicObjectCreationOperation(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DynamicObjectCreation, argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var argument in Arguments)
                {
                    if (argument != null)
                    {
                        yield return argument;
                    }
                }
                if (Initializer != null)
                {
                    yield return Initializer;
                }
            }
        }
        public abstract IObjectOrCollectionInitializerOperation Initializer { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicObjectCreation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicObjectCreation(this, argument);
        }
    }

    internal sealed partial class DynamicObjectCreationOperation : BaseDynamicObjectCreationOperation, IDynamicObjectCreationOperation
    {
        public DynamicObjectCreationOperation(ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, IObjectOrCollectionInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Arguments = SetParentOperation(arguments, this);
            Initializer = SetParentOperation(initializer, this);
        }
        public override ImmutableArray<IOperation> Arguments { get; }
        public override IObjectOrCollectionInitializerOperation Initializer { get; }
    }

    internal abstract class LazyDynamicObjectCreationOperation : BaseDynamicObjectCreationOperation, IDynamicObjectCreationOperation
    {
        private ImmutableArray<IOperation> _lazyArgumentsInterlocked;
        private IObjectOrCollectionInitializerOperation _lazyInitializerInterlocked = s_unsetObjectOrCollectionInitializer;

        public LazyDynamicObjectCreationOperation(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract ImmutableArray<IOperation> CreateArguments();
        protected abstract IObjectOrCollectionInitializerOperation CreateInitializer();

        public override ImmutableArray<IOperation> Arguments
        {
            get
            {
                if (_lazyArgumentsInterlocked.IsDefault)
                {
                    ImmutableArray<IOperation> arguments = CreateArguments();
                    SetParentOperation(arguments, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyArgumentsInterlocked, arguments);
                }

                return _lazyArgumentsInterlocked;
            }
        }

        public override IObjectOrCollectionInitializerOperation Initializer
        {
            get
            {
                if (_lazyInitializerInterlocked == s_unsetObjectOrCollectionInitializer)
                {
                    IObjectOrCollectionInitializerOperation initializer = CreateInitializer();
                    SetParentOperation(initializer, this);
                    Interlocked.CompareExchange(ref _lazyInitializerInterlocked, initializer, s_unsetObjectOrCollectionInitializer);
                }

                return _lazyInitializerInterlocked;
            }
        }
    }

    internal abstract partial class BaseDynamicInvocationOperation : HasDynamicArgumentsExpression, IDynamicInvocationOperation
    {
        public BaseDynamicInvocationOperation(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DynamicInvocation, argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation != null)
                {
                    yield return Operation;
                }
                foreach (var argument in Arguments)
                {
                    if (argument != null)
                    {
                        yield return argument;
                    }
                }
            }
        }
        public abstract IOperation Operation { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicInvocation(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicInvocation(this, argument);
        }
    }

    internal sealed partial class DynamicInvocationOperation : BaseDynamicInvocationOperation, IDynamicInvocationOperation
    {
        public DynamicInvocationOperation(IOperation operation, ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
            Arguments = SetParentOperation(arguments, this);
        }
        public override IOperation Operation { get; }
        public override ImmutableArray<IOperation> Arguments { get; }
    }

    internal abstract class LazyDynamicInvocationOperation : BaseDynamicInvocationOperation, IDynamicInvocationOperation
    {
        private IOperation _lazyOperationInterlocked = s_unset;
        private ImmutableArray<IOperation> _lazyArgumentsInterlocked;

        public LazyDynamicInvocationOperation(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation CreateOperation();
        protected abstract ImmutableArray<IOperation> CreateArguments();

        public override IOperation Operation
        {
            get
            {
                if (_lazyOperationInterlocked == s_unset)
                {
                    IOperation operation = CreateOperation();
                    SetParentOperation(operation, this);
                    Interlocked.CompareExchange(ref _lazyOperationInterlocked, operation, s_unset);
                }

                return _lazyOperationInterlocked;
            }
        }

        public override ImmutableArray<IOperation> Arguments
        {
            get
            {
                if (_lazyArgumentsInterlocked.IsDefault)
                {
                    ImmutableArray<IOperation> arguments = CreateArguments();
                    SetParentOperation(arguments, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyArgumentsInterlocked, arguments);
                }

                return _lazyArgumentsInterlocked;
            }
        }
    }

    internal abstract partial class BaseDynamicIndexerAccessOperation : HasDynamicArgumentsExpression, IDynamicIndexerAccessOperation
    {
        public BaseDynamicIndexerAccessOperation(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(OperationKind.DynamicIndexerAccess, argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation != null)
                {
                    yield return Operation;
                }
                foreach (var argument in Arguments)
                {
                    if (argument != null)
                    {
                        yield return argument;
                    }
                }
            }
        }
        public abstract IOperation Operation { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDynamicIndexerAccess(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicIndexerAccess(this, argument);
        }
    }

    internal sealed partial class DynamicIndexerAccessOperation : BaseDynamicIndexerAccessOperation, IDynamicIndexerAccessOperation
    {
        public DynamicIndexerAccessOperation(IOperation operation, ImmutableArray<IOperation> arguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
            Arguments = SetParentOperation(arguments, this);
        }
        public override IOperation Operation { get; }
        public override ImmutableArray<IOperation> Arguments { get; }
    }

    internal abstract class LazyDynamicIndexerAccessOperation : BaseDynamicIndexerAccessOperation, IDynamicIndexerAccessOperation
    {
        private IOperation _lazyOperationInterlocked = s_unset;
        private ImmutableArray<IOperation> _lazyArgumentsInterlocked;

        public LazyDynamicIndexerAccessOperation(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IOperation CreateOperation();
        protected abstract ImmutableArray<IOperation> CreateArguments();

        public override IOperation Operation
        {
            get
            {
                if (_lazyOperationInterlocked == s_unset)
                {
                    IOperation operation = CreateOperation();
                    SetParentOperation(operation, this);
                    Interlocked.CompareExchange(ref _lazyOperationInterlocked, operation, s_unset);
                }

                return _lazyOperationInterlocked;
            }
        }

        public override ImmutableArray<IOperation> Arguments
        {
            get
            {
                if (_lazyArgumentsInterlocked.IsDefault)
                {
                    ImmutableArray<IOperation> arguments = CreateArguments();
                    SetParentOperation(arguments, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyArgumentsInterlocked, arguments);
                }

                return _lazyArgumentsInterlocked;
            }
        }
    }

    internal abstract partial class BaseWhileLoopOperation : BaseLoopOperation, IWhileLoopOperation
    {
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (ConditionIsTop)
                {
                    if (Condition != null)
                    {
                        yield return Condition;
                    }
                }
                if (Body != null)
                {
                    yield return Body;
                }
                if (!ConditionIsTop)
                {
                    if (Condition != null)
                    {
                        yield return Condition;
                    }
                }
                if (IgnoredCondition != null)
                {
                    yield return IgnoredCondition;
                }
            }
        }
    }

    internal sealed partial class WhileLoopOperation : BaseWhileLoopOperation, IWhileLoopOperation
    {
        public WhileLoopOperation(IOperation condition, IOperation body, IOperation ignoredCondition, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, bool conditionIsTop, bool conditionIsUntil, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            this(condition, conditionIsTop, conditionIsUntil, ignoredCondition, LoopKind.While, body, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)

        { }
    }

    internal sealed partial class ConstantPatternOperation : BaseConstantPatternOperation, IConstantPatternOperation
    {
        public ConstantPatternOperation(ITypeSymbol inputType, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit) :
            this(value, inputType, semanticModel, syntax, type: null, constantValue: default, isImplicit)
        { }
    }

    internal sealed partial class DeclarationPatternOperation : BasePatternOperation, IDeclarationPatternOperation
    {
        public DeclarationPatternOperation(
            ITypeSymbol inputType,
            ITypeSymbol matchedType,
            ISymbol declaredSymbol,
            bool matchesNull,
            SemanticModel semanticModel,
            SyntaxNode syntax,
            bool isImplicit)
            : this(matchedType, matchesNull, declaredSymbol, inputType, semanticModel, syntax, type: default, constantValue: default, isImplicit)
        { }
    }

    internal sealed partial class RecursivePatternOperation : BaseRecursivePatternOperation
    {
        public RecursivePatternOperation(
            ITypeSymbol inputType,
            ITypeSymbol matchedType,
            ISymbol deconstructSymbol,
            ImmutableArray<IPatternOperation> deconstructionSubpatterns,
            ImmutableArray<IPropertySubpatternOperation> propertySubpatterns,
            ISymbol declaredSymbol, SemanticModel semanticModel,
            SyntaxNode syntax,
            bool isImplicit) :
            this(matchedType, deconstructSymbol, deconstructionSubpatterns, propertySubpatterns, declaredSymbol, inputType, semanticModel, syntax, type: null, constantValue: default, isImplicit)
        { }
    }

    internal sealed partial class PropertySubpatternOperation : BasePropertySubpatternOperation
    {
        public PropertySubpatternOperation(
            SemanticModel semanticModel,
            SyntaxNode syntax,
            bool isImplicit,
            IOperation member,
            IPatternOperation pattern) :
            this(member, pattern, semanticModel, syntax, type: null, constantValue: default, isImplicit)
        { }
    }

    internal abstract partial class BasePatternCaseClauseOperation : BaseCaseClauseOperation, IPatternCaseClauseOperation
    {
        protected BasePatternCaseClauseOperation(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(CaseKind.Pattern, label, OperationKind.CaseClause, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Pattern != null)
                {
                    yield return Pattern;
                }
                if (Guard != null)
                {
                    yield return Guard;
                }
            }
        }
        public abstract IPatternOperation Pattern { get; }
        public abstract IOperation Guard { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPatternCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPatternCaseClause(this, argument);
        }
    }

    internal sealed partial class PatternCaseClauseOperation : BasePatternCaseClauseOperation, IPatternCaseClauseOperation
    {
        public PatternCaseClauseOperation(ILabelSymbol label, IPatternOperation pattern, IOperation guardExpression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Pattern = SetParentOperation(pattern, this);
            Guard = SetParentOperation(guardExpression, this);
        }

        public override IPatternOperation Pattern { get; }
        public override IOperation Guard { get; }
    }

    internal abstract class LazyPatternCaseClauseOperation : BasePatternCaseClauseOperation, IPatternCaseClauseOperation
    {
        private IPatternOperation _lazyPatternInterlocked = s_unsetPattern;
        private IOperation _lazyGuardExpressionInterlocked = s_unset;

        public LazyPatternCaseClauseOperation(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected abstract IPatternOperation CreatePattern();
        protected abstract IOperation CreateGuard();

        public override IPatternOperation Pattern
        {
            get
            {
                if (_lazyPatternInterlocked == s_unsetPattern)
                {
                    IPatternOperation pattern = CreatePattern();
                    SetParentOperation(pattern, this);
                    Interlocked.CompareExchange(ref _lazyPatternInterlocked, pattern, s_unsetPattern);
                }

                return _lazyPatternInterlocked;
            }
        }

        public override IOperation Guard
        {
            get
            {
                if (_lazyGuardExpressionInterlocked == s_unset)
                {
                    IOperation guard = CreateGuard();
                    SetParentOperation(guard, this);
                    Interlocked.CompareExchange(ref _lazyGuardExpressionInterlocked, guard, s_unset);
                }

                return _lazyGuardExpressionInterlocked;
            }
        }
    }

    internal sealed partial class SwitchExpressionOperation : BaseSwitchExpressionOperation
    {
        public SwitchExpressionOperation(
            ITypeSymbol type,
            IOperation value,
            ImmutableArray<ISwitchExpressionArmOperation> arms,
            SemanticModel semanticModel,
            SyntaxNode syntax,
            bool isImplicit) :
            this(value, arms, semanticModel, syntax, type, constantValue: default, isImplicit)
        { }
    }

    internal sealed partial class SwitchExpressionArmOperation : BaseSwitchExpressionArmOperation
    {
        public SwitchExpressionArmOperation(
            ImmutableArray<ILocalSymbol> locals,
            IPatternOperation pattern,
            IOperation guard,
            IOperation value,
            SemanticModel semanticModel,
            SyntaxNode syntax,
            bool isImplicit) :
            this(pattern, guard, value, locals, semanticModel, syntax, type: null, constantValue: default, isImplicit)
        { }
    }

    internal sealed partial class FlowCaptureReferenceOperation : Operation, IFlowCaptureReferenceOperation
    {
        public FlowCaptureReferenceOperation(int id, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.FlowCaptureReference, semanticModel: null, syntax: syntax, type: type, constantValue: constantValue, isImplicit: true)
        {
            Id = new CaptureId(id);
        }

        public FlowCaptureReferenceOperation(CaptureId id, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.FlowCaptureReference, semanticModel: null, syntax: syntax, type: type, constantValue: constantValue, isImplicit: true)
        {
            Id = id;
        }
    }

    internal sealed partial class FlowCaptureOperation : Operation, IFlowCaptureOperation
    {
        public FlowCaptureOperation(int id, SyntaxNode syntax, IOperation value) :
            base(OperationKind.FlowCapture, semanticModel: null, syntax: syntax, type: null, constantValue: default, isImplicit: true)
        {
            Debug.Assert(value != null);
            Id = new CaptureId(id);
            Value = SetParentOperation(value, this);
        }

        public CaptureId Id { get; }
        public IOperation Value { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Value;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFlowCapture(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFlowCapture(this, argument);
        }
    }

    internal sealed partial class IsNullOperation : Operation, IIsNullOperation
    {
        public IsNullOperation(SyntaxNode syntax, IOperation operand, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.IsNull, semanticModel: null, syntax: syntax, type: type, constantValue: constantValue, isImplicit: true)
        {
            Debug.Assert(operand != null);
            Operand = SetParentOperation(operand, this);
        }

        public IOperation Operand { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                yield return Operand;
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIsNull(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIsNull(this, argument);
        }
    }

    internal sealed partial class CaughtExceptionOperation : Operation, ICaughtExceptionOperation
    {
        public CaughtExceptionOperation(SyntaxNode syntax, ITypeSymbol type) :
            this(semanticModel: null, syntax: syntax, type: type, constantValue: default, isImplicit: true)
        {
        }
    }

    internal sealed partial class StaticLocalInitializationSemaphoreOperation : Operation, IStaticLocalInitializationSemaphoreOperation
    {
        public StaticLocalInitializationSemaphoreOperation(ILocalSymbol local, SyntaxNode syntax, ITypeSymbol type) :
            base(OperationKind.StaticLocalInitializationSemaphore, semanticModel: null, syntax, type, constantValue: default, isImplicit: true)
        {
            Local = local;
        }
    }

    internal sealed partial class MethodBodyOperation : BaseMethodBodyOperation
    {
        public MethodBodyOperation(SemanticModel semanticModel, SyntaxNode syntax, IBlockOperation blockBody, IBlockOperation expressionBody) :
            this(blockBody, expressionBody, semanticModel, syntax, type: null, constantValue: default, isImplicit: false)
        { }
    }

    internal sealed partial class ConstructorBodyOperation : BaseConstructorBodyOperation
    {
        public ConstructorBodyOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax,
                                        IOperation initializer, IBlockOperation blockBody, IBlockOperation expressionBody) :
            this(locals, initializer, blockBody, expressionBody, semanticModel, syntax, type: null, constantValue: default, isImplicit: false)
        { }
    }

    internal sealed partial class DiscardPatternOperation : BasePatternOperation, IDiscardPatternOperation
    {
        public DiscardPatternOperation(ITypeSymbol inputType, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit) :
            this(inputType, semanticModel, syntax, type: null, constantValue: default, isImplicit)
        { }

    }

    internal sealed partial class RangeOperation : BaseRangeOperation
    {
        public RangeOperation(bool isLifted, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, IOperation leftOperand, IOperation rightOperand, IMethodSymbol symbol, bool isImplicit) :
            this(leftOperand, rightOperand, isLifted, method: symbol, semanticModel, syntax, type, constantValue: default, isImplicit)
        { }
    }
}
