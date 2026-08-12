// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal interface IFormattingValidationPass
{
    Task<bool> IsValidAsync(FormattingContext formattingContext, ImmutableArray<TextChange> changes, CancellationToken cancellationToken);
}
