// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Contains computed information specific to FixAll for a given <see cref="CodeRefactoringProvider"/>, such as supported <see cref="FixAllScope"/>s.
    /// </summary>
    internal sealed class FixAllProviderInfo
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
        /// Gets an optional <see cref="FixAllProviderInfo"/> for the given code refactoring provider.
        /// </summary>
        public static FixAllProviderInfo? Create(CodeRefactoringProvider provider)
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

            return new FixAllProviderInfo(fixAllProvider, scopes);
        }
    }
}
