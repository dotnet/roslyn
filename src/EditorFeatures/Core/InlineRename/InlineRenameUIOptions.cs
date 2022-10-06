// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.InlineRename
{
    internal sealed class InlineRenameUIOptions
    {
        private const string FeatureName = "InlineRename";

        public static readonly Option2<bool> UseInlineAdornment = new(
            FeatureName,
            name: "UseInlineAdornment",
            defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.RenameUseInlineAdornment"));

        public static readonly Option2<bool> CollapseUI = new(
            FeatureName,
            name: "CollapseRenameUI",
            defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.CollapseRenameUI"));
    }
}
