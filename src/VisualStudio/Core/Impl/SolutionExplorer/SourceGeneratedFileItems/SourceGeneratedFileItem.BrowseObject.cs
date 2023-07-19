// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed partial class SourceGeneratedFileItem
    {
        private class BrowseObject : LocalizableProperties
        {
            private readonly SourceGeneratedFileItem _sourceGeneratedFileItem;

            public BrowseObject(SourceGeneratedFileItem sourceGeneratedFileItem)
            {
                _sourceGeneratedFileItem = sourceGeneratedFileItem;
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Name))]
            public string Name => _sourceGeneratedFileItem.HintName;

            public override string GetClassName() => SolutionExplorerShim.Source_Generated_File_Properties;
            public override string GetComponentName() => _sourceGeneratedFileItem.HintName;
        }
    }
}
