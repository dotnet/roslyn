' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class MultilineLambdaOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of MultiLineLambdaExpressionSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New MultilineLambdaOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInClassScope()
            Const code = "
Class C
    Dim r = {|span:$$Sub()
            End Sub|}
End Class
"

            Regions(code,
                Region("span", "Sub() ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInMethodScope()
            Const code = "
Class C
    Sub M()
        Dim r = {|span:$$Sub()
                End Sub|}
    End Sub
End Class
"

            Regions(code,
                Region("span", "Sub() ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestFunction()
            Const code = "
Class C
    Dim r = {|span:$$Function(x As Integer, y As List(Of String)) As Func(Of Integer, Func(Of String, Integer))
            End Function|}
End Class
"

            Regions(code,
                Region("span", "Function(x As Integer, y As List(Of String)) As Func(Of Integer, Func(Of String, Integer)) ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInArgumentContext()
            Const code = "
Class C
    Sub M()
        MethodCall({|span:$$Function(x As Integer, y As List(Of String)) As List(Of List(Of String))
                       Return Nothing
                   End Function|})
    End Sub
End Class
"

            Regions(code,
                Region("span", "Function(x As Integer, y As List(Of String)) As List(Of List(Of String)) ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestLambdaWithReturnType()
            Const code = "
Class C
    Sub M()
        Dim f = {|span:$$Function(x) As Integer
                    Return x
                End Function|}
    End Sub
End Class
"

            Regions(code,
                Region("span", "Function(x) As Integer ...", autoCollapse:=False))
        End Sub

    End Class
End Namespace
