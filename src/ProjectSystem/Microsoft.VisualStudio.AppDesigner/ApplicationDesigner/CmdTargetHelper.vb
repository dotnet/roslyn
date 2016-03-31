Imports Microsoft.VisualStudio.Shell.Interop
Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Editors.AppDesInterop

Imports OleInterop = Microsoft.VisualStudio.OLE.Interop

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner


    ''' <summary>
    ''' We place an instance of this class into the project designer's window frame's ViewHelper property.
    '''   It allows us to gain access to command routing in the shell and also window frame events
    ''' </summary>
    ''' <remarks></remarks>
    Public Class CmdTargetHelper
        Implements OleInterop.IOleCommandTarget
        Implements IVsWindowFrameNotify3

        Private m_WindowPane As ApplicationDesignerWindowPane

        Public Sub New(ByVal WindowPane As ApplicationDesignerWindowPane)
            Debug.Assert(WindowPane IsNot Nothing)
            m_WindowPane = WindowPane
        End Sub

        ' Window navigation items
        Const cmdidPaneNextSubPane As Integer = 1062
        Const cmdidPanePrevSubPane As Integer = 1063
        Const cmdidPaneNextPane As Integer = 316
        Const cmdidPanePrevPane As Integer = 317
        Const cmdidPaneNextTab As Integer = 286
        Const cmdidPanePrevTab As Integer = 287
        Const cmdidCloseDocument As Integer = 658
        'case cmdidPaneCloseToolWindow
        'case cmdidPaneActivateDocWindow
        Const cmdidCloseAllDocuments As Integer = 627
        Const cmdidNextDocumentNav As Integer = 1124
        Const cmdidPrevDocumentNav As Integer = 1125
        Const cmdidNextDocument As Integer = 628
        Const cmdidPrevDocument As Integer = 629
        Const cmdidSaveSolution As Integer = 224

        ''' <summary>
        ''' Executes a specified command or displays help for a command.
        ''' </summary>
        ''' <param name="pguidCmdGroup">[unique][in] Pointer to unique identifier of the command group; can be NULL to specify the standard group. </param>
        ''' <param name="nCmdID">[in] The command to be executed. This command must belong to the group specified with pguidCmdGroup. </param>
        ''' <param name="nCmdexecopt">[in] Values taken from the OLECMDEXECOPT enumeration, which describe how the object should execute the command. </param>
        ''' <param name="pvaIn">[unique][in] Pointer to a VARIANTARG structure containing input arguments. Can be NULL. </param>
        ''' <param name="pvaOut">[unique][in,out] Pointer to a VARIANTARG structure to receive command output. Can be NULL. </param>
        ''' <returns></returns>
        ''' <remarks>
        ''' This method supports the standard return values E_FAIL and E_UNEXPECTED, as well as the following: 
        '''   S_OK 
        '''     The command was executed successfully. 
        '''   OLECMDERR_E_UNKNOWNGROUP 
        '''     The pguidCmdGroup parameter is not NULL but does not specify a recognized command group. 
        '''   OLECMDERR_E_NOTSUPPORTED 
        '''     The nCmdID parameter is not a valid command in the group identified by pguidCmdGroup. 
        '''   OLECMDERR_E_DISABLED 
        '''     The command identified by nCmdID is currently disabled and cannot be executed. 
        '''   OLECMDERR_E_NOHELP 
        '''     The caller has asked for help on the command identified by nCmdID, but no help is available. 
        '''   OLECMDERR_E_CANCELED 
        '''     The user canceled the execution of the command. 
        ''' </remarks>
        Private Function IOleCommandTarget_Exec(ByRef pguidCmdGroup As System.Guid, ByVal nCmdID As UInteger, ByVal nCmdexecopt As UInteger, ByVal pvaIn As System.IntPtr, ByVal pvaOut As System.IntPtr) As Integer Implements OleInterop.IOleCommandTarget.Exec
            Common.Switches.TracePDCmdTarget(TraceLevel.Info, "CmdTargetHelper.IOleCommandTarget.Exec: Guid=" & pguidCmdGroup.ToString & ", nCmdID=" & nCmdID)

            If m_WindowPane Is Nothing Then
                Debug.Fail("CmdTargetHelper.IOleCommandTarget_Exec(): m_WindowPane shouldn't be Nothing")
                Return AppDesInterop.NativeMethods.OLECMDERR_E_NOTSUPPORTED 'Not much we can do
            End If

            'Grab certain commands and handle ourselves
            If pguidCmdGroup.Equals(Microsoft.VisualStudio.Editors.Constants.MenuConstants.guidVSStd97) Then
                Common.Switches.TracePDCmdTarget(TraceLevel.Info, "CmdTargetHelper.IOleCommandTarget.Exec: Guid=guidVSStd97, nCmdID=" & nCmdID)
                Select Case nCmdID
                    Case Microsoft.VisualStudio.Editors.Constants.MenuConstants.cmdidSaveProjectItem
                        Common.Switches.TracePDCmdTarget(TraceLevel.Warning, "  Handling: cmdidSaveProjectItem")
                        'Execute a Save for the App Designer (saves all DocData pages)
                        Return HrSaveProjectDesigner()

                    Case cmdidSaveSolution
                        Common.Switches.TracePDCmdTarget(TraceLevel.Warning, "  Peeking: cmdidSaveSolution")

                        'There are scenarios with some property pages where a page doesn't get saved properly
                        '  with pending changes.  This ensures all pages in the project designer
                        '  get saved.

                        Dim hr As Integer = HrSaveProjectDesigner()

                        If VSErrorHandler.Failed(hr) Then
                            Return hr
                        End If

                        'Now let the shell do its normal processing of this command
                        Return AppDesInterop.NativeMethods.OLECMDERR_E_NOTSUPPORTED

                    Case Microsoft.VisualStudio.Editors.Constants.MenuConstants.cmdidSaveProjectItemAs
                        Debug.Fail("Shouldn't get able to get here - we were supposed to have disabled the menu for save as...")
                        Return NativeMethods.S_OK


                    Case Microsoft.VisualStudio.Editors.Constants.MenuConstants.cmdidFileClose, cmdidCloseDocument
                        'cmdidFileClose = File.CLose
                        'cmdidCloseDocument = CTRL+F4 or right-click Close on the MDI tab

                        'Note: For handling of when the user clicks on the X in the project designer, see IVsWindowFrameNotify3.OnClose
                        '  below.

                        Common.Switches.TracePDCmdTarget(TraceLevel.Warning, "  Handling: cmdidFileClose, cmdidCloseDocument")

                        'Prompt for save, and then close the project designer's window frame.  
                        '  Note that this causes us to notified in IVsWindowFrameNotify3.OnClose.
                        m_WindowPane.ClosePromptSave()
                        Return NativeMethods.S_OK

                    Case cmdidCloseAllDocuments
                        'There are scenarios with some property pages where a page doesn't get saved properly
                        '  with pending changes if all documents are closed.  This makes sure things get saved
                        '  properly.

                        Common.Switches.TracePDCmdTarget(TraceLevel.Warning, "  Peeking: cmdidCloseAllDocuments")

                        m_WindowPane.ClosePromptSave()

                        'Now let the shell do its normal processing of this command
                        Return AppDesInterop.NativeMethods.OLECMDERR_E_NOTSUPPORTED

                    Case cmdidPaneNextTab 'Window.NextTab (CTRL+PGDN by default) - move to the next tab in the project designer
                        Common.Switches.TracePDCmdTarget(TraceLevel.Warning, "  Handling: cmdidPaneNextTab")
                        Me.m_WindowPane.NextTab()
                        Return NativeMethods.S_OK

                    Case cmdidPanePrevTab 'Window.PrevTab (CTRL+PGUP by default) - move to the previous tab in the project designer
                        Common.Switches.TracePDCmdTarget(TraceLevel.Warning, "  Handling: cmdidPanePrevTab")
                        Me.m_WindowPane.PrevTab()
                        Return NativeMethods.S_OK

                        'Are any of these possibly needed?
                        'Case cmdidPaneNextSubPane
                        'Case cmdidPanePrevSubPane
                        'Case cmdidPaneNextPane
                        'Case cmdidPanePrevPane
                        'Case cmdidNextDocumentNav
                        'Case cmdidPrevDocumentNav
                        'Case cmdidNextDocument
                        'Case cmdidPrevDocument

                End Select
            Else
                Common.Switches.TracePDCmdTarget(TraceLevel.Info, "CmdTargetHelper.IOleCommandTarget.Exec: Guid=" & pguidCmdGroup.ToString & ", nCmdID=" & nCmdID)
            End If

            Return AppDesInterop.NativeMethods.OLECMDERR_E_NOTSUPPORTED
        End Function

        ''' <summary>
        ''' Saves the project designer and all its child designers
        ''' </summary>
        ''' <returns>An HRESULT</returns>
        ''' <remarks></remarks>
        Private Function HrSaveProjectDesigner() As Integer
            'Execute a Save for the App Designer (saves all DocData pages)
            Dim hr As Integer = m_WindowPane.SaveChildren(__VSRDTSAVEOPTIONS.RDTSAVEOPT_ForceSave)
            If VSErrorHandler.Succeeded(hr) Then
                '... and also a save of the project file itself
                m_WindowPane.SaveProjectFile() 'This will throw if there's a failure
            End If

            Return hr
        End Function

        ''' <summary>
        ''' Queries the object for the status of one or more commands from the shell
        ''' </summary>
        ''' <param name="pguidCmdGroup">[unique][in] Unique identifier of the command group; can be NULL to specify the standard group. All the commands that are passed in the prgCmds array must belong to the group specified by pguidCmdGroup.</param>
        ''' <param name="cCmds">[in] The number of commands in the prgCmds array. </param>
        ''' <param name="prgCmds">[in,out] A caller-allocated array of OLECMD structures that indicate the commands for which the caller needs status information. This method fills the cmdf member of each structure with values taken from the OLECMDF enumeration. </param>
        ''' <param name="pCmdText">[unique][in,out] Pointer to an OLECMDTEXT structure in which to return name and/or status information of a single command. Can be NULL to indicate that the caller does not need this information. </param>
        ''' <returns></returns>
        ''' <remarks>
        ''' This method supports the standard return values E_FAIL and E_UNEXPECTED, as well as the following: 
        '''   S_OK 
        '''     The command status as any optional strings were returned successfully. 
        '''   E_POINTER 
        '''     The prgCmds argument is NULL. 
        '''   OLECMDERR_E_UNKNOWNGROUP 
        '''     The pguidCmdGroup parameter is not NULL but does not specify a recognized command group. 
        '''
        ''' Callers use IOleCommandTarget::QueryStatus to determine which commands are supported by a target object. The caller can then disable unavailable commands that would otherwise be routed to the object. The caller can also use this method to get the name or status of a single command.
        ''' </remarks>
        Private Function IOleCommandTarget_QueryStatus(ByRef pguidCmdGroup As System.Guid, ByVal cCmds As UInteger, ByVal prgCmds As OLE.Interop.OLECMD(), ByVal pCmdText As System.IntPtr) As Integer Implements OleInterop.IOleCommandTarget.QueryStatus
            'Grab certain commands and handle ourselves

            Const Supported As UInteger = CUInt(OleInterop.OLECMDF.OLECMDF_SUPPORTED)
            Const Enabled As UInteger = CUInt(OleInterop.OLECMDF.OLECMDF_ENABLED)
            Const Invisible As UInteger = CUInt(OleInterop.OLECMDF.OLECMDF_INVISIBLE)

            Debug.Assert(cCmds = 1, "Unsupported: Multiple commands in QueryStatus") 'I don't think VS is ever supposed to give us more than one at a time

            If pguidCmdGroup.Equals(Microsoft.VisualStudio.Editors.Constants.MenuConstants.guidVSStd97) Then
                Common.Switches.TracePDCmdTarget(TraceLevel.Verbose, "CmdTargetHelper.IOleCommandTarget.QueryStatus: Guid=guidVSStd97, nCmdID=" & prgCmds(0).cmdID)
                Select Case prgCmds(0).cmdID
                    Case Microsoft.VisualStudio.Editors.Constants.MenuConstants.cmdidSaveProjectItem
                        Common.Switches.TracePDCmdTarget(TraceLevel.Info, "  Query: cmdidSaveProjectItem")
                        prgCmds(0).cmdf = Supported Or Enabled
                        Return NativeMethods.S_OK

                    Case Microsoft.VisualStudio.Editors.Constants.MenuConstants.cmdidSaveProjectItemAs
                        Common.Switches.TracePDCmdTarget(TraceLevel.Info, "  Query: cmdidSaveProjectItemAs")
                        prgCmds(0).cmdf = Supported Or Invisible 'CONSIDER: Invisible doesn't seem to work, but it does at least get disabled
                        Return NativeMethods.S_OK

                    Case Microsoft.VisualStudio.Editors.Constants.MenuConstants.cmdidFileClose, cmdidCloseDocument
                        Common.Switches.TracePDCmdTarget(TraceLevel.Info, "  Query: cmdidFileClose, cmdidCloseDocument")
                        prgCmds(0).cmdf = Supported Or Enabled
                        Return NativeMethods.S_OK

                    Case cmdidPaneNextTab, cmdidPanePrevTab
                        Common.Switches.TracePDCmdTarget(TraceLevel.Info, "  Query: cmdidPaneNextTab or cmdidPanePrevTab")
                        prgCmds(0).cmdf = Supported Or Enabled
                        Return NativeMethods.S_OK

                        'Are any of these possibly needed?
                        'Case cmdidPaneNextSubPane
                        'Case cmdidPanePrevSubPane
                        'Case cmdidPaneNextPane
                        'Case cmdidPanePrevPane
                        'Case cmdidNextDocumentNav
                        'Case cmdidPrevDocumentNav
                        'Case cmdidNextDocument
                        'Case cmdidPrevDocument
                End Select
            Else
                Common.Switches.TracePDCmdTarget(TraceLevel.Verbose, "CmdTargetHelper.IOleCommandTarget.QueryStatus: Guid=" & pguidCmdGroup.ToString & ", nCmdID=" & prgCmds(0).cmdID)
            End If

            Return AppDesInterop.NativeMethods.OLECMDERR_E_NOTSUPPORTED
        End Function


#Region "IVsWindowFrameNotify3 implementation"

        ' - IVsWindowFrameNotify3 -
        '
        'Notifies a VSPackage when changes are made to one of its window frames.
        '  
        '  When to Implement
        '    Implemented on a window sited in a window frame.
        '
        '  When to Call
        '    Called by the environment to notify a VSPackage of window manipulation by the user.
        '
        ' Note: IVsWindowFrameNotify is implemented on the object that is passed to the window 
        '   frame with the property VSFPROPID_ViewHelper from the VSFPROP enumeration.
        ' [This class is registered as the project designer's view helper.]



        ''' <summary>
        ''' Notifies the VSPackage that a window frame is closing and tells the environment what action to take.
        ''' </summary>
        ''' <param name="pgrfSaveOptions"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' Implementers should develop code to notify users and prompt for save and close and relay those decisions to the environment through pgrfSaveOptions.
        ''' </remarks>
        Private Function OnClose(ByRef pgrfSaveOptions As UInteger) As Integer Implements Shell.Interop.IVsWindowFrameNotify3.OnClose

            'Note: For File.Close and CTRL+F4 (document close), we first get called in the Exec handling above.  Then we close the frame,
            '  which forces us down here (but since we passed in NoClose as the save options, we will exit out quickly).
            'If the user clicks on the X in the project designer window right-top corner, then the first we know about it is in this
            '  call, where we handle saving.

            If m_WindowPane Is Nothing Then
                Debug.Fail("CmdTargetHelper.OnClose(): m_WindowPane shouldn't be Nothing")
                Return NativeMethods.S_OK 'Not much we can do
            End If

            'NOTE: In error cases, m_WindowPane.AppDesignerView may be Nothing, so we must guard against
            '  its use.

            Dim flags As __VSRDTSAVEOPTIONS
            Select Case pgrfSaveOptions
                Case CUInt(Shell.Interop.__FRAMECLOSE.FRAMECLOSE_NoSave)
                    'We hit this in the File.Close/CTRL+F4 case, because we have already saved any files the user wanted to save, and then
                    '  passed in NoSave to CloseFrame.  Nothing to do except notify the project designer that we're shutting down.
                    If m_WindowPane.AppDesignerView IsNot Nothing Then
                        m_WindowPane.AppDesignerView.NotifyShuttingDown()
                    End If
                    Return NativeMethods.S_OK
                Case CUInt(Shell.Interop.__FRAMECLOSE.FRAMECLOSE_PromptSave)
                    flags = __VSRDTSAVEOPTIONS.RDTSAVEOPT_DocClose Or __VSRDTSAVEOPTIONS.RDTSAVEOPT_PromptSave
                Case CUInt(Shell.Interop.__FRAMECLOSE.FRAMECLOSE_SaveIfDirty)
                    flags = __VSRDTSAVEOPTIONS.RDTSAVEOPT_DocClose Or __VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty
                Case Else
                    Debug.Fail("Unexpected save option in IVsWindowFrameNotify3.OnClose")
                    flags = __VSRDTSAVEOPTIONS.RDTSAVEOPT_DocClose Or __VSRDTSAVEOPTIONS.RDTSAVEOPT_PromptSave 'defensive
            End Select

            'Ask the user if s/he wants to save the child documents (depending on flags).  If so, go ahead and save them now.
            Dim hr As Integer = m_WindowPane.SaveChildren(flags)
            If NativeMethods.Failed(hr) Then
                'Fails (among other possible cases) when the user chooses Cancel.  Return the hresult so the
                '  close gets canceled.
                Return hr
            End If

            'Set the options to NoSave so the caller knows all necessary saves have already been done
            pgrfSaveOptions = CUInt(Shell.Interop.__FRAMECLOSE.FRAMECLOSE_NoSave)

            'Let the project designer know it's shutting down
            If m_WindowPane.AppDesignerView IsNot Nothing Then
                m_WindowPane.AppDesignerView.NotifyShuttingDown()
            End If

            Return NativeMethods.S_OK
        End Function


        ''' <summary>
        ''' Notifies the VSPackage that a window's docked state is being altered.
        ''' </summary>
        ''' <param name="fDockable"></param>
        ''' <param name="x"></param>
        ''' <param name="y"></param>
        ''' <param name="w"></param>
        ''' <param name="h"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function OnDockableChange(ByVal fDockable As Integer, ByVal x As Integer, ByVal y As Integer, ByVal w As Integer, ByVal h As Integer) As Integer Implements Shell.Interop.IVsWindowFrameNotify3.OnDockableChange
            Return NativeMethods.S_OK
        End Function


        ''' <summary>
        ''' Notifies the VSPackage that a window is being moved.
        ''' </summary>
        ''' <param name="x"></param>
        ''' <param name="y"></param>
        ''' <param name="w"></param>
        ''' <param name="h"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function OnMove(ByVal x As Integer, ByVal y As Integer, ByVal w As Integer, ByVal h As Integer) As Integer Implements Shell.Interop.IVsWindowFrameNotify3.OnMove
            Return NativeMethods.S_OK
        End Function


        ''' <summary>
        ''' Notifies the VSPackage of a change in the window's display state.
        ''' </summary>
        ''' <param name="fShow"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function OnShow(ByVal fShow As Integer) As Integer Implements Shell.Interop.IVsWindowFrameNotify3.OnShow
            'NOTE: In error cases, m_WindowPane.AppDesignerView may be Nothing, so we must guard against
            '  its use.

#If DEBUG Then
            If fShow <= __FRAMESHOW.FRAMESHOW_AutoHideSlideBegin Then
                Common.Switches.TracePDFocus(TraceLevel.Warning, "CmdTargetHelper.OnShow(" & System.Enum.GetName(GetType(__FRAMESHOW), fShow) & ")")
            ElseIf fShow <= __FRAMESHOW2.FRAMESHOW_BeforeWinHidden Then
                Common.Switches.TracePDFocus(TraceLevel.Warning, "CmdTargetHelper.OnShow(" & System.Enum.GetName(GetType(__FRAMESHOW2), fShow) & ")")
            Else
                Common.Switches.TracePDFocus(TraceLevel.Error, "CmdTargetHelper.OnShow - unrecognized fShow option")
            End If
#End If

            Return NativeMethods.S_OK
        End Function


        ''' <summary>
        ''' Notifies the VSPackage that a window is being resized.
        ''' </summary>
        ''' <param name="x"></param>
        ''' <param name="y"></param>
        ''' <param name="w"></param>
        ''' <param name="h"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function OnSize(ByVal x As Integer, ByVal y As Integer, ByVal w As Integer, ByVal h As Integer) As Integer Implements Shell.Interop.IVsWindowFrameNotify3.OnSize
            Return NativeMethods.S_OK
        End Function

#End Region

    End Class

End Namespace

