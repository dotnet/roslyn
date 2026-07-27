// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

internal sealed class AspNetCoreCSharpRouteSyntaxDetector
{
    public static readonly AspNetCoreCSharpRouteSyntaxDetector Instance = new();

    private static readonly EmbeddedLanguageDetector s_detector;

    static AspNetCoreCSharpRouteSyntaxDetector()
    {
        var identifiers = ImmutableArray.Create("Route");
        s_detector = new EmbeddedLanguageDetector(
            CSharpEmbeddedLanguagesProvider.Info,
            identifiers,
            new EmbeddedLanguageCommentDetector(identifiers));
    }

    private AspNetCoreCSharpRouteSyntaxDetector()
    {
    }

#pragma warning disable CA1822 // Mark members as static
    public bool IsEmbeddedLanguageToken(
        SyntaxToken token,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier,
        out IEnumerable<string>? options)
    {
        return s_detector.IsEmbeddedLanguageToken(token, semanticModel, cancellationToken, out identifier, out options);
    }
#pragma warning restore CA1822
}
