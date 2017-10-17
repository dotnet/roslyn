// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundObjectCreationExpression
    {
        protected override OperationKind OperationKind => this.ExpressionKind;

        protected override ITypeSymbol OperationType => this.Type;

        protected abstract OperationKind ExpressionKind { get; }

        public override abstract void Accept(OperationVisitor visitor);

        public override abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);

        protected override Optional<object> OperationConstantValue
        {
            get
            {
                ConstantValue value = this.ConstantValue;
                return value != null ? new Optional<object>(value.Value) : default(Optional<object>);
            }
        }
    }

    internal sealed partial class BoundDeconstructValuePlaceholder : BoundValuePlaceholderBase, IPlaceholderExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.PlaceholderExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPlaceholderExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPlaceholderExpression(this, argument);
        }
    }

    internal partial class BoundCall : IInvocationExpression
                    {
        IMethodSymbol IInvocationExpression.TargetMethod => this.Method;

        IOperation IInvocationExpression.Instance => ((object)this.Method == null || this.Method.IsStatic) ? null : this.ReceiverOpt;

        bool IInvocationExpression.IsVirtual =>
            (object)this.Method != null &&
            this.ReceiverOpt != null &&
            (this.Method.IsVirtual || this.Method.IsAbstract || this.Method.IsOverride) &&
            !this.ReceiverOpt.SuppressVirtualCalls;

        ImmutableArray<IArgument> IHasArgumentsExpression.ArgumentsInEvaluationOrder
            => DeriveArguments(this, this.BinderOpt, this.Method, this.Method, this.Arguments, this.ArgumentNamesOpt, this.ArgsToParamsOpt, this.ArgumentRefKindsOpt, this.Method.Parameters, this.Expanded,  this.Syntax, this.InvokedAsExtensionMethod);

        protected override OperationKind ExpressionKind => OperationKind.InvocationExpression;
        
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvocationExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvocationExpression(this, argument);
        }

        private static readonly ConditionalWeakTable<BoundExpression, IEnumerable<IArgument>> s_callToArgumentsMappings 
            = new ConditionalWeakTable<BoundExpression, IEnumerable<IArgument>>(); 

        internal static IArgument CreateArgumentOperation(ArgumentKind kind, IParameterSymbol parameter, IOperation value)
        {
            return new Argument(kind,
                parameter,
                value,
                inConversion: null,
                outConversion: null,
                isInvalid: parameter == null || value.IsInvalid,
                syntax: value.Syntax,
                type: value.Type,
                constantValue: default(Optional<object>));
        }

        internal static ImmutableArray<IArgument> DeriveArguments(
            BoundExpression boundNode,
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

            return (ImmutableArray<IArgument>) s_callToArgumentsMappings.GetValue(
                boundNode, 
                (n) =>
                {
                    //TODO: https://github.com/dotnet/roslyn/issues/18722
                    //      Right now, for erroneous code, we exposes all expression in place of arguments as IArgument with Parameter set to null,
                    //      so user needs to check IsInvalid first before using anything we returned. Need to implement a new interface for invalid 
                    //      invocation instead.
                    //      Note this check doesn't cover all scenarios. For example, when a parameter is a generic type but the type of the type argument 
                    //      is undefined.
                    if ((object)optionalParametersMethod == null 
                        || n.HasAnyErrors
                        || parameters.Any(p => p.Type.IsErrorType())
                        || optionalParametersMethod.GetUseSiteDiagnostic()?.DefaultSeverity == DiagnosticSeverity.Error)
                    {
                        // optionalParametersMethod can be null if we are writing to a readonly indexer or reading from an writeonly indexer,
                        // in which case HasErrors property would be true, but we still want to treat this as invalid invocation.
                        return boundArguments.SelectAsArray(arg => CreateArgumentOperation(ArgumentKind.Explicit, null, arg));
                    }                                                                                           

                   return LocalRewriter.MakeArgumentsInEvaluationOrder(
                        binder: binder,
                        syntax: invocationSyntax,
                        arguments: boundArguments,
                        methodOrIndexer: methodOrIndexer,
                        optionalParametersMethod: optionalParametersMethod,
                        expanded: expanded,
                        argsToParamsOpt: argumentsToParametersOpt,
                        invokedAsExtensionMethod: invokedAsExtensionMethod); 
                });
        }
    }

    internal partial class BoundLocal : ILocalReferenceExpression
    {
        ILocalSymbol ILocalReferenceExpression.Local => this.LocalSymbol;
        
        protected override OperationKind ExpressionKind => OperationKind.LocalReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLocalReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLocalReferenceExpression(this, argument);
        }
    }

    internal partial class BoundFieldAccess : IFieldReferenceExpression
    {
        IOperation IMemberReferenceExpression.Instance => this.FieldSymbol.IsStatic ? null : this.ReceiverOpt;

        ISymbol IMemberReferenceExpression.Member => this.FieldSymbol;

        IFieldSymbol IFieldReferenceExpression.Field => this.FieldSymbol;

        protected override OperationKind ExpressionKind => OperationKind.FieldReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFieldReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFieldReferenceExpression(this, argument);
        }
    }

    internal partial class BoundPropertyAccess : IPropertyReferenceExpression
    {
        IPropertySymbol IPropertyReferenceExpression.Property => this.PropertySymbol;
       
        IOperation IMemberReferenceExpression.Instance => this.PropertySymbol.IsStatic ? null : this.ReceiverOpt;

        ISymbol IMemberReferenceExpression.Member => this.PropertySymbol;

        protected override OperationKind ExpressionKind => OperationKind.PropertyReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyReferenceExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyReferenceExpression(this, argument);
        }
    }

    internal partial class BoundIndexerAccess : IIndexedPropertyReferenceExpression
    {
        IPropertySymbol IPropertyReferenceExpression.Property => this.Indexer;

        IOperation IMemberReferenceExpression.Instance => this.Indexer.IsStatic ? null : this.ReceiverOpt;

        ISymbol IMemberReferenceExpression.Member => this.Indexer;

        ImmutableArray<IArgument> IHasArgumentsExpression.ArgumentsInEvaluationOrder
        {
            get
            {
                MethodSymbol accessor = this.UseSetterForDefaultArgumentGeneration 
                    ? this.Indexer.GetOwnOrInheritedSetMethod() 
                    : this.Indexer.GetOwnOrInheritedGetMethod(); 

                return BoundCall.DeriveArguments(this, 
                    this.BinderOpt, 
                    this.Indexer, 
                    accessor, 
                    this.Arguments, 
                    this.ArgumentNamesOpt, 
                    this.ArgsToParamsOpt, 
                    this.ArgumentRefKindsOpt, 
                    this.Indexer.Parameters, 
                    this.Expanded, 
                    this.Syntax);
            }
        }

        bool IOperation.IsInvalid
             => this.HasErrors 
            || (this.Indexer.IsReadOnly && this.UseSetterForDefaultArgumentGeneration) 
            || (this.Indexer.IsWriteOnly && !this.UseSetterForDefaultArgumentGeneration);

        protected override OperationKind ExpressionKind => OperationKind.IndexedPropertyReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIndexedPropertyReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIndexedPropertyReferenceExpression(this, argument);
        }
    }

    internal partial class BoundEventAccess : IEventReferenceExpression
    {
        IEventSymbol IEventReferenceExpression.Event => this.EventSymbol;

        IOperation IMemberReferenceExpression.Instance => this.EventSymbol.IsStatic ? null : this.ReceiverOpt;

        ISymbol IMemberReferenceExpression.Member => this.EventSymbol;

        protected override OperationKind ExpressionKind => OperationKind.EventReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEventReferenceExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitEventReferenceExpression(this, argument);
        }
    }

    internal partial class BoundEventAssignmentOperator : IEventAssignmentExpression
    {
        IEventSymbol IEventAssignmentExpression.Event => this.Event;

        IOperation IEventAssignmentExpression.EventInstance => this.Event.IsStatic ? null : this.ReceiverOpt;

        IOperation IEventAssignmentExpression.HandlerValue => this.Argument;

        bool IEventAssignmentExpression.Adds => this.IsAddition;

        protected override OperationKind ExpressionKind => OperationKind.EventAssignmentExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEventAssignmentExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitEventAssignmentExpression(this, argument);
        }
    }

    internal partial class BoundDelegateCreationExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Argument); 

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundParameter : IParameterReferenceExpression
    {
        IParameterSymbol IParameterReferenceExpression.Parameter => this.ParameterSymbol;

        protected override OperationKind ExpressionKind => OperationKind.ParameterReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitParameterReferenceExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitParameterReferenceExpression(this, argument);
        }
    }

    internal partial class BoundLiteral : ILiteralExpression
    {
        string ILiteralExpression.Text => this.Syntax.ToString();

        protected override OperationKind ExpressionKind => OperationKind.LiteralExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLiteralExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitLiteralExpression(this, argument);
        }
    }

    internal partial class BoundTupleExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => this.Arguments.As<IOperation>();

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundObjectCreationExpression : IObjectCreationExpression
    {
        private static readonly ConditionalWeakTable<BoundObjectCreationExpression, object> s_memberInitializersMappings =
            new ConditionalWeakTable<BoundObjectCreationExpression, object>();

        IMethodSymbol IObjectCreationExpression.Constructor => this.Constructor;

        ImmutableArray<IArgument> IHasArgumentsExpression.ArgumentsInEvaluationOrder 
            => BoundCall.DeriveArguments(this, 
                this.BinderOpt,
                this.Constructor, 
                this.Constructor, 
                this.Arguments, 
                this.ArgumentNamesOpt,
                this.ArgsToParamsOpt, 
                this.ArgumentRefKindsOpt, 
                this.Constructor.Parameters, 
                this.Expanded, 
                this.Syntax);

        ImmutableArray<IOperation> IObjectCreationExpression.Initializers => GetChildInitializers(this.InitializerExpressionOpt).As<IOperation>();

        internal static ImmutableArray<BoundExpression> GetChildInitializers(BoundExpression objectOrCollectionInitializer)
        {
            var objectInitializerExpression = objectOrCollectionInitializer as BoundObjectInitializerExpression;
            if (objectInitializerExpression != null)
            {
                return objectInitializerExpression.Initializers;
            }

            var collectionInitializerExpresion = objectOrCollectionInitializer as BoundCollectionInitializerExpression;
            if (collectionInitializerExpresion != null)
            {
                return collectionInitializerExpresion.Initializers;
            }

            return ImmutableArray<BoundExpression>.Empty;
        }

        protected override OperationKind ExpressionKind => OperationKind.ObjectCreationExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitObjectCreationExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitObjectCreationExpression(this, argument);
        }

        private sealed class FieldInitializer : IFieldInitializer
        {
            public FieldInitializer(SyntaxNode syntax, IFieldSymbol initializedField, IOperation value)
            {
                this.Syntax = syntax;
                this.InitializedField = initializedField;
                this.Value = value;
            }

            public IFieldSymbol InitializedField { get; }

            public ImmutableArray<IFieldSymbol> InitializedFields => ImmutableArray.Create(this.InitializedField);

            public IOperation Value { get; }

            OperationKind IOperation.Kind => OperationKind.FieldInitializerInCreation;

            public SyntaxNode Syntax { get; }

            bool IOperation.IsInvalid => this.Value.IsInvalid || (object)this.InitializedField == null;

            public ITypeSymbol Type => null;

            public Optional<object> ConstantValue => default(Optional<object>);

            void IOperation.Accept(OperationVisitor visitor)
            {
                visitor.VisitFieldInitializer(this);
            }

            TResult IOperation.Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            {
                return visitor.VisitFieldInitializer(this, argument);
            }
        }

        private sealed class PropertyInitializer : IPropertyInitializer
        {
            public PropertyInitializer(SyntaxNode syntax, IPropertySymbol initializedProperty, IOperation value)
            {
                this.Syntax = syntax;
                this.InitializedProperty = initializedProperty;
                this.Value = value;
            }

            public IPropertySymbol InitializedProperty { get; }

            public IOperation Value { get; }

            OperationKind IOperation.Kind => OperationKind.PropertyInitializerInCreation;

            public SyntaxNode Syntax { get; }

            bool IOperation.IsInvalid => this.Value.IsInvalid || (object)this.InitializedProperty == null;

            public ITypeSymbol Type => null;

            public Optional<object> ConstantValue => default(Optional<object>);

            void IOperation.Accept(OperationVisitor visitor)
            {
                visitor.VisitPropertyInitializer(this);
            }

            TResult IOperation.Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            {
                return visitor.VisitPropertyInitializer(this, argument);
            }
        }
    }

    internal partial class UnboundLambda : IUnboundLambdaExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.UnboundLambdaExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitUnboundLambdaExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitUnboundLambdaExpression(this, argument);
        }
    }

    internal partial class BoundLambda : ILambdaExpression
    {
        IMethodSymbol ILambdaExpression.Signature => this.Symbol;

        IBlockStatement ILambdaExpression.Body => this.Body;

        protected override OperationKind ExpressionKind => OperationKind.LambdaExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLambdaExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLambdaExpression(this, argument);
        }
    }

    internal partial class BoundConversion : IConversionExpression, IMethodBindingExpression
    {
        IOperation IConversionExpression.Operand => this.Operand;

        Semantics.ConversionKind IConversionExpression.ConversionKind
        {
            get
            {
                switch (this.ConversionKind)
                {
                    case CSharp.ConversionKind.ExplicitUserDefined:
                    case CSharp.ConversionKind.ImplicitUserDefined:
                        return Semantics.ConversionKind.OperatorMethod;

                    case CSharp.ConversionKind.ExplicitReference:
                    case CSharp.ConversionKind.ImplicitReference:
                    case CSharp.ConversionKind.Boxing:
                    case CSharp.ConversionKind.Unboxing:
                    case CSharp.ConversionKind.Identity:
                        return Semantics.ConversionKind.Cast;

                    case CSharp.ConversionKind.AnonymousFunction:
                    case CSharp.ConversionKind.ExplicitDynamic:
                    case CSharp.ConversionKind.ImplicitDynamic:
                    case CSharp.ConversionKind.ExplicitEnumeration:
                    case CSharp.ConversionKind.ImplicitEnumeration:
                    case CSharp.ConversionKind.ImplicitThrow:
                    case CSharp.ConversionKind.ImplicitTupleLiteral:
                    case CSharp.ConversionKind.ImplicitTuple:
                    case CSharp.ConversionKind.ExplicitTupleLiteral:
                    case CSharp.ConversionKind.ExplicitTuple:
                    case CSharp.ConversionKind.ExplicitNullable:
                    case CSharp.ConversionKind.ImplicitNullable:
                    case CSharp.ConversionKind.ExplicitNumeric:
                    case CSharp.ConversionKind.ImplicitNumeric:
                    case CSharp.ConversionKind.ImplicitConstant:
                    case CSharp.ConversionKind.IntegerToPointer:
                    case CSharp.ConversionKind.IntPtr:
                    case CSharp.ConversionKind.DefaultOrNullLiteral:
                    case CSharp.ConversionKind.NullToPointer:
                    case CSharp.ConversionKind.PointerToInteger:
                    case CSharp.ConversionKind.PointerToPointer:
                    case CSharp.ConversionKind.PointerToVoid:
                        return Semantics.ConversionKind.CSharp;

                    default:
                        return Semantics.ConversionKind.Invalid;
                }
            }
        }

        bool IConversionExpression.IsExplicit => this.ExplicitCastInCode;

        IMethodSymbol IHasOperatorMethodExpression.OperatorMethod => this.SymbolOpt;

        bool IHasOperatorMethodExpression.UsesOperatorMethod => this.ConversionKind == CSharp.ConversionKind.ExplicitUserDefined || this.ConversionKind == CSharp.ConversionKind.ImplicitUserDefined;

        // Consider introducing a different bound node type for method group conversions. These aren't truly conversions, but represent selection of a particular method.
        protected override OperationKind ExpressionKind => this.ConversionKind == ConversionKind.MethodGroup ? OperationKind.MethodBindingExpression : OperationKind.ConversionExpression;

        IMethodSymbol IMethodBindingExpression.Method => this.ConversionKind == ConversionKind.MethodGroup ? this.SymbolOpt as IMethodSymbol : null;

        bool IMethodBindingExpression.IsVirtual
        {
            get
            {
                var method = this.SymbolOpt;
                return (object)method != null &&
                    (method.IsAbstract || method.IsOverride || method.IsVirtual) &&
                    !this.SuppressVirtualCalls;
            }
        }

        IOperation IMemberReferenceExpression.Instance
        {
            get
            {
                if (this.ConversionKind == ConversionKind.MethodGroup)
                {
                    BoundMethodGroup methodGroup = this.Operand as BoundMethodGroup;
                    if (methodGroup != null)
                    {
                        return methodGroup.InstanceOpt;
                    }
                }

                return null;
            }
        }

        ISymbol IMemberReferenceExpression.Member => ((IMethodBindingExpression)this).Method;

        public override void Accept(OperationVisitor visitor)
        {
            if (this.ExpressionKind == OperationKind.MethodBindingExpression)
            {
                visitor.VisitMethodBindingExpression(this);
    }
            else
            {
                visitor.VisitConversionExpression(this);
            }
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return this.ExpressionKind == OperationKind.MethodBindingExpression
                    ? visitor.VisitMethodBindingExpression(this, argument)
                    : visitor.VisitConversionExpression(this, argument);
        }
    }

    internal partial class BoundAsOperator : IConversionExpression
    {
        IOperation IConversionExpression.Operand => this.Operand;

        Semantics.ConversionKind IConversionExpression.ConversionKind => Semantics.ConversionKind.TryCast;

        bool IConversionExpression.IsExplicit => true;

        IMethodSymbol IHasOperatorMethodExpression.OperatorMethod => null;

        bool IHasOperatorMethodExpression.UsesOperatorMethod => false;

        protected override OperationKind ExpressionKind => OperationKind.ConversionExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConversionExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitConversionExpression(this, argument);
        }
    }

    internal partial class BoundIsOperator : IIsTypeExpression
    {
        IOperation IIsTypeExpression.Operand => this.Operand;

        ITypeSymbol IIsTypeExpression.IsType => this.TargetType.Type;

        protected override OperationKind ExpressionKind => OperationKind.IsTypeExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIsTypeExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIsTypeExpression(this, argument);
        }
    }

    internal partial class BoundSizeOfOperator : ISizeOfExpression
    {
        ITypeSymbol ITypeOperationExpression.TypeOperand => this.SourceType.Type;

        protected override OperationKind ExpressionKind => OperationKind.SizeOfExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSizeOfExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitSizeOfExpression(this, argument);
        }
    }

    internal partial class BoundTypeOfOperator : ITypeOfExpression
    {
        ITypeSymbol ITypeOperationExpression.TypeOperand => this.SourceType.Type;

        protected override OperationKind ExpressionKind => OperationKind.TypeOfExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTypeOfExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitTypeOfExpression(this, argument);
        }
    }

    internal partial class BoundArrayCreation : IArrayCreationExpression
    {
        ITypeSymbol IArrayCreationExpression.ElementType
        {
            get
            {
                IArrayTypeSymbol arrayType = this.Type as IArrayTypeSymbol;
                if ((object)arrayType != null)
                {
                    return arrayType.ElementType;
                }

                return null;
            }
        }

        ImmutableArray<IOperation> IArrayCreationExpression.DimensionSizes => this.Bounds.As<IOperation>();

        IArrayInitializer IArrayCreationExpression.Initializer => this.InitializerOpt;

        protected override OperationKind ExpressionKind => OperationKind.ArrayCreationExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayCreationExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitArrayCreationExpression(this, argument);
        }
    }

    internal partial class BoundArrayInitialization : IArrayInitializer
    {
        public ImmutableArray<IOperation> ElementValues => this.Initializers.As<IOperation>();

        protected override OperationKind ExpressionKind => OperationKind.ArrayInitializer;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayInitializer(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitArrayInitializer(this, argument);
        }
    }

    internal partial class BoundDefaultExpression : IDefaultValueExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.DefaultValueExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDefaultValueExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDefaultValueExpression(this, argument);
        }
    }

    internal partial class BoundDup
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundBaseReference : IInstanceReferenceExpression
    {
        InstanceReferenceKind IInstanceReferenceExpression.InstanceReferenceKind => InstanceReferenceKind.BaseClass;

        protected override OperationKind ExpressionKind => OperationKind.InstanceReferenceExpression;
        
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInstanceReferenceExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitInstanceReferenceExpression(this, argument);
        }
    }

    internal partial class BoundThisReference : IInstanceReferenceExpression
    {
        InstanceReferenceKind IInstanceReferenceExpression.InstanceReferenceKind => this.Syntax.Kind() == SyntaxKind.ThisExpression ? InstanceReferenceKind.Explicit : InstanceReferenceKind.Implicit;

        protected override OperationKind ExpressionKind => OperationKind.InstanceReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInstanceReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInstanceReferenceExpression(this, argument);
        }
    }

    internal partial class BoundAssignmentOperator : IAssignmentExpression
    {
        IOperation IAssignmentExpression.Target => this.Left;

        IOperation IAssignmentExpression.Value => this.Right;

        protected override OperationKind ExpressionKind => OperationKind.AssignmentExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAssignmentExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAssignmentExpression(this, argument);
        }
    }

    internal sealed partial class BoundDeconstructionAssignmentOperator : BoundExpression
    {
        // TODO: implement IOperation for pattern-matching constructs (https://github.com/dotnet/roslyn/issues/8699)
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Left, this.Right);

        public override void Accept(OperationVisitor visitor)
        {
            // TODO: implement IOperation for pattern-matching constructs (https://github.com/dotnet/roslyn/issues/8699)
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            // TODO: implement IOperation for pattern-matching constructs (https://github.com/dotnet/roslyn/issues/8699)
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundCompoundAssignmentOperator : ICompoundAssignmentExpression
    {
        BinaryOperationKind ICompoundAssignmentExpression.BinaryOperationKind => Expression.DeriveBinaryOperationKind(this.Operator.Kind);

        IOperation IAssignmentExpression.Target => this.Left;

        IOperation IAssignmentExpression.Value => this.Right;

        bool IHasOperatorMethodExpression.UsesOperatorMethod => (this.Operator.Kind & BinaryOperatorKind.TypeMask) == BinaryOperatorKind.UserDefined;

        IMethodSymbol IHasOperatorMethodExpression.OperatorMethod => this.Operator.Method;

        protected override OperationKind ExpressionKind => OperationKind.CompoundAssignmentExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCompoundAssignmentExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCompoundAssignmentExpression(this, argument);
        }
    }

    internal partial class BoundIncrementOperator : IIncrementExpression
    {
        UnaryOperationKind IIncrementExpression.IncrementOperationKind => Expression.DeriveUnaryOperationKind(this.OperatorKind);

        BinaryOperationKind ICompoundAssignmentExpression.BinaryOperationKind => Expression.DeriveBinaryOperationKind(((IIncrementExpression)this).IncrementOperationKind);

        IOperation IAssignmentExpression.Target => this.Operand;

        private static readonly ConditionalWeakTable<BoundIncrementOperator, IOperation> s_incrementValueMappings = new ConditionalWeakTable<BoundIncrementOperator, IOperation>();

        IOperation IAssignmentExpression.Value => s_incrementValueMappings.GetValue(this, (increment) => new BoundLiteral(this.Syntax, Semantics.Expression.SynthesizeNumeric(increment.Type, 1), increment.Type));

        bool IHasOperatorMethodExpression.UsesOperatorMethod => (this.OperatorKind & UnaryOperatorKind.TypeMask) == UnaryOperatorKind.UserDefined;

        IMethodSymbol IHasOperatorMethodExpression.OperatorMethod => this.MethodOpt;

        protected override OperationKind ExpressionKind => OperationKind.IncrementExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIncrementExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIncrementExpression(this, argument);
    }
    }

    internal partial class BoundBadExpression : IInvalidExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.InvalidExpression;

        ImmutableArray<IOperation> IInvalidExpression.Children => StaticCast<IOperation>.From(ChildBoundNodes);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvalidExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitInvalidExpression(this, argument);
        }
    }

    internal partial class BoundNewT : ITypeParameterObjectCreationExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.TypeParameterObjectCreationExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTypeParameterObjectCreationExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitTypeParameterObjectCreationExpression(this, argument);
        }
    }

    internal partial class BoundUnaryOperator : IUnaryOperatorExpression
    {
        UnaryOperationKind IUnaryOperatorExpression.UnaryOperationKind => Expression.DeriveUnaryOperationKind(this.OperatorKind);

        IOperation IUnaryOperatorExpression.Operand => this.Operand;

        bool IHasOperatorMethodExpression.UsesOperatorMethod => (this.OperatorKind & UnaryOperatorKind.TypeMask) == UnaryOperatorKind.UserDefined;

        IMethodSymbol IHasOperatorMethodExpression.OperatorMethod => this.MethodOpt;

        protected override OperationKind ExpressionKind => OperationKind.UnaryOperatorExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitUnaryOperatorExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitUnaryOperatorExpression(this, argument);
        }
    }

    internal partial class BoundBinaryOperator : IBinaryOperatorExpression
    {
        BinaryOperationKind IBinaryOperatorExpression.BinaryOperationKind => Expression.DeriveBinaryOperationKind(this.OperatorKind);

        IOperation IBinaryOperatorExpression.LeftOperand => this.Left;

        IOperation IBinaryOperatorExpression.RightOperand => this.Right;

        bool IHasOperatorMethodExpression.UsesOperatorMethod => (this.OperatorKind & BinaryOperatorKind.TypeMask) == BinaryOperatorKind.UserDefined;

        IMethodSymbol IHasOperatorMethodExpression.OperatorMethod => this.MethodOpt;

        protected override OperationKind ExpressionKind
        {
            get
            {
                switch (this.OperatorKind & BinaryOperatorKind.OpMask)
                {
                    case BinaryOperatorKind.Addition:
                    case BinaryOperatorKind.Subtraction:
                    case BinaryOperatorKind.Multiplication:
                    case BinaryOperatorKind.Division:
                    case BinaryOperatorKind.Remainder:
                    case BinaryOperatorKind.LeftShift:
                    case BinaryOperatorKind.RightShift:
                    case BinaryOperatorKind.And:
                    case BinaryOperatorKind.Or:
                    case BinaryOperatorKind.Xor:
                    case BinaryOperatorKind.LessThan:
                    case BinaryOperatorKind.LessThanOrEqual:
                    case BinaryOperatorKind.Equal:
                    case BinaryOperatorKind.NotEqual:
                    case BinaryOperatorKind.GreaterThan:
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return OperationKind.BinaryOperatorExpression;

                    default:
                        return OperationKind.InvalidExpression;
                }
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitBinaryOperatorExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitBinaryOperatorExpression(this, argument);
        }
    }

    internal partial class BoundConditionalOperator : IConditionalChoiceExpression
    {
        IOperation IConditionalChoiceExpression.Condition => this.Condition;

        IOperation IConditionalChoiceExpression.IfTrueValue => this.Consequence;

        IOperation IConditionalChoiceExpression.IfFalseValue => this.Alternative;

        protected override OperationKind ExpressionKind => OperationKind.ConditionalChoiceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalChoiceExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitConditionalChoiceExpression(this, argument);
        }
    }

    internal partial class BoundNullCoalescingOperator : INullCoalescingExpression
    {
        IOperation INullCoalescingExpression.PrimaryOperand => this.LeftOperand;

        IOperation INullCoalescingExpression.SecondaryOperand => this.RightOperand;

        protected override OperationKind ExpressionKind => OperationKind.NullCoalescingExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNullCoalescingExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitNullCoalescingExpression(this, argument);
        }
    }

    internal partial class BoundAwaitExpression : IAwaitExpression
    {
        IOperation IAwaitExpression.AwaitedValue => this.Expression;

        protected override OperationKind ExpressionKind => OperationKind.AwaitExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAwaitExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAwaitExpression(this, argument);
        }
    }

    internal partial class BoundArrayAccess : IArrayElementReferenceExpression
    {
        IOperation IArrayElementReferenceExpression.ArrayReference => this.Expression;

        ImmutableArray<IOperation> IArrayElementReferenceExpression.Indices => this.Indices.As<IOperation>();
        
        protected override OperationKind ExpressionKind => OperationKind.ArrayElementReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayElementReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayElementReferenceExpression(this, argument);
        }
    }

    internal partial class BoundSuppressNullableWarningExpression : ISuppressNullableWarningExpression
    {
        IOperation ISuppressNullableWarningExpression.Expression => this.Expression;

        protected override OperationKind ExpressionKind => OperationKind.SuppressNullableWarningExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSuppressNullableWarningExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSuppressNullableWarningExpression(this, argument);
        }
    }

    internal partial class BoundPointerIndirectionOperator : IPointerIndirectionReferenceExpression
    {
        IOperation IPointerIndirectionReferenceExpression.Pointer => this.Operand;
        
        protected override OperationKind ExpressionKind => OperationKind.PointerIndirectionReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPointerIndirectionReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPointerIndirectionReferenceExpression(this, argument);
        }
    }

    internal partial class BoundAddressOfOperator : IAddressOfExpression
    {
        IOperation IAddressOfExpression.Reference => this.Operand;

        protected override OperationKind ExpressionKind => OperationKind.AddressOfExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAddressOfExpression(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
    {
            return visitor.VisitAddressOfExpression(this, argument);
        }
    }

    internal partial class BoundImplicitReceiver : IInstanceReferenceExpression
    {
        InstanceReferenceKind IInstanceReferenceExpression.InstanceReferenceKind => InstanceReferenceKind.Implicit;

        protected override OperationKind ExpressionKind => OperationKind.InstanceReferenceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInstanceReferenceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInstanceReferenceExpression(this, argument);
        }
    }

    internal partial class BoundConditionalAccess : IConditionalAccessExpression
    {
        IOperation IConditionalAccessExpression.ConditionalValue => this.AccessExpression;

        IOperation IConditionalAccessExpression.ConditionalInstance => this.Receiver;

        protected override OperationKind ExpressionKind => OperationKind.ConditionalAccessExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalAccessExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalAccessExpression(this, argument);
        }
    }

    internal partial class BoundConditionalReceiver : IConditionalAccessInstanceExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.ConditionalAccessInstanceExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalAccessInstanceExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalAccessInstanceExpression(this, argument);
        }
    }

    internal partial class BoundEqualsValue : ISymbolInitializer
    {
        IOperation ISymbolInitializer.Value => this.Value;

        bool IOperation.IsInvalid => ((IOperation)this.Value).IsInvalid;

        public override abstract void Accept(OperationVisitor visitor);

        public override abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
    }

    internal partial class BoundFieldEqualsValue : IFieldInitializer
    {
        ImmutableArray<IFieldSymbol> IFieldInitializer.InitializedFields => ImmutableArray.Create<IFieldSymbol>(this.Field);

        protected override OperationKind OperationKind => OperationKind.FieldInitializerAtDeclaration;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFieldInitializer(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFieldInitializer(this, argument);
        }
    }

    internal partial class BoundPropertyEqualsValue : IPropertyInitializer
    {
        IPropertySymbol IPropertyInitializer.InitializedProperty => this.Property;

        protected override OperationKind OperationKind => OperationKind.PropertyInitializerAtDeclaration;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyInitializer(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyInitializer(this, argument);
        }
    }

    internal partial class BoundParameterEqualsValue : IParameterInitializer
    {
        IParameterSymbol IParameterInitializer.Parameter => this.Parameter;

        protected override OperationKind OperationKind => OperationKind.ParameterInitializerAtDeclaration;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitParameterInitializer(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParameterInitializer(this, argument);
        }
    }

    internal partial class BoundDynamicIndexerAccess
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => this.Arguments.Insert(0, this.ReceiverOpt).As<IOperation>();

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundUserDefinedConditionalLogicalOperator
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Left, this.Right);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundAnonymousObjectCreationExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => this.Arguments.As<IOperation>();

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundAnonymousPropertyDeclaration
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundAttribute
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => this.ConstructorArguments.AddRange(this.NamedArguments).As<IOperation>();

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundRangeVariable
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundLabel
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundObjectInitializerMember
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundQueryClause
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Value);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundPattern
    {
        protected override abstract ImmutableArray<IOperation> Children { get; }
    }

    internal partial class BoundDeclarationPattern
    {
        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.VariableAccess);
    }

    internal partial class BoundConstantPattern
    {
        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Value);
    }

    internal partial class BoundWildcardPattern
    {
        protected override ImmutableArray<IOperation> Children => ImmutableArray<IOperation>.Empty;
    }

    internal partial class BoundArgListOperator
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => Arguments.As<IOperation>();

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundPropertyGroup
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
    }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundCollectionElementInitializer
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => this.Arguments.As<IOperation>();

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundNameOfOperator
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Argument);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundMethodGroup
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundTypeExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundNamespaceExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundSequencePointExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundSequence
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundPreviousSubmissionReference
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundHostObjectMemberReference
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundTypeOrValueExpression
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundPseudoVariable
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundPointerElementAccess
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Expression, this.Index);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundRefTypeOperator
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Operand);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundDynamicMemberAccess
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Receiver);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundMakeRefOperator
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Operand);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundRefValueOperator
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Operand);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundDynamicInvocation
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => this.Arguments.As<IOperation>().Insert(0, this.Expression);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundArrayLength
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundMethodDefIndex
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundModuleVersionId
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundModuleVersionIdString
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundInstrumentationPayloadRoot
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundMaximumMethodDefIndex
    {
        protected override OperationKind ExpressionKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        internal static ImmutableArray<BoundExpression> GetChildInitializers(BoundExpression objectOrCollectionInitializer)
        {
            var objectInitializerExpression = objectOrCollectionInitializer as BoundObjectInitializerExpression;
            if (objectInitializerExpression != null)
            {
                return objectInitializerExpression.Initializers;
            }

            var collectionInitializerExpresion = objectOrCollectionInitializer as BoundCollectionInitializerExpression;
            if (collectionInitializerExpresion != null)
            {
                return collectionInitializerExpresion.Initializers;
            }

            return ImmutableArray<BoundExpression>.Empty;
        }
    }

    internal sealed partial class BoundDeconstructionAssignmentOperator : BoundExpression
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Left, this.Right);
    }

    internal partial class BoundBadExpression
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.ChildBoundNodes);
    }

    internal partial class BoundDynamicIndexerAccess
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.Arguments.Insert(0, this.ReceiverOpt));
    }

    internal partial class BoundUserDefinedConditionalLogicalOperator
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Left, this.Right);
    }

    internal partial class BoundAnonymousObjectCreationExpression
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.Arguments);
    }

    internal partial class BoundAttribute
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.ConstructorArguments.AddRange(this.NamedArguments));
    }

    internal partial class BoundQueryClause
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Value);
    }

    internal partial class BoundArgListOperator
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.Arguments);
    }

    internal partial class BoundNameOfOperator
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Argument);
    }

    internal partial class BoundPointerElementAccess
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Expression, this.Index);
    }

    internal partial class BoundRefTypeOperator
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Operand);
    }

    internal partial class BoundDynamicMemberAccess
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Receiver);
    }

    internal partial class BoundMakeRefOperator
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Operand);
    }

    internal partial class BoundRefValueOperator
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Operand);
    }

    internal partial class BoundDynamicInvocation
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.Arguments.Insert(0, this.Expression));
    }

    internal partial class BoundFixedLocalCollectionInitializer
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Expression);
    }

    internal partial class BoundStackAllocArrayCreation
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Count);
    }

    internal partial class BoundConvertedStackAllocExpression
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Count);
    }

    internal partial class BoundDynamicObjectCreationExpression
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.Arguments.AddRange(BoundObjectCreationExpression.GetChildInitializers(this.InitializerExpressionOpt)));
    }

    internal partial class BoundNoPiaObjectCreationExpression
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(BoundObjectCreationExpression.GetChildInitializers(this.InitializerExpressionOpt));
    }

    partial class BoundThrowExpression
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Expression);
    }

    internal abstract partial class BoundMethodOrPropertyGroup
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.ReceiverOpt);
    }

    internal partial class BoundSequence
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.SideEffects.Add(this.Value));
    }

    internal partial class BoundStatementList
    {
        protected override ImmutableArray<BoundNode> Children => 
            (this.Kind == BoundKind.StatementList || this.Kind == BoundKind.Scope) ? StaticCast<BoundNode>.From(this.Statements) : ImmutableArray<BoundNode>.Empty;
    }
}
