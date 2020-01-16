// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
