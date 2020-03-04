// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal abstract class DocumentServiceProviderFactory
    {
        public abstract IDocumentServiceProvider CreateEmpty();

        public abstract IDocumentServiceProvider Create(IRazorDocumentContainer documentContainer);
    }
}
