// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal interface ITextAndVersionSource
{
    /// <summary>
    /// True if <see cref="SourceText"/> can be reloaded.
    /// </summary>
    bool CanReloadText { get; }

    bool TryGetValue(LoadTextOptions options, [MaybeNullWhen(false)] out TextAndVersion value);
    TextAndVersion GetValue(LoadTextOptions options, CancellationToken cancellationToken);
    Task<TextAndVersion> GetValueAsync(LoadTextOptions options, CancellationToken cancellationToken);
}
