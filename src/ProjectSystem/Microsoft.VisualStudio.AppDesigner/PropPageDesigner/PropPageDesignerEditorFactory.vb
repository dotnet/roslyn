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

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    '**************************************************************************
    ';PropPageDesignerEditorFactory
    '
    'Remarks:
    '   The editor factory for the resource editor.  The job of this class is
    '   simply to create a new resource editor designer when requested by the
    '   shell.
    '**************************************************************************
    <CLSCompliant(False), _
    Guid("b270807c-d8c6-49eb-8ebe-8e8d566637a1")> _
    Public NotInheritable Class PropPageDesignerEditorFactory
        Implements IVsEditorFactory

        'The all important GUIDs 
        Private Shared ReadOnly m_EditorGuid As New Guid("{b270807c-d8c6-49eb-8ebe-8e8d566637a1}")
        Private Shared ReadOnly m_CommandUIGuid As New Guid("{86670efa-3c28-4115-8776-a4d5bb1f27cc}")

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
        ''' <param name="Canceled">Returns True if user canceled</param>
        ''' <remarks></remarks>
        Private Sub InternalCreateEditorInstance(ByVal VsCreateEditorFlags As System.UInt32, _
                ByVal FileName As String, _
                ByVal PhysicalView As String, _
                ByVal Hierarchy As IVsHierarchy, _
                ByVal ItemId As System.UInt32, _
                ByVal ExistingDocData As Object, _
                ByRef DocView As Object, _
                ByRef DocData As Object, _
                ByRef Caption As String, _
                ByRef CmdUIGuid As System.Guid, _
                ByRef Canceled As Boolean)
            Canceled = False
            CmdUIGuid = System.Guid.Empty

            Dim DesignerLoader As PropPageDesignerLoader = Nothing

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
                        DocData = New PropPageDesignerDocData(m_SiteProvider)
                    Else
                        Throw New COMException(SR.GetString(SR.DFX_IncompatibleBuffer), AppDesInterop.NativeMethods.VS_E_INCOMPATIBLEDOCDATA)
                    End If

                    DesignerLoader = CType(DesignerService.CreateDesignerLoader(GetType(PropPageDesignerLoader).AssemblyQualifiedName), PropPageDesignerLoader)
                    DesignerLoader.InitializeEx(m_SiteProvider, Hierarchy, ItemId, DocData)

                    Dim OleProvider As OLE.Interop.IServiceProvider = CType(m_SiteProvider.GetService(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                    Dim Designer As IVSMDDesigner = DesignerService.CreateDesigner(OleProvider, DesignerLoader)

                    'Site the TextStream
                    If TypeOf DocData Is IObjectWithSite Then
                        CType(DocData, IObjectWithSite).SetSite(m_Site)
                    Else
                        Debug.Fail("DocData does not implement IObjectWithSite")
                    End If

                    Debug.Assert(Not (Designer Is Nothing), "Designer service should have thrown if it had a problem.")

                    'Set the out params
                    DocView = Designer.View 'Gets the object that can support IVsWindowPane

                    Caption = "" ' Leave empty - The property page Title will appear as the caption 'Application|References|Debug etc.'

                    'Set the command UI
                    CmdUIGuid = m_CommandUIGuid
                End Using

            Catch ex As Exception

                If DesignerLoader IsNot Nothing Then
                    'We need to let the DesignerLoader disconnect from events
                    DesignerLoader.Dispose()
                End If

                Throw New System.Exception(SR.GetString(SR.DFX_CreateEditorInstanceFailed_Ex, ex.Message))
            End Try
        End Sub


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
                ByRef FCanceled As Integer) As Integer _
        Implements IVsEditorFactory.CreateEditorInstance

            Dim ExistingDocData As Object = Nothing
            Dim DocView As Object = Nothing
            Dim DocData As Object = Nothing
            Dim CanceledAsBoolean As Boolean = False

            DocViewPtr = IntPtr.Zero
            DocDataPtr = IntPtr.Zero

            If Not ExistingDocDataPtr.Equals(IntPtr.Zero) Then
                ExistingDocData = Marshal.GetObjectForIUnknown(ExistingDocDataPtr)
            End If

            Caption = Nothing

            InternalCreateEditorInstance(vscreateeditorflags, FileName, PhysicalView, Hierarchy, Itemid, ExistingDocData, _
                DocView, DocData, Caption, CmdUIGuid, CanceledAsBoolean)

            If CanceledAsBoolean Then
                FCanceled = 1
            Else
                FCanceled = 0
            End If

            If Not (DocView Is Nothing) Then
                DocViewPtr = Marshal.GetIUnknownForObject(DocView)
            End If
            If Not (DocData Is Nothing) Then
                DocDataPtr = Marshal.GetIUnknownForObject(DocData)
            End If
        End Function

        ''' <summary>
        ''' We only support the default view
        ''' </summary>
        ''' <param name="rguidLogicalView"></param>
        ''' <param name="pbstrPhysicalView"></param>
        ''' <remarks></remarks>
        Public Function MapLogicalView(ByRef rguidLogicalView As System.Guid, ByRef pbstrPhysicalView As String) As Integer Implements Shell.Interop.IVsEditorFactory.MapLogicalView
            pbstrPhysicalView = Nothing
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
