// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Options;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages
{
    internal sealed partial class ForceLowMemoryMode
    {
        public static readonly Option2<bool> Enabled = new(nameof(ForceLowMemoryMode), nameof(Enabled), defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(@"Roslyn\ForceLowMemoryMode\Enabled"));

        public static readonly Option2<int> SizeInMegabytes = new(nameof(ForceLowMemoryMode), nameof(SizeInMegabytes), defaultValue: 500,
            storageLocation: new LocalUserProfileStorageLocation(@"Roslyn\ForceLowMemoryMode\SizeInMegabytes"));
    }
}
