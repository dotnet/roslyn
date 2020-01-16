// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Completion.Providers;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaSymbolMatchPriority
    {
        internal static readonly int Keyword = SymbolMatchPriority.Keyword;
        internal static readonly int PreferType = SymbolMatchPriority.PreferType;
        internal static readonly int PreferNamedArgument = SymbolMatchPriority.PreferNamedArgument;
        internal static readonly int PreferEventOrMethod = SymbolMatchPriority.PreferEventOrMethod;
        internal static readonly int PreferFieldOrProperty = SymbolMatchPriority.PreferFieldOrProperty;
        internal static readonly int PreferLocalOrParameterOrRangeVariable = SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable;
    }
}
