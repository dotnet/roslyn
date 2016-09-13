// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Roslyn.VisualStudio.DiagnosticsWindow.Remote
{
    internal static class RemoteHostClientFactoryOptions
    {
        public const string OptionName = "FeatureManager/Features";

        [ExportOption]
        public static readonly Option<bool> RemoteHost_InProc = new Option<bool>(OptionName, nameof(RemoteHost_InProc), defaultValue: false);
    }
}
