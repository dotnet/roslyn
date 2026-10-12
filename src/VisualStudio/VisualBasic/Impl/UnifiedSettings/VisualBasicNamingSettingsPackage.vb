' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnifiedSettings
    <PackageRegistration(UseManagedResourcesOnly:=True, AllowsBackgroundLoading:=True)>
    <Guid(VisualBasicNamingSettingsPackage.PackageId)>
    <ProvideMenuResource("NamingSettingsCommands.ctmenu", 1)>
    <ProvideSettingsManifest(PackageRelativeManifestFile:="UnifiedSettings\visualBasicNamingSettings.registration.json")>
    <ProvideService(GetType(VisualBasicNamingSettingsPackage.ProviderService), IsAsyncQueryable:=True)>
    <ProvideService(GetType(VisualBasicNamingSettingsPackage.MigratorService), IsAsyncQueryable:=True)>
    Friend NotInheritable Class VisualBasicNamingSettingsPackage
        Inherits NamingSettingsPackage

        Public Const PackageId As String = "f2450d5f-8884-47e3-807a-91fb9f3c0df0"
        Public Const ProviderServiceId As String = "1492a80e-c752-4e11-916b-a72638217936"
        Public Const MigratorServiceId As String = "2e179285-05cc-44f1-9863-c9c9d48ce43c"

        Protected Overrides ReadOnly Property Language As String = LanguageNames.VisualBasic
        Protected Overrides ReadOnly Property CommandSet As Guid = New Guid(PackageId)
        Protected Overrides ReadOnly Property ProviderServiceType As Type = GetType(ProviderService)
        Protected Overrides ReadOnly Property MigratorServiceType As Type = GetType(MigratorService)

        <Guid(ProviderServiceId)>
        Friend NotInheritable Class ProviderService
        End Class

        <Guid(MigratorServiceId)>
        Friend NotInheritable Class MigratorService
        End Class
    End Class
End Namespace
