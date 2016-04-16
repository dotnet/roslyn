// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal static class SignatureHelpOptions
    {
        public const string FeatureName = "Signature Help";

        [ExportOption]
        public static readonly PerLanguageOption<bool> ShowSignatureHelp = new PerLanguageOption<bool>(FeatureName, "Show Signature Help", defaultValue: true);
    }
}
