// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LineCommit
{
    internal class LineCommitOptions
    {
        public static readonly PerLanguageOption2<bool> PrettyListing = new("visual_basic_line_commit_options_pretty_listing", defaultValue: true);
    }
}
