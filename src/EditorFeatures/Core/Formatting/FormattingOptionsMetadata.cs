// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal sealed class FormattingOptionsMetadata
    {
        private const string FeatureName = "FormattingOptions";

        public static readonly PerLanguageOption2<bool> FormatOnPaste =
            new(FeatureName, OptionGroup.Default, "FormatOnPaste", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.FormatOnPaste"));
    }
}
