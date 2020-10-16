﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SourcePropertyAccessorSymbol : SourceMemberMethodSymbol
    {
        private readonly SourcePropertySymbolBase _property;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeWithAnnotations _lazyReturnType;
        private ImmutableArray<CustomModifier> _lazyRefCustomModifiers;
        private readonly ImmutableArray<MethodSymbol> _explicitInterfaceImplementations;
        private readonly string _name;
        private readonly bool _isAutoPropertyAccessor;
        private readonly bool _isExpressionBodied;
        private readonly bool _usesInit;

        public static SourcePropertyAccessorSymbol CreateAccessorSymbol(
            NamedTypeSymbol containingType,
            SourcePropertySymbol property,
            DeclarationModifiers propertyModifiers,
            string propertyName,
            AccessorDeclarationSyntax syntax,
            PropertySymbol explicitlyImplementedPropertyOpt,
            string aliasQualifierOpt,
            bool isAutoPropertyAccessor,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax.Kind() == SyntaxKind.GetAccessorDeclaration ||
                syntax.Kind() == SyntaxKind.SetAccessorDeclaration ||
                syntax.Kind() == SyntaxKind.InitAccessorDeclaration);

            bool isGetMethod = (syntax.Kind() == SyntaxKind.GetAccessorDeclaration);
            string name;
            ImmutableArray<MethodSymbol> explicitInterfaceImplementations;
            GetNameAndExplicitInterfaceImplementations(
                explicitlyImplementedPropertyOpt,
                propertyName,
                property.IsCompilationOutputWinMdObj(),
                aliasQualifierOpt,
                isGetMethod,
                out name,
                out explicitInterfaceImplementations);

            var methodKind = isGetMethod ? MethodKind.PropertyGet : MethodKind.PropertySet;

            bool hasBody = syntax.Body is object;
            bool hasExpressionBody = syntax.ExpressionBody is object;
            CheckForBlockAndExpressionBody(syntax.Body, syntax.ExpressionBody, syntax, diagnostics);
            return new SourcePropertyAccessorSymbol(
                containingType,
                name,
                property,
                propertyModifiers,
                explicitInterfaceImplementations,
                syntax.Keyword.GetLocation(),
                syntax,
                hasBody,
                hasExpressionBody,
                isIterator: SyntaxFacts.HasYieldOperations(syntax.Body),
                syntax.Modifiers,
                methodKind,
                syntax.Keyword.IsKind(SyntaxKind.InitKeyword),
                isAutoPropertyAccessor,
                isExplicitInterfaceImplementation,
                diagnostics);
        }

        public static SourcePropertyAccessorSymbol CreateAccessorSymbol(
            NamedTypeSymbol containingType,
            SourcePropertySymbol property,
            DeclarationModifiers propertyModifiers,
            string propertyName,
            ArrowExpressionClauseSyntax syntax,
            PropertySymbol explicitlyImplementedPropertyOpt,
            string aliasQualifierOpt,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics)
        {
            string name;
            ImmutableArray<MethodSymbol> explicitInterfaceImplementations;
            GetNameAndExplicitInterfaceImplementations(
                explicitlyImplementedPropertyOpt,
                propertyName,
                property.IsCompilationOutputWinMdObj(),
                aliasQualifierOpt,
                isGetMethod: true,
                name: out name,
                explicitInterfaceImplementations:
                out explicitInterfaceImplementations);

            return new SourcePropertyAccessorSymbol(
                containingType,
                name,
                property,
                propertyModifiers,
                explicitInterfaceImplementations,
                syntax.Expression.GetLocation(),
                syntax,
                isExplicitInterfaceImplementation,
                diagnostics);
        }

