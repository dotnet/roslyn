// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// An LSP service that needs asynchronous cleanup when its server exits.  <see cref="LspServices"/> awaits
/// <see cref="DisposeAsync"/> on every created instance before the server's MEF sharing boundary is disposed (which
/// then synchronously disposes any <see cref="IDisposable"/> per-server parts).
/// </summary>
/// <remarks>
/// This intentionally does not derive from <see cref="IAsyncDisposable"/>, and LSP services should not implement
/// that interface: MEF disposes the per-server sharing boundary synchronously, so it would either ignore it or
/// block on it (and call it a second time).
/// </remarks>
internal interface IAsyncDisposableLspService : ILspService
{
    ValueTask DisposeAsync();
}
