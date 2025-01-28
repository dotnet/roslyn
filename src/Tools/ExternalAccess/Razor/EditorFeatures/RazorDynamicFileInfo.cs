// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    /// <summary>
    /// provides info on the given file
    /// 
    /// this will be used to provide dynamic content such as generated content from cshtml to workspace
    /// we acquire this from <see cref="IDynamicFileInfoProvider"/> exposed from external components such as razor for cshtml
    /// </summary>
    internal sealed class RazorDynamicFileInfo
    {
        public RazorDynamicFileInfo(string filePath, SourceCodeKind sourceCodeKind, TextLoader textLoader, IRazorDocumentServiceProvider documentServiceProvider)
        {
            FilePath = filePath;
            SourceCodeKind = sourceCodeKind;
            TextLoader = textLoader;
            DocumentServiceProvider = documentServiceProvider;
        }

        /// <summary>
        /// for now, return null. in future, we will use this to get right options from editorconfig
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// return <see cref="SourceCodeKind"/> for this file
        /// </summary>
        public SourceCodeKind SourceCodeKind { get; }

        /// <summary>
        /// return <see cref="RazorDynamicFileInfo.TextLoader"/> to load content for the dynamic file
        /// </summary>
        public TextLoader TextLoader { get; }

        /// <summary>
        /// return <see cref="IRazorDocumentServiceProvider"/> for the contents it provides
        /// </summary>
        public IRazorDocumentServiceProvider DocumentServiceProvider { get; }

        /// <summary>
        /// Constructs a new <see cref="DocumentInfo"/> from an existing <see cref="DocumentInfo"/> but with updated
        /// text loader and document service provider coming from this instance.
        /// </summary>
        public DocumentInfo ToUpdatedDocumentInfo(DocumentInfo existingDocumentInfo)
        {
            var serviceProvider = new RazorDocumentServiceProviderWrapper(this.DocumentServiceProvider);
            return new DocumentInfo(existingDocumentInfo.Attributes, this.TextLoader, serviceProvider);
        }
    }
}
