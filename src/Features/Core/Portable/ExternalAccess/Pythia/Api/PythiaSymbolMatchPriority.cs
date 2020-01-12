// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Completion.Providers;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaSymbolMatchPriority
    {
        internal static int Keyword = SymbolMatchPriority.Keyword;
        internal static int PreferType = SymbolMatchPriority.PreferType;
        internal static int PreferNamedArgument = SymbolMatchPriority.PreferNamedArgument;
        internal static int PreferEventOrMethod = SymbolMatchPriority.PreferEventOrMethod;
        internal static int PreferFieldOrProperty = SymbolMatchPriority.PreferFieldOrProperty;
        internal static int PreferLocalOrParameterOrRangeVariable = SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable;
    }
}
