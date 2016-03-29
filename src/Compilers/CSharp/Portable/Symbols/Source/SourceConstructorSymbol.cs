// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceConstructorSymbol : SourceMethodSymbol
    {
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeSymbolWithAnnotations _lazyReturnType;
        private bool _lazyIsVararg;

        public static SourceConstructorSymbol CreateConstructorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            ConstructorDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
        {
            var methodKind = syntax.Modifiers.Any(SyntaxKind.StaticKeyword) ? MethodKind.StaticConstructor : MethodKind.Constructor;
            return new SourceConstructorSymbol(containingType, syntax.Identifier.GetLocation(), syntax, methodKind, diagnostics);
        }

        private SourceConstructorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            Location location,
            ConstructorDeclarationSyntax syntax,
            MethodKind methodKind,
            DiagnosticBag diagnostics) :
            base(containingType, syntax.GetReference(), syntax.Body?.GetReference(), ImmutableArray.Create(location))
        {
            bool modifierErrors;
            var declarationModifiers = this.MakeModifiers(syntax.Modifiers, methodKind, location, diagnostics, out modifierErrors);
            this.MakeFlags(methodKind, declarationModifiers, returnsVoid: true, isExtensionMethod: false);

            if (IsExtern)
            {
                if (methodKind == MethodKind.Constructor && syntax.Initializer != null)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternHasConstructorInitializer, location, this);
                }

                if (syntax.Body != null)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternHasBody, location, this);
                }
            }

            var info = ModifierUtils.CheckAccessibility(this.DeclarationModifiers);
            if (info != null)
            {
                diagnostics.Add(info, location);
            }

            if (!modifierErrors)
            {
                this.CheckModifiers(methodKind, location, diagnostics);
            }
        }

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            var syntax = GetSyntax();
            var binderFactory = this.DeclaringCompilation.GetBinderFactory(syntax.SyntaxTree);
            ParameterListSyntax parameterList = syntax.ParameterList;

            // NOTE: if we asked for the binder for the body of the constructor, we'd risk a stack overflow because
            // we might still be constructing the member list of the containing type.  However, getting the binder
            // for the parameters should be safe.
            var bodyBinder = binderFactory.GetBinder(parameterList).WithContainingMemberOrLambda(this);

            SyntaxToken arglistToken;
            _lazyParameters = ParameterHelpers.MakeParameters(bodyBinder, this, parameterList, true, out arglistToken, diagnostics, false);
            _lazyIsVararg = (arglistToken.Kind() == SyntaxKind.ArgListKeyword);
            _lazyReturnType = TypeSymbolWithAnnotations.Create(bodyBinder.GetSpecialType(SpecialType.System_Void, diagnostics, syntax));

            var location = this.Locations[0];
            if (MethodKind == MethodKind.StaticConstructor && (_lazyParameters.Length != 0))
            {
                diagnostics.Add(ErrorCode.ERR_StaticConstParam, location, this);
            }

            this.CheckEffectiveAccessibility(_lazyReturnType, _lazyParameters, diagnostics);

            if (_lazyIsVararg && (IsGenericMethod || ContainingType.IsGenericType || _lazyParameters.Length > 0 && _lazyParameters[_lazyParameters.Length - 1].IsParams))
            {
                diagnostics.Add(ErrorCode.ERR_BadVarargs, location);
            }
        }

        internal ConstructorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (ConstructorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        public override bool IsVararg
        {
            get
            {
                LazyMethodChecks();
                return _lazyIsVararg;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return base.IsImplicitlyDeclared;
            }
        }

        internal override int ParameterCount
        {
            get
            {
                if (!_lazyParameters.IsDefault)
                {
                    return _lazyParameters.Length;
                }

                return GetSyntax().ParameterList.ParameterCount;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                LazyMethodChecks();
                return _lazyParameters;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        internal override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override TypeSymbolWithAnnotations ReturnType
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnType;
            }
        }

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, MethodKind methodKind, Location location, DiagnosticBag diagnostics, out bool modifierErrors)
        {
            var defaultAccess = (methodKind == MethodKind.StaticConstructor) ? DeclarationModifiers.None : DeclarationModifiers.Private;

            // Check that the set of modifiers is allowed
            const DeclarationModifiers allowedModifiers =
                DeclarationModifiers.AccessibilityMask |
                DeclarationModifiers.Static |
                DeclarationModifiers.Extern |
                DeclarationModifiers.Unsafe;

            var mods = ModifierUtils.MakeAndCheckNontypeMemberModifiers(modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors);

            this.CheckUnsafeModifier(mods, diagnostics);

            if (methodKind == MethodKind.StaticConstructor)
            {
                if ((mods & DeclarationModifiers.AccessibilityMask) != 0)
                {
                    diagnostics.Add(ErrorCode.ERR_StaticConstructorWithAccessModifiers, location, this);
                    mods = mods & ~DeclarationModifiers.AccessibilityMask;
                    modifierErrors = true;
                }

                mods |= DeclarationModifiers.Private; // we mark static constructors private in the symbol table
            }

            return mods;
        }

        private void CheckModifiers(MethodKind methodKind, Location location, DiagnosticBag diagnostics)
        {
            if (bodySyntaxReferenceOpt == null && !IsExtern)
            {
                diagnostics.Add(ErrorCode.ERR_ConcreteMissingBody, location, this);
            }
            else if (ContainingType.IsSealed && this.DeclaredAccessibility.HasProtected() && !this.IsOverride)
            {
                diagnostics.Add(AccessCheck.GetProtectedMemberInSealedTypeError(ContainingType), location, this);
            }
            else if (ContainingType.IsStatic && methodKind == MethodKind.Constructor)
            {
                diagnostics.Add(ErrorCode.ERR_ConstructorInStaticClass, location);
            }
        }

        public override string Name
        {
            get { return this.IsStatic ? WellKnownMemberNames.StaticConstructorName : WellKnownMemberNames.InstanceConstructorName; }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            return OneOrMany.Create(((ConstructorDeclarationSyntax)this.SyntaxNode).AttributeLists);
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetReturnTypeAttributeDeclarations()
        {
            // constructors can't have return type attributes
            return OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));
        }

        protected override IAttributeTargetSymbol AttributeOwner
        {
            get
            {
                return base.AttributeOwner;
            }
        }

        internal override bool IsExpressionBodied
        {
            get { return false; }
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }

        internal override int CalculateLocalSyntaxOffset(int position, SyntaxTree tree)
        {
            Debug.Assert(position >= 0 && tree != null);

            TextSpan span;

            // local/lambda/closure defined within the body of the constructor:
            if (tree == bodySyntaxReferenceOpt?.SyntaxTree)
            {
                span = bodySyntaxReferenceOpt.Span;
                if (span.Contains(position))
                {
                    return position - span.Start;
                }
            }

            var ctorSyntax = GetSyntax();

            // closure in ctor initializer lifting its parameter(s) spans the constructor declaration:
            if (position == ctorSyntax.SpanStart)
            {
                // Use a constant that is distinct from any other syntax offset.
                // -1 works since a field initializer and a constructor declaration header can't squeeze into a single character.
                return -1;
            }

            // lambdas in ctor initializer:
            int ctorInitializerLength;
            var ctorInitializer = ctorSyntax.Initializer;
            if (tree == ctorInitializer?.SyntaxTree)
            {
                span = ctorInitializer.Span;
                ctorInitializerLength = span.Length;

                if (span.Contains(position))
                {
                    return -ctorInitializerLength + (position - span.Start);
                }
            }
            else
            {
                ctorInitializerLength = 0;
            }

            // lambdas in field/property initializers:
            int syntaxOffset;
            var containingType = (SourceNamedTypeSymbol)this.ContainingType;
            if (containingType.TryCalculateSyntaxOffsetOfPositionInInitializer(position, tree, this.IsStatic, ctorInitializerLength, out syntaxOffset))
            {
                return syntaxOffset;
            }

            // we haven't found the constructor part that declares the variable:
            throw ExceptionUtilities.Unreachable;
        }
    }
}
