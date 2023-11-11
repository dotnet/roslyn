// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class AsyncThunkForAsync2Method : WrappedMethodSymbol
    {
        // thunks are mostly wrapper symbols to express that a method call is done via a thunk
        // it is not expected that thunks will be used as an input to generic instantiation
        // (that should happen using actual async2 methods)
        //
        // the OriginalDefinition and ConstructedFrom as implemented here are just to satisfy asserts

        private AsyncThunkForAsync2Method? _origDefinition;
        private AsyncThunkForAsync2Method? _constructedFrom;

        internal AsyncThunkForAsync2Method(MethodSymbol underlyingMethod)
        {
            Debug.Assert(underlyingMethod.IsAsync2);
            UnderlyingMethod = underlyingMethod;
        }

        public override MethodSymbol UnderlyingMethod { get; }

        public override Symbol? AssociatedSymbol => null;

        public override TypeWithAnnotations ReturnTypeWithAnnotations => UnderlyingMethod.ReturnTypeWithAnnotations;

        public override bool ReturnsVoid => false;

        internal override bool IsAsync2 => false;

        public override RefKind RefKind => RefKind.None;

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
    }
}
