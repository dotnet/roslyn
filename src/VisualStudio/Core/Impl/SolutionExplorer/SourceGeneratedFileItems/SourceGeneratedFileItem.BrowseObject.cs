// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class SourceGeneratedFileItem
{
    private sealed class BrowseObject(SourceGeneratedFileItem sourceGeneratedFileItem) : LocalizableProperties
    {
        [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Name))]
        public string Name => sourceGeneratedFileItem.HintName;

        public override string GetClassName() => SolutionExplorerShim.Source_Generated_File_Properties;
        public override string GetComponentName() => sourceGeneratedFileItem.HintName;
    }
}
