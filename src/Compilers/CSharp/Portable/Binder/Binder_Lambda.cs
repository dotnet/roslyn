// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Collections;

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

        private Tuple<ImmutableArray<RefKind>, ImmutableArray<TypeSymbolWithAnnotations>, ImmutableArray<string>, bool> AnalyzeAnonymousFunction(
            CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax != null);
            Debug.Assert(syntax.IsAnonymousFunction());

            var names = default(ImmutableArray<string>);
            var refKinds = default(ImmutableArray<RefKind>);
            var types = default(ImmutableArray<TypeSymbolWithAnnotations>);
            bool isAsync = false;

            var namesBuilder = ArrayBuilder<string>.GetInstance();
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

                var typesBuilder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance();
                var refKindsBuilder = ArrayBuilder<RefKind>.GetInstance();

                // In the batch compiler case we probably should have given a syntax error if the
                // user did something like (int x, y)=>x+y -- but in the IDE scenario we might be in
                // this case. If we are, then rather than try to make partial deductions from the
                // typed formal parameters, simply bail out and treat it as an untyped lambda.
                //
                // However, we still want to give errors on every bad type in the list, even if one
                // is missing.

                foreach (var p in parameterSyntaxList.Value)
                {
                    if (p.IsArgList)
                    {
                        Error(diagnostics, ErrorCode.ERR_IllegalVarArgs, p);
                        continue;
                    }

                    var typeSyntax = p.Type;
                    TypeSymbolWithAnnotations type = null;
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
                            if (modifier.Kind() == SyntaxKind.RefKeyword)
                            {
                                refKind = RefKind.Ref;
                                allValue = false;
                                break;
                            }
                            else if (modifier.Kind() == SyntaxKind.OutKeyword)
                            {
                                refKind = RefKind.Out;
                                allValue = false;
                                break;
                            }
                            else if (modifier.Kind() == SyntaxKind.ParamsKeyword)
                            {
                                // This was a parse error in the native compiler; 
                                // it is a semantic analysis error in Roslyn. See comments to
                                // changeset 1674 for details.
                                Error(diagnostics, ErrorCode.ERR_IllegalParams, p);
                            }
                        }
                    }

                    namesBuilder.Add(p.Identifier.ValueText);
                    typesBuilder.Add(type);
                    refKindsBuilder.Add(refKind);
                }

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

            return Tuple.Create(refKinds, types, names, isAsync);
        }

        private UnboundLambda BindAnonymousFunction(CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax != null);
            Debug.Assert(syntax.IsAnonymousFunction());

            var results = AnalyzeAnonymousFunction(syntax, diagnostics);

            var refKinds = results.Item1;
            var types = results.Item2;
            var names = results.Item3;
            var isAsync = results.Item4;

            if (!types.IsDefault)
            {
                foreach (var type in types)
                {
                    // UNDONE: Where do we report improper use of pointer types?
                    if ((object)type != null && type.IsStatic)
                    {
                        Error(diagnostics, ErrorCode.ERR_ParameterIsStaticClass, syntax, type.TypeSymbol);
                    }
                }
            }

            var lambda = new UnboundLambda(syntax, this, refKinds, types, names, isAsync);
            if (!names.IsDefault)
            {
                var binder = new LocalScopeBinder(this);
                var pNames = PooledHashSet<string>.GetInstance();

                for (int i = 0; i < lambda.ParameterCount; i++)
                {
                    var name = lambda.ParameterName(i);

                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    if (pNames.Contains(name))
                    {
                        // The parameter name '{0}' is a duplicate
                        diagnostics.Add(ErrorCode.ERR_DuplicateParamName, lambda.ParameterLocation(i), name);
                    }
                    else
                    {
                        pNames.Add(name);
                        binder.ValidateLambdaParameterNameConflictsInScope(lambda.ParameterLocation(i), name, diagnostics);
                    }
                }
                pNames.Free();
            }

            return lambda;
        }
    }
}
