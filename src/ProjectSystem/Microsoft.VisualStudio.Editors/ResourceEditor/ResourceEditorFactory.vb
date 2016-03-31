Option Explicit On
Option Strict On
Option Compare Binary
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Editors.Interop


Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' The editor factory for the resource editor.  The job of this class is
    '''   simply to create a new resource editor designer when requested by the
    '''   shell.
    ''' </summary>
    ''' <remarks></remarks>
    <CLSCompliant(False), _
    Guid("ff4d6aca-9352-4a5f-821e-f4d6ebdcab11")> _
    Friend NotInheritable Class ResourceEditorFactory
        Inherits DesignerFramework.BaseEditorFactory
        Implements IVsTrackProjectDocumentsEvents2

        ' The editor factory GUID.  This guid must be unique for each editor (and hence editor factory)
        Friend Const ResourceEditor_EditorGuid As String = "ff4d6aca-9352-4a5f-821e-f4d6ebdcab11"


        Private vsTrackProjectDocumentsEventsCookie As UInt32
        Private vsTrackProjectDocuments As IVsTrackProjectDocuments2

        ''' <summary>
        ''' Creates and registers a new editor factory.  This is called
        '''   by the DesignerPackage when it gets sited.
        ''' We pass in our designer loader type to the base.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New()
            MyBase.New(GetType(ResourceEditorDesignerLoader))
        End Sub


        ''' <summary>
        ''' Provides the (constant) GUID for the subclassed editor factory.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' Must overridde the base.  Be sure to use the same GUID on the GUID attribute
        '''    attached to the inheriting class.
        ''' </remarks>
        Protected Overrides ReadOnly Property EditorGuid() As System.Guid
            Get
                Return New Guid(ResourceEditor_EditorGuid)
            End Get
        End Property


        ''' <summary>
        ''' Provides the (constant) GUID for the command UI.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected Overrides ReadOnly Property CommandUIGuid() As System.Guid
            Get
                'This is required for key bindings hook-up to work properly.
                Return Constants.MenuConstants.GUID_RESXEditorCommandUI
            End Get
        End Property



        Protected Overrides Sub OnSited()
            If vsTrackProjectDocuments Is Nothing AndAlso Not Me.ServiceProvider Is Nothing Then
                vsTrackProjectDocuments = TryCast(Me.ServiceProvider.GetService(GetType(SVsTrackProjectDocuments)), IVsTrackProjectDocuments2)
                If Not (vsTrackProjectDocuments Is Nothing) Then
                    ErrorHandler.ThrowOnFailure(vsTrackProjectDocuments.AdviseTrackProjectDocumentsEvents(Me, vsTrackProjectDocumentsEventsCookie))
                    Debug.Assert(vsTrackProjectDocumentsEventsCookie <> 0)
                End If
            End If
        End Sub 'OnSited


        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If vsTrackProjectDocumentsEventsCookie <> 0 Then
                    If Not (vsTrackProjectDocuments Is Nothing) Then
                        ErrorHandler.ThrowOnFailure(vsTrackProjectDocuments.UnadviseTrackProjectDocumentsEvents(vsTrackProjectDocumentsEventsCookie))
                        vsTrackProjectDocumentsEventsCookie = 0
                        vsTrackProjectDocuments = Nothing
                    End If
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub 'Dispose

