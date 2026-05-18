// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

internal abstract class AbstractRazorLspServiceFactory : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return CreateService(new RazorLspServices(lspServices));
    }

    protected abstract AbstractRazorLspService CreateService(IRazorLspServices lspServices);

    private sealed class RazorLspServices(LspServices lspServices) : IRazorLspServices
    {
        public void Dispose()
        {
        }

        public T GetRequiredService<T>() where T : notnull => lspServices.GetRequiredService<T>();
        public IEnumerable<T> GetRequiredServices<T>() => lspServices.GetRequiredServices<T>();
        public T? GetService<T>() where T : notnull => lspServices.GetService<T>();
        public bool TryGetService(Type type, [NotNullWhen(true)] out object? service) => lspServices.TryGetService(type, out service);
    }
}
