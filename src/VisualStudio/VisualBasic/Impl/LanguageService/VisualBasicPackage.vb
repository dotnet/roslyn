' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Options.Formatting
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Task = System.Threading.Tasks.Task

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic

    ' The option page configuration is duplicated in PackageRegistration.pkgdef.
    '
    ' VB option pages tree
    '   Visual Basic
    '     General (from editor)
    '     Scroll Bars (from editor)
    '     Tabs (from editor)
    '     Advanced
    '     Code Style (category)
    '       General
    '       Naming
    '     IntelliSense
    <ProvideLanguageEditorOptionPage(GetType(AdvancedOptionPage), "Basic", Nothing, "Advanced", "#102", 10160)>
    <ProvideLanguageEditorToolsOptionCategory("Basic", "Code Style", "#109")>
    <ProvideLanguageEditorOptionPage(GetType(CodeStylePage), "Basic", "Code Style", "General", "#111", 10161)>
    <ProvideLanguageEditorOptionPage(GetType(NamingStylesOptionPage), "Basic", "Code Style", "Naming", "#110", 10162)>
    <ProvideLanguageEditorOptionPage(GetType(IntelliSenseOptionPage), "Basic", Nothing, "IntelliSense", "#112", 312)>
    <ProvideSettingsManifest(PackageRelativeManifestFile:="UnifiedSettings\visualBasicSettings.registration.json")>
    <Guid(Guids.VisualBasicPackageIdString)>
    Friend NotInheritable Class VisualBasicPackage
        Inherits AbstractPackage(Of VisualBasicPackage, VisualBasicLanguageService)
        Implements IVbCompilerService
        Implements IVsUserSettingsQuery

        ' The property page for WinForms project has a special interface that it uses to get
        ' entry points that are Forms: IVbEntryPointProvider. The semantics for this
        ' interface are the same as VisualBasicProject::GetFormEntryPoints, but callers get
        ' the VB Package and cast it to IVbEntryPointProvider. The property page is managed
        ' and we've redefined the interface, so we have to register a COM aggregate of the
        ' VB package. This is the same pattern we use for the LanguageService and Razor.
        Private ReadOnly _comAggregate As Object

        Private _libraryManager As ObjectBrowserLibraryManager
        Private _libraryManagerCookie As UInteger

        Public Sub New()
            MyBase.New()

            ' This is a UI-affinitized operation. Currently this opeartion prevents setting AllowsBackgroundLoad for
            ' VisualBasicPackage. The call should be removed from the constructor, and the package set back to allowing
            ' background loads.
            _comAggregate = Implementation.Interop.ComAggregate.CreateAggregatedObject(Me)
        End Sub

        Protected Overrides Async Function InitializeAsync(cancellationToken As CancellationToken, progress As IProgress(Of ServiceProgressData)) As Task
            Try
                Await MyBase.InitializeAsync(cancellationToken, progress).ConfigureAwait(True)

                RegisterLanguageService(GetType(IVbCompilerService), Function() Task.FromResult(_comAggregate))

                RegisterService(Of IVbTempPECompilerFactory)(
                    Async Function(ct)
                        Dim workspace = Me.ComponentModel.GetService(Of VisualStudioWorkspace)()
                        Await JoinableTaskFactory.SwitchToMainThreadAsync(ct)
                        Return New TempPECompilerFactory(workspace)
                    End Function)
            Catch ex As Exception When FatalError.ReportAndPropagateUnlessCanceled(ex)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Function

        Protected Overrides Async Function RegisterObjectBrowserLibraryManagerAsync(cancellationToken As CancellationToken) As Task
            Dim workspace As VisualStudioWorkspace = ComponentModel.GetService(Of VisualStudioWorkspace)()

            Await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)

            Dim objectManager = TryCast(Await GetServiceAsync(GetType(SVsObjectManager)).ConfigureAwait(True), IVsObjectManager2)
            If objectManager IsNot Nothing Then
                Me._libraryManager = New ObjectBrowserLibraryManager(Me, ComponentModel, workspace)

                If ErrorHandler.Failed(objectManager.RegisterSimpleLibrary(Me._libraryManager, Me._libraryManagerCookie)) Then
                    Me._libraryManagerCookie = 0
                End If
            End If
        End Function

        Protected Overrides Async Function UnregisterObjectBrowserLibraryManagerAsync(cancellationToken As CancellationToken) As Task
            Await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)

            If _libraryManagerCookie <> 0 Then
                Dim objectManager = TryCast(Await GetServiceAsync(GetType(SVsObjectManager)).ConfigureAwait(True), IVsObjectManager2)
                If objectManager IsNot Nothing Then
                    objectManager.UnregisterLibrary(Me._libraryManagerCookie)
                    Me._libraryManagerCookie = 0
                End If

                Me._libraryManager.Dispose()
                Me._libraryManager = Nothing
            End If
        End Function

        Public Function NeedExport(pageID As String, <Out> ByRef needExportParam As Integer) As Integer Implements IVsUserSettingsQuery.NeedExport
            ' We need to override MPF's definition of NeedExport since it doesn't know about our automation object
            needExportParam = If(pageID = "TextEditor.Basic-Specific", 1, 0)
            Return VSConstants.S_OK
        End Function

        Protected Overrides Function GetAutomationObject(name As String) As Object
            If name = "Basic-Specific" Then
                Return New AutomationObject(ComponentModel.GetService(Of ILegacyGlobalOptionService))
            End If

            Return MyBase.GetAutomationObject(name)
        End Function

        Protected Overrides Function CreateEditorFactories() As IEnumerable(Of IVsEditorFactory)
            Dim editorFactory = New VisualBasicEditorFactory(Me.ComponentModel)
            Dim codePageEditorFactory = New VisualBasicCodePageEditorFactory(editorFactory)

            Return {editorFactory, codePageEditorFactory}
        End Function

        Protected Overrides Function CreateLanguageService() As VisualBasicLanguageService
            Return New VisualBasicLanguageService(Me)
        End Function

        Protected Overrides Sub RegisterMiscellaneousFilesWorkspaceInformation(miscellaneousFilesWorkspace As MiscellaneousFilesWorkspace)
            miscellaneousFilesWorkspace.RegisterLanguage(
                Guids.VisualBasicLanguageServiceId,
                LanguageNames.VisualBasic,
                ".vbx")
        End Sub

        Protected Overrides ReadOnly Property RoslynLanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
