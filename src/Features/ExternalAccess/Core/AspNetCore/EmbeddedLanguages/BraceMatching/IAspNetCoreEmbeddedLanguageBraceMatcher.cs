// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.BraceMatching;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

internal interface IAspNetCoreEmbeddedLanguageBraceMatcher
{
    /// <inheritdoc cref="IBraceMatcher.FindBracesAsync"/>
    AspNetCoreBraceMatchingResult? FindBraces(SemanticModel semanticModel, SyntaxToken token, int position, CancellationToken cancellationToken);
}
