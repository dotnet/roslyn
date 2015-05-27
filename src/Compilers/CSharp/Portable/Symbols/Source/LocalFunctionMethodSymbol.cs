// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class LocalFunctionMethodSymbol : SourceMethodSymbol
    {
        private readonly Binder _binder;
        private readonly LocalFunctionStatementSyntax _syntax;
        private readonly Symbol _containingSymbol;
        private ImmutableArray<ParameterSymbol> _parameters;
        private ImmutableArray<TypeParameterSymbol> _typeParameters;
        private TypeSymbol _returnType;
        private bool _isVararg;

        public LocalFunctionMethodSymbol(
            Binder binder,
            NamedTypeSymbol containingType,
            Symbol containingSymbol,
            LocalFunctionStatementSyntax syntax,
            Location location) :
            base(
                  containingType,
                  syntax.GetReference(),
                  syntax.Body?.GetReference() ?? syntax.ExpressionBody?.GetReference(),
                  location)
        {
            _binder = binder;
            _syntax = syntax;
            _containingSymbol = containingSymbol;

            // It is an error to be an extension method, but we need to compute it to report it
            var firstParam = syntax.ParameterList.Parameters.FirstOrDefault();
            bool isExtensionMethod = firstParam != null &&
                !firstParam.IsArgList &&
                firstParam.Modifiers.Any(SyntaxKind.ThisKeyword);

            this.MakeFlags(
                MethodKind.LocalFunction,
                (_containingSymbol.IsStatic ? DeclarationModifiers.Static : 0) | syntax.Modifiers.ToDeclarationModifiers(),
                returnsVoid: false, // will be fixed in MethodChecks
                isExtensionMethod: isExtensionMethod);
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

        internal override bool GenerateDebugInfo
        {
            get
            {
                return true;
            }
        }

        internal override bool IsExpressionBodied
        {
            get
            {
                return _syntax.ExpressionBody != null;
            }
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
            this.SetReturnsVoid(_returnType.SpecialType == SpecialType.System_Void);
            this.CheckEffectiveAccessibility(_returnType, _parameters, diagnostics);
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
                        locations,
                        ImmutableArray.Create(parameter.GetReference()));

                result.Add(typeParameter);
            }

            return result.ToImmutableAndFree();
        }

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            MethodChecks(diagnostics, _binder);
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
