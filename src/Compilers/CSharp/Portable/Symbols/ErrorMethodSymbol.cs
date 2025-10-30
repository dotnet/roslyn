// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class ErrorMethodSymbol : MethodSymbol
    {
        public static readonly ErrorMethodSymbol UnknownMethod = new ErrorMethodSymbol(ErrorTypeSymbol.UnknownResultType, ErrorTypeSymbol.UnknownResultType, string.Empty);

        private readonly TypeSymbol _containingType;
        private readonly TypeSymbol _returnType;
        private readonly string _name;

        public ErrorMethodSymbol(TypeSymbol containingType, TypeSymbol returnType, string name)
        {
            _containingType = containingType;
            _returnType = returnType;
            _name = name;
        }

        public override string Name
        {
            get { return _name; }
        }

        internal sealed override bool HasSpecialName
        {
            get { return false; }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
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
            get { return false; }
        }

        public override bool IsAsync
        {
            get { return false; }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal sealed override UnmanagedCallersOnlyAttributeData GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => null;

        internal sealed override bool HasSpecialNameAttribute => throw ExceptionUtilities.Unreachable();

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Public; }
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

        public override Symbol ContainingSymbol
        {
            get { return _containingType; }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return Microsoft.Cci.CallingConvention.Default; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsInitOnly => false;

        internal override int ParameterCount
        {
            get { return 0; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return ImmutableArray<ParameterSymbol>.Empty; }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return ImmutableArray<TypeWithAnnotations>.Empty; }
        }

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return TypeWithAnnotations.Create(_returnType); }
        }

        public override bool ReturnsVoid
        {
            get { return this.ReturnType.IsVoidType(); }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override bool IsVararg
        {
            get { return false; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        public override int Arity
        {
            get { return 0; }
        }

        public override MethodKind MethodKind
        {
            get
            {
                switch (_name)
                {
                    case WellKnownMemberNames.InstanceConstructorName:
                        return MethodKind.Constructor;
                    default:
                        // is there a reason to handle other special names?
                        return MethodKind.Ordinary;
                }
            }
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

        internal sealed override bool RequiresSecurityObject
        {
            get { return false; }
        }

        public sealed override DllImportData GetDllImportData()
        {
            return null;
        }

        public override bool AreLocalsZeroed
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal sealed override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal sealed override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal sealed override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        internal sealed override bool IsNullableAnalysisEnabled() => false;

        protected override bool HasSetsRequiredMembersImpl
        {
            get
            {
                Debug.Assert(MethodKind == MethodKind.Constructor);
                return false;
            }
        }

        internal sealed override bool HasUnscopedRefAttribute => false;

        internal sealed override bool UseUpdatedEscapeRules => false;

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
