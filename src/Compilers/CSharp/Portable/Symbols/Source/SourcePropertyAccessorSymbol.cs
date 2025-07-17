// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
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
        private ImmutableArray<MethodSymbol> _lazyExplicitInterfaceImplementations;
        private string _lazyName;
        private readonly bool _isAutoPropertyAccessor;
        private readonly bool _usesInit;

        public static SourcePropertyAccessorSymbol CreateAccessorSymbol(
            NamedTypeSymbol containingType,
            SourcePropertySymbol property,
            DeclarationModifiers propertyModifiers,
            AccessorDeclarationSyntax syntax,
            bool isAutoPropertyAccessor,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(syntax.Kind() == SyntaxKind.GetAccessorDeclaration ||
                syntax.Kind() == SyntaxKind.SetAccessorDeclaration ||
                syntax.Kind() == SyntaxKind.InitAccessorDeclaration);

            bool isGetMethod = (syntax.Kind() == SyntaxKind.GetAccessorDeclaration);
            var methodKind = isGetMethod ? MethodKind.PropertyGet : MethodKind.PropertySet;

            bool hasBody = syntax.Body is object;
            bool hasExpressionBody = syntax.ExpressionBody is object;
            bool isNullableAnalysisEnabled = containingType.DeclaringCompilation.IsNullableAnalysisEnabledIn(syntax);
            CheckForBlockAndExpressionBody(syntax.Body, syntax.ExpressionBody, syntax, diagnostics);

            return new SourcePropertyAccessorSymbol(
                containingType,
                property,
                propertyModifiers,
                syntax.Keyword.GetLocation(),
                syntax,
                hasBody,
                hasExpressionBody,
                isIterator: SyntaxFacts.HasYieldOperations(syntax.Body),
                syntax.Modifiers,
                methodKind,
                syntax.Keyword.IsKind(SyntaxKind.InitKeyword),
                isAutoPropertyAccessor,
                isNullableAnalysisEnabled: isNullableAnalysisEnabled,
                diagnostics);
        }

        public static SourcePropertyAccessorSymbol CreateAccessorSymbol(
            NamedTypeSymbol containingType,
            SourcePropertySymbol property,
            DeclarationModifiers propertyModifiers,
            ArrowExpressionClauseSyntax syntax,
            BindingDiagnosticBag diagnostics)
        {
            bool isNullableAnalysisEnabled = containingType.DeclaringCompilation.IsNullableAnalysisEnabledIn(syntax);
            return new SourcePropertyAccessorSymbol(
                containingType,
                property,
                propertyModifiers,
                syntax.Expression.GetLocation(),
                syntax,
                isNullableAnalysisEnabled: isNullableAnalysisEnabled,
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
            BindingDiagnosticBag diagnostics)
        {
            var methodKind = isGetMethod ? MethodKind.PropertyGet : MethodKind.PropertySet;
            return new SourcePropertyAccessorSymbol(
                containingType,
                property,
                propertyModifiers,
                location,
                syntax,
                hasBlockBody: false,
                hasExpressionBody: false,
                isIterator: false,
                modifiers: default,
                methodKind,
                usesInit,
                isAutoPropertyAccessor: true,
                isNullableAnalysisEnabled: false,
                diagnostics);
        }

        public static SourcePropertyAccessorSymbol CreateAccessorSymbol(
            NamedTypeSymbol containingType,
            SynthesizedRecordEqualityContractProperty property,
            DeclarationModifiers propertyModifiers,
            Location location,
            CSharpSyntaxNode syntax,
            BindingDiagnosticBag diagnostics)
        {
            return new SynthesizedRecordEqualityContractProperty.GetAccessorSymbol(
                containingType,
                property,
                propertyModifiers,
                location,
                syntax,
                diagnostics);
        }
#nullable disable

        internal sealed override ImmutableArray<string> NotNullMembers
            => _property.NotNullMembers.Concat(base.NotNullMembers);

        internal sealed override ImmutableArray<string> NotNullWhenTrueMembers
            => _property.NotNullWhenTrueMembers.Concat(base.NotNullWhenTrueMembers);

        internal sealed override ImmutableArray<string> NotNullWhenFalseMembers
            => _property.NotNullWhenFalseMembers.Concat(base.NotNullWhenFalseMembers);

        private SourcePropertyAccessorSymbol(
            NamedTypeSymbol containingType,
            SourcePropertySymbol property,
            DeclarationModifiers propertyModifiers,
            Location location,
            ArrowExpressionClauseSyntax syntax,
            bool isNullableAnalysisEnabled,
            BindingDiagnosticBag diagnostics) :
            base(containingType, syntax.GetReference(), location, isIterator: false,
                MakeModifiersAndFlags(
                    containingType,
                    property,
                    propertyModifiers,
                    location,
                    hasBlockBody: false,
                    hasExpressionBody: true,
                    modifiers: [],
                    methodKind: MethodKind.PropertyGet,
                    isNullableAnalysisEnabled,
                    diagnostics,
                    out var modifierErrors))
        {
            _property = property;
            _isAutoPropertyAccessor = false;

            CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasBody: true, diagnostics: diagnostics);
            CheckModifiersForBody(location, diagnostics);

            ModifierUtils.CheckAccessibility(this.DeclarationModifiers, this, property.IsExplicitInterfaceImplementation, diagnostics, location);

            this.CheckModifiers(location, hasBody: true, isAutoPropertyOrExpressionBodied: true, diagnostics: diagnostics);
        }

