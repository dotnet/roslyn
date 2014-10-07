using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.Extensions
{
    internal static class IEventSymbolExtensions
    {
        [ExcludeFromCodeCoverage]
        [Obsolete]
        public static IEnumerable<IEventSymbol> GetOverrides(
            this IEventSymbol symbol,
            ISolution solution,
            CancellationToken cancellationToken)
        {
            return ISymbolExtensions.GetOverrides(symbol, solution, cancellationToken).OfType<IEventSymbol>();
        }

        [ExcludeFromCodeCoverage]
        [Obsolete]
        public static IEnumerable<IEventSymbol> GetImplementations(
            this IEventSymbol symbol,
            ISolution solution,
            CancellationToken cancellationToken)
        {
            return ISymbolExtensions.GetImplementations(symbol, solution, cancellationToken).OfType<IEventSymbol>();
        }

        [ExcludeFromCodeCoverage]
        [Obsolete]
        public static IEnumerable<IEventSymbol> GetImplementedInterfaceMembers(
            this IEventSymbol symbol,
            ISolution solution,
            CancellationToken cancellationToken)
        {
            return ISymbolExtensions.GetImplementedInterfaceMembers(symbol, solution, cancellationToken).OfType<IEventSymbol>();
        }
    }
}