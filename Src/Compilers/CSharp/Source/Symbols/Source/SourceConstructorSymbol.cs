// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceConstructorSymbol : SourceMethodSymbol
    {
        public static SourceConstructorSymbol CreateConstructorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            ConstructorDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
        {
            var methodKind = syntax.Modifiers.Any(SyntaxKind.StaticKeyword) ? MethodKind.StaticConstructor : MethodKind.Constructor;
            return new SourceConstructorSymbol(containingType, syntax.Identifier.GetLocation(), syntax, methodKind, diagnostics);
        }

        public static SourceConstructorSymbol CreatePrimaryConstructorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            ParameterListSyntax syntax,
            DiagnosticBag diagnostics)
        {
            return new SourceConstructorSymbol(containingType, syntax.GetLocation(), syntax, diagnostics);
        }

        private ImmutableArray<ParameterSymbol> lazyParameters;
        private TypeSymbol lazyReturnType;
        private bool lazyIsVararg;

        private SourceConstructorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            Location location,
            ParameterListSyntax syntax,
            DiagnosticBag diagnostics) :
            base(containingType, syntax.GetReference(), null, ImmutableArray.Create(location))
        {
            var declarationModifiers = (containingType.IsAbstract ? DeclarationModifiers.Protected : DeclarationModifiers.Public) | DeclarationModifiers.PrimaryCtor;
            this.flags = MakeFlags(MethodKind.Constructor, declarationModifiers, returnsVoid: true, isExtensionMethod: false);
            this.CheckModifiers(MethodKind.Constructor, location, diagnostics);
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
            var syntax = (CSharpSyntaxNode)syntaxReference.GetSyntax();
            var binderFactory = this.DeclaringCompilation.GetBinderFactory(syntaxReference.SyntaxTree);
            ParameterListSyntax parameterList;

            if (syntax.Kind == SyntaxKind.ParameterList)
            {
                // Primary constructor case
                parameterList = (ParameterListSyntax)syntax;
            }
            else
            {
                parameterList = ((ConstructorDeclarationSyntax)syntax).ParameterList;
            }

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

            if (this.IsPrimaryCtor)
            {
                string typeName = ContainingType.Name;

                foreach (var p in this.lazyParameters)
                {
                    if (typeName.Equals(p.Name))
                    {
                        diagnostics.Add(ErrorCode.ERR_PrimaryCtorParameterSameNameAsContainingType, p.Locations[0], ContainingType);
                        break;
                    }
                }
            }
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
                return IsPrimaryCtor ? true : base.IsImplicitlyDeclared;
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

                var syntax = (CSharpSyntaxNode)syntaxReference.GetSyntax();

                if (syntax.Kind == SyntaxKind.ParameterList)
                {
                    // Primary constructor
                    return ((ParameterListSyntax)syntax).ParameterCount;
                }

                return ((ConstructorDeclarationSyntax)syntax).ParameterList.ParameterCount;
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
            if (blockSyntaxReference == null && !IsExtern && !IsPrimaryCtor)
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
            if (this.IsPrimaryCtor)
            {
                if ((object)((SourceNamedTypeSymbol)ContainingType).PrimaryCtor == (object)this)
                {
                    // Main Primary Constructor gets its attributes from attributes on the type.
                    return OneOrMany.Create(((SourceNamedTypeSymbol)ContainingType).GetAttributeDeclarations());
                }

                // Non-Main Primary constructor doesn't have attributes.
                return OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));
            }

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
                if (this.IsPrimaryCtor && (object)((SourceNamedTypeSymbol)ContainingType).PrimaryCtor == (object)this)
                {
                    // Main Primary Constructor gets its attributes from attributes on the type.
                    return (SourceNamedTypeSymbol)ContainingType;
                }

                return base.AttributeOwner;
            }
        }
    }
}
