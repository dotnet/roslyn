// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.SymReaderInterop;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class PdbHelpers
    {
        /// <remarks>
        /// Test helper.
        /// </remarks>
        internal static void GetAllScopes(this ISymUnmanagedMethod method, ArrayBuilder<ISymUnmanagedScope> builder)
        {
            var unused = ArrayBuilder<ISymUnmanagedScope>.GetInstance();
            GetAllScopes(method, builder, unused, offset: -1, isScopeEndInclusive: false);
            unused.Free();
        }

        internal static void GetAllScopes(
            this ISymUnmanagedMethod method, 
            ArrayBuilder<ISymUnmanagedScope> allScopes, 
            ArrayBuilder<ISymUnmanagedScope> containingScopes, 
            int offset, 
            bool isScopeEndInclusive)
        {
            GetAllScopes(method.GetRootScope(), allScopes, containingScopes, offset, isScopeEndInclusive);
        }

        private static void GetAllScopes(
            ISymUnmanagedScope root, 
            ArrayBuilder<ISymUnmanagedScope> allScopes, 
            ArrayBuilder<ISymUnmanagedScope> containingScopes, 
            int offset, 
            bool isScopeEndInclusive)
        {
            var stack = ArrayBuilder<ISymUnmanagedScope>.GetInstance();
            stack.Push(root);

            while (stack.Any())
            {
                var scope = stack.Pop();
                allScopes.Add(scope);
                if (offset >= 0 && scope.IsInScope(offset, isScopeEndInclusive))
                {
                    containingScopes.Add(scope);
                }

                foreach (var nested in scope.GetScopes())
                {
                    stack.Push(nested);
                }
            }
        }
    }
}

