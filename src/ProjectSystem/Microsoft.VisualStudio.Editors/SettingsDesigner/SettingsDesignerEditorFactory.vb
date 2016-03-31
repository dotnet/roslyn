Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Editors
Imports Microsoft.VisualStudio.Shell.Interop
Imports LOGVIEWID = Microsoft.VisualStudio.Editors.Interop.LOGVIEWID
Imports NativeMethods = Microsoft.VisualStudio.Editors.Interop.NativeMethods


Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' The editor factory for the settings designer
    ''' </summary>
    ''' <remarks></remarks>
    <Guid(SettingsDesignerEditorFactory.EditorGuidString)> _
    Friend NotInheritable Class SettingsDesignerEditorFactory
        Inherits DesignerFramework.BaseEditorFactory

        'CONSIDER: we could support View Code/View Designer much like DataSet Editor does
        'Private Const VIEW_CODE As String = "Code"

        ' My editor GUID
        Friend Const EditorGuidString As String = "6d2695f9-5365-4a78-89ed-f205c045bfe6"

        ''' <summary>
        ''' Create a new instance
        ''' </summary>
        ''' <remarks>Only need to tell my base that I want to create SettingsDesignerLoaders</remarks>
        Public Sub New()
            MyBase.New(GetType(SettingsDesignerLoader))
        End Sub

        ''' <summary>
        ''' My commandUI GUID
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected Overrides ReadOnly Property CommandUIGuid() As System.Guid
            Get
                Return Constants.MenuConstants.GUID_SETTINGSDESIGNER_CommandUI
            End Get
        End Property

        ''' <summary>
        ''' Return my editor GUID
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected Overrides ReadOnly Property EditorGuid() As System.Guid
            Get
                Return New System.Guid(EditorGuidString)
            End Get
        End Property

        ''' <summary>
        ''' Create an editor instance (unless we are in a Venus project, cause Venus doesn't support settings)
        ''' </summary>
        ''' <param name="VsCreateEditorFlags"></param>
        ''' <param name="FileName"></param>
        ''' <param name="PhysicalView"></param>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <param name="ExistingDocData"></param>
        ''' <param name="DocView"></param>
        ''' <param name="DocData"></param>
        ''' <param name="Caption"></param>
        ''' <param name="CmdUIGuid"></param>
        ''' <param name="Canceled"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub CreateEditorInstance(ByVal VsCreateEditorFlags As UInteger, ByVal FileName As String, ByVal PhysicalView As String, ByVal Hierarchy As Shell.Interop.IVsHierarchy, ByVal ItemId As UInteger, ByVal ExistingDocData As Object, ByRef DocView As Object, ByRef DocData As Object, ByRef Caption As String, ByRef CmdUIGuid As System.Guid, ByRef Canceled As Boolean)
            Static prjKindVenus As String = "{E24C65DC-7377-472b-9ABA-BC803B73C61A}"
            Dim proj As EnvDTE.Project = Common.DTEUtils.EnvDTEProject(Hierarchy)
            If proj IsNot Nothing AndAlso proj.Kind = prjKindVenus Then
                Throw New COMException("", VSConstants.VS_E_UNSUPPORTEDFORMAT)

                'CONSIDER: we could support View Code/View Designer for a .settings file in a similar fashion to the DataSet Editor
                'ElseIf ((PhysicalView IsNot Nothing) AndAlso (String.Equals(PhysicalView, VIEW_CODE, StringComparison.OrdinalIgnoreCase))) Then
                'docView = null;
                'docData = null;
                'caption = null;
                'cmdUIGuid = Guid.Empty;
                'canceled = false;

                '// if the user selected 'View Code', let's bring up the dataset partial class.
                'ValidationManager validationMgr = new ValidationManager(this.ServiceProvider);
                'ProjectItem schemaPrjItem = ProjectItemUtil.GetProjectitem(hierarchy as IVsProject, fileName);
                'if (schemaPrjItem == null) {
                '    throw new InternalException("Unable to get schema project item.");
                '}

                'DesignDataSource designDs = ProjectDataSourceUtil.GetDataSource(schemaPrjItem);
                'if (designDs == null) {
                '    throw new InternalException("Unable to get DesignDataSource.");
                '}

                'bool succeeded = validationMgr.ViewCodeBehind(schemaPrjItem, designDs);

                'if (succeeded) {
                '    return NativeMethods.S_OK;
                '}
                'else {
                '    return NativeMethods.E_FAIL;
                '}
            Else
                MyBase.CreateEditorInstance(VsCreateEditorFlags, FileName, PhysicalView, Hierarchy, ItemId, ExistingDocData, DocView, DocData, Caption, CmdUIGuid, Canceled)
            End If
        End Sub

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

                'CONSIDER: we could support View Code/View Designer much like DataSet Editor does
                'ElseIf (LogicalView.Equals(LOGVIEWID.LOGVIEWID_Code)) Then
                ' if it's code-view, then we need to pass back "Code" so our editor knows which
                '   view to open up inside of CreateEditorInstance
                '
                'PhysicalViewOut = VIEW_CODE
                'Return NativeMethods.S_OK
            Else
                ' anything else should return E_NOTIMPL
                '
                Return Microsoft.VisualStudio.Editors.Interop.NativeMethods.E_NOTIMPL
            End If
        End Function

    End Class

End Namespace
