// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// provides info on the given file
    /// 
    /// this will be used to provide dynamic content such as generated content from cshtml to workspace
    /// we acquire this from <see cref="IDynamicFileInfoProvider"/> exposed from external components such as razor for cshtml
    /// </summary>
    internal sealed class DynamicFileInfo(string filePath, SourceCodeKind sourceCodeKind, TextLoader textLoader, bool designTimeOnly, IDocumentServiceProvider? documentServiceProvider)
    {

        /// <summary>
        /// The path to the generated file. in future, we will use this to get right options from editorconfig
        /// </summary>
        public string FilePath { get; } = filePath;

        /// <summary>
        /// return <see cref="SourceCodeKind"/> for this file
        /// </summary>
        public SourceCodeKind SourceCodeKind { get; } = sourceCodeKind;

        /// <summary>
        /// return <see cref="TextLoader"/> to load content for the dynamic file
        /// </summary>
        public TextLoader TextLoader { get; } = textLoader;

        /// <summary>
        /// True if the source code contained in the document is only used in design-time (e.g. for completion),
        /// but is not passed to the compiler when the containing project is built, e.g. a Razor view.
        /// </summary>
        public bool DesignTimeOnly { get; } = designTimeOnly;

        /// <summary>
        /// return <see cref="IDocumentServiceProvider"/> for the content it provided
        /// </summary>
        public IDocumentServiceProvider? DocumentServiceProvider { get; } = documentServiceProvider;
    }
}
