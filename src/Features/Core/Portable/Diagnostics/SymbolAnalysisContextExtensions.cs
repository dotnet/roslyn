// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static class SymbolAnalysisContextExtensions
    {
        public static async Task<NamingStylePreferencesInfo> GetNamingStylePreferencesAsync(this SymbolAnalysisContext context)
        {
            var location = context.Symbol.Locations.FirstOrDefault();
            if (location == null)
            {
                return null;
            }

            var cancellationToken = context.CancellationToken;
            var syntaxTree = location.SourceTree;
            var options = context.Options;
            var optionSet = await options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).ConfigureAwait(false);
            string language = context.Compilation.Language;
            var viewModel = optionSet.GetOption(SimplificationOptions.NamingPreferences, language);
            if (viewModel == null)
            {
                return null;
            }

            var preferences = viewModel.GetPreferencesInfo();
            return preferences;
        }
    }
}
