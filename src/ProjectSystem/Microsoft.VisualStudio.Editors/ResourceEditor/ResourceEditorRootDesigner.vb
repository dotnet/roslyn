'******************************************************************************
'* ResourceEditorRootDesigner.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Explicit On
Option Strict On
Option Compare Binary


Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports Microsoft.VisualStudio.Editors.Interop
Imports System
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Collections
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Globalization
Imports VB = Microsoft.VisualBasic
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextManager.Interop
Imports OleInterop = Microsoft.VisualStudio.OLE.Interop

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This is the designer for the top-level resource editor component (ResourceEditorRoot).  I.e., this
    '''   is the top-level designer.  In spite of the fancy term, it doesn't really do much except to dish out
    '''   the actual design surface for the resource editor - ResourceEditorView.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceEditorRootDesigner
        Inherits DesignerFramework.BaseRootDesigner
        Implements IRootDesigner, IVsFindTarget, OLE.Interop.IOleCommandTarget, IVsDebuggerEvents



#Region "Fields"

        ' The view associated with this root designer.
        Private m_View As ResourceEditorView

        ' Cache the IDesignerHost associated to the RootDesigner. 
        ' We use it to temporarily add Resource to the ResourceEditorRoot to push them to Property Window's drop down list.
        Private WithEvents m_DesignerHost As IDesignerHost = Nothing

        ' Contains information about the current state of Find/Replace
        Private m_FindReplace As New FindReplace(Me)

        ' Indicates whether or not we are trying to register our view helper on a delayed basis
        Private m_DelayRegisteringViewHelper As Boolean

        ' The ErrorListProvider to support error list window
        Private m_ErrorListProvider As ErrorListProvider

        ' Cached IVsDebugger from shell in case we don't have a service provider at
        ' shutdown so we can undo our event handler
        Private m_VsDebugger As IVsDebugger
        Private m_VsDebuggerEventsCookie As UInteger

        ' The UndoEngine
        Private WithEvents m_UndoEngine As UndoEngine

        ' Current Debug mode
        Private m_currentDebugMode As Shell.Interop.DBGMODE = DBGMODE.DBGMODE_Design

        ' ReadOnlyMode in design mode, we should restore the original mode when we return to the design mode
        Private m_IsReadOnlyInDesignMode As Boolean

        ' Is waiting to be reloaded, we should prevent refreshing UI when reloading is pending
        Private m_IsInReloading As Boolean

        ' BUGFIX: Dev11#45255 
        ' Hook up to build events so we can enable/disable the property 
        ' page while building
        Private WithEvents m_buildEvents As EnvDTE.BuildEvents
#End Region

#Region "Constructors/destructors"

        ''' <summary>
        ''' Overrides base Dispose()
        ''' </summary>
        ''' <param name="Disposing"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                'We might be being disposed in order to perform a reload on the DocData.  If
                '  so, we need to save our UI state so we can restore it after the reload.
                TryPersistCurrentEditorState()

                DisconnectDebuggerEvents()
                UnRegisterViewHelper()

                m_UndoEngine = Nothing

                If m_View IsNot Nothing Then
                    m_View.Dispose()
                    m_View = Nothing
                End If
            End If

            MyBase.Dispose(Disposing)
        End Sub

#End Region


