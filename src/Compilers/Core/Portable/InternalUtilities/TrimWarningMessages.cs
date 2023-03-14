// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.Utilities;

internal static class TrimWarningMessages
{
    internal const string AnalyzerReflectionLoadMessage = "Loading analyzers via reflection is not supported when trimming.";
    internal const string NativePdbsNotSupported = "Native PDBs use built-in COM, which is not supported when trimming.";
    internal const string LoadsTypeByName = "Loading types by name is not supported when trimming.";
}