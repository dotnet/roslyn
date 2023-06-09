' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessarySuppressions.VisualBasicRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.UpdateLegacySuppressions.UpdateLegacySuppressionsCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UpdateLegacySuppressions

    <Trait(Traits.Feature, Traits.Features.CodeActionsUpdateLegacySuppressions)>
    <WorkItem("https://github.com/dotnet/roslyn/issues/44362")>
    Public Class UpdateLegacySuppressionsTests
        <Theory, CombinatorialData>
        Public Sub TestStandardProperty([property] As AnalyzerProperty)
            VerifyVB.VerifyStandardProperty([property])
        End Sub

        <Theory>
        <InlineData("namespace", "N", "~N:N")>
        <InlineData("type", "N.C+D", "~T:N.C.D")>
        <InlineData("member", "N.C.#F", "~F:N.C.F")>
        <InlineData("member", "N.C.#P", "~P:N.C.P")>
        <InlineData("member", "N.C.#M", "~M:N.C.M")>
        <InlineData("member", "N.C.#M2(!!0)", "~M:N.C.M2``1(``0)~System.Int32")>
        <InlineData("member", "e:N.C.#E", "~E:N.C.E")>
        Public Async Function LegacySuppressions(scope As String, target As String, fixedTarget As String) As Task
            Dim input = $"
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""Id: Title"", Scope:=""{scope}"", Target:={{|#0:""{target}""|}})>

Namespace N
    Class C
        Private F As Integer
        Public Property P As Integer

        Public Sub M()
        End Sub

        Public Function M2(Of T)(tParam As T) As Integer
            Return 0
        End Function

        Public Event E As System.EventHandler(Of Integer)

        Class D
        End Class
    End Class
End Namespace
"
            Dim expectedDiagnostic = VerifyVB.Diagnostic(AbstractRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer.LegacyFormatTargetDescriptor).
                                        WithLocation(0).
                                        WithArguments(target)

            Dim fixedCode = $"
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""Id: Title"", Scope:=""{scope}"", Target:=""{fixedTarget}"")>

Namespace N
    Class C
        Private F As Integer
        Public Property P As Integer

        Public Sub M()
        End Sub

        Public Function M2(Of T)(tParam As T) As Integer
            Return 0
        End Function

        Public Event E As System.EventHandler(Of Integer)

        Class D
        End Class
    End Class
End Namespace
"
            Await VerifyVB.VerifyCodeFixAsync(input, expectedDiagnostic, fixedCode)
        End Function
    End Class
End Namespace
