// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.Internal.EmbeddedLanguages;

[ExportEmbeddedLanguageBraceMatcher(
    nameof(AspNetCoreEmbeddedLanguageBraceMatcher),
    [LanguageNames.CSharp],
    supportsUnannotatedAPIs: false,
    // Add more syntax names here in the future if there are additional cases ASP.Net would like to light up on.
    identifiers: ["Route"]), Shared]
internal class AspNetCoreEmbeddedLanguageBraceMatcher : IEmbeddedLanguageBraceMatcher
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public AspNetCoreEmbeddedLanguageBraceMatcher()
    {
    }

    public BraceMatchingResult? FindBraces(
        Project project,
        SemanticModel semanticModel,
        SyntaxToken token,
        int position,
        BraceMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var braceMatchers = AspNetCoreBraceMatcherExtensionProvider.GetExtensions(project);

        foreach (var braceMatcher in braceMatchers)
        {
            var result = braceMatcher.FindBraces(semanticModel, token, position, cancellationToken)?.ToBraceMatchingResult();
            if (result != null)
                return result;
        }

        return null;
    }
}
