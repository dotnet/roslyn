// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.BraceMatching;

internal interface IEmbeddedLanguageBraceMatcher : IEmbeddedLanguageFeatureService
{
    BraceMatchingResult? FindBraces(
        Project project,
        SemanticModel semanticModel,
        SyntaxToken token,
        int position,
        BraceMatchingOptions options,
        CancellationToken cancellationToken);
}
