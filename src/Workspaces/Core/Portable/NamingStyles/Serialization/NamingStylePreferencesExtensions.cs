// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static class NamingStylePreferencesExtensions
    {
        public static NamingStylePreferences PrependNamingStylePreferences(this NamingStylePreferences original, NamingStylePreferences newPreferences)
        {
            var symbolSpecifications = original.SymbolSpecifications.InsertRange(0, newPreferences.SymbolSpecifications);
            var namingStyles = original.NamingStyles.InsertRange(0, newPreferences.NamingStyles);
            var namingRules = original.NamingRules.InsertRange(0, newPreferences.NamingRules);
            return new NamingStylePreferences(symbolSpecifications, namingStyles, namingRules);
        }
    }
}
