' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Private _projectBuildSystem As IVsProjectBuildSystem
        Private _batchCount As Integer


        ''' <summary>
        ''' </summary>
        ''' <param name="projectHierarchy"> The VS project object</param>
        ''' <param name="startBatch">If true, we start a batch process immediately</param>
        Friend Sub New(ByVal projectHierarchy As IVsHierarchy, Optional ByVal startBatch As Boolean = True)
            _projectBuildSystem = TryCast(projectHierarchy, IVsProjectBuildSystem)
            If startBatch AndAlso _projectBuildSystem IsNot Nothing Then
                _projectBuildSystem.StartBatchEdit()
                _batchCount = 1
            End If
        End Sub


        ''' <summary>
        ''' Disposes the object, and end the batch process if necessary
        ''' </summary>
        Friend Sub Dispose() Implements IDisposable.Dispose
            If _batchCount > 0 AndAlso _projectBuildSystem IsNot Nothing Then
                _projectBuildSystem.EndBatchEdit()
                _batchCount = 0
            End If
        End Sub

        ''' <summary>
        ''' Start a batch edit
        ''' </summary>
        Friend Sub StartBatch()
            Debug.Assert(_batchCount >= 0, "We should never call EndBatch more than StartBatch.")
            If _batchCount = 0 AndAlso _projectBuildSystem IsNot Nothing Then
                _projectBuildSystem.StartBatchEdit()
                _batchCount = 1
            Else
                _batchCount = _batchCount + 1
            End If
        End Sub

        ''' <summary>
        ''' End a batch edit
        ''' </summary>
        Friend Sub EndBatch()
            If _batchCount > 0 Then
                If _batchCount = 1 AndAlso _projectBuildSystem IsNot Nothing Then
                    _projectBuildSystem.EndBatchEdit()
                    _batchCount = 0
                Else
                    _batchCount = _batchCount - 1
                End If
            Else
                Debug.Fail("We should never call EndBatch more than StartBatch.")
            End If
        End Sub

    End Class

End Namespace
