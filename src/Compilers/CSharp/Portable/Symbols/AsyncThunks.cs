// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class AsyncThunkMethod : WrappedMethodSymbol
    {
        internal AsyncThunkMethod(MethodSymbol underlyingMethod)
        {
            UnderlyingMethod = underlyingMethod;
        }

        public override MethodSymbol UnderlyingMethod { get; }

        public override Symbol? AssociatedSymbol => null;

        public override TypeWithAnnotations ReturnTypeWithAnnotations => UnderlyingMethod.ReturnTypeWithAnnotations;

        public override bool ReturnsVoid => false;

        public override RefKind RefKind => RefKind.None;

        internal override TypeMap TypeSubstitution => UnderlyingMethod.TypeSubstitution;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => UnderlyingMethod.TypeArgumentsWithAnnotations;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => UnderlyingMethod.TypeParameters;

        public override ImmutableArray<ParameterSymbol> Parameters => UnderlyingMethod.Parameters;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => UnderlyingMethod.ExplicitInterfaceImplementations;

        public override Symbol ContainingSymbol => UnderlyingMethod.ContainingSymbol;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => UnderlyingMethod.CalculateLocalSyntaxOffset(localPosition, localTree);

        internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => UnderlyingMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete);

        internal override bool IsNullableAnalysisEnabled() => UnderlyingMethod.IsNullableAnalysisEnabled();

        internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            builderArgument = null;
            return false;
        }
    }

    internal sealed class AsyncThunkForAsync2Method : AsyncThunkMethod
    {
        // thunks are mostly wrapper symbols to express that a method call is done via a thunk
        // it is not expected that thunks will be used as an input to generic instantiation
        // (that should happen using actual async2 methods)
        //
        // the OriginalDefinition and ConstructedFrom as implemented here are just to satisfy asserts

        internal AsyncThunkForAsync2Method(MethodSymbol underlyingMethod)
            : base(underlyingMethod)
        {
            Debug.Assert(underlyingMethod.IsAsync2);
        }

        private AsyncThunkForAsync2Method? _origDefinition;
        private AsyncThunkForAsync2Method? _constructedFrom;

        internal override bool IsAsync2 => false;

        public override MethodSymbol OriginalDefinition
        {
            get
            {
                if (UnderlyingMethod.IsDefinition)
                {
                    return this;
                }

                if (_origDefinition == null)
                {
                    _origDefinition = new AsyncThunkForAsync2Method(UnderlyingMethod.OriginalDefinition);
                }

                return _origDefinition;
            }
        }

        public override MethodSymbol ConstructedFrom
        {
            get
            {
                if (UnderlyingMethod.IsDefinition)
                {
                    return this;
                }

                if (_constructedFrom == null)
                {
                    _constructedFrom = new AsyncThunkForAsync2Method(UnderlyingMethod.ConstructedFrom);
                }

                return _constructedFrom;
            }
        }
    }

    internal sealed class Async2ThunkForAsyncMethod : AsyncThunkMethod
    {
        // thunks are mostly wrapper symbols to express that a method call is done via a thunk
        // it is not expected that thunks will be used as an input to generic instantiation
        // (that should happen using actual async methods)
        //
        // the OriginalDefinition and ConstructedFrom as implemented here are just to satisfy asserts

        internal Async2ThunkForAsyncMethod(MethodSymbol underlyingMethod)
            : base(underlyingMethod)
        {
            Debug.Assert(!underlyingMethod.IsAsync2);
        }

        private Async2ThunkForAsyncMethod? _origDefinition;
        private Async2ThunkForAsyncMethod? _constructedFrom;

        internal override bool IsAsync2 => true;

        public override MethodSymbol OriginalDefinition
        {
            get
            {
                if (UnderlyingMethod.IsDefinition)
                {
                    return this;
                }

                if (_origDefinition == null)
                {
                    _origDefinition = new Async2ThunkForAsyncMethod(UnderlyingMethod.OriginalDefinition);
                }

                return _origDefinition;
            }
        }

        public override MethodSymbol ConstructedFrom
        {
            get
            {
                if (UnderlyingMethod.IsDefinition)
                {
                    return this;
                }

                if (_constructedFrom == null)
                {
                    _constructedFrom = new Async2ThunkForAsyncMethod(UnderlyingMethod.ConstructedFrom);
                }

                return _constructedFrom;
            }
        }
    }
}
