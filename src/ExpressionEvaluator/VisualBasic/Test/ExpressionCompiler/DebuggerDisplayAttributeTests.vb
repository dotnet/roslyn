' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class DebuggerDisplayAttributeTests
        Inherits ExpressionCompilerTestBase

        <Fact()>
        Public Sub VirtualMethod()
            Const source = "
Imports System.Diagnostics

<DebuggerDisplay(""{GetDebuggerDisplay()}"")>
Public Class Base
    Protected Overridable Function GetDebuggerDisplay() As String
        Return ""Base""
    End Function
End Class

Public Class Derived
    Inherits Base

    Protected Overrides Function GetDebuggerDisplay() As String
        Return ""Derived""
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateTypeContext(runtime, "Derived")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            Dim result = context.CompileExpression("GetDebuggerDisplay()", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""Function Derived.GetDebuggerDisplay() As String""
  IL_0006:  ret
}")
        End Sub
    End Class
End Namespace