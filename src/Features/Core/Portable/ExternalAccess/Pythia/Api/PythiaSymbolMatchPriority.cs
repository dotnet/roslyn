// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion.Providers;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;

internal static class PythiaSymbolMatchPriority
{
    internal static readonly int Keyword = SymbolMatchPriority.Keyword;
    internal static readonly int PreferType = SymbolMatchPriority.PreferType;
    internal static readonly int PreferNamedArgument = SymbolMatchPriority.PreferNamedArgument;
    internal static readonly int PreferEventOrMethod = SymbolMatchPriority.PreferEventOrMethod;
    internal static readonly int PreferFieldOrProperty = SymbolMatchPriority.PreferFieldOrProperty;
    internal static readonly int PreferLocalOrParameterOrRangeVariable = SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable;
}
