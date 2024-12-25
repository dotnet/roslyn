// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities;

/// <summary>
/// This partial contains methods that must be shared by source with the workspaces layer
/// </summary>
internal static partial class AssemblyUtilities
{
    /// <summary>
    /// Given a path to an assembly, returns its MVID (Module Version ID).
    /// May throw.
    /// </summary>
    /// <exception cref="IOException">If the file at <paramref name="filePath"/> does not exist or cannot be accessed.</exception>
    /// <exception cref="BadImageFormatException">If the file is not an assembly or is somehow corrupted.</exception>
    public static Guid ReadMvid(string filePath)
    {
        RoslynDebug.Assert(PathUtilities.IsAbsolute(filePath));

        using (var reader = new PEReader(FileUtilities.OpenRead(filePath)))
        {
            var metadataReader = reader.GetMetadataReader();
            var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
            var fileMvid = metadataReader.GetGuid(mvidHandle);

            return fileMvid;
        }
    }
}
