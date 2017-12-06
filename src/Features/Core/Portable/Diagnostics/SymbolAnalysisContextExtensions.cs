// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static class SymbolAnalysisContextExtensions
    {
        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public static async ValueTask<NamingStylePreferences> GetNamingStylePreferencesAsync(
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
