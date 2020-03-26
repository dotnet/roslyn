// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Roslyn.VisualStudio.DiagnosticsWindow.Remote
{
    internal static class RemoteHostClientFactoryOptions
    {
        internal const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        public static readonly Option<bool> RemoteHost_InProc = new Option<bool>(
            "InternalFeatureOnOffOptions", nameof(RemoteHost_InProc), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(RemoteHost_InProc)));
    }

    [ExportOptionProvider, Shared]
    internal class RemoteHostClientFactoryOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public RemoteHostClientFactoryOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            RemoteHostClientFactoryOptions.RemoteHost_InProc);
    }
}
