' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class MultilineLambdaStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of MultiLineLambdaExpressionSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New MultilineLambdaStructureProvider()
        End Function

        <Fact>
        Public Async Function TestInClassScope() As Task
            Const code = "
Class C
    Dim r = {|span:$$Sub()
            End Sub|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub() ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestInMethodScope() As Task
            Const code = "
Class C
    Sub M()
        Dim r = {|span:$$Sub()
                End Sub|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Sub() ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestFunction() As Task
            Const code = "
Class C
    Dim r = {|span:$$Function(x As Integer, y As List(Of String)) As Func(Of Integer, Func(Of String, Integer))
            End Function|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Function(x As Integer, y As List(Of String)) As Func(Of Integer, Func(Of String, Integer)) ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestInArgumentContext() As Task
            Const code = "
Class C
    Sub M()
        MethodCall({|span:$$Function(x As Integer, y As List(Of String)) As List(Of List(Of String))
                       Return Nothing
                   End Function|})
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Function(x As Integer, y As List(Of String)) As List(Of List(Of String)) ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestLambdaWithReturnType() As Task
            Const code = "
Class C
    Sub M()
        Dim f = {|span:$$Function(x) As Integer
                    Return x
                End Function|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Function(x) As Integer ...", autoCollapse:=False))
        End Function
    End Class
End Namespace
