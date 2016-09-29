' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class ScriptSemanticsTests
        Inherits BasicTestBase

        <WorkItem(530404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530404")>
        <Fact>
        Public Sub DiagnosticsPass()
            Dim source0 = "
Function F(e As System.Linq.Expressions.Expression(Of System.Func(Of Object))) As Object
    Return e.Compile()()
End Function"

            Dim c0 = CreateSubmission(source0, {SystemCoreRef})

            Dim source1 = "
F(Function()
    Return Nothing
  End Function)
"
            Dim c1 = CreateSubmission(source1, {SystemCoreRef}, previous:=c0)

            AssertTheseDiagnostics(c1,
<errors>
BC36675: Statement lambdas cannot be converted to expression trees.
F(Function()
  ~~~~~~~~~~~
</errors>)
        End Sub
    End Class
End Namespace

