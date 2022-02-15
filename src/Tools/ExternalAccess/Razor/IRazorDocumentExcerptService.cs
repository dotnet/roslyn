// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Obsolete("Use IRazorDocumentExcerptServiceImplementation instead")]
    internal interface IRazorDocumentExcerptService
    {
        Task<RazorExcerptResult?> TryExcerptAsync(Document document, TextSpan span, RazorExcerptMode mode, CancellationToken cancellationToken);
    }

    internal interface IRazorDocumentExcerptServiceImplementation
    {
        Task<RazorExcerptResult?> TryExcerptAsync(Document document, TextSpan span, RazorExcerptMode mode, RazorClassificationOptionsWrapper options, CancellationToken cancellationToken);
    }
}
