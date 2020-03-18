' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.TextManager.Interop
Imports MSXML

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Friend Class TestExpansionSession
        Implements IVsExpansionSession, IVsExpansionSessionInternal

        Public snippetSpanInSurfaceBuffer As TextSpan
        Public endSpanInSurfaceBuffer As TextSpan

        Public Function GetSnippetSpan(<ComAliasName("Microsoft.VisualStudio.TextManager.Interop.TextSpan")> pts() As TextSpan) As Integer Implements IVsExpansionSession.GetSnippetSpan
            pts(0) = snippetSpanInSurfaceBuffer
            Return 0
        End Function

        Public Function SetEndSpan(<ComAliasName("Microsoft.VisualStudio.TextManager.Interop.TextSpan")> ts As TextSpan) As Integer Implements IVsExpansionSession.SetEndSpan
            endSpanInSurfaceBuffer = ts
            Return 0
        End Function

        Public Function GetEndSpan(<ComAliasName("Microsoft.VisualStudio.TextManager.Interop.TextSpan")> pts() As TextSpan) As Integer Implements IVsExpansionSession.GetEndSpan
            pts(0) = endSpanInSurfaceBuffer
            Return 0
        End Function

        Public Function GetSnippetNode(bstrNode As String, ByRef pNode As IntPtr) As Integer Implements IVsExpansionSessionInternal.GetSnippetNode
            Return -1
        End Function

        Public Function EndCurrentExpansion(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")> fLeaveCaret As Integer) As Integer Implements IVsExpansionSession.EndCurrentExpansion
            Throw New NotImplementedException()
        End Function

        Public Function GetDeclarationNode(bstrNode As String, ByRef pNode As IXMLDOMNode) As Integer Implements IVsExpansionSession.GetDeclarationNode
            Throw New NotImplementedException()
        End Function

        Public Function GetFieldSpan(bstrField As String, <ComAliasName("Microsoft.VisualStudio.TextManager.Interop.TextSpan")> ptsSpan() As TextSpan) As Integer Implements IVsExpansionSession.GetFieldSpan
            Throw New NotImplementedException()
        End Function

        Public Function GetFieldValue(bstrFieldName As String, ByRef pbstrValue As String) As Integer Implements IVsExpansionSession.GetFieldValue
            Throw New NotImplementedException()
        End Function

        Public Function GetHeaderNode(bstrNode As String, ByRef pNode As IXMLDOMNode) As Integer Implements IVsExpansionSession.GetHeaderNode
            Throw New NotImplementedException()
        End Function

        Public Function GetSnippetNode(bstrNode As String, ByRef pNode As IXMLDOMNode) As Integer Implements IVsExpansionSession.GetSnippetNode
            Throw New NotImplementedException()
        End Function

        Public Function GoToNextExpansionField(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")> fCommitIfLast As Integer) As Integer Implements IVsExpansionSession.GoToNextExpansionField
            Throw New NotImplementedException()
        End Function

        Public Function GoToPreviousExpansionField() As Integer Implements IVsExpansionSession.GoToPreviousExpansionField
            Throw New NotImplementedException()
        End Function

        Public Function SetFieldDefault(bstrFieldName As String, bstrNewValue As String) As Integer Implements IVsExpansionSession.SetFieldDefault
            Throw New NotImplementedException()
        End Function

        Public Sub Reserved1() Implements IVsExpansionSessionInternal.Reserved1
            Throw New NotImplementedException()
        End Sub

        Public Sub Reserved2() Implements IVsExpansionSessionInternal.Reserved2
            Throw New NotImplementedException()
        End Sub

        Public Sub Reserved3() Implements IVsExpansionSessionInternal.Reserved3
            Throw New NotImplementedException()
        End Sub

        Public Sub Reserved4() Implements IVsExpansionSessionInternal.Reserved4
            Throw New NotImplementedException()
        End Sub

        Public Sub Reserved5() Implements IVsExpansionSessionInternal.Reserved5
            Throw New NotImplementedException()
        End Sub

        Public Sub Reserved6() Implements IVsExpansionSessionInternal.Reserved6
            Throw New NotImplementedException()
        End Sub

        Public Sub Reserved7() Implements IVsExpansionSessionInternal.Reserved7
            Throw New NotImplementedException()
        End Sub

        Public Sub Reserved8() Implements IVsExpansionSessionInternal.Reserved8
            Throw New NotImplementedException()
        End Sub

        Public Sub Reserved9() Implements IVsExpansionSessionInternal.Reserved9
            Throw New NotImplementedException()
        End Sub

        Public Sub Reserved10() Implements IVsExpansionSessionInternal.Reserved10
            Throw New NotImplementedException()
        End Sub

        Public Sub Reserved11() Implements IVsExpansionSessionInternal.Reserved11
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace
