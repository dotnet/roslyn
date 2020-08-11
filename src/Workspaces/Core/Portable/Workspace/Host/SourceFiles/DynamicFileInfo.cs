// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// provides info on the given file
    /// 
    /// this will be used to provide dynamic content such as generated content from cshtml to workspace
    /// we acquire this from <see cref="IDynamicFileInfoProvider"/> exposed from external components such as razor for cshtml
    /// </summary>
    internal sealed class DynamicFileInfo
    {
        public DynamicFileInfo(string filePath, SourceCodeKind sourceCodeKind, TextLoader textLoader, bool designTimeOnly, string? diagnosticsLspClientName, IDocumentServiceProvider documentServiceProvider)
        {
            FilePath = filePath;
            SourceCodeKind = sourceCodeKind;
            TextLoader = textLoader;
            DocumentServiceProvider = documentServiceProvider;
            DesignTimeOnly = designTimeOnly;
            DiagnosticsLspClientName = diagnosticsLspClientName;
        }

        /// <summary>
        /// The path to the generated file. in future, we will use this to get right options from editorconfig
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// return <see cref="SourceCodeKind"/> for this file
        /// </summary>
        public SourceCodeKind SourceCodeKind { get; }

        /// <summary>
        /// return <see cref="TextLoader"/> to load content for the dynamic file
        /// </summary>
        public TextLoader TextLoader { get; }

        /// <summary>
        /// True if the source code contained in the document is only used in design-time (e.g. for completion),
        /// but is not passed to the compiler when the containing project is built.
        /// </summary>
        public bool DesignTimeOnly { get; }

        /// <summary>
        /// The LSP client name that should get the diagnostics produced by this document; any other source
        /// will not show these diagnostics.  For example, razor uses this to exclude diagnostics from the error list
        /// so that they can handle the final display.
        /// If null, the diagnostics do not have this special handling.
        /// </summary>
        public string? DiagnosticsLspClientName { get; }

        /// <summary>
        /// return <see cref="IDocumentServiceProvider"/> for the content it provided
        /// </summary>
        public IDocumentServiceProvider DocumentServiceProvider { get; }
    }
}
