// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.VisualStudio.LanguageServices.KeybindingReset
{
    [ExportGlobalOptionProvider, Shared]
    internal sealed class KeybindingResetOptions : IOptionProvider
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\KeybindingsStatus\";

        public static readonly Option2<ReSharperStatus> ReSharperStatus = new(nameof(KeybindingResetOptions),
            nameof(ReSharperStatus), defaultValue: KeybindingReset.ReSharperStatus.NotInstalledOrDisabled,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(ReSharperStatus)));

        public static readonly Option2<bool> NeedsReset = new(nameof(KeybindingResetOptions),
            nameof(NeedsReset), defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(NeedsReset)));

        public static readonly Option2<bool> NeverShowAgain = new(nameof(KeybindingResetOptions),
            nameof(NeverShowAgain), defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(NeverShowAgain)));

        public static readonly Option2<bool> EnabledFeatureFlag = new(nameof(KeybindingResetOptions),
            nameof(EnabledFeatureFlag), defaultValue: false,
            storageLocation: new FeatureFlagStorageLocation("Roslyn.KeybindingResetEnabled"));

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            ReSharperStatus,
            NeedsReset,
            NeverShowAgain,
            EnabledFeatureFlag);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public KeybindingResetOptions()
        {
        }
    }
}
