// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1802 // Use literals where appropriate - if any of these are used by an assembly that has IVT it would be breaking to change to constant

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal static class SymbolMatchPriority
{
    internal static readonly int Keyword = 100;
    internal static readonly int PreferType = 200;
    internal static readonly int PreferNamedArgument = 300;
    internal static readonly int PreferEventOrMethod = 400;
    internal static readonly int PreferFieldOrProperty = 500;
    internal static readonly int PreferLocalOrParameterOrRangeVariable = 600;
}
