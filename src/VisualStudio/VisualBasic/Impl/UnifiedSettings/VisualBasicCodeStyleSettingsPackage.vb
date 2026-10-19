' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Design
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions
Imports Microsoft.VisualStudio.LanguageServices.Options
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnifiedSettings
    <PackageRegistration(UseManagedResourcesOnly:=True, AllowsBackgroundLoading:=True)>
    <Guid(VisualBasicCodeStyleSettingsPackage.PackageId)>
    <ProvideMenuResource("CodeStyleSettingsCommands.ctmenu", 1)>
    <ProvideSettingsManifest(PackageRelativeManifestFile:="UnifiedSettings\visualBasicCodeStyleSettings.registration.json")>
    Friend NotInheritable Class VisualBasicCodeStyleSettingsPackage
        Inherits AsyncPackage

        Public Const PackageId As String = "20e17bb0-890f-45f2-8390-55c6c3fe1053"
        Public Const PreviewFactoryId As String = "6d095b36-00cc-4c19-9ec4-99f81806d4b4"
        Private _provider As CodeStyleSettingsProvider
        Private _commands As IDisposable

        Protected Overrides Async Function InitializeAsync(cancellationToken As CancellationToken, progress As IProgress(Of ServiceProgressData)) As Task
            Await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)
            Dim globalOptions = Me.GetMefService(Of IGlobalOptionService)()
            Dim store = CodeStyleSettingsProvider.CreateMetadataStore(Me, globalOptions, LanguageNames.VisualBasic)
            Dim settings As New List(Of CodeStyleSetting)
            Using model As New StyleViewModel(store, Me)
                For Each item In model.CodeStyleItems
                    settings.Add(CodeStyleSettingsProvider.CreateStyleSetting(item, LanguageNames.VisualBasic))
                Next
            End Using
            _provider = New CodeStyleSettingsProvider(globalOptions, LanguageNames.VisualBasic, settings, threadingContext:=Me.GetMefService(Of IThreadingContext)())
            Dim registrar = TryCast(Await GetServiceAsync(GetType(SVsUIFactory)).ConfigureAwait(True), IVsRegisterUIFactories)
            If registrar Is Nothing Then
                Throw New InvalidOperationException()
            End If
            Dim factoryId As New Guid(PreviewFactoryId)
            Dim factory = New CodeStyleSettingsPreviewFactory(Me, Function(elementId) _provider, Function() _provider.CreatePreviewStore(), Function(elementId, options) New StyleViewModel(options, Me), 0, DisposalToken)
            ErrorHandler.ThrowOnFailure(registrar.RegisterUIFactory(factoryId, factory))
            Dim commandService = TryCast(Await GetServiceAsync(GetType(IMenuCommandService)).ConfigureAwait(True), IMenuCommandService)
            If commandService Is Nothing Then
                Throw New InvalidOperationException()
            End If
            _commands = CodeStyleSettingsCommands.Register(commandService, New Guid(PackageId),
                Sub() CodeStyleSettingsCommands.GenerateEditorConfig(Me, factory.CreatePendingStore(), LanguageNames.VisualBasic),
                AddressOf CodeStyleSettingsCommands.ShowHelp, DisposalToken)
        End Function

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                _commands?.Dispose()
                _provider?.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub
    End Class
End Namespace
