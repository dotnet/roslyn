// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

/// <summary>
/// An interface implemented by hosts to provide the host-level analyzers; for example in Visual Studio for Windows this
/// is where we'll fetch VSIX-defined analyzers.
/// </summary>
internal interface IHostDiagnosticAnalyzerProvider
{
    ImmutableArray<(AnalyzerFileReference reference, string extensionId)> GetAnalyzerReferencesInExtensions();
}
