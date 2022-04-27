// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Formatting
{
    internal static class OmniSharpFormatter
    {
        public static Task<Document> FormatAsync(Document document, IEnumerable<TextSpan>? spans, OmniSharpSyntaxFormattingOptionsWrapper options, CancellationToken cancellationToken)
            => Formatter.FormatAsync(document, spans, options.CleanupOptions.FormattingOptions, rules: null, cancellationToken);

        public static async Task<Document> OrganizeImportsAsync(Document document, OmniSharpOrganizeImportsOptionsWrapper options, CancellationToken cancellationToken)
        {
            var organizeImportsService = document.GetLanguageService<IOrganizeImportsService>();
            if (organizeImportsService is null)
            {
                return document;
            }

            return await organizeImportsService.OrganizeImportsAsync(document, options.UnderlyingObject, cancellationToken).ConfigureAwait(false);
        }
    }
}
