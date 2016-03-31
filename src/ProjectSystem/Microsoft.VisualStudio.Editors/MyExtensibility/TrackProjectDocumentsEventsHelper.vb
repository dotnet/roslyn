Option Strict On
Option Explicit On
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Editors.Interop

Namespace Microsoft.VisualStudio.Editors.MyExtensibility
    Friend Class TrackProjectDocumentsEventsHelper
        Implements IVsTrackProjectDocumentsEvents2

        Public Shared Function GetInstance(ByVal serviceProvider As IServiceProvider) As TrackProjectDocumentsEventsHelper
            Try
                Return New TrackProjectDocumentsEventsHelper(serviceProvider)
            Catch ex As Exception
                Debug.Fail(String.Format("Fail to listen to IVsTrackProjectDocumentsEvents2: {0}", ex.ToString()))
                Return Nothing
            End Try
        End Function

        Public Event AfterAddFilesEx(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As VSADDFILEFLAGS)
        Public Event AfterRemoveFiles(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As VSREMOVEFILEFLAGS)
        Public Event AfterRenameFiles(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As VSRENAMEFILEFLAGS)
        Public Event AfterRemoveDirectories(ByVal cProjects As Integer, ByVal cDirectories As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As VSREMOVEDIRECTORYFLAGS)
        Public Event AfterRenameDirectories(ByVal cProjects As Integer, ByVal cDirs As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As VSRENAMEDIRECTORYFLAGS)

        ''' ;UnAdviseTrackProjectDocumentsEvents
        ''' <summary>
        ''' Stop listening to IVsTrackProjectDocumentsEvents2
        ''' </summary>
        Public Sub UnAdviseTrackProjectDocumentsEvents()
            If m_vsTrackProjectDocumentsEventsCookie <> 0 Then
                If m_vsTrackProjectDocuments IsNot Nothing Then
                    m_vsTrackProjectDocuments.UnadviseTrackProjectDocumentsEvents(m_vsTrackProjectDocumentsEventsCookie)
                    m_vsTrackProjectDocuments = Nothing
                End If
                m_vsTrackProjectDocumentsEventsCookie = 0
            End If
        End Sub

        Private Sub New(ByVal serviceProvider As IServiceProvider)
            If serviceProvider Is Nothing Then
                Throw New ArgumentNullException("serviceProvider")
            End If

            m_ServiceProvider = serviceProvider

            m_vsTrackProjectDocuments = TryCast(m_ServiceProvider.GetService(GetType(SVsTrackProjectDocuments)), _
                IVsTrackProjectDocuments2)
            If m_vsTrackProjectDocuments Is Nothing Then
                Throw New Exception("Could not get IVsTrackProjectDocuments2!")
            End If

            ErrorHandler.ThrowOnFailure( _
                m_vsTrackProjectDocuments.AdviseTrackProjectDocumentsEvents(Me, m_vsTrackProjectDocumentsEventsCookie))
            Debug.Assert(m_vsTrackProjectDocumentsEventsCookie <> 0)
        End Sub

#Region " IVsTrackProjectDocumentsEvents2 methods that are handled "
        Private Function OnAfterAddFilesEx(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As VSADDFILEFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx
            RaiseEvent AfterAddFilesEx(cProjects, cFiles, rgpProjects, rgFirstIndices, rgpszMkDocuments, rgFlags)
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRemoveFiles(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As VSREMOVEFILEFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles
            RaiseEvent AfterRemoveFiles(cProjects, cFiles, rgpProjects, rgFirstIndices, rgpszMkDocuments, rgFlags)
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRenameFiles(ByVal cProjects As Integer, ByVal cFiles As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As VSRENAMEFILEFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles
            RaiseEvent AfterRenameFiles(cProjects, cFiles, rgpProjects, rgFirstIndices, rgszMkOldNames, rgszMkNewNames, rgFlags)
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRemoveDirectories(ByVal cProjects As Integer, ByVal cDirectories As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As VSREMOVEDIRECTORYFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRemoveDirectories
            RaiseEvent AfterRemoveDirectories(cProjects, cDirectories, rgpProjects, rgFirstIndices, rgpszMkDocuments, rgFlags)
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRenameDirectories(ByVal cProjects As Integer, ByVal cDirs As Integer, ByVal rgpProjects() As IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgszMkOldNames() As String, ByVal rgszMkNewNames() As String, ByVal rgFlags() As VSRENAMEDIRECTORYFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRenameDirectories
            RaiseEvent AfterRenameDirectories(cProjects, cDirs, rgpProjects, rgFirstIndices, rgszMkOldNames, rgszMkNewNames, rgFlags)
            Return NativeMethods.S_OK
        End Function
#End Region

#Region " IVsTrackProjectDocumentsEvents2 methods that are ignored "

        Private Function OnAfterAddDirectoriesEx(ByVal cProjects As Integer, ByVal cDirectories As Integer, ByVal rgpProjects() As Shell.Interop.IVsProject, ByVal rgFirstIndices() As Integer, ByVal rgpszMkDocuments() As String, ByVal rgFlags() As Shell.Interop.VSADDDIRECTORYFLAGS) As Integer Implements Shell.Interop.IVsTrackProjectDocumentsEvents2.OnAfterAddDirectoriesEx
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

        Private m_ServiceProvider As IServiceProvider
        Private m_vsTrackProjectDocuments As IVsTrackProjectDocuments2
        Private m_vsTrackProjectDocumentsEventsCookie As UInteger
    End Class
End Namespace
