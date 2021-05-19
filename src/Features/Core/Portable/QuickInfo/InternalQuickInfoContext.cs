// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// <see cref="SemanticModel"/> based context presented to a <see cref="InternalQuickInfoProvider"/> when providing quick info.
    /// All our internal C# and VB providers are <see cref="InternalQuickInfoProvider"/>'s.
    /// </summary>
    internal sealed class InternalQuickInfoContext : AbstractQuickInfoContext
    {
        /// <summary>
        /// The semantic model that quick info was requested within.
        /// </summary>
        public SemanticModel SemanticModel { get; }

        private readonly Document? _document;

        /// <summary>
        /// The token for which the quick info was requested.
        /// </summary>
        public SyntaxToken Token { get; }

        private InternalQuickInfoContext(
            SemanticModel semanticModel,
            Document? document,
            int position,
            SyntaxToken token,
            HostLanguageServices languageServices,
            CancellationToken cancellationToken)
            : base(position, languageServices, cancellationToken)
        {
            SemanticModel = semanticModel;
            Token = token;
            _document = document;
        }

        /// <summary>
        /// TGet the document that quick info was requested within.
        /// Can be <see langword="null"/> for quick info scenarios based off a <see cref="Compilation"/>,
        /// where <see cref="SemanticModel"/> is guaranteed to be non-null.
        /// </summary>
        public bool TryGetDocument([NotNullWhen(returnValue: true)] out Document? document)
        {
            document = _document;
            return document != null;
        }

        public InternalQuickInfoContext WithToken(SyntaxToken token)
            => new(SemanticModel, _document, Position, token, LanguageServices, CancellationToken);

        /// <summary>
        /// Creates an <see cref="InternalQuickInfoContext"/> instance for a document.
        /// </summary>
        public static async Task<InternalQuickInfoContext> CreateAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var token = await semanticModel.SyntaxTree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
            return new InternalQuickInfoContext(semanticModel, document, position, token, document.Project.LanguageServices, cancellationToken);
        }

        /// <summary>
        /// Creates an <see cref="InternalQuickInfoContext"/> instance for a non-document, compilation-only context.
        /// </summary>
        public static async Task<InternalQuickInfoContext> CreateAsync(
            SemanticModel semanticModel,
            int position,
            HostLanguageServices languageServices,
            CancellationToken cancellationToken)
        {
            var token = await semanticModel.SyntaxTree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
            return new(semanticModel, document: null, position, token, languageServices, cancellationToken);
        }
    }
}
