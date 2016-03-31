Option Explicit On
Option Strict On
Option Compare Binary
Imports System.IO
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VSDesigner

Namespace Microsoft.VisualStudio.Editors.ResourceEditor


    ''' <summary>
    ''' Designer loader for the Resource Editor.  Handles serialization and services.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class ResourceEditorDesignerLoader
        Inherits DesignerFramework.BaseDesignerLoader

        'A reference to the root component that we loaded.  This is needed during Flush
        '  operations, etc.
        Private m_RootComponent As ResourceEditorRootComponent

        ' User confirmed to update a form resx file. It is very easy to corrupt this file...
        Private m_AllowToUpdateDependentFile As Boolean

        ' When we are trying check-out the file, some dialogs could pop up and the designer could lose focus. But we should ignore
        '  some of those events to prevent committing the change again because of those Focus changing events...
        Private m_IsTryingCheckOut As Boolean

        ''' <summary>
        ''' "Saves" or flushes the current contents of the resource editor into the DocData's
        '''   buffer.
        ''' </summary>
        ''' <param name="SerializationManager"></param>
        ''' <remarks>
        ''' This is how we handle save (although it does not necessarily correspond
        '''   to the exact point at which the file is saved, just to when the IDE thinks
        '''   it needs an updated version of the file contents).
        ''' </remarks>
        Protected Overrides Sub HandleFlush(ByVal SerializationManager As IDesignerSerializationManager)
            Debug.Assert(Modified, "PerformFlush shouldn't get called if the designer's not dirty")

            Try
                If m_RootComponent IsNot Nothing Then
                    ' Make sure the ResourceFile is up to date with any pending user changes
                    m_RootComponent.RootDesigner.CommitAnyPendingChanges()

                    Using New WaitCursor
                        Debug.Assert(m_RootComponent.ResourceFile IsNot Nothing)
                        SetAllBufferText(SerializeResourcesToText(m_RootComponent.ResourceFile))
                    End Using
                Else
                    Debug.Fail("m_RootComponent is Nothing")
                End If
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Debug.Fail("Warning: Exception during flush: " & ex.ToString())
                Throw
            End Try
        End Sub


        ''' <summary>
        ''' This must be overloaded to return the assembly-qualified name of the base component that is 
        '''   being designed by this editor.  This information is required by the managed VSIP classes.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function GetBaseComponentClassName() As String
            Return GetType(ResourceEditorRootComponent).AssemblyQualifiedName
        End Function


        ''' <summary>
        ''' Depersists the state of the component tree (root component and any
        '''   sub-components) from the text buffer.  Creates the components (and
        '''   therefore their designers).
        ''' </summary>
        ''' <param name="SerializationManager">The Serialization from the designer host.</param>
        ''' <remarks>
        ''' If the load fails, this routine should throw an exception.  That exception
        '''   will automatically be added to the ErrorList by VSDesignerLoader.  If there
        '''   are more specific local exceptions, they can be added to ErrorList manually.
        ''' </remarks>
        Protected Overrides Sub HandleLoad(ByVal SerializationManager As System.ComponentModel.Design.Serialization.IDesignerSerializationManager)
            Dim NewResourceEditorRoot As ResourceEditorRootComponent = Nothing
            Using New WaitCursor
                If LoaderHost IsNot Nothing Then
                    ' Create the root component (this will also automatically create its root designer
                    '   and hook it up to the component)
                    NewResourceEditorRoot = CType(LoaderHost.CreateComponent(GetType(ResourceEditorRootComponent)), ResourceEditorRootComponent)
                    Debug.Assert(Not NewResourceEditorRoot Is Nothing, "should have thrown on failure")

                    'Figure out the base path to use for relative links in the resx file.  This should be the directory where the resx file
                    '  lives.
                    Dim BasePath As String = ""
                    Dim VsProject As IVsProject = TryCast(VsHierarchy, IVsProject)
                    If VsProject Is Nothing Then
                        Debug.Fail("Unable to get IVsProject from hierarchy for resxfile - will have to use absolute paths for all linked resources")
                    Else
                        Dim ResXFileName As String = Nothing
                        VSErrorHandler.ThrowOnFailure(VsProject.GetMkDocument(ProjectItemid, ResXFileName))
                        If ResXFileName = "" Then
                            Debug.Fail("IVsProject.GetMkDocument returned bad filename - will have to use absolute paths for all linked resources")
                        Else
                            Debug.Assert(File.Exists(ResXFileName), "Couldn't find resx file where we thought it was")
                            BasePath = Path.GetDirectoryName(ResXFileName)
                        End If
                        NewResourceEditorRoot.ResourceFileName = ResXFileName
                    End If

                    Dim mtSvr As MultiTargetService

                    Try
                        mtSvr = New MultiTargetService(Me.VsHierarchy, Me.ProjectItemid, isGlobalDTAR:=False)
                    Catch ex As ArgumentException
                        ' Can happen if there is no supported TargetFrameworkMoniker
                        mtSvr = Nothing
                    End Try

                    Dim ResourceFile As New ResourceFile(mtSvr, NewResourceEditorRoot, LoaderHost, BasePath)

                    'Read the resources from the text string into the ResourceFile

                    Try
                        Dim bufferText As String = GetAllBufferText()
                        ' We want to show an empty designer when we open an empty resx file. The file could come from a V7.x projects
                        If Not String.IsNullOrEmpty(bufferText) Then
                            'CONSIDER: get DocDataBufferReader and pass that in to ReadResources directly instead of the additional overhead of calling GetAllBufferText, which turns a stream into a string, and then turning the string into a stream.
                            Using Reader As StringReader = New StringReader(bufferText)
                                ResourceFile.ReadResources(Reader)
                            End Using
                        End If

                        'NOTE:  We should consider to restoer many view states before populating the designer surface...
                        NewResourceEditorRoot.LoadResXResourceFile(ResourceFile)

                        'We need to stash away a reference to the root component for use in Flush, etc.
                        m_RootComponent = NewResourceEditorRoot

                        'Try to restore the editor state from before the last reload, if any.
                        NewResourceEditorRoot.RootDesigner.TryDepersistSavedEditorState()

                        'Now that we know the load succeeded, we can try registering our view helper
                        NewResourceEditorRoot.RootDesigner.RegisterViewHelper()
                    Catch ex As Exception
                        RethrowIfUnrecoverable(ex)

                        m_RootComponent = Nothing

                        'No need to dispose the resource editor root, the host will do this for us.
                        ResourceFile.Dispose()

                        Throw
                    End Try

                End If
            End Using
        End Sub

        ''' <summary>
        ''' OnDesignerLoadCompleted will be called when we finish loading the designer
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub OnDesignerLoadCompleted()
            If m_RootComponent IsNot Nothing Then
                m_RootComponent.RootDesigner.OnDesignerLoadCompleted()
            End If
        End Sub

        ''' <summary>
        ''' Get a string representing the current contents of the ResX file
        ''' </summary>
        ''' <param name="ResXFile"></param>
        ''' <returns>The ResX in a string.</returns>
        ''' <remarks></remarks>
        Private Function SerializeResourcesToText(ByVal ResXFile As ResourceFile) As String
            If m_RootComponent IsNot Nothing Then
                ' Make sure the ResourceFile is up to date with any pending user changes
                m_RootComponent.RootDesigner.CommitAnyPendingChanges()

                ' Note that we cannot use a StringWriter based on a StringBuilder, 
                ' because that would produce a ResX file with UTF-16 encoding 
                ' (since that's what StringWriter uses).
                ' Instead, we use a MemoryStream and a StreamWriter/Reader.

                '... Write out the .resx file into a memory stream
                Dim StreamResXContents As New MemoryStream
                Dim Writer As New StreamWriter(StreamResXContents, Encoding.UTF8)
                ResXFile.WriteResources(Writer)
                Writer.Flush()

                '... Now read from the stream and return the text
                StreamResXContents.Position = 0
                Dim Reader As New StreamReader(StreamResXContents)
                Return Reader.ReadToEnd()
            Else
                Debug.Fail("m_RootComponent is nothing")
                Return ""
            End If
        End Function


        ''' <summary>
        ''' This method is called immediately after the first time
        '''   BeginLoad is invoked.  This is an appopriate place to
        '''   add custom services to the loader host.  Remember to
        '''   remove any custom services you add here by overriding
        '''   Dispose.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub Initialize()
            MyBase.Initialize()

            'Add our ComponentSerializationService so that the basic desiger will give us automatic Undo/Redo
            Dim SerializationService As New ResourceSerializationService(LoaderHost)
            LoaderHost.AddService(GetType(ComponentSerializationService), SerializationService)
            Debug.Assert(GetService(GetType(ComponentSerializationService)) IsNot Nothing, _
                "We just made the ComponentSerializationService service available.  Why isn't it there?")

            'Add our EditorState object to the host as a service.
            'This is needed in order for the view to have its state persisted in the case of
            '  a document reload.
            'Since the host is not recycled when the doc data is torn down during a reload, this 
            '  is a good place to persist this object.
            LoaderHost.AddService(GetType(ResourceEditorView.EditorState), New ResourceEditorView.EditorState)
            Debug.Assert(GetService(GetType(ResourceEditorView.EditorState)) IsNot Nothing, _
                "We just made the EditorState service available.  Why isn't it there?")
        End Sub


        ''' <summary>
        ''' Overrides base Dispose.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub Dispose()
            'Remove any services we proffered.
            '
            'Note: LoaderHost.RemoveService does not raise any exceptions if the service we're trying to
            '  remove isn't already there, so there's no need for a try/catch.
            LoaderHost.RemoveService(GetType(ComponentSerializationService))
            LoaderHost.RemoveService(GetType(ResourceEditorView.EditorState))

            MyBase.Dispose()
        End Sub


        ''' <summary>
        ''' Called when the document's window is activated or deactivated
        ''' </summary>
        ''' <param name="Activated"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnDesignerWindowActivated(ByVal Activated As Boolean)
            MyBase.OnDesignerWindowActivated(Activated)
            If m_RootComponent IsNot Nothing AndAlso m_RootComponent.RootDesigner IsNot Nothing Then
                If m_RootComponent.RootDesigner.HasView Then
                    m_RootComponent.RootDesigner.GetView().OnDesignerWindowActivated(Activated)
                End If
                If Not Activated Then
                    If Not m_IsTryingCheckOut Then
                        ' Commit any changes when the user moves to another window
                        m_RootComponent.RootDesigner.CommitAnyPendingChanges()
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' ManualCheckOut without the ProjectReloaded flag because it is not needed in
        '''   the resource editor.
        ''' </summary>
        ''' <remarks></remarks>
        Friend Overloads Sub ManualCheckOut()
            Dim ProjectReloaded As Boolean
            ManualCheckOut(ProjectReloaded)
            Debug.Assert(Not ProjectReloaded, "The project file should never be checked out in ResourceEditorDesignerLoader.ManualCheckout because " _
                & "the resource editor does not manage the project file in its SourceCodeControlManager.  " _
                & "If this design ever changes, then the resource editor's designer loader's ManualCheckOut should expose the ProjectReloaded argument and " _
                & "all callers should honor the returned flag.")
        End Sub

        ''' <summary>
        ''' Attempts to check out the DocData manually (without dirtying the DocData).  
        ''' Will throw an exception if it fails.
        ''' </summary>
        ''' <remarks>
        '''  See the comments in the base class...
        ''' </remarks>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
        Friend Overrides Sub ManualCheckOut(ByRef ProjectReloaded As Boolean)
            Dim originalState As Boolean = m_IsTryingCheckOut
            m_IsTryingCheckOut = True
            Try
                ' NOTE: What we do here is not really check-out operation. We actually don't want users to edit Form RESX file.
                '  Because the winForm designer doesn't support linked file yet, a new linked file added in the resource editor will make
                '  WinForm designer to fail. In this case, the customer will have to remove the new item by hand. Also, any new items or comments added will be ignored and discarded by winForm designer.
                '  However, we don't want to block them to edit that file totally, as some user do want to edit some strings in this way -- or export/import an image to edit it.
                '  So, we pop up a warning dialog when the customer starts to edit the file. 
                If m_RootComponent IsNot Nothing Then
                    Dim Designer As ResourceEditorRootDesigner = m_RootComponent.RootDesigner
                    If Designer IsNot Nothing Then
                        Dim ResourceView As ResourceEditorView = Designer.GetView()
                        ' We let the base class handle the read only mode
                        If ResourceView IsNot Nothing AndAlso Not ResourceView.ReadOnlyMode AndAlso _
                            m_RootComponent.IsDependentFile AndAlso Not m_AllowToUpdateDependentFile Then

                            If ResourceView.DsMsgBox(SR.GetString(SR.RSE_Err_UpdateADependentFile), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2, HelpIDs.Err_EditFormResx) = DialogResult.Yes Then
                                m_AllowToUpdateDependentFile = True
                            End If

                            If Not m_AllowToUpdateDependentFile Then
                                'Throw New OperationCanceledException()
                                Throw CheckoutException.Canceled
                            End If
                        End If
                    End If
                End If

                MyBase.ManualCheckOut(ProjectReloaded)
            Finally
                m_IsTryingCheckOut = originalState
            End Try
        End Sub

        ''' <summary>
        ''' If this function returns true, the designer will enter edit mode automatically.
        '''  The function in the baseclass checks whether the file has been checked out. For the resource designer, we should check whether
        '''  it is a dependent file (for example: a resx file of a winForm). We shouldn't enter the edit mode until the user said 'yes'.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Overrides Function OkToEdit() As Boolean
            If Not m_AllowToUpdateDependentFile Then
                If m_RootComponent IsNot Nothing Then
                    Dim Designer As ResourceEditorRootDesigner = m_RootComponent.RootDesigner
                    If Designer IsNot Nothing Then
                        If Not Designer.HasView Then
                            ' It means the DesignerView hasn't been created, or we are still initilizing it
                            Return False
                        End If

                        If m_RootComponent.IsDependentFile Then
                            Return False
                        End If
                    End If
                End If
            End If

            Return MyBase.OkToEdit()
        End Function

    End Class

End Namespace
