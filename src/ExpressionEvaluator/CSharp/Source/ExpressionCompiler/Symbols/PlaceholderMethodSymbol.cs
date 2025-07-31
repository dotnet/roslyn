// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    /// <summary>
    /// Represents an intrinsic debugger method with byref return type.
    /// </summary>
    internal sealed partial class PlaceholderMethodSymbol : MethodSymbol
    {
        internal delegate ImmutableArray<TypeParameterSymbol> GetTypeParameters(PlaceholderMethodSymbol method);
        internal delegate ImmutableArray<ParameterSymbol> GetParameters(PlaceholderMethodSymbol method);
        internal delegate TypeSymbol GetReturnType(PlaceholderMethodSymbol method);

        private readonly NamedTypeSymbol _container;
        private readonly string _name;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly TypeWithAnnotations _returnType;
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        internal PlaceholderMethodSymbol(
            NamedTypeSymbol container,
            string name,
            GetTypeParameters getTypeParameters,
            GetReturnType getReturnType,
            GetParameters getParameters)
        {
            _container = container;
            _name = name;
            _typeParameters = getTypeParameters(this);
            _returnType = TypeWithAnnotations.Create(getReturnType(this));
            _parameters = getParameters(this);
        }

        public override int Arity
        {
            get { return _typeParameters.Length; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _container; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Internal; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        public override bool IsAsync
        {
            get { return false; }
        }

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return false; }
        }

        public override bool IsStatic
        {
            get { return true; }
        }

        public override bool IsVararg
        {
            get { return false; }
        }

        public override bool IsVirtual
        {
            get { return false; }
        }

        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsInitOnly => false;

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.Ordinary; }
        }

        public override string Name
        {
            get { return _name; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }

        public override bool ReturnsVoid
        {
            get { return false; }
        }

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return _returnType; }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return GetTypeParametersAsTypeArguments(); }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        internal override Cci.CallingConvention CallingConvention
        {
            get
            {
                Debug.Assert(this.IsStatic);
                return this.IsGenericMethod ? Cci.CallingConvention.Generic : Cci.CallingConvention.Default;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal override bool HasSpecialName
        {
            get { return true; }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal sealed override UnmanagedCallersOnlyAttributeData GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => throw ExceptionUtilities.Unreachable();

        internal sealed override bool HasSpecialNameAttribute => throw ExceptionUtilities.Unreachable();

        internal override bool HasUnscopedRefAttribute => false;

        internal override bool UseUpdatedEscapeRules => false;

        internal override bool RequiresSecurityObject
        {
            get { return false; }
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        public override DllImportData GetDllImportData()
        {
            return null;
        }

        public override bool AreLocalsZeroed
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None)
        {
            return false;
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool IsNullableAnalysisEnabled() => false;

        protected override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal override int TryGetOverloadResolutionPriority() => 0;

#if DEBUG
        protected override MethodSymbolAdapter CreateCciAdapter()
        {
            return new PlaceholderMethodSymbolAdapter(this);
        }
#endif
    }

#if DEBUG
    internal sealed partial class PlaceholderMethodSymbolAdapter : MethodSymbolAdapter
    {
        internal PlaceholderMethodSymbolAdapter(MethodSymbol underlyingMethodSymbol) : base(underlyingMethodSymbol)
        { }
    }
#endif

#if DEBUG
    internal partial class PlaceholderMethodSymbolAdapter :
#else
    internal partial class PlaceholderMethodSymbol :
#endif
        Cci.ISignature
    {
        bool Cci.ISignature.ReturnValueIsByRef
        {
            get { return true; }
        }

        // This should be inherited from the base class implementation, but it does not currently work with Nullable
        // Reference Types.
        // https://github.com/dotnet/roslyn/issues/39167
        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.RefCustomModifiers
        {
            get
            {
                return ImmutableArray<Cci.ICustomModifier>.CastUp(AdaptedMethodSymbol.RefCustomModifiers);
            }
        }
    }
}
