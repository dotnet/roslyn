// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourcePropertyAccessorSymbol : SourceMethodSymbol
    {
        private readonly SourcePropertySymbol _property;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeSymbol _lazyReturnType;
        private ImmutableArray<CustomModifier> _lazyReturnTypeCustomModifiers;
        private readonly ImmutableArray<MethodSymbol> _explicitInterfaceImplementations;
        private readonly string _name;
        private readonly bool _isAutoPropertyAccessor;

        public static SourcePropertyAccessorSymbol CreateAccessorSymbol(
            NamedTypeSymbol containingType,
            SourcePropertySymbol property,
            DeclarationModifiers propertyModifiers,
            string propertyName,
            AccessorDeclarationSyntax syntax,
            PropertySymbol explicitlyImplementedPropertyOpt,
            string aliasQualifierOpt,
            bool isAutoPropertyAccessor,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax.Kind() == SyntaxKind.GetAccessorDeclaration || syntax.Kind() == SyntaxKind.SetAccessorDeclaration);

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
            return new SourcePropertyAccessorSymbol(
                containingType,
                name,
                property,
                propertyModifiers,
                explicitInterfaceImplementations,
                syntax.Keyword.GetLocation(),
                syntax,
                methodKind,
                isAutoPropertyAccessor,
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
                diagnostics);
        }

        internal override bool IsExpressionBodied
        {
            get
            {
                return _property.IsExpressionBodied;
            }
        }

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
            DiagnosticBag diagnostics) :
            base(containingType, syntax.GetReference(), syntax.GetReference(), location)
        {
            _property = property;
            _explicitInterfaceImplementations = explicitInterfaceImplementations;
            _name = name;
            _isAutoPropertyAccessor = false;

            // The modifiers for the accessor are the same as the modifiers for the property,
            // minus the indexer bit
            var declarationModifiers = propertyModifiers & ~DeclarationModifiers.Indexer;

            // ReturnsVoid property is overridden in this class so
            // returnsVoid argument to MakeFlags is ignored.
            this.MakeFlags(MethodKind.PropertyGet, declarationModifiers, returnsVoid: false, isExtensionMethod: false,
                isMetadataVirtualIgnoringModifiers: explicitInterfaceImplementations.Any());

            CheckModifiersForBody(location, diagnostics);

            var info = ModifierUtils.CheckAccessibility(this.DeclarationModifiers);
            if (info != null)
            {
                diagnostics.Add(info, location);
            }

            this.CheckModifiers(location, isAutoPropertyOrExpressionBodied: true, diagnostics: diagnostics);

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

        private SourcePropertyAccessorSymbol(
            NamedTypeSymbol containingType,
            string name,
            SourcePropertySymbol property,
            DeclarationModifiers propertyModifiers,
            ImmutableArray<MethodSymbol> explicitInterfaceImplementations,
            Location location,
            AccessorDeclarationSyntax syntax,
            MethodKind methodKind,
            bool isAutoPropertyAccessor,
            DiagnosticBag diagnostics) :
            base(containingType, syntax.GetReference(), syntax.Body?.GetReference(), location)
        {
            _property = property;
            _explicitInterfaceImplementations = explicitInterfaceImplementations;
            _name = name;
            _isAutoPropertyAccessor = isAutoPropertyAccessor;

            bool modifierErrors;
            var declarationModifiers = this.MakeModifiers(syntax, location, diagnostics, out modifierErrors);

            // Include modifiers from the containing property.
            propertyModifiers &= ~DeclarationModifiers.AccessibilityMask;
            if ((declarationModifiers & DeclarationModifiers.Private) != 0)
            {
                // Private accessors cannot be virtual.
                propertyModifiers &= ~DeclarationModifiers.Virtual;
            }
            declarationModifiers |= propertyModifiers & ~DeclarationModifiers.Indexer;

            // ReturnsVoid property is overridden in this class so
            // returnsVoid argument to MakeFlags is ignored.
            this.MakeFlags(methodKind, declarationModifiers, returnsVoid: false, isExtensionMethod: false,
                isMetadataVirtualIgnoringModifiers: explicitInterfaceImplementations.Any());

            var bodyOpt = syntax.Body;
            if (bodyOpt != null)
            {
                CheckModifiersForBody(location, diagnostics);
            }

            var info = ModifierUtils.CheckAccessibility(this.DeclarationModifiers);
            if (info != null)
            {
                diagnostics.Add(info, location);
            }

            if (!modifierErrors)
            {
                this.CheckModifiers(location, isAutoPropertyAccessor, diagnostics);
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

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            // These values may not be final, but we need to have something set here in the
            // event that we need to find the overridden accessor.
            _lazyParameters = ComputeParameters(diagnostics);
            _lazyReturnType = ComputeReturnType(diagnostics);
            _lazyReturnTypeCustomModifiers = ImmutableArray<CustomModifier>.Empty;

            if (_explicitInterfaceImplementations.Length > 0)
            {
                Debug.Assert(_explicitInterfaceImplementations.Length == 1);
                MethodSymbol implementedMethod = _explicitInterfaceImplementations[0];
                CustomModifierUtils.CopyMethodCustomModifiers(implementedMethod, this, out _lazyReturnType, out _lazyReturnTypeCustomModifiers, out _lazyParameters, alsoCopyParamsModifier: false);
            }
            else if (this.IsOverride)
            {
                // This will cause another call to SourceMethodSymbol.LazyMethodChecks, 
                // but that method already handles reentrancy for exactly this case.
                MethodSymbol overriddenMethod = this.OverriddenMethod;
                if ((object)overriddenMethod != null)
                {
                    CustomModifierUtils.CopyMethodCustomModifiers(overriddenMethod, this, out _lazyReturnType, out _lazyReturnTypeCustomModifiers, out _lazyParameters, alsoCopyParamsModifier: true);
                }
            }
            else if (_lazyReturnType.SpecialType != SpecialType.System_Void)
            {
                PropertySymbol associatedProperty = _property;
                _lazyReturnType = CustomModifierUtils.CopyTypeCustomModifiers(associatedProperty.Type, _lazyReturnType, RefKind.None, this.ContainingAssembly);
                _lazyReturnTypeCustomModifiers = associatedProperty.TypeCustomModifiers;
            }
        }

        public override Accessibility DeclaredAccessibility
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

        public override Symbol AssociatedSymbol
        {
            get { return _property; }
        }

        public override bool IsVararg
        {
            get { return false; }
        }

        public override bool ReturnsVoid
        {
            get { return this.ReturnType.SpecialType == SpecialType.System_Void; }
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

        public override TypeSymbol ReturnType
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnType;
            }
        }

        private TypeSymbol ComputeReturnType(DiagnosticBag diagnostics)
        {
            if (this.MethodKind == MethodKind.PropertyGet)
            {
                var type = _property.Type;
                if (!ContainingType.IsInterfaceType() && type.IsStatic)
                {
                    // '{0}': static types cannot be used as return types
                    diagnostics.Add(ErrorCode.ERR_ReturnTypeIsStaticClass, this.locations[0], type);
                }
                return type;
            }
            else
            {
                var binder = GetBinder();
                return binder.GetSpecialType(SpecialType.System_Void, diagnostics, this.GetSyntax());
            }
        }

        private Binder GetBinder()
        {
            var syntax = this.GetSyntax();
            var compilation = this.DeclaringCompilation;
            var binderFactory = compilation.GetBinderFactory(syntax.SyntaxTree);
            return binderFactory.GetBinder(syntax);
        }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnTypeCustomModifiers;
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

        private DeclarationModifiers MakeModifiers(AccessorDeclarationSyntax syntax, Location location, DiagnosticBag diagnostics, out bool modifierErrors)
        {
            // No default accessibility. If unset, accessibility
            // will be inherited from the property.
            const DeclarationModifiers defaultAccess = DeclarationModifiers.None;

            // Check that the set of modifiers is allowed
            const DeclarationModifiers allowedModifiers = DeclarationModifiers.AccessibilityMask;
            var mods = ModifierUtils.MakeAndCheckNontypeMemberModifiers(syntax.Modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors);

            // For interface, check there are no accessibility modifiers.
            // (This check is handled outside of MakeAndCheckModifiers
            // since a distinct error message is reported for interfaces.)
            if (this.ContainingType.IsInterface)
            {
                if ((mods & DeclarationModifiers.AccessibilityMask) != 0)
                {
                    diagnostics.Add(ErrorCode.ERR_PropertyAccessModInInterface, location, this);
                    mods = (mods & ~DeclarationModifiers.AccessibilityMask);
                }
            }

            return mods;
        }

        private void CheckModifiers(Location location, bool isAutoPropertyOrExpressionBodied, DiagnosticBag diagnostics)
        {
            // Check accessibility against the accessibility declared on the accessor not the property.
            var localAccessibility = this.LocalAccessibility;

            if (IsAbstract && !ContainingType.IsAbstract && (ContainingType.TypeKind == TypeKind.Class || ContainingType.TypeKind == TypeKind.Submission))
            {
                // '{0}' is abstract but it is contained in non-abstract class '{1}'
                diagnostics.Add(ErrorCode.ERR_AbstractInConcreteClass, location, this, ContainingType);
            }
            else if (IsVirtual && ContainingType.IsSealed && ContainingType.TypeKind != TypeKind.Struct) // error CS0106 on struct already
            {
                // '{0}' is a new virtual member in sealed class '{1}'
                diagnostics.Add(ErrorCode.ERR_NewVirtualInSealed, location, this, ContainingType);
            }
            else if (bodySyntaxReferenceOpt == null && !IsExtern && !IsAbstract && !isAutoPropertyOrExpressionBodied)
            {
                diagnostics.Add(ErrorCode.ERR_ConcreteMissingBody, location, this);
            }
            else if (ContainingType.IsSealed && localAccessibility.HasProtected() && !this.IsOverride)
            {
                diagnostics.Add(AccessCheck.GetProtectedMemberInSealedTypeError(ContainingType), location, this);
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
        /// <see cref="AccessorDeclarationSyntax"/> or <see cref="ArrowExpressionClauseSyntax"/>
        /// </returns>
        internal CSharpSyntaxNode GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (CSharpSyntaxNode)syntaxReferenceOpt.GetSyntax();
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return _explicitInterfaceImplementations;
            }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            var syntax = this.GetSyntax();
            switch (syntax.Kind())
            {
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                    return OneOrMany.Create(((AccessorDeclarationSyntax)syntax).AttributeLists);
            }

            return base.GetAttributeDeclarations();
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                // Per design meeting resolution [see bug 11253], no source accessor is implicitly declared in C#,
                // because there is "get", "set", or expression-body syntax.
                return false;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get
            {
                return true;
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
                var propertyType = _property.Type;
                if (!ContainingType.IsInterfaceType() && propertyType.IsStatic)
                {
                    // '{0}': static types cannot be used as parameters
                    diagnostics.Add(ErrorCode.ERR_ParameterIsStaticClass, this.locations[0], propertyType);
                }

                parameters.Add(new SynthesizedAccessorValueParameterSymbol(this, propertyType, parameters.Count, _property.TypeCustomModifiers));
            }

            return parameters.ToImmutableAndFree();
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            if (_isAutoPropertyAccessor)
            {
                var compilation = this.DeclaringCompilation;
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            }
        }
    }
}
