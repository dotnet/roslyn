' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class UsingStatementTest
        Inherits BasicTestBase

#Region "Semantic API"
        <Fact()>
        Public Sub MultipleResourceWithDifferentType_SemanticAPI()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MultipleResourceWithDifferentType">
    <file name="a.vb">
Option Infer On
Imports System
Class C1
    Private Shared Sub goo(ByRef x1 As MyManagedClass)
        Using x2, x3 As New MyManagedClass(), x4, x5 As New MyManagedClass1()
            x1 = New MyManagedClass()
        End Using
    End Sub
End Class
Structure MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Structure
Structure MyManagedClass1
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Structure
    </file>
</compilation>)
            Dim symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "x2", "x3", "x4", "x5")
            For Each x In symbols
                Dim localSymbol = DirectCast(x, LocalSymbol)
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1)

            Next

            VerifySymbolInfoForUsingStatements(compilation, 1, 1, "MyManagedClass", DirectCast(symbols(0), LocalSymbol).Type, DirectCast(symbols(1), LocalSymbol).Type)
            VerifySymbolInfoForUsingStatements(compilation, 1, 2, "MyManagedClass1", DirectCast(symbols(2), LocalSymbol).Type, DirectCast(symbols(3), LocalSymbol).Type)
        End Sub

        <Fact()>
        Public Sub InitResourceWithFunctionCall()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Class C1
    Private Shared Sub goo()
        Using x1 = NewClass1(), x2 = NewClass2
        End Using
    End Sub
    Shared Function NewClass1() As MyManagedClass1
        Return New MyManagedClass1()
    End Function
    Shared Function NewClass2() As MyManagedClass2
        Return New MyManagedClass2()
    End Function
End Class
Structure MyManagedClass1
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Structure
Structure MyManagedClass2
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Structure
    </file>
</compilation>)
            Dim symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "x1", "x2")
            For Each x In symbols
                Dim localSymbol = DirectCast(x, LocalSymbol)
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1)
            Next

            VerifySymbolInfoForUsingStatements(compilation, 1, 1, "NewClass1()", DirectCast(symbols(0), LocalSymbol).Type)
            VerifySymbolInfoForUsingStatements(compilation, 1, 2, "NewClass2", DirectCast(symbols(1), LocalSymbol).Type)
        End Sub

        <Fact()>
        Public Sub InitResource()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="InitResource">
    <file name="a.vb">
Option Infer On
Imports System
Class C1
    Dim x As MyManagedClass1 = New MyManagedClass1()
    Dim y As MyManagedClass2 = New MyManagedClass2()
    Private Sub goo()
        Using x1 = x, x2 = y
        End Using
    End Sub
End Class
Structure MyManagedClass1
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Structure
Structure MyManagedClass2
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Structure
    </file>
</compilation>)
            Dim symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "x1", "x2")
            For Each x In symbols
                Dim localSymbol = DirectCast(x, LocalSymbol)
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1)
            Next

            VerifySymbolInfoForUsingStatements(compilation, 1, 1, "x", DirectCast(symbols(0), LocalSymbol).Type)
            VerifySymbolInfoForUsingStatements(compilation, 1, 2, "y", DirectCast(symbols(1), LocalSymbol).Type)
        End Sub

        <Fact()>
        Public Sub NoVariableDeclaredInUSING()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NoVariableDeclaredInUSING">
    <file name="a.vb">
Option Infer On
Option Strict Off
Imports System
Class C1
    Dim f As MyManagedClass
    Private Sub goo(p1 As MyManagedClass)
        Dim x1 As MyManagedClass
        Using p1
            Using x1
                Using f
                    Using Nothing

                    End Using
                End Using
            End Using
        End Using
    End Sub
End Class
Structure MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Structure
    </file>
</compilation>)
            VerifyDeclaredSymbolForUsingStatements(compilation, 1)
            VerifyDeclaredSymbolForUsingStatements(compilation, 2)
            VerifyDeclaredSymbolForUsingStatements(compilation, 3)
            VerifyDeclaredSymbolForUsingStatements(compilation, 4)

        End Sub

        <Fact()>
        Public Sub NestedUsing()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NestedUsing">
    <file name="a.vb">
Option Infer On
Imports System
Class C1
    Dim x As C1
    Private Sub goo()
        Using NewClass1()
            Using x1 = x.NewClass1(), x2 = x.NewClass2()
            End Using
        End Using
    End Sub
    Function NewClass1() As MyManagedClass1
        Return New MyManagedClass1()
    End Function
    Function NewClass2() As MyManagedClass2
        Return New MyManagedClass2()
    End Function
End Class
Structure MyManagedClass1
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Structure
Structure MyManagedClass2
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
    End Sub
End Structure
    </file>
</compilation>)
            VerifyDeclaredSymbolForUsingStatements(compilation, 1)
            Dim symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 2, "x1", "x2")
            For Each x In symbols
                Dim localSymbol = DirectCast(x, LocalSymbol)
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 2)
            Next

            VerifySymbolInfoForUsingStatements(compilation, 2, 1, "x.NewClass1()", DirectCast(symbols(0), LocalSymbol).Type)
            VerifySymbolInfoForUsingStatements(compilation, 2, 2, "x.NewClass2", DirectCast(symbols(1), LocalSymbol).Type)
        End Sub
