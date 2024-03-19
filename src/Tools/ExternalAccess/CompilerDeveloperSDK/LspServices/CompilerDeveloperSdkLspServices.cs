// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.CompilerDeveloperSdk;

internal readonly struct CompilerDeveloperSdkLspServices(LspServices lspServices)
{
    public T GetRequiredService<T>() where T : notnull
        => lspServices.GetRequiredService<T>();

    public T? GetService<T>()
        => lspServices.GetService<T>();
}