#nullable enable
        protected SourcePropertyAccessorSymbol(
            NamedTypeSymbol containingType,
            SourcePropertySymbolBase property,
            DeclarationModifiers propertyModifiers,
            Location location,
            CSharpSyntaxNode syntax,
            bool hasBlockBody,
            bool hasExpressionBody,
            bool isIterator,
            SyntaxTokenList modifiers,
            MethodKind methodKind,
            bool usesInit,
            bool isAutoPropertyAccessor,
            bool isNullableAnalysisEnabled,
            BindingDiagnosticBag diagnostics)
            : base(containingType,
                   syntax.GetReference(),
                   location,
                   isIterator,
                   MakeModifiersAndFlags(containingType, property, propertyModifiers, location, hasBlockBody, hasExpressionBody, modifiers, methodKind, isNullableAnalysisEnabled, diagnostics, out bool modifierErrors))
        {
            _property = property;
            _isAutoPropertyAccessor = isAutoPropertyAccessor;
            Debug.Assert(!_property.IsExpressionBodied, "Cannot have accessors in expression bodied lightweight properties");
            var hasAnyBody = hasBlockBody || hasExpressionBody;
            _usesInit = usesInit;
            if (_usesInit)
            {
                Binder.CheckFeatureAvailability(syntax, MessageID.IDS_FeatureInitOnlySetters, diagnostics, location);
            }

            CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasBody: hasAnyBody || isAutoPropertyAccessor, diagnostics);

            if (hasAnyBody)
            {
                CheckModifiersForBody(location, diagnostics);
            }

            ModifierUtils.CheckAccessibility(this.DeclarationModifiers, this, property.IsExplicitInterfaceImplementation, diagnostics, location);

            if (!modifierErrors)
            {
                this.CheckModifiers(location, hasAnyBody, isAutoPropertyAccessor, diagnostics);
            }

            if (modifiers.Count > 0)
                MessageID.IDS_FeaturePropertyAccessorMods.CheckFeatureAvailability(diagnostics, modifiers[0]);
        }

        private static (DeclarationModifiers, Flags) MakeModifiersAndFlags(
            NamedTypeSymbol containingType, SourcePropertySymbolBase property, DeclarationModifiers propertyModifiers, Location location,
            bool hasBlockBody, bool hasExpressionBody, SyntaxTokenList modifiers, MethodKind methodKind, bool isNullableAnalysisEnabled,
            BindingDiagnosticBag diagnostics, out bool modifierErrors)
        {
            var isExpressionBodied = !hasBlockBody && hasExpressionBody;
            var hasAnyBody = hasBlockBody || hasExpressionBody;

            bool isExplicitInterfaceImplementation = property.IsExplicitInterfaceImplementation;
            var declarationModifiers = MakeModifiers(containingType, modifiers, isExplicitInterfaceImplementation, hasAnyBody, location, diagnostics, out modifierErrors);

            // Include some modifiers from the containing property, but not the accessibility modifiers.
            declarationModifiers |= GetAccessorModifiers(propertyModifiers) & ~DeclarationModifiers.AccessibilityMask;
            if ((declarationModifiers & DeclarationModifiers.Private) != 0)
            {
                // Private accessors cannot be virtual.
                declarationModifiers &= ~DeclarationModifiers.Virtual;
            }

            // ReturnsVoid property is overridden in this class so
            // returnsVoid argument to MakeFlags is ignored.
            Flags flags = MakeFlags(methodKind, property.RefKind, declarationModifiers, returnsVoid: false, returnsVoidIsSet: false,
                                    isExpressionBodied: isExpressionBodied, isExtensionMethod: false, isNullableAnalysisEnabled: isNullableAnalysisEnabled,
                                    isVarArg: false, isExplicitInterfaceImplementation: isExplicitInterfaceImplementation, hasThisInitializer: false);

            return (declarationModifiers, flags);
        }
