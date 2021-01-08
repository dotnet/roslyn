// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// The context presented to a <see cref="QuickInfoProvider"/> when providing quick info.
    /// </summary>
    internal sealed class QuickInfoContextWithoutDocument : QuickInfoContext
    {
        public QuickInfoContextWithoutDocument(
            SemanticModel semanticModel,
            int position,
            SyntaxToken token,
            HostLanguageServices languageServices,
            CancellationToken cancellationToken)
            : base(semanticModel, position, token, languageServices, cancellationToken)
        {
        }

        public override QuickInfoContext With(SyntaxToken token)
            => new QuickInfoContextWithoutDocument(SemanticModel, Position, token, LanguageServices, CancellationToken);
    }
}
