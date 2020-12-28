﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed partial class SourceGeneratorItem
    {
        internal sealed class BrowseObject : LocalizableProperties
        {
            private readonly SourceGeneratorItem _sourceGeneratorItem;

            public BrowseObject(SourceGeneratorItem sourceGeneratorItem)
            {
                _sourceGeneratorItem = sourceGeneratorItem;
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Type_Name))]
            public string TypeName => _sourceGeneratorItem.Generator.GetType().FullName;

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Path))]
            public string? Path => _sourceGeneratorItem.AnalyzerReference.FullPath;

            public override string GetClassName() => SolutionExplorerShim.Source_Generator_Properties;
            public override string GetComponentName() => TypeName;
        }
    }
}
