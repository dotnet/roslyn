// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.Shell;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages
{
    internal sealed partial class ForceLowMemoryMode
    {
        public static readonly Option<bool> Enabled = new Option<bool>(nameof(ForceLowMemoryMode), nameof(Enabled), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(@"Roslyn\ForceLowMemoryMode\Enabled"));

        public static readonly Option<int> SizeInMegabytes = new Option<int>(nameof(ForceLowMemoryMode), nameof(SizeInMegabytes), defaultValue: 500,
            storageLocations: new LocalUserProfileStorageLocation(@"Roslyn\ForceLowMemoryMode\SizeInMegabytes"));
    }
}
