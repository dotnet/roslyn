// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.SymReaderInterop;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class PdbHelpers
    {
        internal static void GetAllScopes(this ISymUnmanagedMethod method, ArrayBuilder<ISymUnmanagedScope> builder)
        {
            GetAllScopes(method, builder, offset: -1, isScopeEndInclusive: false);
        }

        internal static void GetAllScopes(this ISymUnmanagedMethod method, ArrayBuilder<ISymUnmanagedScope> builder, int offset, bool isScopeEndInclusive)
        {
            ISymUnmanagedScope scope = method.GetRootScope();
            GetAllScopes(scope, builder, offset, isScopeEndInclusive);
        }

        private static void GetAllScopes(ISymUnmanagedScope scope, ArrayBuilder<ISymUnmanagedScope> builder, int offset, bool isScopeEndInclusive)
        {
            builder.Add(scope);
            foreach (var nested in scope.GetScopes())
            {
                if ((offset < 0) || nested.IsInScope(offset, isScopeEndInclusive))
                {
                    GetAllScopes(nested, builder, offset, isScopeEndInclusive);
                    if (offset >= 0)
                    {
                        return;
                    }
                }
            }
        }
    }
}

