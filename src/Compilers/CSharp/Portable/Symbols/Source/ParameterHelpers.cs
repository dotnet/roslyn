﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class ParameterHelpers
    {
        public static ImmutableArray<ParameterSymbol> MakeParameters(
            Binder binder,
            Symbol owner,
            BaseParameterListSyntax syntax,
            out SyntaxToken arglistToken,
            DiagnosticBag diagnostics,
            bool allowRefOrOut,
            bool allowThis,
            bool addRefReadOnlyModifier)
        {
            return MakeParameters<ParameterSyntax, ParameterSymbol, Symbol>(
                binder,
                owner,
                syntax.Parameters,
                out arglistToken,
                diagnostics,
                allowRefOrOut,
                allowThis,
                addRefReadOnlyModifier,
                suppressUseSiteDiagnostics: false,
                lastIndex: syntax.Parameters.Count - 1,
                parameterCreationFunc: (Binder context, Symbol owner, TypeWithAnnotations parameterType,
                                        ParameterSyntax syntax, RefKind refKind, int ordinal,
                                        SyntaxToken paramsKeyword, SyntaxToken thisKeyword, bool addRefReadOnlyModifier,
                                        DiagnosticBag declarationDiagnostics) =>
                {
                    return SourceParameterSymbol.Create(
                        context,
                        owner,
                        parameterType,
                        syntax,
                        refKind,
                        syntax.Identifier,
                        ordinal,
                        isParams: paramsKeyword.Kind() != SyntaxKind.None,
                        isExtensionMethodThis: ordinal == 0 && thisKeyword.Kind() != SyntaxKind.None,
                        addRefReadOnlyModifier,
                        declarationDiagnostics);
                }
);
        }

        public static ImmutableArray<FunctionPointerParameterSymbol> MakeFunctionPointerParameters(
            Binder binder,
            FunctionPointerMethodSymbol owner,
            SeparatedSyntaxList<FunctionPointerParameterSyntax> parametersList,
            DiagnosticBag diagnostics,
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
                parameterCreationFunc: (Binder binder, FunctionPointerMethodSymbol owner, TypeWithAnnotations parameterType,
                                        FunctionPointerParameterSyntax syntax, RefKind refKind, int ordinal,
                                        SyntaxToken paramsKeyword, SyntaxToken thisKeyword, bool addRefReadOnlyModifier,
                                        DiagnosticBag diagnostics) =>
                {
                    // Non-function pointer locations have other locations to encode in/ref readonly/outness. For function pointers,
                    // these modreqs are the only locations where this can be encoded. If that changes, we should update this.
                    Debug.Assert(addRefReadOnlyModifier, "If addReadonlyRef isn't true, we must have found a different location to encode the readonlyness of a function pointer");
                    ImmutableArray<CustomModifier> customModifiers = refKind switch
                    {
                        RefKind.In => CreateInModifiers(binder, diagnostics, syntax),
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

        private static ImmutableArray<TParameterSymbol> MakeParameters<TParameterSyntax, TParameterSymbol, TOwningSymbol>(
            Binder binder,
            TOwningSymbol owner,
            SeparatedSyntaxList<TParameterSyntax> parametersList,
            out SyntaxToken arglistToken,
            DiagnosticBag diagnostics,
            bool allowRefOrOut,
            bool allowThis,
            bool addRefReadOnlyModifier,
            bool suppressUseSiteDiagnostics,
            int lastIndex,
            Func<Binder, TOwningSymbol, TypeWithAnnotations, TParameterSyntax, RefKind, int, SyntaxToken, SyntaxToken, bool, DiagnosticBag, TParameterSymbol> parameterCreationFunc,
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
            var mustBeLastParameter = (ParameterSyntax)null;

            foreach (var parameterSyntax in parametersList)
            {
                if (parameterIndex > lastIndex) break;

                CheckParameterModifiers(parameterSyntax, diagnostics, parsingFunctionPointer);

                var refKind = GetModifiers(parameterSyntax.Modifiers, out SyntaxToken refnessKeyword, out SyntaxToken paramsKeyword, out SyntaxToken thisKeyword);
                if (thisKeyword.Kind() != SyntaxKind.None && !allowThis)
                {
                    diagnostics.Add(ErrorCode.ERR_ThisInBadContext, thisKeyword.GetLocation());
                }

                if (parameterSyntax is ParameterSyntax concreteParam)
                {
                    if (mustBeLastParameter == null &&
                        (concreteParam.Modifiers.Any(SyntaxKind.ParamsKeyword) ||
                         concreteParam.Identifier.Kind() == SyntaxKind.ArgListKeyword))
                    {
                        mustBeLastParameter = concreteParam;
                    }

                    if (concreteParam.IsArgList)
                    {
                        arglistToken = concreteParam.Identifier;
                        // The native compiler produces "Expected type" here, in the parser. Roslyn produces
                        // the somewhat more informative "arglist not valid" error.
                        if (paramsKeyword.Kind() != SyntaxKind.None
                            || refnessKeyword.Kind() != SyntaxKind.None
                            || thisKeyword.Kind() != SyntaxKind.None)
                        {
                            // CS1669: __arglist is not valid in this context
                            diagnostics.Add(ErrorCode.ERR_IllegalVarArgs, arglistToken.GetLocation());
                        }

                        continue;
                    }

                    if (concreteParam.Default != null && firstDefault == -1)
                    {
                        firstDefault = parameterIndex;
                    }
                }

                Debug.Assert(parameterSyntax.Type != null);
                var parameterType = binder.BindType(parameterSyntax.Type, diagnostics, suppressUseSiteDiagnostics: suppressUseSiteDiagnostics);

                if (!allowRefOrOut && (refKind == RefKind.Ref || refKind == RefKind.Out))
                {
                    Debug.Assert(refnessKeyword.Kind() != SyntaxKind.None);

                    // error CS0631: ref and out are not valid in this context
                    diagnostics.Add(ErrorCode.ERR_IllegalRefParam, refnessKeyword.GetLocation());
                }

                TParameterSymbol parameter = parameterCreationFunc(binder, owner, parameterType, parameterSyntax, refKind, parameterIndex, paramsKeyword, thisKeyword, addRefReadOnlyModifier, diagnostics);

                ReportParameterErrors(owner, parameterSyntax, parameter, thisKeyword, paramsKeyword, firstDefault, diagnostics);

                builder.Add(parameter);
                ++parameterIndex;
            }

            if (mustBeLastParameter != null && mustBeLastParameter != parametersList[lastIndex])
            {
                diagnostics.Add(
                    mustBeLastParameter.Identifier.Kind() == SyntaxKind.ArgListKeyword
                        ? ErrorCode.ERR_VarargsLast
                        : ErrorCode.ERR_ParamsLast,
                    mustBeLastParameter.GetLocation());
            }

            ImmutableArray<TParameterSymbol> parameters = builder.ToImmutableAndFree();

            if (!parsingFunctionPointer)
            {
                var methodOwner = owner as MethodSymbol;
                var typeParameters = (object)methodOwner != null ?
                    methodOwner.TypeParameters :
                    default(ImmutableArray<TypeParameterSymbol>);

                Debug.Assert(methodOwner?.MethodKind != MethodKind.LambdaMethod);
                bool allowShadowingNames = binder.Compilation.IsFeatureEnabled(MessageID.IDS_FeatureNameShadowingInNestedFunctions) &&
                    methodOwner?.MethodKind == MethodKind.LocalFunction;

                binder.ValidateParameterNameConflicts(typeParameters, parameters.Cast<TParameterSymbol, ParameterSymbol>(), allowShadowingNames, diagnostics);
            }

            return parameters;
        }

        internal static void EnsureIsReadOnlyAttributeExists(CSharpCompilation compilation, ImmutableArray<ParameterSymbol> parameters, DiagnosticBag diagnostics, bool modifyCompilation)
        {
            // These parameters might not come from a compilation (example: lambdas evaluated in EE).
            // During rewriting, lowering will take care of flagging the appropriate PEModuleBuilder instead.
            if (compilation == null)
            {
                return;
            }

            foreach (var parameter in parameters)
            {
                if (parameter.RefKind == RefKind.In)
                {
                    compilation.EnsureIsReadOnlyAttributeExists(diagnostics, GetParameterLocation(parameter), modifyCompilation);
                }
            }
        }

        internal static void EnsureNativeIntegerAttributeExists(CSharpCompilation compilation, ImmutableArray<ParameterSymbol> parameters, DiagnosticBag diagnostics, bool modifyCompilation)
        {
            // These parameters might not come from a compilation (example: lambdas evaluated in EE).
            // During rewriting, lowering will take care of flagging the appropriate PEModuleBuilder instead.
            if (compilation == null)
            {
                return;
            }

            foreach (var parameter in parameters)
            {
                if (parameter.TypeWithAnnotations.ContainsNativeInteger())
                {
                    compilation.EnsureNativeIntegerAttributeExists(diagnostics, GetParameterLocation(parameter), modifyCompilation);
                }
            }
        }

        internal static void EnsureNullableAttributeExists(CSharpCompilation compilation, Symbol container, ImmutableArray<ParameterSymbol> parameters, DiagnosticBag diagnostics, bool modifyCompilation)
        {
            // These parameters might not come from a compilation (example: lambdas evaluated in EE).
            // During rewriting, lowering will take care of flagging the appropriate PEModuleBuilder instead.
            if (compilation == null)
            {
                return;
            }

            if (parameters.Length > 0 && compilation.ShouldEmitNullableAttributes(container))
            {
                foreach (var parameter in parameters)
                {
                    if (parameter.TypeWithAnnotations.NeedsNullableAttribute())
                    {
                        compilation.EnsureNullableAttributeExists(diagnostics, GetParameterLocation(parameter), modifyCompilation);
                    }
                }
            }
        }

        private static Location GetParameterLocation(ParameterSymbol parameter) => parameter.GetNonNullSyntaxNode().Location;

        private static void CheckParameterModifiers(BaseParameterSyntax parameter, DiagnosticBag diagnostics, bool parsingFunctionPointerParams)
        {
            var seenThis = false;
            var seenRef = false;
            var seenOut = false;
            var seenParams = false;
            var seenIn = false;

            foreach (var modifier in parameter.Modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.ThisKeyword:
                        if (seenThis)
                        {
                            diagnostics.Add(ErrorCode.ERR_DupParamMod, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ThisKeyword));
                        }
                        else if (seenOut)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ThisKeyword), SyntaxFacts.GetText(SyntaxKind.OutKeyword));
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
                        if (seenRef)
                        {
                            diagnostics.Add(ErrorCode.ERR_DupParamMod, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.RefKeyword));
                        }
                        else if (seenParams)
                        {
                            diagnostics.Add(ErrorCode.ERR_ParamsCantBeWithModifier, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.RefKeyword));
                        }
                        else if (seenOut)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.RefKeyword), SyntaxFacts.GetText(SyntaxKind.OutKeyword));
                        }
                        else if (seenIn)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.RefKeyword), SyntaxFacts.GetText(SyntaxKind.InKeyword));
                        }
                        else
                        {
                            seenRef = true;
                        }
                        break;

                    case SyntaxKind.OutKeyword:
                        if (seenOut)
                        {
                            diagnostics.Add(ErrorCode.ERR_DupParamMod, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.OutKeyword));
                        }
                        else if (seenThis)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.OutKeyword), SyntaxFacts.GetText(SyntaxKind.ThisKeyword));
                        }
                        else if (seenParams)
                        {
                            diagnostics.Add(ErrorCode.ERR_ParamsCantBeWithModifier, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.OutKeyword));
                        }
                        else if (seenRef)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.OutKeyword), SyntaxFacts.GetText(SyntaxKind.RefKeyword));
                        }
                        else if (seenIn)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.OutKeyword), SyntaxFacts.GetText(SyntaxKind.InKeyword));
                        }
                        else
                        {
                            seenOut = true;
                        }
                        break;

                    case SyntaxKind.ParamsKeyword when !parsingFunctionPointerParams:
                        if (seenParams)
                        {
                            diagnostics.Add(ErrorCode.ERR_DupParamMod, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ParamsKeyword));
                        }
                        else if (seenThis)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParamModThis, modifier.GetLocation());
                        }
                        else if (seenRef)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ParamsKeyword), SyntaxFacts.GetText(SyntaxKind.RefKeyword));
                        }
                        else if (seenIn)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ParamsKeyword), SyntaxFacts.GetText(SyntaxKind.InKeyword));
                        }
                        else if (seenOut)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ParamsKeyword), SyntaxFacts.GetText(SyntaxKind.OutKeyword));
                        }
                        else
                        {
                            seenParams = true;
                        }
                        break;

                    case SyntaxKind.InKeyword:
                        if (seenIn)
                        {
                            diagnostics.Add(ErrorCode.ERR_DupParamMod, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.InKeyword));
                        }
                        else if (seenOut)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.InKeyword), SyntaxFacts.GetText(SyntaxKind.OutKeyword));
                        }
                        else if (seenRef)
                        {
                            diagnostics.Add(ErrorCode.ERR_BadParameterModifiers, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.InKeyword), SyntaxFacts.GetText(SyntaxKind.RefKeyword));
                        }
                        else if (seenParams)
                        {
                            diagnostics.Add(ErrorCode.ERR_ParamsCantBeWithModifier, modifier.GetLocation(), SyntaxFacts.GetText(SyntaxKind.InKeyword));
                        }
                        else
                        {
                            seenIn = true;
                        }
                        break;

                    case SyntaxKind.ParamsKeyword when parsingFunctionPointerParams:
                    case SyntaxKind.ReadOnlyKeyword when parsingFunctionPointerParams:
                        diagnostics.Add(ErrorCode.ERR_BadFuncPointerParamModifier, modifier.GetLocation(), SyntaxFacts.GetText(modifier.Kind()));
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(modifier.Kind());
                }
            }
        }

        private static void ReportParameterErrors(
            Symbol owner,
            BaseParameterSyntax parameterSyntax,
            ParameterSymbol parameter,
            SyntaxToken thisKeyword,
            SyntaxToken paramsKeyword,
            int firstDefault,
            DiagnosticBag diagnostics)
        {
            int parameterIndex = parameter.Ordinal;
            bool isDefault = parameterSyntax is ParameterSyntax { Default: { } };

            if (thisKeyword.Kind() == SyntaxKind.ThisKeyword && parameterIndex != 0)
            {
                // Report CS1100 on "this". Note that is a change from Dev10
                // which reports the error on the type following "this".

                // error CS1100: Method '{0}' has a parameter modifier 'this' which is not on the first parameter
                diagnostics.Add(ErrorCode.ERR_BadThisParam, thisKeyword.GetLocation(), owner.Name);
            }
            else if (parameter.IsParams && owner.IsOperator())
            {
                // error CS1670: params is not valid in this context
                diagnostics.Add(ErrorCode.ERR_IllegalParams, paramsKeyword.GetLocation());
            }
            else if (parameter.IsParams && !parameter.TypeWithAnnotations.IsSZArray())
            {
                // error CS0225: The params parameter must be a single dimensional array
                diagnostics.Add(ErrorCode.ERR_ParamsMustBeArray, paramsKeyword.GetLocation());
            }
            else if (parameter.TypeWithAnnotations.IsStatic)
            {
                Debug.Assert(parameter.ContainingSymbol is FunctionPointerMethodSymbol or { ContainingType: not null });
                // error CS0721: '{0}': static types cannot be used as parameters
                diagnostics.Add(
                    ErrorFacts.GetStaticClassParameterCode(parameter.ContainingSymbol.ContainingType?.IsInterfaceType() ?? false),
                    owner.Locations.IsEmpty ? parameterSyntax.GetLocation() : owner.Locations[0],
                    parameter.Type);
            }
            else if (firstDefault != -1 && parameterIndex > firstDefault && !isDefault && !parameter.IsParams)
            {
                // error CS1737: Optional parameters must appear after all required parameters
                Location loc = ((ParameterSyntax)(BaseParameterSyntax)parameterSyntax).Identifier.GetNextToken(includeZeroWidth: true).GetLocation(); //could be missing
                diagnostics.Add(ErrorCode.ERR_DefaultValueBeforeRequiredValue, loc);
            }
            else if (parameter.RefKind != RefKind.None &&
                parameter.TypeWithAnnotations.IsRestrictedType(ignoreSpanLikeTypes: true))
            {
                // CS1601: Cannot make reference to variable of type 'System.TypedReference'
                diagnostics.Add(ErrorCode.ERR_MethodArgCantBeRefAny, parameterSyntax.Location, parameter.Type);
            }
        }

        internal static bool ReportDefaultParameterErrors(
            Binder binder,
            Symbol owner,
            ParameterSyntax parameterSyntax,
            SourceParameterSymbol parameter,
            BoundExpression defaultExpression,
            BoundExpression convertedExpression,
            DiagnosticBag diagnostics)
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

            TypeSymbol parameterType = parameter.Type;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion conversion = binder.Conversions.ClassifyImplicitConversionFromExpression(defaultExpression, parameterType, ref useSiteDiagnostics);
            diagnostics.Add(defaultExpression.Syntax, useSiteDiagnostics);

            var refKind = GetModifiers(parameterSyntax.Modifiers, out SyntaxToken refnessKeyword, out SyntaxToken paramsKeyword, out SyntaxToken thisKeyword);

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
                // error CS1751: Cannot specify a default value for a parameter array
                diagnostics.Add(ErrorCode.ERR_DefaultValueForParamsParameter, paramsKeyword.GetLocation());
                hasErrors = true;
            }
            else if (thisKeyword.Kind() == SyntaxKind.ThisKeyword)
            {
                // Only need to report CS1743 for the first parameter. The caller will
                // have reported CS1100 if 'this' appeared on another parameter.
                if (parameter.Ordinal == 0)
                {
                    // error CS1743: Cannot specify a default value for the 'this' parameter
                    diagnostics.Add(ErrorCode.ERR_DefaultValueForExtensionParameter, thisKeyword.GetLocation());
                    hasErrors = true;
                }
            }
            else if (!defaultExpression.HasAnyErrors && !IsValidDefaultValue(defaultExpression.IsTypelessNew() ? convertedExpression : defaultExpression))
            {
                // error CS1736: Default parameter value for '{0}' must be a compile-time constant
                diagnostics.Add(ErrorCode.ERR_DefaultValueMustBeConstant, parameterSyntax.Default.Value.Location, parameterSyntax.Identifier.ValueText);
                hasErrors = true;
            }
            else if (!conversion.Exists ||
                conversion.IsUserDefined ||
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
                (parameterType.SpecialType == SpecialType.System_Object || parameterType.Kind == SymbolKind.DynamicType) &&
                (object)defaultExpression.Type != null &&
                defaultExpression.Type.SpecialType == SpecialType.System_String ||
                conversion.IsBoxing)
            {
                // We don't allow object x = "hello", object x = 123, dynamic x = "hello", etc.
                // error CS1763: '{0}' is of type '{1}'. A default parameter value of a reference type other than string can only be initialized with null
                diagnostics.Add(ErrorCode.ERR_NotNullRefDefaultParameter, parameterSyntax.Identifier.GetLocation(),
                    parameterSyntax.Identifier.ValueText, parameterType);

                hasErrors = true;
            }
            else if (conversion.IsNullable && !defaultExpression.Type.IsNullableType() &&
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
                    defaultExpression.Type, parameterSyntax.Identifier.ValueText);

                hasErrors = true;
            }

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
            if (expression.ConstantValue != null)
            {
                return true;
            }
            while (true)
            {
                switch (expression.Kind)
                {
                    case BoundKind.DefaultLiteral:
                    case BoundKind.DefaultExpression:
                        return true;
                    case BoundKind.ObjectCreationExpression:
                        return IsValidDefaultValue((BoundObjectCreationExpression)expression);
                    default:
                        return false;
                }
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

        private static RefKind GetModifiers(SyntaxTokenList modifiers, out SyntaxToken refnessKeyword, out SyntaxToken paramsKeyword, out SyntaxToken thisKeyword)
        {
            var refKind = RefKind.None;

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
                        paramsKeyword = modifier;
                        break;
                    case SyntaxKind.ThisKeyword:
                        thisKeyword = modifier;
                        break;
                }
            }

            return refKind;
        }

        internal static ImmutableArray<CustomModifier> ConditionallyCreateInModifiers(RefKind refKind, bool addRefReadOnlyModifier, Binder binder, DiagnosticBag diagnostics, SyntaxNode syntax)
        {
            if (addRefReadOnlyModifier && refKind == RefKind.In)
            {
                return CreateInModifiers(binder, diagnostics, syntax);
            }
            else
            {
                return ImmutableArray<CustomModifier>.Empty;
            }
        }

        internal static ImmutableArray<CustomModifier> CreateInModifiers(Binder binder, DiagnosticBag diagnostics, SyntaxNode syntax)
        {
            return CreateModifiers(WellKnownType.System_Runtime_InteropServices_InAttribute, binder, diagnostics, syntax);
        }

        internal static ImmutableArray<CustomModifier> CreateOutModifiers(Binder binder, DiagnosticBag diagnostics, SyntaxNode syntax)
        {
            return CreateModifiers(WellKnownType.System_Runtime_InteropServices_OutAttribute, binder, diagnostics, syntax);
        }

        private static ImmutableArray<CustomModifier> CreateModifiers(WellKnownType modifier, Binder binder, DiagnosticBag diagnostics, SyntaxNode syntax)
        {
            var modifierType = binder.GetWellKnownType(modifier, diagnostics, syntax);
            return ImmutableArray.Create(CSharpCustomModifier.CreateRequired(modifierType));
        }
    }
}
