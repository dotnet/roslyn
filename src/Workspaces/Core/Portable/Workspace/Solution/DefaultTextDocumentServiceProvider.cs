// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// <see cref="IDocumentServiceProvider"/> for regular C#/VB files.
    /// </summary>
    internal sealed class DefaultTextDocumentServiceProvider : IDocumentServiceProvider
    {
        public static readonly DefaultTextDocumentServiceProvider Instance = new DefaultTextDocumentServiceProvider();

        private DefaultTextDocumentServiceProvider() { }

        public TService GetService<TService>() where TService : class, IDocumentService
        {
            return default;
        }
    }
}
