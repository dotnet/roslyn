﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MetadataAsSource
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

        bool TryAddDocumentToWorkspace(string filePath, SourceTextContainer buffer);

        bool TryRemoveDocumentFromWorkspace(string filePath);

        void CleanupGeneratedFiles();

        bool IsNavigableMetadataSymbol(ISymbol symbol);
    }
}
