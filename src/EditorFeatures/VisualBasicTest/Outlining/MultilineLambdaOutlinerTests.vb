' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class MultilineLambdaOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of MultiLineLambdaExpressionSyntax)

        Friend Overrides Function GetRegions(lambdaExpression As MultiLineLambdaExpressionSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New MultilineLambdaOutliner
            Return outliner.GetOutliningSpans(lambdaExpression, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInClassScope()
            Dim syntaxTree = ParseLines("Class Class1",
                                  "Dim r = Sub()",
                                  "End Sub",
                                  "End Class")
            Dim lambdaExpression = syntaxTree.GetCompilationUnitRoot().FindFirstNodeOfType(Of MultiLineLambdaExpressionSyntax)()
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(22, 36),
                "Sub() ...",
                False)
            Dim actualRegion = GetRegion(lambdaExpression)
            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInMethodScope()
            Dim syntaxTree = ParseLines("Class Class1",
                                  "Sub Method1()",
                                  "Dim r = Sub()",
                                  "End Sub",
                                  "End Sub",
                                  "End Class")
            Dim lambdaExpression = syntaxTree.GetCompilationUnitRoot().FindFirstNodeOfType(Of MultiLineLambdaExpressionSyntax)()
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(37, 51),
                "Sub() ...",
                False)
            Dim actualRegion = GetRegion(lambdaExpression)
            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestFunction()
            Dim syntaxTree = ParseLines("Class Class1",
                                  "Dim r = Function(x As Integer, y As List(Of String)) As Func(Of Integer, Func(Of String, Integer))",
                                  "End Function",
                                  "End Class")
            Dim lambdaExpression = syntaxTree.GetCompilationUnitRoot().FindFirstNodeOfType(Of MultiLineLambdaExpressionSyntax)()
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(22, 126),
                "Function(x As Integer, y As List(Of String)) As Func(Of Integer, Func(Of String, Integer)) ...",
                False)
            Dim actualRegion = GetRegion(lambdaExpression)
            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInArgumentContext()
            Dim syntaxTree = ParseLines("Class Class1",
                                  "Sub Main()",
                                  "MethodCall(Function(x As Integer, y As List(Of String)) As List(Of List(Of String))",
                                  " Return Nothing",
                                  " End Function)",
                                  "End Sub",
                                  "End Class")

            Dim lambdaExpression = syntaxTree.GetCompilationUnitRoot().FindFirstNodeOfType(Of MultiLineLambdaExpressionSyntax)()
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(37, 141),
                "Function(x As Integer, y As List(Of String)) As List(Of List(Of String)) ...",
                False)
            Dim actualRegion = GetRegion(lambdaExpression)
            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestLambdaWithReturnType()
            Dim syntaxTree = ParseLines("Class C1",
                                 "  Sub M1()",
                                 "    Dim f = Function(x) As Integer",
                                 "              return x",
                                 "            End Function",
                                 "  End Sub",
                                 "End Class")

            Dim lambdaExpression = syntaxTree.GetCompilationUnitRoot().FindFirstNodeOfType(Of MultiLineLambdaExpressionSyntax)()

            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(34, 106),
                                     "Function(x) As Integer ...",
                                     autoCollapse:=False)
            Dim actualRegion = GetRegion(lambdaExpression)
            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
