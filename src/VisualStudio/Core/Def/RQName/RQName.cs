// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Features.RQName;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices;

/// <summary>
/// Helpers related to <see cref="RQName"/>s. The resulting strings are suitable to pass as the pszRQName
/// arguments to methods in <see cref="IVsRefactorNotify"/> and <see cref="IVsSymbolicNavigationNotify"/>.
/// </summary>
public static class RQName
{
    /// <summary>
    /// Returns an RQName for the given symbol, or <see langword="null"/> if the symbol cannot be represented by an RQName.
    /// </summary>
    /// <param name="symbol">The symbol to build an RQName for.</param>
    /// <returns>A string suitable to pass as the pszRQName argument to methods in <see cref="IVsRefactorNotify"/>
    /// and <see cref="IVsSymbolicNavigationNotify"/>.</returns>
    public static string? From(ISymbol symbol)
        => RQNameInternal.From(symbol);
}
