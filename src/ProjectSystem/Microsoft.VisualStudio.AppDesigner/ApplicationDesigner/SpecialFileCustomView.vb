Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    ''' This class is a custom view for the project designer that displays
    '''   a message asking the user to click it in order to add a special file
    '''   to the project (using IVsProjectSpecialFiles).  It is currently used
    '''   to allow users to view the Resources and Settings tabs without actually
    '''   having a default resx or settings file in the project.  When they want to
    '''   create one, they just click on the message and the new file is created
    '''   using IVsProjectSpecialFiles, then the view is changed to display the editor
    '''   for the new file.
    ''' </summary>
    ''' <remarks></remarks>
    Public Class SpecialFileCustomView
        Inherits System.Windows.Forms.UserControl

        'The SpecialFileCustomViewProvider which created this class instance
        Private m_ViewProvider As SpecialFileCustomViewProvider


        ''' <summary>
        ''' Communicates to this class the SpecialFileCustomViewProvider which created it.
        '''   Used to access the special file ID, etc.
        ''' </summary>
        ''' <param name="ViewProvider"></param>
        ''' <remarks></remarks>
        Public Sub SetSite(ByVal ViewProvider As SpecialFileCustomViewProvider)
            m_ViewProvider = ViewProvider
            If ViewProvider IsNot Nothing Then
                LinkLabel.Text = m_ViewProvider.LinkText
            End If
        End Sub

        ''' <summary>
        ''' The link message has been clicked.  This means the user has requested that the
        '''   file be created.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub LinkLabel_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles LinkLabel.LinkClicked
            CreateNewSpecialFile()
        End Sub

        ''' <summary>
        ''' Create the special file associated with this view.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub CreateNewSpecialFile()
            If m_ViewProvider IsNot Nothing Then
                Debug.Assert(m_ViewProvider.DesignerView IsNot Nothing)
                If m_ViewProvider.DesignerView IsNot Nothing _
                AndAlso m_ViewProvider.DesignerPanel IsNot Nothing _
                AndAlso m_ViewProvider.DesignerView.SpecialFiles IsNot Nothing Then
                    Dim ItemId As UInteger
                    Dim FileName As String = Nothing

                    Try
                        'Create the file
                        VSErrorHandler.ThrowOnFailure( _
                            m_ViewProvider.DesignerView.SpecialFiles.GetFile(m_ViewProvider.SpecialFileId, _
                                CUInt(__PSFFLAGS.PSFF_FullPath + __PSFFLAGS.PSFF_CreateIfNotExist), _
                                ItemId, FileName) _
                            )

                        'Set the filename
                        m_ViewProvider.DesignerPanel.MkDocument = FileName

                        'Remove the custom view
                        m_ViewProvider.DesignerPanel.CloseFrame() 'Note: this call may Dispose 'Me'
                        m_ViewProvider.DesignerPanel.CustomViewProvider = Nothing

                        'Now show without the custom view, which will cause the
                        '  real editor to appear on the file
                        m_ViewProvider.DesignerPanel.ShowDesigner(True)
                    Catch ex As Exception
                        AppDesCommon.RethrowIfUnrecoverable(ex)
                        m_ViewProvider.DesignerView.DsMsgBox(ex)
                    End Try
                End If
            End If
        End Sub
    End Class

End Namespace
