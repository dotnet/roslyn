// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// The context presented to a <see cref="QuickInfoProvider"/> when providing quick info.
    /// </summary>
    internal sealed class QuickInfoContextWithDocument : QuickInfoContext
    {
        /// <summary>
        /// The document that quick info was requested within.
        /// </summary>
        public Document Document { get; }

        public QuickInfoContextWithDocument(
            Document document,
            SemanticModel semanticModel,
            int position,
            SyntaxToken token,
            CancellationToken cancellationToken)
            : base(semanticModel, position, token, document.Project.LanguageServices, cancellationToken)
        {
            Document = document;
        }

        public override QuickInfoContext With(SyntaxToken token)
            => new QuickInfoContextWithDocument(Document, SemanticModel, Position, token, CancellationToken);
    }
}
