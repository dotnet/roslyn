// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Formatting
{
    internal readonly record struct OmniSharpSyntaxFormattingOptionsWrapper
    {
        internal readonly SyntaxFormattingOptions UnderlyingObject;

        internal OmniSharpSyntaxFormattingOptionsWrapper(SyntaxFormattingOptions underlyingObject)
            => UnderlyingObject = underlyingObject;

        public static async ValueTask<OmniSharpSyntaxFormattingOptionsWrapper> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var options = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            return new OmniSharpSyntaxFormattingOptionsWrapper(options);
        }
    }
}
