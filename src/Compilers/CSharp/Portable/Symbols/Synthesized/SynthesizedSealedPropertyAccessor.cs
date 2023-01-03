// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// If a sealed override property defines fewer accessors than the
    /// original virtual property, it is necessary to synthesize a sealed
    /// accessor so that the accessor will not be overridable from metadata.
    /// </summary>
    internal sealed partial class SynthesizedSealedPropertyAccessor : SynthesizedInstanceMethodSymbol
    {
        private readonly PropertySymbol _property;
        private readonly MethodSymbol _overriddenAccessor;
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        public SynthesizedSealedPropertyAccessor(PropertySymbol property, MethodSymbol overriddenAccessor)
        {
            Debug.Assert((object)property != null);
            Debug.Assert(property.IsSealed);
            Debug.Assert((object)overriddenAccessor != null);

            _property = property;
            _overriddenAccessor = overriddenAccessor;
            _parameters = SynthesizedParameterSymbol.DeriveParameters(overriddenAccessor, this);
        }

        internal MethodSymbol OverriddenAccessor
        {
            get
            {
                return _overriddenAccessor;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _property.ContainingType;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                Accessibility overriddenAccessibility = _overriddenAccessor.DeclaredAccessibility;
                switch (overriddenAccessibility)
                {
                    case Accessibility.ProtectedOrInternal:
                        if (!this.ContainingAssembly.HasInternalAccessTo(_overriddenAccessor.ContainingAssembly))
                        {
                            // NOTE: Dev10 actually reports ERR_CantChangeAccessOnOverride (CS0507) in this case,
                            // but it's not clear why.  It seems like it would make more sense to just correct
                            // the accessibility of the synthesized override, the same way a programmer would if
                            // it existed in source.

                            return Accessibility.Protected;
                        }
                        break;

                    case Accessibility.ProtectedAndInternal:
                        if (!this.ContainingAssembly.HasInternalAccessTo(_overriddenAccessor.ContainingAssembly))
                        {
                            // Of course this must trigger an error later, as you cannot override a private
                            // protected member from another assembly.
                            return Accessibility.Private;
                        }
                        break;
                }

#if DEBUG
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                Debug.Assert(AccessCheck.IsSymbolAccessible(_overriddenAccessor, this.ContainingType, ref discardedUseSiteInfo));
#endif
                return overriddenAccessibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return false; //this is an override
            }
        }

        public override bool IsAsync
        {
            get
            {
                return false;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return false; //this is an override
            }
        }

        internal override Cci.CallingConvention CallingConvention
        {
            get
            {
                return _overriddenAccessor.CallingConvention;
            }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return _overriddenAccessor.MethodKind;
            }
        }

        public override int Arity
        {
            get
            {
                return 0;
            }
        }

        public override bool IsExtensionMethod
        {
            get
            {
                return false;
            }
        }

        public override bool HidesBaseMethodsByName
        {
            get
            {
                return false;
            }
        }

        public override bool IsVararg
        {
            get
            {
                return _overriddenAccessor.IsVararg;
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                return _overriddenAccessor.ReturnsVoid;
            }
        }

        public override RefKind RefKind
        {
            get
            {
                return _overriddenAccessor.RefKind;
            }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                return _overriddenAccessor.ReturnTypeWithAnnotations;
            }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get
            {
                return ImmutableArray<TypeWithAnnotations>.Empty;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                return _parameters;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return ImmutableArray<MethodSymbol>.Empty;
            }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return _overriddenAccessor.RefCustomModifiers;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return _property;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return true;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return true;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        public override string Name
        {
            get
            {
                return _overriddenAccessor.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return true;
            }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                return default(System.Reflection.MethodImplAttributes);
            }
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return true;
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return true;
            }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool RequiresSecurityObject
        {
            get
            {
                return false;
            }
        }

        public override DllImportData GetDllImportData()
        {
            return null;
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }
    }
}
