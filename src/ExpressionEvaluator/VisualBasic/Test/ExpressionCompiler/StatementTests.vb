' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class StatementTests
        Inherits ExpressionCompilerTestBase

        <Fact>
        Public Sub AddAssignmentStatement()
            Const source = "
Class C
    Sub M()
        Dim d As Double = 0
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="d += 1.1",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (Double V_0) //d
  IL_0000:  ldloc.0
  IL_0001:  ldc.r8     1.1
  IL_000a:  add
  IL_000b:  stloc.0
  IL_000c:  ret
}
")
        End Sub

        <Fact>
        Public Sub AddHandlerStatement()
            Const source = "
Class C
    Event a()
    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="  AddHandler a, AddressOf System.Console.WriteLine",
                errorMessage:=errorMessage)

            'Statements of type 'AddHandlerStatement' are not allowed in the Immediate window.
            Assert.Equal(String.Format(ExpressionEvaluator.Resources.InvalidDebuggerStatement, SyntaxKind.AddHandlerStatement), errorMessage)
        End Sub

        <Fact>
        Public Sub ArrayAccess()
            Const source = "
Module Module1
    Sub Main(args() As String)
    End Sub
End Module
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="Module1.Main",
                expr:="args(1)",
                errorMessage:=errorMessage)
            Assert.Equal("error BC30454: Expression is not a method.", errorMessage)

            testData = EvaluateStatement(
                source,
                methodName:="Module1.Main",
                expr:="args(1) = ""Hi""",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldstr      ""Hi""
  IL_0007:  stelem.ref
  IL_0008:  ret
}
")

            testData = EvaluateStatement(
                source,
                methodName:="Module1.Main",
                expr:="?args(1)",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldelem.ref
  IL_0003:  ret
}
")

            testData = EvaluateStatement(
                source,
                methodName:="Module1.Main",
                expr:="?args(1) = ""Hi""",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldelem.ref
  IL_0003:  ldstr      ""Hi""
  IL_0008:  ldc.i4.0
  IL_0009:  call       ""Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer""
  IL_000e:  ldc.i4.0
  IL_000f:  ceq
  IL_0011:  ret
}
")
        End Sub

        <Fact>
        Public Sub CallStatement()
            Const source = "
Class C
    Private member As Integer = 1
    Public Sub Increment()
        member += 1
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.Increment",
                expr:="Call (New C).Increment()",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (Integer& V_0)
  IL_0000:  newobj     ""Sub C..ctor()""
  IL_0005:  call       ""Sub C.Increment()""
  IL_000a:  ret
}
")
        End Sub

        <Fact>
        Public Sub ConcatenateAssignmentStatement()
            Const source = "
Module Module1
    Sub M()
        Dim s As String = ""a""
    End Sub
End Module
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="Module1.M",
                expr:="s &= 1",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       14 (0xe)
  .maxstack  2
  .locals init (String V_0) //s
  IL_0000:  ldloc.0
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String""
  IL_0007:  call       ""Function String.Concat(String, String) As String""
  IL_000c:  stloc.0
  IL_000d:  ret
}
")
        End Sub

        <Fact>
        Public Sub DivideAssignmentStatement()
            Const source = "
Class C
    Sub M()
        Dim b As Byte = 1
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="b /= 0",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (Byte V_0) //b
  IL_0000:  ldloc.0
  IL_0001:  conv.r8
  IL_0002:  ldc.r8     0
  IL_000b:  div
  IL_000c:  call       ""Function System.Math.Round(Double) As Double""
  IL_0011:  conv.ovf.u1
  IL_0012:  stloc.0
  IL_0013:  ret
}
")
        End Sub

        <Fact>
        Public Sub EmptyStatement()
            Const source = "
Class C
    Sub M()
    End Sub
End Class
"
            ' We should never hit this case in the VS scenario, but testing to make
            ' sure we do something reasonable if we ever did.
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="",
                errorMessage:=errorMessage)

            'Statements of type 'EmptyStatement' are not allowed in the Immediate window.
            Assert.Equal(String.Format(ExpressionEvaluator.Resources.InvalidDebuggerStatement, SyntaxKind.EmptyStatement), errorMessage)
        End Sub

        <Fact>
        Public Sub EndStatement()
            Const source = "
Class C
    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="End",
                errorMessage:=errorMessage)

            'Statements of type 'EndStatement' are not allowed in the Immediate window.
            Assert.Equal(String.Format(ExpressionEvaluator.Resources.InvalidDebuggerStatement, SyntaxKind.EndStatement), errorMessage)
        End Sub

        <Fact>
        Public Sub ExponentiateAssignmentStatement()
            Const source = "
Class C
    Sub M()
        Dim m As Decimal
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="m ^= 2",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (Decimal V_0) //m
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldloc.0
  IL_0003:  call       ""Function System.Convert.ToDouble(Decimal) As Double""
  IL_0008:  ldc.r8     2
  IL_0011:  call       ""Function System.Math.Pow(Double, Double) As Double""
  IL_0016:  call       ""Sub Decimal..ctor(Double)""
  IL_001b:  ret
}
")
        End Sub

        <Fact>
        Public Sub ExpressionStatement()
            Const source = "
