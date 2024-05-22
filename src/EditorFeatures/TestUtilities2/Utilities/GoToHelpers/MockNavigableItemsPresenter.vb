' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.FindUsages

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Class MockStreamingFindUsagesPresenter
        Implements IStreamingFindUsagesPresenter

        Public ReadOnly Context As SimpleFindUsagesContext
        Private ReadOnly _action As Action

        Public Sub New(action As Action)
            _action = action
            Context = New SimpleFindUsagesContext()
        End Sub

        Public Sub ClearAll() Implements IStreamingFindUsagesPresenter.ClearAll
            Throw New NotImplementedException()
        End Sub

        Public Function StartSearch(title As String, options As StreamingFindUsagesPresenterOptions) As (FindUsagesContext, CancellationToken) Implements IStreamingFindUsagesPresenter.StartSearch
            _action()
            Return (Context, CancellationToken.None)
        End Function
    End Class
End Namespace
