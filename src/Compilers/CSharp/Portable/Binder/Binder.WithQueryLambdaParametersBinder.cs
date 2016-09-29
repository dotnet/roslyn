// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        // A binder that finds query variables (BoundRangeVariableSymbol) and can bind them
        // to the appropriate rewriting involving lambda parameters when transparent identifiers are involved.
        private sealed class WithQueryLambdaParametersBinder : WithLambdaParametersBinder
        {
            private readonly RangeVariableMap _rangeVariableMap;
            private readonly MultiDictionary<string, RangeVariableSymbol> _parameterMap;

            public WithQueryLambdaParametersBinder(LambdaSymbol lambdaSymbol, RangeVariableMap rangeVariableMap, Binder next)
                : base(lambdaSymbol, next)
            {
                _rangeVariableMap = rangeVariableMap;
                _parameterMap = new MultiDictionary<string, RangeVariableSymbol>();
                foreach (var qv in rangeVariableMap.Keys)
                {
                    _parameterMap.Add(qv.Name, qv);
                }
            }

            protected override BoundExpression BindRangeVariable(SimpleNameSyntax node, RangeVariableSymbol qv, DiagnosticBag diagnostics)
            {
                Debug.Assert(!qv.IsTransparent);

                BoundExpression translation;
                ImmutableArray<string> path;
                if (_rangeVariableMap.TryGetValue(qv, out path))
                {
                    if (path.IsEmpty)
                    {
                        // the range variable maps directly to a use of the parameter of that name
                        var value = base.parameterMap[qv.Name];
                        Debug.Assert(value.Count == 1);
                        translation = new BoundParameter(node, value.Single()) { WasCompilerGenerated = true };
                    }
                    else
                    {
                        // if the query variable map for this variable is non empty, we always start with the current
                        // lambda's first parameter, which is a transparent identifier.
                        Debug.Assert(base.lambdaSymbol.Parameters[0].Name.StartsWith(transparentIdentifierPrefix, StringComparison.Ordinal));
                        translation = new BoundParameter(node, base.lambdaSymbol.Parameters[0]) { WasCompilerGenerated = true };
                        for (int i = path.Length - 1; i >= 0; i--)
                        {
                            var nextField = path[i];
                            translation = SelectField(node, translation, nextField, diagnostics);
                            translation.WasCompilerGenerated = true;
                        }
                    }

                    return new BoundRangeVariable(node, qv, translation, translation.Type);
                }

                return base.BindRangeVariable(node, qv, diagnostics);
            }

            private BoundExpression SelectField(SimpleNameSyntax node, BoundExpression receiver, string name, DiagnosticBag diagnostics)
            {
                var receiverType = receiver.Type as NamedTypeSymbol;
                if ((object)receiverType == null || !receiverType.IsAnonymousType)
                {
                    // We only construct transparent query variables using anonymous types, so if we're trying to navigate through
                    // some other type, we must have some query API where the types don't match up as expected.
                    var info = new CSDiagnosticInfo(ErrorCode.ERR_UnsupportedTransparentIdentifierAccess, name, receiver.ExpressionSymbol ?? receiverType);
                    if (receiver.Type?.IsErrorType() != true)
                    {
                        Error(diagnostics, info, node);
                    }

                    return new BoundBadExpression(
                        node,
                        LookupResultKind.Empty,
                        ImmutableArray.Create<Symbol>(receiver.ExpressionSymbol),
                        ImmutableArray.Create<BoundNode>(receiver),
                        new ExtendedErrorTypeSymbol(this.Compilation, "", 0, info));
                }

                LookupResult lookupResult = LookupResult.GetInstance();
                LookupOptions options = LookupOptions.MustBeInstance;
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                LookupMembersWithFallback(lookupResult, receiver.Type, name, 0, ref useSiteDiagnostics, basesBeingResolved: null, options: options);
                diagnostics.Add(node, useSiteDiagnostics);

                var result = BindMemberOfType(node, node, name, 0, receiver, default(SeparatedSyntaxList<TypeSyntax>), default(ImmutableArray<TypeSymbol>), lookupResult, BoundMethodGroupFlags.None, diagnostics);
                result.WasCompilerGenerated = true;
                lookupResult.Free();
                return result;
            }

            internal override void LookupSymbolsInSingleBinder(
                LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            {
                Debug.Assert(result.IsClear);

                if ((options & LookupOptions.NamespaceAliasesOnly) != 0)
                {
                    return;
                }

                foreach (var rangeVariable in _parameterMap[name])
                {
                    result.MergeEqual(originalBinder.CheckViability(rangeVariable, arity, options, null, diagnose, ref useSiteDiagnostics));
                }
            }

            protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
            {
                if (options.CanConsiderMembers())
                {
                    foreach (var kvp in _parameterMap)
                    {
                        result.AddSymbol(null, kvp.Key, 0);
                    }
                }
            }
        }
    }
}
