// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// The context presented to a <see cref="QuickInfoProvider"/> when providing quick info.
    /// </summary>
    internal abstract class QuickInfoContext
    {
        /// <summary>
        /// The semantic model that quick info was requested within.
        /// </summary>
        public SemanticModel SemanticModel { get; }

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

        protected QuickInfoContext(
            SemanticModel semanticModel,
            int position,
            SyntaxToken token,
            HostLanguageServices languageServices,
            CancellationToken cancellationToken)
        {
            SemanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
            Position = position;
            Token = token;
            LanguageServices = languageServices;
            CancellationToken = cancellationToken;
        }

        public abstract QuickInfoContext With(SyntaxToken token);

        /// <summary>
        /// Creates a <see cref="QuickInfoContext"/> instance.
        /// </summary>
        public static async Task<QuickInfoContext> CreateAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var token = await semanticModel.SyntaxTree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
            return new QuickInfoContextWithDocument(document, semanticModel, position, token, cancellationToken);
        }

        /// <summary>
        /// Creates a <see cref="QuickInfoContext"/> instance.
        /// </summary>
        public static async Task<QuickInfoContext> CreateAsync(
            SemanticModel semanticModel,
            int position,
            HostLanguageServices languageServices,
            CancellationToken cancellationToken)
        {
            var token = await semanticModel.SyntaxTree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
            return new QuickInfoContextWithoutDocument(semanticModel, position, token, languageServices, cancellationToken);
        }
    }
}
