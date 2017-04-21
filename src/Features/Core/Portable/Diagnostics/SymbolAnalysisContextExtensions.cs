// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static class SymbolAnalysisContextExtensions
    {
        public static async Task<NamingStylePreferences> GetNamingStylePreferencesAsync(
            this SymbolAnalysisContext context)
        {
            var location = context.Symbol.Locations.FirstOrDefault();
            if (location == null)
            {
                return null;
            }

            var optionSet = await context.Options.GetDocumentOptionSetAsync(location.SourceTree, context.CancellationToken).ConfigureAwait(false);
            return optionSet?.GetOption(SimplificationOptions.NamingPreferences, context.Compilation.Language);
        }
    }
}