#Region "Properties"

        ''' <summary>
        ''' Shadows MyBase.Component, just so we can hide it from Intellisense and
        '''   encourage the use of the strongly-typed RootComponent instead.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        <EditorBrowsable(EditorBrowsableState.Never)> _
        Friend Shadows ReadOnly Property Component() As ResourceEditorRootComponent
            Get
                Return RootComponent
            End Get
        End Property


        ''' <summary>
        ''' Returns the ResourceEditorRoot component that is being edited by this designer.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property RootComponent() As ResourceEditorRootComponent
            Get
                Dim Root As ResourceEditorRootComponent = CType(MyBase.Component, ResourceEditorRootComponent)
                Debug.Assert(Not Root Is Nothing)
                Return Root
            End Get
        End Property


        ''' <summary>
        '''  Returns the IDesignerHost from the RootDesigner.
        ''' </summary>
        ''' <value>An instance of IDesignerHost.</value>
        ''' <remarks> 
        ''' </remarks>
        Friend ReadOnly Property DesignerHost() As IDesignerHost
            Get
                Debug.Assert(m_DesignerHost IsNot Nothing, "Cannot get IDesignerHost!!!")
                Return m_DesignerHost
            End Get
        End Property


        ''' <summary>
        '''  Returns the DesignerLoader associated with this RootDesigner.
        ''' </summary>
        ''' <value>The ResourceEditorDesignerLoader instance.</value>
        ''' <remarks> 
        ''' </remarks>
        Friend ReadOnly Property DesignerLoader() As ResourceEditorDesignerLoader
            Get
                Dim DesignerLoaderService As Object = GetService(GetType(IDesignerLoaderService))
                If DesignerLoaderService IsNot Nothing Then
                    Debug.Assert(TypeOf DesignerLoaderService Is ResourceEditorDesignerLoader)
                    Return DirectCast(DesignerLoaderService, ResourceEditorDesignerLoader)
                End If

                Debug.Fail("Failed getting the DesignerLoader - this shouldn't happen")
                Throw New System.InvalidOperationException
            End Get
        End Property

        ''' <summary>
        '''  It will return true, if the designer is waiting to be reloaded. We should prevent refreshing UI in this state.
        '''  The designer will be destroyed, and a new designer will be created later, so we will never set it back.
        ''' </summary>
        ''' <value>Whether we are in reloading mode</value>
        ''' <remarks> 
        ''' </remarks>
        Friend Property IsInReloading() As Boolean
            Get
                Return m_IsInReloading
            End Get
            Set(ByVal value As Boolean)
                m_IsInReloading = value
            End Set
        End Property

#End Region


