// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly Document _document;
        private readonly Project _project;
        private readonly TextSpan _span;
        private readonly ImmutableArray<Diagnostic> _diagnostics;
        private readonly CancellationToken _cancellationToken;
        private readonly Action<CodeAction, ImmutableArray<Diagnostic>> _registerCodeFix;

        /// <summary>
        /// Document corresponding to the <see cref="CodeFixContext.Span"/> to fix.
        /// </summary>
        public Document Document { get { return _document; } }

        /// <summary>
        /// Project corresponding to the diagnostics to fix.
        /// </summary>
        internal Project Project { get { return _project; } }

        /// <summary>
        /// Text span within the <see cref="CodeFixContext.Document"/> to fix.
        /// </summary>
        public TextSpan Span { get { return _span; } }

        /// <summary>
        /// Diagnostics to fix.
        /// NOTE: All the diagnostics in this collection have the same <see cref="CodeFixContext.Span"/>.
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics { get { return _diagnostics; } }

        /// <summary>
        /// CancellationToken.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        /// <summary>
        /// Creates a code fix context to be passed into <see cref="CodeFixProvider.RegisterCodeFixesAsync(CodeFixContext)"/> method.
        /// </summary>
        /// <param name="document">Document to fix.</param>
        /// <param name="span">Text span within the <paramref name="document"/> to fix.</param>
        /// <param name="diagnostics">
        /// Diagnostics to fix.
        /// All the diagnostics must have the same <paramref name="span"/>.
        /// Additionally, the <see cref="Diagnostic.Id"/> of each diagnostic must be in the set of the <see cref="CodeFixProvider.FixableDiagnosticIds"/> of the associated <see cref="CodeFixProvider"/>.
        /// </param>
        /// <param name="registerCodeFix">Delegate to register a <see cref="CodeAction"/> fixing a subset of diagnostics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ArgumentNullException">Throws this exception if any of the arguments is null.</exception>
        /// <exception cref="ArgumentException">
        /// Throws this exception if the given <paramref name="diagnostics"/> is empty,
        /// has a null element or has an element whose span is not equal to <paramref name="span"/>.
        /// </exception>
        public CodeFixContext(
            Document document,
            TextSpan span,
            ImmutableArray<Diagnostic> diagnostics,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            CancellationToken cancellationToken)
            : this(document, span, diagnostics, registerCodeFix, verifyArguments: true, cancellationToken: cancellationToken)
        {
        }

        /// <summary>
        /// Creates a code fix context to be passed into <see cref="CodeFixProvider.RegisterCodeFixesAsync(CodeFixContext)"/> method.
        /// </summary>
        /// <param name="document">Document to fix.</param>
        /// <param name="diagnostic">
        /// Diagnostic to fix.
        /// The <see cref="Diagnostic.Id"/> of this diagnostic must be in the set of the <see cref="CodeFixProvider.FixableDiagnosticIds"/> of the associated <see cref="CodeFixProvider"/>.
        /// </param>
        /// <param name="registerCodeFix">Delegate to register a <see cref="CodeAction"/> fixing a subset of diagnostics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ArgumentNullException">Throws this exception if any of the arguments is null.</exception>
        public CodeFixContext(
            Document document,
            Diagnostic diagnostic,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            CancellationToken cancellationToken)
            : this(document, diagnostic.Location.SourceSpan, ImmutableArray.Create(diagnostic), registerCodeFix, verifyArguments: true, cancellationToken: cancellationToken)
        {
        }

        internal CodeFixContext(
            Document document,
            TextSpan span,
            ImmutableArray<Diagnostic> diagnostics,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            bool verifyArguments,
            CancellationToken cancellationToken)
            : this(document, document.Project, span, diagnostics, registerCodeFix, verifyArguments, cancellationToken)
        {
        }

        internal CodeFixContext(
            Project project,
            ImmutableArray<Diagnostic> diagnostics,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            CancellationToken cancellationToken)
            : this(document: null, project: project, span: default(TextSpan), diagnostics: diagnostics, registerCodeFix: registerCodeFix, verifyArguments: false, cancellationToken: cancellationToken)
        {
        }

        private CodeFixContext(
            Document document,
            Project project,
            TextSpan span,
            ImmutableArray<Diagnostic> diagnostics,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            bool verifyArguments,
            CancellationToken cancellationToken)
        {
            if (verifyArguments)
            {
                if (document == null)
                {
                    throw new ArgumentNullException(nameof(document));
                }

                if (registerCodeFix == null)
                {
                    throw new ArgumentNullException(nameof(registerCodeFix));
                }

                VerifyDiagnosticsArgument(diagnostics, span);
            }

            _document = document;
            _project = project;
            _span = span;
            _diagnostics = diagnostics;
            _registerCodeFix = registerCodeFix;
            _cancellationToken = cancellationToken;
        }

        internal CodeFixContext(
            Document document,
            Diagnostic diagnostic,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            bool verifyArguments,
            CancellationToken cancellationToken)
            : this(document, diagnostic.Location.SourceSpan, ImmutableArray.Create(diagnostic), registerCodeFix, verifyArguments, cancellationToken)
        {
        }

        /// <summary>
        /// Add supplied <paramref name="action"/> to the list of fixes that will be offered to the user.
        /// </summary>
        /// <param name="action">The <see cref="CodeAction"/> that will be invoked to apply the fix.</param>
        /// <param name="diagnostic">The subset of <see cref="Diagnostics"/> being addressed / fixed by the <paramref name="action"/>.</param>
        public void RegisterCodeFix(CodeAction action, Diagnostic diagnostic)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            _registerCodeFix(action, ImmutableArray.Create(diagnostic));
        }

        /// <summary>
        /// Add supplied <paramref name="action"/> to the list of fixes that will be offered to the user.
        /// </summary>
        /// <param name="action">The <see cref="CodeAction"/> that will be invoked to apply the fix.</param>
        /// <param name="diagnostics">The subset of <see cref="Diagnostics"/> being addressed / fixed by the <paramref name="action"/>.</param>
        public void RegisterCodeFix(CodeAction action, IEnumerable<Diagnostic> diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            RegisterCodeFix(action, diagnostics.ToImmutableArray());
        }

        /// <summary>
        /// Add supplied <paramref name="action"/> to the list of fixes that will be offered to the user.
        /// </summary>
        /// <param name="action">The <see cref="CodeAction"/> that will be invoked to apply the fix.</param>
        /// <param name="diagnostics">The subset of <see cref="Diagnostics"/> being addressed / fixed by the <paramref name="action"/>.</param>
        public void RegisterCodeFix(CodeAction action, ImmutableArray<Diagnostic> diagnostics)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            VerifyDiagnosticsArgument(diagnostics, _span);

            // TODO: 
            // - Check that all diagnostics are unique (no duplicates).
            // - Check that supplied diagnostics form subset of diagnostics originally
            //   passed to the provider via CodeFixContext.Diagnostics.

            _registerCodeFix(action, diagnostics);
        }

        private static void VerifyDiagnosticsArgument(ImmutableArray<Diagnostic> diagnostics, TextSpan span)
        {
            if (diagnostics.IsDefault)
            {
                throw new ArgumentException(nameof(diagnostics));
            }

            if (diagnostics.Length == 0)
            {
                throw new ArgumentException(WorkspacesResources.DiagnosticsCannotBeEmpty, nameof(diagnostics));
            }

            if (diagnostics.Any(d => d == null))
            {
                throw new ArgumentException(WorkspacesResources.DiagnosticCannotBeNull, nameof(diagnostics));
            }

            if (diagnostics.Any(d => d.Location.SourceSpan != span))
            {
                throw new ArgumentException(string.Format(WorkspacesResources.DiagnosticMustHaveMatchingSpan, span.ToString()), nameof(diagnostics));
            }
        }
    }
}
