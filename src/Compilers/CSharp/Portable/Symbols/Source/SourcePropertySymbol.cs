// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourcePropertySymbol : SourcePropertySymbolBase
    {
        private SourcePropertySymbol? _otherPartOfPartial;

        internal static SourcePropertySymbol Create(SourceMemberContainerTypeSymbol containingType, Binder bodyBinder, PropertyDeclarationSyntax syntax, BindingDiagnosticBag diagnostics)
        {
            var nameToken = syntax.Identifier;
            var location = nameToken.GetLocation();
            return Create(containingType, bodyBinder, syntax, nameToken.ValueText, location, diagnostics);
        }

        internal static SourcePropertySymbol Create(SourceMemberContainerTypeSymbol containingType, Binder bodyBinder, IndexerDeclarationSyntax syntax, BindingDiagnosticBag diagnostics)
        {
            var location = syntax.ThisKeyword.GetLocation();
            return Create(containingType, bodyBinder, syntax, DefaultIndexerName, location, diagnostics);
        }

        private static SourcePropertySymbol Create(
            SourceMemberContainerTypeSymbol containingType,
            Binder binder,
            BasePropertyDeclarationSyntax syntax,
            string name,
            Location location,
            BindingDiagnosticBag diagnostics)
        {
            GetAccessorDeclarations(
                syntax,
                diagnostics,
                out bool hasAccessorList,
                out bool accessorsHaveImplementation,
                out bool isInitOnly,
                out var getSyntax,
                out var setSyntax);

            var explicitInterfaceSpecifier = GetExplicitInterfaceSpecifier(syntax);
            SyntaxTokenList modifiersTokenList = GetModifierTokensSyntax(syntax);
            bool isExplicitInterfaceImplementation = explicitInterfaceSpecifier is object;
            var (modifiers, hasExplicitAccessMod) = MakeModifiers(
                containingType,
                modifiersTokenList,
                isExplicitInterfaceImplementation,
                isIndexer: syntax.Kind() == SyntaxKind.IndexerDeclaration,
                accessorsHaveImplementation: accessorsHaveImplementation,
                location,
                diagnostics,
                out _);

            bool isAutoProperty = (modifiers & DeclarationModifiers.Partial) == 0 && !accessorsHaveImplementation;
            bool isExpressionBodied = !hasAccessorList && GetArrowExpression(syntax) != null;

            binder = binder.SetOrClearUnsafeRegionIfNecessary(modifiersTokenList);
            TypeSymbol? explicitInterfaceType;
            string? aliasQualifierOpt;
            string memberName = ExplicitInterfaceHelpers.GetMemberNameAndInterfaceSymbol(binder, explicitInterfaceSpecifier, name, diagnostics, out explicitInterfaceType, out aliasQualifierOpt);

            return new SourcePropertySymbol(
                containingType,
                syntax,
                hasGetAccessor: getSyntax != null || isExpressionBodied,
                hasSetAccessor: setSyntax != null,
                isExplicitInterfaceImplementation,
                explicitInterfaceType,
                aliasQualifierOpt,
                modifiers,
                hasExplicitAccessMod: hasExplicitAccessMod,
                isAutoProperty: isAutoProperty,
                isExpressionBodied: isExpressionBodied,
                isInitOnly: isInitOnly,
                accessorsHaveImplementation: accessorsHaveImplementation,
                memberName,
                location,
                diagnostics);
        }

        private SourcePropertySymbol(
            SourceMemberContainerTypeSymbol containingType,
            BasePropertyDeclarationSyntax syntax,
            bool hasGetAccessor,
            bool hasSetAccessor,
            bool isExplicitInterfaceImplementation,
            TypeSymbol? explicitInterfaceType,
            string? aliasQualifierOpt,
            DeclarationModifiers modifiers,
            bool hasExplicitAccessMod,
            bool isAutoProperty,
            bool isExpressionBodied,
            bool isInitOnly,
            bool accessorsHaveImplementation,
            string memberName,
            Location location,
            BindingDiagnosticBag diagnostics)
            : base(
                containingType,
                syntax,
                hasGetAccessor,
                hasSetAccessor,
                isExplicitInterfaceImplementation,
                explicitInterfaceType,
                aliasQualifierOpt,
                modifiers,
                hasInitializer: HasInitializer(syntax),
                hasExplicitAccessMod: hasExplicitAccessMod,
                isAutoProperty: isAutoProperty,
                isExpressionBodied: isExpressionBodied,
                isInitOnly: isInitOnly,
                accessorsHaveImplementation: accessorsHaveImplementation,
                syntax.Type.SkipScoped(out _).GetRefKindInLocalOrReturn(diagnostics),
                memberName,
                syntax.AttributeLists,
                location,
                diagnostics)
        {
            Debug.Assert(syntax.Type is not ScopedTypeSyntax);

            if (IsAutoProperty)
            {
                Binder.CheckFeatureAvailability(
                    syntax,
                    (hasGetAccessor && !hasSetAccessor) ? MessageID.IDS_FeatureReadonlyAutoImplementedProperties : MessageID.IDS_FeatureAutoImplementedProperties,
                    diagnostics,
                    location);
            }

            CheckForBlockAndExpressionBody(
                syntax.AccessorList,
                syntax.GetExpressionBodySyntax(),
                syntax,
                diagnostics);

            if (syntax is PropertyDeclarationSyntax { Initializer: { } initializer })
                MessageID.IDS_FeatureAutoPropertyInitializer.CheckFeatureAvailability(diagnostics, initializer.EqualsToken);
        }

        internal override void ForceComplete(SourceLocation? locationOpt, Predicate<Symbol>? filter, CancellationToken cancellationToken)
        {
            PartialImplementationPart?.ForceComplete(locationOpt, filter, cancellationToken);
            base.ForceComplete(locationOpt, filter, cancellationToken);
        }

        private TypeSyntax GetTypeSyntax(SyntaxNode syntax) => ((BasePropertyDeclarationSyntax)syntax).Type;

        protected override Location TypeLocation
            => GetTypeSyntax(CSharpSyntaxNode).Location;

        private static SyntaxTokenList GetModifierTokensSyntax(SyntaxNode syntax)
            => ((BasePropertyDeclarationSyntax)syntax).Modifiers;

        private static ArrowExpressionClauseSyntax? GetArrowExpression(SyntaxNode syntax)
            => syntax switch
            {
                PropertyDeclarationSyntax p => p.ExpressionBody,
                IndexerDeclarationSyntax i => i.ExpressionBody,
                _ => throw ExceptionUtilities.UnexpectedValue(syntax.Kind())
            };

        private static bool HasInitializer(SyntaxNode syntax)
            => syntax is PropertyDeclarationSyntax { Initializer: { } };

        public override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            // Attributes on partial properties are owned by the definition part.
            // If this symbol has a non-null PartialDefinitionPart, we should have accessed this method through that definition symbol instead
            Debug.Assert(PartialDefinitionPart is null
                // We might still get here when asking for the attributes on a backing field.
                // This is an error scenario (requires using a property initializer and field-targeted attributes on partial property implementation part).
                || this.BackingField is not null);

            if (PartialImplementationPart is { } implementationPart)
            {
                return OneOrMany.Create(
                    ((BasePropertyDeclarationSyntax)CSharpSyntaxNode).AttributeLists,
                    ((BasePropertyDeclarationSyntax)implementationPart.CSharpSyntaxNode).AttributeLists);
            }
            else
            {
                return OneOrMany.Create(((BasePropertyDeclarationSyntax)CSharpSyntaxNode).AttributeLists);
            }
        }

        protected override SourcePropertySymbolBase? BoundAttributesSource => PartialDefinitionPart;

        public override IAttributeTargetSymbol AttributesOwner => this;

        private static void GetAccessorDeclarations(
            CSharpSyntaxNode syntaxNode,
            BindingDiagnosticBag diagnostics,
            out bool hasAccessorList,
            out bool accessorsHaveImplementation,
            out bool isInitOnly,
            out CSharpSyntaxNode? getSyntax,
            out CSharpSyntaxNode? setSyntax)
        {
            var syntax = (BasePropertyDeclarationSyntax)syntaxNode;
            hasAccessorList = syntax.AccessorList != null;
            getSyntax = null;
            setSyntax = null;
            isInitOnly = false;

            if (hasAccessorList)
            {
                accessorsHaveImplementation = false;
                foreach (var accessor in syntax.AccessorList!.Accessors)
                {
                    switch (accessor.Kind())
                    {
                        case SyntaxKind.GetAccessorDeclaration:
                            if (getSyntax == null)
                            {
                                getSyntax = accessor;
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateAccessor, accessor.Keyword.GetLocation());
                            }
                            break;
                        case SyntaxKind.SetAccessorDeclaration:
                        case SyntaxKind.InitAccessorDeclaration:
                            if (setSyntax == null)
                            {
                                setSyntax = accessor;
                                if (accessor.Keyword.IsKind(SyntaxKind.InitKeyword))
                                {
                                    isInitOnly = true;
                                }
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateAccessor, accessor.Keyword.GetLocation());
                            }
                            break;
                        case SyntaxKind.AddAccessorDeclaration:
                        case SyntaxKind.RemoveAccessorDeclaration:
                            diagnostics.Add(ErrorCode.ERR_GetOrSetExpected, accessor.Keyword.GetLocation());
                            continue;
                        case SyntaxKind.UnknownAccessorDeclaration:
                            // We don't need to report an error here as the parser will already have
                            // done that for us.
                            continue;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(accessor.Kind());
                    }

                    if (accessor.Body != null || accessor.ExpressionBody != null)
                    {
                        accessorsHaveImplementation = true;
                    }
                }
            }
            else
            {
                accessorsHaveImplementation = GetArrowExpression(syntax) is object;
                Debug.Assert(accessorsHaveImplementation); // it's not clear how this even parsed as a property if it has no accessor list and no arrow expression.
            }
        }

        private static AccessorDeclarationSyntax GetGetAccessorDeclaration(BasePropertyDeclarationSyntax syntax)
        {
            foreach (var accessor in syntax.AccessorList!.Accessors)
            {
                switch (accessor.Kind())
                {
                    case SyntaxKind.GetAccessorDeclaration:
                        return accessor;
                }
            }

            throw ExceptionUtilities.Unreachable();
        }

        private static AccessorDeclarationSyntax GetSetAccessorDeclaration(BasePropertyDeclarationSyntax syntax)
        {
            foreach (var accessor in syntax.AccessorList!.Accessors)
            {
                switch (accessor.Kind())
                {
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.InitAccessorDeclaration:
                        return accessor;
                }
            }

            throw ExceptionUtilities.Unreachable();
        }

        private static (DeclarationModifiers modifiers, bool hasExplicitAccessMod) MakeModifiers(
            NamedTypeSymbol containingType,
            SyntaxTokenList modifiers,
            bool isExplicitInterfaceImplementation,
            bool isIndexer,
            bool accessorsHaveImplementation,
            Location location,
            BindingDiagnosticBag diagnostics,
            out bool modifierErrors)
        {
            bool inInterface = containingType.IsInterface;
            bool inExtension = containingType.IsExtension;
            var defaultAccess = inInterface && !isExplicitInterfaceImplementation ? DeclarationModifiers.Public : DeclarationModifiers.Private;

            // Check that the set of modifiers is allowed
            var allowedModifiers = DeclarationModifiers.Partial | DeclarationModifiers.Unsafe;
            var defaultInterfaceImplementationModifiers = DeclarationModifiers.None;

            if (inExtension)
            {
                allowedModifiers |= DeclarationModifiers.New |
                                    DeclarationModifiers.AccessibilityMask;

                if (!isIndexer)
                {
                    allowedModifiers |= DeclarationModifiers.Static;
                }
            }
            else if (!isExplicitInterfaceImplementation)
            {
                allowedModifiers |= DeclarationModifiers.New |
                                    DeclarationModifiers.Sealed |
                                    DeclarationModifiers.Abstract |
                                    DeclarationModifiers.Virtual |
                                    DeclarationModifiers.AccessibilityMask;

                if (!isIndexer)
                {
                    allowedModifiers |= DeclarationModifiers.Static;
                }

                if (!inInterface)
                {
                    allowedModifiers |= DeclarationModifiers.Override;

                    if (!isIndexer)
                    {
                        allowedModifiers |= DeclarationModifiers.Required;
                    }
                }
                else
                {
                    // This is needed to make sure we can detect 'public' modifier specified explicitly and
                    // check it against language version below.
                    defaultAccess = DeclarationModifiers.None;

                    defaultInterfaceImplementationModifiers |= DeclarationModifiers.Sealed |
                                                               DeclarationModifiers.Abstract |
                                                               (isIndexer ? 0 : DeclarationModifiers.Static) |
                                                               DeclarationModifiers.Virtual |
                                                               DeclarationModifiers.Extern |
                                                               DeclarationModifiers.AccessibilityMask;
                }
            }
            else
            {
                Debug.Assert(isExplicitInterfaceImplementation);

                if (inInterface)
                {
                    allowedModifiers |= DeclarationModifiers.Abstract;
                }

                if (!isIndexer)
                {
                    allowedModifiers |= DeclarationModifiers.Static;
                }
            }

            if (containingType.IsStructType())
            {
                allowedModifiers |= DeclarationModifiers.ReadOnly;
            }

            allowedModifiers |= DeclarationModifiers.Extern;

            // In order to detect whether explicit accessibility mods were provided, we pass the default value
            // for 'defaultAccess' and manually add in the 'defaultAccess' flags after the call.
            bool hasExplicitAccessMod;
            var mods = ModifierUtils.MakeAndCheckNonTypeMemberModifiers(isOrdinaryMethod: false, isForInterfaceMember: inInterface,
                                                                        modifiers, defaultAccess: DeclarationModifiers.None, allowedModifiers, location, diagnostics, out modifierErrors);
            if ((mods & DeclarationModifiers.AccessibilityMask) == 0)
            {
                hasExplicitAccessMod = false;
                mods |= defaultAccess;
            }
            else
            {
                hasExplicitAccessMod = true;
            }

            if ((mods & DeclarationModifiers.Partial) != 0)
            {
                Debug.Assert(location.SourceTree is not null);

                LanguageVersion availableVersion = ((CSharpParseOptions)location.SourceTree.Options).LanguageVersion;
                LanguageVersion requiredVersion = MessageID.IDS_FeaturePartialProperties.RequiredVersion();
                if (availableVersion < requiredVersion)
                {
                    ModifierUtils.ReportUnsupportedModifiersForLanguageVersion(mods, DeclarationModifiers.Partial, location, diagnostics, availableVersion, requiredVersion);
                }
            }

            ModifierUtils.CheckFeatureAvailabilityForStaticAbstractMembersInInterfacesIfNeeded(mods, isExplicitInterfaceImplementation, location, diagnostics);

            containingType.CheckUnsafeModifier(mods, location, diagnostics);

            ModifierUtils.ReportDefaultInterfaceImplementationModifiers(accessorsHaveImplementation, mods,
                                                                        defaultInterfaceImplementationModifiers,
                                                                        location, diagnostics);

            // Let's overwrite modifiers for interface properties with what they are supposed to be.
            // Proper errors must have been reported by now.
            if (inInterface)
            {
                mods = ModifierUtils.AdjustModifiersForAnInterfaceMember(mods, accessorsHaveImplementation, isExplicitInterfaceImplementation);
            }

            if (isIndexer)
            {
                mods |= DeclarationModifiers.Indexer;
            }

            if ((mods & DeclarationModifiers.Static) != 0 && (mods & DeclarationModifiers.Required) != 0)
            {
                // The modifier 'required' is not valid for this item
                diagnostics.Add(ErrorCode.ERR_BadMemberFlag, location, SyntaxFacts.GetText(SyntaxKind.RequiredKeyword));
                mods &= ~DeclarationModifiers.Required;
            }

            return (mods, hasExplicitAccessMod);
        }

        protected override SourcePropertyAccessorSymbol CreateGetAccessorSymbol(bool isAutoPropertyAccessor, BindingDiagnosticBag diagnostics)
        {
            var syntax = (BasePropertyDeclarationSyntax)CSharpSyntaxNode;
            ArrowExpressionClauseSyntax? arrowExpression = GetArrowExpression(syntax);

            if (syntax.AccessorList is null && arrowExpression != null)
            {
                return CreateExpressionBodiedAccessor(
                    arrowExpression,
                    diagnostics);
            }
            else
            {
                return CreateAccessorSymbol(GetGetAccessorDeclaration(syntax), isAutoPropertyAccessor, diagnostics);
            }
        }

        protected override SourcePropertyAccessorSymbol CreateSetAccessorSymbol(bool isAutoPropertyAccessor, BindingDiagnosticBag diagnostics)
        {
            var syntax = (BasePropertyDeclarationSyntax)CSharpSyntaxNode;
            Debug.Assert(!(syntax.AccessorList is null && GetArrowExpression(syntax) != null));

            return CreateAccessorSymbol(GetSetAccessorDeclaration(syntax), isAutoPropertyAccessor, diagnostics);
        }

        private SourcePropertyAccessorSymbol CreateAccessorSymbol(
            AccessorDeclarationSyntax syntax,
            bool isAutoPropertyAccessor,
            BindingDiagnosticBag diagnostics)
        {
            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(
                ContainingType,
                this,
                _modifiers,
                syntax,
                isAutoPropertyAccessor,
                diagnostics);
        }

        private SourcePropertyAccessorSymbol CreateExpressionBodiedAccessor(
            ArrowExpressionClauseSyntax syntax,
            BindingDiagnosticBag diagnostics)
        {
            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(
                ContainingType,
                this,
                _modifiers,
                syntax,
                diagnostics);
        }

        private Binder CreateBinderForTypeAndParameters()
        {
            var compilation = this.DeclaringCompilation;
            var syntaxTree = SyntaxTree;
            var syntax = CSharpSyntaxNode;
            var binderFactory = compilation.GetBinderFactory(syntaxTree);
            var binder = binderFactory.GetBinder(syntax, syntax, this);
            SyntaxTokenList modifiers = GetModifierTokensSyntax(syntax);
            binder = binder.SetOrClearUnsafeRegionIfNecessary(modifiers);
            return binder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);
        }

        protected override (TypeWithAnnotations Type, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindType(BindingDiagnosticBag diagnostics)
        {
            Binder binder = CreateBinderForTypeAndParameters();
            var syntax = CSharpSyntaxNode;

            return (ComputeType(binder, syntax, diagnostics), ComputeParameters(binder, syntax, diagnostics));
        }

        private TypeWithAnnotations ComputeType(Binder binder, SyntaxNode syntax, BindingDiagnosticBag diagnostics)
        {
            var typeSyntax = GetTypeSyntax(syntax);
            Debug.Assert(typeSyntax is not ScopedTypeSyntax);

            typeSyntax = typeSyntax.SkipScoped(out _).SkipRef();
            var type = binder.BindType(typeSyntax, diagnostics);
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagnostics);

            if (GetExplicitInterfaceSpecifier() is null && !this.IsNoMoreVisibleThan(type, ref useSiteInfo))
            {
                // "Inconsistent accessibility: indexer return type '{1}' is less accessible than indexer '{0}'"
                // "Inconsistent accessibility: property type '{1}' is less accessible than property '{0}'"
                diagnostics.Add((this.IsIndexer ? ErrorCode.ERR_BadVisIndexerReturn : ErrorCode.ERR_BadVisPropertyType), Location, this, type.Type);
            }

            if (type.Type.HasFileLocalTypes() && !ContainingType.HasFileLocalTypes())
            {
                diagnostics.Add(ErrorCode.ERR_FileTypeDisallowedInSignature, Location, type.Type, ContainingType);
            }

            diagnostics.Add(Location, useSiteInfo);

            if (type.IsVoidType())
            {
                if (this.IsIndexer)
                {
                    diagnostics.Add(ErrorCode.ERR_IndexerCantHaveVoidType, Location);
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_PropertyCantHaveVoidType, Location, this);
                }
            }

            return type;
        }

        private static ImmutableArray<ParameterSymbol> MakeParameters(
            Binder binder, SourcePropertySymbolBase owner, BaseParameterListSyntax? parameterSyntaxOpt, BindingDiagnosticBag diagnostics, bool addRefReadOnlyModifier)
        {
            if (parameterSyntaxOpt == null)
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }

            if (parameterSyntaxOpt.Parameters.Count < 1)
            {
                diagnostics.Add(ErrorCode.ERR_IndexerNeedsParam, parameterSyntaxOpt.GetLastToken().GetLocation());
            }

            SyntaxToken arglistToken;
            var parameters = ParameterHelpers.MakeParameters(
                binder, owner, parameterSyntaxOpt, out arglistToken,
                allowRefOrOut: false,
                allowThis: false,
                addRefReadOnlyModifier: addRefReadOnlyModifier,
                diagnostics: diagnostics).Cast<SourceParameterSymbol, ParameterSymbol>();

            if (arglistToken.Kind() != SyntaxKind.None)
            {
                diagnostics.Add(ErrorCode.ERR_IllegalVarArgs, arglistToken.GetLocation());
            }

            // There is a special warning for an indexer with exactly one parameter, which is optional.
            // ParameterHelpers already warns for default values on explicit interface implementations.
            if (parameters.Length == 1 && !owner.IsExplicitInterfaceImplementation)
            {
                ParameterSyntax parameterSyntax = parameterSyntaxOpt.Parameters[0];
                if (parameterSyntax.Default != null)
                {
                    SyntaxToken paramNameToken = parameterSyntax.Identifier;
                    diagnostics.Add(ErrorCode.WRN_DefaultValueForUnconsumedLocation, paramNameToken.GetLocation(), paramNameToken.ValueText);
                }
            }

            return parameters;
        }

        private ImmutableArray<ParameterSymbol> ComputeParameters(Binder binder, CSharpSyntaxNode syntax, BindingDiagnosticBag diagnostics)
        {
            var parameterSyntaxOpt = GetParameterListSyntax(syntax);
            var parameters = MakeParameters(binder, this, parameterSyntaxOpt, diagnostics, addRefReadOnlyModifier: IsVirtual || IsAbstract);
            return parameters;
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

            foreach (ParameterSymbol param in Parameters)
            {
                if (!IsExplicitInterfaceImplementation && !this.IsNoMoreVisibleThan(param.Type, ref useSiteInfo))
                {
                    diagnostics.Add(ErrorCode.ERR_BadVisIndexerParam, Location, this, param.Type);
                }
                else if (param.Type.HasFileLocalTypes() && !this.ContainingType.HasFileLocalTypes())
                {
                    diagnostics.Add(ErrorCode.ERR_FileTypeDisallowedInSignature, Location, param.Type, this.ContainingType);
                }
                else if (SetMethod is object && param.Name == ParameterSymbol.ValueParameterName)
                {
                    diagnostics.Add(ErrorCode.ERR_DuplicateGeneratedName, param.TryGetFirstLocation() ?? Location, param.Name);
                }
            }

            diagnostics.Add(Location, useSiteInfo);

            if (IsPartialDefinition && OtherPartOfPartial is { } implementation)
            {
                PartialPropertyChecks(implementation, diagnostics);
                implementation.CheckInitializerIfNeeded(diagnostics);
            }
        }

        /// <remarks>
        /// This method is analogous to <see cref="SourceOrdinaryMethodSymbol.PartialMethodChecks(SourceOrdinaryMethodSymbol, SourceOrdinaryMethodSymbol, BindingDiagnosticBag)" />.
        /// Whenever new checks are added to this method, the other method should also have those checks added, if applicable.
        /// </remarks>
        private void PartialPropertyChecks(SourcePropertySymbol implementation, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(this.IsPartialDefinition);
            Debug.Assert((object)this != implementation);
            Debug.Assert((object?)this.OtherPartOfPartial == implementation);

            // The purpose of this flag is to avoid cascading a type difference error with an additional redundant warning.
            bool hasTypeDifferences = !TypeWithAnnotations.Equals(implementation.TypeWithAnnotations, TypeCompareKind.AllIgnoreOptions);

            if (hasTypeDifferences)
            {
                diagnostics.Add(ErrorCode.ERR_PartialPropertyTypeDifference, implementation.GetFirstLocation());
            }
            else if (MemberSignatureComparer.ConsideringTupleNamesCreatesDifference(this, implementation))
            {
                hasTypeDifferences = true;
                diagnostics.Add(ErrorCode.ERR_PartialMemberInconsistentTupleNames, implementation.GetFirstLocation(), this, implementation);
            }

            if (RefKind != implementation.RefKind)
            {
                hasTypeDifferences = true;
                diagnostics.Add(ErrorCode.ERR_PartialMemberRefReturnDifference, implementation.GetFirstLocation());
            }

            if ((!hasTypeDifferences && !MemberSignatureComparer.PartialMethodsStrictComparer.Equals(this, implementation))
                || !Parameters.SequenceEqual(implementation.Parameters, (a, b) => a.Name == b.Name))
            {
                diagnostics.Add(ErrorCode.WRN_PartialPropertySignatureDifference, implementation.GetFirstLocation(),
                    new FormattedSymbol(this, SymbolDisplayFormat.MinimallyQualifiedFormat),
                    new FormattedSymbol(implementation, SymbolDisplayFormat.MinimallyQualifiedFormat));
            }

            if (IsRequired != implementation.IsRequired)
            {
                diagnostics.Add(ErrorCode.ERR_PartialPropertyRequiredDifference, implementation.GetFirstLocation());
            }

            if (IsStatic != implementation.IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberStaticDifference, implementation.GetFirstLocation());
            }

            if (HasReadOnlyModifier != implementation.HasReadOnlyModifier)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberReadOnlyDifference, implementation.GetFirstLocation());
            }

            if ((_modifiers & DeclarationModifiers.Unsafe) != (implementation._modifiers & DeclarationModifiers.Unsafe) && this.CompilationAllowsUnsafe()) // Don't cascade.
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberUnsafeDifference, implementation.GetFirstLocation());
            }

            if (this.IsParams() != implementation.IsParams())
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberParamsDifference, implementation.GetFirstLocation());
            }

            if (DeclaredAccessibility != implementation.DeclaredAccessibility
                || HasExplicitAccessModifier != implementation.HasExplicitAccessModifier)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberAccessibilityDifference, implementation.GetFirstLocation());
            }

            if (IsVirtual != implementation.IsVirtual
                || IsOverride != implementation.IsOverride
                || IsSealed != implementation.IsSealed
                || IsNew != implementation.IsNew)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberExtendedModDifference, implementation.GetFirstLocation());
            }

            Debug.Assert(this.ParameterCount == implementation.ParameterCount);
            for (var i = 0; i < this.ParameterCount; i++)
            {
                // An error is only reported for a modifier difference here, regardless of whether the difference is safe or not.
                // Presence of UnscopedRefAttribute is also not considered when checking partial signatures, because when the attribute is used, it will affect both parts the same way.
                var definitionParameter = (SourceParameterSymbol)this.Parameters[i];
                var implementationParameter = (SourceParameterSymbol)implementation.Parameters[i];
                if (definitionParameter.DeclaredScope != implementationParameter.DeclaredScope)
                {
                    diagnostics.Add(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, implementation.GetFirstLocation(), new FormattedSymbol(implementation.Parameters[i], SymbolDisplayFormat.ShortFormat));
                }
            }

            if (this.GetMethod is { } definitionGetAccessor && implementation.GetMethod is { } implementationGetAccessor)
            {
                ((SourcePropertyAccessorSymbol)definitionGetAccessor).PartialAccessorChecks((SourcePropertyAccessorSymbol)implementationGetAccessor, diagnostics);
            }

            if (this.SetMethod is { } definitionSetAccessor && implementation.SetMethod is { } implementationSetAccessor)
            {
                ((SourcePropertyAccessorSymbol)definitionSetAccessor).PartialAccessorChecks((SourcePropertyAccessorSymbol)implementationSetAccessor, diagnostics);
            }
        }

        private static BaseParameterListSyntax? GetParameterListSyntax(CSharpSyntaxNode syntax)
            => (syntax as IndexerDeclarationSyntax)?.ParameterList;

        public sealed override bool IsExtern => PartialImplementationPart is { } implementation ? implementation.IsExtern : HasExternModifier;

        internal SourcePropertySymbol? OtherPartOfPartial => _otherPartOfPartial;

        internal bool IsPartialDefinition => IsPartial && !AccessorsHaveImplementation && !HasExternModifier;

        internal bool IsPartialImplementation => IsPartial && (AccessorsHaveImplementation || HasExternModifier);

        internal SourcePropertySymbol? PartialDefinitionPart => IsPartialImplementation ? OtherPartOfPartial : null;

        internal SourcePropertySymbol? PartialImplementationPart => IsPartialDefinition ? OtherPartOfPartial : null;

        internal static void InitializePartialPropertyParts(SourcePropertySymbol definition, SourcePropertySymbol implementation)
        {
            Debug.Assert(definition.IsPartialDefinition);
            Debug.Assert(implementation.IsPartialImplementation);

            Debug.Assert(definition._otherPartOfPartial is not { } alreadySetImplPart || alreadySetImplPart == implementation);
            Debug.Assert(implementation._otherPartOfPartial is not { } alreadySetDefPart || alreadySetDefPart == definition);

            definition._otherPartOfPartial = implementation;
            implementation._otherPartOfPartial = definition;

            Debug.Assert(definition._otherPartOfPartial == implementation);
            Debug.Assert(implementation._otherPartOfPartial == definition);
        }
    }
}
