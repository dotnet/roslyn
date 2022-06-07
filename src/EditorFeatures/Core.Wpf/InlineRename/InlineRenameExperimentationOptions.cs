// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.InlineRename
{
    internal sealed class InlineRenameExperimentationOptions
    {
        public static readonly Option<bool> UseInlineAdornment = new(
            feature: "InlineRenameExperimentation",
            name: "UseInlineAdornment",
            defaultValue: false,
            storageLocation: new FeatureFlagStorageLocation("Roslyn.UseInlineAdornmentForRename"));
    }
}
