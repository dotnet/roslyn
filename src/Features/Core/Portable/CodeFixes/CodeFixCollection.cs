// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Represents a collection of <see cref="CodeFix"/>es supplied by a given fix provider
    /// (such as <see cref="CodeFixProvider"/> or <see cref="IConfigurationFixProvider"/>).
    /// </summary>
    internal class CodeFixCollection
    {
        public object Provider { get; }
        public TextSpan TextSpan { get; }
        public ImmutableArray<CodeFix> Fixes { get; }

        /// <summary>
        /// Optional fix all context, which is non-null if the given <see cref="Provider"/> supports fix all occurrences code fix.
        /// </summary>
        public FixAllState FixAllState { get; }
        public ImmutableArray<FixAllScope> SupportedScopes { get; }
        public Diagnostic FirstDiagnostic { get; }

        public CodeFixCollection(
            object provider,
            TextSpan span,
            ImmutableArray<CodeFix> fixes,
            FixAllState fixAllState,
            ImmutableArray<FixAllScope> supportedScopes,
            Diagnostic firstDiagnostic)
        {
            Provider = provider;
            TextSpan = span;
            Fixes = fixes.NullToEmpty();
            FixAllState = fixAllState;
            SupportedScopes = supportedScopes.NullToEmpty();
            FirstDiagnostic = firstDiagnostic;
        }
    }
}
