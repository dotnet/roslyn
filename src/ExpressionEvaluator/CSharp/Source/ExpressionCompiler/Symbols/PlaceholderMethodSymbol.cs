// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    /// <summary>
    /// Synthesized method that represents an intrinsic debugger method.
    /// </summary>
    internal sealed class PlaceholderMethodSymbol : MethodSymbol, Cci.ISignature
    {
        internal delegate ImmutableArray<TypeParameterSymbol> GetTypeParameters(PlaceholderMethodSymbol method);
        internal delegate ImmutableArray<ParameterSymbol> GetParameters(PlaceholderMethodSymbol method);
        internal delegate TypeSymbol GetReturnType(PlaceholderMethodSymbol method);

        private readonly NamedTypeSymbol _container;
        private readonly CSharpSyntaxNode _syntax;
        private readonly string _name;
        private readonly ImmutableArray<Location> _locations;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private readonly TypeSymbol _returnType;
        private readonly bool _returnValueIsByRef;

        internal PlaceholderMethodSymbol(
            NamedTypeSymbol container,
            CSharpSyntaxNode syntax,
            string name,
            TypeSymbol returnType,
            GetParameters getParameters) :
            this(container, syntax, name)
        {
            Debug.Assert(
                (returnType.SpecialType == SpecialType.System_Void) ||
                (returnType.SpecialType == SpecialType.System_Object) ||
                (returnType.Name == "Exception"));

            _typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            _returnType = returnType;
            _parameters = getParameters(this);
        }

        internal PlaceholderMethodSymbol(
            NamedTypeSymbol container,
            CSharpSyntaxNode syntax,
            string name,
            GetTypeParameters getTypeParameters,
            GetReturnType getReturnType,
            GetParameters getParameters,
            bool returnValueIsByRef) :
            this(container, syntax, name)
        {
            _typeParameters = getTypeParameters(this);
            _returnType = getReturnType(this);
            _parameters = getParameters(this);
            _returnValueIsByRef = returnValueIsByRef;
        }

        private PlaceholderMethodSymbol(
            NamedTypeSymbol container,
            CSharpSyntaxNode syntax,
            string name)
        {
            _container = container;
            _syntax = syntax;
            _name = name;
            _locations = ImmutableArray.Create(syntax.Location);
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
            get { return _locations; }
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
            get { return _returnType.SpecialType == SpecialType.System_Void; }
        }

        public override TypeSymbol ReturnType
        {
            get { return _returnType; }
        }

        bool Cci.ISignature.ReturnValueIsByRef
        {
            get { return _returnValueIsByRef; }
        }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get { return _typeParameters.Cast<TypeParameterSymbol, TypeSymbol>(); }
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

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var factory = new SyntheticBoundNodeFactory(this, _syntax, compilationState, diagnostics);
            factory.CurrentMethod = this;
            // The method body is "throw null;" although the body
            // is arbitrary since the method will not be invoked.
            var body = factory.Block(factory.ThrowNull());
            factory.CloseMethod(body);
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
