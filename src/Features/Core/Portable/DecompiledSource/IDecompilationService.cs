// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.DecompiledSource;

/// <summary>
/// Abstraction over the actual decompilation libraries so we can avoid shipping them in source built assemblies.
/// </summary>
internal interface IDecompilationService : ILanguageService
{
    Document? PerformDecompilation(Document document, string fullName, Compilation compilation, MetadataReference? metadataReference, string? assemblyLocation);

    FileVersionInfo GetDecompilerVersion();
}
