// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Binder used to place extension parameter, if any, in scope.
    /// </summary>
    internal sealed class WithExtensionParameterBinder : Binder
    {
        private readonly NamedTypeSymbol _type;

        internal WithExtensionParameterBinder(NamedTypeSymbol type, Binder next)
            : base(next)
        {
            _type = type;
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderMembers())
            {
                if (_type.ExtensionParameter is { Name: not "" } parameter &&
                    originalBinder.CanAddLookupSymbolInfo(parameter, options, result, null))
                {
                    result.AddSymbol(parameter, parameter.Name, 0);
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

            if (_type.ExtensionParameter is { Name: not "" } parameter && parameter.Name == name)
            {
                result.MergeEqual(originalBinder.CheckViability(parameter, arity, options, null, diagnose, ref useSiteInfo));
            }
        }
    }
}
