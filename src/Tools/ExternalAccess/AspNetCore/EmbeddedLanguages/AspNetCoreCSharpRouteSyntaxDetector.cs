// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages
{
    internal sealed class AspNetCoreCSharpRouteSyntaxDetector
    {
        public static readonly AspNetCoreCSharpRouteSyntaxDetector Instance = new();

        private readonly EmbeddedLanguageDetector _detector = new(
            CSharpEmbeddedLanguagesProvider.Info,
            ImmutableArray.Create("Route"));

        private AspNetCoreCSharpRouteSyntaxDetector()
        {
        }

        public bool IsEmbeddedLanguageToken(
            SyntaxToken token,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            [NotNullWhen(true)] out string? identifier,
            out IEnumerable<string>? options)
        {
            return _detector.IsEmbeddedLanguageToken(token, semanticModel, cancellationToken, out identifier, out options);
        }
    }
}
