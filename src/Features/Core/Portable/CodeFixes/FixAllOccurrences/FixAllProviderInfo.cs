// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Contains computed information for a given <see cref="CodeFixes.FixAllProvider"/>, such as supported diagnostic Ids and supported <see cref="FixAllScope"/>.
    /// </summary>
    internal class FixAllProviderInfo
    {
        public readonly FixAllProvider FixAllProvider;
        public readonly IEnumerable<string> SupportedDiagnosticIds;
        public readonly IEnumerable<FixAllScope> SupportedScopes;

        private FixAllProviderInfo(
            FixAllProvider fixAllProvider,
            IEnumerable<string> supportedDiagnosticIds,
            IEnumerable<FixAllScope> supportedScopes)
        {
            this.FixAllProvider = fixAllProvider;
            this.SupportedDiagnosticIds = supportedDiagnosticIds;
            this.SupportedScopes = supportedScopes;
        }

        /// <summary>
        /// Gets an optional <see cref="FixAllProviderInfo"/> for the given code fix provider.
        /// </summary>
        public static FixAllProviderInfo Create(CodeFixProvider provider)
        {
            var fixAllProvider = provider.GetFixAllProvider();
            if (fixAllProvider == null)
            {
                return null;
            }

            var diagnosticIds = fixAllProvider.GetSupportedFixAllDiagnosticIds(provider);
            if (diagnosticIds == null || diagnosticIds.IsEmpty())
            {
                return null;
            }

            var scopes = fixAllProvider.GetSupportedFixAllScopes();
            if (scopes == null || scopes.IsEmpty())
            {
                return null;
            }

            return new FixAllProviderInfo(fixAllProvider, diagnosticIds, scopes);
        }
    }
}