Class C
    Public Sub M()
        Dim x As Integer = 1
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="System.Console.WriteLine()",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        6 (0x6)
  .maxstack  0
  .locals init (Integer V_0) //x
  IL_0000:  call       ""Sub System.Console.WriteLine()""
  IL_0005:  ret
}
")

            testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="System.Console.Read()",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0) //x
  IL_0000:  call       ""Function System.Console.Read() As Integer""
  IL_0005:  pop
  IL_0006:  ret
}
")

            testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="x",
                errorMessage:=errorMessage)
            Assert.Equal("error BC30454: Expression is not a method.", errorMessage)
        End Sub

        <Fact>
        Public Sub IfStatement()
            Const source = "
Class C
    Sub M()
        Dim x As Integer = 0
    End Sub
End Class
"
            ' The old EE used to support single-line If statements, but Roslyn does not.  This is because Parser.ParseStatementInMethodBody
            ' does directly handle the single-line case. If we end up needing to support single-line if, then we either need to fix parsing
            ' of single-line Ifs in ParseStatementInMethodBody (ADGreen knows more), or we need to duplicate the parser logic for handling
            ' them in the EE.
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="  If True Then x = 1",
                errorMessage:=errorMessage)
            Assert.Equal("error BC30035: Syntax error.", errorMessage)

            testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="  If True Then x = 1 Else x = 2",
                errorMessage:=errorMessage)
            Assert.Equal("error BC30035: Syntax error.", errorMessage)
        End Sub

        <Fact>
        Public Sub IntegerDivideAssignmentStatement()
            Const source = "
Class C
    Sub M()
        Dim odd As UInteger
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="odd \= 2",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (UInteger V_0) //odd
  IL_0000:  ldloc.0
  IL_0001:  conv.u8
  IL_0002:  ldc.i4.2
  IL_0003:  conv.i8
  IL_0004:  div
  IL_0005:  conv.ovf.u4
  IL_0006:  stloc.0
  IL_0007:  ret
}
")
        End Sub

        <Fact>
        Public Sub LeftShiftAssignmentStatement()
            Const source = "
Class C
    Sub M()
        Dim s As Short
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="s <<= 16",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       10 (0xa)
  .maxstack  3
  .locals init (Short V_0) //s
  IL_0000:  ldloc.0
  IL_0001:  ldc.i4.s   16
  IL_0003:  ldc.i4.s   15
  IL_0005:  and
  IL_0006:  shl
  IL_0007:  conv.i2
  IL_0008:  stloc.0
  IL_0009:  ret
}
")
        End Sub

        <Fact>
        Public Sub MultiplyAssignmentStatement()
            Const source = "
Class C
    Sub M()
        Dim l As ULong
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="l *= l",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        5 (0x5)
  .maxstack  2
  .locals init (ULong V_0) //l
  IL_0000:  ldloc.0
  IL_0001:  ldloc.0
  IL_0002:  mul.ovf.un
  IL_0003:  stloc.0
  IL_0004:  ret
}
")
        End Sub

        <Fact>
        Public Sub OnErrorResumeNextStatement()
            Const source = "
Class C
    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="On Error Resume Next",
                errorMessage:=errorMessage)

            'Statements of type 'OnErrorResumeNextStatement' are not allowed in the Immediate window.
            Assert.Equal(String.Format(ExpressionEvaluator.Resources.InvalidDebuggerStatement, SyntaxKind.OnErrorResumeNextStatement), errorMessage)
        End Sub

        <Fact>
        Public Sub PrintStatement()
            Const source = "
Class C
    Public Sub M()
        Dim x As Integer = 1
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="?x = 1",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        5 (0x5)
  .maxstack  2
  .locals init (Integer V_0) //x
  IL_0000:  ldloc.0
  IL_0001:  ldc.i4.1
  IL_0002:  ceq
  IL_0004:  ret
}
")

            ' We should never hit this case in the VS scenario, but testing to make
            ' sure we do something reasonable if we ever did.
            testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="? ",
                errorMessage:=errorMessage)
            Assert.Equal("error BC30201: Expression expected.", errorMessage)

            testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="??x = 1",
                errorMessage:=errorMessage)
            Assert.Equal("error BC30201: Expression expected.", errorMessage)

            testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="?System.Console.WriteLine()",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        6 (0x6)
  .maxstack  0
  .locals init (Integer V_0) //x
  IL_0000:  call       ""Sub System.Console.WriteLine()""
  IL_0005:  ret
}
")

            testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="?System.Console.Read()",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (Integer V_0) //x
  IL_0000:  call       ""Function System.Console.Read() As Integer""
  IL_0005:  ret
}
")

            testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="?AddressOf System.Console.WriteLine",
                errorMessage:=errorMessage)
            Assert.Equal("error BC30491: Expression does not produce a value.", errorMessage)

            testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="?x += 1",
                errorMessage:=errorMessage)
            Assert.Equal("error BC37237: '+=' is not a valid format specifier", errorMessage) ' not the best error, but not worth modifying parsing to improve...
        End Sub

        <Fact>
        Public Sub RedimStatement()
            Const source = "
