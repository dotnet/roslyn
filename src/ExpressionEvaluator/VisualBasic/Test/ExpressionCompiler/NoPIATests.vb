' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class NoPIATests
        Inherits ExpressionCompilerTestBase

        <Fact>
        Public Sub ExplicitEmbeddedType()
            Const source =
"Imports System.Runtime.InteropServices
<TypeIdentifier>
<Guid(""863D5BC0-46A1-49AD-97AA-A5F0D441A9D9"")>
Public Interface I
    Function F() As Object
End Interface
Class C
    Sub M()
        Dim o As I = Nothing
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib(
                {source},
                options:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("Me", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (I V_0) //o
  IL_0000:  ldarg.0
  IL_0001:  ret
}")
        End Sub

    End Class

End Namespace