// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal enum DeclarationScope : byte
    {
        Unscoped = 0,
        RefScoped = 1,
        ValueScoped = 2,
    }

    internal static class DeclarationScopeExtensions
    {
        internal static ScopedKind AsScopedKind(this DeclarationScope scope)
        {
            return scope switch
            {
                DeclarationScope.Unscoped => ScopedKind.None,
                DeclarationScope.RefScoped => ScopedKind.ScopedRef,
                DeclarationScope.ValueScoped => ScopedKind.ScopedValue,
                _ => throw ExceptionUtilities.UnexpectedValue(scope),
            };
        }
    }
}