Class C
    Public Sub M()
        Dim a(1) As Integer
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="ReDim a(2), a(3)",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Integer() V_0) //a
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     ""Integer""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.4
  IL_0008:  newarr     ""Integer""
  IL_000d:  stloc.0
  IL_000e:  ret
}
")
            testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="?ReDim a(2), a(3)",
                errorMessage:=errorMessage)
            Assert.Equal("error BC30201: Expression expected.", errorMessage)
        End Sub

        <Fact>
        Public Sub RedimPreserveStatement()
            Const source = "
Module Module1
    Public Sub M()
        Dim a(1) As Integer
    End Sub
End Module
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="Module1.M",
                expr:="ReDim Preserve a(2)",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (Integer() V_0) //a
  IL_0000:  ldloc.0
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""Integer""
  IL_0007:  call       ""Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array""
  IL_000c:  castclass  ""Integer()""
  IL_0011:  stloc.0
  IL_0012:  ret
}
")
        End Sub

        <Fact>
        Public Sub RightShiftAssignmentStatement()
            Const source = "
Class C
    Sub M()
        Dim u As UShort = &H8000
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="u >>= &H1",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        6 (0x6)
  .maxstack  2
  .locals init (UShort V_0) //u
  IL_0000:  ldloc.0
  IL_0001:  ldc.i4.1
  IL_0002:  shr.un
  IL_0003:  conv.u2
  IL_0004:  stloc.0
  IL_0005:  ret
}
")
        End Sub

        <Fact>
        Public Sub SimpleAssignmentStatement()
            Const source = "
Class C
    Private field As Integer = 1
    Property Prop As C
    Public Sub M()
        Dim c1 As New C()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="field = 2",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (C V_0) //c1
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  stfld      ""C.field As Integer""
  IL_0007:  ret
}
")
            testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="c1.Prop.field = 2",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (C V_0) //c1
  IL_0000:  ldloc.0
  IL_0001:  callvirt   ""Function C.get_Prop() As C""
  IL_0006:  ldc.i4.2
  IL_0007:  stfld      ""C.field As Integer""
  IL_000c:  ret
}
")
        End Sub

        <Fact>
        Public Sub StopStatement()
            Const source = "
Class C
    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="Stop",
                errorMessage:=errorMessage)

            'Statements of type 'StopStatement' are not allowed in the Immediate window.
            Assert.Equal(String.Format(ExpressionEvaluator.Resources.InvalidDebuggerStatement, SyntaxKind.StopStatement), errorMessage)
        End Sub

        <Fact>
        Public Sub SubtractAssignmentStatement()
            Const source = "
Class C
    Property D As Decimal
    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="d -= 3.14159D",
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       34 (0x22)
  .maxstack  7
  .locals init (C V_0)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  callvirt   ""Function C.get_D() As Decimal""
  IL_0009:  ldc.i4     0x4cb2f
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  newobj     ""Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)""
  IL_0017:  call       ""Function Decimal.Subtract(Decimal, Decimal) As Decimal""
  IL_001c:  callvirt   ""Sub C.set_D(Decimal)""
  IL_0021:  ret
}
")
        End Sub

        <Fact>
        Public Sub WhileStatement()
            Const source = "
Class C
    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = EvaluateStatement(
                source,
                methodName:="C.M",
                expr:="  While True :: End While",
                errorMessage:=errorMessage)
            Assert.Equal("error BC30035: Syntax error.", errorMessage) ' not the best error, but not worth modifying parsing to improve...
        End Sub

        Private Function EvaluateStatement(source As String, methodName As String, expr As String, <Out> ByRef errorMessage As String, Optional atLineNumber As Integer = -1) As CompilationTestData
            Dim compilationFlags = DkmEvaluationFlags.None
            If expr IsNot Nothing AndAlso expr.StartsWith("?", StringComparison.Ordinal) Then
                ' This mimics Immediate Window behavior...
                compilationFlags = DkmEvaluationFlags.TreatAsExpression
                expr = expr.Substring(1)
            End If
            Dim compilation0 = CreateCompilationWithReferences(
                {Parse(source)},
                {MscorlibRef_v4_0_30316_17626, SystemRef, MsvbRef},
                options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(runtime, methodName, atLineNumber)
            Dim testData = New CompilationTestData()
            Dim resultProperties As ResultProperties = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim result = context.CompileExpression(
                    expr,
                    compilationFlags,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    resultProperties,
                    errorMessage,
                    missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData)
            Assert.Empty(missingAssemblyIdentities)
            Return testData
        End Function

    End Class

End Namespace
