// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Import this to get the <typeparamref name="T"/> service of the LSP server the importing part belongs to.  The
/// service is resolved (lazily) through <see cref="LspServices"/>, so it respects the server's LSP contract, server
/// kind overrides and base services, and it is the same instance every other consumer in that server gets.
/// </summary>
/// <remarks>
/// MEF only closes open generic exports under their default contract name, so this must not be given a custom
/// contract name.
/// </remarks>
[Export(typeof(LspService<>)), Shared(LspServiceComposition.SharingBoundary)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspService<T>(LspServerScope scope) where T : notnull
{
    /// <summary>
    /// The service; throws if the server does not have a <typeparamref name="T"/> service.
    /// </summary>
    public T Value => scope.LspServices.GetRequiredService<T>();

    /// <summary>
    /// The service, or <see langword="null"/> if the server does not have a <typeparamref name="T"/> service.
    /// </summary>
    public T? GetValueOrDefault() => scope.LspServices.GetService<T>();
}
