// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourcePropertySymbol : SourcePropertySymbolBase
    {
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
                out bool isAutoProperty,
                out bool hasAccessorList,
                out bool accessorsHaveImplementation,
                out bool isInitOnly,
                out var getSyntax,
                out var setSyntax);

            var explicitInterfaceSpecifier = GetExplicitInterfaceSpecifier(syntax);
            SyntaxTokenList modifiersTokenList = GetModifierTokensSyntax(syntax);
            bool isExplicitInterfaceImplementation = explicitInterfaceSpecifier is object;
            var modifiers = MakeModifiers(
                containingType,
                modifiersTokenList,
                isExplicitInterfaceImplementation,
                isIndexer: syntax.Kind() == SyntaxKind.IndexerDeclaration,
                accessorsHaveImplementation: accessorsHaveImplementation,
                location,
                diagnostics,
                out _);

            bool isExpressionBodied = !hasAccessorList && GetArrowExpression(syntax) != null;

            binder = binder.WithUnsafeRegionIfNecessary(modifiersTokenList);
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
                isAutoProperty: isAutoProperty,
                isExpressionBodied: isExpressionBodied,
                isInitOnly: isInitOnly,
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
            bool isAutoProperty,
            bool isExpressionBodied,
            bool isInitOnly,
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
                isAutoProperty: isAutoProperty,
                isExpressionBodied: isExpressionBodied,
                isInitOnly: isInitOnly,
                syntax.Type.GetRefKind(),
                memberName,
                syntax.AttributeLists,
                location,
                diagnostics)
        {
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

        public override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => ((BasePropertyDeclarationSyntax)CSharpSyntaxNode).AttributeLists;

        public override IAttributeTargetSymbol AttributesOwner => this;

        private static void GetAccessorDeclarations(
            CSharpSyntaxNode syntaxNode,
            BindingDiagnosticBag diagnostics,
            out bool isAutoProperty,
            out bool hasAccessorList,
            out bool accessorsHaveImplementation,
            out bool isInitOnly,
            out CSharpSyntaxNode? getSyntax,
            out CSharpSyntaxNode? setSyntax)
        {
            var syntax = (BasePropertyDeclarationSyntax)syntaxNode;
            isAutoProperty = true;
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
                        isAutoProperty = false;
                        accessorsHaveImplementation = true;
                    }
                }
            }
            else
            {
                isAutoProperty = false;
                accessorsHaveImplementation = GetArrowExpression(syntax) is object;
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

            throw ExceptionUtilities.Unreachable;
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

            throw ExceptionUtilities.Unreachable;
        }

        private static DeclarationModifiers MakeModifiers(
            NamedTypeSymbol containingType,
            SyntaxTokenList modifiers,
            bool isExplicitInterfaceImplementation,
            bool isIndexer,
            bool accessorsHaveImplementation,
            Location location,
            BindingDiagnosticBag diagnostics,
            out bool modifierErrors)
        {
            bool isInterface = containingType.IsInterface;
            var defaultAccess = isInterface && !isExplicitInterfaceImplementation ? DeclarationModifiers.Public : DeclarationModifiers.Private;

            // Check that the set of modifiers is allowed
            var allowedModifiers = DeclarationModifiers.Unsafe;
            var defaultInterfaceImplementationModifiers = DeclarationModifiers.None;

            if (!isExplicitInterfaceImplementation)
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

                if (!isInterface)
                {
                    allowedModifiers |= DeclarationModifiers.Override;
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

                if (isInterface)
                {
                    allowedModifiers |= DeclarationModifiers.Abstract;
                }
                else if (!isIndexer)
                {
                    allowedModifiers |= DeclarationModifiers.Static;
                }
            }

            if (containingType.IsStructType())
            {
                allowedModifiers |= DeclarationModifiers.ReadOnly;
            }

            allowedModifiers |= DeclarationModifiers.Extern;

            var mods = ModifierUtils.MakeAndCheckNontypeMemberModifiers(modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors);

            ModifierUtils.CheckFeatureAvailabilityForStaticAbstractMembersInInterfacesIfNeeded(mods, isExplicitInterfaceImplementation, location, diagnostics);

            containingType.CheckUnsafeModifier(mods, location, diagnostics);

            ModifierUtils.ReportDefaultInterfaceImplementationModifiers(accessorsHaveImplementation, mods,
                                                                        defaultInterfaceImplementationModifiers,
                                                                        location, diagnostics);

            // Let's overwrite modifiers for interface properties with what they are supposed to be.
            // Proper errors must have been reported by now.
            if (isInterface)
            {
                mods = ModifierUtils.AdjustModifiersForAnInterfaceMember(mods, accessorsHaveImplementation, isExplicitInterfaceImplementation);
            }

            if (isIndexer)
            {
                mods |= DeclarationModifiers.Indexer;
            }

            return mods;
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
            binder = binder.WithUnsafeRegionIfNecessary(modifiers);
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
            RefKind refKind;
            var typeSyntax = GetTypeSyntax(syntax).SkipRef(out refKind);
            var type = binder.BindType(typeSyntax, diagnostics);
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagnostics);

            if (GetExplicitInterfaceSpecifier() is null && !this.IsNoMoreVisibleThan(type, ref useSiteInfo))
            {
                // "Inconsistent accessibility: indexer return type '{1}' is less accessible than indexer '{0}'"
                // "Inconsistent accessibility: property type '{1}' is less accessible than property '{0}'"
                diagnostics.Add((this.IsIndexer ? ErrorCode.ERR_BadVisIndexerReturn : ErrorCode.ERR_BadVisPropertyType), Location, this, type.Type);
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
                diagnostics: diagnostics);

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
                else if (SetMethod is object && param.Name == ParameterSymbol.ValueParameterName)
                {
                    diagnostics.Add(ErrorCode.ERR_DuplicateGeneratedName, param.Locations.FirstOrDefault() ?? Location, param.Name);
                }
            }

            diagnostics.Add(Location, useSiteInfo);
        }

        protected override bool HasPointerTypeSyntactically
        {
            get
            {
                var typeSyntax = GetTypeSyntax(CSharpSyntaxNode).SkipRef(out _);
                return typeSyntax.Kind() switch { SyntaxKind.PointerType => true, SyntaxKind.FunctionPointerType => true, _ => false };
            }
        }

        private static BaseParameterListSyntax? GetParameterListSyntax(CSharpSyntaxNode syntax)
            => (syntax as IndexerDeclarationSyntax)?.ParameterList;
    }
}
