// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.DocumentationComments
{
    internal static class PEDocumentationCommentUtils
    {
        internal static string GetDocumentationComment(
            Symbol symbol,
            PEModuleSymbol containingPEModule,
            CultureInfo preferredCulture,
            CancellationToken cancellationToken,
            ref Tuple<CultureInfo, string> lazyDocComment)
        {
            // Have we cached anything?
            if (lazyDocComment == null)
            {
                Interlocked.CompareExchange(
                    ref lazyDocComment,
                    Tuple.Create(
                        preferredCulture,
                        containingPEModule.DocumentationProvider.GetDocumentationForSymbol(
                            symbol.GetDocumentationCommentId(), preferredCulture, cancellationToken)),
                    null);
            }

            // Does the cached version match the culture we asked for?
            if (object.Equals(lazyDocComment.Item1, preferredCulture))
            {
                return lazyDocComment.Item2;
            }

            // We've already cached a different culture - create a fresh version.
            return containingPEModule.DocumentationProvider.GetDocumentationForSymbol(
                symbol.GetDocumentationCommentId(), preferredCulture, cancellationToken);
        }
    }
}
