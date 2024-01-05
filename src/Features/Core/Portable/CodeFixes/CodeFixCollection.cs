﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Represents a collection of <see cref="CodeFix"/>es supplied by a given fix provider
    /// (such as <see cref="CodeFixProvider"/> or <see cref="IConfigurationFixProvider"/>).
    /// </summary>
    internal class CodeFixCollection(
        object provider,
        TextSpan span,
        ImmutableArray<CodeFix> fixes,
        FixAllState fixAllState,
        ImmutableArray<FixAllScope> supportedScopes,
        Diagnostic firstDiagnostic)
    {
        public object Provider { get; } = provider;
        public TextSpan TextSpan { get; } = span;
        public ImmutableArray<CodeFix> Fixes { get; } = fixes.NullToEmpty();

        /// <summary>
        /// Optional fix all context, which is non-null if the given <see cref="Provider"/> supports fix all occurrences code fix.
        /// </summary>
        public FixAllState FixAllState { get; } = fixAllState;
        public ImmutableArray<FixAllScope> SupportedScopes { get; } = supportedScopes.NullToEmpty();
        public Diagnostic FirstDiagnostic { get; } = firstDiagnostic;
    }
}
