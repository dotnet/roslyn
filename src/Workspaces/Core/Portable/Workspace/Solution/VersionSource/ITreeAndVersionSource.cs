// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Similar to <see cref="ITextAndVersionSource"/>, but for trees.  Allows hiding (or introspecting) the details of how
/// a tree is created for a particular document.
/// </summary>
internal interface ITreeAndVersionSource
{
    Task<TreeAndVersion> GetValueAsync(CancellationToken cancellationToken);
    TreeAndVersion GetValue(CancellationToken cancellationToken);
    bool TryGetValue([NotNullWhen(true)] out TreeAndVersion? value);
}
