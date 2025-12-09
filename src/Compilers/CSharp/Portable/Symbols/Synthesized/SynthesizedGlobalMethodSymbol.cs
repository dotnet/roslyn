// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated synthesized method symbol
    /// that must be emitted in the compiler generated
    /// PrivateImplementationDetails class
    /// </summary>
    internal abstract class SynthesizedGlobalMethodSymbol : MethodSymbol, ISynthesizedGlobalMethodSymbol
    {
        private readonly SynthesizedPrivateImplementationDetailsType _privateImplType;
        private TypeSymbol _returnType;
        private ImmutableArray<ParameterSymbol> _parameters;
        private ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly string _name;

        internal SynthesizedGlobalMethodSymbol(SynthesizedPrivateImplementationDetailsType privateImplType, string name)
        {
            Debug.Assert(privateImplType is not null);
            Debug.Assert(name != null);

            _privateImplType = privateImplType;
            _name = name;
        }

        internal SynthesizedGlobalMethodSymbol(SynthesizedPrivateImplementationDetailsType privateImplType, TypeSymbol returnType, string name)
            : this(privateImplType, name)
        {
            Debug.Assert((object)returnType != null);
            _returnType = returnType;
            _typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
        }

        protected void SetReturnType(TypeSymbol returnType)
        {
            Debug.Assert(returnType is not null);
            Debug.Assert(_returnType is null);
            _returnType = returnType;
        }

        protected void SetParameters(ImmutableArray<ParameterSymbol> parameters)
        {
            Debug.Assert(!parameters.IsDefault);
            Debug.Assert(_parameters.IsDefault);
            _parameters = parameters;
        }

        protected void SetTypeParameters(ImmutableArray<TypeParameterSymbol> typeParameters)
        {
            Debug.Assert(!typeParameters.IsDefault);
            Debug.Assert(_typeParameters.IsDefault);
            _typeParameters = typeParameters;
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal sealed override bool GenerateDebugInfo
        {
            get { return false; }
        }

        public sealed override Symbol ContainingSymbol
        {
            get { return _privateImplType; }
        }

        public sealed override NamedTypeSymbol ContainingType
        {
            get
            {
                return _privateImplType;
            }
        }

        public PrivateImplementationDetails ContainingPrivateImplementationDetailsType
        {
            get { return _privateImplType.PrivateImplementationDetails; }
        }

        public override string Name
        {
            get { return _name; }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
        }

        internal override bool RequiresSecurityObject
        {
            get { return false; }
        }

        public override DllImportData GetDllImportData()
        {
            return null;
        }

        public sealed override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public sealed override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public sealed override bool AreLocalsZeroed => ContainingModule.AreLocalsZeroed;

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal sealed override UnmanagedCallersOnlyAttributeData GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => null;

        internal sealed override bool HasSpecialNameAttribute => throw ExceptionUtilities.Unreachable();

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        public override bool IsVararg
        {
            get { return false; }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                RoslynDebug.Assert(!_typeParameters.IsDefault, $"Expected {nameof(SetTypeParameters)} prior to accessing this property.");
                if (_typeParameters.IsDefault)
                {
                    return ImmutableArray<TypeParameterSymbol>.Empty;
                }

                return _typeParameters;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                RoslynDebug.Assert(!_parameters.IsDefault, $"Expected {nameof(SetParameters)} prior to accessing this property.");
                if (_parameters.IsDefault)
                {
                    return ImmutableArray<ParameterSymbol>.Empty;
                }

                return _parameters;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Internal; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return TypeWithAnnotations.Create(_returnType); }
        }

        public sealed override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get { return FlowAnalysisAnnotations.None; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return ImmutableArray<TypeWithAnnotations>.Empty; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        public override int Arity
        {
            get { return TypeParameters.Length; }
        }

        public override bool ReturnsVoid
        {
            get { return this.ReturnType.IsVoidType(); }
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.Ordinary; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return false; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsVirtual
        {
            get { return false; }
        }

        public override bool IsStatic
        {
            get { return true; }
        }

        public override bool IsAsync
        {
            get { return false; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal sealed override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None)
        {
            return false;
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        internal override Cci.CallingConvention CallingConvention
        {
            get
            {
                if (IsGenericMethod)
                {
                    return Cci.CallingConvention.Generic;
                }

                return Cci.CallingConvention.Default;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        internal sealed override bool IsDeclaredReadOnly => false;

        internal sealed override bool IsInitOnly => false;

        internal override bool SynthesizesLoweredBoundBody
        {
            get { return true; }
        }

        internal abstract override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics);

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal sealed override bool IsNullableAnalysisEnabled() => false;

        protected sealed override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();

        internal sealed override bool HasUnscopedRefAttribute => false;

        internal sealed override bool UseUpdatedEscapeRules => ContainingModule.UseUpdatedEscapeRules;

        internal sealed override bool IsCallerUnsafe => false;

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal sealed override int TryGetOverloadResolutionPriority()
        {
            return 0;
        }
    }
}
