// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated synthesized method symbol
    /// that must be emitted in the compiler generated
    /// PrivateImplementationDetails class
    /// </summary>
    internal abstract class SynthesizedGlobalMethodSymbol : MethodSymbol
    {
        private readonly ModuleSymbol _containingModule;
        private readonly PrivateImplementationDetails _privateImplType;
        private readonly TypeSymbol _returnType;
        private ImmutableArray<ParameterSymbol> _parameters;
        private readonly string _name;

        internal SynthesizedGlobalMethodSymbol(ModuleSymbol containingModule, PrivateImplementationDetails privateImplType, TypeSymbol returnType, string name)
        {
            Debug.Assert((object)containingModule != null);
            Debug.Assert(privateImplType != null);
            Debug.Assert((object)returnType != null);
            Debug.Assert(name != null);

            _containingModule = containingModule;
            _privateImplType = privateImplType;
            _returnType = returnType;
            _name = name;
        }

        protected void SetParameters(ImmutableArray<ParameterSymbol> parameters)
        {
            ImmutableInterlocked.InterlockedExchange(ref _parameters, parameters);
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal sealed override bool GenerateDebugInfo
        {
            get { return false; }
        }

        internal sealed override ModuleSymbol ContainingModule
        {
            get
            {
                return _containingModule;
            }
        }

        public sealed override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _containingModule.ContainingAssembly;
            }
        }

        /// <summary>
        /// Synthesized methods that must be emitted in the compiler generated
        /// PrivateImplementationDetails class have null containing type symbol.
        /// </summary>
        public sealed override Symbol ContainingSymbol
        {
            get { return null; }
        }

        public sealed override NamedTypeSymbol ContainingType
        {
            get
            {
                return null;
            }
        }

        internal PrivateImplementationDetails ContainingPrivateImplementationDetailsType
        {
            get { return _privateImplType; }
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
            throw ExceptionUtilities.Unreachable;
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

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
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_parameters.IsEmpty)
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

        internal override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override TypeSymbolWithAnnotations ReturnType
        {
            get { return TypeSymbolWithAnnotations.Create(_returnType); }
        }

        public override ImmutableArray<TypeSymbolWithAnnotations> TypeArguments
        {
            get { return ImmutableArray<TypeSymbolWithAnnotations>.Empty; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        public override int Arity
        {
            get { return 0; }
        }

        public override bool ReturnsVoid
        {
            get { return this.ReturnType.SpecialType == SpecialType.System_Void; }
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

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
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
            get { return 0; }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        internal override bool SynthesizesLoweredBoundBody
        {
            get { return true; }
        }

        internal abstract override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics);

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
