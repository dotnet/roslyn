Option Strict On
Option Explicit On

Imports Microsoft.VisualStudio.Shell.Interop


Namespace Microsoft.VisualStudio.Editors.Common

    ''' <summary>
    ''' ProjectBatchEdit:
    '''    Utility class that will create a batch operation in the project system
    '''    The project system will hold build process until the batch operation ends.
    '''    with the Using keyword as follows:
    ''' 
    '''    Sub Func()
    '''        Using New ProjectBatchEdit(projectHierarchy)
    '''            (do work)
    '''        End Using
    '''    End Sub
    ''' </summary>
    Friend Class ProjectBatchEdit
        Implements IDisposable

        Private projectBuildSystem As IVsProjectBuildSystem
        Private batchCount As Integer


        ''' <summary>
        ''' </summary>
        ''' <param name="projectHierarchy"> The VS project object</param>
        ''' <param name="startBatch">If true, we start a batch process immediately</param>
        Friend Sub New(ByVal projectHierarchy As IVsHierarchy, Optional ByVal startBatch As Boolean = True)
            projectBuildSystem = TryCast(projectHierarchy, IVsProjectBuildSystem)
            If startBatch AndAlso projectBuildSystem IsNot Nothing Then
                projectBuildSystem.StartBatchEdit()
                batchCount = 1
            End If
        End Sub


        ''' <summary>
        ''' Disposes the object, and end the batch process if necessary
        ''' </summary>
        Friend Sub Dispose() Implements IDisposable.Dispose
            If batchCount > 0 AndAlso projectBuildSystem IsNot Nothing Then
                projectBuildSystem.EndBatchEdit()
                batchCount = 0
            End If
        End Sub

        ''' <summary>
        ''' Start a batch edit
        ''' </summary>
        Friend Sub StartBatch()
            Debug.Assert(batchCount >= 0, "We should never call EndBatch more than StartBatch.")
            If batchCount = 0 AndAlso projectBuildSystem IsNot Nothing Then
                projectBuildSystem.StartBatchEdit()
                batchCount = 1
            Else
                batchCount = batchCount + 1
            End If
        End Sub

        ''' <summary>
        ''' End a batch edit
        ''' </summary>
        Friend Sub EndBatch()
            If batchCount > 0 Then
                If batchCount = 1 AndAlso projectBuildSystem IsNot Nothing Then
                    projectBuildSystem.EndBatchEdit()
                    batchCount = 0
                Else
                    batchCount = batchCount - 1
                End If
            Else
                Debug.Fail("We should never call EndBatch more than StartBatch.")
            End If
        End Sub

    End Class

End Namespace
