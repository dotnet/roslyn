// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for code fixes provided by an <see cref="CodeFixProvider"/>.
    /// </summary>
    public struct CodeFixContext
    {
        private readonly Document document;
        private readonly TextSpan span;
        private readonly IEnumerable<Diagnostic> diagnostics;
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
        /// Diagnostics to fix.
        /// </summary>
        public IEnumerable<Diagnostic> Diagnostics { get { return this.diagnostics; } }

        private readonly Action<CodeAction, IEnumerable<Diagnostic>> registerFix;

        /// <summary>
        /// CancellationToken.
        /// </summary>
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        /// <summary>
        /// Creates a code fix context to be passed into <see cref="CodeFixProvider.ComputeFixesAsync(CodeFixContext)"/> method.
        /// </summary>
        public CodeFixContext(
            Document document,
            TextSpan span,
            IEnumerable<Diagnostic> diagnostics,
            Action<CodeAction, IEnumerable<Diagnostic>> registerFix,
            CancellationToken cancellationToken)
        {
            this.document = document;
            this.span = span;
            this.diagnostics = diagnostics;
            this.registerFix = registerFix;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Creates a code fix context to be passed into <see cref="CodeFixProvider.ComputeFixesAsync(CodeFixContext)"/> method.
        /// </summary>
        public CodeFixContext(
            Document document,
            Diagnostic diagnostic,
            Action<CodeAction, IEnumerable<Diagnostic>> registerFix,
            CancellationToken cancellationToken)
            : this(document, diagnostic.Location.SourceSpan, SpecializedCollections.SingletonEnumerable(diagnostic), registerFix, cancellationToken)
        {
        }

        /// <summary>
        /// Add supplied <paramref name="action"/> to the list of fixes that will be offered to the user.
        /// </summary>
        /// <param name="action">The <see cref="CodeAction"/> that will be invoked to apply the fix.</param>
        /// <param name="diagnostic">The subset of <see cref="Diagnostics"/> being addressed / fixed by the <paramref name="action"/>.</param>
        public void RegisterFix(CodeAction action, Diagnostic diagnostic)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            this.registerFix(action, ImmutableArray.Create(diagnostic));
        }

        /// <summary>
        /// Add supplied <paramref name="action"/> to the list of fixes that will be offered to the user.
        /// </summary>
        /// <param name="action">The <see cref="CodeAction"/> that will be invoked to apply the fix.</param>
        /// <param name="diagnostics">The subset of <see cref="Diagnostics"/> being addressed / fixed by the <paramref name="action"/>.</param>
        public void RegisterFix(CodeAction action, IEnumerable<Diagnostic> diagnostics)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            var diagnosticsArray = diagnostics.ToImmutableArray();
            if (diagnosticsArray.Length == 0)
            {
                throw new ArgumentException(WorkspacesResources.DiagnosticsCannotBeEmpty, nameof(diagnostics));
            }

            if (diagnosticsArray.Any(d => d == null))
            {
                throw new ArgumentException(WorkspacesResources.DiagnoisticCannotBeNull, nameof(diagnostics));
            }

            // TODO: 
            // - Check that all diagnostics are unique (no duplicates).
            // - Check that supplied diagnostics form subset of diagnostics originally
            //   passed to the provider via CodeFixContext.Diagnostics.

            this.registerFix(action, diagnosticsArray);
        }
    }
}