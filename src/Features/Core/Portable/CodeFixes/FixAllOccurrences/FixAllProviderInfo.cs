// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Contains computed information for a given <see cref="CodeFixes.FixAllProvider"/>, such as supported diagnostic Ids and supported <see cref="FixAllScope"/>.
    /// </summary>
    internal abstract class FixAllProviderInfo
    {
        public readonly FixAllProvider FixAllProvider;
        public readonly IEnumerable<FixAllScope> SupportedScopes;

        private FixAllProviderInfo(
            FixAllProvider fixAllProvider,
            IEnumerable<FixAllScope> supportedScopes)
        {
            this.FixAllProvider = fixAllProvider;
            this.SupportedScopes = supportedScopes;
        }

        /// <summary>
        /// Gets an optional <see cref="FixAllProviderInfo"/> for the given code fix provider or suppression fix provider.
        /// </summary>
        public static FixAllProviderInfo Create(object provider)
        {
            var codeFixProvider = provider as CodeFixProvider;
            if (codeFixProvider != null)
            {
                return CreateWithCodeFixer(codeFixProvider);
            }

            return CreateWithSuppressionFixer((ISuppressionFixProvider)provider);
        }

        /// <summary>
        /// Gets an optional <see cref="FixAllProviderInfo"/> for the given code fix provider.
        /// </summary>
        private static FixAllProviderInfo CreateWithCodeFixer(CodeFixProvider provider)
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

            return new CodeFixerFixAllProviderInfo(fixAllProvider, diagnosticIds, scopes);
        }

        /// <summary>
        /// Gets an optional <see cref="FixAllProviderInfo"/> for the given suppression fix provider.
        /// </summary>
        private static FixAllProviderInfo CreateWithSuppressionFixer(ISuppressionFixProvider provider)
        {
            var fixAllProvider = provider.GetFixAllProvider();
            if (fixAllProvider == null)
            {
                return null;
            }

            var scopes = fixAllProvider.GetSupportedFixAllScopes();
            if (scopes == null || scopes.IsEmpty())
            {
                return null;
            }

            return new SuppressionFixerFixAllProviderInfo(fixAllProvider, provider, scopes);
        }

        public abstract bool CanBeFixed(Diagnostic diagnostic);

        private class CodeFixerFixAllProviderInfo : FixAllProviderInfo
        {
            private readonly IEnumerable<string> _supportedDiagnosticIds;

            public CodeFixerFixAllProviderInfo(
                FixAllProvider fixAllProvider,
                IEnumerable<string> supportedDiagnosticIds,
                IEnumerable<FixAllScope> supportedScopes)
                : base (fixAllProvider, supportedScopes)
            {
                this._supportedDiagnosticIds = supportedDiagnosticIds;
            }

            public override bool CanBeFixed(Diagnostic diagnostic)
            {
                return _supportedDiagnosticIds.Contains(diagnostic.Id);
            }
        }

        private class SuppressionFixerFixAllProviderInfo : FixAllProviderInfo
        {
            private readonly Func<Diagnostic, bool> _canBeSuppressedOrUnsuppressed;

            public SuppressionFixerFixAllProviderInfo(
                FixAllProvider fixAllProvider,
                ISuppressionFixProvider suppressionFixer,
                IEnumerable<FixAllScope> supportedScopes)
                : base(fixAllProvider, supportedScopes)
            {
                this._canBeSuppressedOrUnsuppressed = suppressionFixer.CanBeSuppressedOrUnsuppressed;
            }

            public override bool CanBeFixed(Diagnostic diagnostic)
            {
                return _canBeSuppressedOrUnsuppressed(diagnostic);
            }
        }
    }
}
