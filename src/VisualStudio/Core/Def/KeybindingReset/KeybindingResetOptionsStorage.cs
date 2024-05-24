// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.KeybindingReset;

internal sealed class KeybindingResetOptionsStorage
{
    public static readonly Option2<ReSharperStatus> ReSharperStatus = new("visual_studio_resharper_key_binding_status", defaultValue: KeybindingReset.ReSharperStatus.NotInstalledOrDisabled);
    public static readonly Option2<bool> NeedsReset = new("visual_studio_key_binding_needs_reset", defaultValue: false);
    public static readonly Option2<bool> NeverShowAgain = new("visual_studio_key_binding_reset_never_show_again", defaultValue: false);
    public static readonly Option2<bool> EnabledFeatureFlag = new("visual_studio_enable_key_binding_reset", defaultValue: false);
}
