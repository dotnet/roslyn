// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    [Export(typeof(PythiaGlobalOptions)), Shared]
    internal sealed class PythiaGlobalOptions
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PythiaGlobalOptions(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public bool ShowDebugInfo
        {
            get => _globalOptions.GetOption(s_showDebugInfoOption);
            set => _globalOptions.SetGlobalOption(new OptionKey(s_showDebugInfoOption), value);
        }

        public bool RemoveRecommendationLimit
        {
            get => _globalOptions.GetOption(s_removeRecommendationLimitOption);
            set => _globalOptions.SetGlobalOption(new OptionKey(s_removeRecommendationLimitOption), value);
        }

        public const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        private static readonly Option2<bool> s_showDebugInfoOption = new(
            "InternalFeatureOnOffOptions", "ShowDebugInfo", defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "ShowDebugInfo"));

        private static readonly Option2<bool> s_removeRecommendationLimitOption = new(
            "InternalFeatureOnOffOptions", "RemoveRecommendationLimit", defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "RemoveRecommendationLimit"));
    }
}
