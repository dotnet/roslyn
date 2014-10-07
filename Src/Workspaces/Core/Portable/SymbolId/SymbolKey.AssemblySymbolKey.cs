// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class AssemblySymbolKey : AbstractSymbolKey<AssemblySymbolKey>
        {
            private readonly string assemblyName;

            internal AssemblySymbolKey(IAssemblySymbol symbol)
            {
                this.assemblyName = symbol.Identity.Name;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                return CreateSymbolInfo(GetAssemblySymbols(compilation, ignoreAssemblyKey));
            }

            private IEnumerable<IAssemblySymbol> GetAssemblySymbols(Compilation compilation, bool ignoreAssemblyKey)
            {
                if (ignoreAssemblyKey || compilation.Assembly.Identity.Name == this.assemblyName)
                {
                    yield return compilation.Assembly;
                }

                foreach (var reference in compilation.References)
                {
                    var assembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                    if (assembly != null && (ignoreAssemblyKey || assembly.Identity.Name == this.assemblyName))
                    {
                        yield return assembly;
                    }
                }
            }

            internal override bool Equals(AssemblySymbolKey other, ComparisonOptions options)
            {
                // isCaseSensitive doesn't apply here as AssemblyIdentity is always case
                // insensitive.
                return options.IgnoreAssemblyKey || other.assemblyName == this.assemblyName;
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // isCaseSensitive doesn't apply here as AssemblyIdentity is always case
                // insensitive.
                return options.IgnoreAssemblyKey ? 1 : this.assemblyName.GetHashCode();
            }
        }
    }
}