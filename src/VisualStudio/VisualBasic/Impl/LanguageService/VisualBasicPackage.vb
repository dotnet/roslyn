﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Design
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.Options
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
    <ProvideService(GetType(IVbTempPECompilerFactory), IsAsyncQueryable:=False, IsCacheable:=True, IsFreeThreaded:=True, ServiceName:="Visual Basic TempPE Compiler Factory Service")>
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

        Protected Overrides Sub RegisterInitializeAsyncWork(packageInitializationTasks As PackageLoadTasks)

            MyBase.RegisterInitializeAsyncWork(packageInitializationTasks)

            packageInitializationTasks.AddTask(
                isMainThreadTask:=False,
                task:=Function() As Task
                          Try
                              RegisterLanguageService(GetType(IVbCompilerService), Function() Task.FromResult(_comAggregate))

                              DirectCast(Me, IServiceContainer).AddService(
                                  GetType(IVbTempPECompilerFactory),
                                  Function(_1, _2) New TempPECompilerFactory(Me.ComponentModel.GetService(Of VisualStudioWorkspace)()))
                          Catch ex As Exception When FatalError.ReportAndPropagateUnlessCanceled(ex)
                              Throw ExceptionUtilities.Unreachable
                          End Try

                          Return Task.CompletedTask
                      End Function)
        End Sub

        Protected Overrides Sub RegisterObjectBrowserLibraryManager()
            Dim workspace As VisualStudioWorkspace = ComponentModel.GetService(Of VisualStudioWorkspace)()

            Contract.ThrowIfFalse(JoinableTaskFactory.Context.IsOnMainThread)

            Dim objectManager = TryCast(GetService(GetType(SVsObjectManager)), IVsObjectManager2)
            If objectManager IsNot Nothing Then
                Me._libraryManager = New ObjectBrowserLibraryManager(Me, ComponentModel, workspace)

                If ErrorHandler.Failed(objectManager.RegisterSimpleLibrary(Me._libraryManager, Me._libraryManagerCookie)) Then
                    Me._libraryManagerCookie = 0
                End If
            End If
        End Sub

        Protected Overrides Sub UnregisterObjectBrowserLibraryManager()
            Contract.ThrowIfFalse(JoinableTaskFactory.Context.IsOnMainThread)

            If _libraryManagerCookie <> 0 Then
                Dim objectManager = TryCast(GetService(GetType(SVsObjectManager)), IVsObjectManager2)
                If objectManager IsNot Nothing Then
                    objectManager.UnregisterLibrary(Me._libraryManagerCookie)
                    Me._libraryManagerCookie = 0
                End If

                Me._libraryManager.Dispose()
                Me._libraryManager = Nothing
            End If
        End Sub

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
