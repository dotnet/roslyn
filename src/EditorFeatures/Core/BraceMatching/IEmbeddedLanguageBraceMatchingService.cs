// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.BraceMatching
{
    internal interface IEmbeddedLanguageBraceMatchingService
    {
        BraceMatchingResult? FindBraces(
            SemanticModel semanticModel,
            SyntaxToken syntaxToken,
            BraceMatchingOptions options,
            CancellationToken cancellationToken);
    }
}
