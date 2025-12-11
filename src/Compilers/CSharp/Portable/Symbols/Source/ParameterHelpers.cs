// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class ParameterHelpers
    {
        public static ImmutableArray<SourceParameterSymbol> MakeParameters(
            Binder withTypeParametersBinder,
            Symbol owner,
            BaseParameterListSyntax syntax,
            out SyntaxToken arglistToken,
            BindingDiagnosticBag diagnostics,
            bool allowRefOrOut,
            bool allowThis,
            bool addRefReadOnlyModifier)
        {
            return MakeParameters<ParameterSyntax, SourceParameterSymbol, Symbol>(
                withTypeParametersBinder,
                owner,
                syntax.Parameters,
                out arglistToken,
                diagnostics,
                allowRefOrOut,
                allowThis,
                addRefReadOnlyModifier,
                suppressUseSiteDiagnostics: false,
                lastIndex: syntax.Parameters.Count - 1,
                parameterCreationFunc: static (Binder context, Symbol owner, TypeWithAnnotations parameterType,
                                        ParameterSyntax syntax, RefKind refKind, int ordinal,
                                        SyntaxToken paramsKeyword, SyntaxToken thisKeyword, bool addRefReadOnlyModifier,
                                        ScopedKind scope,
                                        BindingDiagnosticBag declarationDiagnostics) =>
                {
                    return SourceParameterSymbol.Create(
                        context,
                        owner,
                        parameterType,
                        syntax,
                        refKind,
                        syntax.Identifier,
                        ordinal,
                        hasParamsModifier: paramsKeyword.Kind() != SyntaxKind.None,
                        isExtensionMethodThis: ordinal == 0 && thisKeyword.Kind() != SyntaxKind.None,
                        addRefReadOnlyModifier,
                        scope,
                        declarationDiagnostics);
                });
        }

#nullable enable
        public static SourceParameterSymbol? MakeExtensionReceiverParameter(
            Binder withTypeParametersBinder,
            Symbol owner,
            ParameterListSyntax syntax,
            BindingDiagnosticBag diagnostics)
        {
            var parameterCreationFunc = static (Binder context, Symbol owner, TypeWithAnnotations parameterType,
                                        ParameterSyntax syntax, RefKind refKind, int ordinal,
                                        SyntaxToken paramsKeyword, SyntaxToken thisKeyword, bool addRefReadOnlyModifier,
                                        ScopedKind scope,
                                        BindingDiagnosticBag declarationDiagnostics) =>
                {
                    Debug.Assert(ordinal == 0);
                    Debug.Assert(paramsKeyword.Kind() == SyntaxKind.None);

                    return SourceParameterSymbol.Create(
                        context,
                        owner,
                        parameterType,
                        syntax,
                        refKind,
                        syntax.Identifier,
                        ordinal,
                        hasParamsModifier: false,
                        isExtensionMethodThis: false,
                        addRefReadOnlyModifier,
                        scope,
                        declarationDiagnostics);
                };

            SyntaxToken arglistToken = default;
            int firstDefault = -1;

            return MakeParameter(
                withTypeParametersBinder,
                owner,
                syntax.Parameters[0],
                ref arglistToken,
                diagnostics,
                allowRefOrOut: true,
                allowThis: false,
                addRefReadOnlyModifier: false,
                suppressUseSiteDiagnostics: false,
                lastIndex: syntax.Parameters.Count - 1,
                parameterCreationFunc: parameterCreationFunc,
                parameterIndex: 0,
                firstDefault: ref firstDefault,
                ParameterContext.ExtensionReceiverParameter);
        }
#nullable disable

        public static ImmutableArray<FunctionPointerParameterSymbol> MakeFunctionPointerParameters(
            Binder binder,
            FunctionPointerMethodSymbol owner,
            SeparatedSyntaxList<FunctionPointerParameterSyntax> parametersList,
            BindingDiagnosticBag diagnostics,
            bool suppressUseSiteDiagnostics)
        {
            return MakeParameters<FunctionPointerParameterSyntax, FunctionPointerParameterSymbol, FunctionPointerMethodSymbol>(
                binder,
                owner,
                parametersList,
                out _,
                diagnostics,
                allowRefOrOut: true,
                allowThis: false,
                addRefReadOnlyModifier: true,
                suppressUseSiteDiagnostics,
                parametersList.Count - 2,
                parameterCreationFunc: static (Binder binder, FunctionPointerMethodSymbol owner, TypeWithAnnotations parameterType,
                                        FunctionPointerParameterSyntax syntax, RefKind refKind, int ordinal,
                                        SyntaxToken paramsKeyword, SyntaxToken thisKeyword, bool addRefReadOnlyModifier,
                                        ScopedKind scope,
                                        BindingDiagnosticBag diagnostics) =>
                {
                    // Non-function pointer locations have other locations to encode in/ref readonly/outness. For function pointers,
                    // these modreqs are the only locations where this can be encoded. If that changes, we should update this.
                    Debug.Assert(addRefReadOnlyModifier, "If addReadonlyRef isn't true, we must have found a different location to encode the readonlyness of a function pointer");
                    ImmutableArray<CustomModifier> customModifiers = refKind switch
                    {
                        RefKind.In => CreateInModifiers(binder, diagnostics, syntax),
                        RefKind.RefReadOnlyParameter => CreateRefReadonlyParameterModifiers(binder, diagnostics, syntax),
                        RefKind.Out => CreateOutModifiers(binder, diagnostics, syntax),
                        _ => ImmutableArray<CustomModifier>.Empty
                    };

                    if (parameterType.IsVoidType())
                    {
                        diagnostics.Add(ErrorCode.ERR_NoVoidParameter, syntax.Type.Location);
                    }

                    return new FunctionPointerParameterSymbol(
                        parameterType,
                        refKind,
                        ordinal,
                        owner,
                        customModifiers);
                },
                parsingFunctionPointer: true);
        }

#nullable enable
        private static ImmutableArray<TParameterSymbol> MakeParameters<TParameterSyntax, TParameterSymbol, TOwningSymbol>(
            Binder withTypeParametersBinder,
            TOwningSymbol owner,
            SeparatedSyntaxList<TParameterSyntax> parametersList,
            out SyntaxToken arglistToken,
            BindingDiagnosticBag diagnostics,
            bool allowRefOrOut,
            bool allowThis,
            bool addRefReadOnlyModifier,
            bool suppressUseSiteDiagnostics,
            int lastIndex,
            Func<Binder, TOwningSymbol, TypeWithAnnotations, TParameterSyntax, RefKind, int, SyntaxToken, SyntaxToken, bool, ScopedKind, BindingDiagnosticBag, TParameterSymbol> parameterCreationFunc,
            bool parsingFunctionPointer = false)
            where TParameterSyntax : BaseParameterSyntax
            where TParameterSymbol : ParameterSymbol
            where TOwningSymbol : Symbol
        {
            Debug.Assert(!parsingFunctionPointer || owner is FunctionPointerMethodSymbol);
            arglistToken = default(SyntaxToken);

            int parameterIndex = 0;
            int firstDefault = -1;

            var builder = ArrayBuilder<TParameterSymbol>.GetInstance();
            var parameterContext = parsingFunctionPointer ? ParameterContext.FunctionPointer : ParameterContext.Default;

            foreach (var parameterSyntax in parametersList)
            {
                if (parameterIndex > lastIndex) break;

                TParameterSymbol? parameter = MakeParameter(withTypeParametersBinder, owner, parameterSyntax, ref arglistToken, diagnostics,
                    allowRefOrOut, allowThis, addRefReadOnlyModifier, suppressUseSiteDiagnostics, lastIndex, parameterCreationFunc,
                    parameterIndex, ref firstDefault, parameterContext);

                if (parameter is null) continue;

                builder.Add(parameter);
                ++parameterIndex;
            }

            ImmutableArray<TParameterSymbol> parameters = builder.ToImmutableAndFree();

            if (!parsingFunctionPointer)
            {
                var methodOwner = owner as MethodSymbol;
                var typeParameters = (object?)methodOwner != null ?
                    methodOwner.TypeParameters :
                    [];

                ImmutableArray<ParameterSymbol> parametersForNameConflict = parameters.Cast<TParameterSymbol, ParameterSymbol>();

                if (owner.IsExtensionBlockMember())
                {
                    typeParameters = owner.ContainingType.TypeParameters.Concat(typeParameters);

                    if (owner.ContainingType.ExtensionParameter is { Name: not "" } receiver)
                    {
                        parametersForNameConflict = parametersForNameConflict.Insert(0, receiver);
                    }
                }

                Debug.Assert(methodOwner?.MethodKind != MethodKind.LambdaMethod);
                bool allowShadowingNames = withTypeParametersBinder.Compilation.IsFeatureEnabled(MessageID.IDS_FeatureNameShadowingInNestedFunctions) &&
                    methodOwner?.MethodKind == MethodKind.LocalFunction;

                withTypeParametersBinder.ValidateParameterNameConflicts(typeParameters, parametersForNameConflict, allowShadowingNames, diagnostics);
            }

            return parameters;
        }

        internal enum ParameterContext
        {
            Default,
            FunctionPointer,
            Lambda,
            AnonymousMethod,
            ExtensionReceiverParameter
        }

        private static TParameterSymbol? MakeParameter<TParameterSyntax, TParameterSymbol, TOwningSymbol>(
            Binder withTypeParametersBinder,
            TOwningSymbol owner,
            TParameterSyntax parameterSyntax,
            ref SyntaxToken arglistToken,
            BindingDiagnosticBag diagnostics,
            bool allowRefOrOut,
            bool allowThis,
            bool addRefReadOnlyModifier,
            bool suppressUseSiteDiagnostics,
            int lastIndex,
            Func<Binder, TOwningSymbol, TypeWithAnnotations, TParameterSyntax, RefKind, int, SyntaxToken, SyntaxToken, bool, ScopedKind, BindingDiagnosticBag, TParameterSymbol> parameterCreationFunc,
            int parameterIndex,
            ref int firstDefault,
            ParameterContext parameterContext)
            where TParameterSyntax : BaseParameterSyntax
            where TParameterSymbol : ParameterSymbol
            where TOwningSymbol : Symbol
        {
            Debug.Assert(parameterContext is ParameterContext.Default or ParameterContext.FunctionPointer or ParameterContext.ExtensionReceiverParameter);
            CheckParameterModifiers(parameterSyntax, diagnostics, parameterContext);

            bool inExtension = parameterContext is ParameterContext.ExtensionReceiverParameter;
            var refKind = GetModifiers(parameterSyntax.Modifiers, ignoreParams: inExtension, out SyntaxToken refnessKeyword, out SyntaxToken paramsKeyword, out SyntaxToken thisKeyword, out ScopedKind scope);

            if (thisKeyword.Kind() != SyntaxKind.None && !allowThis)
            {
                diagnostics.Add(ErrorCode.ERR_ThisInBadContext, thisKeyword.GetLocation());
            }

            if (parameterSyntax is ParameterSyntax concreteParam)
            {
                if (concreteParam.IsArgList)
                {
                    arglistToken = concreteParam.Identifier;
                    // The native compiler produces "Expected type" here, in the parser. Roslyn produces
                    // the somewhat more informative "arglist not valid" error.
                    if (paramsKeyword.Kind() != SyntaxKind.None
                        || refnessKeyword.Kind() != SyntaxKind.None
                        || thisKeyword.Kind() != SyntaxKind.None
                        || inExtension)
                    {
                        // CS1669: __arglist is not valid in this context
                        diagnostics.Add(ErrorCode.ERR_IllegalVarArgs, arglistToken.GetLocation());
                    }

                    if (parameterIndex != lastIndex && !inExtension)
                    {
                        // CS0257: An __arglist parameter must be the last parameter in a parameter list
                        diagnostics.Add(ErrorCode.ERR_VarargsLast, concreteParam.GetLocation());
                    }

                    return null;
                }

                if (concreteParam.Default != null && firstDefault == -1)
                {
                    firstDefault = parameterIndex;
                }
            }

            Debug.Assert(parameterSyntax.Type != null);
            var parameterType = withTypeParametersBinder.BindType(parameterSyntax.Type, diagnostics, suppressUseSiteDiagnostics: suppressUseSiteDiagnostics);

            if (!allowRefOrOut && (refKind == RefKind.Ref || refKind == RefKind.Out))
            {
                Debug.Assert(refnessKeyword.Kind() != SyntaxKind.None);

                // error CS0631: ref and out are not valid in this context
                diagnostics.Add(ErrorCode.ERR_IllegalRefParam, refnessKeyword.GetLocation());
            }

            TParameterSymbol parameter = parameterCreationFunc(withTypeParametersBinder, owner, parameterType, parameterSyntax, refKind, parameterIndex, paramsKeyword, thisKeyword, addRefReadOnlyModifier, scope, diagnostics);

            Debug.Assert(parameter is SourceComplexParameterSymbolBase || !parameter.IsParams); // Only SourceComplexParameterSymbolBase validates 'params' type.
            Debug.Assert(parameter is SourceComplexParameterSymbolBase || parameter is not SourceParameterSymbol s || s.DeclaredScope == ScopedKind.None); // Only SourceComplexParameterSymbolBase validates 'scope'.
            ReportParameterErrors(owner, parameterSyntax, parameter.Ordinal, lastParameterIndex: lastIndex, isParams: parameter.IsParams, typeWithAnnotations: parameter.TypeWithAnnotations,
                                  refKind: parameter.RefKind, containingSymbol: parameter.ContainingSymbol, thisKeyword: thisKeyword, paramsKeyword: paramsKeyword, firstDefault: firstDefault, diagnostics: diagnostics);
            return parameter;
        }

        internal static void EnsureRefKindAttributesExist(PEModuleBuilder moduleBuilder, ImmutableArray<ParameterSymbol> parameters)
        {
            EnsureRefKindAttributesExist(moduleBuilder.Compilation, parameters, diagnostics: null, modifyCompilation: false, moduleBuilder);
        }

        internal static void EnsureRefKindAttributesExist(CSharpCompilation? compilation, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag diagnostics, bool modifyCompilation)
        {
            // These parameters might not come from a compilation (example: lambdas evaluated in EE).
            // During rewriting, lowering will take care of flagging the appropriate PEModuleBuilder instead.
            if (compilation == null)
            {
                return;
            }

            EnsureRefKindAttributesExist(compilation, parameters, diagnostics, modifyCompilation, moduleBuilder: null);
        }

        private static void EnsureRefKindAttributesExist(CSharpCompilation compilation, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag? diagnostics, bool modifyCompilation, PEModuleBuilder? moduleBuilder)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.RefKind == RefKind.In)
                {
                    if (moduleBuilder is { })
                    {
                        moduleBuilder.EnsureIsReadOnlyAttributeExists();
                    }
                    else
                    {
                        compilation.EnsureIsReadOnlyAttributeExists(diagnostics, GetParameterLocation(parameter), modifyCompilation);
                    }
                }
                else if (parameter.RefKind == RefKind.RefReadOnlyParameter)
                {
                    if (moduleBuilder is { })
                    {
                        moduleBuilder.EnsureRequiresLocationAttributeExists();
                    }
                    else
                    {
                        compilation.EnsureRequiresLocationAttributeExists(diagnostics, GetParameterLocation(parameter), modifyCompilation);
                    }
                }
            }
        }

        internal static void EnsureParamCollectionAttributeExists(PEModuleBuilder moduleBuilder, ImmutableArray<ParameterSymbol> parameters)
        {
            if (parameters.LastOrDefault(static (p) => p.IsParamsCollection) is { } parameter)
            {
                moduleBuilder.EnsureParamCollectionAttributeExists(null, null);
            }
        }

        internal static void EnsureParamCollectionAttributeExists(CSharpCompilation compilation, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag diagnostics, bool modifyCompilation)
        {
            if (parameters.LastOrDefault(static (p) => p.IsParamsCollection) is { } parameter)
            {
                compilation.EnsureParamCollectionAttributeExists(diagnostics, GetParameterLocation(parameter), modifyCompilation);
            }
        }

        internal static void EnsureNativeIntegerAttributeExists(PEModuleBuilder moduleBuilder, ImmutableArray<ParameterSymbol> parameters)
        {
            Debug.Assert(moduleBuilder.Compilation.ShouldEmitNativeIntegerAttributes());
            EnsureNativeIntegerAttributeExists(moduleBuilder.Compilation, parameters, diagnostics: null, modifyCompilation: false, moduleBuilder);
        }

        internal static void EnsureNativeIntegerAttributeExists(CSharpCompilation? compilation, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag diagnostics, bool modifyCompilation)
        {
            // These parameters might not come from a compilation (example: lambdas evaluated in EE).
            // During rewriting, lowering will take care of flagging the appropriate PEModuleBuilder instead.
            if (compilation == null)
            {
                return;
            }

            if (!compilation.ShouldEmitNativeIntegerAttributes())
            {
                return;
            }

            EnsureNativeIntegerAttributeExists(compilation, parameters, diagnostics, modifyCompilation, moduleBuilder: null);
        }

        private static void EnsureNativeIntegerAttributeExists(CSharpCompilation compilation, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag? diagnostics, bool modifyCompilation, PEModuleBuilder? moduleBuilder)
        {
            Debug.Assert(compilation.ShouldEmitNativeIntegerAttributes());
            foreach (var parameter in parameters)
            {
                if (parameter.TypeWithAnnotations.ContainsNativeIntegerWrapperType())
                {
                    if (moduleBuilder is { })
                    {
                        moduleBuilder.EnsureNativeIntegerAttributeExists();
                    }
                    else
                    {
                        compilation.EnsureNativeIntegerAttributeExists(diagnostics, GetParameterLocation(parameter), modifyCompilation);
                    }
                }
            }
        }

        internal static bool RequiresScopedRefAttribute(ParameterSymbol parameter)
        {
            Debug.Assert(!parameter.IsThis);

            var scope = parameter.EffectiveScope;
            if (scope == ScopedKind.None)
            {
                return false;
            }
            if (IsRefScopedByDefault(parameter))
            {
                return scope == ScopedKind.ScopedValue;
            }
            return true;
        }

        internal static bool IsRefScopedByDefault(ParameterSymbol parameter)
        {
            return IsRefScopedByDefault(parameter.UseUpdatedEscapeRules, parameter.RefKind);
        }

        internal static bool IsRefScopedByDefault(bool useUpdatedEscapeRules, RefKind refKind)
        {
            return useUpdatedEscapeRules && refKind == RefKind.Out;
        }

        internal static void EnsureScopedRefAttributeExists(PEModuleBuilder moduleBuilder, ImmutableArray<ParameterSymbol> parameters)
        {
            EnsureScopedRefAttributeExists(moduleBuilder.Compilation, parameters, diagnostics: null, modifyCompilation: false, moduleBuilder);
        }

        internal static void EnsureScopedRefAttributeExists(CSharpCompilation? compilation, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag diagnostics, bool modifyCompilation)
        {
            // These parameters might not come from a compilation (example: lambdas evaluated in EE).
            // During rewriting, lowering will take care of flagging the appropriate PEModuleBuilder instead.
            if (compilation == null)
            {
                return;
            }

            EnsureScopedRefAttributeExists(compilation, parameters, diagnostics, modifyCompilation, moduleBuilder: null);
        }

        private static void EnsureScopedRefAttributeExists(CSharpCompilation compilation, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag? diagnostics, bool modifyCompilation, PEModuleBuilder? moduleBuilder)
        {
            foreach (var parameter in parameters)
            {
                if (RequiresScopedRefAttribute(parameter))
                {
                    if (moduleBuilder is { })
                    {
                        moduleBuilder.EnsureScopedRefAttributeExists();
                    }
                    else
                    {
                        compilation.EnsureScopedRefAttributeExists(diagnostics, GetParameterLocation(parameter), modifyCompilation);
                    }
                }
            }
        }

        internal static void EnsureNullableAttributeExists(PEModuleBuilder moduleBuilder, Symbol container, ImmutableArray<ParameterSymbol> parameters)
        {
            EnsureNullableAttributeExists(moduleBuilder.Compilation, container, parameters, diagnostics: null, modifyCompilation: false, moduleBuilder);
        }

        internal static void EnsureNullableAttributeExists(CSharpCompilation? compilation, Symbol container, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag? diagnostics, bool modifyCompilation)
        {
            // These parameters might not come from a compilation (example: lambdas evaluated in EE).
            // During rewriting, lowering will take care of flagging the appropriate PEModuleBuilder instead.
            if (compilation == null)
            {
                return;
            }

            EnsureNullableAttributeExists(compilation, container, parameters, diagnostics, modifyCompilation, moduleBuilder: null);
        }

        private static void EnsureNullableAttributeExists(CSharpCompilation compilation, Symbol container, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag? diagnostics, bool modifyCompilation, PEModuleBuilder? moduleBuilder)
        {
            if (parameters.Length > 0 && compilation.ShouldEmitNullableAttributes(container))
            {
                foreach (var parameter in parameters)
                {
                    if (parameter.TypeWithAnnotations.NeedsNullableAttribute())
                    {
                        if (moduleBuilder is { })
                        {
                            moduleBuilder.EnsureNullableAttributeExists();
                        }
                        else
                        {
                            compilation.EnsureNullableAttributeExists(diagnostics, GetParameterLocation(parameter), modifyCompilation);
                        }
                    }
                }
            }
        }

        internal static void CheckUnderspecifiedGenericExtension(Symbol extensionMember, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(extensionMember.IsExtensionBlockMember());

            NamedTypeSymbol extension = extensionMember.ContainingType;
            if (extension.ExtensionParameter is not { } extensionParameter || extension.ContainingType?.Arity != 0)
            {
                // error cases, already reported elsewhere
                return;
            }

            if (extension.Arity == 0)
            {
                return;
            }

            var usedTypeParameters = PooledHashSet<TypeParameterSymbol>.GetInstance();
            reportUnusedExtensionTypeParameters(extensionMember, parameters, diagnostics, extension, extensionParameter, usedTypeParameters);

            usedTypeParameters.Free();
            return;

            static void reportUnusedExtensionTypeParameters(Symbol extensionMember, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag diagnostics, NamedTypeSymbol extension, ParameterSymbol extensionParameter, PooledHashSet<TypeParameterSymbol> usedTypeParameters)
            {
                int extensionArity = extension.Arity;
                int extensionMemberArity = extensionMember.GetMemberArity();
                Debug.Assert(extensionMemberArity == 0); // we currently only apply the check for members which may not be generic

                extensionParameter.Type.VisitType(collectTypeParameters, arg: usedTypeParameters);

                if (usedTypeParameters.Count == extensionArity && extensionMemberArity == 0)
                {
                    return;
                }

                foreach (var parameter in parameters)
                {
                    parameter.Type.VisitType(collectTypeParameters, arg: usedTypeParameters);

                    if (usedTypeParameters.Count == extensionArity && extensionMemberArity == 0)
                    {
                        return;
                    }
                }

                foreach (var typeParameter in extension.TypeParameters)
                {
                    if (!usedTypeParameters.Contains(typeParameter))
                    {
                        diagnostics.Add(ErrorCode.ERR_UnderspecifiedExtension, extensionMember.GetFirstLocation(), typeParameter);
                    }
                }
            }

            static bool collectTypeParameters(TypeSymbol type, PooledHashSet<TypeParameterSymbol> typeParameters, bool ignored)
            {
                if (type is TypeParameterSymbol typeParameter)
                {
                    typeParameters.Add(typeParameter);
                }

                return false;
            }
        }

        internal static Location GetParameterLocation(ParameterSymbol parameter) => parameter.GetNonNullSyntaxNode().Location;

        internal static void CheckParameterModifiers(
            BaseParameterSyntax parameter,
            BindingDiagnosticBag diagnostics,
            ParameterContext parameterContext)
        {
            var seenThis = false;
            var seenRef = false;
            var seenOut = false;
            var seenParams = false;
            var seenIn = false;
            bool seenScoped = false;
            bool seenReadonly = false;

            SyntaxToken? previousModifier = null;
            foreach (var modifier in parameter.Modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.ThisKeyword:
                        Binder.CheckFeatureAvailability(modifier, MessageID.IDS_FeatureExtensionMethod, diagnostics);

                        if (seenRef || seenIn)
                        {
                            Binder.CheckFeatureAvailability(modifier, MessageID.IDS_FeatureRefExtensionMethods, diagnostics);
                        }

                        // `this` on extension parameters was already reported elsewhere
                        if (parameterContext is ParameterContext.Lambda or ParameterContext.AnonymousMethod)
                        {
                            diagnostics.Add(ErrorCode.ERR_ThisInBadContext, modifier.GetLocation());
                        }
                        else if (seenThis)
                        {
                            addERR_DupParamMod(diagnostics, modifier);
                        }
                        else if (seenOut)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.OutKeyword);
                        }
                        else if (seenParams)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParamModThis, modifier.GetLocation());
                        }
                        else
                        {
                            seenThis = true;
                        }
                        break;

                    case SyntaxKind.RefKeyword:
                        if (seenThis)
                        {
                            Binder.CheckFeatureAvailability(modifier, MessageID.IDS_FeatureRefExtensionMethods, diagnostics);
                        }

                        if (seenRef)
                        {
                            addERR_DupParamMod(diagnostics, modifier);
                        }
                        else if (seenParams)
                        {
                            addERR_ParamsCantBeWithModifier(diagnostics, modifier, SyntaxKind.RefKeyword);
                        }
                        else if (seenOut)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.OutKeyword);
                        }
                        else if (seenIn)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.InKeyword);
                        }
                        else
                        {
                            seenRef = true;
                        }
                        break;

                    case SyntaxKind.OutKeyword:
                        if (seenOut)
                        {
                            addERR_DupParamMod(diagnostics, modifier);
                        }
                        else if (seenThis)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.ThisKeyword);
                        }
                        else if (parameterContext is ParameterContext.ExtensionReceiverParameter)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.ExtensionKeyword);
                        }
                        else if (seenParams)
                        {
                            addERR_ParamsCantBeWithModifier(diagnostics, modifier, SyntaxKind.OutKeyword);
                        }
                        else if (seenRef)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.RefKeyword);
                        }
                        else if (seenIn)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.InKeyword);
                        }
                        else
                        {
                            seenOut = true;
                        }
                        break;

                    case SyntaxKind.ParamsKeyword when parameterContext is not ParameterContext.FunctionPointer:
                        if (parameterContext is ParameterContext.AnonymousMethod or ParameterContext.ExtensionReceiverParameter)
                        {
                            diagnostics.Add(ErrorCode.ERR_IllegalParams, modifier.GetLocation());
                        }
                        else if (seenParams)
                        {
                            addERR_DupParamMod(diagnostics, modifier);
                        }
                        else if (seenThis)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParamModThis, modifier.GetLocation());
                        }
                        else if (seenRef)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.RefKeyword);
                        }
                        else if (seenIn)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.InKeyword);
                        }
                        else if (seenOut)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.OutKeyword);
                        }
                        else
                        {
                            seenParams = true;
                        }

                        if (parameterContext is ParameterContext.Lambda)
                        {
                            MessageID.IDS_FeatureLambdaParamsArray.CheckFeatureAvailability(diagnostics, modifier);

                            if (parameter is ParameterSyntax { Type: null, Identifier.Text: var parameterIdentifier })
                            {
                                diagnostics.Add(ErrorCode.ERR_ImplicitlyTypedParamsParameter, modifier, parameterIdentifier);
                            }
                        }
                        break;

                    case SyntaxKind.InKeyword:
                        Binder.CheckFeatureAvailability(modifier, MessageID.IDS_FeatureReadOnlyReferences, diagnostics);

                        if (seenThis)
                        {
                            Binder.CheckFeatureAvailability(modifier, MessageID.IDS_FeatureRefExtensionMethods, diagnostics);
                        }

                        if (seenIn)
                        {
                            addERR_DupParamMod(diagnostics, modifier);
                        }
                        else if (seenOut)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.OutKeyword);
                        }
                        else if (seenRef)
                        {
                            addERR_BadParameterModifiers(diagnostics, modifier, SyntaxKind.RefKeyword);
                        }
                        else if (seenParams)
                        {
                            addERR_ParamsCantBeWithModifier(diagnostics, modifier, SyntaxKind.InKeyword);
                        }
                        else
                        {
                            seenIn = true;
                        }
                        break;

                    case SyntaxKind.ScopedKeyword when parameterContext is not ParameterContext.FunctionPointer:
                        ModifierUtils.CheckScopedModifierAvailability(parameter, modifier, diagnostics);
                        Debug.Assert(!seenIn);
                        Debug.Assert(!seenOut);
                        Debug.Assert(!seenRef);
                        Debug.Assert(!seenScoped);

                        seenScoped = true;
                        break;

                    case SyntaxKind.ReadOnlyKeyword:
                        if (seenReadonly)
                        {
                            addERR_DupParamMod(diagnostics, modifier);
                        }
                        else if (previousModifier?.Kind() != SyntaxKind.RefKeyword)
                        {
                            diagnostics.Add(ErrorCode.ERR_RefReadOnlyWrongOrdering, modifier);
                        }
                        else if (seenRef)
                        {
                            Binder.CheckFeatureAvailability(modifier, MessageID.IDS_FeatureRefReadonlyParameters, diagnostics);
                            seenReadonly = true;
                        }
                        break;

                    case SyntaxKind.ParamsKeyword when parameterContext is ParameterContext.FunctionPointer:
                    case SyntaxKind.ScopedKeyword when parameterContext is ParameterContext.FunctionPointer:
                        diagnostics.Add(ErrorCode.ERR_BadFuncPointerParamModifier, modifier.GetLocation(), SyntaxFacts.GetText(modifier.Kind()));
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(modifier.Kind());
                }

                previousModifier = modifier;
            }

            static void addERR_DupParamMod(BindingDiagnosticBag diagnostics, SyntaxToken modifier)
            {
                diagnostics.Add(ErrorCode.ERR_DupParamMod, modifier.GetLocation(), SyntaxFacts.GetText(modifier.Kind()));
            }

            static void addERR_BadParameterModifiers(BindingDiagnosticBag diagnostics, SyntaxToken modifier, SyntaxKind otherModifierKind)
            {
                diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(modifier.Kind()), SyntaxFacts.GetText(otherModifierKind));
            }

            static void addERR_ParamsCantBeWithModifier(BindingDiagnosticBag diagnostics, SyntaxToken modifier, SyntaxKind otherModifierKind)
            {
                diagnostics.Add(ErrorCode.ERR_ParamsCantBeWithModifier, modifier.GetLocation(), SyntaxFacts.GetText(otherModifierKind));
            }
        }

        /// <param name="typeWithAnnotations">Will be <see langword="default"/> if this is called for a lambda parameter
        /// with an explicit type provided.</param>
        public static void ReportParameterErrors(
            Symbol? owner,
            BaseParameterSyntax syntax,
            int ordinal,
            int lastParameterIndex,
            bool isParams,
            TypeWithAnnotations typeWithAnnotations,
            RefKind refKind,
            Symbol? containingSymbol,
            SyntaxToken thisKeyword,
            SyntaxToken paramsKeyword,
            int firstDefault,
            BindingDiagnosticBag diagnostics)
        {
            int parameterIndex = ordinal;
            bool isDefault = syntax is ParameterSyntax { Default: { } };

            if (thisKeyword.Kind() == SyntaxKind.ThisKeyword && parameterIndex != 0 && owner?.IsExtensionBlockMember() != true)
            {
                // Report CS1100 on "this". Note that is a change from Dev10
                // which reports the error on the type following "this".

                // error CS1100: Method '{0}' has a parameter modifier 'this' which is not on the first parameter
                diagnostics.Add(ErrorCode.ERR_BadThisParam, thisKeyword.GetLocation(), owner?.Name ?? "");
            }
            else if (isParams && owner is { } && owner.IsOperator())
            {
                // error CS1670: params is not valid in this context
                diagnostics.Add(ErrorCode.ERR_IllegalParams, paramsKeyword.GetLocation());
            }
            else if (!typeWithAnnotations.IsDefault && typeWithAnnotations.IsStatic)
            {
                bool inExtension = owner is SynthesizedExtensionMarker;
                bool isNamed = syntax is ParameterSyntax parameterSyntax && parameterSyntax.Identifier.Kind() != SyntaxKind.None;

                Debug.Assert(containingSymbol is null
                    || (containingSymbol is FunctionPointerMethodSymbol or { ContainingType: not null })
                    || inExtension);

                if (!inExtension || isNamed)
                {
                    // error CS0721: '{0}': static types cannot be used as parameters
                    diagnostics.Add(
                        ErrorFacts.GetStaticClassParameterCode(containingSymbol?.ContainingType?.IsInterfaceType() ?? false),
                        syntax.Type?.Location ?? syntax.GetLocation(),
                        typeWithAnnotations.Type);
                }
            }
            else if (firstDefault != -1 && parameterIndex > firstDefault && !isDefault && !isParams)
            {
                // error CS1737: Optional parameters must appear after all required parameters
                Location loc = ((ParameterSyntax)syntax).Identifier.GetNextToken(includeZeroWidth: true).GetLocation(); //could be missing
                diagnostics.Add(ErrorCode.ERR_DefaultValueBeforeRequiredValue, loc);
            }
            else if (refKind != RefKind.None &&
                !typeWithAnnotations.IsDefault &&
                typeWithAnnotations.IsRestrictedType(ignoreSpanLikeTypes: true))
            {
                // CS1601: Cannot make reference to variable of type 'System.TypedReference'
                diagnostics.Add(ErrorCode.ERR_MethodArgCantBeRefAny, syntax.Location, typeWithAnnotations.Type);
            }

            if (isParams && ordinal != lastParameterIndex)
            {
                // error CS0231: A params parameter must be the last parameter in a parameter list
                diagnostics.Add(ErrorCode.ERR_ParamsLast, syntax.GetLocation());
            }
        }

