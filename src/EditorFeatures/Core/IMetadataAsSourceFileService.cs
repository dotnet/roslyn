// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IMetadataAsSourceFileService
    {
        /// <summary>
        /// Generates a file on disk containing general information about the symbol's containing
        /// assembly, and the formatted source code for the public, protected, and
        /// protected-or-internal interface of which the given ISymbol is or is a part of.
        /// </summary>
        /// <param name="project">The project from which the symbol to generate source for came
        /// from.</param>
        /// <param name="symbol">The symbol whose interface to generate source for</param>
        /// <param name="allowDecompilation"><see langword="true"/> to allow a decompiler or other technology to show a
        /// representation of the original sources; otherwise <see langword="false"/> to only show member
        /// signatures.</param>
        /// <param name="cancellationToken">To cancel project and document operations</param>
        Task<MetadataAsSourceFile> GetGeneratedFileAsync(Project project, ISymbol symbol, bool allowDecompilation, CancellationToken cancellationToken = default);

        bool TryAddDocumentToWorkspace(string filePath, ITextBuffer buffer);

        bool TryRemoveDocumentFromWorkspace(string filePath);

        void CleanupGeneratedFiles();

        bool IsNavigableMetadataSymbol(ISymbol symbol);
    }
}
