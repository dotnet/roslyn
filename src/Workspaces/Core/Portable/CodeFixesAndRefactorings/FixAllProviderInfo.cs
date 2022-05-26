// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings
{
    /// <summary>
    /// Contains computed information for a given <see cref="FixAllProvider"/>, such as supported diagnostic Ids and supported <see cref="FixAllScope"/>.
    /// </summary>
    internal abstract class FixAllProviderInfo
    {
        public readonly IFixAllProvider FixAllProvider;
        public readonly ImmutableArray<FixAllScope> SupportedScopes;

        private FixAllProviderInfo(
            IFixAllProvider fixAllProvider,
            ImmutableArray<FixAllScope> supportedScopes)
        {
            FixAllProvider = fixAllProvider;
            SupportedScopes = supportedScopes;
        }

        /// <summary>
        /// Gets an optional <see cref="FixAllProviderInfo"/> for the given code fix provider or suppression fix provider.
        /// </summary>
        public static FixAllProviderInfo? Create(object provider)
        {
            if (provider is CodeFixProvider codeFixProvider)
            {
                return CreateWithCodeFixer(codeFixProvider);
            }
            else if (provider is CodeRefactoringProvider codeRefactoringProvider)
            {
                return CreateWithCodeRefactoring(codeRefactoringProvider);
            }

            return CreateWithSuppressionFixer((IConfigurationFixProvider)provider);
        }

        /// <summary>
        /// Gets an optional <see cref="FixAllProviderInfo"/> for the given code fix provider.
        /// </summary>
        private static FixAllProviderInfo? CreateWithCodeFixer(CodeFixProvider provider)
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
        /// Gets an optional <see cref="FixAllProviderInfo"/> for the given code refactoring provider.
        /// </summary>
        private static FixAllProviderInfo? CreateWithCodeRefactoring(CodeRefactoringProvider provider)
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

            return new CodeRefactoringFixAllProviderInfo(fixAllProvider, scopes);
        }

        /// <summary>
        /// Gets an optional <see cref="FixAllProviderInfo"/> for the given suppression fix provider.
        /// </summary>
        private static FixAllProviderInfo? CreateWithSuppressionFixer(IConfigurationFixProvider provider)
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
                IFixAllProvider fixAllProvider,
                IEnumerable<string> supportedDiagnosticIds,
                ImmutableArray<FixAllScope> supportedScopes)
                : base(fixAllProvider, supportedScopes)
            {
                _supportedDiagnosticIds = supportedDiagnosticIds;
            }

            public override bool CanBeFixed(Diagnostic diagnostic)
                => _supportedDiagnosticIds.Contains(diagnostic.Id);
        }

        private class SuppressionFixerFixAllProviderInfo : FixAllProviderInfo
        {
            private readonly Func<Diagnostic, bool> _canBeSuppressedOrUnsuppressed;

            public SuppressionFixerFixAllProviderInfo(
                IFixAllProvider fixAllProvider,
                IConfigurationFixProvider suppressionFixer,
                ImmutableArray<FixAllScope> supportedScopes)
                : base(fixAllProvider, supportedScopes)
            {
                _canBeSuppressedOrUnsuppressed = suppressionFixer.IsFixableDiagnostic;
            }

            public override bool CanBeFixed(Diagnostic diagnostic)
                => _canBeSuppressedOrUnsuppressed(diagnostic);
        }

        private class CodeRefactoringFixAllProviderInfo : FixAllProviderInfo
        {
            public CodeRefactoringFixAllProviderInfo(
                IFixAllProvider fixAllProvider,
                ImmutableArray<FixAllScope> supportedScopes)
                : base(fixAllProvider, supportedScopes)
            {
            }

            public override bool CanBeFixed(Diagnostic diagnostic)
                => throw ExceptionUtilities.Unreachable;
        }
    }
}
