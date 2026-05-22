// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Shared]
[Export(typeof(LanguageServerFeatureOptions))]
[method: ImportingConstructor]
internal class VSCodeLanguageServerFeatureOptions() : LanguageServerFeatureOptions
{
    public override bool SupportsFileManipulation => true;
    public override bool ShowAllCSharpCodeActions => false;
    public override bool ReturnCodeActionAndRenamePathsWithPrefixedSlash => PlatformInformation.IsWindows;
}
