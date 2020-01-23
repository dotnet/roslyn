// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#pragma warning disable CA1802 // Use literals where appropriate - if any of these are used by an assembly that has IVT it would be breaking to change to constant

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class SymbolMatchPriority
    {
        internal readonly static int Keyword = 100;
        internal readonly static int PreferType = 200;
        internal readonly static int PreferNamedArgument = 300;
        internal readonly static int PreferEventOrMethod = 400;
        internal readonly static int PreferFieldOrProperty = 500;
        internal readonly static int PreferLocalOrParameterOrRangeVariable = 600;
    }
}
