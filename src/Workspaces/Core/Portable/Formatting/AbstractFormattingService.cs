// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// Base implementation of C# and VB formatting services.
    /// </summary>
    internal abstract class AbstractFormattingService : IFormattingService
    {
        public Task<Document> FormatAsync(Document document, IEnumerable<TextSpan>? spans, OptionSet? options, CancellationToken cancellationToken)
            => Formatter.FormatAsync(document, spans, options, rules: null, cancellationToken: cancellationToken);
    }
}
