// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal class SymbolMatchPriority
    {
        internal static int Keyword = 100;
        internal static int PreferType = 200;
        internal static int PreferNamedArgument = 300;
        internal static int PreferEventOrMethod = 400;
        internal static int PreferFieldOrProperty = 500;
        internal static int PreferLocalOrParameterOrRangeVariable = 600;
    }
}
