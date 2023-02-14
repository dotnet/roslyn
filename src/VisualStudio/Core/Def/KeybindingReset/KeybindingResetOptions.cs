// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.KeybindingReset
{
    internal sealed class KeybindingResetOptions
    {
        public static readonly Option2<ReSharperStatus> ReSharperStatus = new("dotnet_key_Binding_reset_options_resharper_status", defaultValue: KeybindingReset.ReSharperStatus.NotInstalledOrDisabled);
        public static readonly Option2<bool> NeedsReset = new("dotnet_key_binding_reset_options_needs_reset", defaultValue: false);
        public static readonly Option2<bool> NeverShowAgain = new("dotnet_key_binding_reset_options_never_show_again", defaultValue: false);
        public static readonly Option2<bool> EnabledFeatureFlag = new("dotnet_key_binding_reset_options_enabled_feature_flag", defaultValue: false);
    }
}