#nullable enable
        public static SourcePropertyAccessorSymbol CreateAccessorSymbol(
            bool isGetMethod,
            bool usesInit,
            NamedTypeSymbol containingType,
            SynthesizedRecordPropertySymbol property,
            DeclarationModifiers propertyModifiers,
            Location location,
            CSharpSyntaxNode syntax,
            DiagnosticBag diagnostics)
        {
            string name;
            ImmutableArray<MethodSymbol> explicitInterfaceImplementations;
            GetNameAndExplicitInterfaceImplementations(
                explicitlyImplementedPropertyOpt: null,
                property.Name,
                property.IsCompilationOutputWinMdObj(),
                aliasQualifierOpt: null,
                isGetMethod,
                out name,
                out explicitInterfaceImplementations);

            var methodKind = isGetMethod ? MethodKind.PropertyGet : MethodKind.PropertySet;
            return new SourcePropertyAccessorSymbol(
                containingType,
                name,
                property,
                propertyModifiers,
                explicitInterfaceImplementations,
                location,
                syntax,
                hasBody: false,
                hasExpressionBody: false,
                isIterator: false,
                modifiers: new SyntaxTokenList(),
                methodKind,
                usesInit,
                isAutoPropertyAccessor: true,
                isExplicitInterfaceImplementation: false,
                diagnostics);
        }

        public static SourcePropertyAccessorSymbol CreateAccessorSymbol(
            NamedTypeSymbol containingType,
            SynthesizedRecordEqualityContractProperty property,
            DeclarationModifiers propertyModifiers,
            Location location,
            CSharpSyntaxNode syntax,
            DiagnosticBag diagnostics)
        {
            string name;
            ImmutableArray<MethodSymbol> explicitInterfaceImplementations;
            GetNameAndExplicitInterfaceImplementations(
                explicitlyImplementedPropertyOpt: null,
                property.Name,
                property.IsCompilationOutputWinMdObj(),
                aliasQualifierOpt: null,
                isGetMethod: true,
                out name,
                out explicitInterfaceImplementations);

            return new SynthesizedRecordEqualityContractProperty.GetAccessorSymbol(
                containingType,
                name,
                property,
                propertyModifiers,
                explicitInterfaceImplementations,
                location,
                syntax,
                diagnostics);
        }
#nullable disable

        internal sealed override bool IsExpressionBodied
            => _isExpressionBodied;

        internal sealed override ImmutableArray<string> NotNullMembers
            => _property.NotNullMembers.Concat(base.NotNullMembers);

        internal sealed override ImmutableArray<string> NotNullWhenTrueMembers
            => _property.NotNullWhenTrueMembers.Concat(base.NotNullWhenTrueMembers);

        internal sealed override ImmutableArray<string> NotNullWhenFalseMembers
            => _property.NotNullWhenFalseMembers.Concat(base.NotNullWhenFalseMembers);

        private static void GetNameAndExplicitInterfaceImplementations(
            PropertySymbol explicitlyImplementedPropertyOpt,
            string propertyName,
            bool isWinMd,
            string aliasQualifierOpt,
            bool isGetMethod,
            out string name,
            out ImmutableArray<MethodSymbol> explicitInterfaceImplementations)
        {
            if ((object)explicitlyImplementedPropertyOpt == null)
            {
                name = GetAccessorName(propertyName, isGetMethod, isWinMd);
                explicitInterfaceImplementations = ImmutableArray<MethodSymbol>.Empty;
            }
            else
            {
                MethodSymbol implementedAccessor = isGetMethod
                    ? explicitlyImplementedPropertyOpt.GetMethod
                    : explicitlyImplementedPropertyOpt.SetMethod;

                string accessorName = (object)implementedAccessor != null
                    ? implementedAccessor.Name
                    : GetAccessorName(explicitlyImplementedPropertyOpt.MetadataName,
                        isGetMethod, isWinMd); //Not name - could be indexer placeholder

                name = ExplicitInterfaceHelpers.GetMemberName(accessorName, explicitlyImplementedPropertyOpt.ContainingType, aliasQualifierOpt);
                explicitInterfaceImplementations = (object)implementedAccessor == null
                    ? ImmutableArray<MethodSymbol>.Empty
                    : ImmutableArray.Create<MethodSymbol>(implementedAccessor);
            }
        }

        private SourcePropertyAccessorSymbol(
            NamedTypeSymbol containingType,
            string name,
            SourcePropertySymbol property,
            DeclarationModifiers propertyModifiers,
            ImmutableArray<MethodSymbol> explicitInterfaceImplementations,
            Location location,
            ArrowExpressionClauseSyntax syntax,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics) :
            base(containingType, syntax.GetReference(), location, isIterator: false)
        {
            _property = property;
            _explicitInterfaceImplementations = explicitInterfaceImplementations;
            _name = name;
            _isAutoPropertyAccessor = false;
            _isExpressionBodied = true;

            // The modifiers for the accessor are the same as the modifiers for the property,
            // minus the indexer and readonly bit
            var declarationModifiers = GetAccessorModifiers(propertyModifiers);

            // ReturnsVoid property is overridden in this class so
            // returnsVoid argument to MakeFlags is ignored.
            this.MakeFlags(MethodKind.PropertyGet, declarationModifiers, returnsVoid: false, isExtensionMethod: false,
                isMetadataVirtualIgnoringModifiers: explicitInterfaceImplementations.Any());

            CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasBody: true, diagnostics: diagnostics);
            CheckModifiersForBody(location, diagnostics);

            var info = ModifierUtils.CheckAccessibility(this.DeclarationModifiers, this, isExplicitInterfaceImplementation);
            if (info != null)
            {
                diagnostics.Add(info, location);
            }

            this.CheckModifiers(location, hasBody: true, isAutoPropertyOrExpressionBodied: true, diagnostics: diagnostics);

            if (this.IsOverride)
            {
                MethodSymbol overriddenMethod = this.OverriddenMethod;
                if ((object)overriddenMethod != null)
                {
                    // If this accessor is overriding a method from metadata, it is possible that
                    // the name of the overridden method doesn't follow the C# get_X/set_X pattern.
                    // We should copy the name so that the runtime will recognize this as an override.
                    _name = overriddenMethod.Name;
                }
            }
        }

