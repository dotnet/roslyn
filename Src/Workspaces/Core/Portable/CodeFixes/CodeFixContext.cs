// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for code fixes provided by an <see cref="CodeFixProvider"/>.
    /// </summary>
    public struct CodeFixContext
    {
        private readonly Document document;
        private readonly TextSpan span;
        private readonly Diagnostic diagnostic;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// Document corresponding to the <see cref="CodeFixContext.Span"/> to fix.
        /// </summary>
        public Document Document { get { return this.document; } }

        /// <summary>
        /// Text span within the <see cref="CodeFixContext.Document"/> to fix.
        /// </summary>
        public TextSpan Span { get { return this.span; } }

        /// <summary>
        /// Diagnostic to fix.
        /// </summary>
        public Diagnostic Diagnostic { get { return this.diagnostic; } }

        /// <summary>
        /// CancellationToken.
        /// </summary>
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        /// <summary>
        /// Creates a code fix context to be passed into <see cref="CodeFixProvider.GetFixesAsync(CodeFixContext)"/> method.
        /// </summary>
        public CodeFixContext(
            Document document,
            TextSpan span,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            this.document = document;
            this.span = span;
            this.diagnostic = diagnostic;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Creates a code fix context to be passed into <see cref="CodeFixProvider.GetFixesAsync(CodeFixContext)"/> method.
        /// </summary>
        public CodeFixContext(
            Document document,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
            : this(document, diagnostic.Location.SourceSpan, diagnostic, cancellationToken)
        {
        }
    }
}