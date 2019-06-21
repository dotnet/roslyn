// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Contains computed information for a given <see cref="CodeFixes.FixAllProvider"/>, such as supported diagnostic Ids and supported <see cref="FixAllScope"/>.
    /// </summary>
    internal abstract class FixAllProviderInfo
    {
        public readonly FixAllProvider FixAllProvider;
        public readonly ImmutableArray<FixAllScope> SupportedScopes;

        private FixAllProviderInfo(
            FixAllProvider fixAllProvider,
            ImmutableArray<FixAllScope> supportedScopes)
        {
            FixAllProvider = fixAllProvider;
            SupportedScopes = supportedScopes;
        }

        /// <summary>
        /// Gets an optional <see cref="FixAllProviderInfo"/> for the given code fix provider or suppression fix provider.
        /// </summary>
        public static FixAllProviderInfo Create(object provider)
        {
            if (provider is CodeFixProvider codeFixProvider)
            {
                return CreateWithCodeFixer(codeFixProvider);
            }

            return CreateWithSuppressionFixer((IConfigurationFixProvider)provider);
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

            var scopes = fixAllProvider.GetSupportedFixAllScopes().ToImmutableArrayOrEmpty();
            if (scopes.IsEmpty)
            {
                return null;
            }

            return new CodeFixerFixAllProviderInfo(fixAllProvider, diagnosticIds, scopes);
        }

        /// <summary>
        /// Gets an optional <see cref="FixAllProviderInfo"/> for the given suppression fix provider.
        /// </summary>
        private static FixAllProviderInfo CreateWithSuppressionFixer(IConfigurationFixProvider provider)
        {
            var fixAllProvider = provider.GetFixAllProvider();
            if (fixAllProvider == null)
            {
                return null;
            }

            var scopes = fixAllProvider.GetSupportedFixAllScopes().ToImmutableArrayOrEmpty();
            if (scopes.IsEmpty)
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
                ImmutableArray<FixAllScope> supportedScopes)
                : base(fixAllProvider, supportedScopes)
            {
                _supportedDiagnosticIds = supportedDiagnosticIds;
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
                IConfigurationFixProvider suppressionFixer,
                ImmutableArray<FixAllScope> supportedScopes)
                : base(fixAllProvider, supportedScopes)
            {
                _canBeSuppressedOrUnsuppressed = suppressionFixer.IsFixableDiagnostic;
            }

            public override bool CanBeFixed(Diagnostic diagnostic)
            {
                return _canBeSuppressedOrUnsuppressed(diagnostic);
            }
        }
    }
}
