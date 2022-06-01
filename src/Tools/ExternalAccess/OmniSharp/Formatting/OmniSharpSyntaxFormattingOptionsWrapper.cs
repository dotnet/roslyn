// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Formatting
{
    internal readonly record struct OmniSharpSyntaxFormattingOptionsWrapper
    {
        internal readonly SyntaxFormattingOptions FormattingOptions;
        internal readonly SimplifierOptions SimplifierOptions;

        internal OmniSharpSyntaxFormattingOptionsWrapper(SyntaxFormattingOptions formattingOptions, SimplifierOptions simplifierOptions)
        {
            FormattingOptions = formattingOptions;
            SimplifierOptions = simplifierOptions;
        }

        public static async ValueTask<OmniSharpSyntaxFormattingOptionsWrapper> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var formattingOptions = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            var simplifierOptions = await SimplifierOptions.FromDocumentAsync(document, fallbackOptions: null, cancellationToken).ConfigureAwait(false);

            return new OmniSharpSyntaxFormattingOptionsWrapper(formattingOptions, simplifierOptions);
        }
    }
}
