// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class LocalFunctionMethodSymbol : SourceMethodSymbol
    {
        private readonly Binder _binder;
        private readonly LocalFunctionStatementSyntax _syntax;
        private ImmutableArray<ParameterSymbol> _parameters;
        private TypeSymbol _returnType;
        private bool _isVararg;

        public LocalFunctionMethodSymbol(
            Binder binder,
            NamedTypeSymbol containingType,
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

            // It is an error to be an extension method, but we need to compute it to report it
            var firstParam = syntax.ParameterList.Parameters.FirstOrDefault();
            bool isExtensionMethod = firstParam != null &&
                !firstParam.IsArgList &&
                firstParam.Modifiers.Any(SyntaxKind.ThisKeyword);

            this.MakeFlags(
                MethodKind.LocalFunction,
                DeclarationModifiers.Static | syntax.Modifiers.ToDeclarationModifiers(), // TODO: Will change when we allow local captures (also change in LocalFunctionRewriter)
                returnsVoid: false, // will be fixed in MethodChecks
                isExtensionMethod: isExtensionMethod);
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
                return ImmutableArray<TypeParameterSymbol>.Empty;
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

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            MethodChecks(diagnostics, _binder);
        }
    }
}
