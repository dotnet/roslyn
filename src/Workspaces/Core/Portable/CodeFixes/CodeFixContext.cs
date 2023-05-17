// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for code fixes provided by a <see cref="CodeFixProvider"/>.
    /// </summary>
    public readonly struct CodeFixContext
    {
        private readonly TextDocument _document;
        private readonly TextSpan _span;
        private readonly ImmutableArray<Diagnostic> _diagnostics;
        private readonly CancellationToken _cancellationToken;
        private readonly Action<CodeAction, ImmutableArray<Diagnostic>> _registerCodeFix;

        /// <summary>
        /// Document corresponding to the <see cref="CodeFixContext.Span"/> to fix.
        /// For code fixes that support non-source documents by providing a non-default value for
        /// <see cref="ExportCodeFixProviderAttribute.DocumentKinds"/>, this property will
        /// throw an <see cref="InvalidOperationException"/>. Such fixers should use the
        /// <see cref="CodeFixContext.TextDocument"/> property instead.
        /// </summary>
        public Document Document
        {
            get
            {
                if (TextDocument is not Document document)
                {
                    throw new InvalidOperationException(WorkspacesResources.Use_TextDocument_property_instead_of_Document_property_as_the_provider_supports_non_source_text_documents);
                }

                return document;
            }
        }

        /// <summary>
        /// TextDocument corresponding to the <see cref="Span"/> to fix.
        /// This property should be used instead of <see cref="Document"/> property by
        /// code fixes that support non-source documents by providing a non-default value for
        /// <see cref="ExportCodeFixProviderAttribute.DocumentKinds"/>
        /// </summary>
        public TextDocument TextDocument => _document;

        /// <summary>
        /// Text span within the <see cref="Document"/> or <see cref="TextDocument"/> to fix.
        /// </summary>
        public TextSpan Span => _span;

        /// <summary>
        /// Diagnostics to fix.
        /// NOTE: All the diagnostics in this collection have the same <see cref="Span"/>.
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// CancellationToken.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationToken;

        /// <summary>
        /// IDE supplied options to use for settings not specified in the corresponding editorconfig file.
        /// These are not available in Code Style layer. Use <see cref="CodeActionOptionsProviders.GetOptionsProvider(CodeFixContext)"/> extension method 
        /// to access these options in code shared with Code Style layer.
        /// </summary>
        /// <remarks>
        /// This is a <see cref="CodeActionOptionsProvider"/> (rather than <see cref="CodeActionOptions"/> directly)
        /// to allow code fix to update documents across multiple projects that differ in language (and hence language specific options).
        /// </remarks>
        internal readonly CodeActionOptionsProvider Options;

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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CodeFixContext(
            Document document,
            TextSpan span,
            ImmutableArray<Diagnostic> diagnostics,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            CancellationToken cancellationToken)
            : this(document,
                   span,
                   diagnostics,
                   registerCodeFix,
                   CodeActionOptions.DefaultProvider,
                   cancellationToken)
        {
        }

        /// <summary>
        /// Creates a code fix context to be passed into <see cref="CodeFixProvider.RegisterCodeFixesAsync(CodeFixContext)"/> method.
        /// </summary>
        /// <param name="document">Text document to fix.</param>
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
            TextDocument document,
            TextSpan span,
            ImmutableArray<Diagnostic> diagnostics,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            CancellationToken cancellationToken)
            : this(document,
                   span,
                   diagnostics,
                   registerCodeFix,
                   CodeActionOptions.DefaultProvider,
                   cancellationToken)
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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CodeFixContext(
            Document document,
            Diagnostic diagnostic,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            CancellationToken cancellationToken)
            : this(document,
                   (diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))).Location.SourceSpan,
                   ImmutableArray.Create(diagnostic),
                   registerCodeFix,
                   CodeActionOptions.DefaultProvider,
                   cancellationToken)
        {
        }

        /// <summary>
        /// Creates a code fix context to be passed into <see cref="CodeFixProvider.RegisterCodeFixesAsync(CodeFixContext)"/> method.
        /// </summary>
        /// <param name="document">Text document to fix.</param>
        /// <param name="diagnostic">
        /// Diagnostic to fix.
        /// The <see cref="Diagnostic.Id"/> of this diagnostic must be in the set of the <see cref="CodeFixProvider.FixableDiagnosticIds"/> of the associated <see cref="CodeFixProvider"/>.
        /// </param>
        /// <param name="registerCodeFix">Delegate to register a <see cref="CodeAction"/> fixing a subset of diagnostics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ArgumentNullException">Throws this exception if any of the arguments is null.</exception>
        public CodeFixContext(
            TextDocument document,
            Diagnostic diagnostic,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            CancellationToken cancellationToken)
            : this(document,
                   (diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))).Location.SourceSpan,
                   ImmutableArray.Create(diagnostic),
                   registerCodeFix,
                   CodeActionOptions.DefaultProvider,
                   cancellationToken)
        {
        }

        internal CodeFixContext(
            TextDocument document,
            TextSpan span,
            ImmutableArray<Diagnostic> diagnostics,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            CodeActionOptionsProvider options,
            CancellationToken cancellationToken)
        {
            VerifyDiagnosticsArgument(diagnostics, span);

            _document = document ?? throw new ArgumentNullException(nameof(document));
            _span = span;
            _diagnostics = diagnostics;
            _registerCodeFix = registerCodeFix ?? throw new ArgumentNullException(nameof(registerCodeFix));
            Options = options;
            _cancellationToken = cancellationToken;
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
            if (diagnostics.IsDefaultOrEmpty)
            {
                throw new ArgumentException(WorkspacesResources.At_least_one_diagnostic_must_be_supplied, nameof(diagnostics));
            }

            if (diagnostics.Any(static d => d == null))
            {
                throw new ArgumentException(WorkspaceExtensionsResources.Supplied_diagnostic_cannot_be_null, nameof(diagnostics));
            }

            if (diagnostics.Any((d, span) => d.Location.SourceSpan != span, span))
            {
                throw new ArgumentException(string.Format(WorkspacesResources.Diagnostic_must_have_span_0, span.ToString()), nameof(diagnostics));
            }
        }
    }
}
