// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Represents a collection of <see cref="CodeFix"/>es supplied by a given fix provider
    /// (such as <see cref="CodeFixProvider"/> or <see cref="ISuppressionFixProvider"/>).
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
        public IEnumerable<FixAllScope> SupportedScopes { get; }
        public Diagnostic FirstDiagnostic { get; }

        public CodeFixCollection(
            object provider,
            TextSpan span,
            IEnumerable<CodeFix> fixes,
            FixAllState fixAllState,
            IEnumerable<FixAllScope> supportedScopes,
            Diagnostic firstDiagnostic) :
            this(provider, span, fixes.ToImmutableArray(), fixAllState, supportedScopes, firstDiagnostic)
        {
        }

        public CodeFixCollection(
            object provider,
            TextSpan span,
            ImmutableArray<CodeFix> fixes,
            FixAllState fixAllState,
            IEnumerable<FixAllScope> supportedScopes,
            Diagnostic firstDiagnostic)
        {
            this.Provider = provider;
            this.TextSpan = span;
            this.Fixes = fixes;
            this.FixAllState = fixAllState;
            this.SupportedScopes = supportedScopes;
            this.FirstDiagnostic = firstDiagnostic;
        }
    }
}
