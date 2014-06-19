// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly PropertySymbol property;
        private readonly MethodSymbol overriddenAccessor;
        private readonly ImmutableArray<ParameterSymbol> parameters;

        public SynthesizedSealedPropertyAccessor(PropertySymbol property, MethodSymbol overriddenAccessor)
        {
            Debug.Assert((object)property != null);
            Debug.Assert(property.IsSealed);
            Debug.Assert((object)overriddenAccessor != null);

            this.property = property;
            this.overriddenAccessor = overriddenAccessor;
            this.parameters = SynthesizedParameterSymbol.DeriveParameters(overriddenAccessor, this);
        }

        internal MethodSymbol OverriddenAccessor
        {
            get
            {
                return overriddenAccessor;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return property.ContainingType;
            }
        }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            return LexicalSortKey.NotInSource;
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
                Accessibility overriddenAccessibility = this.overriddenAccessor.DeclaredAccessibility;

                if (overriddenAccessibility == Accessibility.ProtectedOrInternal &&
                    !this.ContainingAssembly.HasInternalAccessTo(overriddenAccessor.ContainingAssembly))
                {
                    // NOTE: Dev10 actually reports ERR_CantChangeAccessOnOverride (CS0507) in this case,
                    // but it's not clear why.  It seems like it would make more sense to just correct
                    // the accessibility of the synthesized override, the same way a programmer would if
                    // it existed in source.

                    return Accessibility.Protected;
                }
                else
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    Debug.Assert(AccessCheck.IsSymbolAccessible(overriddenAccessor, this.ContainingType, ref useSiteDiagnostics));
                    return overriddenAccessibility;
                }
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
                return overriddenAccessor.CallingConvention;
            }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return overriddenAccessor.MethodKind;
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
                return overriddenAccessor.IsVararg;
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                return overriddenAccessor.ReturnsVoid;
            }
        }

        public override TypeSymbol ReturnType
        {
            get
            {
                return overriddenAccessor.ReturnType;
            }
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get
            {
                return ImmutableArray<TypeSymbol>.Empty;
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
                return this.parameters;
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

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get
            {
                return overriddenAccessor.ReturnTypeCustomModifiers;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return property;
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
                return overriddenAccessor.Name;
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

        internal override bool IsMetadataFinal()
        {
            return true;
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
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }
    }
}