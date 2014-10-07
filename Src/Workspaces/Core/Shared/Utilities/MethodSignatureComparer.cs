#if false
using System;
using System.Collections.Generic;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.Utilities
{
    internal class MethodSignatureComparer
        : IEqualityComparer<IMethodSymbol>
    {
        public static readonly MethodSignatureComparer Instance = new MethodSignatureComparer(assembliesCanDiffer: false);
        public static readonly MethodSignatureComparer IgnoreAssembliesInstance = new MethodSignatureComparer(assembliesCanDiffer: true);

        private readonly SymbolEquivalenceComparer symbolEqualityComparer;

        public MethodSignatureComparer(bool assembliesCanDiffer)
        {
            this.symbolEqualityComparer = assembliesCanDiffer
                ? SymbolEquivalenceComparer.IgnoreAssembliesInstance
                : SymbolEquivalenceComparer.Instance;
        }

        public bool Equals(IMethodSymbol method1, IMethodSymbol method2)
        {
            if (method1 == method2)
            {
                return true;
            }

            if (method1 == null || method2 == null)
            {
                return false;
            }

            if (method1.DeclaredAccessibility != method2.DeclaredAccessibility ||
                method1.IsStatic != method2.IsStatic ||
                method1.TypeParameters.Count != method2.TypeParameters.Count ||
                method1.Parameters.Count != method2.Parameters.Count ||
                method1.ReturnsVoid != method2.ReturnsVoid ||
                method1.Name != method2.Name)
            {
                return false;
            }

            if (!method1.ReturnsVoid &&
                !this.symbolEqualityComparer.SignatureTypeEquivalenceComparer.Equals(method1.ReturnType, method2.ReturnType))
            {
                return false;
            }

            if (!HaveSameParameterTypes(method1, method2))
            {
                return false;
            }

            if (!HaveSameConstraints(method1, method2))
            {
                return false;
            }

            return true;
        }

        private bool HaveSameParameterTypes(IMethodSymbol method1, IMethodSymbol method2)
        {
            var parameters1 = method1.Parameters;
            var parameters2 = method2.Parameters;

            for (var i = 0; i < parameters1.Count; i++)
            {
                if (!this.symbolEqualityComparer.ParameterEquivalenceComparer.Equals(parameters1[i], parameters2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool HaveSameConstraints(IMethodSymbol method1, IMethodSymbol method2)
        {
            // TODO(cyrusn): implement this.
            return true;
        }

        public int GetHashCode(IMethodSymbol obj)
        {
            throw new NotImplementedException();
        }
    }
}
#endif