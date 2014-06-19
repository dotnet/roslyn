Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Reflection.Emit
Imports System.Text
Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic.Emit

    Friend Class PEBuilder

        Friend Shared Sub SetModuleSerializationProperties(moduleBeingBuilt As [Module], compilation As Compilation, win32ResourcesInRESFormat As Stream,
                                                            diagnostics As DiagnosticBag)
            If (win32ResourcesInRESFormat IsNot Nothing) Then
                Dim resources As List(Of RESOURCE)

                Try
                    resources = CvtResFile.ReadResFile(win32ResourcesInRESFormat)
                Catch ex As IOException
                    diagnostics.Add(New Diagnostic(New DiagnosticInfo(ErrorFactory.MessageProvider, DirectCast(ERRID.ERR_ErrorCreatingWin32ResourceFile, Integer), ex.Message), NoLocation.Singleton))
                    Return
                End Try


                If (resources IsNot Nothing) Then
                    Dim resourceList As New List(Of Win32Resource)

                    For Each r In resources
                        Dim result As New Win32Resource(
                            data:=r.data,
                            codePage:=0,
                            languageId:=r.LanguageId,
                            id:=r.pstringName.Ordinal,
                            name:=r.pstringName.theString,
                            typeId:=r.pstringType.Ordinal,
                            TypeName:=r.pstringType.theString)
                        resourceList.Add(result)
                    Next

                    moduleBeingBuilt.Win32Resources = resourceList
                End If

            End If
        End Sub


        Public Shared Function Serialize(executableStream As Stream,
            pdbFileName As String,
            pdbStream As Stream,
            xmlDocStream As Stream,
            xmlNameResolver As Func(Of String, Stream),
            moduleBeingBuilt As [Module],
            metadataOnly As Boolean,
            cancellationToken As CancellationToken
        ) As Boolean
            Dim sourceLocationProvider = New SourceLocationProvider()

            Dim pdbWriter As Microsoft.Cci.PdbWriter = Nothing

            If (pdbStream IsNot Nothing) Then
                Dim istream = New IStreamWrapper(pdbStream)
                pdbWriter = New Microsoft.Cci.PdbWriter(pdbFileName, istream, sourceLocationProvider)
            End If

            Try
                Microsoft.Cci.PeWriter.WritePeToStream(moduleBeingBuilt, moduleBeingBuilt,
                        executableStream,
                        sourceLocationProvider,
                        New LocalScopeProvider(),
                        pdbWriter,
                        metadataOnly,
                        cancellationToken)
            Finally
                If (pdbWriter IsNot Nothing) Then
                    pdbWriter.Dispose()
                    pdbWriter = Nothing
                End If
            End Try

            Return True
        End Function
    End Class
End Namespace
