// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // Async2 method is just a method with an attr. Returns Task
    // call to async2 method is just a call, regardless who calls

    // await of Task-returning method in async2 method is call to a thunk
    // the only place where modreq comes to play

    // from metadata - no longer case if async2, only an in-source concern

    internal class Async2ThunkForAsyncMethod : WrappedMethodSymbol
    {
        internal Async2ThunkForAsyncMethod(MethodSymbol underlyingMethod)
        {
            UnderlyingMethod = underlyingMethod;
        }

        // thunk is a wrapper symbols to express that a method call is done via a thunk
        // it is not expected that thunks will be used as an input to generic instantiation
        // (that should happen using actual async methods)
        //
        // the OriginalDefinition and ConstructedFrom as implemented here are just to satisfy asserts

        private Async2ThunkForAsyncMethod? _origDefinition;
        private Async2ThunkForAsyncMethod? _constructedFrom;

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

        public override MethodSymbol UnderlyingMethod { get; }

        public override Symbol? AssociatedSymbol => null;

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                NamedTypeSymbol underlyingReturnType = (NamedTypeSymbol)UnderlyingMethod.ReturnType;
                if (!underlyingReturnType.IsGenericType)
                {
                    var voidType = UnderlyingMethod.ContainingAssembly.GetSpecialType(SpecialType.System_Void);
                    return TypeWithAnnotations.Create(voidType).WithModifiers(ImmutableArray.Create(CSharpCustomModifier.CreateRequired(underlyingReturnType)));
                }

                var taskType = underlyingReturnType.OriginalDefinition.ConstructUnboundGenericType();
                var elementType = underlyingReturnType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;

                return TypeWithAnnotations.Create(elementType).WithModifiers(ImmutableArray.Create(CSharpCustomModifier.CreateRequired(taskType)));
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                NamedTypeSymbol underlyingReturnType = (NamedTypeSymbol)UnderlyingMethod.ReturnType;
                return !underlyingReturnType.IsGenericType;
            }
        }

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
}
