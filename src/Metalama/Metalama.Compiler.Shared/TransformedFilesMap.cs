// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Metalama.Compiler;

/// <summary>
/// Contains information about files written by Metalama when the <c>MetalamaEmitCompilerTransformedFiles</c> project property is set.
/// </summary>
public class TransformedFilesMap
{
    /// <summary>
    /// The name of the file containing this information. It's located in the <c>$(IntermediateOutputPath)/metalama</c> directory.
    /// </summary>
    public static string FileName => "filemap.json";

    /// <summary>
    /// The files modified by Metalama transformations and written to disk.
    /// </summary>
    public IReadOnlyList<TransformedFileMapping> TransformedFiles { get; }

    public TransformedFilesMap(IReadOnlyList<TransformedFileMapping> transformedFiles)
    {
        TransformedFiles = transformedFiles;
    }
}
