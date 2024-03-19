// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Binder used to place Primary Constructor parameters, if any, in scope.
    /// </summary>
    internal sealed class WithPrimaryConstructorParametersBinder : Binder
    {
        private readonly NamedTypeSymbol _type;
        private MethodSymbol? _lazyPrimaryCtorWithParameters = ErrorMethodSymbol.UnknownMethod;
        private MultiDictionary<string, ParameterSymbol>? _lazyParameterMap;

        internal WithPrimaryConstructorParametersBinder(NamedTypeSymbol type, Binder next)
            : base(next)
        {
            _type = type;
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderMembers())
            {
                EnsurePrimaryConstructor();

                if (_lazyPrimaryCtorWithParameters is null)
                {
                    return;
                }

                foreach (var parameter in _lazyPrimaryCtorWithParameters.Parameters)
                {
                    if (originalBinder.CanAddLookupSymbolInfo(parameter, options, result, null))
                    {
                        result.AddSymbol(parameter, parameter.Name, 0);
                    }
                }
            }
        }

        private void EnsurePrimaryConstructor()
        {
            if (_lazyPrimaryCtorWithParameters == (object)ErrorMethodSymbol.UnknownMethod)
            {
                if (_type is SourceMemberContainerTypeSymbol { PrimaryConstructor: { ParameterCount: not 0 } primaryCtor })
                {
                    _lazyPrimaryCtorWithParameters = primaryCtor;
                }
                else
                {
                    _lazyPrimaryCtorWithParameters = null;
                }
            }
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(result.IsClear);

            if ((options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.NamespacesOrTypesOnly)) != 0)
            {
                return;
            }

            EnsurePrimaryConstructor();

            if (_lazyPrimaryCtorWithParameters is null)
            {
                return;
            }

            var parameterMap = _lazyParameterMap;
            if (parameterMap == null)
            {
                var parameters = _lazyPrimaryCtorWithParameters.Parameters;
                parameterMap = new MultiDictionary<string, ParameterSymbol>(parameters.Length, EqualityComparer<string>.Default);
                foreach (var parameter in parameters)
                {
                    parameterMap.Add(parameter.Name, parameter);
                }

                _lazyParameterMap = parameterMap;
            }

            foreach (var parameterSymbol in parameterMap[name])
            {
                result.MergeEqual(originalBinder.CheckViability(parameterSymbol, arity, options, null, diagnose, ref useSiteInfo));
            }
        }
    }
}
