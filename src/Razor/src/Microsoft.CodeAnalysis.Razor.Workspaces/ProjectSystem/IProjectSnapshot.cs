// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal interface IProjectSnapshot
{
    IEnumerable<string> DocumentFilePaths { get; }

    /// <summary>
    /// Gets the full path to the .csproj file for this project
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Gets the full path to the folder under 'obj' where the project.razor.bin file will live
    /// </summary>
    string IntermediateOutputPath { get; }

    string DisplayName { get; }

    ValueTask<TagHelperCollection> GetTagHelpersAsync(CancellationToken cancellationToken);

    bool ContainsDocument(string filePath);
    bool TryGetDocument(string filePath, [NotNullWhen(true)] out IDocumentSnapshot? document);
}
