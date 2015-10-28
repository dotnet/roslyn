// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    /// <summary>
    /// Represents an intrinsic debugger method with byref return type.
    /// </summary>
    internal sealed class PlaceholderMethodSymbol : MethodSymbol, Cci.ISignature
    {
        internal delegate ImmutableArray<TypeParameterSymbol> GetTypeParameters(PlaceholderMethodSymbol method);
        internal delegate ImmutableArray<ParameterSymbol> GetParameters(PlaceholderMethodSymbol method);
        internal delegate TypeSymbol GetReturnType(PlaceholderMethodSymbol method);

        private readonly NamedTypeSymbol _container;
        private readonly string _name;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly TypeSymbolWithAnnotations _returnType;
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
            _returnType = TypeSymbolWithAnnotations.Create(getReturnType(this));
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
            get { throw ExceptionUtilities.Unreachable; }
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

        public override TypeSymbolWithAnnotations ReturnType
        {
            get { return _returnType; }
        }

        bool Cci.ISignature.ReturnValueIsByRef
        {
            get { return true; }
        }

        public override ImmutableArray<TypeSymbolWithAnnotations> TypeArguments
        {
            get { return _typeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations); }
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
            get { throw ExceptionUtilities.Unreachable; }
        }

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

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
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

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
