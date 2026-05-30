// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Creates and caches <see cref="SynthesizedRefStructClosureTypeSymbol"/> instances for the
    /// "ref struct closures for lambdas" feature (csharplang#10209). A distinct closure type is
    /// produced for each lambda conversion site (keyed by the unbound lambda and the function
    /// interface it is converted to), so that repeated inference attempts on the same site share
    /// one symbol.
    /// </summary>
    internal sealed class RefStructClosureTypeManager
    {
        private readonly CSharpCompilation _compilation;
        private readonly ConcurrentDictionary<(UnboundLambda lambda, NamedTypeSymbol functionInterface), SynthesizedRefStructClosureTypeSymbol> _cache
            = new ConcurrentDictionary<(UnboundLambda, NamedTypeSymbol), SynthesizedRefStructClosureTypeSymbol>(KeyComparer.Instance);
        private int _nameCounter;

        internal RefStructClosureTypeManager(CSharpCompilation compilation)
        {
            _compilation = compilation;
        }

        internal SynthesizedRefStructClosureTypeSymbol GetOrCreateClosureType(UnboundLambda lambda, NamedTypeSymbol functionInterface)
        {
            Debug.Assert(functionInterface.IsInterface);

            if (_cache.TryGetValue((lambda, functionInterface), out var existing))
            {
                return existing;
            }

            var interfaceInvoke = functionInterface.GetFunctionInterfaceInvokeMethod(_compilation);
            Debug.Assert(interfaceInvoke is { });

            var created = new SynthesizedRefStructClosureTypeSymbol(
                (SourceModuleSymbol)_compilation.SourceModule,
                GeneratedNames.MakeRefStructClosureTypeName(Interlocked.Increment(ref _nameCounter) - 1),
                functionInterface,
                interfaceInvoke!);

            return _cache.GetOrAdd((lambda, functionInterface), created);
        }

        private sealed class KeyComparer : IEqualityComparer<(UnboundLambda lambda, NamedTypeSymbol functionInterface)>
        {
            internal static readonly KeyComparer Instance = new KeyComparer();

            public bool Equals((UnboundLambda lambda, NamedTypeSymbol functionInterface) x, (UnboundLambda lambda, NamedTypeSymbol functionInterface) y)
                => ReferenceEquals(x.lambda, y.lambda) &&
                   x.functionInterface.Equals(y.functionInterface, TypeCompareKind.ConsiderEverything);

            public int GetHashCode((UnboundLambda lambda, NamedTypeSymbol functionInterface) obj)
                => Hash.Combine(
                    RuntimeHelpers.GetHashCode(obj.lambda),
                    obj.functionInterface.GetHashCode());
        }
    }
}
