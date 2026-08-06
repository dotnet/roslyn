// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.SpellCheck;

internal interface ICSharpSpellCheckRangeProvider
{
    Task<ImmutableArray<SpellCheckRange>> GetCSharpSpellCheckRangesAsync(RemoteDocumentContext documentContext, CancellationToken cancellationToken);
}
