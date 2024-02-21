// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.CodeMapper;

internal interface ICSharpCopilotMapCodeService
{
    Task<ImmutableArray<TextChange>?> MapCodeAsync(
        Document document,
        ImmutableArray<string> contents,
        ImmutableArray<(Document document, TextSpan textSpan)> prioritizedFocusLocations,
        CancellationToken cancellationToken);
}
