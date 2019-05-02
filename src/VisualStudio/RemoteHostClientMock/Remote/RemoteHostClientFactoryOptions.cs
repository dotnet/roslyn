// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
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

#pragma warning disable RS0032 // Test exports should not be discoverable
    [ExportOptionProvider, Shared]
#pragma warning restore RS0032 // Test exports should not be discoverable
    internal class RemoteHostClientFactoryOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteHostClientFactoryOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            RemoteHostClientFactoryOptions.RemoteHost_InProc);
    }
}
