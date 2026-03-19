// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class SourceGeneratorItem
{
    internal sealed class BrowseObject(SourceGeneratorItem sourceGeneratorItem) : LocalizableProperties
    {
        [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Type_Name))]
        public string TypeName => sourceGeneratorItem.Identity.TypeName;

        [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Path))]
        public string? Path => sourceGeneratorItem._path;

        public override string GetClassName() => SolutionExplorerShim.Source_Generator_Properties;
        public override string GetComponentName() => TypeName;
    }
}