#End Region

        <Fact(), WorkItem(545110, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545110")>
        Public Sub UsingWithNothingOptionStrictOff()
            Dim verifier = CompileAndVerify(
    <compilation name="UsingWithNothing">
        <file name="a.vb">
Imports System

Module M

    Sub Main()
        Using Nothing
            Console.Write("Hi ")
        End Using
        Using If(Nothing, Nothing)
            Console.Write("there")
        End Using
    End Sub
End Module
    </file>
    </compilation>, expectedOutput:="Hi there", options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off)).
                VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (Object V_0,
                Object V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  .try
{
  IL_0002:  ldstr      "Hi "
  IL_0007:  call       "Sub System.Console.Write(String)"
  IL_000c:  leave.s    IL_001d
}
  finally
{
  IL_000e:  ldloc.0
  IL_000f:  brfalse.s  IL_001c
  IL_0011:  ldloc.0
  IL_0012:  castclass  "System.IDisposable"
  IL_0017:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_001c:  endfinally
}
  IL_001d:  ldnull
  IL_001e:  stloc.1
  .try
{
  IL_001f:  ldstr      "there"
  IL_0024:  call       "Sub System.Console.Write(String)"
  IL_0029:  leave.s    IL_003a
}
  finally
{
  IL_002b:  ldloc.1
  IL_002c:  brfalse.s  IL_0039
  IL_002e:  ldloc.1
  IL_002f:  castclass  "System.IDisposable"
  IL_0034:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0039:  endfinally
}
  IL_003a:  ret
}
    ]]>)

            Dim compilation = verifier.Compilation

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Object' to 'IDisposable'.
        Using Nothing
              ~~~~~~~
BC42016: Implicit conversion from 'Object' to 'IDisposable'.
        Using If(Nothing, Nothing)
              ~~~~~~~~~~~~~~~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36010: 'Using' operand of type 'Object' must implement 'System.IDisposable'.
        Using Nothing
              ~~~~~~~
BC36010: 'Using' operand of type 'Object' must implement 'System.IDisposable'.
        Using If(Nothing, Nothing)
              ~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

#Region "Help Method"
        Private Function GetUsingStatements(compilation As VisualBasicCompilation, Optional index As Integer = 1) As UsingStatementSyntax
            Dim tree = compilation.SyntaxTrees.[Single]()
            Dim model = compilation.GetSemanticModel(tree)
            Dim usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of UsingStatementSyntax)().ToList()
            Return usingStatements(index - 1)
        End Function

        Private Function VerifyDeclaredSymbolForUsingStatements(compilation As VisualBasicCompilation, index As Integer, ParamArray variables As String()) As List(Of ISymbol)
            Dim tree = compilation.SyntaxTrees.[Single]()
            Dim model = compilation.GetSemanticModel(tree)

            Dim usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of UsingStatementSyntax)().ToList()
            Dim i = 0
            Dim symbols = New List(Of ISymbol)()
            For Each x In usingStatements(index - 1).Variables
                For Each y In x.Names
                    Dim symbol = model.GetDeclaredSymbol(y)
                    Assert.Equal(SymbolKind.Local, symbol.Kind)
                    Assert.Equal(variables(i).ToString(), symbol.ToDisplayString())
                    i = i + 1
                    symbols.Add(symbol)
                Next
            Next
            Return symbols
        End Function

        Private Sub VerifySymbolInfoForUsingStatements(compilation As VisualBasicCompilation, Usingindex As Integer, Declaratorindex As Integer, expressionStr As String, ParamArray symbols As Symbol())
            Dim tree = compilation.SyntaxTrees.[Single]()
            Dim model = compilation.GetSemanticModel(tree)
            Dim usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of UsingStatementSyntax)().ToList()
            Dim i = 0
            Dim declarator = usingStatements(Usingindex - 1).Variables(Declaratorindex - 1)
            Dim expression = declarator.DescendantNodes().OfType(Of ExpressionSyntax)().Where(Function(item) item.ToString() = expressionStr).First
            Dim type = model.GetSymbolInfo(expression)
            If (type.Symbol.Kind = SymbolKind.Method) Then
                If (DirectCast(type.Symbol, MethodSymbol).MethodKind = MethodKind.Constructor) Then

                    For Each Symbol In symbols
                        Assert.Equal(symbols(i), type.Symbol.ContainingSymbol)
                        i = i + 1
                    Next
                Else
                    For Each Symbol In symbols
                        Assert.Equal(symbols(i), DirectCast(type.Symbol, MethodSymbol).ReturnType)
                        i = i + 1
                    Next
                End If
            ElseIf (type.Symbol.Kind = SymbolKind.Field) Then
                For Each Symbol In symbols
                    Assert.Equal(symbols(i), DirectCast(type.Symbol, FieldSymbol).Type)
                    i = i + 1
                Next
            Else
                For Each Symbol In symbols
                    Assert.Equal(symbols(i), type.Symbol)
                    i = i + 1
                Next
            End If

        End Sub

        Private Sub VerifyLookUpSymbolForUsingStatements(compilation As VisualBasicCompilation, symbol As Symbol, Optional index As Integer = 1)
            Dim tree = compilation.SyntaxTrees.[Single]()
            Dim model = compilation.GetSemanticModel(tree)
            Dim usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of UsingStatementSyntax)().ToList()

            Dim ActualSymbol = model.LookupSymbols(usingStatements(index - 1).SpanStart, name:=symbol.Name).[Single]()
            Assert.Equal(SymbolKind.Local, ActualSymbol.Kind)
            Assert.Equal(symbol, ActualSymbol)
        End Sub

#End Region

    End Class
End Namespace