#nullable disable

        private static DeclarationModifiers GetAccessorModifiers(DeclarationModifiers propertyModifiers) =>
            propertyModifiers & ~(DeclarationModifiers.Indexer | DeclarationModifiers.ReadOnly);

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory binderFactoryOpt = null, bool ignoreAccessibility = false)
        {
            return TryGetBodyBinderFromSyntax(binderFactoryOpt, ignoreAccessibility);
        }

        protected sealed override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
            // These values may not be final, but we need to have something set here in the
            // event that we need to find the overridden accessor.
            _lazyParameters = ComputeParameters();
            _lazyReturnType = ComputeReturnType(diagnostics);
            _lazyRefCustomModifiers = ImmutableArray<CustomModifier>.Empty;

            var explicitInterfaceImplementations = ExplicitInterfaceImplementations;
            if (explicitInterfaceImplementations.Length > 0)
            {
                Debug.Assert(explicitInterfaceImplementations.Length == 1);
                MethodSymbol implementedMethod = explicitInterfaceImplementations[0];
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

        public sealed override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
            => ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;

        public sealed override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
            => ImmutableArray<TypeParameterConstraintKind>.Empty;

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

        private TypeWithAnnotations ComputeReturnType(BindingDiagnosticBag diagnostics)
        {
            if (this.MethodKind == MethodKind.PropertyGet)
            {
                return _property.TypeWithAnnotations;
            }
            else
            {
                var binder = GetBinder();
                var type = TypeWithAnnotations.Create(binder.GetSpecialType(SpecialType.System_Void, diagnostics, this.GetSyntax()));

                if (IsInitOnly)
                {
                    var isInitOnlyType = Binder.GetWellKnownType(this.DeclaringCompilation,
                        WellKnownType.System_Runtime_CompilerServices_IsExternalInit, diagnostics, _location);

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

        internal bool IsAutoPropertyAccessor => _isAutoPropertyAccessor;

        internal sealed override bool IsInitOnly => !IsStatic && _usesInit;

        private static DeclarationModifiers MakeModifiers(NamedTypeSymbol containingType, SyntaxTokenList modifiers, bool isExplicitInterfaceImplementation,
            bool hasBody, Location location, BindingDiagnosticBag diagnostics, out bool modifierErrors)
        {
            // No default accessibility. If unset, accessibility
            // will be inherited from the property.
            const DeclarationModifiers defaultAccess = DeclarationModifiers.None;

            // Check that the set of modifiers is allowed
            var allowedModifiers = isExplicitInterfaceImplementation ? DeclarationModifiers.None : DeclarationModifiers.AccessibilityMask;
            if (containingType.IsStructType())
            {
                allowedModifiers |= DeclarationModifiers.ReadOnly;
            }

            var defaultInterfaceImplementationModifiers = DeclarationModifiers.None;

            bool isInterface = containingType.IsInterface;
            if (isInterface && !isExplicitInterfaceImplementation)
            {
                defaultInterfaceImplementationModifiers = DeclarationModifiers.AccessibilityMask;
            }

            var mods = ModifierUtils.MakeAndCheckNonTypeMemberModifiers(isOrdinaryMethod: false, isForInterfaceMember: isInterface,
                                                                        modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors, out _);

            ModifierUtils.ReportDefaultInterfaceImplementationModifiers(hasBody, mods,
                                                                        defaultInterfaceImplementationModifiers,
                                                                        location, diagnostics);

            return mods;
        }

        private void CheckModifiers(Location location, bool hasBody, bool isAutoPropertyOrExpressionBodied, BindingDiagnosticBag diagnostics)
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
            else if (!hasBody && !IsExtern && !IsAbstract && !isAutoPropertyOrExpressionBodied && !IsPartialDefinition)
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
            else if (ContainingType.IsExtension && IsInitOnly)
            {
                diagnostics.Add(ErrorCode.ERR_InitInExtension, location, _property);
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

#nullable enable
        public sealed override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (_lazyExplicitInterfaceImplementations.IsDefault)
                {
                    PropertySymbol? explicitlyImplementedPropertyOpt = IsExplicitInterfaceImplementation ? _property.ExplicitInterfaceImplementations.FirstOrDefault() : null;
                    ImmutableArray<MethodSymbol> explicitInterfaceImplementations;

                    if (explicitlyImplementedPropertyOpt is null)
                    {
                        explicitInterfaceImplementations = ImmutableArray<MethodSymbol>.Empty;
                    }
                    else
                    {
                        MethodSymbol implementedAccessor = this.MethodKind == MethodKind.PropertyGet
                            ? explicitlyImplementedPropertyOpt.GetMethod
                            : explicitlyImplementedPropertyOpt.SetMethod;

                        explicitInterfaceImplementations = (object)implementedAccessor == null
                            ? ImmutableArray<MethodSymbol>.Empty
                            : ImmutableArray.Create<MethodSymbol>(implementedAccessor);
                    }

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyExplicitInterfaceImplementations, explicitInterfaceImplementations);
                }

                return _lazyExplicitInterfaceImplementations;
            }
        }
#nullable disable

        internal sealed override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            if (PartialImplementationPart is { } implementation)
            {
                return OneOrMany.Create(AttributeDeclarationList, ((SourcePropertyAccessorSymbol)implementation).AttributeDeclarationList);
            }

            // If we are asking this question on a partial implementation symbol,
            // it must be from a context which prefers to order implementation attributes before definition attributes.
            // For example, the 'value' parameter of a set accessor.
            if (PartialDefinitionPart is { } definition)
            {
                Debug.Assert(MethodKind == MethodKind.PropertySet);
                return OneOrMany.Create(AttributeDeclarationList, ((SourcePropertyAccessorSymbol)definition).AttributeDeclarationList);
            }

            return OneOrMany.Create(AttributeDeclarationList);
        }

        private SyntaxList<AttributeListSyntax> AttributeDeclarationList
        {
            get
            {
                if (this._property.ContainingType is SourceMemberContainerTypeSymbol { AnyMemberHasAttributes: true })
                {
                    var syntax = this.GetSyntax();
                    switch (syntax.Kind())
                    {
                        case SyntaxKind.GetAccessorDeclaration:
                        case SyntaxKind.SetAccessorDeclaration:
                        case SyntaxKind.InitAccessorDeclaration:
                            return ((AccessorDeclarationSyntax)syntax).AttributeLists;
                    }
                }

                return default;
            }
        }

#nullable enable
        public sealed override string Name
        {
            get
            {
                if (_lazyName is null)
                {
                    bool isGetMethod = this.MethodKind == MethodKind.PropertyGet;
                    string? name = null;

                    if (IsExplicitInterfaceImplementation)
                    {
                        PropertySymbol? explicitlyImplementedPropertyOpt = _property.ExplicitInterfaceImplementations.FirstOrDefault();

                        if (explicitlyImplementedPropertyOpt is object)
                        {
                            MethodSymbol? implementedAccessor = isGetMethod
                                ? explicitlyImplementedPropertyOpt.GetMethod
                                : explicitlyImplementedPropertyOpt.SetMethod;

                            string accessorName = (object)implementedAccessor != null
                                ? implementedAccessor.Name
                                : GetAccessorName(explicitlyImplementedPropertyOpt.MetadataName,
                                    isGetMethod, isWinMdOutput: _property.IsCompilationOutputWinMdObj()); //Not name - could be indexer placeholder

                            string? aliasQualifierOpt = _property.GetExplicitInterfaceSpecifier()?.Name.GetAliasQualifierOpt();
                            name = ExplicitInterfaceHelpers.GetMemberName(accessorName, explicitlyImplementedPropertyOpt.ContainingType, aliasQualifierOpt);
                        }
                    }
                    else if (IsOverride)
                    {
                        MethodSymbol overriddenMethod = this.OverriddenMethod;
                        if ((object)overriddenMethod != null)
                        {
                            // If this accessor is overriding a method from metadata, it is possible that
                            // the name of the overridden method doesn't follow the C# get_X/set_X pattern.
                            // We should copy the name so that the runtime will recognize this as an override.
                            name = overriddenMethod.Name;
                        }
                    }

                    if (name is null)
                    {
                        name = GetAccessorName(_property.SourceName, isGetMethod, isWinMdOutput: _property.IsCompilationOutputWinMdObj());
                    }

                    InterlockedOperations.Initialize(ref _lazyName, name);
                }

                return _lazyName;
            }
        }
#nullable disable

        public override bool IsImplicitlyDeclared
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
                }

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

        private ImmutableArray<ParameterSymbol> ComputeParameters()
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
                parameters.Add(new SourcePropertyClonedParameterSymbolForAccessors(propertyParam, this));
            }

            if (!isGetMethod)
            {
                parameters.Add(new SynthesizedPropertyAccessorValueParameterSymbol(this, parameters.Count));
            }

            return parameters.ToImmutableAndFree();
        }

        internal sealed override void AddSynthesizedReturnTypeAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedReturnTypeAttributes(moduleBuilder, ref attributes);
            AddSynthesizedReturnTypeFlowAnalysisAttributes(ref attributes);
        }

        internal void AddSynthesizedReturnTypeFlowAnalysisAttributes(ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            var annotations = ReturnTypeFlowAnalysisAnnotations;
            if ((annotations & FlowAnalysisAnnotations.MaybeNull) != 0)
            {
                AddSynthesizedAttribute(ref attributes, SynthesizedAttributeData.Create(_property.MaybeNullAttributeIfExists));
            }
            if ((annotations & FlowAnalysisAnnotations.NotNull) != 0)
            {
                AddSynthesizedAttribute(ref attributes, SynthesizedAttributeData.Create(_property.NotNullAttributeIfExists));
            }
        }

