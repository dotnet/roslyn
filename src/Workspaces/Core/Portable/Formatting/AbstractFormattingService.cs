// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public Task<Document> FormatAsync(Document document, IEnumerable<TextSpan> spans, OptionSet options, CancellationToken cancellationToken)
            => Formatter.FormatAsync(document, spans, options, rules: null, cancellationToken: cancellationToken);
    }
}
