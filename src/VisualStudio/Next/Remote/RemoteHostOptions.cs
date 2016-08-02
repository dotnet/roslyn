// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal static class RemoteHostOptions
    {
        public const string OptionName = "FeatureManager/Features";

        [ExportOption]
        public static readonly Option<bool> RemoteHost = new Option<bool>(OptionName, nameof(RemoteHost), defaultValue: false);
    }
}
