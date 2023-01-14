// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Structure
{
    internal readonly record struct OmniSharpBlockStructureOptions(
        bool ShowBlockStructureGuidesForCommentsAndPreprocessorRegions,
        bool ShowOutliningForCommentsAndPreprocessorRegions)
    {
        internal BlockStructureOptions ToBlockStructureOptions()
            => BlockStructureOptions.Default with
            {
                ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = ShowBlockStructureGuidesForCommentsAndPreprocessorRegions,
                ShowOutliningForCommentsAndPreprocessorRegions = ShowOutliningForCommentsAndPreprocessorRegions,
            };
    }
}
