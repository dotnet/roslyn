// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Dynamic call-site delegate, for call-sites that do not
    /// match System.Action or System.Func signatures.
    /// </summary>
    internal sealed class SynthesizedDelegateSymbol : SynthesizedContainer
    {
        private readonly NamespaceOrTypeSymbol _containingSymbol;
        private readonly MethodSymbol _constructor;
        private readonly MethodSymbol _invoke;

        public SynthesizedDelegateSymbol(
            NamespaceOrTypeSymbol containingSymbol,
            string name,
            TypeSymbol objectType,
            TypeSymbol intPtrType,
            TypeSymbol voidReturnTypeOpt,
            int parameterCount,
            BitVector byRefParameters)
            : base(name, parameterCount, returnsVoid: (object)voidReturnTypeOpt != null)
        {
            _containingSymbol = containingSymbol;
            _constructor = new DelegateConstructor(this, objectType, intPtrType);
            _invoke = new InvokeMethod(this, byRefParameters, voidReturnTypeOpt);
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Delegate; }
        }

        internal override MethodSymbol Constructor
        {
            get { return _constructor; }
        }

        public override IEnumerable<string> MemberNames
        {
            get { return new[] { _constructor.Name, _invoke.Name }; }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return ImmutableArray.Create<Symbol>(_constructor, _invoke);
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return
                (name == _constructor.Name) ? ImmutableArray.Create<Symbol>(_constructor) :
                (name == _invoke.Name) ? ImmutableArray.Create<Symbol>(_invoke) :
                ImmutableArray<Symbol>.Empty;
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Internal; }
        }

        public override bool IsSealed
        {
            get { return true; }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get { return ContainingAssembly.GetSpecialType(SpecialType.System_MulticastDelegate); }
        }

        private sealed class DelegateConstructor : SynthesizedInstanceConstructor
        {
            private readonly ImmutableArray<ParameterSymbol> _parameters;

            public DelegateConstructor(NamedTypeSymbol containingType, TypeSymbol objectType, TypeSymbol intPtrType)
                : base(containingType)
            {
                _parameters = ImmutableArray.Create<ParameterSymbol>(
                   new SynthesizedParameterSymbol(this, objectType, 0, RefKind.None, "object"),
                   new SynthesizedParameterSymbol(this, intPtrType, 1, RefKind.None, "method"));
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return _parameters; }
            }
        }

        private sealed class InvokeMethod : SynthesizedInstanceMethodSymbol
        {
            private readonly ImmutableArray<ParameterSymbol> _parameters;
            private readonly TypeSymbol _containingType;
            private readonly TypeSymbol _returnType;

            internal InvokeMethod(SynthesizedDelegateSymbol containingType, BitVector byRefParameters, TypeSymbol voidReturnTypeOpt)
            {
                var typeParams = containingType.TypeParameters;

                _containingType = containingType;

                // if we are given Void type the method returns Void, otherwise its return type is the last type parameter of the delegate:
                _returnType = voidReturnTypeOpt ?? typeParams.Last();

                var parameters = new ParameterSymbol[typeParams.Length - ((object)voidReturnTypeOpt != null ? 0 : 1)];
                for (int i = 0; i < parameters.Length; i++)
                {
                    // we don't need to distinguish between out and ref since this is an internal synthesized symbol:
                    var refKind = !byRefParameters.IsNull && byRefParameters[i] ? RefKind.Ref : RefKind.None;

                    parameters[i] = new SynthesizedParameterSymbol(this, typeParams[i], i, refKind);
                }

                _parameters = parameters.AsImmutableOrNull();
            }

            public override string Name
            {
                get { return WellKnownMemberNames.DelegateInvokeName; }
            }

            internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
            {
                return true;
            }

            internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
            {
                return true;
            }

            internal override bool IsMetadataFinal
            {
                get
                {
                    return false;
                }
            }

            public override MethodKind MethodKind
            {
                get { return MethodKind.DelegateInvoke; }
            }

            public override int Arity
            {
                get { return 0; }
            }

            public override bool IsExtensionMethod
            {
                get { return false; }
            }

            internal override bool HasSpecialName
            {
                get { return false; }
            }

            internal override System.Reflection.MethodImplAttributes ImplementationAttributes
            {
                get { return System.Reflection.MethodImplAttributes.Runtime; }
            }

            internal override bool HasDeclarativeSecurity
            {
                get { return false; }
            }

            public override DllImportData GetDllImportData()
            {
                return null;
            }

            internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
            {
                get { return null; }
            }

            internal override bool RequiresSecurityObject
            {
                get { return false; }
            }

            public override bool HidesBaseMethodsByName
            {
                get { return false; }
            }

            public override bool IsVararg
            {
                get { return false; }
            }

            public override bool ReturnsVoid
            {
                get { return _returnType.SpecialType == SpecialType.System_Void; }
            }

            public override bool IsAsync
            {
                get { return false; }
            }

            public override TypeSymbol ReturnType
            {
                get { return _returnType; }
            }

            public override ImmutableArray<TypeSymbol> TypeArguments
            {
                get { return ImmutableArray<TypeSymbol>.Empty; }
            }

            public override ImmutableArray<TypeParameterSymbol> TypeParameters
            {
                get { return ImmutableArray<TypeParameterSymbol>.Empty; }
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return _parameters; }
            }

            public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
            {
                get { return ImmutableArray<MethodSymbol>.Empty; }
            }

            public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
            {
                get { return ImmutableArray<CustomModifier>.Empty; }
            }

            public override Symbol AssociatedSymbol
            {
                get { return null; }
            }

            internal override ImmutableArray<string> GetAppliedConditionalSymbols()
            {
                return ImmutableArray<string>.Empty;
            }

            internal override Microsoft.Cci.CallingConvention CallingConvention
            {
                get { return Microsoft.Cci.CallingConvention.HasThis; }
            }

            internal override bool GenerateDebugInfo
            {
                get { return false; }
            }

            public override Symbol ContainingSymbol
            {
                get { return _containingType; }
            }

            public override ImmutableArray<Location> Locations
            {
                get { return ImmutableArray<Location>.Empty; }
            }

            public override Accessibility DeclaredAccessibility
            {
                get
                {
                    // Invoke method of a delegate used in a dynamic call-site must be public 
                    // since the DLR looks only for public Invoke methods:
                    return Accessibility.Public;
                }
            }

            public override bool IsStatic
            {
                get { return false; }
            }

            public override bool IsVirtual
            {
                get { return true; }
            }

            public override bool IsOverride
            {
                get { return false; }
            }

            public override bool IsAbstract
            {
                get { return false; }
            }

            public override bool IsSealed
            {
                get { return false; }
            }

            public override bool IsExtern
            {
                get { return false; }
            }
        }
    }
}