#Region "IVsRunningDocTableEvents2 Implementation"
        ' The following code is stripped from SettingsGlobalObjectProvider
        ' 
        Private Function OnAfterAddDirectoriesEx(ByVal cProjects As Integer, ByVal cDirectories As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSADDDIRECTORYFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterAddDirectoriesEx
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterAddFilesEx(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSADDFILEFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRemoveDirectories(ByVal cProjects As Integer, ByVal cDirectories As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSREMOVEDIRECTORYFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterRemoveDirectories
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRemoveFiles(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSREMOVEFILEFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRenameDirectories(ByVal cProjects As Integer, ByVal cDirs As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As Shell.Interop.VSRENAMEDIRECTORYFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterRenameDirectories
            Return NativeMethods.S_OK
        End Function

        ' If the resource file is renamed/moved while it's open in the editor, we need to force a reload so that we pick up
        ' the correct new location for relative linked files
        Private Function OnAfterRenameFiles(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As Shell.Interop.VSRENAMEFILEFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles
            ' Validate arguments....
            Debug.Assert(rgpProjects IsNot Nothing AndAlso rgpProjects.Length = cProjects, "null rgpProjects or bad-length array")
            If (rgpProjects Is Nothing) Then Throw New ArgumentNullException("rgpProjects")
            If (rgpProjects.Length <> cProjects) Then Throw Common.CreateArgumentException("rgpProjects")

            Debug.Assert(rgFirstIndices IsNot Nothing AndAlso rgFirstIndices.Length = cProjects, "null rgFirstIndices or bad-length array")
            If (rgFirstIndices Is Nothing) Then Throw New ArgumentNullException("rgFirstIndices")
            If (rgFirstIndices.Length <> cProjects) Then Throw Common.CreateArgumentException("rgFirstIndices")

            Debug.Assert(rgszMkOldNames IsNot Nothing AndAlso rgszMkOldNames.Length = cFiles, "null rgszMkOldNames or bad-length array")
            If (rgszMkOldNames Is Nothing) Then Throw New ArgumentNullException("rgszMkOldNames")
            If (rgszMkOldNames.Length <> cFiles) Then Throw Common.CreateArgumentException("rgszMkOldNames")

            Debug.Assert(rgszMkNewNames IsNot Nothing AndAlso rgszMkNewNames.Length = cFiles, "null rgszMkNewNames or bad-length array")
            If (rgszMkNewNames Is Nothing) Then Throw New ArgumentNullException("rgszMkNewNames")
            If (rgszMkNewNames.Length <> cFiles) Then Throw Common.CreateArgumentException("rgszMkNewNames")

            Debug.Assert(rgFlags IsNot Nothing AndAlso rgFlags.Length = cFiles, "null rgFlags or bad-length array")
            If (rgFlags Is Nothing) Then Throw New ArgumentNullException("rgFlags")
            If (rgFlags.Length <> cFiles) Then Throw Common.CreateArgumentException("rgFlags")

            For i As Integer = 0 To cFiles - 1
                If Utility.HasResourceFileExtension(rgszMkNewNames(i)) Then
                    Dim designerEventService As IDesignerEventService = TryCast(Me.ServiceProvider.GetService(GetType(IDesignerEventService)), IDesignerEventService)
                    Debug.Assert(Not designerEventService Is Nothing)
                    If (Not designerEventService Is Nothing) Then
                        For Each host As IDesignerHost In designerEventService.Designers
                            Dim comp As ResourceEditorRootComponent = TryCast(host.RootComponent, ResourceEditorRootComponent)
                            If (Not comp Is Nothing AndAlso (String.Equals(rgszMkNewNames(i), comp.ResourceFileName, StringComparison.Ordinal) OrElse String.Equals(rgszMkOldNames(i), comp.ResourceFileName, StringComparison.Ordinal))) Then
                                Dim loaderService As IDesignerLoaderService = TryCast(host.GetService(GetType(IDesignerLoaderService)), IDesignerLoaderService)
                                If (Not loaderService Is Nothing) Then
                                    comp.RootDesigner.IsInReloading = True
                                    loaderService.Reload()
                                End If
                            End If
                        Next
                    End If
                End If
            Next
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterSccStatusChanged(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgdwSccStatus() As UInteger) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterSccStatusChanged
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryAddDirectories(ByVal pProject As Shell.Interop.IVsProject, ByVal cDirectories As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSQUERYADDDIRECTORYFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYADDDIRECTORYRESULTS, ByVal rgResults() As Shell.Interop.VSQUERYADDDIRECTORYRESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryAddDirectories
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryAddFiles(ByVal pProject As Shell.Interop.IVsProject, ByVal cFiles As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSQUERYADDFILEFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYADDFILERESULTS, ByVal rgResults() As Shell.Interop.VSQUERYADDFILERESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryAddFiles
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRemoveDirectories(ByVal pProject As Shell.Interop.IVsProject, ByVal cDirectories As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSQUERYREMOVEDIRECTORYFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYREMOVEDIRECTORYRESULTS, ByVal rgResults() As Shell.Interop.VSQUERYREMOVEDIRECTORYRESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryRemoveDirectories
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRemoveFiles(ByVal pProject As Shell.Interop.IVsProject, ByVal cFiles As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSQUERYREMOVEFILEFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYREMOVEFILERESULTS, ByVal rgResults() As Shell.Interop.VSQUERYREMOVEFILERESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryRemoveFiles
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRenameDirectories(ByVal pProject As Shell.Interop.IVsProject, ByVal cDirs As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As Shell.Interop.VSQUERYRENAMEDIRECTORYFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYRENAMEDIRECTORYRESULTS, ByVal rgResults() As Shell.Interop.VSQUERYRENAMEDIRECTORYRESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryRenameDirectories
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRenameFiles(ByVal pProject As Shell.Interop.IVsProject, ByVal cFiles As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As Shell.Interop.VSQUERYRENAMEFILEFLAGS, ByVal pSummaryResult() As Shell.Interop.VSQUERYRENAMEFILERESULTS, ByVal rgResults() As Shell.Interop.VSQUERYRENAMEFILERESULTS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnQueryRenameFiles
            Return NativeMethods.S_OK
        End Function


#End Region

        ''' <summary>
        ''' This method is called by the Environment (inside IVsUIShellOpenDocument::
        ''' OpenStandardEditor and OpenSpecificEditor) to map a LOGICAL view to a 
        ''' PHYSICAL view. A LOGICAL view identifies the purpose of the view that is
        ''' desired (e.g. a view appropriate for Debugging [LOGVIEWID_Debugging], or a 
        ''' view appropriate for text view manipulation as by navigating to a find
        ''' result [LOGVIEWID_TextView]). A PHYSICAL view identifies an actual type 
        ''' of view implementation that an IVsEditorFactory can create. 
        ''' 	
        ''' NOTE: Physical views are identified by a string of your choice with the 
        ''' one constraint that the default/primary physical view for an editor  
        ''' *MUST* use a NULL string as its physical view name (*pbstrPhysicalView = NULL).
        ''' 	
        ''' NOTE: It is essential that the implementation of MapLogicalView properly
        ''' validates that the LogicalView desired is actually supported by the editor.
        ''' If an unsupported LogicalView is requested then E_NOTIMPL must be returned.
        ''' 	
        ''' NOTE: The special Logical Views supported by an Editor Factory must also 
        ''' be registered in the local registry hive. LOGVIEWID_Primary is implicitly 
        ''' supported by all editor types and does not need to be registered.
        ''' For example, an editor that supports a ViewCode/ViewDesigner scenario
        ''' might register something like the following:
        ''' HKLM\Software\Microsoft\VisualStudio\[CurrentVSVersion]\Editors\
        ''' {...guidEditor...}\
        ''' LogicalViews\
        ''' {...LOGVIEWID_TextView...} = s ''
        ''' {...LOGVIEWID_Code...} = s ''
        ''' {...LOGVIEWID_Debugging...} = s ''
        ''' {...LOGVIEWID_Designer...} = s 'Form'
        ''' </summary>
        Protected Overrides Function MapLogicalView(ByRef LogicalView As System.Guid, ByRef PhysicalViewOut As String) As Integer

            'The default view must have the value of Nothing.
            PhysicalViewOut = Nothing

            If (LogicalView.Equals(LOGVIEWID.LOGVIEWID_Primary) OrElse LogicalView.Equals(LOGVIEWID.LOGVIEWID_Designer)) Then
                ' if it's primary or designer, then that's our bread & butter, so return S_OK
                '
                Return NativeMethods.S_OK
            Else
                ' anything else should return E_NOTIMPL
                '
                Return Microsoft.VisualStudio.Editors.Interop.NativeMethods.E_NOTIMPL
            End If
        End Function

    End Class
End Namespace
