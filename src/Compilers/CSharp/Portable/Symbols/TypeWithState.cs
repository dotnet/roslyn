// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A type and its corresponding flow state resulting from evaluating an rvalue expression.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct TypeWithState
    {
        public TypeSymbol Type { get; }
        public NullableFlowState State { get; }
        public bool HasNullType => Type is null;
        public bool MaybeNull => State == NullableFlowState.MaybeNull;
        public bool NotNull => State == NullableFlowState.NotNull;
        public static TypeWithState ForType(TypeSymbol type) => new TypeWithState(type, type?.CanContainNull() == true ? NullableFlowState.MaybeNull : NullableFlowState.NotNull);
        public TypeWithState(TypeSymbol type, NullableFlowState state) => (Type, State) = (type, state);
        public void Deconstruct(out TypeSymbol type, out NullableFlowState state) => (type, state) = (Type, State);
        public string GetDebuggerDisplay() => $"{{Type:{Type?.GetDebuggerDisplay()}, State:{State}{"}"}";
        public TypeWithState WithNotNullState() => new TypeWithState(Type, NullableFlowState.NotNull);
        public TypeSymbolWithAnnotations ToTypeSymbolWithAnnotations()
        {
            NullableAnnotation annotation = this.State.IsNotNull() || Type?.CanContainNull() == false || Type?.IsTypeParameterDisallowingAnnotation() == true
                ? NullableAnnotation.NotAnnotated : NullableAnnotation.Annotated;
            return TypeSymbolWithAnnotations.Create(this.Type, annotation);
        }
    }
}
