// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IDocumentServiceProvider
    {
        /// <summary>
        /// Gets a document specific service provided by the host identified by the service type. 
        /// If the host does not provide the service, this method returns null.
        /// </summary>
        TService? GetService<TService>() where TService : class, IDocumentService;
    }
}
