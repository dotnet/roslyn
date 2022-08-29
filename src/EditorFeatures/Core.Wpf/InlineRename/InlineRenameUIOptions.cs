// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.InlineRename
{
    internal sealed class InlineRenameUIOptions
    {
        public static readonly Option2<bool> UseInlineAdornment = new(
            feature: "InlineRename",
            name: "UseInlineAdornment",
            defaultValue: true,
            storageLocation: new FeatureFlagStorageLocation("Roslyn.Rename_UseInlineAdornment"));
    }
}
