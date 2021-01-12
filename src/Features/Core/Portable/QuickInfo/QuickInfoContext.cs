// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// The context presented to a <see cref="QuickInfoProvider"/> when providing quick info.
    /// </summary>
    internal sealed class QuickInfoContext
    {
        /// <summary>
        /// The document that quick info was requested within.
        /// Can be <see langword="null"/> for quick info scenarios based off a <see cref="Compilation"/>,
        /// where <see cref="SemanticModel"/> is guaranteed to be non-null.
        /// </summary>
        public Document? Document { get; }

        /// <summary>
        /// The semantic model that quick info was requested within.
        /// Can be <see langword="null"/> for quick info scenarios where <see cref="Document.SupportsSemanticModel"/> is <see langword="false"/>
        /// for the <see cref="Document"/> whose quick info is being requested,
        /// </summary>
        public SemanticModel? SemanticModel { get; }

        /// <summary>
        /// The caret position where quick info was requested from.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// The token for which the quick info was requested.
        /// </summary>
        public SyntaxToken Token { get; }

        /// <summary>
        /// Host language services for the workspace.
        /// </summary>
        public HostLanguageServices LanguageServices { get; }

        /// <summary>
        /// Workspace.
        /// </summary>
        public Workspace Workspace => LanguageServices.WorkspaceServices.Workspace;

        /// <summary>
        /// The cancellation token to use for this operation.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        private QuickInfoContext(
            Document? document,
            SemanticModel? semanticModel,
            int position,
            SyntaxToken token,
            HostLanguageServices languageServices,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document != null || semanticModel != null);
            Contract.ThrowIfFalse(semanticModel != null || token == default);

            Document = document;
            SemanticModel = semanticModel;
            Position = position;
            Token = token;
            LanguageServices = languageServices;
            CancellationToken = cancellationToken;
        }

        public QuickInfoContext With(SyntaxToken token)
            => new(Document, SemanticModel, Position, token, LanguageServices, CancellationToken);

        /// <summary>
        /// Creates a <see cref="QuickInfoContext"/> instance for a document.
        /// </summary>
        public static async Task<QuickInfoContext> CreateAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var token = semanticModel != null ? await semanticModel.SyntaxTree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false) : default;
            return new QuickInfoContext(document, semanticModel, position, token, document.Project.LanguageServices, cancellationToken);
        }

        /// <summary>
        /// Creates a <see cref="QuickInfoContext"/> instance for a non-document, compilation-only context.
        /// </summary>
        public static async Task<QuickInfoContext> CreateAsync(
            SemanticModel semanticModel,
            int position,
            HostLanguageServices languageServices,
            CancellationToken cancellationToken)
        {
            var token = await semanticModel.SyntaxTree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
            return new QuickInfoContext(document: null, semanticModel, position, token, languageServices, cancellationToken);
        }
    }
}
