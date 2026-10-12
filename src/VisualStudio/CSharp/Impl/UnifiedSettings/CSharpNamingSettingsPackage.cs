// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnifiedSettings;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageId)]
[ProvideMenuResource("NamingSettingsCommands.ctmenu", 1)]
[ProvideSettingsManifest(PackageRelativeManifestFile = @"UnifiedSettings\csharpNamingSettings.registration.json")]
[ProvideService(typeof(ProviderService), IsAsyncQueryable = true)]
[ProvideService(typeof(MigratorService), IsAsyncQueryable = true)]
internal sealed class CSharpNamingSettingsPackage : NamingSettingsPackage
{
    public const string PackageId = "2c96c85f-e680-40e4-9b8a-c2fd55dac0f1";
    public const string ProviderServiceId = "c573a3f9-63f0-4297-9eb4-eaa2f16b87d7";
    public const string MigratorServiceId = "cf41c6e3-6a8f-43b0-bdb9-2325ec46e6cc";
    protected override string Language => LanguageNames.CSharp;
    protected override Guid CommandSet => new(PackageId);
    protected override Type ProviderServiceType => typeof(ProviderService);
    protected override Type MigratorServiceType => typeof(MigratorService);

    [Guid(ProviderServiceId)]
    private sealed class ProviderService;
    [Guid(MigratorServiceId)]
    private sealed class MigratorService;
}
