// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

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
            CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax != null);
            Debug.Assert(syntax.IsAnonymousFunction());

            var names = default(ImmutableArray<string>);
            var refKinds = default(ImmutableArray<RefKind>);
            var types = default(ImmutableArray<TypeWithAnnotations>);
            bool isAsync = false;

            var namesBuilder = ArrayBuilder<string>.GetInstance();
            ImmutableArray<bool> discardsOpt = default;
            SeparatedSyntaxList<ParameterSyntax>? parameterSyntaxList = null;
            bool hasSignature;

            switch (syntax.Kind())
            {
                default:
                case SyntaxKind.SimpleLambdaExpression:
                    // x => ...
                    hasSignature = true;
                    var simple = (SimpleLambdaExpressionSyntax)syntax;
                    namesBuilder.Add(simple.Parameter.Identifier.ValueText);
                    isAsync = (simple.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword);
                    break;
                case SyntaxKind.ParenthesizedLambdaExpression:
                    // (T x, U y) => ...
                    // (x, y) => ...
                    hasSignature = true;
                    var paren = (ParenthesizedLambdaExpressionSyntax)syntax;
                    parameterSyntaxList = paren.ParameterList.Parameters;
                    CheckParenthesizedLambdaParameters(parameterSyntaxList.Value, diagnostics);
                    isAsync = (paren.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword);
                    break;
                case SyntaxKind.AnonymousMethodExpression:
                    // delegate (int x) { }
                    // delegate { }
                    var anon = (AnonymousMethodExpressionSyntax)syntax;
                    hasSignature = anon.ParameterList != null;
                    if (hasSignature)
                    {
                        parameterSyntaxList = anon.ParameterList.Parameters;
                    }
                    isAsync = (anon.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword);
                    break;
            }

            if (parameterSyntaxList != null)
            {
                var hasExplicitlyTypedParameterList = true;
                var allValue = true;

                var typesBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                var refKindsBuilder = ArrayBuilder<RefKind>.GetInstance();

                // In the batch compiler case we probably should have given a syntax error if the
                // user did something like (int x, y)=>x+y -- but in the IDE scenario we might be in
                // this case. If we are, then rather than try to make partial deductions from the
                // typed formal parameters, simply bail out and treat it as an untyped lambda.
                //
                // However, we still want to give errors on every bad type in the list, even if one
                // is missing.

                int underscoresCount = 0;
                foreach (var p in parameterSyntaxList.Value)
                {
                    if (p.Identifier.IsUnderscoreToken())
                    {
                        underscoresCount++;
                    }

                    foreach (var attributeList in p.AttributeLists)
                    {
                        Error(diagnostics, ErrorCode.ERR_AttributesNotAllowed, attributeList);
                    }

                    if (p.Default != null)
                    {
                        Error(diagnostics, ErrorCode.ERR_DefaultValueNotAllowed, p.Default.EqualsToken);
                    }

                    if (p.IsArgList)
                    {
                        Error(diagnostics, ErrorCode.ERR_IllegalVarArgs, p);
                        continue;
                    }

                    var typeSyntax = p.Type;
                    TypeWithAnnotations type = default;
                    var refKind = RefKind.None;

                    if (typeSyntax == null)
                    {
                        hasExplicitlyTypedParameterList = false;
                    }
                    else
                    {
                        type = BindType(typeSyntax, diagnostics);
                        foreach (var modifier in p.Modifiers)
                        {
                            switch (modifier.Kind())
                            {
                                case SyntaxKind.RefKeyword:
                                    refKind = RefKind.Ref;
                                    allValue = false;
                                    break;

                                case SyntaxKind.OutKeyword:
                                    refKind = RefKind.Out;
                                    allValue = false;
                                    break;

                                case SyntaxKind.InKeyword:
                                    refKind = RefKind.In;
                                    allValue = false;
                                    break;

                                case SyntaxKind.ParamsKeyword:
                                    // This was a parse error in the native compiler; 
                                    // it is a semantic analysis error in Roslyn. See comments to
                                    // changeset 1674 for details.
                                    Error(diagnostics, ErrorCode.ERR_IllegalParams, p);
                                    break;

                                case SyntaxKind.ThisKeyword:
                                    Error(diagnostics, ErrorCode.ERR_ThisInBadContext, modifier);
                                    break;
                            }
                        }
                    }

                    namesBuilder.Add(p.Identifier.ValueText);
                    typesBuilder.Add(type);
                    refKindsBuilder.Add(refKind);
                }

                discardsOpt = computeDiscards(parameterSyntaxList.Value, underscoresCount);

                if (hasExplicitlyTypedParameterList)
                {
                    types = typesBuilder.ToImmutable();
                }

                if (!allValue)
                {
                    refKinds = refKindsBuilder.ToImmutable();
                }

                typesBuilder.Free();
                refKindsBuilder.Free();
            }

            if (hasSignature)
            {
                names = namesBuilder.ToImmutable();
            }

            namesBuilder.Free();

            return new UnboundLambda(syntax, this, refKinds, types, names, discardsOpt, isAsync);

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
        }

        private void CheckParenthesizedLambdaParameters(
            SeparatedSyntaxList<ParameterSyntax> parameterSyntaxList, DiagnosticBag diagnostics)
        {
            if (parameterSyntaxList.Count > 0)
            {
                var hasTypes = parameterSyntaxList[0].Type != null;

                for (int i = 1, n = parameterSyntaxList.Count; i < n; i++)
                {
                    var parameter = parameterSyntaxList[i];

                    // Ignore parameters with missing names.  We'll have already reported an error
                    // about them in the parser.
                    if (!parameter.Identifier.IsMissing)
                    {
                        var thisParameterHasType = parameter.Type != null;
                        if (hasTypes != thisParameterHasType)
                        {
                            diagnostics.Add(ErrorCode.ERR_InconsistentLambdaParameterUsage,
                                parameter.Type?.GetLocation() ?? parameter.Identifier.GetLocation());
                        }
                    }
                }
            }
        }

        private UnboundLambda BindAnonymousFunction(CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax != null);
            Debug.Assert(syntax.IsAnonymousFunction());

            var lambda = AnalyzeAnonymousFunction(syntax, diagnostics);
            var data = (PlainUnboundLambdaState)lambda.Data;
            if (data.HasTypes)
            {
                for (int i = 0; i < lambda.ParameterCount; i++)
                {
                    // UNDONE: Where do we report improper use of pointer types?
                    var type = lambda.Data.ParameterTypeWithAnnotations(i);
                    if (type.HasType && type.IsStatic)
                    {
                        Error(diagnostics, ErrorCode.ERR_ParameterIsStaticClass, syntax, type.Type);
                    }
                }
            }

            if (data.HasNames)
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
                            MessageID.IDS_FeatureDiscardParameters.CheckFeatureAvailability(
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
    }
}
