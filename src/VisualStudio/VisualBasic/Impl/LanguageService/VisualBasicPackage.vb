﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Utilities
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Options.Formatting
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Task = System.Threading.Tasks.Task

' NOTE(DustinCa): The EditorFactory registration is in VisualStudioComponents\VisualBasicPackageRegistration.pkgdef.
' The reason for this is because the ProvideEditorLogicalView does not allow a name value to specified in addition to
' its GUID. This name value is used to identify untrusted logical views and link them to their physical view attributes.
' The net result is that using the attributes only causes designers to be loaded in the preview tab, even when they
' shouldn't be.

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    ' TODO(DustinCa): Put all of this in VisualBasicPackageRegistration.pkgdef rather than using attributes
    ' (See setupauthoring\vb\components\vblanguageservice.pkgdef for an example).

    ' VB option pages tree
    '   Basic
    '     General (from editor)
    '     Scroll Bars (from editor)
    '     Tabs (from editor)
    '     Advanced
    '     Code Style (category)
    '       General
    '       Naming

    <Guid(Guids.VisualBasicPackageIdString)>
    <PackageRegistration(UseManagedResourcesOnly:=True, AllowsBackgroundLoading:=True)>
    <ProvideRoslynVersionRegistration(Guids.VisualBasicPackageIdString, "Microsoft Visual Basic", 113, 114)>
    <ProvideLanguageExtension(GetType(VisualBasicLanguageService), ".bas")>
    <ProvideLanguageExtension(GetType(VisualBasicLanguageService), ".cls")>
    <ProvideLanguageExtension(GetType(VisualBasicLanguageService), ".ctl")>
    <ProvideLanguageExtension(GetType(VisualBasicLanguageService), ".dob")>
    <ProvideLanguageExtension(GetType(VisualBasicLanguageService), ".dsr")>
    <ProvideLanguageExtension(GetType(VisualBasicLanguageService), ".frm")>
    <ProvideLanguageExtension(GetType(VisualBasicLanguageService), ".pag")>
    <ProvideLanguageExtension(GetType(VisualBasicLanguageService), ".vb")>
    <ProvideLanguageEditorOptionPage(GetType(AdvancedOptionPage), "Basic", Nothing, "Advanced", "#102", 10160)>
    <ProvideLanguageEditorToolsOptionCategory("Basic", "Code Style", "#109")>
    <ProvideLanguageEditorOptionPage(GetType(CodeStylePage), "Basic", "Code Style", "General", "#111", 10161)>
    <ProvideLanguageEditorOptionPage(GetType(NamingStylesOptionPage), "Basic", "Code Style", "Naming", "#110", 10162)>
    <ProvideLanguageEditorOptionPage(GetType(IntelliSenseOptionPage), "Basic", Nothing, "IntelliSense", "#112", 312)>
    <ProvideAutomationProperties("TextEditor", "Basic", Guids.TextManagerPackageString, 103, 105, Guids.VisualBasicPackageIdString)>
    <ProvideAutomationProperties("TextEditor", "Basic-Specific", Guids.VisualBasicPackageIdString, 104, 106)>
    <ProvideService(GetType(VisualBasicLanguageService), ServiceName:="Visual Basic Language Service", IsAsyncQueryable:=True)>
    <ProvideService(GetType(IVbCompilerService), ServiceName:="Visual Basic Project System Shim", IsAsyncQueryable:=True)>
    <ProvideService(GetType(IVbTempPECompilerFactory), ServiceName:="Visual Basic TempPE Compiler Factory Service", IsAsyncQueryable:=True)>
    Friend Class VisualBasicPackage
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

            _comAggregate = Interop.ComAggregate.CreateAggregatedObject(Me)
        End Sub

        Protected Overrides Function CreateWorkspace() As VisualStudioWorkspaceImpl
            Return Me.ComponentModel.GetService(Of VisualStudioWorkspaceImpl)
        End Function

        Protected Overrides Async Function InitializeAsync(cancellationToken As CancellationToken, progress As IProgress(Of ServiceProgressData)) As Task
            Try
                Await MyBase.InitializeAsync(cancellationToken, progress).ConfigureAwait(True)
                Await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)

                RegisterLanguageService(GetType(IVbCompilerService), Function() Task.FromResult(_comAggregate))

                Dim workspace = Me.ComponentModel.GetService(Of VisualStudioWorkspaceImpl)()
                RegisterService(Of IVbTempPECompilerFactory)(
                    Async Function(ct)
                        Await JoinableTaskFactory.SwitchToMainThreadAsync(ct)
                        Return New TempPECompilerFactory(workspace)
                    End Function)

                Await RegisterObjectBrowserLibraryManagerAsync(cancellationToken).ConfigureAwait(True)
            Catch ex As Exception When FatalError.ReportUnlessCanceled(ex)
            End Try
        End Function

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                JoinableTaskFactory.Run(Function() UnregisterObjectBrowserLibraryManagerAsync(CancellationToken.None))
            End If

            MyBase.Dispose(disposing)
        End Sub

        Private Async Function RegisterObjectBrowserLibraryManagerAsync(cancellationToken As CancellationToken) As Task
            Await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)

            Dim objectManager = TryCast(Await GetServiceAsync(GetType(SVsObjectManager)).ConfigureAwait(True), IVsObjectManager2)
            If objectManager IsNot Nothing Then
                Me._libraryManager = New ObjectBrowserLibraryManager(Me, ComponentModel, Workspace)

                If ErrorHandler.Failed(objectManager.RegisterSimpleLibrary(Me._libraryManager, Me._libraryManagerCookie)) Then
                    Me._libraryManagerCookie = 0
                End If
            End If
        End Function

        Private Async Function UnregisterObjectBrowserLibraryManagerAsync(cancellationToken As CancellationToken) As Task
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
                Dim workspace = Me.ComponentModel.GetService(Of VisualStudioWorkspace)()
                Return New AutomationObject(workspace)
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
