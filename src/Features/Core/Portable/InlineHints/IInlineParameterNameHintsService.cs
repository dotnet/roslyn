// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineHints;

/// <summary>
/// Gets inline hints for type locations.  This is an internal service only for C# and VB.  Use <see
/// cref="IInlineHintsService"/> for other languages.
/// </summary>
internal interface IInlineParameterNameHintsService : ILanguageService
{
    Task AddInlineHintsAsync(
        Document document,
        TextSpan textSpan,
        InlineParameterHintsOptions options,
        SymbolDescriptionOptions displayOptions,
        bool displayAllOverride,
        ArrayBuilder<InlineHint> result,
        CancellationToken cancellationToken);
}
