// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceMemberMethodSymbol : SourceMethodSymbol
    {
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly TypeSymbol _explicitInterfaceType;
        private readonly string _name;
        private readonly bool _isExpressionBodied;
        private readonly RefKind _refKind;

        private ImmutableArray<MethodSymbol> _lazyExplicitInterfaceImplementations;
        private ImmutableArray<CustomModifier> _lazyReturnTypeCustomModifiers;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeSymbol _lazyReturnType;
        private bool _lazyIsVararg;

        /// <summary>
        /// A collection of type parameter constraints, populated when
        /// constraints for the first type parameter is requested.
        /// </summary>
        private ImmutableArray<TypeParameterConstraintClause> _lazyTypeParameterConstraints;

        /// <summary>
        /// If this symbol represents a partial method definition or implementation part, its other part (if any).
        /// This should be set, if at all, before this symbol appears among the members of its owner.  
        /// The implementation part is not listed among the "members" of the enclosing type.
        /// </summary>
        private SourceMemberMethodSymbol _otherPartOfPartial;

        /// <summary>
        /// A binder to use for binding generic constraints. The field is only non-null while the .ctor
        /// is executing, and allows constraints to be bound before the method is added to the
        /// containing type. (Until the method symbol has been added to the container, we cannot
        /// get a binder for the method without triggering a recursive attempt to bind the method.)
        /// </summary>
        private readonly Binder _constraintClauseBinder;

        public static SourceMemberMethodSymbol CreateMethodSymbol(
            NamedTypeSymbol containingType,
            Binder bodyBinder,
            MethodDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
        {
            var interfaceSpecifier = syntax.ExplicitInterfaceSpecifier;
            var nameToken = syntax.Identifier;

            TypeSymbol explicitInterfaceType;
            string discardedAliasQualifier;
            var name = ExplicitInterfaceHelpers.GetMemberNameAndInterfaceSymbol(bodyBinder, interfaceSpecifier, nameToken.ValueText, diagnostics, out explicitInterfaceType, out discardedAliasQualifier);
            var location = new SourceLocation(nameToken);

            var methodKind = interfaceSpecifier == null
                ? MethodKind.Ordinary
                : MethodKind.ExplicitInterfaceImplementation;

            return new SourceMemberMethodSymbol(containingType, explicitInterfaceType, name, location, bodyBinder, syntax, methodKind, diagnostics);
        }

        private SourceMemberMethodSymbol(
            NamedTypeSymbol containingType,
            TypeSymbol explicitInterfaceType,
            string name,
            Location location,
            Binder bodyBinder,
            MethodDeclarationSyntax syntax,
            MethodKind methodKind,
            DiagnosticBag diagnostics) :
            base(containingType,
                 syntax.GetReference(),
                 // Prefer a block body if both exist
                 syntax.Body?.GetReference() ?? syntax.ExpressionBody?.GetReference(),
                 location)
        {
            _name = name;
            _explicitInterfaceType = explicitInterfaceType;

            SyntaxTokenList modifiers = syntax.Modifiers;

            // The following two values are used to compute and store the initial value of the flags
            // However, these two components are placeholders; the correct value will be
            // computed lazily later and then the flags will be fixed up.
            const bool returnsVoid = false;

            var firstParam = syntax.ParameterList.Parameters.FirstOrDefault();
            bool isExtensionMethod = firstParam != null &&
                !firstParam.IsArgList &&
                firstParam.Modifiers.Any(SyntaxKind.ThisKeyword);

            bool modifierErrors;
            var declarationModifiers = this.MakeModifiers(modifiers, methodKind, location, diagnostics, out modifierErrors);

            var isMetadataVirtualIgnoringModifiers = (object)explicitInterfaceType != null; //explicit impls must be marked metadata virtual

            this.MakeFlags(methodKind, declarationModifiers, returnsVoid, isExtensionMethod, isMetadataVirtualIgnoringModifiers);

            // NOTE: by creating a WithMethodTypeParametersBinder, we are effectively duplicating the
            // functionality of the BinderFactory.  Unfortunately, we cannot use the BinderFactory
            // because it depends on having access to the member list of our containing type and
            // that list cannot be complete because we're not finished constructing this member.
            // TODO: at least keep this in sync with BinderFactory.VisitMethodDeclaration.
            bodyBinder = bodyBinder.WithUnsafeRegionIfNecessary(modifiers);

            Binder withTypeParamsBinder;
            if (syntax.Arity == 0)
            {
                withTypeParamsBinder = bodyBinder;
                _typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }
            else
            {
                var parameterBinder = new WithMethodTypeParametersBinder(this, bodyBinder);
                withTypeParamsBinder = parameterBinder;
                _typeParameters = MakeTypeParameters(syntax, diagnostics);
            }

            bool hasBlockBody = syntax.Body != null;
            _isExpressionBodied = !hasBlockBody && syntax.ExpressionBody != null;

            if (hasBlockBody || _isExpressionBodied)
            {
                CheckModifiersForBody(location, diagnostics);
            }

            _refKind = syntax.RefKeyword.Kind().GetRefKind();

            var info = ModifierUtils.CheckAccessibility(this.DeclarationModifiers);
            if (info != null)
            {
                diagnostics.Add(info, location);
            }

            if (this.IsPartial)
            {
                // Partial methods must be completed early because they are matched up
                // by signature while producing the enclosing type's member list. However,
                // that means any type parameter constraints will be bound before the method
                // is added to the containing type. To enable binding of constraints before the
                // .ctor completes we hold on to the current binder while the .ctor is executing.
                // If we change the handling of partial methods, so that partial methods are
                // completed lazily, the 'constraintClauseBinder' field should be removed.
                _constraintClauseBinder = withTypeParamsBinder;

                state.NotePartComplete(CompletionPart.StartMethodChecks);
                MethodChecks(syntax, withTypeParamsBinder, diagnostics);
                state.NotePartComplete(CompletionPart.FinishMethodChecks);
                _constraintClauseBinder = null;
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                LazyMethodChecks();
                return base.ReturnsVoid;
            }
        }

        private void MethodChecks(MethodDeclarationSyntax syntax, Binder withTypeParamsBinder, DiagnosticBag diagnostics)
        {
            SyntaxToken arglistToken;

            // Constraint checking for parameter and return types must be delayed until
            // the method has been added to the containing type member list since
            // evaluating the constraints may depend on accessing this method from
            // the container (comparing this method to others to find overrides for
            // instance). Constraints are checked in AfterAddingTypeMembersChecks.
            var signatureBinder = withTypeParamsBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

            _lazyParameters = ParameterHelpers.MakeParameters(signatureBinder, this, syntax.ParameterList, true, out arglistToken, diagnostics, false);
            _lazyIsVararg = (arglistToken.Kind() == SyntaxKind.ArgListKeyword);
            _lazyReturnType = signatureBinder.BindType(syntax.ReturnType, diagnostics);

            if (_lazyReturnType.IsRestrictedType())
            {
                if (_lazyReturnType.SpecialType == SpecialType.System_TypedReference &&
                    (this.ContainingType.SpecialType == SpecialType.System_TypedReference || this.ContainingType.SpecialType == SpecialType.System_ArgIterator))
                {
                    // Two special cases: methods in the special types TypedReference and ArgIterator are allowed to return TypedReference
                }
                else
                {
                    // Method or delegate cannot return type '{0}'
                    diagnostics.Add(ErrorCode.ERR_MethodReturnCantBeRefAny, syntax.ReturnType.Location, _lazyReturnType);
                }
            }

            var returnsVoid = _lazyReturnType.SpecialType == SpecialType.System_Void;
            if (this.RefKind != RefKind.None && returnsVoid)
            {
                diagnostics.Add(ErrorCode.ERR_VoidReturningMethodCannotReturnByRef, syntax.RefKeyword.GetLocation());
            }

            // set ReturnsVoid flag
            this.SetReturnsVoid(returnsVoid);

            var location = this.Locations[0];
            this.CheckEffectiveAccessibility(_lazyReturnType, _lazyParameters, diagnostics);

            // Checks taken from MemberDefiner::defineMethod
            if (this.Name == WellKnownMemberNames.DestructorName && this.ParameterCount == 0 && this.Arity == 0 && this.ReturnsVoid)
            {
                diagnostics.Add(ErrorCode.WRN_FinalizeMethod, location);
            }

            // errors relevant for extension methods
            if (IsExtensionMethod)
            {
                var parameter0Type = this.Parameters[0].Type;
                if (!parameter0Type.IsValidExtensionParameterType())
                {
                    // Duplicate Dev10 behavior by selecting the parameter type.
                    var parameterSyntax = syntax.ParameterList.Parameters[0];
                    Debug.Assert(parameterSyntax.Type != null);
                    var loc = parameterSyntax.Type.Location;
                    diagnostics.Add(ErrorCode.ERR_BadTypeforThis, loc, parameter0Type);
                }
                else if ((object)ContainingType.ContainingType != null)
                {
                    diagnostics.Add(ErrorCode.ERR_ExtensionMethodsDecl, location, ContainingType.Name);
                }
                else if (!ContainingType.IsScriptClass && !(ContainingType.IsStatic && ContainingType.Arity == 0))
                {
                    // Duplicate Dev10 behavior by selecting the containing type identifier. However if there
                    // is no containing type (in the interactive case for instance), select the method identifier.
                    var typeDecl = syntax.Parent as TypeDeclarationSyntax;
                    var identifier = (typeDecl != null) ? typeDecl.Identifier : syntax.Identifier;
                    var loc = identifier.GetLocation();
                    diagnostics.Add(ErrorCode.ERR_BadExtensionAgg, loc);
                }
                else if (!IsStatic)
                {
                    diagnostics.Add(ErrorCode.ERR_BadExtensionMeth, location);
                }
                else
                {
                    // Verify ExtensionAttribute is available.
                    var attributeConstructor = withTypeParamsBinder.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor);
                    if ((object)attributeConstructor == null)
                    {
                        var memberDescriptor = WellKnownMembers.GetDescriptor(WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor);
                        // do not use Binder.ReportUseSiteErrorForAttributeCtor in this case, because we'll need to report a special error id, not a generic use site error.
                        diagnostics.Add(
                            ErrorCode.ERR_ExtensionAttrNotFound,
                            syntax.ParameterList.Parameters[0].Modifiers.FirstOrDefault(SyntaxKind.ThisKeyword).GetLocation(),
                            memberDescriptor.DeclaringTypeMetadataName);
                    }
                }
            }

            if (this.MethodKind == MethodKind.UserDefinedOperator)
            {
                foreach (var p in this.Parameters)
                {
                    if (p.RefKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.ERR_IllegalRefParam, location);
                        break;
                    }
                }
            }
            else if (IsPartial)
            {
                // check that there are no out parameters in a partial
                foreach (var p in this.Parameters)
                {
                    if (p.RefKind == RefKind.Out)
                    {
                        diagnostics.Add(ErrorCode.ERR_PartialMethodCannotHaveOutParameters, location);
                        break;
                    }
                }

                if (MethodKind == MethodKind.ExplicitInterfaceImplementation)
                {
                    diagnostics.Add(ErrorCode.ERR_PartialMethodNotExplicit, location);
                }

                if (!ContainingType.IsPartial() || ContainingType.IsInterface)
                {
                    diagnostics.Add(ErrorCode.ERR_PartialMethodOnlyInPartialClass, location);
                }
            }

            if (!IsPartial)
            {
                LazyAsyncMethodChecks(CancellationToken.None);
                Debug.Assert(state.HasComplete(CompletionPart.FinishAsyncMethodChecks));
            }

            // The runtime will not treat this method as an override or implementation of another
            // method unless both the signatures and the custom modifiers match.  Hence, in the
            // case of overrides and *explicit* implementations, we need to copy the custom modifiers
            // that are in the signature of the overridden/implemented method.  (From source, we know
            // that there can only be one such method, so there are no conflicts.)  This is
            // unnecessary for implicit implementations because, if the custom modifiers don't match,
            // we'll insert a bridge method (an explicit implementation that delegates to the implicit
            // implementation) with the correct custom modifiers 
            // (see SourceNamedTypeSymbol.ImplementInterfaceMember).

            // Note: we're checking if the syntax indicates explicit implementation rather,
            // than if explicitInterfaceType is null because we don't want to look for an
            // overridden property if this is supposed to be an explicit implementation.
            if (syntax.ExplicitInterfaceSpecifier == null)
            {
                Debug.Assert(_lazyExplicitInterfaceImplementations.IsDefault);
                _lazyExplicitInterfaceImplementations = ImmutableArray<MethodSymbol>.Empty;

                // This value may not be correct, but we need something while we compute this.OverriddenMethod.
                // May be re-assigned below.
                Debug.Assert(_lazyReturnTypeCustomModifiers.IsDefault);
                _lazyReturnTypeCustomModifiers = ImmutableArray<CustomModifier>.Empty;

                // If this method is an override, we may need to copy custom modifiers from
                // the overridden method (so that the runtime will recognize it as an override).
                // We check for this case here, while we can still modify the parameters and
                // return type without losing the appearance of immutability.
                if (this.IsOverride)
                {
                    // This computation will necessarily be performed with partially incomplete
                    // information.  There is no way we can determine the complete signature
                    // (i.e. including custom modifiers) until we have found the method that
                    // this method overrides.  To accommodate this, MethodSymbol.OverriddenOrHiddenMembers
                    // is written to allow relaxed matching of custom modifiers for source methods,
                    // on the assumption that they will be updated appropriately.
                    MethodSymbol overriddenMethod = this.OverriddenMethod;

                    if ((object)overriddenMethod != null)
                    {
                        CustomModifierUtils.CopyMethodCustomModifiers(overriddenMethod, this, out _lazyReturnType, out _lazyReturnTypeCustomModifiers, out _lazyParameters, alsoCopyParamsModifier: true);
                    }
                }
            }
            else if ((object)_explicitInterfaceType != null)
            {
                //do this last so that it can assume the method symbol is constructed (except for ExplicitInterfaceImplementation)
                MethodSymbol implementedMethod = this.FindExplicitlyImplementedMethod(_explicitInterfaceType, syntax.Identifier.ValueText, syntax.ExplicitInterfaceSpecifier, diagnostics);

                if ((object)implementedMethod != null)
                {
                    Debug.Assert(_lazyExplicitInterfaceImplementations.IsDefault);
                    _lazyExplicitInterfaceImplementations = ImmutableArray.Create<MethodSymbol>(implementedMethod);

                    CustomModifierUtils.CopyMethodCustomModifiers(implementedMethod, this, out _lazyReturnType, out _lazyReturnTypeCustomModifiers, out _lazyParameters, alsoCopyParamsModifier: false);
                }
                else
                {
                    Debug.Assert(_lazyExplicitInterfaceImplementations.IsDefault);
                    _lazyExplicitInterfaceImplementations = ImmutableArray<MethodSymbol>.Empty;

                    Debug.Assert(_lazyReturnTypeCustomModifiers.IsDefault);
                    _lazyReturnTypeCustomModifiers = ImmutableArray<CustomModifier>.Empty;
                }
            }

            CheckModifiers(location, diagnostics);
        }

        // This is also used for async lambdas.  Probably not the best place to locate this method, but where else could it go?
        internal static void ReportAsyncParameterErrors(MethodSymbol method, DiagnosticBag diagnostics, Location location)
        {
            if (method.IsAsync)
            {
                foreach (var parameter in method.Parameters)
                {
                    var loc = parameter.Locations.Any() ? parameter.Locations[0] : location;
                    if (parameter.RefKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.ERR_BadAsyncArgType, loc);
                    }
                    else if (parameter.Type.IsUnsafe())
                    {
                        diagnostics.Add(ErrorCode.ERR_UnsafeAsyncArgType, loc);
                    }
                    else if (parameter.Type.IsRestrictedType())
                    {
                        diagnostics.Add(ErrorCode.ERR_BadSpecialByRefLocal, loc, parameter.Type);
                    }
                }
            }
        }

        protected sealed override void LazyAsyncMethodChecks(CancellationToken cancellationToken)
        {
            Debug.Assert(this.IsPartial == state.HasComplete(CompletionPart.FinishMethodChecks),
                "Partial methods complete method checks during construction.  " +
                "Other methods can't complete method checks before executing this method.");

            if (!this.IsAsync)
            {
                if (state.NotePartComplete(CompletionPart.StartAsyncMethodChecks))
                {
                    if (IsPartialDefinition && (object)PartialImplementationPart == null)
                    {
                        DeclaringCompilation.SymbolDeclaredEvent(this);
                    }

                    state.NotePartComplete(CompletionPart.FinishAsyncMethodChecks);
                }
                else
                {
                    state.SpinWaitComplete(CompletionPart.FinishAsyncMethodChecks, cancellationToken);
                }

                return;
            }

            DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
            Location errorLocation = this.Locations[0];

            Debug.Assert(this.RefKind == RefKind.None);
            if (!this.IsGenericTaskReturningAsync(this.DeclaringCompilation) && !this.IsTaskReturningAsync(this.DeclaringCompilation) && !this.IsVoidReturningAsync())
            {
                // The return type of an async method must be void, Task or Task<T>
                diagnostics.Add(ErrorCode.ERR_BadAsyncReturn, errorLocation);
            }

            for (NamedTypeSymbol curr = this.ContainingType; (object)curr != null; curr = curr.ContainingType)
            {
                var sourceNamedTypeSymbol = curr as SourceNamedTypeSymbol;
                if ((object)sourceNamedTypeSymbol != null && sourceNamedTypeSymbol.HasSecurityCriticalAttributes)
                {
                    diagnostics.Add(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct, errorLocation);
                    break;
                }
            }

            if ((this.ImplementationAttributes & System.Reflection.MethodImplAttributes.Synchronized) != 0)
            {
                diagnostics.Add(ErrorCode.ERR_SynchronizedAsyncMethod, errorLocation);
            }

            if (diagnostics.IsEmptyWithoutResolution)
            {
                ReportAsyncParameterErrors(this, diagnostics, errorLocation);
            }

            if (state.NotePartComplete(CompletionPart.StartAsyncMethodChecks))
            {
                AddDeclarationDiagnostics(diagnostics);
                if (IsPartialDefinition) DeclaringCompilation.SymbolDeclaredEvent(this);
                state.NotePartComplete(CompletionPart.FinishAsyncMethodChecks);
            }
            else
            {
                state.SpinWaitComplete(CompletionPart.FinishAsyncMethodChecks, cancellationToken);
            }

            diagnostics.Free();
        }

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            var syntax = GetSyntax();
            var withTypeParamsBinder = this.DeclaringCompilation.GetBinderFactory(syntax.SyntaxTree).GetBinder(syntax.ReturnType);
            MethodChecks(syntax, withTypeParamsBinder, diagnostics);
        }

        internal MethodDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (MethodDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        internal TypeParameterConstraintKind GetTypeParameterConstraints(int ordinal)
        {
            var clause = this.GetTypeParameterConstraintClause(ordinal);
            return (clause != null) ? clause.Constraints : TypeParameterConstraintKind.None;
        }

        internal ImmutableArray<TypeSymbol> GetTypeParameterConstraintTypes(int ordinal)
        {
            var clause = this.GetTypeParameterConstraintClause(ordinal);
            return (clause != null) ? clause.ConstraintTypes : ImmutableArray<TypeSymbol>.Empty;
        }

        private TypeParameterConstraintClause GetTypeParameterConstraintClause(int ordinal)
        {
            if (_lazyTypeParameterConstraints.IsDefault)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                if (ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameterConstraints, MakeTypeParameterConstraints(diagnostics)))
                {
                    this.AddDeclarationDiagnostics(diagnostics);
                }
                diagnostics.Free();
            }

            var clauses = _lazyTypeParameterConstraints;
            return (clauses.Length > 0) ? clauses[ordinal] : null;
        }

        private ImmutableArray<TypeParameterConstraintClause> MakeTypeParameterConstraints(DiagnosticBag diagnostics)
        {
            var typeParameters = this.TypeParameters;
            if (typeParameters.Length == 0)
            {
                return ImmutableArray<TypeParameterConstraintClause>.Empty;
            }

            var syntax = GetSyntax();
            var constraintClauses = syntax.ConstraintClauses;
            if (constraintClauses.Count == 0)
            {
                return ImmutableArray<TypeParameterConstraintClause>.Empty;
            }

            var syntaxTree = syntax.SyntaxTree;

            // If we're binding these constraints before the method has been
            // fully constructed (see partial method comment in .ctor), we have
            // a binder. Otherwise, lookup the binder in the BinderFactory.
            var binder = _constraintClauseBinder;
            if (binder == null)
            {
                var compilation = this.DeclaringCompilation;
                var binderFactory = compilation.GetBinderFactory(syntaxTree);
                binder = binderFactory.GetBinder(constraintClauses[0]);
            }

            // Wrap binder from factory in a generic constraints specific binder
            // to avoid checking constraints when binding type names.
            Debug.Assert(!binder.Flags.Includes(BinderFlags.GenericConstraintsClause));
            binder = binder.WithAdditionalFlags(BinderFlags.GenericConstraintsClause | BinderFlags.SuppressConstraintChecks);

            var result = binder.BindTypeParameterConstraintClauses(this, typeParameters, constraintClauses, diagnostics);
            this.CheckConstraintTypesVisibility(new SourceLocation(syntax.Identifier), result, diagnostics);
            return result;
        }

        public override bool IsVararg
        {
            get
            {
                LazyMethodChecks();
                return _lazyIsVararg;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.locations;
            }
        }

        internal override int ParameterCount
        {
            get
            {
                return !_lazyParameters.IsDefault ? _lazyParameters.Length : GetSyntax().ParameterList.ParameterCount;
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

        internal override RefKind RefKind
        {
            get { return _refKind; }
        }

        public override TypeSymbol ReturnType
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnType;
            }
        }

        internal static void InitializePartialMethodParts(SourceMemberMethodSymbol definition, SourceMemberMethodSymbol implementation)
        {
            Debug.Assert(definition.IsPartialDefinition);
            Debug.Assert(implementation.IsPartialImplementation);
            Debug.Assert((object)definition._otherPartOfPartial == null);
            Debug.Assert((object)implementation._otherPartOfPartial == null);

            definition._otherPartOfPartial = implementation;
            implementation._otherPartOfPartial = definition;
        }

        /// <summary>
        /// If this is a partial implementation part returns the definition part and vice versa.
        /// </summary>
        internal SourceMemberMethodSymbol OtherPartOfPartial
        {
            get { return _otherPartOfPartial; }
        }

        /// <summary>
        /// Returns true if this symbol represents a partial method definition (the part that specifies a signature but no body).
        /// </summary>
        internal bool IsPartialDefinition
        {
            get
            {
                return this.IsPartial
                    && this.BodySyntax == null;
            }
        }

        /// <summary>
        /// Returns true if this symbol represents a partial method implementation (the part that specifies both signature and body).
        /// </summary>
        internal bool IsPartialImplementation
        {
            get
            {
                return this.IsPartial
                    && this.BodySyntax != null;
            }
        }

        /// <summary>
        /// True if this is a partial method that doesn't have an implementation part.
        /// </summary>
        internal bool IsPartialWithoutImplementation
        {
            get
            {
                return this.IsPartialDefinition && (object)_otherPartOfPartial == null;
            }
        }

        /// <summary>
        /// Returns the implementation part of a partial method definition, 
        /// or null if this is not a partial method or it is the definition part.
        /// </summary>
        internal SourceMemberMethodSymbol SourcePartialDefinition
        {
            get
            {
                return this.IsPartialImplementation ? _otherPartOfPartial : null;
            }
        }

        /// <summary>
        /// Returns the definition part of a partial method implementation, 
        /// or null if this is not a partial method or it is the implementation part.
        /// </summary>
        internal SourceMemberMethodSymbol SourcePartialImplementation
        {
            get
            {
                return this.IsPartialDefinition ? _otherPartOfPartial : null;
            }
        }

        public override MethodSymbol PartialDefinitionPart
        {
            get
            {
                return SourcePartialDefinition;
            }
        }

        public override MethodSymbol PartialImplementationPart
        {
            get
            {
                return SourcePartialImplementation;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this.SourcePartialImplementation ?? this, expandIncludes, ref lazyDocComment);
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get
            {
                return this.GetSyntax().ExplicitInterfaceSpecifier != null;
            }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                LazyMethodChecks();
                return _lazyExplicitInterfaceImplementations;
            }
        }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnTypeCustomModifiers;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        protected override SourceMethodSymbol BoundAttributesSource
        {
            get
            {
                return this.SourcePartialDefinition;
            }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            if ((object)this.SourcePartialImplementation != null)
            {
                return OneOrMany.Create(ImmutableArray.Create(AttributeDeclarationSyntaxList, this.SourcePartialImplementation.AttributeDeclarationSyntaxList));
            }
            else
            {
                return OneOrMany.Create(AttributeDeclarationSyntaxList);
            }
        }

        private SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
        {
            get
            {
                var sourceContainer = this.ContainingType as SourceMemberContainerTypeSymbol;
                if ((object)sourceContainer != null && sourceContainer.AnyMemberHasAttributes)
                {
                    return this.GetSyntax().AttributeLists;
                }

                return default(SyntaxList<AttributeListSyntax>);
            }
        }

        internal override bool IsExpressionBodied
        {
            get { return _isExpressionBodied; }
        }

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, MethodKind methodKind, Location location, DiagnosticBag diagnostics, out bool modifierErrors)
        {
            bool isInterface = this.ContainingType.IsInterface;
            var defaultAccess = isInterface ? DeclarationModifiers.Public : DeclarationModifiers.Private;

            // Check that the set of modifiers is allowed
            var allowedModifiers = DeclarationModifiers.Partial | DeclarationModifiers.Unsafe;

            if (methodKind != MethodKind.ExplicitInterfaceImplementation)
            {
                allowedModifiers |= DeclarationModifiers.New;

                if (!isInterface)
                {
                    allowedModifiers |=
                        DeclarationModifiers.AccessibilityMask |
                        DeclarationModifiers.Sealed |
                        DeclarationModifiers.Abstract |
                        DeclarationModifiers.Static |
                        DeclarationModifiers.Virtual |
                        DeclarationModifiers.Override;
                }
            }

            if (!isInterface)
            {
                allowedModifiers |= DeclarationModifiers.Extern |
                    DeclarationModifiers.Async;
            }

            var mods = ModifierUtils.MakeAndCheckNontypeMemberModifiers(modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors);

            this.CheckUnsafeModifier(mods, diagnostics);

            mods = AddImpliedModifiers(mods, isInterface, methodKind);
            return mods;
        }

        private static DeclarationModifiers AddImpliedModifiers(DeclarationModifiers mods, bool containingTypeIsInterface, MethodKind methodKind)
        {
            // Let's overwrite modifiers for interface and explicit interface implementation methods with what they are supposed to be. 
            // Proper errors must have been reported by now.
            if (containingTypeIsInterface)
            {
                mods = (mods & ~DeclarationModifiers.AccessibilityMask) | DeclarationModifiers.Public | DeclarationModifiers.Abstract;
            }
            else if (methodKind == MethodKind.ExplicitInterfaceImplementation)
            {
                mods = (mods & ~DeclarationModifiers.AccessibilityMask) | DeclarationModifiers.Private;
            }
            return mods;
        }

        private ImmutableArray<TypeParameterSymbol> MakeTypeParameters(MethodDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax.TypeParameterList != null);

            OverriddenMethodTypeParameterMapBase typeMap = null;
            if (this.IsOverride)
            {
                typeMap = new OverriddenMethodTypeParameterMap(this);
            }
            else if (this.IsExplicitInterfaceImplementation)
            {
                typeMap = new ExplicitInterfaceMethodTypeParameterMap(this);
            }

            var typeParameters = syntax.TypeParameterList.Parameters;
            var result = ArrayBuilder<TypeParameterSymbol>.GetInstance();

            for (int ordinal = 0; ordinal < typeParameters.Count; ordinal++)
            {
                var parameter = typeParameters[ordinal];
                var identifier = parameter.Identifier;
                var location = identifier.GetLocation();
                var name = identifier.ValueText;

                // Note: It is not an error to have a type parameter named the same as its enclosing method: void M<M>() {}

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

                var syntaxRefs = ImmutableArray.Create(parameter.GetReference());
                var locations = ImmutableArray.Create(location);
                var typeParameter = (typeMap != null) ?
                    (TypeParameterSymbol)new SourceOverridingMethodTypeParameterSymbol(
                        typeMap,
                        name,
                        ordinal,
                        locations,
                        syntaxRefs) :
                    new SourceMethodTypeParameterSymbol(
                        this,
                        name,
                        ordinal,
                        locations,
                        syntaxRefs);

                result.Add(typeParameter);
            }

            return result.ToImmutableAndFree();
        }

        private void CheckModifiers(Location location, DiagnosticBag diagnostics)
        {
            const DeclarationModifiers partialMethodInvalidModifierMask = (DeclarationModifiers.AccessibilityMask & ~DeclarationModifiers.Private) |
                     DeclarationModifiers.Virtual |
                     DeclarationModifiers.Abstract |
                     DeclarationModifiers.Override |
                     DeclarationModifiers.New |
                     DeclarationModifiers.Sealed |
                     DeclarationModifiers.Extern;

            if (IsPartial && !ReturnsVoid)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodMustReturnVoid, location);
            }
            else if (IsPartial && !ContainingType.IsInterface && (DeclarationModifiers & partialMethodInvalidModifierMask) != 0)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodInvalidModifier, location);
            }
            else if (this.DeclaredAccessibility == Accessibility.Private && (IsVirtual || IsAbstract || IsOverride))
            {
                diagnostics.Add(ErrorCode.ERR_VirtualPrivate, location, this);
            }
            else if (IsStatic && (IsOverride || IsVirtual || IsAbstract))
            {
                // A static member '{0}' cannot be marked as override, virtual, or abstract
                diagnostics.Add(ErrorCode.ERR_StaticNotVirtual, location, this);
            }
            else if (IsOverride && (IsNew || IsVirtual))
            {
                // A member '{0}' marked as override cannot be marked as new or virtual
                diagnostics.Add(ErrorCode.ERR_OverrideNotNew, location, this);
            }
            else if (IsSealed && !IsOverride)
            {
                // '{0}' cannot be sealed because it is not an override
                diagnostics.Add(ErrorCode.ERR_SealedNonOverride, location, this);
            }
            else if (!ContainingType.IsInterfaceType() && _lazyReturnType.IsStatic)
            {
                // '{0}': static types cannot be used as return types
                diagnostics.Add(ErrorCode.ERR_ReturnTypeIsStaticClass, location, _lazyReturnType);
            }
            else if (IsAbstract && IsExtern)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractAndExtern, location, this);
            }
            else if (IsAbstract && IsSealed)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractAndSealed, location, this);
            }
            else if (IsAbstract && IsVirtual)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractNotVirtual, location, this);
            }
            else if (IsAbstract && ContainingType.TypeKind == TypeKind.Struct)
            {
                // The modifier '{0}' is not valid for this item
                diagnostics.Add(ErrorCode.ERR_BadMemberFlag, location, SyntaxFacts.GetText(SyntaxKind.AbstractKeyword));
            }
            else if (IsVirtual && ContainingType.TypeKind == TypeKind.Struct)
            {
                // The modifier '{0}' is not valid for this item
                diagnostics.Add(ErrorCode.ERR_BadMemberFlag, location, SyntaxFacts.GetText(SyntaxKind.VirtualKeyword));
            }
            else if (IsAbstract && !ContainingType.IsAbstract && (ContainingType.TypeKind == TypeKind.Class || ContainingType.TypeKind == TypeKind.Submission))
            {
                // '{0}' is abstract but it is contained in non-abstract class '{1}'
                diagnostics.Add(ErrorCode.ERR_AbstractInConcreteClass, location, this, ContainingType);
            }
            else if (IsVirtual && ContainingType.IsSealed)
            {
                // '{0}' is a new virtual member in sealed class '{1}'
                diagnostics.Add(ErrorCode.ERR_NewVirtualInSealed, location, this, ContainingType);
            }
            else if (bodySyntaxReferenceOpt == null && IsAsync)
            {
                diagnostics.Add(ErrorCode.ERR_BadAsyncLacksBody, location);
            }
            else if (bodySyntaxReferenceOpt == null && !IsExtern && !IsAbstract && !IsPartial && !IsExpressionBodied)
            {
                diagnostics.Add(ErrorCode.ERR_ConcreteMissingBody, location, this);
            }
            else if (ContainingType.IsSealed && this.DeclaredAccessibility.HasProtected() && !this.IsOverride)
            {
                diagnostics.Add(AccessCheck.GetProtectedMemberInSealedTypeError(ContainingType), location, this);
            }
            else if (ContainingType.IsStatic && !IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_InstanceMemberInStaticClass, location, Name);
            }
            else if (_lazyIsVararg && (IsGenericMethod || ContainingType.IsGenericType || _lazyParameters.Length > 0 && _lazyParameters[_lazyParameters.Length - 1].IsParams))
            {
                diagnostics.Add(ErrorCode.ERR_BadVarargs, location);
            }
            else if (_lazyIsVararg && IsAsync)
            {
                diagnostics.Add(ErrorCode.ERR_VarargsAsync, location);
            }
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            if (this.IsExtensionMethod)
            {
                // No need to check if [Extension] attribute was explicitly set since
                // we'll issue CS1112 error in those cases and won't generate IL.
                var compilation = this.DeclaringCompilation;

                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor));
            }
        }

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            var implementingPart = this.SourcePartialImplementation;
            if ((object)implementingPart != null)
            {
                implementingPart.ForceComplete(locationOpt, cancellationToken);
            }

            base.ForceComplete(locationOpt, cancellationToken);
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
            // Check constraints on return type and parameters. Note: Dev10 uses the
            // method name location for any such errors. We'll do the same for return
            // type errors but for parameter errors, we'll use the parameter location.

            if ((object)_explicitInterfaceType != null)
            {
                var syntax = this.GetSyntax();
                Debug.Assert(syntax.ExplicitInterfaceSpecifier != null);
                _explicitInterfaceType.CheckAllConstraints(conversions, new SourceLocation(syntax.ExplicitInterfaceSpecifier.Name), diagnostics);
            }

            this.ReturnType.CheckAllConstraints(conversions, this.Locations[0], diagnostics);

            foreach (var parameter in this.Parameters)
            {
                parameter.Type.CheckAllConstraints(conversions, parameter.Locations[0], diagnostics);
            }

            var implementingPart = this.SourcePartialImplementation;
            if ((object)implementingPart != null)
            {
                PartialMethodChecks(this, implementingPart, diagnostics);
            }
        }

        /// <summary>
        /// Report differences between the defining and implementing
        /// parts of a partial method. Diagnostics are reported on the
        /// implementing part, matching Dev10 behavior.
        /// </summary>
        private static void PartialMethodChecks(SourceMemberMethodSymbol definition, SourceMemberMethodSymbol implementation, DiagnosticBag diagnostics)
        {
            Debug.Assert(!ReferenceEquals(definition, implementation));

            if (definition.IsStatic != implementation.IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodStaticDifference, implementation.Locations[0]);
            }

            if (definition.IsExtensionMethod != implementation.IsExtensionMethod)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodExtensionDifference, implementation.Locations[0]);
            }

            if (definition.IsUnsafe != implementation.IsUnsafe && definition.CompilationAllowsUnsafe()) // Don't cascade.
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodUnsafeDifference, implementation.Locations[0]);
            }

            if (definition.IsParams() != implementation.IsParams())
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodParamsDifference, implementation.Locations[0]);
            }

            if (!HaveSameConstraints(definition, implementation))
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodInconsistentConstraints, implementation.Locations[0], implementation);
            }
        }

        /// <summary>
        /// Returns true if the two partial methods have the same constraints.
        /// </summary>
        private static bool HaveSameConstraints(SourceMemberMethodSymbol part1, SourceMemberMethodSymbol part2)
        {
            Debug.Assert(!ReferenceEquals(part1, part2));
            Debug.Assert(part1.Arity == part2.Arity);

            var typeParameters1 = part1.TypeParameters;

            int arity = typeParameters1.Length;
            if (arity == 0)
            {
                return true;
            }

            var typeParameters2 = part2.TypeParameters;
            var indexedTypeParameters = IndexedTypeParameterSymbol.Take(arity);
            var typeMap1 = new TypeMap(typeParameters1, indexedTypeParameters, allowAlpha: true);
            var typeMap2 = new TypeMap(typeParameters2, indexedTypeParameters, allowAlpha: true);

            return MemberSignatureComparer.HaveSameConstraints(typeParameters1, typeMap1, typeParameters2, typeMap2);
        }

        internal override bool CallsAreOmitted(SyntaxTree syntaxTree)
        {
            if (this.IsPartialWithoutImplementation)
            {
                return true;
            }

            return base.CallsAreOmitted(syntaxTree);
        }

        internal override bool GenerateDebugInfo => !IsAsync && !IsIterator;
    }
}
