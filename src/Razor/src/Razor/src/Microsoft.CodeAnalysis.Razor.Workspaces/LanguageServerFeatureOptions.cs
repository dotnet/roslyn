// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal abstract class LanguageServerFeatureOptions
{
    public abstract bool SupportsFileManipulation { get; }

    public abstract bool ShowAllCSharpCodeActions { get; }

    // Code action and rename paths in Windows VS Code need to be prefixed with '/':
    // https://github.com/dotnet/razor/issues/8131
    public abstract bool ReturnCodeActionAndRenamePathsWithPrefixedSlash { get; }
}