#nullable disable

        internal static bool ReportDefaultParameterErrors(
            Binder binder,
            Symbol owner,
            ParameterSyntax parameterSyntax,
            SourceParameterSymbol parameter,
            BoundExpression defaultExpression,
            BoundExpression convertedExpression,
            BindingDiagnosticBag diagnostics)
        {
            bool hasErrors = false;

            // SPEC VIOLATION: The spec says that the conversion from the initializer to the 
            // parameter type is required to be either an identity or a nullable conversion, but
            // that is not right:
            //
            // void M(short myShort = 10) {}
            // * not an identity or nullable conversion but should be legal
            //
            // void M(object obj = (dynamic)null) {}
            // * an identity conversion, but should be illegal
            //
            // void M(MyStruct? myStruct = default(MyStruct)) {}
            // * a nullable conversion, but must be illegal because we cannot generate metadata for it
            // 
            // Even if the expression is thoroughly illegal, we still want to bind it and 
            // stick it in the parameter because we want to be able to analyze it for
            // IntelliSense purposes.

            bool inExtension = parameter.ContainingSymbol is SynthesizedExtensionMarker;
            if (inExtension)
            {
                diagnostics.Add(ErrorCode.ERR_ExtensionParameterDisallowsDefaultValue, parameterSyntax.GetLocation());
                return true;
            }

            TypeSymbol parameterType = parameter.Type;
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagnostics);
            Conversion conversion = binder.Conversions.ClassifyImplicitConversionFromExpression(defaultExpression, parameterType, ref useSiteInfo);
            diagnostics.Add(defaultExpression.Syntax, useSiteInfo);

            var refKind = GetModifiers(parameterSyntax.Modifiers, ignoreParams: inExtension, out SyntaxToken refnessKeyword, out SyntaxToken paramsKeyword, out SyntaxToken thisKeyword, out _);

            // CONSIDER: We are inconsistent here regarding where the error is reported; is it
            // CONSIDER: reported on the parameter name, or on the value of the initializer?
            // CONSIDER: Consider making this consistent.

            if (refKind == RefKind.Ref || refKind == RefKind.Out)
            {
                // error CS1741: A ref or out parameter cannot have a default value
                diagnostics.Add(ErrorCode.ERR_RefOutDefaultValue, refnessKeyword.GetLocation());
                hasErrors = true;
            }
            else if (paramsKeyword.Kind() == SyntaxKind.ParamsKeyword)
            {
                // error CS1751: Cannot specify a default value for a parameter collection
                diagnostics.Add(ErrorCode.ERR_DefaultValueForParamsParameter, paramsKeyword.GetLocation());
                hasErrors = true;
            }
            else if (thisKeyword.Kind() == SyntaxKind.ThisKeyword)
            {
                // Only need to report CS1743 for the first parameter. The caller will
                // have reported CS1100 if 'this' appeared on another parameter.
                if (parameter.Ordinal == 0 && !parameter.ContainingSymbol.IsExtensionBlockMember())
                {
                    // error CS1743: Cannot specify a default value for the 'this' parameter
                    diagnostics.Add(ErrorCode.ERR_DefaultValueForExtensionParameter, thisKeyword.GetLocation());
                    hasErrors = true;
                }
            }
            else if (!defaultExpression.HasAnyErrors &&
                !IsValidDefaultValue(defaultExpression.IsImplicitObjectCreation() ?
                    convertedExpression : defaultExpression))
            {
                // error CS1736: Default parameter value for '{0}' must be a compile-time constant
                diagnostics.Add(ErrorCode.ERR_DefaultValueMustBeConstant, parameterSyntax.Default.Value.Location, parameterSyntax.Identifier.ValueText);
                hasErrors = true;
            }
            else if (!conversion.Exists ||
                conversion.IsUserDefined ||
                conversion.IsUnion || // PROTOTYPE: Add coverage
                conversion.IsIdentity && parameterType.SpecialType == SpecialType.System_Object && defaultExpression.Type.IsDynamic())
            {
                // If we had no implicit conversion, or a user-defined conversion, report an error.
                //
                // Even though "object x = (dynamic)null" is a legal identity conversion, we do not allow it. 
                // CONSIDER: We could. Doesn't hurt anything.

                // error CS1750: A value of type '{0}' cannot be used as a default parameter because there are no standard conversions to type '{1}'
                diagnostics.Add(ErrorCode.ERR_NoConversionForDefaultParam, parameterSyntax.Identifier.GetLocation(),
                    defaultExpression.Display, parameterType);

                hasErrors = true;
            }
            else if (conversion.IsReference &&
                (object)defaultExpression.Type != null &&
                defaultExpression.Type.SpecialType == SpecialType.System_String ||
                conversion.IsBoxing)
            {
                // We don't allow object x = "hello", object x = 123, dynamic x = "hello", IEnumerable<char> x = "hello", etc.
                // error CS1763: '{0}' is of type '{1}'. A default parameter value of a reference type other than string can only be initialized with null
                diagnostics.Add(ErrorCode.ERR_NotNullRefDefaultParameter, parameterSyntax.Identifier.GetLocation(),
                    parameterSyntax.Identifier.ValueText, parameterType);

                hasErrors = true;
            }
            else if (((conversion.IsNullable && !defaultExpression.Type.IsNullableType()) ||
                      (conversion.IsObjectCreation && convertedExpression.Type.IsNullableType())) &&
                !(parameterType.GetNullableUnderlyingType().IsEnumType() || parameterType.GetNullableUnderlyingType().IsIntrinsicType()))
            {
                // We can do:
                // M(int? x = default(int)) 
                // M(int? x = default(int?)) 
                // M(MyEnum? e = default(enum))
                // M(MyEnum? e = default(enum?))
                // M(MyStruct? s = default(MyStruct?))
                //
                // but we cannot do:
                //
                // M(MyStruct? s = default(MyStruct))

                // error CS1770: 
                // A value of type '{0}' cannot be used as default parameter for nullable parameter '{1}' because '{0}' is not a simple type
                diagnostics.Add(ErrorCode.ERR_NoConversionForNubDefaultParam, parameterSyntax.Identifier.GetLocation(),
                    (defaultExpression.IsImplicitObjectCreation() ? convertedExpression.Type.StrippedType() : defaultExpression.Type), parameterSyntax.Identifier.ValueText);

                hasErrors = true;
            }

            ConstantValueUtils.CheckLangVersionForConstantValue(convertedExpression, diagnostics);

            // Certain contexts allow default parameter values syntactically but they are ignored during
            // semantic analysis. They are:

            // 1. Explicitly implemented interface methods; since the method will always be called
            //    via the interface, the defaults declared on the implementation will not 
            //    be seen at the call site.
            //
            // UNDONE: 2. The "actual" side of a partial method; the default values are taken from the
            // UNDONE:    "declaring" side of the method.
            //
            // UNDONE: 3. An indexer with only one formal parameter; it is illegal to omit every argument
            // UNDONE:    to an indexer.
            //
            // 4. A user-defined operator; it is syntactically impossible to omit the argument.

            if (owner.IsExplicitInterfaceImplementation() ||
                owner.IsPartialImplementation() ||
                owner.IsOperator())
            {
                // CS1066: The default value specified for parameter '{0}' will have no effect because it applies to a 
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_DefaultValueForUnconsumedLocation,
                    parameterSyntax.Identifier.GetLocation(),
                    parameterSyntax.Identifier.ValueText);
            }

            if (refKind == RefKind.RefReadOnlyParameter)
            {
                // A default value is specified for 'ref readonly' parameter '{0}', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
                diagnostics.Add(ErrorCode.WRN_RefReadonlyParameterDefaultValue, parameterSyntax.Default.Value, parameterSyntax.Identifier.ValueText);
            }

            return hasErrors;
        }

        private static bool IsValidDefaultValue(BoundExpression expression)
        {
            // SPEC VIOLATION: 
            // By the spec an optional parameter initializer is required to be either:
            // * a constant,
            // * new S() where S is a value type
            // * default(S) where S is a value type.
            // 
            // The native compiler considers default(T) to be a valid
            // initializer regardless of whether T is a value type
            // reference type, type parameter type, and so on.
            // We should consider simply allowing this in the spec.
            //
            // Also when valuetype S has a parameterless constructor, 
            // new S() is clearly not a constant expression and should produce an error
            if (expression.ConstantValueOpt != null)
            {
                return true;
            }

            switch (expression.Kind)
            {
                case BoundKind.DefaultLiteral:
                case BoundKind.DefaultExpression:
                    return true;
                case BoundKind.ObjectCreationExpression:
                    return IsValidDefaultValue((BoundObjectCreationExpression)expression);
                case BoundKind.Conversion:
                    var conversion = (BoundConversion)expression;
                    return conversion is { Conversion.IsObjectCreation: true, Operand: BoundObjectCreationExpression { WasTargetTyped: true } operand } &&
                           IsValidDefaultValue(operand);
                default:
                    return false;
            }
        }

        private static bool IsValidDefaultValue(BoundObjectCreationExpression expression)
        {
            return expression.Constructor.IsDefaultValueTypeConstructor() && expression.InitializerExpressionOpt == null;
        }

        internal static MethodSymbol FindContainingGenericMethod(Symbol symbol)
        {
            for (Symbol current = symbol; (object)current != null; current = current.ContainingSymbol)
            {
                if (current.Kind == SymbolKind.Method)
                {
                    MethodSymbol method = (MethodSymbol)current;
                    if (method.MethodKind != MethodKind.AnonymousFunction)
                    {
                        return method.IsGenericMethod ? method : null;
                    }
                }
            }
            return null;
        }

        internal static RefKind GetModifiers(SyntaxTokenList modifiers, bool ignoreParams, out SyntaxToken refnessKeyword, out SyntaxToken paramsKeyword, out SyntaxToken thisKeyword, out ScopedKind scope)
        {
            var refKind = RefKind.None;
            bool isScoped = false;

            refnessKeyword = default(SyntaxToken);
            paramsKeyword = default(SyntaxToken);
            thisKeyword = default(SyntaxToken);

            foreach (var modifier in modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.OutKeyword:
                        if (refKind == RefKind.None)
                        {
                            refnessKeyword = modifier;
                            refKind = RefKind.Out;
                        }
                        break;
                    case SyntaxKind.RefKeyword:
                        if (refKind == RefKind.None)
                        {
                            refnessKeyword = modifier;
                            refKind = RefKind.Ref;
                        }
                        break;
                    case SyntaxKind.InKeyword:
                        if (refKind == RefKind.None)
                        {
                            refnessKeyword = modifier;
                            refKind = RefKind.In;
                        }
                        break;
                    case SyntaxKind.ParamsKeyword:
                        if (!ignoreParams)
                        {
                            paramsKeyword = modifier;
                        }
                        break;
                    case SyntaxKind.ThisKeyword:
                        thisKeyword = modifier;
                        break;
                    case SyntaxKind.ScopedKeyword:
                        Debug.Assert(refKind == RefKind.None);
                        isScoped = true;
                        break;
                    case SyntaxKind.ReadOnlyKeyword:
                        if (refKind == RefKind.Ref && refnessKeyword.GetNextToken() == modifier)
                        {
                            refKind = RefKind.RefReadOnlyParameter;
                        }
                        break;
                }
            }

            if (isScoped)
            {
                scope = (refKind == RefKind.None) ? ScopedKind.ScopedValue : ScopedKind.ScopedRef;
            }
            else
            {
                scope = ScopedKind.None;
            }

            return refKind;
        }

        internal static ImmutableArray<CustomModifier> ConditionallyCreateInModifiers(RefKind refKind, bool addRefReadOnlyModifier, Binder binder, BindingDiagnosticBag diagnostics, SyntaxNode syntax)
        {
            if (addRefReadOnlyModifier && refKind is RefKind.In or RefKind.RefReadOnlyParameter)
            {
                return CreateInModifiers(binder, diagnostics, syntax);
            }
            else
            {
                return ImmutableArray<CustomModifier>.Empty;
            }
        }

        internal static ImmutableArray<CustomModifier> CreateInModifiers(Binder binder, BindingDiagnosticBag diagnostics, SyntaxNode syntax)
        {
            return CreateModifiers(WellKnownType.System_Runtime_InteropServices_InAttribute, binder, diagnostics, syntax);
        }

        // only for function pointer parameters
        private static ImmutableArray<CustomModifier> CreateRefReadonlyParameterModifiers(Binder binder, BindingDiagnosticBag diagnostics, SyntaxNode syntax)
        {
            var requiresLocationType = binder.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_RequiresLocationAttribute, diagnostics, syntax);
            return ImmutableArray.Create(CSharpCustomModifier.CreateOptional(requiresLocationType));
        }

        internal static ImmutableArray<CustomModifier> CreateOutModifiers(Binder binder, BindingDiagnosticBag diagnostics, SyntaxNode syntax)
        {
            return CreateModifiers(WellKnownType.System_Runtime_InteropServices_OutAttribute, binder, diagnostics, syntax);
        }

        private static ImmutableArray<CustomModifier> CreateModifiers(WellKnownType modifier, Binder binder, BindingDiagnosticBag diagnostics, SyntaxNode syntax)
        {
            var modifierType = binder.GetWellKnownType(modifier, diagnostics, syntax);
            return ImmutableArray.Create(CSharpCustomModifier.CreateRequired(modifierType));
        }
    }
}
