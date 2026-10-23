' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.LanguageServices.Options
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnifiedSettings
    <PackageRegistration(UseManagedResourcesOnly:=True, AllowsBackgroundLoading:=True)>
    <Guid(VisualBasicUnifiedSettingsPackage.PackageId)>
    <ProvideMenuResource("UnifiedSettingsCommands.ctmenu", 1)>
    <ProvideSettingsManifest(PackageRelativeManifestFile:="UnifiedSettings\visualBasicUnifiedSettings.registration.json")>
    <ProvideService(GetType(VisualBasicUnifiedSettingsPackage.ProviderService), IsAsyncQueryable:=True)>
    <ProvideService(GetType(VisualBasicUnifiedSettingsPackage.MigratorService), IsAsyncQueryable:=True)>
    Friend NotInheritable Class VisualBasicUnifiedSettingsPackage
        Inherits UnifiedSettingsPackage

        Public Const PackageId As String = "20e17bb0-890f-45f2-8390-55c6c3fe1053"
        Public Const PreviewFactoryId As String = "6d095b36-00cc-4c19-9ec4-99f81806d4b4"
        Public Const ProviderServiceId As String = "1492a80e-c752-4e11-916b-a72638217936"
        Public Const MigratorServiceId As String = "2e179285-05cc-44f1-9863-c9c9d48ce43c"
        Private _provider As CodeStyleSettingsProvider

        Protected Overrides ReadOnly Property Language As String = LanguageNames.VisualBasic
        Protected Overrides ReadOnly Property CommandSet As Guid = New Guid(PackageId)
        Protected Overrides ReadOnly Property PreviewFactory As Guid = New Guid(PreviewFactoryId)
        Protected Overrides ReadOnly Property ProviderServiceType As Type = GetType(ProviderService)
        Protected Overrides ReadOnly Property MigratorServiceType As Type = GetType(MigratorService)

        Protected Overrides Function CreatePreviewFactory() As CodeStyleSettingsPreviewFactory
            Return New CodeStyleSettingsPreviewFactory(Me, Function(elementId) GetProvider(), Function() GetProvider().CreatePreviewStore(), Function(elementId, options) New StyleViewModel(options, Me), 0, DisposalToken)
        End Function

        Private Function GetProvider() As CodeStyleSettingsProvider
            ThreadHelper.ThrowIfNotOnUIThread()
            If _provider Is Nothing Then
                _provider = CreateProvider()
            End If
            Return _provider
        End Function

        Private Function CreateProvider() As CodeStyleSettingsProvider
            Dim store = CodeStyleSettingsProvider.CreateMetadataStore(Me, GlobalOptions, LanguageNames.VisualBasic)
            Dim settings As New List(Of CodeStyleSetting)
            Using model As New StyleViewModel(store, Me)
                For Each item In model.CodeStyleItems
                    settings.Add(CodeStyleSettingsProvider.CreateStyleSetting(item, LanguageNames.VisualBasic))
                Next
            End Using
            Return New CodeStyleSettingsProvider(GlobalOptions, LanguageNames.VisualBasic, settings, threadingContext:=ThreadingContext)
        End Function

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                _provider?.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub

        <Guid(ProviderServiceId)>
        Friend NotInheritable Class ProviderService
        End Class

        <Guid(MigratorServiceId)>
        Friend NotInheritable Class MigratorService
        End Class
    End Class
End Namespace
