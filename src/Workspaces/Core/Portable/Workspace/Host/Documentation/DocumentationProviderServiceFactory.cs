// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [Export(typeof(IDocumentationProviderService)), Shared]
    internal sealed class DocumentationProviderService : IDocumentationProviderService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentationProviderService()
        {
        }

        private readonly ConcurrentDictionary<string, DocumentationProvider> _assemblyPathToDocumentationProviderMap = new();

        public DocumentationProvider GetDocumentationProvider(string assemblyPath)
        {
            if (assemblyPath == null)
                throw new ArgumentNullException(nameof(assemblyPath));

            assemblyPath = Path.ChangeExtension(assemblyPath, "xml");
            return _assemblyPathToDocumentationProviderMap.GetOrAdd(assemblyPath, static assemblyPath => XmlDocumentationProvider.CreateFromFile(assemblyPath));
        }
    }
}
