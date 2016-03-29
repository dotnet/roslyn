' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

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
    <PackageRegistration(UseManagedResourcesOnly:=True)>
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
    <ProvideLanguageEditorOptionPage(GetType(StyleOptionPage), "Basic", "Code Style", "General", "#111", 10161)>
    <ProvideLanguageEditorOptionPage(GetType(NamingStylesOptionPage), "Basic", "Code Style", "Naming", "#110", 10162)>
    <ProvideAutomationProperties("TextEditor", "Basic", Guids.TextManagerPackageString, 103, 105, Guids.VisualBasicPackageIdString)>
    <ProvideAutomationProperties("TextEditor", "Basic-Specific", Guids.VisualBasicPackageIdString, 104, 106)>
    <ProvideService(GetType(VisualBasicLanguageService), ServiceName:="Visual Basic Language Service")>
    <ProvideService(GetType(IVbCompilerService), ServiceName:="Visual Basic Project System Shim")>
    <ProvideService(GetType(IVbTempPECompilerFactory), ServiceName:="Visual Basic TempPE Compiler Factory Service")>
    Friend Class VisualBasicPackage
        Inherits AbstractPackage(Of VisualBasicPackage, VisualBasicLanguageService, VisualBasicProject)
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

        Protected Overrides Sub Initialize()
            Try
                MyBase.Initialize()

                RegisterLanguageService(GetType(IVbCompilerService), Function() _comAggregate)

                Dim workspace = Me.ComponentModel.GetService(Of VisualStudioWorkspaceImpl)()
                RegisterService(Of IVbTempPECompilerFactory)(Function() New TempPECompilerFactory(workspace))

                RegisterObjectBrowserLibraryManager()
            Catch ex As Exception When FatalError.Report(ex)
            End Try
        End Sub

        Protected Overrides Sub Dispose(disposing As Boolean)
            UnregisterObjectBrowserLibraryManager()

            MyBase.Dispose(disposing)
        End Sub

        Private Sub RegisterObjectBrowserLibraryManager()
            Dim objectManager = TryCast(Me.GetService(GetType(SVsObjectManager)), IVsObjectManager2)
            If objectManager IsNot Nothing Then
                Me._libraryManager = New ObjectBrowserLibraryManager(Me)

                If ErrorHandler.Failed(objectManager.RegisterSimpleLibrary(Me._libraryManager, Me._libraryManagerCookie)) Then
                    Me._libraryManagerCookie = 0
                End If
            End If
        End Sub

        Private Sub UnregisterObjectBrowserLibraryManager()
            If _libraryManagerCookie <> 0 Then
                Dim objectManager = TryCast(Me.GetService(GetType(SVsObjectManager)), IVsObjectManager2)
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
                Dim workspace = Me.ComponentModel.GetService(Of VisualStudioWorkspace)()
                Dim optionService = workspace.Services.GetService(Of IOptionService)()
                Return New AutomationObject(optionService)
            End If

            Return MyBase.GetAutomationObject(name)
        End Function

        Protected Overrides Function CreateEditorFactories() As IEnumerable(Of IVsEditorFactory)
            Dim editorFactory = New VisualBasicEditorFactory(Me)
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
                ".vbx",
                VisualBasicParseOptions.Default)
        End Sub

        Protected Overrides ReadOnly Property RoslynLanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
