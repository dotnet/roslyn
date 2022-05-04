// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Formatting
{
    internal readonly record struct OmniSharpSyntaxFormattingOptionsWrapper
    {
        internal readonly CodeCleanupOptions CleanupOptions;

        internal OmniSharpSyntaxFormattingOptionsWrapper(CodeCleanupOptions cleanupOptions)
        {
            CleanupOptions = cleanupOptions;
        }

        public static async ValueTask<OmniSharpSyntaxFormattingOptionsWrapper> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var cleanupOptions = await document.GetCodeCleanupOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);
            return new OmniSharpSyntaxFormattingOptionsWrapper(cleanupOptions);
        }
    }
}