#nullable enable
        protected SourcePropertyAccessorSymbol(
            NamedTypeSymbol containingType,
            string name,
            SourcePropertySymbolBase property,
            DeclarationModifiers propertyModifiers,
            ImmutableArray<MethodSymbol> explicitInterfaceImplementations,
            Location location,
            CSharpSyntaxNode syntax,
            bool hasBody,
            bool hasExpressionBody,
            bool isIterator,
            SyntaxTokenList modifiers,
            MethodKind methodKind,
            bool usesInit,
            bool isAutoPropertyAccessor,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics)
            : base(containingType,
                   syntax.GetReference(),
                   location,
                   isIterator: isIterator)
        {
            _property = property;
            _explicitInterfaceImplementations = explicitInterfaceImplementations;
            _name = name;
            _isAutoPropertyAccessor = isAutoPropertyAccessor;
            Debug.Assert(!_property.IsExpressionBodied, "Cannot have accessors in expression bodied lightweight properties");
            _isExpressionBodied = !hasBody && hasExpressionBody;
            _usesInit = usesInit;
            if (_usesInit)
            {
                Binder.CheckFeatureAvailability(syntax, MessageID.IDS_FeatureInitOnlySetters, diagnostics, location);
            }

            bool modifierErrors;
            var declarationModifiers = this.MakeModifiers(modifiers, isExplicitInterfaceImplementation, hasBody || hasExpressionBody, location, diagnostics, out modifierErrors);

            // Include some modifiers from the containing property, but not the accessibility modifiers.
            declarationModifiers |= GetAccessorModifiers(propertyModifiers) & ~DeclarationModifiers.AccessibilityMask;
            if ((declarationModifiers & DeclarationModifiers.Private) != 0)
            {
                // Private accessors cannot be virtual.
                declarationModifiers &= ~DeclarationModifiers.Virtual;
            }

            // ReturnsVoid property is overridden in this class so
            // returnsVoid argument to MakeFlags is ignored.
            this.MakeFlags(methodKind, declarationModifiers, returnsVoid: false, isExtensionMethod: false,
                isMetadataVirtualIgnoringModifiers: explicitInterfaceImplementations.Any());

            CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasBody: hasBody || hasExpressionBody || isAutoPropertyAccessor, diagnostics);

            if (hasBody || hasExpressionBody)
            {
                CheckModifiersForBody(location, diagnostics);
            }

            var info = ModifierUtils.CheckAccessibility(this.DeclarationModifiers, this, isExplicitInterfaceImplementation);
            if (info != null)
            {
                diagnostics.Add(info, location);
            }

            if (!modifierErrors)
            {
                this.CheckModifiers(location, hasBody || hasExpressionBody, isAutoPropertyAccessor, diagnostics);
            }

            if (this.IsOverride)
            {
                MethodSymbol overriddenMethod = this.OverriddenMethod;
                if ((object)overriddenMethod != null)
                {
                    // If this accessor is overriding a method from metadata, it is possible that
                    // the name of the overridden method doesn't follow the C# get_X/set_X pattern.
                    // We should copy the name so that the runtime will recognize this as an override.
                    _name = overriddenMethod.Name;
                }
            }

        }