#nullable enable
        protected sealed override SourceMemberMethodSymbol? BoundAttributesSource => (SourceMemberMethodSymbol?)PartialDefinitionPart;

        public sealed override MethodSymbol? PartialImplementationPart => _property is SourcePropertySymbol { IsPartialDefinition: true, OtherPartOfPartial: { } other }
            ? (MethodKind == MethodKind.PropertyGet ? other.GetMethod : other.SetMethod)
            : null;

        public sealed override MethodSymbol? PartialDefinitionPart => _property is SourcePropertySymbol { IsPartialImplementation: true, OtherPartOfPartial: { } other }
            ? (MethodKind == MethodKind.PropertyGet ? other.GetMethod : other.SetMethod)
            : null;

        internal bool IsPartialDefinition => _property is SourcePropertySymbol { IsPartialDefinition: true };
        internal bool IsPartialImplementation => _property is SourcePropertySymbol { IsPartialImplementation: true };

        public sealed override bool IsExtern => PartialImplementationPart is { } implementation ? implementation.IsExtern : base.IsExtern;

        internal void PartialAccessorChecks(SourcePropertyAccessorSymbol implementationAccessor, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(IsPartialDefinition);

            if (LocalAccessibility != implementationAccessor.LocalAccessibility)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberAccessibilityDifference, implementationAccessor.GetFirstLocation());
            }

            if (LocalDeclaredReadOnly != implementationAccessor.LocalDeclaredReadOnly)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberReadOnlyDifference, implementationAccessor.GetFirstLocation());
            }

            if (_usesInit != implementationAccessor._usesInit)
            {
                var accessorName = _usesInit ? "init" : "set";
                diagnostics.Add(ErrorCode.ERR_PartialPropertyInitMismatch, implementationAccessor.GetFirstLocation(), implementationAccessor, accessorName);
            }
        }
    }
}
