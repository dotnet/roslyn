// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Rename;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Remote.Razor.Rename;

[Export(typeof(IRenameService)), Shared]
[method: ImportingConstructor]
internal sealed class OOPRenameService(
    IRazorComponentSearchEngine componentSearchEngine,
    IFileSystem fileSystem,
    LanguageServerFeatureOptions languageServerFeatureOptions)
    : RenameService(componentSearchEngine, fileSystem, languageServerFeatureOptions)
{
}