#Region "View support (IRootDesigner implementation)"

        ''' <summary>
        ''' Retrieves the current view for the designer (this is the actual display surface that the
        '''   user sees and associates as being the resource editor).  If one doesn't exist, a new
        '''   one will be created.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetView() As ResourceEditorView
            Return CType(IRootDesigner_GetView(ViewTechnology.Default), ResourceEditorView)
        End Function


        ''' <summary>
        ''' Returns True iff there a View has already been created.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property HasView() As Boolean
            Get
                Return m_View IsNot Nothing
            End Get
        End Property


        ''' <summary>
        ''' Called by the managed designer mechanism to determine what kinds of view technologies we support.
        '''   We currently support only "Default" (i.e., our designer view, ResourceEditorView,
        '''   inherits from System.Windows.Forms.Control).
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property IRootDesigner_SupportedTechnologies() As ViewTechnology() Implements IRootDesigner.SupportedTechnologies
            Get
                Return New ViewTechnology() {ViewTechnology.Default}
            End Get
        End Property


        ''' <summary>
        ''' Called by the managed designer technology to get our view, or the actual control that implements
        '''   our resource editor's designer surface.  In this case, we return an instance of ResourceEditorView.
        ''' </summary>
        ''' <param name="Technology"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IRootDesigner_GetView(ByVal Technology As ViewTechnology) As Object Implements IRootDesigner.GetView
            If Technology <> ViewTechnology.Default Then
                Throw New ArgumentException("Not a supported view technology", "Technology")
            End If

            If m_View Is Nothing Then
                'Need to create a view

                If Me.RootComponent.IsTearingDown Then
                    Debug.Fail("Creating new View while component is being disposed")
                    Throw New Package.InternalException
                End If

                Static CreatingNewView As Boolean
                If CreatingNewView Then
                    Debug.Fail("GetView called recursively while trying to create a new View")
                    Throw New Package.InternalException
                End If

                CreatingNewView = True
                Try

                    m_View = New ResourceEditorView(Me)

                    'Let the view know of its root designer (me).
                    '
                    'Note: Setting the site is not done via a constructor because we need the
                    '  constructor to return quickly.  It's possible for RegisterMenuCommands
                    '  (done on SetSite) to end up calling GetView again, and if the constructor
                    '  call weren't complete at that time, this would end up getting called
                    '  recursively, creating a second view (bad).
                    m_View.SetRootDesigner(Me)

                Finally
                    CreatingNewView = False
                End Try
            End If

            Return m_View
        End Function

#End Region

#Region "TaskList"

        ''' <summary>
        '''  We need create a ErrorListProvider to support the error list
        ''' </summary>
        ''' <remarks></remarks>
        Friend Function GetErrorListProvider() As ErrorListProvider
            If m_ErrorListProvider Is Nothing Then
                m_ErrorListProvider = New ErrorListProvider(DesignerHost)
            End If
            Return m_ErrorListProvider
        End Function

#End Region

#Region "Miscellaneous"

        ''' <summary>
        ''' Initialize is called to bind the designer to the component. 
        '''  We need override this function to start listen to the events in the designerHost.
        ''' </summary>
        Public Overrides Sub Initialize(ByVal component As IComponent)
            MyBase.Initialize(component)

            m_DesignerHost = CType(GetService(GetType(IDesignerHost)), IDesignerHost)
            Debug.Assert(m_DesignerHost IsNot Nothing, "Cannot get IDesignerHost!!!")
        End Sub

        ''' <summary>
        ''' Commits any current changes in the resource editor (for instance, if the user has
        '''   typed some characters into a string resource cell, but has not committed them by
        '''   moving to another cell or pressing ENTER).
        ''' 
        ''' </summary>
        ''' <remarks>This should be done before attempting to persist.</remarks>
        Friend Sub CommitAnyPendingChanges()
            If HasView Then
                GetView().CommitPendingChanges()
            End If
        End Sub


        ''' <summary>
        ''' Used to set the ResourceFile which should be displayed in the designer view.
        ''' </summary>
        ''' <param name="ResXResourceFile"></param>
        ''' <remarks></remarks>
        Friend Sub SetResourceFile(ByVal ResXResourceFile As ResourceFile)
            GetView().SetResourceFile(ResXResourceFile)
        End Sub


        ''' <summary>
        ''' Attempts to retrieve the filename and path of the .resx file being edited.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetResXFileNameAndPath() As String
            Try
                Dim ProjectItem As EnvDTE.ProjectItem = TryCast(GetService(GetType(EnvDTE.ProjectItem)), EnvDTE.ProjectItem)
                If ProjectItem IsNot Nothing Then
                    'FileNames is 1-indexed
                    Return ProjectItem.FileNames(1)
                Else
                    Debug.Fail("Couldn't find ExtensibilityObject as service (should have been added by ResourceEditorDesignerLoader")
                End If

                'If that method failed, then try this...

                Dim WindowFrame As IVsWindowFrame = CType(GetService(GetType(IVsWindowFrame)), IVsWindowFrame)
                If WindowFrame IsNot Nothing Then
                    Dim punkDocData As Object = __VSFPROPID.VSFPROPID_DocData
                    If punkDocData IsNot Nothing Then
                        If TypeOf punkDocData Is TextManager.Interop.IVsUserData Then
                            Dim Guid As Guid = GetType(TextManager.Interop.IVsUserData).GUID
                            Dim vt As Object = Nothing
                            VSErrorHandler.ThrowOnFailure(CType(punkDocData, TextManager.Interop.IVsUserData).GetData(Guid, vt))
                            If TypeOf vt Is String Then
                                Return IO.Path.GetFileName(CStr(vt))
                            End If
                        End If
                    End If
                End If
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Return ""
            End Try

            Debug.Fail("Couldn't get name of resx file for UI purposes - returning empty string")
            Return ""
        End Function

        ''' <summary>
        ''' Returns whether the designer is editing a resw file
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IsEditingResWFile() As Boolean
            Return Utility.HasResWExtension(GetResXFileNameAndPath())
        End Function

#End Region


#Region "Persist/depersist editor state"

        ''' <summary>
        ''' Attempts to persist the editor's current state, so that it can be reestablished
        '''   after a document reload.  It's not good enough to just do this on a flush
        '''   because flushes only occur if the designer is actually dirtied.  UI state can
        '''   change without dirtying the designer.
        ''' </summary>
        ''' <remarks>
        ''' Never throws exceptions (ignores any errors)
        ''' </remarks>
        Public Sub TryPersistCurrentEditorState()
            If m_View IsNot Nothing Then
                Try
                    'See if our designer loader has already placed an EditorState object in the host as a service.  If so, persist into it.
                    Dim EditorState As ResourceEditorView.EditorState = DirectCast(GetService(GetType(ResourceEditorView.EditorState)), ResourceEditorView.EditorState)
                    If EditorState IsNot Nothing Then
                        'Go ahead and persist the state
                        EditorState.PersistStateFrom(m_View)
                    Else
                        'The service was not proffered.  This should mean simply that our designer loader has already
                        '  been disposed (so our service was removed), which is the case when the resource editor
                        '  is actually being closed, and not simply reloaded.
                        '
                        'So, nothing for us to do.
                    End If
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    Debug.Fail("Exception trying to persist editor state: " & ex.ToString())
                    'Ignore error
                End Try
            End If
        End Sub


        ''' <summary>
        ''' Attempts to depersist the editor state from that which was saved in the last call
        '''   to TryPersistCurrentEditorState().
        ''' </summary>
        ''' <remarks>
        ''' Never throws exceptions (ignores any errors).
        ''' </remarks>
        Public Sub TryDepersistSavedEditorState()
            If m_View IsNot Nothing Then
                Try
                    'Do we have an old editor state?  If so, it would be on the host as a service (cause we put it there
                    '  in TryPersistCurrentEditorState().
                    Dim EditorState As ResourceEditorView.EditorState = DirectCast(GetService(GetType(ResourceEditorView.EditorState)), ResourceEditorView.EditorState)
                    If EditorState IsNot Nothing AndAlso EditorState.StatePersisted Then
                        EditorState.DepersistStateInto(m_View)
                    End If
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    Debug.Fail("Exception trying to restore editor state after reload: " & ex.ToString)
                    'Now ignore the exception
                End Try
            End If
        End Sub

#End Region


#Region " Find / Replace - IVsFindTarget "


        'NOTE: A note on the params in the PIAs that take an array when they look like they should be
        '  taking a ByRef parameter:
        '
        ' From Chris McGuire:
        '   Most of them are coded that way because they are either optional parameters or the parameter may be passed 
        '   as NULL.  C# does not allow a NULL out parameter unless it is an array.  In the cases where the parameter 
        '   is an array where you are the caller, create a single element array and put your value in it or pass null.  
        '   If you are called you can test for null and then dereference the first element to make an assignment.  If 
        '   you know of a specific item that you believe is not in this category I will take a look.
        '




        ''' <summary>
        ''' Register this root designer as a view helper with the current frame so the shell will can find our
        '''   implementations of IVsFindTarget, IOleCommandTarget, etc.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub RegisterViewHelper()
            Try
                Dim VsWindowFrame As IVsWindowFrame = CType(GetService(GetType(IVsWindowFrame)), IVsWindowFrame)

                If VsWindowFrame IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(VsWindowFrame.SetProperty(__VSFPROPID.VSFPROPID_ViewHelper, New UnknownWrapper(Me)))
                Else
                    If m_View IsNot Nothing Then
                        'We don't have a window frame yet.  Need to delay this registration until we do.
                        '  Easiest way is to use BeginInvoke.
                        If m_DelayRegisteringViewHelper Then
                            'This is already our second try
                            Debug.Fail("Unable to delay-register our view helper")
                            m_DelayRegisteringViewHelper = False
                        Else
                            'Try again, delayed.
                            m_DelayRegisteringViewHelper = True

                            ' VS Whidbey #260046 -- Make sure the control is created before calling Invoke/BeginInvoke                                                      
                            If (m_View.Created = False) Then
                                m_View.CreateControl()
                            End If

                            m_View.BeginInvoke(New System.Windows.Forms.MethodInvoker(AddressOf RegisterViewHelper))
                        End If
                    Else
                        Debug.Fail("View not set in RegisterViewHelper() - can't delay-register view helper")
                    End If
                End If
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Debug.Fail(ex.ToString)
            End Try
        End Sub


        ''' <summary>
        '''  Unregister our IVsFindTarget with the current frame so the shell will call us to find / replace.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UnRegisterViewHelper()
            Try
                Dim VsWindowFrame As IVsWindowFrame = CType(GetService(GetType(IVsWindowFrame)), IVsWindowFrame)
                Debug.Assert(VsWindowFrame IsNot Nothing, "Cannot get VsWindowFrame!!!")

                If VsWindowFrame IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(VsWindowFrame.SetProperty(__VSFPROPID.VSFPROPID_ViewHelper, New UnknownWrapper(Nothing)))
                End If
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Debug.Fail(ex.ToString)
            End Try
        End Sub


        ''' <summary>
        '''  Reset the array of Resources to find / replace in and the find / replace loop.
        ''' </summary>
        ''' <remarks>
        ''' This should be called when resources are added or removed, so that the search 
        '''   can be reset.
        ''' </remarks>
        Friend Sub InvalidateFindLoop(ByVal ResourcesAddedOrRemoved As Boolean)
            m_FindReplace.InvalidateFindLoop(ResourcesAddedOrRemoved)
        End Sub


        ''' <summary>
        ''' Specifies our editor's supported capabilities for Find / Replace.
        ''' </summary>
        ''' <param name="pfImage">Set to True if supporting GetSearchImage - seaching in a text image.</param>
        ''' <param name="pgrfOptions">Specifies supported options, syntax and options, taken from __VSFINDOPTIONS.</param>
        ''' <remarks></remarks>
        Private Function GetCapabilities(ByVal pfImage() As Boolean, ByVal pgrfOptions() As UInteger) As Integer Implements TextManager.Interop.IVsFindTarget.GetCapabilities
            m_FindReplace.GetCapabilities(pfImage, pgrfOptions)
            Return NativeMethods.S_OK
        End Function


        ''' <summary>
        ''' Returns a value of a requested property.
        ''' </summary>
        ''' <param name="propid">Property identifier of the requested property, taken from VSFTPROPID enum.</param>
        ''' <param name="pvar">Property value.</param>
        ''' <returns>S_OK if success, otherwise an error code.</returns>
        ''' <remarks></remarks>
        Private Function GetProperty(ByVal propid As UInteger, ByRef pvar As Object) As Integer Implements TextManager.Interop.IVsFindTarget.GetProperty
            Return m_FindReplace.GetProperty(propid, pvar)
        End Function


        ''' <summary>
        ''' Gets the find state object that we hold for the find engine.
        ''' </summary>
        ''' <returns>m_FindState</returns>
        ''' <remarks>If m_FindState is set to Nothing, the shell will reset the next find loop.</remarks>
        Private Function GetFindState(ByRef state As Object) As Integer Implements TextManager.Interop.IVsFindTarget.GetFindState
            state = m_FindReplace.GetFindState()
            Return NativeMethods.S_OK
        End Function


        ''' <summary>
        ''' Sets the find state object that we hold for the find engine.
        ''' </summary>
        ''' <param name="pUnk">The find state object to hold.</param>
        ''' <remarks></remarks>
        Private Function SetFindState(ByVal pUnk As Object) As Integer Implements TextManager.Interop.IVsFindTarget.SetFindState
            m_FindReplace.SetFindState(pUnk)
            Return NativeMethods.S_OK
        End Function


        ''' <summary>
        ''' Searches for a text pattern.
        ''' </summary>
        ''' <param name="pszSearch">The search pattern.</param>
        ''' <param name="grfOptions">The options of the test (from __VSFINDOPTIONS).</param>
        ''' <param name="fResetStartPoint">1 means the find loop is reset, otherwise 0.</param>
        ''' <param name="pHelper">IVsFindHelper interface containing utiliy methods for Find.</param>
        ''' <param name="pResult">Search result, values are taken from __VSFINDRESULT.</param>
        ''' <remarks> 
        ''' Find works as follow:
        ''' - User clicks Find, shell will call our Find, passing in the string to search, the options for the search,
        '''      and a flag of whether the start point is reset (true for the first time, false for the rest of 'Find Next').
        ''' - We search for the text and return an enum value to the shell to display the dialog box if not found, etc...
        ''' - We are responsible for selecting the found object and keeping track of where we are in the object list.
        ''' </remarks>
        Private Function Find(ByVal pszSearch As String, ByVal grfOptions As UInteger, ByVal fResetStartPoint As Integer, _
                            ByVal pHelper As IVsFindHelper, ByRef pResult As UInteger) As Integer Implements IVsFindTarget.Find
            m_FindReplace.Find(pszSearch, grfOptions, fResetStartPoint, pHelper, pResult)
            Return NativeMethods.S_OK
        End Function


        ''' <summary>
        '''  Requests a text string replace.
        ''' </summary>
        ''' <param name="pszSearch">Pointer to a null teminated string containing the search text.</param>
        ''' <param name="pszReplace">Pointer to a null teminated string containing the replacement text.</param>
        ''' <param name="grfOptions">Specifies the options. Values are from __VSFINDOPTIONS.</param>
        ''' <param name="fResetStartPoint">Flag to reset the search start point.</param>
        ''' <param name="pHelper">Pointer to an IVsFindHelper interface.</param>
        ''' <param name="pfReplaced">True if the replacement was successful.</param>
        ''' <remarks>
        ''' Replace works as follow:
        ''' - From user perspective: If user clicks Replace right at the beginning, we select the first matching ojbect 
        '''      but NOT replacing it. If user clicks Replace again, we replace that object and jump to the next matching ojbect.
        ''' - Implementation: User clicks Replace, shell will call our Replace. Right after that, shell will call our Find
        '''      to search for the next object. Therefore, at the beginning of Replace, we check to see Replace was clicked first.
        '''      If it's true, we don't do anything and let shell call our Find. The next time we will replace.
        ''' </remarks>
        Private Function Replace(ByVal pszSearch As String, ByVal pszReplace As String, ByVal grfOptions As UInteger, _
                                ByVal fResetStartPoint As Integer, ByVal pHelper As TextManager.Interop.IVsFindHelper, ByRef pfReplaced As Integer) As Integer _
                                Implements TextManager.Interop.IVsFindTarget.Replace
            'We don't currently support replace.  NOP
            Return NativeMethods.E_NOTIMPL
        End Function


