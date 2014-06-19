using System.Collections.Generic;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// This comparer is not for comparing signatures; it is used to determine whether two explicitly
    /// implemented methods are the same.
    /// </summary>
    internal class ExplicitInterfaceMethodComparer : IEqualityComparer<MethodSymbol>
    {
        public static readonly IEqualityComparer<MethodSymbol> Instance = new ExplicitInterfaceMethodComparer();

        private ExplicitInterfaceMethodComparer()
        {
        }

        #region IEqualityComparer<MethodSymbol> Members

        bool IEqualityComparer<MethodSymbol>.Equals(MethodSymbol method1, MethodSymbol method2)
        {
            if (method1 == null)
            {
                return method2 == null;
            }
            else if (method2 == null)
            {
                return false;
            }

            if (ReferenceEquals(method1, method2))
            {
                return true;
            }

            //we use this slightly roundabout check to account for substituted methods
            return method1.ContainingType == method2.ContainingType && method1.OriginalDefinition == method2.OriginalDefinition;
        }

        int IEqualityComparer<MethodSymbol>.GetHashCode(MethodSymbol method)
        {
            int hash = 1;
            if (method != null)
            {
                hash = HashFunctions.CombineHashKey(hash, method.ContainingType);
                hash = HashFunctions.CombineHashKey(hash, method.OriginalDefinition);
            }
            return hash;
        }

        #endregion
    }
}