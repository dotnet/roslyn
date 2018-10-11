// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class FormattingCodeFixHelper
    {
        internal static async Task<Document> FixOneAsync(Document document, OptionSet options, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            // The span to format is the full line(s) containing the diagnostic
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var diagnosticLinePositionSpan = text.Lines.GetLinePositionSpan(diagnosticSpan);
            var spanToFormat = TextSpan.FromBounds(
                text.Lines[diagnosticLinePositionSpan.Start.Line].Start,
                text.Lines[diagnosticLinePositionSpan.End.Line].End);

            return await Formatter.FormatAsync(document, spanToFormat, options, cancellationToken).ConfigureAwait(false);
        }
    }
}
