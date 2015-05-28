// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class LocalFunctionMethodSymbol : MethodSymbol
    {
        private readonly Binder _binder;
        private readonly LocalFunctionStatementSyntax _syntax;
        private readonly Symbol _containingSymbol;
        private readonly DeclarationModifiers _declarationModifiers;
        private ImmutableArray<ParameterSymbol> _parameters;
        private ImmutableArray<TypeParameterSymbol> _typeParameters;
        private TypeSymbol _returnType;
        private TypeSymbol _iteratorElementType;
        private bool _isVararg;

        public LocalFunctionMethodSymbol(
            Binder binder,
            NamedTypeSymbol containingType,
            Symbol containingSymbol,
            LocalFunctionStatementSyntax syntax)
        {
            _binder = binder;
            _syntax = syntax;
            _containingSymbol = containingSymbol;

            _declarationModifiers = (_containingSymbol.IsStatic ? DeclarationModifiers.Static : 0) | syntax.Modifiers.ToDeclarationModifiers();
        }

        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return _containingSymbol;
            }
        }

        public override string Name
        {
            get
            {
                return _syntax.Identifier.ValueText;
            }
        }

        public SyntaxToken NameToken
        {
            get
            {
                return _syntax.Identifier;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create<Location>(_syntax.Identifier.GetLocation());
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray.Create<SyntaxReference>(_syntax.GetReference());
            }
        }

        public override bool IsVararg
        {
            get
            {
                EnsureLazyInitFinished();
                return _isVararg;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                EnsureLazyInitFinished();
                return _parameters;
            }
        }

        public override TypeSymbol ReturnType
        {
            get
            {
                EnsureLazyInitFinished();
                return _returnType;
            }
        }
        public override bool ReturnsVoid
        {
            get
            {
                EnsureLazyInitFinished();
                return _returnType.SpecialType == SpecialType.System_Void;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                // this if statement breaks a recursive loop, where Parameters needs TypeParameters to bind
                if (!_typeParameters.IsDefault)
                    return _typeParameters;
                EnsureLazyInitFinished();
                return _typeParameters;
            }
        }
        public override bool IsExtensionMethod
        {
            get
            {
                // It is an error to be an extension method, but we need to compute it to report it
                var firstParam = _syntax.ParameterList.Parameters.FirstOrDefault();
                return firstParam != null &&
                    !firstParam.IsArgList &&
                    firstParam.Modifiers.Any(SyntaxKind.ThisKeyword);
            }
        }

        internal override TypeSymbol IteratorElementType
        {
            get
            {
                return _iteratorElementType;
            }
            set
            {
                Debug.Assert((object)_iteratorElementType == null || _iteratorElementType == value);
                Interlocked.CompareExchange(ref _iteratorElementType, value, null);
            }
        }

        internal override bool GenerateDebugInfo => true;
        public override MethodKind MethodKind => MethodKind.LocalFunction;
        public override int Arity => TypeParameters.Length;
        internal override bool HasSpecialName => false;
        internal override MethodImplAttributes ImplementationAttributes => default(MethodImplAttributes);
        internal override bool HasDeclarativeSecurity => false;
        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => null;
        internal override bool RequiresSecurityObject => false;
        public override bool HidesBaseMethodsByName => false;
        public override ImmutableArray<TypeSymbol> TypeArguments => TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>();
        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;
        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => ImmutableArray<CustomModifier>.Empty;
        public override Symbol AssociatedSymbol => null;
        internal override CallingConvention CallingConvention => CallingConvention.Default;
        public override Accessibility DeclaredAccessibility => Accessibility.Private;
        public override bool IsAsync => (_declarationModifiers & DeclarationModifiers.Async) != 0;
        public override bool IsStatic => (_declarationModifiers & DeclarationModifiers.Static) != 0;
        public override bool IsVirtual => (_declarationModifiers & DeclarationModifiers.Virtual) != 0;
        public override bool IsOverride => (_declarationModifiers & DeclarationModifiers.Override) != 0;
        public override bool IsAbstract => (_declarationModifiers & DeclarationModifiers.Abstract) != 0;
        public override bool IsSealed => (_declarationModifiers & DeclarationModifiers.Sealed) != 0;
        public override bool IsExtern => (_declarationModifiers & DeclarationModifiers.Extern) != 0;
        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        public override DllImportData GetDllImportData() => null;
        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;
        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            // Local function symbols have no "this" parameter
            thisParameter = null;
            return true;
        }

        private void EnsureLazyInitFinished()
        {
            if (_returnType != null)
                return;
            DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
            MethodChecks(diagnostics, _binder);
            AddDeclarationDiagnostics(diagnostics);
            diagnostics.Free();
        }

        private void MethodChecks(DiagnosticBag diagnostics, Binder parameterBinder)
        {
            if (_syntax.TypeParameterListOpt != null)
            {
                parameterBinder = new WithMethodTypeParametersBinder(this, parameterBinder);
                _typeParameters = MakeTypeParameters(diagnostics);
            }
            else
            {
                _typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }
            SyntaxToken arglistToken;
            _parameters = ParameterHelpers.MakeParameters(parameterBinder, this, _syntax.ParameterList, true, out arglistToken, diagnostics);
            _isVararg = (arglistToken.Kind() == SyntaxKind.ArgListKeyword);
            _returnType = parameterBinder.BindType(_syntax.ReturnType, diagnostics);
            if (IsExtensionMethod)
            {
                diagnostics.Add(ErrorCode.ERR_BadExtensionAgg, Locations[0]);
            }
        }

        private ImmutableArray<TypeParameterSymbol> MakeTypeParameters(DiagnosticBag diagnostics)
        {
            var result = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            var typeParameters = _syntax.TypeParameterListOpt.Parameters;
            for (int ordinal = 0; ordinal < typeParameters.Count; ordinal++)
            {
                var parameter = typeParameters[ordinal];
                var identifier = parameter.Identifier;
                var location = identifier.GetLocation();
                var name = identifier.ValueText;

                // TODO: Add diagnostic checks for nested local functions (and containing method)
                if (name == this.Name)
                {
                    diagnostics.Add(ErrorCode.ERR_TypeVariableSameAsParent, location, name);
                }

                for (int i = 0; i < result.Count; i++)
                {
                    if (name == result[i].Name)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateTypeParameter, location, name);
                        break;
                    }
                }

                var tpEnclosing = ContainingType.FindEnclosingTypeParameter(name);
                if ((object)tpEnclosing != null)
                {
                    // Type parameter '{0}' has the same name as the type parameter from outer type '{1}'
                    diagnostics.Add(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, location, name, tpEnclosing.ContainingType);
                }

                var typeParameter = new LocalFunctionTypeParameterSymbol(
                        this,
                        name,
                        ordinal,
                        ImmutableArray.Create(location),
                        ImmutableArray.Create(parameter.GetReference()));

                result.Add(typeParameter);
            }

            return result.ToImmutableAndFree();
        }

        public override int GetHashCode()
        {
            // this is what lambdas do (do not use hashes of other fields)
            return _syntax.GetHashCode();
        }

        public sealed override bool Equals(object symbol)
        {
            if ((object)this == symbol) return true;

            var localFunction = symbol as LocalFunctionMethodSymbol;
            return (object)localFunction != null
                && localFunction._syntax == _syntax
                && localFunction.ReturnType == this.ReturnType
                && System.Linq.ImmutableArrayExtensions.SequenceEqual(localFunction.ParameterTypes, this.ParameterTypes)
                && System.Linq.ImmutableArrayExtensions.SequenceEqual(localFunction.TypeParameters, this.TypeParameters)
                && localFunction.IsVararg == this.IsVararg
                && Equals(localFunction.ContainingSymbol, this.ContainingSymbol);
        }
    }
}
