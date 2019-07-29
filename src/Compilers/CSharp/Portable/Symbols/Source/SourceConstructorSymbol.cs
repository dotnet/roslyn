// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceConstructorSymbol : SourceMemberMethodSymbol
    {
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeWithAnnotations _lazyReturnType;
        private bool _lazyIsVararg;
        private readonly bool _isExpressionBodied;

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
            base(containingType, syntax.GetReference(), ImmutableArray.Create(location))
        {
            bool hasBlockBody = syntax.Body != null;
            _isExpressionBodied = !hasBlockBody && syntax.ExpressionBody != null;
            bool hasBody = hasBlockBody || _isExpressionBodied;

            bool modifierErrors;
            var declarationModifiers = this.MakeModifiers(syntax.Modifiers, methodKind, hasBody, location, diagnostics, out modifierErrors);
            this.MakeFlags(methodKind, declarationModifiers, returnsVoid: true, isExtensionMethod: false);

            if (syntax.Identifier.ValueText != containingType.Name)
            {
                // This is probably a method declaration with the type missing.
                diagnostics.Add(ErrorCode.ERR_MemberNeedsType, location);
            }

            if (IsExtern)
            {
                if (methodKind == MethodKind.Constructor && syntax.Initializer != null)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternHasConstructorInitializer, location, this);
                }

                if (hasBody)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternHasBody, location, this);
                }
            }

            if (methodKind == MethodKind.StaticConstructor)
            {
                CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasBody, diagnostics);
            }

            var info = ModifierUtils.CheckAccessibility(this.DeclarationModifiers, this, isExplicitInterfaceImplementation: false);
            if (info != null)
            {
                diagnostics.Add(info, location);
            }

            if (!modifierErrors)
            {
                this.CheckModifiers(methodKind, hasBody, location, diagnostics);
            }

            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);
        }

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            var syntax = GetSyntax();
            var binderFactory = this.DeclaringCompilation.GetBinderFactory(syntax.SyntaxTree);
            ParameterListSyntax parameterList = syntax.ParameterList;

            // NOTE: if we asked for the binder for the body of the constructor, we'd risk a stack overflow because
            // we might still be constructing the member list of the containing type.  However, getting the binder
            // for the parameters should be safe.
            var bodyBinder = binderFactory.GetBinder(parameterList, syntax, this).WithContainingMemberOrLambda(this);

            // Constraint checking for parameter and return types must be delayed until
            // the method has been added to the containing type member list since
            // evaluating the constraints may depend on accessing this method from
            // the container (comparing this method to others to find overrides for
            // instance). Constraints are checked in AfterAddingTypeMembersChecks.
            var signatureBinder = bodyBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

            SyntaxToken arglistToken;
            _lazyParameters = ParameterHelpers.MakeParameters(
                signatureBinder, this, parameterList, out arglistToken,
                allowRefOrOut: true,
                allowThis: false,
                addRefReadOnlyModifier: false,
                diagnostics: diagnostics);

            _lazyIsVararg = (arglistToken.Kind() == SyntaxKind.ArgListKeyword);
            _lazyReturnType = TypeWithAnnotations.Create(bodyBinder.GetSpecialType(SpecialType.System_Void, diagnostics, syntax));

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

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            var compilation = DeclaringCompilation;
            ParameterHelpers.EnsureIsReadOnlyAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);
            ParameterHelpers.EnsureNullableAttributeExists(compilation, this, Parameters, diagnostics, modifyCompilation: true);

            foreach (var parameter in this.Parameters)
            {
                parameter.Type.CheckAllConstraints(compilation, conversions, parameter.Locations[0], diagnostics);
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

        public override ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses()
            => ImmutableArray<TypeParameterConstraintClause>.Empty;

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnType;
            }
        }

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, MethodKind methodKind, bool hasBody, Location location, DiagnosticBag diagnostics, out bool modifierErrors)
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

                if (this.ContainingType.IsInterface)
                {
                    ModifierUtils.ReportDefaultInterfaceImplementationModifiers(hasBody, mods,
                                                                                DeclarationModifiers.Extern,
                                                                                location, diagnostics);
                }
            }

            return mods;
        }

        private void CheckModifiers(MethodKind methodKind, bool hasBody, Location location, DiagnosticBag diagnostics)
        {
            if (!hasBody && !IsExtern)
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
            get
            {
                return _isExpressionBodied;
            }
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
            ConstructorDeclarationSyntax ctorSyntax = GetSyntax();
            if (tree == ctorSyntax.SyntaxTree)
            {
                if (ctorSyntax.Body?.Span.Contains(position) == true)
                {
                    return position - ctorSyntax.Body.Span.Start;
                }
                else if (ctorSyntax.ExpressionBody?.Span.Contains(position) == true)
                {
                    return position - ctorSyntax.ExpressionBody.Span.Start;
                }
            }

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
