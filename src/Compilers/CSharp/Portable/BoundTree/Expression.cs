﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Semantics;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundExpression : IOperation
    {
        ITypeSymbol IOperation.Type => this.Type;

        OperationKind IOperation.Kind => this.ExpressionKind;

        bool IOperation.IsInvalid => this.HasErrors;

        Optional<object> IOperation.ConstantValue
        {
            get
            {
                ConstantValue value = this.ConstantValue;
                return value != null ? new Optional<object>(value.Value) : default(Optional<object>);
            }
        }
        SyntaxNode IOperation.Syntax => this.Syntax;

        protected abstract OperationKind ExpressionKind { get; }

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
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

        ImmutableArray<IArgument> IInvocationExpression.ArgumentsInSourceOrder
        {
            get
            {
                ArrayBuilder<IArgument> sourceOrderArguments = ArrayBuilder<IArgument>.GetInstance(this.Arguments.Length);
                for (int argumentIndex = 0; argumentIndex < this.Arguments.Length; argumentIndex++)
                {
                    IArgument argument = DeriveArgument(this.ArgsToParamsOpt.IsDefault ? argumentIndex : this.ArgsToParamsOpt[argumentIndex], argumentIndex, this.Arguments, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, this.Method.Parameters, this.Syntax);
                    sourceOrderArguments.Add(argument);
                    if (argument.ArgumentKind == ArgumentKind.ParamArray)
                    {
                        break;
                    }
                }

                return sourceOrderArguments.ToImmutableAndFree();
            }
        }

        ImmutableArray<IArgument> IHasArgumentsExpression.ArgumentsInParameterOrder => DeriveArguments(this.Arguments, this.ArgumentNamesOpt, this.ArgsToParamsOpt, this.ArgumentRefKindsOpt, this.Method.Parameters, this.Syntax);

        IArgument IHasArgumentsExpression.GetArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return ArgumentMatchingParameter(this.Arguments, this.ArgsToParamsOpt, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, parameter.ContainingSymbol, ((Symbols.MethodSymbol)parameter.ContainingSymbol).Parameters, parameter, this.Syntax);
        }

        protected override OperationKind ExpressionKind => OperationKind.InvocationExpression;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvocationExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvocationExpression(this, argument);
        }

        internal static ImmutableArray<IArgument> DeriveArguments(ImmutableArray<BoundExpression> boundArguments, ImmutableArray<string> argumentNames, ImmutableArray<int> argumentsToParameters, ImmutableArray<RefKind> argumentRefKinds, ImmutableArray<Symbols.ParameterSymbol> parameters, SyntaxNode invocationSyntax)
        {
            ArrayBuilder<IArgument> arguments = ArrayBuilder<IArgument>.GetInstance(boundArguments.Length);
            for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                int argumentIndex = -1;
                if (argumentsToParameters.IsDefault)
                {
                    argumentIndex = parameterIndex;
                }
                else
                {
                    argumentIndex = argumentsToParameters.IndexOf(parameterIndex);
                }

                if ((uint)argumentIndex >= (uint)boundArguments.Length)
                {
                    // No argument has been supplied for the parameter at `parameterIndex`:
                    // 1. `argumentIndex == -1' when the arguments are specified out of parameter order, and no argument is provided for parameter corresponding to `parameters[parameterIndex]`.
                    // 2. `argumentIndex >= boundArguments.Length` when the arguments are specified in parameter order, and no argument is provided at `parameterIndex`.

                    Symbols.ParameterSymbol parameter = parameters[parameterIndex];
                    if (parameter.HasExplicitDefaultValue)
                    {
                        // The parameter is optional with a default value.
                        arguments.Add(new Argument(ArgumentKind.DefaultValue, parameter, new Literal(parameter.ExplicitDefaultConstantValue, parameter.Type, invocationSyntax)));
                    }
                    else
                    {
                        // If the invocation is semantically valid, the parameter will be a params array and an empty array will be provided.
                        // If the argument is otherwise omitted for a parameter with no default value, the invocation is not valid and a null argument will be provided.
                        arguments.Add(DeriveArgument(parameterIndex, boundArguments.Length, boundArguments, argumentNames, argumentRefKinds, parameters, invocationSyntax));
                    }
                }
                else
                {
                    arguments.Add(DeriveArgument(parameterIndex, argumentIndex, boundArguments, argumentNames, argumentRefKinds, parameters, invocationSyntax));
                }
            }

            return arguments.ToImmutableAndFree();
        }

        private static readonly ConditionalWeakTable<BoundExpression, IArgument> s_argumentMappings = new ConditionalWeakTable<BoundExpression, IArgument>();

        private static IArgument DeriveArgument(int parameterIndex, int argumentIndex, ImmutableArray<BoundExpression> boundArguments, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, ImmutableArray<Symbols.ParameterSymbol> parameters, SyntaxNode invocationSyntax)
        {
            if ((uint)argumentIndex >= (uint)boundArguments.Length)
            {
                // Check for an omitted argument that becomes an empty params array.
                if (parameters.Length > 0)
                {
                    Symbols.ParameterSymbol lastParameter = parameters[parameters.Length - 1];
                    if (lastParameter.IsParams)
                    {
                        return new Argument(ArgumentKind.ParamArray, lastParameter, CreateParamArray(lastParameter, boundArguments, argumentIndex, invocationSyntax));
                    }
                }

                // There is no supplied argument and there is no params parameter. Any action is suspect at this point.
                return new SimpleArgument(null, new InvalidExpression(invocationSyntax));
            }

            return s_argumentMappings.GetValue(
                boundArguments[argumentIndex],
                (argument) =>
                {
                    string name = !argumentNames.IsDefaultOrEmpty ? argumentNames[argumentIndex] : null;
                    Symbols.ParameterSymbol parameter = (uint)parameterIndex < (uint)parameters.Length ? parameters[parameterIndex] : null;

                    if ((object)name == null)
                    {
                        RefKind refMode = argumentRefKinds.IsDefaultOrEmpty ? RefKind.None : argumentRefKinds[argumentIndex];

                        if (refMode != RefKind.None)
                        {
                            return new Argument(ArgumentKind.Positional, parameter, argument);
                        }

                        if (argumentIndex >= parameters.Length - 1 &&
                            parameters.Length > 0 &&
                            parameters[parameters.Length - 1].IsParams &&
                            // An argument that is an array of the appropriate type is not a params argument.
                            (boundArguments.Length > argumentIndex + 1 ||
                             argument.Type.TypeKind != TypeKind.Array ||
                             !argument.Type.Equals(parameters[parameters.Length - 1].Type, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds)))
                        {
                            return new Argument(ArgumentKind.ParamArray, parameters[parameters.Length - 1], CreateParamArray(parameters[parameters.Length - 1], boundArguments, argumentIndex, invocationSyntax));
                        }
                        else
                        {
                            return new SimpleArgument(parameter, argument);
                        }
                    }

                    return new Argument(ArgumentKind.Named, parameter, argument);
                });
        }

        private static IOperation CreateParamArray(IParameterSymbol parameter, ImmutableArray<BoundExpression> boundArguments, int firstArgumentElementIndex, SyntaxNode invocationSyntax)
        {
            if (parameter.Type.TypeKind == TypeKind.Array)
            {
                IArrayTypeSymbol arrayType = (IArrayTypeSymbol)parameter.Type;
                ArrayBuilder<IOperation> builder = ArrayBuilder<IOperation>.GetInstance(boundArguments.Length - firstArgumentElementIndex);

                for (int index = firstArgumentElementIndex; index < boundArguments.Length; index++)
                {
                    builder.Add(boundArguments[index]);
                }
                var paramArrayArguments = builder.ToImmutableAndFree();

                // Use the invocation syntax node if there is no actual syntax available for the argument (because the paramarray is empty.)
                return new ArrayCreation(arrayType, paramArrayArguments, paramArrayArguments.Length > 0 ? paramArrayArguments[0].Syntax : invocationSyntax);
            }

            return new InvalidExpression(invocationSyntax);
        }

        internal static IArgument ArgumentMatchingParameter(ImmutableArray<BoundExpression> arguments, ImmutableArray<int> argumentsToParameters, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, ISymbol targetMethod, ImmutableArray<Symbols.ParameterSymbol> parameters, IParameterSymbol parameter, SyntaxNode invocationSyntax)
        {
            int argumentIndex = ArgumentIndexMatchingParameter(argumentsToParameters, targetMethod, parameter);
            if (argumentIndex >= 0)
            {
                return DeriveArgument(parameter.Ordinal, argumentIndex, arguments, argumentNames, argumentRefKinds, parameters, invocationSyntax);
            }

            return null;
        }

        private static int ArgumentIndexMatchingParameter(ImmutableArray<int> argumentsToParameters, ISymbol targetMethod, IParameterSymbol parameter)
        {
            if (parameter.ContainingSymbol == targetMethod)
            {
                int parameterIndex = parameter.Ordinal;
                ImmutableArray<int> parameterIndices = argumentsToParameters;
                if (!parameterIndices.IsDefaultOrEmpty)
                {
                    return parameterIndices.IndexOf(parameterIndex);
                }

                return parameterIndex;
            }

            return -1;
        }

        private abstract class ArgumentBase : IArgument
        {
            public ArgumentBase(IParameterSymbol parameter, IOperation value)
            {
                Debug.Assert(value != null);

                this.Value = value;
                this.Parameter = parameter;
            }

            public IParameterSymbol Parameter { get; }

            public IOperation Value { get; }

            IOperation IArgument.InConversion => null;

            IOperation IArgument.OutConversion => null;

            bool IOperation.IsInvalid => (object)this.Parameter == null || this.Value.IsInvalid;

            OperationKind IOperation.Kind => OperationKind.Argument;

            SyntaxNode IOperation.Syntax => this.Value?.Syntax;

            public ITypeSymbol Type => null;

            public Optional<object> ConstantValue => default(Optional<object>);

            public abstract ArgumentKind ArgumentKind { get; }

            void IOperation.Accept(OperationVisitor visitor)
            {
                visitor.VisitArgument(this);
            }

            TResult IOperation.Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            {
                return visitor.VisitArgument(this, argument);
            }
        }

        private sealed class SimpleArgument : ArgumentBase
        {
            public SimpleArgument(IParameterSymbol parameter, IOperation value)
                : base(parameter, value)
            { }

            public override ArgumentKind ArgumentKind => ArgumentKind.Positional;
        }

        private sealed class Argument : ArgumentBase
        {
            public Argument(ArgumentKind kind, IParameterSymbol parameter, IOperation value)
                : base(parameter, value)
            {
                this.ArgumentKind = kind;
            }

            public override ArgumentKind ArgumentKind { get; }
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

        ImmutableArray<IArgument> IHasArgumentsExpression.ArgumentsInParameterOrder => BoundCall.DeriveArguments(this.Arguments, this.ArgumentNamesOpt, this.ArgsToParamsOpt, this.ArgumentRefKindsOpt, this.Indexer.Parameters, this.Syntax);

        IArgument IHasArgumentsExpression.GetArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return BoundCall.ArgumentMatchingParameter(this.Arguments, this.ArgsToParamsOpt, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, this.Indexer, this.Indexer.Parameters, parameter, this.Syntax);
        }

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

    internal partial class BoundDelegateCreationExpression : IMethodBindingExpression
    {
        IOperation IMemberReferenceExpression.Instance
        {
            get
            {
                BoundMethodGroup methodGroup = this.Argument as BoundMethodGroup;
                if (methodGroup != null)
                {
                    return methodGroup.InstanceOpt;
                }

                return null;
            }
        }

        bool IMethodBindingExpression.IsVirtual =>
            (object)this.MethodOpt != null &&
            (this.MethodOpt.IsVirtual || this.MethodOpt.IsAbstract || this.MethodOpt.IsOverride) &&
            !this.SuppressVirtualCalls;

        ISymbol IMemberReferenceExpression.Member => this.MethodOpt;

        IMethodSymbol IMethodBindingExpression.Method => this.MethodOpt;

        protected override OperationKind ExpressionKind => OperationKind.MethodBindingExpression;

        // SyntaxNode for MethodBindingExpression is the argument of DelegateCreationExpression 
        SyntaxNode IOperation.Syntax => this.Argument.Syntax;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitMethodBindingExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitMethodBindingExpression(this, argument);
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

        ImmutableArray<IArgument> IHasArgumentsExpression.ArgumentsInParameterOrder => BoundCall.DeriveArguments(this.Arguments, this.ArgumentNamesOpt, this.ArgsToParamsOpt, this.ArgumentRefKindsOpt, this.Constructor.Parameters, this.Syntax);

        IArgument IHasArgumentsExpression.GetArgumentMatchingParameter(IParameterSymbol parameter)
        {
            return BoundCall.ArgumentMatchingParameter(this.Arguments, this.ArgsToParamsOpt, this.ArgumentNamesOpt, this.ArgumentRefKindsOpt, this.Constructor, this.Constructor.Parameters, parameter, this.Syntax);
        }

        ImmutableArray<ISymbolInitializer> IObjectCreationExpression.MemberInitializers
        {
            get
            {
                return (ImmutableArray<ISymbolInitializer>)s_memberInitializersMappings.GetValue(this,
                    objectCreationExpression =>
                    {
                        var objectInitializerExpression = this.InitializerExpressionOpt as BoundObjectInitializerExpression;
                        if (objectInitializerExpression != null)
                        {
                            var builder = ArrayBuilder<ISymbolInitializer>.GetInstance(objectInitializerExpression.Initializers.Length);
                            foreach (var memberAssignment in objectInitializerExpression.Initializers)
                            {
                                var assignment = memberAssignment as BoundAssignmentOperator;
                                var leftSymbol = (assignment?.Left as BoundObjectInitializerMember)?.MemberSymbol;

                                if ((object)leftSymbol == null)
                                {
                                    continue;
                                }

                                switch (leftSymbol.Kind)
                                {
                                    case SymbolKind.Field:
                                        builder.Add(new FieldInitializer(assignment.Syntax, (IFieldSymbol)leftSymbol, assignment.Right));
                                        break;
                                    case SymbolKind.Property:
                                        builder.Add(new PropertyInitializer(assignment.Syntax, (IPropertySymbol)leftSymbol, assignment.Right));
                                        break;
                                }
                            }
                            return builder.ToImmutableAndFree();
                        }
                        return ImmutableArray<ISymbolInitializer>.Empty;
                    });
            }
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
                    case CSharp.ConversionKind.NullLiteral:
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

        IMethodSymbol IMethodBindingExpression.Method => this.ConversionKind == ConversionKind.MethodGroup ? this.SymbolOpt : null;

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

    internal partial class BoundDefaultOperator : IDefaultValueExpression
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

    internal sealed partial class BoundLocalDeconstructionDeclaration : BoundStatement
    {
        protected override OperationKind StatementKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal sealed partial class BoundVoid : BoundExpression
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

        SyntaxNode IOperation.Syntax => this.Syntax;

        bool IOperation.IsInvalid => ((IOperation)this.Value).IsInvalid;

        OperationKind IOperation.Kind => this.OperationKind;

        protected abstract OperationKind OperationKind { get; }

        ITypeSymbol IOperation.Type => null;

        Optional<object> IOperation.ConstantValue => default(Optional<object>);

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
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

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundArgListOperator
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

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }
    
    internal partial class BoundSourceDocumentIndex
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

    internal partial class BoundMethodInfo
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

    internal partial class BoundCollectionInitializerExpression
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

    internal partial class BoundFieldInfo
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

    internal partial class BoundLoweredConditionalAccess
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

    internal partial class BoundArgList
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

    internal partial class BoundDynamicCollectionElementInitializer
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

    internal partial class BoundComplexConditionalReceiver
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

    internal partial class BoundFixedLocalCollectionInitializer
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

    internal partial class BoundStackAllocArrayCreation
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

    internal partial class BoundDynamicObjectCreationExpression
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

    internal partial class BoundHoistedFieldAccess
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

    internal partial class BoundInterpolatedString
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

    internal partial class BoundNoPiaObjectCreationExpression
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

    internal partial class BoundObjectInitializerExpression
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

    internal partial class BoundStringInsert
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

    internal partial class BoundDynamicObjectInitializerMember
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

    internal class Expression
    {
        internal static BinaryOperationKind DeriveBinaryOperationKind(UnaryOperationKind incrementKind)
        {
            switch (incrementKind)
            {
                case UnaryOperationKind.OperatorMethodPostfixIncrement:
                case UnaryOperationKind.OperatorMethodPrefixIncrement:
                    return BinaryOperationKind.OperatorMethodAdd;
                case UnaryOperationKind.OperatorMethodPostfixDecrement:
                case UnaryOperationKind.OperatorMethodPrefixDecrement:
                    return BinaryOperationKind.OperatorMethodSubtract;
                case UnaryOperationKind.IntegerPostfixIncrement:
                case UnaryOperationKind.IntegerPrefixIncrement:
                    return BinaryOperationKind.IntegerAdd;
                case UnaryOperationKind.IntegerPostfixDecrement:
                case UnaryOperationKind.IntegerPrefixDecrement:
                    return BinaryOperationKind.IntegerSubtract;
                case UnaryOperationKind.UnsignedPostfixIncrement:
                case UnaryOperationKind.UnsignedPrefixIncrement:
                    return BinaryOperationKind.UnsignedAdd;
                case UnaryOperationKind.UnsignedPostfixDecrement:
                case UnaryOperationKind.UnsignedPrefixDecrement:
                    return BinaryOperationKind.UnsignedSubtract;
                case UnaryOperationKind.FloatingPostfixIncrement:
                case UnaryOperationKind.FloatingPrefixIncrement:
                    return BinaryOperationKind.FloatingAdd;
                case UnaryOperationKind.FloatingPostfixDecrement:
                case UnaryOperationKind.FloatingPrefixDecrement:
                    return BinaryOperationKind.FloatingSubtract;
                case UnaryOperationKind.DecimalPostfixIncrement:
                case UnaryOperationKind.DecimalPrefixIncrement:
                    return BinaryOperationKind.DecimalAdd;
                case UnaryOperationKind.DecimalPostfixDecrement:
                case UnaryOperationKind.DecimalPrefixDecrement:
                    return BinaryOperationKind.DecimalSubtract;
                case UnaryOperationKind.EnumPostfixIncrement:
                case UnaryOperationKind.EnumPrefixIncrement:
                    return BinaryOperationKind.EnumAdd;
                case UnaryOperationKind.EnumPostfixDecrement:
                case UnaryOperationKind.EnumPrefixDecrement:
                    return BinaryOperationKind.EnumSubtract;
                case UnaryOperationKind.PointerPostfixIncrement:
                case UnaryOperationKind.PointerPrefixIncrement:
                    return BinaryOperationKind.PointerIntegerAdd;
                case UnaryOperationKind.PointerPostfixDecrement:
                case UnaryOperationKind.PointerPrefixDecrement:
                    return BinaryOperationKind.PointerIntegerSubtract;
                case UnaryOperationKind.DynamicPostfixIncrement:
                case UnaryOperationKind.DynamicPrefixIncrement:
                    return BinaryOperationKind.DynamicAdd;
                case UnaryOperationKind.DynamicPostfixDecrement:
                case UnaryOperationKind.DynamicPrefixDecrement:
                    return BinaryOperationKind.DynamicSubtract;

                default:
                    return BinaryOperationKind.Invalid;
            }
        }

        internal static UnaryOperationKind DeriveUnaryOperationKind(UnaryOperatorKind operatorKind)
        {
            switch (operatorKind & UnaryOperatorKind.OpMask)
            {
                case UnaryOperatorKind.PostfixIncrement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.SByte:
                        case UnaryOperatorKind.Short:
                            return UnaryOperationKind.IntegerPostfixIncrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return UnaryOperationKind.UnsignedPostfixIncrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperationKind.FloatingPostfixIncrement;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperationKind.DecimalPostfixIncrement;
                        case UnaryOperatorKind.Enum:
                            return UnaryOperationKind.EnumPostfixIncrement;
                        case UnaryOperatorKind.Pointer:
                            return UnaryOperationKind.PointerPostfixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicPostfixIncrement;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorMethodPostfixIncrement;
                    }

                    break;

                case UnaryOperatorKind.PostfixDecrement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.SByte:
                        case UnaryOperatorKind.Short:
                            return UnaryOperationKind.IntegerPostfixDecrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return UnaryOperationKind.UnsignedPostfixDecrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperationKind.FloatingPostfixDecrement;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperationKind.DecimalPostfixDecrement;
                        case UnaryOperatorKind.Enum:
                            return UnaryOperationKind.EnumPostfixDecrement;
                        case UnaryOperatorKind.Pointer:
                            return UnaryOperationKind.PointerPostfixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicPostfixDecrement;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorMethodPostfixDecrement;
                    }

                    break;

                case UnaryOperatorKind.PrefixIncrement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.SByte:
                        case UnaryOperatorKind.Short:
                            return UnaryOperationKind.IntegerPrefixIncrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return UnaryOperationKind.UnsignedPrefixIncrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperationKind.FloatingPrefixIncrement;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperationKind.DecimalPrefixIncrement;
                        case UnaryOperatorKind.Enum:
                            return UnaryOperationKind.EnumPrefixIncrement;
                        case UnaryOperatorKind.Pointer:
                            return UnaryOperationKind.PointerPrefixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicPrefixIncrement;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorMethodPrefixIncrement;
                    }

                    break;

                case UnaryOperatorKind.PrefixDecrement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.SByte:
                        case UnaryOperatorKind.Short:
                            return UnaryOperationKind.IntegerPrefixDecrement;
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.ULong:
                        case UnaryOperatorKind.Byte:
                        case UnaryOperatorKind.UShort:
                        case UnaryOperatorKind.Char:
                            return UnaryOperationKind.UnsignedPrefixDecrement;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperationKind.FloatingPrefixDecrement;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperationKind.DecimalPrefixDecrement;
                        case UnaryOperatorKind.Enum:
                            return UnaryOperationKind.EnumPrefixDecrement;
                        case UnaryOperatorKind.Pointer:
                            return UnaryOperationKind.PointerPrefixIncrement;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicPrefixDecrement;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorMethodPrefixDecrement;
                    }

                    break;

                case UnaryOperatorKind.UnaryPlus:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.ULong:
                            return UnaryOperationKind.IntegerPlus;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperationKind.FloatingPlus;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperationKind.DecimalPlus;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicPlus;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorMethodPlus;
                    }

                    break;

                case UnaryOperatorKind.UnaryMinus:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.ULong:
                            return UnaryOperationKind.IntegerMinus;
                        case UnaryOperatorKind.Float:
                        case UnaryOperatorKind.Double:
                            return UnaryOperationKind.FloatingMinus;
                        case UnaryOperatorKind.Decimal:
                            return UnaryOperationKind.DecimalMinus;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicMinus;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorMethodMinus;
                    }

                    break;

                case UnaryOperatorKind.LogicalNegation:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Bool:
                            return UnaryOperationKind.BooleanLogicalNot;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicLogicalNot;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorMethodLogicalNot;
                    }

                    break;
                case UnaryOperatorKind.BitwiseComplement:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Int:
                        case UnaryOperatorKind.UInt:
                        case UnaryOperatorKind.Long:
                        case UnaryOperatorKind.ULong:
                            return UnaryOperationKind.IntegerBitwiseNegation;
                        case UnaryOperatorKind.Bool:
                            return UnaryOperationKind.BooleanBitwiseNegation;
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicBitwiseNegation;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorMethodBitwiseNegation;
                    }

                    break;

                case UnaryOperatorKind.True:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicTrue;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorMethodTrue;
                    }

                    break;

                case UnaryOperatorKind.False:
                    switch (operatorKind & UnaryOperatorKind.TypeMask)
                    {
                        case UnaryOperatorKind.Dynamic:
                            return UnaryOperationKind.DynamicFalse;
                        case UnaryOperatorKind.UserDefined:
                            return UnaryOperationKind.OperatorMethodFalse;
                    }

                    break;
            }

            return UnaryOperationKind.Invalid;
        }

        internal static BinaryOperationKind DeriveBinaryOperationKind(BinaryOperatorKind operatorKind)
        {
            switch (operatorKind & BinaryOperatorKind.OpMask)
            {
                case BinaryOperatorKind.Addition:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerAdd;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedAdd;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return BinaryOperationKind.FloatingAdd;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperationKind.DecimalAdd;
                        case BinaryOperatorKind.EnumAndUnderlying:
                        case BinaryOperatorKind.UnderlyingAndEnum:
                            return BinaryOperationKind.EnumAdd;
                        case BinaryOperatorKind.PointerAndInt:
                        case BinaryOperatorKind.PointerAndUInt:
                        case BinaryOperatorKind.PointerAndLong:
                        case BinaryOperatorKind.PointerAndULong:
                            return BinaryOperationKind.PointerIntegerAdd;
                        case BinaryOperatorKind.IntAndPointer:
                        case BinaryOperatorKind.UIntAndPointer:
                        case BinaryOperatorKind.LongAndPointer:
                        case BinaryOperatorKind.ULongAndPointer:
                            return BinaryOperationKind.IntegerPointerAdd;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperationKind.DynamicAdd;
                        case BinaryOperatorKind.String:
                        case BinaryOperatorKind.StringAndObject:
                        case BinaryOperatorKind.ObjectAndString:
                            return BinaryOperationKind.StringConcatenate;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodAdd;
                    }

                    break;

                case BinaryOperatorKind.Subtraction:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerSubtract;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedSubtract;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return BinaryOperationKind.FloatingSubtract;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperationKind.DecimalSubtract;
                        case BinaryOperatorKind.EnumAndUnderlying:
                        case BinaryOperatorKind.UnderlyingAndEnum:
                            return BinaryOperationKind.EnumSubtract;
                        case BinaryOperatorKind.PointerAndInt:
                        case BinaryOperatorKind.PointerAndUInt:
                        case BinaryOperatorKind.PointerAndLong:
                        case BinaryOperatorKind.PointerAndULong:
                            return BinaryOperationKind.PointerIntegerSubtract;
                        case BinaryOperatorKind.Pointer:
                            return BinaryOperationKind.PointerSubtract;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperationKind.DynamicSubtract;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodSubtract;
                    }

                    break;

                case BinaryOperatorKind.Multiplication:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerMultiply;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedMultiply;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return BinaryOperationKind.FloatingMultiply;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperationKind.DecimalMultiply;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperationKind.DynamicMultiply;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodMultiply;
                    }

                    break;

                case BinaryOperatorKind.Division:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerDivide;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedDivide;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return BinaryOperationKind.FloatingDivide;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperationKind.DecimalDivide;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperationKind.DynamicDivide;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodDivide;
                    }

                    break;

                case BinaryOperatorKind.Remainder:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerRemainder;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedRemainder;
                        case BinaryOperatorKind.Double:
                        case BinaryOperatorKind.Float:
                            return BinaryOperationKind.FloatingRemainder;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperationKind.DynamicRemainder;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodRemainder;
                    }

                    break;

                case BinaryOperatorKind.LeftShift:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerLeftShift;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedLeftShift;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperationKind.DynamicLeftShift;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodLeftShift;
                    }

                    break;

                case BinaryOperatorKind.RightShift:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerRightShift;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedRightShift;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperationKind.DynamicRightShift;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodRightShift;
                    }

                    break;

                case BinaryOperatorKind.And:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerAnd;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedAnd;
                        case BinaryOperatorKind.Bool:
                            if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                            {
                                return BinaryOperationKind.BooleanConditionalAnd;
                            }

                            return BinaryOperationKind.BooleanAnd;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperationKind.EnumAnd;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperationKind.DynamicAnd;
                        case BinaryOperatorKind.UserDefined:
                            if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                            {
                                return BinaryOperationKind.OperatorMethodConditionalAnd;
                            }

                            return BinaryOperationKind.OperatorMethodAnd;
                    }

                    break;

                case BinaryOperatorKind.Or:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerOr;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedOr;
                        case BinaryOperatorKind.Bool:
                            if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                            {
                                return BinaryOperationKind.BooleanConditionalOr;
                            }

                            return BinaryOperationKind.BooleanOr;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperationKind.EnumOr;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperationKind.DynamicOr;
                        case BinaryOperatorKind.UserDefined:
                            if ((operatorKind & BinaryOperatorKind.Logical) != 0)
                            {
                                return BinaryOperationKind.OperatorMethodConditionalOr;
                            }

                            return BinaryOperationKind.OperatorMethodOr;
                    }

                    break;

                case BinaryOperatorKind.Xor:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerExclusiveOr;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedExclusiveOr;
                        case BinaryOperatorKind.Bool:
                            return BinaryOperationKind.BooleanExclusiveOr;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperationKind.EnumExclusiveOr;
                        case BinaryOperatorKind.Dynamic:
                            return BinaryOperationKind.DynamicExclusiveOr;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodExclusiveOr;
                    }

                    break;

                case BinaryOperatorKind.LessThan:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerLessThan;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedLessThan;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return BinaryOperationKind.FloatingLessThan;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperationKind.DecimalLessThan;
                        case BinaryOperatorKind.Pointer:
                            return BinaryOperationKind.PointerLessThan;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperationKind.EnumLessThan;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodLessThan;
                    }

                    break;

                case BinaryOperatorKind.LessThanOrEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerLessThanOrEqual;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedLessThanOrEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return BinaryOperationKind.FloatingLessThanOrEqual;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperationKind.DecimalLessThanOrEqual;
                        case BinaryOperatorKind.Pointer:
                            return BinaryOperationKind.PointerLessThanOrEqual;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperationKind.EnumLessThanOrEqual;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodLessThanOrEqual;
                    }

                    break;

                case BinaryOperatorKind.Equal:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.IntegerEquals;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return BinaryOperationKind.FloatingEquals;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperationKind.DecimalEquals;
                        case BinaryOperatorKind.Pointer:
                            return BinaryOperationKind.PointerEquals;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperationKind.EnumEquals;
                        case BinaryOperatorKind.Bool:
                            return BinaryOperationKind.BooleanEquals;
                        case BinaryOperatorKind.String:
                            return BinaryOperationKind.StringEquals;
                        case BinaryOperatorKind.Object:
                            return BinaryOperationKind.ObjectEquals;
                        case BinaryOperatorKind.Delegate:
                            return BinaryOperationKind.DelegateEquals;
                        case BinaryOperatorKind.NullableNull:
                            return BinaryOperationKind.NullableEquals;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodEquals;
                    }

                    break;

                case BinaryOperatorKind.NotEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.IntegerNotEquals;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return BinaryOperationKind.FloatingNotEquals;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperationKind.DecimalNotEquals;
                        case BinaryOperatorKind.Pointer:
                            return BinaryOperationKind.PointerNotEquals;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperationKind.EnumNotEquals;
                        case BinaryOperatorKind.Bool:
                            return BinaryOperationKind.BooleanNotEquals;
                        case BinaryOperatorKind.String:
                            return BinaryOperationKind.StringNotEquals;
                        case BinaryOperatorKind.Object:
                            return BinaryOperationKind.ObjectNotEquals;
                        case BinaryOperatorKind.Delegate:
                            return BinaryOperationKind.DelegateNotEquals;
                        case BinaryOperatorKind.NullableNull:
                            return BinaryOperationKind.NullableNotEquals;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodNotEquals;
                    }

                    break;

                case BinaryOperatorKind.GreaterThanOrEqual:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerGreaterThanOrEqual;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedGreaterThanOrEqual;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return BinaryOperationKind.FloatingGreaterThanOrEqual;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperationKind.DecimalGreaterThanOrEqual;
                        case BinaryOperatorKind.Pointer:
                            return BinaryOperationKind.PointerGreaterThanOrEqual;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperationKind.EnumGreaterThanOrEqual;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodGreaterThanOrEqual;
                    }

                    break;

                case BinaryOperatorKind.GreaterThan:
                    switch (operatorKind & BinaryOperatorKind.TypeMask)
                    {
                        case BinaryOperatorKind.Int:
                        case BinaryOperatorKind.Long:
                            return BinaryOperationKind.IntegerGreaterThan;
                        case BinaryOperatorKind.UInt:
                        case BinaryOperatorKind.ULong:
                            return BinaryOperationKind.UnsignedGreaterThan;
                        case BinaryOperatorKind.Float:
                        case BinaryOperatorKind.Double:
                            return BinaryOperationKind.FloatingGreaterThan;
                        case BinaryOperatorKind.Decimal:
                            return BinaryOperationKind.DecimalGreaterThan;
                        case BinaryOperatorKind.Pointer:
                            return BinaryOperationKind.PointerGreaterThan;
                        case BinaryOperatorKind.Enum:
                            return BinaryOperationKind.EnumGreaterThan;
                        case BinaryOperatorKind.UserDefined:
                            return BinaryOperationKind.OperatorMethodGreaterThan;
                    }

                    break;
            }

            return BinaryOperationKind.Invalid;
        }
    }

    /// <summary>
    /// This node represents an 'out var' parameter to a Deconstruct method.
    /// It is only used temporarily during initial binding.
    /// </summary>
    internal partial class OutDeconstructVarPendingInference
    {
        public override void Accept(OperationVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        protected override OperationKind ExpressionKind
        {
            get { throw ExceptionUtilities.Unreachable; }
        }
    }

    partial class BoundIsPatternExpression
    {
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

        // TODO: implement IOperation for pattern-matching constructs (https://github.com/dotnet/roslyn/issues/8699)
        protected override OperationKind ExpressionKind => OperationKind.None;
    }

    /// <summary>
    /// This node represents an out variable.
    /// It is only used temporarily during initial binding.
    /// </summary>
    internal partial class OutVariablePendingInference
    {
        public override void Accept(OperationVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        protected override OperationKind ExpressionKind
        {
            get { throw ExceptionUtilities.Unreachable; }
        }
    }

    /// <summary>
    /// This node represents a deconstruction local.
    /// It is only used temporarily during initial binding.
    /// </summary>
    internal partial class DeconstructionVariablePendingInference
    {
        public override void Accept(OperationVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        protected override OperationKind ExpressionKind
        {
            get { throw ExceptionUtilities.Unreachable; }
        }
    }

    partial class BoundThrowExpression
    {
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

        // TODO: implement IOperation for pattern-matching constructs (https://github.com/dotnet/roslyn/issues/8699)
        protected override OperationKind ExpressionKind => OperationKind.None;
    }

    internal partial class BoundDeclarationPattern
    {
        public BoundDeclarationPattern(SyntaxNode syntax, LocalSymbol localSymbol, BoundTypeExpression declaredType, bool isVar, bool hasErrors = false)
            : this(syntax, localSymbol, new BoundLocal(syntax, localSymbol, null, declaredType.Type), declaredType, isVar, hasErrors)
        {
        }
    }
}
