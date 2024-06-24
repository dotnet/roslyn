// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.InlineHints
{
    /// <inheritdoc cref="IInlineHintsService"/>
    internal interface IFSharpInlineHintsService
    {
        /// <inheritdoc cref="IInlineHintsService.GetInlineHintsAsync"/>
        Task<ImmutableArray<FSharpInlineHint>> GetInlineHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);
    }
}
