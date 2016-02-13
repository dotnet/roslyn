// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class AssemblySymbolKey : AbstractSymbolKey<AssemblySymbolKey>
        {
            private readonly string _assemblyName;

            internal AssemblySymbolKey(IAssemblySymbol symbol)
            {
                _assemblyName = symbol.Identity.Name;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                return CreateSymbolInfo(GetAssemblySymbols(compilation, ignoreAssemblyKey));
            }

            private IEnumerable<IAssemblySymbol> GetAssemblySymbols(Compilation compilation, bool ignoreAssemblyKey)
            {
                if (ignoreAssemblyKey || compilation.Assembly.Identity.Name == _assemblyName)
                {
                    yield return compilation.Assembly;
                }

                // Might need keys for symbols from previous script compilations.
                foreach (var assembly in compilation.GetReferencedAssemblySymbols())
                {
                    if (ignoreAssemblyKey || assembly.Identity.Name == _assemblyName)
                    {
                        yield return assembly;
                    }
                }
            }

            internal override bool Equals(AssemblySymbolKey other, ComparisonOptions options)
            {
                // isCaseSensitive doesn't apply here as AssemblyIdentity is always case
                // insensitive.
                return options.IgnoreAssemblyKey || other._assemblyName == _assemblyName;
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // isCaseSensitive doesn't apply here as AssemblyIdentity is always case
                // insensitive.
                return options.IgnoreAssemblyKey ? 1 : _assemblyName.GetHashCode();
            }
        }
    }
}
