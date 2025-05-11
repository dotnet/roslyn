// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A method symbol that wraps a factory method from a CollectionBuilderAttribute,
    /// but with the ReadOnlySpan&lt;T&gt; items parameter removed. This symbol is used
    /// for overload resolution of collection arguments within a collection expression.
    /// </summary>
    internal sealed class CollectionBuilderArgumentsOnlyMethodSymbol : MethodSymbol
    {
        internal readonly MethodSymbol UnderlyingMethod;

        private readonly ImmutableArray<ParameterSymbol> _parameters;

        internal CollectionBuilderArgumentsOnlyMethodSymbol(MethodSymbol underlyingMethod)
        {
            Debug.Assert(underlyingMethod.MethodKind == MethodKind.Ordinary);
            Debug.Assert(underlyingMethod.IsStatic);

            UnderlyingMethod = underlyingMethod;
            _parameters = createParameters(this, underlyingMethod.Parameters);

            static ImmutableArray<ParameterSymbol> createParameters(
                CollectionBuilderArgumentsOnlyMethodSymbol containingMethod,
                ImmutableArray<ParameterSymbol> underlyingParameters)
            {
                int n = underlyingParameters.Length - 1;
                Debug.Assert(n >= 0);
                var parameterBuilder = ArrayBuilder<ParameterSymbol>.GetInstance(n);
                for (int i = 0; i < n; i++)
                {
                    parameterBuilder.Add(new CollectionBuilderArgumentsOnlyParameterSymbol(containingMethod, underlyingParameters[i]));
                }
                return parameterBuilder.ToImmutableAndFree();
            }
        }

        public override string Name => UnderlyingMethod.Name;

        public override MethodKind MethodKind => UnderlyingMethod.MethodKind;

        public override int Arity => TypeParameters.Length;

        public override bool IsExtensionMethod => false;

        public override bool HidesBaseMethodsByName => false;

        public override bool IsVararg => false;

        public override bool ReturnsVoid => UnderlyingMethod.ReturnsVoid;

        public override bool IsAsync => false;

        public override RefKind RefKind => UnderlyingMethod.RefKind;

        public override TypeWithAnnotations ReturnTypeWithAnnotations => UnderlyingMethod.ReturnTypeWithAnnotations;

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => UnderlyingMethod.ReturnTypeFlowAnalysisAnnotations;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => throw ExceptionUtilities.Unreachable(); // PROTOTYPE: Should this property be checked in NullableWalker?

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => throw ExceptionUtilities.Unreachable();

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => GetTypeParametersAsTypeArguments();

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];

        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => throw ExceptionUtilities.Unreachable();

        public override ImmutableArray<CustomModifier> RefCustomModifiers => UnderlyingMethod.RefCustomModifiers;

        public override Symbol? AssociatedSymbol => null;

        public override bool AreLocalsZeroed => throw ExceptionUtilities.Unreachable();

        public override Symbol ContainingSymbol => UnderlyingMethod.ContainingSymbol;

        public override ImmutableArray<Location> Locations => throw ExceptionUtilities.Unreachable();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw ExceptionUtilities.Unreachable();

        public override Accessibility DeclaredAccessibility => UnderlyingMethod.DeclaredAccessibility;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => throw ExceptionUtilities.Unreachable();

        public override bool IsExtern => throw ExceptionUtilities.Unreachable();

        protected override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();

        internal override bool HasSpecialName => throw ExceptionUtilities.Unreachable();

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes => throw ExceptionUtilities.Unreachable();

        internal override bool HasDeclarativeSecurity => throw ExceptionUtilities.Unreachable();

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => throw ExceptionUtilities.Unreachable();

        internal override bool RequiresSecurityObject => throw ExceptionUtilities.Unreachable();

        internal override bool IsDeclaredReadOnly => UnderlyingMethod.IsDeclaredReadOnly;

        internal override bool IsInitOnly => UnderlyingMethod.IsInitOnly;

        internal override bool HasUnscopedRefAttribute => throw ExceptionUtilities.Unreachable();

        // PROTOTYPE: Should be "throw ExceptionUtilities.Unreachable();" because ref analysis
        // should be looking at the UnderlyingMethod rather than this method, to ensure we're
        // considering the ReadOnlySpan<T> parameter.
        internal override bool UseUpdatedEscapeRules => UnderlyingMethod.UseUpdatedEscapeRules;

        internal override Microsoft.Cci.CallingConvention CallingConvention => throw ExceptionUtilities.Unreachable();

        internal override bool GenerateDebugInfo => throw ExceptionUtilities.Unreachable();

        internal override ObsoleteAttributeData? ObsoleteAttributeData => UnderlyingMethod.ObsoleteAttributeData;

        public override DllImportData? GetDllImportData() => throw ExceptionUtilities.Unreachable();

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => throw ExceptionUtilities.Unreachable();

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation() => throw ExceptionUtilities.Unreachable();

        internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete)
        {
            return UnderlyingMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete);
        }

        internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument) => throw ExceptionUtilities.Unreachable();

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => throw ExceptionUtilities.Unreachable();

        internal override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None) => throw ExceptionUtilities.Unreachable();

        internal override bool IsNullableAnalysisEnabled() => throw ExceptionUtilities.Unreachable();

        internal override int TryGetOverloadResolutionPriority() => UnderlyingMethod.TryGetOverloadResolutionPriority();

        public override bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            return other is CollectionBuilderArgumentsOnlyMethodSymbol { UnderlyingMethod: var otherMethod } &&
                UnderlyingMethod.Equals(otherMethod, compareKind);
        }

        public override int GetHashCode() => UnderlyingMethod.GetHashCode();

        private sealed class CollectionBuilderArgumentsOnlyParameterSymbol : WrappedParameterSymbol
        {
            private readonly CollectionBuilderArgumentsOnlyMethodSymbol _containingMethod;

            internal CollectionBuilderArgumentsOnlyParameterSymbol(CollectionBuilderArgumentsOnlyMethodSymbol containingMethod, ParameterSymbol underlyingParameter) :
                base(underlyingParameter)
            {
                _containingMethod = containingMethod;
            }

            public override Symbol ContainingSymbol => _containingMethod;

            public override TypeWithAnnotations TypeWithAnnotations => _underlyingParameter.TypeWithAnnotations;

            public override ImmutableArray<CustomModifier> RefCustomModifiers => _underlyingParameter.RefCustomModifiers;

            internal override bool IsCallerFilePath => _underlyingParameter.IsCallerFilePath;

            internal override bool IsCallerLineNumber => _underlyingParameter.IsCallerLineNumber;

            internal override bool IsCallerMemberName => _underlyingParameter.IsCallerMemberName;

            internal override int CallerArgumentExpressionParameterIndex => _underlyingParameter.CallerArgumentExpressionParameterIndex;

            internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => throw ExceptionUtilities.Unreachable();

            internal override bool HasInterpolatedStringHandlerArgumentError => throw ExceptionUtilities.Unreachable();

            internal override bool HasEnumeratorCancellationAttribute => _underlyingParameter.HasEnumeratorCancellationAttribute;
        }
    }
}
