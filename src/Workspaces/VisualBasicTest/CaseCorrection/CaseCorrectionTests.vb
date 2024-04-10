' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.CaseCorrection
    Public Class CaseCorrectionTests
        Inherits VisualBasicCaseCorrectionTestBase

        <Fact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/45508")>
        Public Async Function NamedTupleIdentifier() As Task
            Dim code = <Code>Class C
    Sub NamedTupleCasing(value1 As Integer)
        Dim a = (Value1:=Value1, Value2:=2)
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub NamedTupleCasing(value1 As Integer)
        Dim a = (Value1:=value1, Value2:=2)
    End Sub
End Class</Code>

            Await AssertCaseCorrectAsync(code.Value, expected.Value)
        End Function

    End Class
End Namespace
