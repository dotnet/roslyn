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
        public FixAllCodeActionContext FixAllContext { get; }

        public CodeFixCollection(object provider, TextSpan span, IEnumerable<CodeFix> fixes, FixAllCodeActionContext fixAllContext = null) :
            this(provider, span, fixes.ToImmutableArray(), fixAllContext)
        {
        }

        public CodeFixCollection(object provider, TextSpan span, ImmutableArray<CodeFix> fixes, FixAllCodeActionContext fixAllContext = null)
        {
            this.Provider = provider;
            this.TextSpan = span;
            this.Fixes = fixes;
            this.FixAllContext = fixAllContext;
        }
    }
}
