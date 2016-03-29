'******************************************************************************
'* ApplicationDesignerEditorFactory.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports System
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Runtime.Serialization.Formatters
Imports System.Windows.Forms
Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Microsoft.VisualStudio.Editors.AppDesDesignerFramework
Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports LOGVIEWID = Microsoft.VisualStudio.Editors.AppDesInterop.LOGVIEWID

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    '**************************************************************************
    ';ApplicationDesignerEditorFactory
    '
    'Remarks:
    '   The editor factory for the resource editor.  The job of this class is
    '   simply to create a new resource editor designer when requested by the
    '   shell.
    '**************************************************************************
    <CLSCompliant(False), _
    Guid("04b8ab82-a572-4fef-95ce-5222444b6b64")> _
    Public NotInheritable Class ApplicationDesignerEditorFactory
        Implements IVsEditorFactory

        'The all important GUIDs 
        Private Shared ReadOnly m_EditorGuid As New Guid("{04b8ab82-a572-4fef-95ce-5222444b6b64}")
        Private Shared ReadOnly m_CommandUIGuid As New Guid("{d06cd5e3-d961-44dc-9d80-c89a1a8d9d56}")

        'Exposing the GUID for the rest of the assembly to see
        Public Shared ReadOnly Property EditorGuid() As Guid
            Get
                Return m_EditorGuid
            End Get
        End Property

        'Exposing the GUID for the rest of the assembly to see
        Public Shared ReadOnly Property CommandUIGuid() As Guid
            Get
                Return m_CommandUIGuid
            End Get
        End Property

        Private m_Site As Object 'The site that owns this editor factory
        Private m_SiteProvider As Shell.ServiceProvider 'The service provider from m_Site

        ''' <summary>
        ''' Creates a new editor for the given pile of flags.  Helper function for the overload
        ''' which implements IVsEditorFactory.CreateEditorInstance
        ''' </summary>
        ''' <param name="VsCreateEditorFlags"></param>
        ''' <param name="FileName">[In] Filename being opened</param>
        ''' <param name="PhysicalView"></param>
        ''' <param name="Hierarchy">[In] IVsHierarchy of node being opened</param>
        ''' <param name="ItemId">[In] ItemId for node being opened</param>
        ''' <param name="ExistingDocData">[In] Existing DocData if any</param>
        ''' <param name="DocView">Returns the IVsWindowPane object</param>
        ''' <param name="DocData">Returns DocData object</param>
        ''' <param name="Caption">Returns caption for document window</param>
        ''' <param name="CmdUIGuid">Returns guid for CMDUI</param>
        ''' <param name="pgrfCDW">[out] Flags to be passed to CreateDocumentWindow</param>
        ''' <remarks></remarks>
        Private Function InternalCreateEditorInstance(ByVal VsCreateEditorFlags As System.UInt32, _
                ByVal FileName As String, _
                ByVal PhysicalView As String, _
                ByVal Hierarchy As IVsHierarchy, _
                ByVal ItemId As System.UInt32, _
                ByVal ExistingDocData As Object, _
                ByRef DocView As Object, _
                ByRef DocData As Object, _
                ByRef Caption As String, _
                ByRef CmdUIGuid As System.Guid, _
                ByRef pgrfCDW As Integer) As Integer
            pgrfCDW = 0
            CmdUIGuid = System.Guid.Empty

            Dim DesignerLoader As ApplicationDesignerLoader = Nothing

            Try
                Using New Common.WaitCursor

                    DocView = Nothing
                    DocData = Nothing
                    Caption = Nothing

                    Dim DesignerService As IVSMDDesignerService = CType(m_SiteProvider.GetService(GetType(IVSMDDesignerService)), IVSMDDesignerService)
                    If DesignerService Is Nothing Then
                        Throw New Exception(SR.GetString(SR.DFX_EditorNoDesignerService, FileName))
                    End If

                    If ExistingDocData Is Nothing Then
                        'We do not support being loaded without a DocData on the project file being passed to us by
                        '  QI'ing for  IVsHierarchy.
                        Trace.WriteLine("*** ApplicationDesignerEditorFactory: ExistingDocData = Nothing, returning VS_E_UNSUPPORTEDFORMAT - we shouldn't be called this way")
                        Return VSErrorCodes.VS_E_UNSUPPORTEDFORMAT
                    Else
                        'Verify that the DocData passed in to us really is the project file
                        Dim VsHierarchy As IVsHierarchy = TryCast(ExistingDocData, IVsHierarchy)
                        If VsHierarchy Is Nothing Then
                            Debug.Fail("The DocData passed in to the project designer was not the project file - this is not supported.")
                            Return VSErrorCodes.VS_E_UNSUPPORTEDFORMAT
                        End If

                        DocData = ExistingDocData
                    End If

                    DesignerLoader = CType(DesignerService.CreateDesignerLoader(GetType(ApplicationDesignerLoader).AssemblyQualifiedName), ApplicationDesignerLoader)
                    DesignerLoader.InitializeEx(m_SiteProvider, Hierarchy, ItemId, DocData)
                    'If ExistingDocData IsNot Nothing Then
                    'Don't pass this value back
                    'DocData = Nothing
                    'End If

                    'Site the TextStream
                    'If TypeOf DocData Is IObjectWithSite Then
                    '   CType(DocData, IObjectWithSite).SetSite(m_Site)
                    'Else
                    '   Debug.Fail("DocData does not implement IObjectWithSite")
                    'End If

                    Dim OleProvider As OLE.Interop.IServiceProvider = CType(m_SiteProvider.GetService(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                    Dim Designer As IVSMDDesigner = DesignerService.CreateDesigner(OleProvider, DesignerLoader)

                    Debug.Assert(Not (Designer Is Nothing), "Designer service should have thrown if it had a problem.")

                    'Set the out params
                    DocView = Designer.View 'Gets the object that can support IVsWindowPane

                    'An empty caption allows the projectname to be used as the caption
                    'The OpenSpecificEditor call takes a "%1" for the user caption.  We currently use the 
                    ' project name
                    Caption = ""

                    'Set the command UI
                    CmdUIGuid = m_CommandUIGuid

                    'Flags to pass back, these flags get passed toCreateDocumentWindow.  We need these because of the
                    '  way the project designer is shown by the project system.
                    pgrfCDW = _VSRDTFLAGS.RDT_VirtualDocument Or _VSRDTFLAGS.RDT_ProjSlnDocument
                End Using

            Catch ex As Exception

                If DesignerLoader IsNot Nothing Then
                    'We need to let the DesignerLoader disconnect from events
                    DesignerLoader.Dispose()
                End If

                Throw New System.Exception(SR.GetString(SR.DFX_CreateEditorInstanceFailed_Ex, ex.Message))
            End Try
        End Function


        ''' <summary>
        ''' Disconnect from the owning site
        ''' </summary>
        ''' <remarks></remarks>
        Public Function Close() As Integer Implements Shell.Interop.IVsEditorFactory.Close
            m_SiteProvider = Nothing
            m_Site = Nothing
        End Function

        ''' <summary>
        ''' Wrapper of COM interface which delegates to Internal
        ''' </summary>
        ''' <remarks></remarks>
        Private Function IVsEditorFactory_CreateEditorInstance( _
                ByVal vscreateeditorflags As UInteger, _
                ByVal FileName As String, _
                ByVal PhysicalView As String, _
                ByVal Hierarchy As IVsHierarchy, _
                ByVal Itemid As UInteger, _
                ByVal ExistingDocDataPtr As IntPtr, _
                ByRef DocViewPtr As IntPtr, _
                ByRef DocDataPtr As IntPtr, _
                ByRef Caption As String, _
                ByRef CmdUIGuid As System.Guid, _
                ByRef pgrfCDW As Integer) As Integer _
        Implements IVsEditorFactory.CreateEditorInstance

            Dim ExistingDocData As Object = Nothing
            Dim DocView As Object = Nothing
            Dim DocData As Object = Nothing

            DocViewPtr = IntPtr.Zero
            DocDataPtr = IntPtr.Zero

            If Not ExistingDocDataPtr.Equals(IntPtr.Zero) Then
                ExistingDocData = Marshal.GetObjectForIUnknown(ExistingDocDataPtr)
            End If

            Caption = Nothing

            Dim hr As Integer = InternalCreateEditorInstance(vscreateeditorflags, FileName, PhysicalView, Hierarchy, Itemid, ExistingDocData, _
                DocView, DocData, Caption, CmdUIGuid, pgrfCDW)
            If NativeMethods.Failed(hr) Then
                Return hr
            End If

            If Not (DocView Is Nothing) Then
                DocViewPtr = Marshal.GetIUnknownForObject(DocView)
            End If

            If Not (DocData Is Nothing) Then
                DocDataPtr = Marshal.GetIUnknownForObject(DocData)
            End If

            Return hr
        End Function

        ''' <summary>
        ''' We only support the default view
        ''' </summary>
        ''' <param name="rguidLogicalView"></param>
        ''' <param name="pbstrPhysicalView"></param>
        ''' <remarks></remarks>
        Public Function MapLogicalView(ByRef rguidLogicalView As System.Guid, ByRef pbstrPhysicalView As String) As Integer Implements Shell.Interop.IVsEditorFactory.MapLogicalView
            pbstrPhysicalView = Nothing

            ' Normal logic for MapLogicalView is to return E_NOTIMPL for any rguidLogicalView values
            '   that this editor does not support. However, the project-designer is a bit different in
            '   that one of our NYI features is to treat the logical-view passed in as the initial
            '   property-page to display, and given that the possible set of pages is not known until
            '   runtime, we can't code something to look for known values. Therefore, we're returning
            '   S_OK no matter what logical view is being passed in.
            '
            ' Note that my comment above isn't fully accurate because while that is the design, we know
            '   that the VSCore project system currently only passes LOGVIEWID_Primary. We are adding
            '   this assert so that if that changes on the project-system side, they're aware that it
            '   won't work yet.
            '
            'Debug.Assert(rguidLogicalView.Equals(LOGVIEWID.LOGVIEWID_Primary), "NYI: Project Designer does not yet support choosing the initial property page thru the logical-view passed to our editor factory.")

            Return Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Called by owning site after creation
        ''' </summary>
        ''' <param name="Site"></param>
        ''' <remarks></remarks>
        Public Function SetSite(ByVal Site As OLE.Interop.IServiceProvider) As Integer Implements Shell.Interop.IVsEditorFactory.SetSite
            'This same Site already set?  Or Site not yet initialized (= Nothing)?  If so, NOP.
            If Me.m_Site Is Site Then
                Exit Function
            End If
            'Site is different - set it
            Me.m_Site = Site
            If TypeOf Site Is OLE.Interop.IServiceProvider Then
                m_SiteProvider = New ServiceProvider(CType(Site, Microsoft.VisualStudio.OLE.Interop.IServiceProvider))
            Else
                Debug.Fail("Site IsNot OLE.Interop.IServiceProvider")
            End If
        End Function

    End Class

End Namespace