#Region " Not-implemented IVsFindTarget methods "

        ' NOTE: HuyN: We don't implement the methods below since we handle the search / replace ourselves.

        Private Function GetCurrentSpan(ByVal pts() As TextManager.Interop.TextSpan) As Integer Implements TextManager.Interop.IVsFindTarget.GetCurrentSpan
            Return NativeMethods.E_NOTIMPL
        End Function

        Private Function GetMatchRect(ByVal prc() As OLE.Interop.RECT) As Integer Implements TextManager.Interop.IVsFindTarget.GetMatchRect
            Return NativeMethods.E_NOTIMPL
        End Function

        Private Function GetSearchImage(ByVal grfOptions As UInteger, ByVal ppSpans() As TextManager.Interop.IVsTextSpanSet, ByRef ppTextImage As TextManager.Interop.IVsTextImage) As Integer Implements TextManager.Interop.IVsFindTarget.GetSearchImage
            Return NativeMethods.E_NOTIMPL
        End Function

        Private Function MarkSpan(ByVal pts() As TextManager.Interop.TextSpan) As Integer Implements TextManager.Interop.IVsFindTarget.MarkSpan
            Return NativeMethods.E_NOTIMPL
        End Function

        Private Function NavigateTo(ByVal pts() As TextManager.Interop.TextSpan) As Integer Implements TextManager.Interop.IVsFindTarget.NavigateTo
            Return NativeMethods.E_NOTIMPL
        End Function

        Private Function NotifyFindTarget(ByVal notification As UInteger) As Integer Implements TextManager.Interop.IVsFindTarget.NotifyFindTarget
            'Debug.WriteLine("DSRootDesigner.NotifyFindTarget: " + CType(notification, __VSFTNOTIFY).ToString)
            Return NativeMethods.E_NOTIMPL
        End Function

