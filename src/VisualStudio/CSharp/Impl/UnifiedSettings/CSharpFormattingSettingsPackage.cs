// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnifiedSettings;

// Each package can register only one settings manifest. The callbacks and preview factory
// are supplied by CSharpCodeStyleSettingsPackage, without changing CSharpPackage's manifest.
// Independent scalar settings use oneTimeFromLegacy: installed TextEditor.* IsRoamed/IsShared
// metadata upgrades it to FullWithoutDefaults. Only projections of the two combined bitfields
// remain external so that changing one flag preserves the other flags and unknown bits.
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid("46e38cde-eded-4ac0-afd4-e7995c8d4092")]
[ProvideSettingsManifest(PackageRelativeManifestFile = @"UnifiedSettings\csharpFormattingSettings.registration.json")]
internal sealed class CSharpFormattingSettingsPackage : AsyncPackage
{
}
