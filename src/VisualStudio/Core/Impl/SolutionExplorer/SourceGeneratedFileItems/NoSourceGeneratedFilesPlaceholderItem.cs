// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed class NoSourceGeneratedFilesPlaceholderItem : BaseItem
    {
        public NoSourceGeneratedFilesPlaceholderItem()
            : base(SolutionExplorerShim.This_generator_is_not_generating_files)
        {
        }

        public override ImageMoniker IconMoniker => KnownMonikers.StatusInformationNoColor;
    }
}
