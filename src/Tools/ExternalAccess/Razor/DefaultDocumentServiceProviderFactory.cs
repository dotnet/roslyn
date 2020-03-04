// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Shared]
    [Export(typeof(DocumentServiceProviderFactory))]
    internal class DefaultDocumentServiceProviderFactory : DocumentServiceProviderFactory
    {
        public override IDocumentServiceProvider Create(IRazorDocumentContainer documentContainer)
        {
            if (documentContainer == null)
            {
                throw new ArgumentNullException(nameof(documentContainer));
            }

            return new DocumentServiceProvider(documentContainer);
        }

        public override IDocumentServiceProvider CreateEmpty()
        {
            return new DocumentServiceProvider();
        }
    }
}