#nullable disable

        private static DeclarationModifiers GetAccessorModifiers(DeclarationModifiers propertyModifiers) =>
            propertyModifiers & ~(DeclarationModifiers.Indexer | DeclarationModifiers.ReadOnly);

        protected sealed override void MethodChecks(DiagnosticBag diagnostics)
        {
            // These values may not be final, but we need to have something set here in the
            // event that we need to find the overridden accessor.
            _lazyParameters = ComputeParameters(diagnostics);
            _lazyReturnType = ComputeReturnType(diagnostics);
            _lazyRefCustomModifiers = ImmutableArray<CustomModifier>.Empty;

            if (_explicitInterfaceImplementations.Length > 0)
            {
                Debug.Assert(_explicitInterfaceImplementations.Length == 1);
                MethodSymbol implementedMethod = _explicitInterfaceImplementations[0];
                CustomModifierUtils.CopyMethodCustomModifiers(implementedMethod, this, out _lazyReturnType,
                                                              out _lazyRefCustomModifiers,
                                                              out _lazyParameters, alsoCopyParamsModifier: false);
            }
            else if (this.IsOverride)
            {
                // This will cause another call to SourceMethodSymbol.LazyMethodChecks,
                // but that method already handles reentrancy for exactly this case.
                MethodSymbol overriddenMethod = this.OverriddenMethod;
                if ((object)overriddenMethod != null)
                {
                    CustomModifierUtils.CopyMethodCustomModifiers(overriddenMethod, this, out _lazyReturnType,
                                                                  out _lazyRefCustomModifiers,
                                                                  out _lazyParameters, alsoCopyParamsModifier: true);
                }
            }
            else if (!_lazyReturnType.IsVoidType())
            {
                PropertySymbol associatedProperty = _property;
                var type = associatedProperty.TypeWithAnnotations;
                _lazyReturnType = _lazyReturnType.WithTypeAndModifiers(
                    CustomModifierUtils.CopyTypeCustomModifiers(type.Type, _lazyReturnType.Type, this.ContainingAssembly),
                    type.CustomModifiers);
                _lazyRefCustomModifiers = associatedProperty.RefCustomModifiers;
            }
        }

        public sealed override Accessibility DeclaredAccessibility
        {
            get
            {
                var accessibility = this.LocalAccessibility;
                if (accessibility != Accessibility.NotApplicable)
                {
                    return accessibility;
                }

                var propertyAccessibility = _property.DeclaredAccessibility;
                Debug.Assert(propertyAccessibility != Accessibility.NotApplicable);
                return propertyAccessibility;
            }
        }

        public sealed override Symbol AssociatedSymbol
        {
            get { return _property; }
        }

        public sealed override bool IsVararg
        {
            get { return false; }
        }

        public sealed override bool ReturnsVoid
        {
            get { return this.ReturnType.IsVoidType(); }
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                LazyMethodChecks();
                return _lazyParameters;
            }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        public sealed override ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses(bool canIgnoreNullableContext)
            => ImmutableArray<TypeParameterConstraintClause>.Empty;

        public sealed override RefKind RefKind
        {
            get { return _property.RefKind; }
        }

        public sealed override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnType;
            }
        }

        public sealed override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations
        {
            get
            {
                if (MethodKind == MethodKind.PropertySet)
                {
                    return FlowAnalysisAnnotations.None;
                }

                var result = FlowAnalysisAnnotations.None;
                if (_property.HasMaybeNull)
                {
                    result |= FlowAnalysisAnnotations.MaybeNull;
                }
                if (_property.HasNotNull)
                {
                    result |= FlowAnalysisAnnotations.NotNull;
                }
                return result;
            }
        }

        public sealed override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        private TypeWithAnnotations ComputeReturnType(DiagnosticBag diagnostics)
        {
            if (this.MethodKind == MethodKind.PropertyGet)
            {
                var type = _property.TypeWithAnnotations;
                if (type.Type.IsStatic)
                {
                    // '{0}': static types cannot be used as return types
                    diagnostics.Add(ErrorFacts.GetStaticClassReturnCode(ContainingType.IsInterfaceType()), this.locations[0], type.Type);
                }

                return type;
            }
            else
            {
                var binder = GetBinder();
                var type = TypeWithAnnotations.Create(binder.GetSpecialType(SpecialType.System_Void, diagnostics, this.GetSyntax()));

                if (IsInitOnly)
                {
                    var isInitOnlyType = Binder.GetWellKnownType(this.DeclaringCompilation,
                        WellKnownType.System_Runtime_CompilerServices_IsExternalInit, diagnostics, this.locations[0]);

                    var modifiers = ImmutableArray.Create<CustomModifier>(
                        CSharpCustomModifier.CreateRequired(isInitOnlyType));
                    type = type.WithModifiers(modifiers);
                }

                return type;
            }
        }

        private Binder GetBinder()
        {
            var syntax = this.GetSyntax();
            var compilation = this.DeclaringCompilation;
            var binderFactory = compilation.GetBinderFactory(syntax.SyntaxTree);
            return binderFactory.GetBinder(syntax);
        }

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                LazyMethodChecks();
                return _lazyRefCustomModifiers;
            }
        }

        /// <summary>
        /// Return Accessibility declared locally on the accessor, or
        /// NotApplicable if no accessibility was declared explicitly.
        /// </summary>
        internal Accessibility LocalAccessibility
        {
            get { return ModifierUtils.EffectiveAccessibility(this.DeclarationModifiers); }
        }

        /// <summary>
        /// Indicates whether this accessor itself has a 'readonly' modifier.
        /// </summary>
        internal bool LocalDeclaredReadOnly => (DeclarationModifiers & DeclarationModifiers.ReadOnly) != 0;

        /// <summary>
        /// Indicates whether this accessor is readonly due to reasons scoped to itself and its containing property.
        /// </summary>
        internal sealed override bool IsDeclaredReadOnly
        {
            get
            {
                if (LocalDeclaredReadOnly || (_property.HasReadOnlyModifier && IsValidReadOnlyTarget))
                {
                    return true;
                }

                // The below checks are used to decide if this accessor is implicitly 'readonly'.

                // Making a member implicitly 'readonly' allows valid C# 7.0 code to break PEVerify.
                // For instance:

                // struct S {
                //     int Value { get; set; }
                //     static readonly S StaticField = new S();
                //     static void M() {
                //         System.Console.WriteLine(StaticField.Value);
                //     }
                // }

                // The above program will fail PEVerify if the 'S.Value.get' accessor is made implicitly readonly because
                // we won't emit an implicit copy of 'S.StaticField' to pass to 'S.Value.get'.

                // Code emitted in C# 7.0 and before must be PEVerify compatible, so we will only make
                // members implicitly readonly in language versions which support the readonly members feature.
                var options = (CSharpParseOptions)SyntaxTree.Options;
                if (!options.IsFeatureEnabled(MessageID.IDS_FeatureReadOnlyMembers))
                {
                    return false;
                }

                // If we have IsReadOnly..ctor, we can use the attribute. Otherwise, we need to NOT be a netmodule and the type must not already exist in order to synthesize it.
                var isReadOnlyAttributeUsable = DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IsReadOnlyAttribute__ctor) != null ||
                    (DeclaringCompilation.Options.OutputKind != OutputKind.NetModule &&
                     DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute) is MissingMetadataTypeSymbol);

                if (!isReadOnlyAttributeUsable)
                {
                    // if the readonly attribute isn't usable, don't implicitly make auto-getters readonly.
                    return false;
                }

                return ContainingType.IsStructType() &&
                    !_property.IsStatic &&
                    _isAutoPropertyAccessor &&
                    MethodKind == MethodKind.PropertyGet;
            }
        }

        internal sealed override bool IsInitOnly => !IsStatic && _usesInit;

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, bool isExplicitInterfaceImplementation,
            bool hasBody, Location location, DiagnosticBag diagnostics, out bool modifierErrors)
        {
            // No default accessibility. If unset, accessibility
            // will be inherited from the property.
            const DeclarationModifiers defaultAccess = DeclarationModifiers.None;

            // Check that the set of modifiers is allowed
            var allowedModifiers = isExplicitInterfaceImplementation ? DeclarationModifiers.None : DeclarationModifiers.AccessibilityMask;
            if (this.ContainingType.IsStructType())
            {
                allowedModifiers |= DeclarationModifiers.ReadOnly;
            }

            var defaultInterfaceImplementationModifiers = DeclarationModifiers.None;

            if (this.ContainingType.IsInterface && !isExplicitInterfaceImplementation)
            {
                defaultInterfaceImplementationModifiers = DeclarationModifiers.AccessibilityMask;
            }

            var mods = ModifierUtils.MakeAndCheckNontypeMemberModifiers(modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors);

            ModifierUtils.ReportDefaultInterfaceImplementationModifiers(hasBody, mods,
                                                                        defaultInterfaceImplementationModifiers,
                                                                        location, diagnostics);

            return mods;
        }

        private void CheckModifiers(Location location, bool hasBody, bool isAutoPropertyOrExpressionBodied, DiagnosticBag diagnostics)
        {
            // Check accessibility against the accessibility declared on the accessor not the property.
            var localAccessibility = this.LocalAccessibility;

            if (IsAbstract && !ContainingType.IsAbstract && (ContainingType.TypeKind == TypeKind.Class || ContainingType.TypeKind == TypeKind.Submission))
            {
                // '{0}' is abstract but it is contained in non-abstract type '{1}'
                diagnostics.Add(ErrorCode.ERR_AbstractInConcreteClass, location, this, ContainingType);
            }
            else if (IsVirtual && ContainingType.IsSealed && ContainingType.TypeKind != TypeKind.Struct) // error CS0106 on struct already
            {
                // '{0}' is a new virtual member in sealed type '{1}'
                diagnostics.Add(ErrorCode.ERR_NewVirtualInSealed, location, this, ContainingType);
            }
            else if (!hasBody && !IsExtern && !IsAbstract && !isAutoPropertyOrExpressionBodied)
            {
                diagnostics.Add(ErrorCode.ERR_ConcreteMissingBody, location, this);
            }
            else if (ContainingType.IsSealed && localAccessibility.HasProtected() && !this.IsOverride)
            {
                diagnostics.Add(AccessCheck.GetProtectedMemberInSealedTypeError(ContainingType), location, this);
            }
            else if (LocalDeclaredReadOnly && _property.HasReadOnlyModifier)
            {
                // Cannot specify 'readonly' modifiers on both property or indexer '{0}' and its accessors.
                diagnostics.Add(ErrorCode.ERR_InvalidPropertyReadOnlyMods, location, _property);
            }
            else if (LocalDeclaredReadOnly && IsStatic)
            {
                // Static member '{0}' cannot be marked 'readonly'.
                diagnostics.Add(ErrorCode.ERR_StaticMemberCantBeReadOnly, location, this);
            }
            else if (LocalDeclaredReadOnly && IsInitOnly)
            {
                // 'init' accessors cannot be marked 'readonly'. Mark '{0}' readonly instead.
                diagnostics.Add(ErrorCode.ERR_InitCannotBeReadonly, location, _property);
            }
            else if (LocalDeclaredReadOnly && _isAutoPropertyAccessor && MethodKind == MethodKind.PropertySet)
            {
                // Auto-implemented accessor '{0}' cannot be marked 'readonly'.
                diagnostics.Add(ErrorCode.ERR_AutoSetterCantBeReadOnly, location, this);
            }
            else if (_usesInit && IsStatic)
            {
                // The 'init' accessor is not valid on static members
                diagnostics.Add(ErrorCode.ERR_BadInitAccessor, location);
            }
        }

        /// <summary>
        /// If we are outputting a .winmdobj then the setter name is put_, not set_.
        /// </summary>
        internal static string GetAccessorName(string propertyName, bool getNotSet, bool isWinMdOutput)
        {
            var prefix = getNotSet ? "get_" : isWinMdOutput ? "put_" : "set_";
            return prefix + propertyName;
        }

        /// <returns>
        /// The declaring syntax for the accessor, or property if there is no accessor-specific
        /// syntax.
        /// </returns>
        internal CSharpSyntaxNode GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (CSharpSyntaxNode)syntaxReferenceOpt.GetSyntax();
        }

        internal sealed override bool IsExplicitInterfaceImplementation
        {
            get
            {
                return _property.IsExplicitInterfaceImplementation;
            }
        }

        public sealed override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return _explicitInterfaceImplementations;
            }
        }

        internal sealed override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            var syntax = this.GetSyntax();
            switch (syntax.Kind())
            {
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                    return OneOrMany.Create(((AccessorDeclarationSyntax)syntax).AttributeLists);
            }

            return base.GetAttributeDeclarations();
        }

        public sealed override string Name
        {
            get
            {
                return _name;
            }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                // Per design meeting resolution [see bug 11253], no source accessor is implicitly declared in C#,
                // if there is "get", "set", or expression-body syntax.
                switch (GetSyntax().Kind())
                {
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.InitAccessorDeclaration:
                    case SyntaxKind.ArrowExpressionClause:
                        return false;
                };

                return true;
            }
        }

        internal sealed override bool GenerateDebugInfo
        {
            get
            {
                return true;
            }
        }

        public sealed override bool AreLocalsZeroed
        {
            get
            {
                return !_property.HasSkipLocalsInitAttribute && base.AreLocalsZeroed;
            }
        }

        private ImmutableArray<ParameterSymbol> ComputeParameters(DiagnosticBag diagnostics)
        {
            bool isGetMethod = this.MethodKind == MethodKind.PropertyGet;
            var propertyParameters = _property.Parameters;
            int nPropertyParameters = propertyParameters.Length;
            int nParameters = nPropertyParameters + (isGetMethod ? 0 : 1);

            if (nParameters == 0)
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }

            var parameters = ArrayBuilder<ParameterSymbol>.GetInstance(nParameters);

            // Clone the property parameters for the accessor method. The
            // parameters are cloned (rather than referenced from the property)
            // since the ContainingSymbol needs to be set to the accessor.
            foreach (SourceParameterSymbol propertyParam in propertyParameters)
            {
                parameters.Add(new SourceClonedParameterSymbol(propertyParam, this, propertyParam.Ordinal, suppressOptional: false));
            }

            if (!isGetMethod)
            {
                var propertyType = _property.TypeWithAnnotations;
                if (propertyType.IsStatic)
                {
                    // '{0}': static types cannot be used as parameters
                    diagnostics.Add(ErrorFacts.GetStaticClassParameterCode(ContainingType.IsInterfaceType()), this.locations[0], propertyType.Type);
                }

                parameters.Add(new SynthesizedAccessorValueParameterSymbol(this, propertyType, parameters.Count));
            }

            return parameters.ToImmutableAndFree();
        }

        internal sealed override void AddSynthesizedReturnTypeAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedReturnTypeAttributes(moduleBuilder, ref attributes);

            var annotations = ReturnTypeFlowAnalysisAnnotations;
            if ((annotations & FlowAnalysisAnnotations.MaybeNull) != 0)
            {
                AddSynthesizedAttribute(ref attributes, new SynthesizedAttributeData(_property.MaybeNullAttributeIfExists));
            }
            if ((annotations & FlowAnalysisAnnotations.NotNull) != 0)
            {
                AddSynthesizedAttribute(ref attributes, new SynthesizedAttributeData(_property.NotNullAttributeIfExists));
            }
        }

        internal sealed override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            if (_isAutoPropertyAccessor)
            {
                var compilation = this.DeclaringCompilation;
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            }

            if (!NotNullMembers.IsEmpty)
            {
                foreach (var attributeData in _property.MemberNotNullAttributeIfExists)
                {
                    AddSynthesizedAttribute(ref attributes, new SynthesizedAttributeData(attributeData));
                }
            }

            if (!NotNullWhenTrueMembers.IsEmpty || !NotNullWhenFalseMembers.IsEmpty)
            {
                foreach (var attributeData in _property.MemberNotNullWhenAttributeIfExists)
                {
                    AddSynthesizedAttribute(ref attributes, new SynthesizedAttributeData(attributeData));
                }
            }
        }
    }
}
