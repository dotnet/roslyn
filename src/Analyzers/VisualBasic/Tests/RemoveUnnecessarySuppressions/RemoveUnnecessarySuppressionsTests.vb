' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessarySuppressions.VisualBasicRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions.RemoveUnnecessaryAttributeSuppressionsCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnnecessarySuppressions

    <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessarySuppressions)>
    <WorkItem("https://github.com/dotnet/roslyn/issues/44176")>
    Public Class RemoveUnnecessarySuppressionsTests
        <Theory, CombinatorialData>
        Public Sub TestStandardProperty([property] As AnalyzerProperty)
            VerifyVB.VerifyStandardProperty([property])
        End Sub

        <Theory>
        <InlineData("Scope:=""member""", "Target:=""~F:N.C.F""", "Assembly")>
        <InlineData("Scope:=""member""", "Target:=""~P:N.C.P""", "Assembly")>
        <InlineData("Scope:=""member""", "Target:=""~M:N.C.M()""", "Assembly")>
        <InlineData("Scope:=""member""", "Target:=""~T:N.C""", "Assembly")>
        <InlineData("Scope:=""namespace""", "Target:=""~N:N""", "Assembly")>
        <InlineData("Scope:=""namespaceanddescendants""", "Target:=""~N:N""", "Assembly")>
        <InlineData(Nothing, Nothing, "Assembly")>
        <InlineData("Scope:=""module""", Nothing, "Assembly")>
        <InlineData("Scope:=""module""", "Target:=Nothing", "Assembly")>
        <InlineData("Scope:=""resource""", "Target:=""""", "Assembly")>
        <InlineData("Scope:=""member""", "Target:=""~M:N.C.M()""", "Module")>
        <InlineData("Scope:=""type""", "Target:=""~M:N.C.M()""", "Assembly")>
        <InlineData("Scope:=""namespace""", "Target:=""~F:N.C.F""", "Assembly")>
        <InlineData("Scope:=""Member""", "Target:=""~F:N.C.F""", "Assembly")>
        <InlineData("Scope:=""MEMBER""", "Target:=""~F:N.C.F""", "Assembly")>
        Public Async Function ValidSuppressions(scope As String, target As String, attributeTarget As String) As Task
            Dim scopeString = If(scope IsNot Nothing, $", {scope}", String.Empty)
            Dim targetString = If(target IsNot Nothing, $", {target}", String.Empty)

            Dim input = $"
<{attributeTarget}: System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""Id: Title"", Justification:=""Pending""{scopeString}{targetString})>

Namespace N
  Class C
    Public F As Integer

    Public ReadOnly Property P As Integer

    Public Sub M()
    End Sub
  End Class
End Namespace
"
            Await VerifyVB.VerifyCodeFixAsync(input, input)
        End Function

        <Theory>
        <InlineData("Scope:=""member""", "Target:=""~F:N.C.F2""", "Assembly")>
        <InlineData("Scope:=""Member""", "Target:=""~F:N.C.F2""", "Assembly")>
        <InlineData("Scope:=""MEMBER""", "Target:=""~F:N.C.F2""", "Assembly")>
        <InlineData("Scope:=""invalid""", "Target:=""~P:N.C.P""", "Assembly")>
        <InlineData("Scope:=""member""", "Target:=""~M:N.C.M(System.Int32)""", "Assembly")>
        <InlineData("Scope:=""module""", "Target:=""~M:N.C.M()""", "Assembly")>
        <InlineData("Scope:=Nothing", "Target:=""~M:N.C.M()""", "Assembly")>
        <InlineData(Nothing, "Target:=""~M:N.C.M()""", "Assembly")>
        <InlineData("Scope:=""member""", "Target:=Nothing", "Assembly")>
        <InlineData("Scope:=""member""", Nothing, "Assembly")>
        <InlineData("Scope:=""type""", "Target:=""~T:N2.C""", "Assembly")>
        <InlineData("Scope:=""namespace""", "Target:=""~N:N.N2""", "Assembly")>
        <InlineData("Scope:=""namespaceanddescendants""", "Target:=""""", "Assembly")>
        <InlineData(Nothing, "Target:=""""", "Assembly")>
        <InlineData(Nothing, "Target:=""~T:N.C""", "Assembly")>
        <InlineData("Scope:=""module""", "Target:=""""", "Assembly")>
        <InlineData("Scope:=""module""", "Target:=""~T:N.C""", "Assembly")>
        Public Async Function InvalidSuppressions(scope As String, target As String, attributeTarget As String) As Task
            Dim scopeString = If(scope IsNot Nothing, $", {scope}", String.Empty)
            Dim targetString = If(target IsNot Nothing, $", {target}", String.Empty)

            Dim input = $"
<[|{attributeTarget}: System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""Id: Title"", Justification:=""Pending""{scopeString}{targetString})|]>

Namespace N
  Class C
    Public F As Integer

    Public ReadOnly Property P As Integer

    Public Sub M()
    End Sub
  End Class
End Namespace
"
            Dim fixedCode = $"

Namespace N
  Class C
    Public F As Integer

    Public ReadOnly Property P As Integer

    Public Sub M()
    End Sub
  End Class
End Namespace
"
            Await VerifyVB.VerifyCodeFixAsync(input, fixedCode)
        End Function

        <Fact>
        Public Async Function ValidAndInvalidSuppressions() As Task
            Dim attributePrefix = "Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""Id: Title"", Justification:=""Pending"""
            Dim validSuppression = $"{attributePrefix}, Scope:=""member"", Target:=""~T:C"")"
            Dim invalidSuppression = $"[|{attributePrefix}, Scope:=""member"", Target:="""")|]"

            Dim input = $"
<{validSuppression}>
<{invalidSuppression}>
<{validSuppression}, {validSuppression}>
<{invalidSuppression}, {invalidSuppression}>
<{validSuppression}, {invalidSuppression}>
<{invalidSuppression}, {validSuppression}>
<{invalidSuppression}, {validSuppression}, {invalidSuppression}, {validSuppression}>

Class C
End Class
"
            Dim fixedCode = $"
<{validSuppression}>
<{validSuppression}, {validSuppression}>
<{validSuppression}>
<{validSuppression}>
<{validSuppression}, {validSuppression}>

Class C
End Class
"
            Await VerifyVB.VerifyCodeFixAsync(input, fixedCode)
        End Function

        <Theory>
        <InlineData("")>
        <InlineData(", Scope:=""member"", Target:=""~M:C.M()""")>
        <InlineData(", Scope:=""invalid"", Target:=""invalid""")>
        Public Async Function LocalSuppressions(ByVal scopeAndTarget As String) As Task
            Dim input = $"
<System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""Id: Title"", Justification:=""Pending""{scopeAndTarget})>
Class C
    Sub M()
    End Sub
End Class"
            Await VerifyVB.VerifyCodeFixAsync(input, input)
        End Function
    End Class
End Namespace