#End Region

#End Region

        ''' <summary>
        ''' Executes a specified command or displays help for a command.
        ''' </summary>
        ''' <param name="pguidCmdGroup">Pointer to unique identifier of the command group; can be NULL to specify the standard group. </param>
        ''' <param name="nCmdID">The command to be executed. This command must belong to the group specified with pguidCmdGroup. </param>
        ''' <param name="nCmdexecopt">Values taken from the OLECMDEXECOPT enumeration, which describe how the object should execute the command. </param>
        ''' <param name="pvaIn">Pointer to a VARIANTARG structure containing input arguments. Can be NULL. </param>
        ''' <param name="pvaOut">Pointer to a VARIANTARG structure to receive command output. Can be NULL. </param>
        ''' <returns>OLECMDERR_E_NOTSUPPORTED if the command is not handled by this class, otherwise should return E_OK if handled.</returns>
        ''' <remarks>
        ''' See comments in ResourceEditorView.HandleViewHelperCommandExec for why we are implementing IOleCommandTarget.
        ''' </remarks>
        Public Function IOleCommandTarget_Exec(ByRef pguidCmdGroup As System.Guid, ByVal nCmdID As UInteger, ByVal nCmdexecopt As UInteger, ByVal pvaIn As System.IntPtr, ByVal pvaOut As System.IntPtr) As Integer _
        Implements OLE.Interop.IOleCommandTarget.Exec
            Dim View As ResourceEditorView = m_View
            If View IsNot Nothing Then
                Dim Handled As Boolean = False
                View.HandleViewHelperCommandExec(pguidCmdGroup, nCmdID, Handled)
                If Handled Then
                    Return Interop.NativeMethods.S_OK
                End If
            End If

            Return Interop.NativeMethods.OLECMDERR_E_NOTSUPPORTED
        End Function


        ''' <summary>
        ''' Queries the object for the status of one or more commands generated by user interface events.
        ''' </summary>
        ''' <param name="pguidCmdGroup">Unique identifier of the command group; can be NULL to specify the standard group. All the commands that are passed in the prgCmds array must belong to the group specified by pguidCmdGroup. </param>
        ''' <param name="cCmds">The number of commands in the prgCmds array. </param>
        ''' <param name="prgCmds">[in,out] A caller-allocated array of OLECMD structures that indicate the commands for which the caller needs status information. This method fills the cmdf member of each structure with values taken from the OLECMDF enumeration. </param>
        ''' <param name="pCmdText">[unique][in,out] Pointer to an OLECMDTEXT structure in which to return name and/or status information of a single command. Can be NULL to indicate that the caller does not need this information. </param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IOleCommandTarget_QueryStatus(ByRef pguidCmdGroup As System.Guid, ByVal cCmds As UInteger, ByVal prgCmds As OLE.Interop.OLECMD(), ByVal pCmdText As System.IntPtr) As Integer _
        Implements OLE.Interop.IOleCommandTarget.QueryStatus
            'We don't implement this.
            Return Interop.NativeMethods.OLECMDERR_E_NOTSUPPORTED
        End Function
        '
        ''' <summary>
        ''' OnDesignerLoadCompleted will be called when we finish loading the designer
        ''' </summary>
        ''' <remarks></remarks>
        Friend Sub OnDesignerLoadCompleted()
            ConnectDebuggerEvents()
            ' BUGFIX: Dev11#45255 
            ConnectBuildEvents()
            'test if in build process
            Dim DesignerLoaderService As Object = GetService(GetType(IDesignerLoaderService))
            If m_View IsNot Nothing And DesignerLoaderService IsNot Nothing Then
                If IsInBuildProgress() Then
                    DesignerLoader.SetReadOnlyMode(True, String.Empty)
                    m_View.ReadOnlyMode = True
                Else
                    DesignerLoader.SetReadOnlyMode(False, String.Empty)
                    m_View.ReadOnlyMode = False
                End If
            End If
        End Sub

        ''' <summary>
        ''' We need get undoEngine to monitor undo state. But it is not available when the designer is just loaded. We do this on the first transaction (change) happens in the designer.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DesignerHost_TransactionOpening(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_DesignerHost.TransactionOpening
            If m_UndoEngine Is Nothing Then
                m_UndoEngine = DirectCast(GetService(GetType(UndoEngine)), UndoEngine)
                ' We get UndoEngine here, because we need monitor undo start/end event. But when this trasaction itself is caused by an UNDO operation,
                ' it is already too late to hook up the event, and we lost the first UNDO start event. Here, we check whether the transaction
                '  is caused by UNDO, and simulate the first UNDO start event.
                If m_UndoEngine IsNot Nothing AndAlso m_UndoEngine.UndoInProgress Then
                    UndoEngine_Undoing(Me, System.EventArgs.Empty)
                End If
            End If
        End Sub

#Region "ReadOnly during debug mode and build" ' BUGFIX: Dev11#45255 
        ''' <summary>
        ''' Start listening to build events and set our initial build status
        ''' </summary>
        Private Sub ConnectBuildEvents()
            Dim dte As EnvDTE.DTE
            DTE = CType(GetService(GetType(EnvDTE.DTE)), EnvDTE.DTE)
            If dte IsNot Nothing Then
                m_buildEvents = dte.Events.BuildEvents
            Else
                Debug.Fail("No DTE - can't hook up build events - we don't know if start/stop building...")
            End If
        End Sub

        ''' <summary>
        ''' A build has started - disable/enable page
        ''' </summary>
        ''' <param name="scope"></param>
        ''' <param name="action"></param>
        ''' <remarks></remarks>
        Private Sub BuildBegin(ByVal scope As EnvDTE.vsBuildScope, ByVal action As EnvDTE.vsBuildAction) Handles m_buildEvents.OnBuildBegin
            Dim DesignerLoaderService As Object = GetService(GetType(IDesignerLoaderService))
            If DesignerLoaderService IsNot Nothing Then
                DesignerLoader.SetReadOnlyMode(True, String.Empty)
                m_View.ReadOnlyMode = True
            End If
        End Sub

        ''' <summary>
        ''' A build has finished - disable/enable page
        ''' </summary>
        ''' <param name="scope"></param>
        ''' <param name="action"></param>
        ''' <remarks></remarks>
        Private Sub BuildDone(ByVal scope As EnvDTE.vsBuildScope, ByVal action As EnvDTE.vsBuildAction) Handles m_buildEvents.OnBuildDone
            Dim DesignerLoaderService As Object = GetService(GetType(IDesignerLoaderService))
            If DesignerLoaderService IsNot Nothing Then
                DesignerLoader.SetReadOnlyMode(False, String.Empty)
                m_View.ReadOnlyMode = False
            End If
        End Sub

        ''' <summary>
        ''' Hook up with the debugger event mechanism to determine current debug mode
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ConnectDebuggerEvents()
            If m_VsDebuggerEventsCookie = 0 Then
                m_VsDebugger = CType(GetService(GetType(IVsDebugger)), IVsDebugger)
                If m_VsDebugger IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(m_VsDebugger.AdviseDebuggerEvents(Me, m_VsDebuggerEventsCookie))

                    Dim mode As DBGMODE() = New DBGMODE() {DBGMODE.DBGMODE_Design}
                    'Get the current mode
                    VSErrorHandler.ThrowOnFailure(m_VsDebugger.GetMode(mode))
                    OnModeChange(mode(0))
                Else
                    Debug.Fail("Cannot obtain IVsDebugger from shell")
                    OnModeChange(DBGMODE.DBGMODE_Design)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Unhook event notification for debugger 
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DisconnectDebuggerEvents()
            If m_VsDebugger IsNot Nothing AndAlso m_VsDebuggerEventsCookie <> 0 Then
                VSErrorHandler.ThrowOnFailure(m_VsDebugger.UnadviseDebuggerEvents(m_VsDebuggerEventsCookie))
                m_VsDebuggerEventsCookie = 0
                m_VsDebugger = Nothing
            End If
        End Sub

        ''' <summary>
        ''' handle DebugMode change event, disable the designer when in debug mode...
        ''' </summary>
        ''' <param name="dbgmodeNew"></param>
        Private Function OnModeChange(ByVal dbgmodeNew As Shell.Interop.DBGMODE) As Integer Implements Shell.Interop.IVsDebuggerEvents.OnModeChange
            Try
                If m_View IsNot Nothing Then
                    If dbgmodeNew = DBGMODE.DBGMODE_Design Then
                        If m_currentDebugMode <> DBGMODE.DBGMODE_Design AndAlso Not m_IsReadOnlyInDesignMode Then
                            DesignerLoader.SetReadOnlyMode(False, String.Empty)
                            m_View.ReadOnlyMode = False
                        End If
                    ElseIf m_currentDebugMode = DBGMODE.DBGMODE_Design Then
                        m_IsReadOnlyInDesignMode = m_View.ReadOnlyMode
                        DesignerLoader.SetReadOnlyMode(True, SR.GetString(SR.RSE_Err_CantEditInDebugMode))
                        m_View.ReadOnlyMode = True
                    End If
                End If
            Finally
                m_currentDebugMode = dbgmodeNew
            End Try
        End Function

#End Region


#Region "UndoEngine"

        ''' <summary>
        ''' handle Undo Event, it will be called when undo is started
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub UndoEngine_Undoing(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_UndoEngine.Undoing
            If m_View IsNot Nothing Then
                m_View.OnUndoing()
            End If
        End Sub

        ''' <summary>
        ''' handle Undo Event, it will be called when undo is done
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub UndoEngine_Undone(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_UndoEngine.Undone
            If m_View IsNot Nothing Then
                m_View.OnUndone()
            End If
        End Sub

#End Region


    End Class
End Namespace
