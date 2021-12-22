// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// Base implementation of C# and VB formatting services.
    /// </summary>
    internal abstract class AbstractFormattingService : IFormattingService
    {
        public async Task<Document> FormatAsync(Document document, IEnumerable<TextSpan>? spans, OptionSet options, CancellationToken cancellationToken)
        {
            var formattingOptions = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            return await Formatter.FormatAsync(document, spans, formattingOptions, rules: null, cancellationToken).ConfigureAwait(false);
        }
    }
}
