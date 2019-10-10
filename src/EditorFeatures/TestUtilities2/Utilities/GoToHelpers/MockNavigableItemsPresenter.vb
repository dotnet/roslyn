' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.FindUsages
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.FindUsages

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
    Friend Class MockStreamingFindUsagesPresenter
        Implements IStreamingFindUsagesPresenter

        Public ReadOnly Context As New SimpleFindUsagesContext(CancellationToken.None)
        Private ReadOnly _action As Action

        Public Sub New(action As Action)
            _action = action
        End Sub

        Public Sub ClearAll() Implements IStreamingFindUsagesPresenter.ClearAll
            Throw New NotImplementedException()
        End Sub

        Public Function StartSearch(title As String, alwaysShowDeclarations As Boolean) As FindUsagesContext Implements IStreamingFindUsagesPresenter.StartSearch
            _action()
            Return Context
        End Function

        Public Function StartSearchWithCustomColumns(title As String, supportsReferences As Boolean, includeContainingTypeAndMemberColumns As Boolean, includeKindColumn As Boolean) As FindUsagesContext Implements IStreamingFindUsagesPresenter.StartSearchWithCustomColumns
            Return Context
        End Function
    End Class
End Namespace
