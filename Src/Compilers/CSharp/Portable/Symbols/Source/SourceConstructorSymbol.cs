// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceConstructorSymbol : SourceMethodSymbol
    {
        private ImmutableArray<ParameterSymbol> lazyParameters;
        private TypeSymbol lazyReturnType;
        private bool lazyIsVararg;

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
            base(containingType, syntax.GetReference(), syntax.Body.GetReferenceOrNull(), ImmutableArray.Create(location))
        {
            bool modifierErrors;
            var declarationModifiers = this.MakeModifiers(syntax.Modifiers, methodKind, location, diagnostics, out modifierErrors);
            this.flags = MakeFlags(methodKind, declarationModifiers, returnsVoid: true, isExtensionMethod: false);

            var bodyOpt = syntax.Body;
            if (bodyOpt != null)
            {
                if (IsExtern)
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
            this.lazyParameters = ParameterHelpers.MakeParameters(bodyBinder, this, parameterList, true, out arglistToken, diagnostics);
            this.lazyIsVararg = (arglistToken.CSharpKind() == SyntaxKind.ArgListKeyword);
            this.lazyReturnType = bodyBinder.GetSpecialType(SpecialType.System_Void, diagnostics, syntax);

            if (MethodKind == MethodKind.StaticConstructor && (lazyParameters.Length != 0))
            {
                diagnostics.Add(ErrorCode.ERR_StaticConstParam, Locations[0], this);
            }

            this.CheckEffectiveAccessibility(lazyReturnType, lazyParameters, diagnostics);

            if (this.lazyIsVararg && (IsGenericMethod || ContainingType.IsGenericType || this.lazyParameters.Length > 0 && this.lazyParameters[this.lazyParameters.Length - 1].IsParams))
            {
                diagnostics.Add(ErrorCode.ERR_BadVarargs, Locations[0]);
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
                return this.lazyIsVararg;
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
                if (!this.lazyParameters.IsDefault)
                {
                    return this.lazyParameters.Length;
                }

                return GetSyntax().ParameterList.ParameterCount;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                LazyMethodChecks();
                return this.lazyParameters;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        public override TypeSymbol ReturnType
        {
            get
            {
                LazyMethodChecks();
                return this.lazyReturnType;
            }
        }

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, MethodKind methodKind, Location location, DiagnosticBag diagnostics, out bool modifierErrors)
        {
            var defaultAccess = (methodKind == MethodKind.StaticConstructor) ? DeclarationModifiers.None : DeclarationModifiers.Private;

            // Check that the set of modifiers is allowed
            var allowedModifiers =
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

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            TextSpan span;

            // local defined within the body of the constructor:
            if (bodySyntaxReferenceOpt != null && localTree == bodySyntaxReferenceOpt.SyntaxTree)
            {
                span = bodySyntaxReferenceOpt.Span;
                if (span.Contains(localPosition))
                {
                    return localPosition - span.Start;
                }
            }

            // we haven't found the contructor part that declares the variable:
            throw ExceptionUtilities.Unreachable;
        }
    }
}
