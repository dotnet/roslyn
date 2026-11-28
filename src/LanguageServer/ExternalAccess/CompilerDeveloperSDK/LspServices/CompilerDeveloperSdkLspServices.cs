// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.CompilerDeveloperSdk;

/// <summary>
/// Provides access to the LSP services of an LSP server.  Import this from a service shared in
/// <see cref="CompilerDeveloperSdkLspServiceComposition.SharingBoundary"/>.
/// </summary>
[Export(typeof(CompilerDeveloperSdkLspServices)), Shared(LspServiceComposition.SharingBoundary)]
[method: ImportingConstructor]
[method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
internal sealed class CompilerDeveloperSdkLspServices(LspService<LspServices> lspServices)
{
    public T GetRequiredService<T>() where T : notnull
        => lspServices.Value.GetRequiredService<T>();

    public T? GetService<T>() where T : notnull
        => lspServices.Value.GetService<T>();
}
