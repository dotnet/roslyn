// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A type and its corresponding flow state resulting from evaluating an rvalue expression.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct TypeWithState
    {
        public readonly TypeSymbol Type;
        public readonly NullableFlowState State;
        public bool HasNullType => Type is null;
        public bool MayBeNull => State == NullableFlowState.MaybeNull;
        public bool IsNotNull => State == NullableFlowState.NotNull;

        public static TypeWithState ForType(TypeSymbol type)
        {
            var state = type?.CanContainNull() != false ? NullableFlowState.MaybeNull : NullableFlowState.NotNull;
            return new TypeWithState(type, state);
        }

        public static TypeWithState Create(TypeSymbol type, NullableFlowState defaultState)
        {
            var state = defaultState == NullableFlowState.MaybeNull && type?.CanContainNull() != false ? NullableFlowState.MaybeNull : NullableFlowState.NotNull;
            return new TypeWithState(type, state);
        }

        public static TypeWithState Create(TypeWithAnnotations typeWithAnnotations, FlowAnalysisAnnotations annotations = FlowAnalysisAnnotations.None)
        {
            var type = typeWithAnnotations.Type;
            Debug.Assert((object)type != null);

            NullableFlowState state;
            if (type.CanContainNull())
            {
                if ((annotations & FlowAnalysisAnnotations.MaybeNull) == FlowAnalysisAnnotations.MaybeNull)
                {
                    state = NullableFlowState.MaybeNull;
                }
                else if ((annotations & FlowAnalysisAnnotations.NotNull) == FlowAnalysisAnnotations.NotNull)
                {
                    state = NullableFlowState.NotNull;
                }
                else
                {
                    return typeWithAnnotations.ToTypeWithState();
                }
            }
            else
            {
                state = NullableFlowState.NotNull;
            }

            return new TypeWithState(type, state);
        }

        private TypeWithState(TypeSymbol type, NullableFlowState state)
        {
            Debug.Assert(state == NullableFlowState.NotNull || type?.CanContainNull() != false);
            Type = type;
            State = state;
        }

        public void Deconstruct(out TypeSymbol type, out NullableFlowState state) => (type, state) = (Type, State);

        public string GetDebuggerDisplay() => $"{{Type:{Type?.GetDebuggerDisplay()}, State:{State}{"}"}";

        public override string ToString() => GetDebuggerDisplay();

        public TypeWithState WithNotNullState() => new TypeWithState(Type, NullableFlowState.NotNull);

        public TypeWithState WithSuppression(bool suppress) => suppress ? new TypeWithState(Type, NullableFlowState.NotNull) : this;

        public TypeWithAnnotations ToTypeWithAnnotations()
        {
            NullableAnnotation annotation = this.State.IsNotNull() || Type?.CanContainNull() == false || Type?.IsTypeParameterDisallowingAnnotation() == true
                ? NullableAnnotation.NotAnnotated : NullableAnnotation.Annotated;
            return TypeWithAnnotations.Create(this.Type, annotation);
        }
    }
}
