// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

internal static class CopilotUtilities
{
    public static bool IsResultantVisibilityPublic(this ISymbol symbol)
    {
        return symbol.GetResultantVisibility() == Shared.Utilities.SymbolVisibility.Public;
    }

    public static bool IsValidIdentifier([NotNullWhen(returnValue: true)] string? name)
    {
        return UnicodeCharacterUtilities.IsValidIdentifier(name);
    }
}
