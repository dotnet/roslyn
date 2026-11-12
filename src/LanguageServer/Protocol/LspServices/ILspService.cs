// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// A service used by an LSP server. Stateful services may implement <see cref="IDisposable"/> or
/// <see cref="IAsyncDisposable"/> and are disposed when the server exits; asynchronous disposal is
/// preferred when both interfaces are implemented. Stateless services are owned by the MEF container.
/// </summary>
internal interface ILspService
{
}
