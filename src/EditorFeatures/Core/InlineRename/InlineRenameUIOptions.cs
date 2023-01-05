﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.InlineRename
{
    internal sealed class InlineRenameUIOptions
    {
        private const string FeatureName = "InlineRename";

        public static readonly Option2<bool> UseInlineAdornment = new(FeatureName, "UseInlineAdornment", defaultValue: true);
        public static readonly Option2<bool> CollapseUI = new(FeatureName, "CollapseRenameUI", defaultValue: false);
    }
}
