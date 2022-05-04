// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for code fixes provided by a <see cref="CodeFixProvider"/>.
    /// </summary>
#pragma warning disable CS0612 // Type or member is obsolete
    public readonly struct CodeFixContext : ITypeScriptCodeFixContext
#pragma warning restore
    {
        private readonly Document _document;
        private readonly TextSpan _span;
        private readonly ImmutableArray<Diagnostic> _diagnostics;
        private readonly CancellationToken _cancellationToken;
        private readonly Action<CodeAction, ImmutableArray<Diagnostic>> _registerCodeFix;

        /// <summary>
        /// Document corresponding to the <see cref="Span"/> to fix.
        /// </summary>
        public Document Document => _document;

        /// <summary>
        /// Text span within the <see cref="Document"/> to fix.
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
        /// </summary>
        /// <remarks>
        /// Provider to allow code fix to update documents across multiple projects that differ in language (and hence language specific options).
        /// </remarks>
        internal readonly CodeActionOptionsProvider Options;

        [Obsolete]
        bool ITypeScriptCodeFixContext.IsBlocking
            => Options.GetOptions(Document.Project.LanguageServices).IsBlocking;

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
            : this(document ?? throw new ArgumentNullException(nameof(document)),
                   span,
                   VerifyDiagnosticsArgument(diagnostics, span),
                   registerCodeFix ?? throw new ArgumentNullException(nameof(registerCodeFix)),
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
        public CodeFixContext(
            Document document,
            Diagnostic diagnostic,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            CancellationToken cancellationToken)
            : this(document ?? throw new ArgumentNullException(nameof(document)),
                   (diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))).Location.SourceSpan,
                   ImmutableArray.Create(diagnostic),
                   registerCodeFix ?? throw new ArgumentNullException(nameof(registerCodeFix)),
                   CodeActionOptions.DefaultProvider,
                   cancellationToken)
        {
        }

        internal CodeFixContext(
            Document document,
            TextSpan span,
            ImmutableArray<Diagnostic> diagnostics,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            CodeActionOptionsProvider options,
            CancellationToken cancellationToken)
        {
            Debug.Assert(diagnostics.Any(d => d.Location.SourceSpan == span));

            _document = document;
            _span = span;
            _diagnostics = diagnostics;
            _registerCodeFix = registerCodeFix;
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

        private static ImmutableArray<Diagnostic> VerifyDiagnosticsArgument(ImmutableArray<Diagnostic> diagnostics, TextSpan span)
        {
            if (diagnostics.IsDefaultOrEmpty)
            {
                throw new ArgumentException(WorkspacesResources.At_least_one_diagnostic_must_be_supplied, nameof(diagnostics));
            }

            if (diagnostics.Any(d => d == null))
            {
                throw new ArgumentException(WorkspaceExtensionsResources.Supplied_diagnostic_cannot_be_null, nameof(diagnostics));
            }

            if (diagnostics.Any((d, span) => d.Location.SourceSpan != span, span))
            {
                throw new ArgumentException(string.Format(WorkspacesResources.Diagnostic_must_have_span_0, span.ToString()), nameof(diagnostics));
            }

            return diagnostics;
        }
    }

    [Obsolete]
    internal interface ITypeScriptCodeFixContext
    {
        bool IsBlocking { get; }
    }
}
