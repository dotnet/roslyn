// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using static Microsoft.CodeAnalysis.CSharp.Symbols.ParameterHelpers;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        // An anonymous function can be of the form:
        // 
        // delegate { }              (missing parameter list)
        // delegate (int x) { }      (typed parameter list)
        // x => ...                  (type-inferred parameter list)
        // (x) => ...                (type-inferred parameter list)
        // (ref x) => ...            (type-inferred parameter list with modifiers)
        // (x, y) => ...             (type-inferred parameter list)
        // ( ) => ...                (typed parameter list)
        // (ref int x) => ...        (typed parameter list)
        // (int x, out int y) => ... (typed parameter list)
        //
        // and so on. We want to canonicalize these various ways of writing the signatures.
        // 
        // If we are in the first case then the name, modifier and type arrays are all null.
        // If we have a parameter list then the names array is non-null, but possibly empty.
        // If we have types then the types array is non-null, but possibly empty.
        // If we have no modifiers then the modifiers array is null; if we have any modifiers
        // then the modifiers array is non-null and not empty.

        private UnboundLambda AnalyzeAnonymousFunction(
            AnonymousFunctionExpressionSyntax syntax, BindingDiagnosticBag diagnostics)
        {
            // !!! The only binding operations allowed here - binding type references

            Debug.Assert(syntax != null);
            Debug.Assert(syntax.IsAnonymousFunction());

            ImmutableArray<string> names = default;
            ImmutableArray<RefKind> refKinds = default;
            ImmutableArray<ScopedKind> scopes = default;
            ImmutableArray<TypeWithAnnotations> types = default;
            ImmutableArray<EqualsValueClauseSyntax?> defaultValues = default;
            RefKind returnRefKind = RefKind.None;
            ImmutableArray<CustomModifier> refCustomModifiers = [];
            TypeWithAnnotations returnType = default;
            ImmutableArray<SyntaxList<AttributeListSyntax>> parameterAttributes = default;

            var namesBuilder = ArrayBuilder<string>.GetInstance();
            ImmutableArray<bool> discardsOpt = default;
            SeparatedSyntaxList<ParameterSyntax>? parameterSyntaxListOpt = null;
            bool hasSignature;

            if (syntax is LambdaExpressionSyntax lambdaSyntax)
            {
                MessageID.IDS_FeatureLambda.CheckFeatureAvailability(diagnostics, lambdaSyntax.ArrowToken);

                checkAttributes(syntax, lambdaSyntax.AttributeLists, diagnostics);
            }

            switch (syntax.Kind())
            {
                default:
                case SyntaxKind.SimpleLambdaExpression:
                    // x => ...
                    hasSignature = true;
                    var simple = (SimpleLambdaExpressionSyntax)syntax;
                    ReportFieldContextualKeywordConflictIfAny(simple.Parameter, diagnostics);
                    namesBuilder.Add(simple.Parameter.Identifier.ValueText);
                    break;
                case SyntaxKind.ParenthesizedLambdaExpression:
                    // (T x, U y) => ...
                    // (x, y) => ...
                    hasSignature = true;
                    var paren = (ParenthesizedLambdaExpressionSyntax)syntax;
                    if (paren.ReturnType is { } returnTypeSyntax)
                    {
                        (returnRefKind, refCustomModifiers, returnType) = BindExplicitLambdaReturnType(returnTypeSyntax, diagnostics);
                    }

                    parameterSyntaxListOpt = paren.ParameterList.Parameters;
                    CheckParenthesizedLambdaParameters(parameterSyntaxListOpt.Value, diagnostics);
                    break;
                case SyntaxKind.AnonymousMethodExpression:
                    // delegate (int x) { }
                    // delegate { }
                    var anon = (AnonymousMethodExpressionSyntax)syntax;
                    MessageID.IDS_FeatureAnonDelegates.CheckFeatureAvailability(diagnostics, anon.DelegateKeyword);

                    hasSignature = anon.ParameterList != null;
                    if (hasSignature)
                    {
                        parameterSyntaxListOpt = anon.ParameterList!.Parameters;
                    }

                    break;
            }

            bool isAsync = false;
            bool isStatic = false;

            foreach (var modifier in syntax.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.AsyncKeyword))
                {
                    MessageID.IDS_FeatureAsync.CheckFeatureAvailability(diagnostics, modifier);
                    isAsync = true;
                }
                else if (modifier.IsKind(SyntaxKind.StaticKeyword))
                {
                    MessageID.IDS_FeatureStaticAnonymousFunction.CheckFeatureAvailability(diagnostics, modifier);
                    isStatic = true;
                }
            }

            if (parameterSyntaxListOpt is { } parameterSyntaxList)
            {
                var isAnonymousMethod = syntax.IsKind(SyntaxKind.AnonymousMethodExpression);
                var hasExplicitlyTypedParameterList = parameterSyntaxList.All(static p => p.Type != null);

                var typesBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                var refKindsBuilder = ArrayBuilder<RefKind>.GetInstance();
                var scopesBuilder = ArrayBuilder<ScopedKind>.GetInstance();
                var attributesBuilder = ArrayBuilder<SyntaxList<AttributeListSyntax>>.GetInstance();
                var defaultValueBuilder = ArrayBuilder<EqualsValueClauseSyntax?>.GetInstance();

                // In the batch compiler case we probably should have given a syntax error if the
                // user did something like (int x, y)=>x+y -- but in the IDE scenario we might be in
                // this case. If we are, then rather than try to make partial deductions from the
                // typed formal parameters, simply bail out and treat it as an untyped lambda.
                //
                // However, we still want to give errors on every bad type in the list, even if one
                // is missing.

                int parameterCount = 0;
                int underscoresCount = 0;
                int firstDefault = -1;
                for (int i = 0, n = parameterSyntaxList.Count; i < n; i++)
                {
                    parameterCount++;

                    var p = parameterSyntaxList[i];
                    if (p.Identifier.IsUnderscoreToken())
                    {
                        underscoresCount++;
                    }

                    checkAttributes(syntax, p.AttributeLists, diagnostics);

                    if (p.Default != null)
                    {
                        if (firstDefault == -1)
                        {
                            firstDefault = i;
                        }

                        if (isAnonymousMethod)
                        {
                            Error(diagnostics, ErrorCode.ERR_DefaultValueNotAllowed, p.Default.EqualsToken);
                        }
                        else
                        {
                            MessageID.IDS_FeatureLambdaOptionalParameters.CheckFeatureAvailability(diagnostics, p.Default.EqualsToken);
                        }
                    }

                    if (p.IsArgList)
                    {
                        Error(diagnostics, ErrorCode.ERR_IllegalVarArgs, p);
                        continue;
                    }

                    var typeOpt = p.Type is not null ? BindType(p.Type, diagnostics) : default;

                    var refKind = ParameterHelpers.GetModifiers(p.Modifiers, ignoreParams: false, out _, out var paramsKeyword, out _, out var scope);
                    var isParams = paramsKeyword.Kind() != SyntaxKind.None;

                    ParameterHelpers.CheckParameterModifiers(p, diagnostics, isAnonymousMethod ? ParameterContext.AnonymousMethod : ParameterContext.Lambda);

                    ParameterHelpers.ReportParameterErrors(
                        owner: null, syntax: p, ordinal: i, lastParameterIndex: n - 1, isParams: isParams, typeWithAnnotations: typeOpt,
                        refKind: refKind, containingSymbol: null, thisKeyword: default, paramsKeyword: paramsKeyword, firstDefault: firstDefault, diagnostics: diagnostics);

                    if (parameterCount == parameterSyntaxList.Count &&
                        paramsKeyword.Kind() != SyntaxKind.None &&
                        !typeOpt.IsDefault &&
                        typeOpt.IsSZArray())
                    {
                        ReportUseSiteDiagnosticForSynthesizedAttribute(Compilation,
                            WellKnownMember.System_ParamArrayAttribute__ctor,
                            diagnostics,
                            paramsKeyword.GetLocation());
                    }

                    ReportFieldContextualKeywordConflictIfAny(p, diagnostics);

                    namesBuilder.Add(p.Identifier.ValueText);
                    typesBuilder.Add(typeOpt);
                    refKindsBuilder.Add(refKind);
                    scopesBuilder.Add(scope);
                    attributesBuilder.Add(syntax.Kind() == SyntaxKind.ParenthesizedLambdaExpression ? p.AttributeLists : default);
                    defaultValueBuilder.Add(p.Default);
                }

                discardsOpt = computeDiscards(parameterSyntaxListOpt.Value, underscoresCount);

                // Only include the types if *all* the parameters had types.  Otherwise, if there were no parameter
                // types (or a mix of typed and untyped parameters) include no types.  Note, in the latter case we will
                // have already reported an error about this issue.
                if (hasExplicitlyTypedParameterList)
                {
                    types = typesBuilder.ToImmutable();
                }

                if (refKindsBuilder.Any(r => r != RefKind.None))
                {
                    refKinds = refKindsBuilder.ToImmutable();
                }

                if (scopesBuilder.Any(s => s != ScopedKind.None))
                {
                    scopes = scopesBuilder.ToImmutable();
                }

                if (attributesBuilder.Any(a => a.Count > 0))
                {
                    parameterAttributes = attributesBuilder.ToImmutable();
                }

                if (defaultValueBuilder.Any(v => v != null))
                {
                    defaultValues = defaultValueBuilder.ToImmutable();
                }

                typesBuilder.Free();
                scopesBuilder.Free();
                refKindsBuilder.Free();
                attributesBuilder.Free();
                defaultValueBuilder.Free();
            }

            if (hasSignature)
            {
                names = namesBuilder.ToImmutable();
            }

            namesBuilder.Free();

            return UnboundLambda.Create(syntax, this, diagnostics.AccumulatesDependencies, returnRefKind, refCustomModifiers, returnType, parameterAttributes, refKinds, scopes, types, names, discardsOpt, parameterSyntaxListOpt, defaultValues, isAsync: isAsync, isStatic: isStatic);

            static ImmutableArray<bool> computeDiscards(SeparatedSyntaxList<ParameterSyntax> parameters, int underscoresCount)
            {
                if (underscoresCount <= 1)
                {
                    return default;
                }

                // When there are two or more underscores, they are discards
                var discardsBuilder = ArrayBuilder<bool>.GetInstance(parameters.Count);
                foreach (var p in parameters)
                {
                    discardsBuilder.Add(p.Identifier.IsUnderscoreToken());
                }

                return discardsBuilder.ToImmutableAndFree();
            }

            static void checkAttributes(AnonymousFunctionExpressionSyntax syntax, SyntaxList<AttributeListSyntax> attributeLists, BindingDiagnosticBag diagnostics)
            {
                foreach (var attributeList in attributeLists)
                {
                    if (syntax.Kind() == SyntaxKind.ParenthesizedLambdaExpression)
                    {
                        MessageID.IDS_FeatureLambdaAttributes.CheckFeatureAvailability(diagnostics, attributeList);
                    }
                    else
                    {
                        Error(diagnostics, syntax.Kind() == SyntaxKind.SimpleLambdaExpression ? ErrorCode.ERR_AttributesRequireParenthesizedLambdaExpression : ErrorCode.ERR_AttributesNotAllowed, attributeList);
                    }
                }
            }

        }

        private (RefKind, ImmutableArray<CustomModifier> refCustomModifiers, TypeWithAnnotations) BindExplicitLambdaReturnType(TypeSyntax syntax, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureLambdaReturnType.CheckFeatureAvailability(diagnostics, syntax);

            Debug.Assert(syntax is not ScopedTypeSyntax);
            syntax = syntax.SkipScoped(out _).SkipRefInLocalOrReturn(diagnostics, out RefKind refKind);
            if (syntax is IdentifierNameSyntax { Identifier.RawContextualKind: (int)SyntaxKind.VarKeyword })
            {
                diagnostics.Add(ErrorCode.ERR_LambdaExplicitReturnTypeVar, syntax.Location);
            }

            var returnType = BindType(syntax, diagnostics);
            var type = returnType.Type;

            if (returnType.IsStatic)
            {
                diagnostics.Add(ErrorFacts.GetStaticClassReturnCode(useWarning: false), syntax.Location, type);
            }
            else if (returnType.IsRestrictedType(ignoreSpanLikeTypes: true))
            {
                diagnostics.Add(ErrorCode.ERR_MethodReturnCantBeRefAny, syntax.Location, type);
            }

            ImmutableArray<CustomModifier> refCustomModifiers = [];
            if (refKind == RefKind.RefReadOnly)
            {
                refCustomModifiers = [CSharpCustomModifier.CreateRequired(Binder.GetWellKnownType(Compilation, WellKnownType.System_Runtime_InteropServices_InAttribute, diagnostics, syntax.Location))];
            }

            return (refKind, refCustomModifiers, returnType);
        }

        private static void CheckParenthesizedLambdaParameters(
            SeparatedSyntaxList<ParameterSyntax> parameterSyntaxList, BindingDiagnosticBag diagnostics)
        {
            if (parameterSyntaxList.Count > 0)
            {
                // If one parameter has a type, then all parameters must have a type.
                var requiresTypes = parameterSyntaxList.Any(static p => p.Type != null);

                foreach (var parameter in parameterSyntaxList)
                {
                    // Ignore parameters with missing names.  We'll have already reported an error
                    // about them in the parser.
                    if (!parameter.Identifier.IsMissing)
                    {
                        if (requiresTypes)
                        {
                            if (parameter.Type is null)
                            {
                                diagnostics.Add(ErrorCode.ERR_InconsistentLambdaParameterUsage,
                                    parameter.Identifier.GetLocation());
                            }
                        }
                        else
                        {
                            if (parameter.Default != null)
                            {
                                diagnostics.Add(ErrorCode.ERR_ImplicitlyTypedDefaultParameter,
                                    parameter.Identifier.GetLocation(), parameter.Identifier.Text);
                            }
                        }

                        // Check if `(ref i) => ...` is supported by this language version.
                        if (parameter.Modifiers.Count > 0 && parameter.Type is null)
                        {
                            CheckFeatureAvailability(parameter, MessageID.IDS_FeatureSimpleLambdaParameterModifiers, diagnostics);
                        }
                    }
                }
            }
        }

        private UnboundLambda BindAnonymousFunction(AnonymousFunctionExpressionSyntax syntax, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(syntax != null);
            Debug.Assert(syntax.IsAnonymousFunction());

            var lambda = AnalyzeAnonymousFunction(syntax, diagnostics);
            var data = lambda.Data;

            // Parser will only have accepted static/async as allowed modifiers on this construct.
            // However, it may have accepted duplicates of those modifiers.  Ensure that any dupes
            // are reported now.
            ModifierUtils.ToDeclarationModifiers(syntax.Modifiers, isForTypeDeclaration: false, diagnostics.DiagnosticBag ?? new DiagnosticBag());

            if (data.HasSignature)
            {
                var binder = new LocalScopeBinder(this);
                bool allowShadowingNames = binder.Compilation.IsFeatureEnabled(MessageID.IDS_FeatureNameShadowingInNestedFunctions);
                var pNames = PooledHashSet<string>.GetInstance();
                bool seenDiscard = false;

                for (int i = 0; i < lambda.ParameterCount; i++)
                {
                    var name = lambda.ParameterName(i);

                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    if (lambda.ParameterIsDiscard(i))
                    {
                        if (seenDiscard)
                        {
                            // We only report the diagnostic on the second and subsequent underscores
                            MessageID.IDS_FeatureLambdaDiscardParameters.CheckFeatureAvailability(
                                diagnostics,
                                binder.Compilation,
                                lambda.ParameterLocation(i));
                        }

                        seenDiscard = true;
                        continue;
                    }

                    if (!pNames.Add(name))
                    {
                        // The parameter name '{0}' is a duplicate
                        diagnostics.Add(ErrorCode.ERR_DuplicateParamName, lambda.ParameterLocation(i), name);
                    }
                    else if (!allowShadowingNames)
                    {
                        binder.ValidateLambdaParameterNameConflictsInScope(lambda.ParameterLocation(i), name, diagnostics);
                    }
                }
                pNames.Free();
            }

            return lambda;
        }

        // Please don't use thread local storage widely. This should be one of only a few uses.
        [ThreadStatic] private static PooledDictionary<SyntaxNode, int>? s_lambdaBindings;

        internal TResult BindWithLambdaBindingCountDiagnostics<TSyntax, TArg, TResult>(
            TSyntax syntax,
            TArg arg,
            BindingDiagnosticBag diagnostics,
            Func<Binder, TSyntax, TArg, BindingDiagnosticBag, TResult> bind)
            where TSyntax : SyntaxNode
            where TResult : BoundNode
        {
            Debug.Assert(s_lambdaBindings is null);
            var bindings = PooledDictionary<SyntaxNode, int>.GetInstance();
            s_lambdaBindings = bindings;

            try
            {
                TResult result = bind(this, syntax, arg, diagnostics);

                foreach (var pair in bindings)
                {
                    // The particular max value is arbitrary, but large enough so diagnostics should
                    // only be reported for lambda expressions used as arguments to method calls
                    // where the product of the number of applicable overloads for that method call
                    // and for overloads for any containing lambda expressions is large.
                    const int maxLambdaBinding = 100;
                    int count = pair.Value;
                    if (count > maxLambdaBinding)
                    {
                        int truncatedToHundreds = (count / 100) * 100;
                        diagnostics.Add(ErrorCode.INF_TooManyBoundLambdas, GetAnonymousFunctionLocation(pair.Key), truncatedToHundreds);
                    }
                }

                return result;
            }
            finally
            {
                bindings.Free();
                s_lambdaBindings = null;
            }
        }

        internal static void RecordLambdaBinding(SyntaxNode syntax)
        {
            var bindings = s_lambdaBindings;
            if (bindings is null)
            {
                return;
            }
            if (bindings.TryGetValue(syntax, out int count))
            {
                bindings[syntax] = ++count;
            }
            else
            {
                bindings.Add(syntax, 1);
            }
        }
    }
}
