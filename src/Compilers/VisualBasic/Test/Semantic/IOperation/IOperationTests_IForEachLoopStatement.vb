' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Array()
            Dim source = <![CDATA[
Imports System
Class C
    Shared Sub Main()
        Dim arr As String() = New String(1) {}
        arr(0) = "one"
        arr(1) = "two"
        For Each s As String In arr'BIND:"For Each s As String In arr"
            Console.WriteLine(s)
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: s As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each s  ... Next')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'arr')
      ILocalReferenceExpression: arr (OperationKind.LocalReferenceExpression, Type: System.String()) (Syntax: 'arr')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each s  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(s)')
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 's')
              ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_List()
            Dim source = <![CDATA[
Option Strict Off
Imports System
Class C
    Shared Sub Main()
        Dim o As Object = {1, 2, 3}
        For Each x In o'BIND:"For Each x In o"
            Console.WriteLine(x)
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Object) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'o')
      ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(x)')
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(x)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Dictionary()
            Dim source = <![CDATA[
Imports System.Collections.Generic

Class Program
    Shared _h As New Dictionary(Of Integer, Integer)()

    Public Shared Sub Main()
        _h.Add(5, 4)
        _h.Add(4, 3)
        _h.Add(2, 1)
        For Each pair As KeyValuePair(Of Integer, Integer) In _h'BIND:"For Each pair As KeyValuePair(Of Integer, Integer) In _h"
            System.Console.WriteLine("{0},{1}", pair.Key, pair.Value)
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: pair As System.Collections.Generic.KeyValuePair(Of System.Int32, System.Int32)) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each pa ... Next')
  Collection: IFieldReferenceExpression: Program._h As System.Collections.Generic.Dictionary(Of System.Int32, System.Int32) (Static) (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)) (Syntax: '_h')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each pa ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... pair.Value)')
        IInvocationExpression (static Sub System.Console.WriteLine(format As System.String, arg0 As System.Object, arg1 As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... pair.Value)')
          Arguments(3): IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument) (Syntax: '"{0},{1}"')
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "{0},{1}") (Syntax: '"{0},{1}"')
            IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument) (Syntax: 'pair.Key')
              IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'pair.Key')
                IIndexedPropertyReferenceExpression: ReadOnly Property System.Collections.Generic.KeyValuePair(Of System.Int32, System.Int32).Key As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'pair.Key')
                  Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair(Of System.Int32, System.Int32)) (Syntax: 'pair')
            IArgument (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument) (Syntax: 'pair.Value')
              IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'pair.Value')
                IIndexedPropertyReferenceExpression: ReadOnly Property System.Collections.Generic.KeyValuePair(Of System.Int32, System.Int32).Value As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'pair.Value')
                  Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair(Of System.Int32, System.Int32)) (Syntax: 'pair')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Break()
            Dim source = <![CDATA[
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S'BIND:"For Each x As String In S"
            For Each y As Char In x
                If y = "B"c Then
                    Exit For
                Else
                    System.Console.WriteLine(y)
                End If
            Next
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'S')
      ILocalReferenceExpression: S (OperationKind.LocalReferenceExpression, Type: System.String()) (Syntax: 'S')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IForEachLoopStatement (Iteration variable: y As System.Char) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each y  ... Next')
        Collection: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'x')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each y  ... Next')
            IIfStatement (OperationKind.IfStatement) (Syntax: 'If y = "B"c ... End If')
              Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y = "B"c')
                  Left: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Char, Constant: B) (Syntax: '"B"c')
              IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If y = "B"c ... End If')
                  IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit For')
              IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else ... riteLine(y)')
                  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(y)')
                    IInvocationExpression (static Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
                      Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'y')
                          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Continue()
            Dim source = <![CDATA[
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S'BIND:"For Each x As String In S"
            For Each y As Char In x
                If y = "B"c Then
                    Continue For
                End If
                System.Console.WriteLine(y)
        Next y, x
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next y, x')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'S')
      ILocalReferenceExpression: S (OperationKind.LocalReferenceExpression, Type: System.String()) (Syntax: 'S')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next y, x')
      IForEachLoopStatement (Iteration variable: y As System.Char) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each y  ... Next y, x')
        Collection: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'x')
        Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'For Each y  ... Next y, x')
            IIfStatement (OperationKind.IfStatement) (Syntax: 'If y = "B"c ... End If')
              Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y = "B"c')
                  Left: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Char, Constant: B) (Syntax: '"B"c')
              IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If y = "B"c ... End If')
                  IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement) (Syntax: 'Continue For')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(y)')
              IInvocationExpression (static Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
                Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'y')
                    ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Nested()
            Dim source = <![CDATA[
Class C
    Shared Sub Main()
        Dim c(3)() As Integer
        For Each x As Integer() In c'BIND:"For Each x As Integer() In c"
            ReDim x(3)
            For i As Integer = 0 To 3
                x(i) = i
            Next
            For Each y As Integer In x
                System.Console.WriteLine(y)
            Next
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Int32()) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'c')
      ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Int32()()) (Syntax: 'c')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For i As In ... Next')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '3')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
            Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
        Before: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '0')
            IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32) (Syntax: '0')
              Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
              Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For i As In ... Next')
            ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For i As In ... Next')
              Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
              Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x(i) = i')
              IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32) (Syntax: 'x(i) = i')
                Left: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'x(i)')
                    Array reference: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'x')
                    Indices(1): ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      IForEachLoopStatement (Iteration variable: y As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each y  ... Next')
        Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'x')
            ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'x')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each y  ... Next')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(y)')
              IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
                Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'y')
                    ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Nested1()
            Dim source = <![CDATA[
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S'BIND:"For Each x As String In S"
            For Each y As Char In x
                System.Console.WriteLine(y)
            Next
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'S')
      ILocalReferenceExpression: S (OperationKind.LocalReferenceExpression, Type: System.String()) (Syntax: 'S')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IForEachLoopStatement (Iteration variable: y As System.Char) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each y  ... Next')
        Collection: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'x')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each y  ... Next')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(y)')
              IInvocationExpression (static Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
                Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'y')
                    ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Enumerator()
            Dim source = <![CDATA[
Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()'BIND:"For Each x In New Enumerable()"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Implements System.Collections.IEnumerable
    Private Function System_Collections_IEnumerable_GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Dim list As New System.Collections.Generic.List(Of Integer)()
        list.Add(3)
        list.Add(2)
        list.Add(1)
        Return list.GetEnumerator()
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Object) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'New Enumerable()')
      IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable) (Syntax: 'New Enumerable()')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(x)')
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Struct()
            Dim source = <![CDATA[
Option Infer On
Imports System.Collections
Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()'BIND:"For Each x In New Enumerable()"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Structure Enumerable
    Public Function GetEnumerator() As IEnumerator
        Return New Integer() {1, 2, 3}.GetEnumerator()
    End Function
End Structure]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Object) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable) (Syntax: 'New Enumerable()')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(x)')
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_ConstantNull()
            Dim source = <![CDATA[
Class P
    Public Shared Sub Main()
        Const s As String = Nothing
        For Each y In TryCast(s, String)'BIND:"For Each y In TryCast(s, String)"
            System.Console.WriteLine(y)
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: y As System.Char) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each y  ... Next')
  Collection: IConversionExpression (ConversionKind.TryCast, Explicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'TryCast(s, String)')
      ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Constant: null) (Syntax: 's')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each y  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(y)')
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'y')
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_MultiDimensioanl()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()

        Dim k(,) = {{1}, {1}}
        For Each [Custom] In k'BIND:"For Each [Custom] In k"
            Console.Write(VerifyStaticType([Custom], GetType(Integer)))
            Console.Write(VerifyStaticType([Custom], GetType(Object)))
            Exit For
        Next
    End Sub

    Function VerifyStaticType(Of T)(ByVal x As T, ByVal y As System.Type) As Boolean
        Return GetType(T) Is y
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: Custom As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each [C ... Next')
  Collection: ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32(,)) (Syntax: 'k')
  Body: IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'For Each [C ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... (Integer)))')
        IInvocationExpression (static Sub System.Console.Write(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... (Integer)))')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'VerifyStati ... e(Integer))')
              IInvocationExpression (static Function Program.VerifyStaticType(Of System.Int32)(x As System.Int32, y As System.Type) As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'VerifyStati ... e(Integer))')
                Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '[Custom]')
                    ILocalReferenceExpression: Custom (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '[Custom]')
                  IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'GetType(Integer)')
                    IOperation:  (OperationKind.None) (Syntax: 'GetType(Integer)')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... e(Object)))')
        IInvocationExpression (static Sub System.Console.Write(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... e(Object)))')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'VerifyStati ... pe(Object))')
              IInvocationExpression (static Function Program.VerifyStaticType(Of System.Int32)(x As System.Int32, y As System.Type) As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'VerifyStati ... pe(Object))')
                Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '[Custom]')
                    ILocalReferenceExpression: Custom (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '[Custom]')
                  IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'GetType(Object)')
                    IOperation:  (OperationKind.None) (Syntax: 'GetType(Object)')
      IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit For')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_LateBinding()
            Dim source = <![CDATA[
Option Strict Off
Imports System
Class C
    Shared Sub Main()
        Dim o As Object = {1, 2, 3}
        For Each x In o'BIND:"For Each x In o"
            Console.WriteLine(x)
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Object) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'o')
      ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(x)')
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(x)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_WithPattern()
            Dim source = <![CDATA[
Option Infer On
Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()'BIND:"For Each x In New Enumerable()"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class

Class Enumerator
    Private x As Integer = 0
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) + 4
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable) (Syntax: 'New Enumerable()')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(x)')
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_WithLamda()
            Dim source = <![CDATA[
Option Infer On
Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()'BIND:"For Each x In New Enumerable()"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class

Class Enumerator
    Private x As Integer = 0
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) + 4
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable) (Syntax: 'New Enumerable()')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(x)')
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_InvalidConverstion()
            Dim source = <![CDATA[
Imports System
Class C1
    Public Shared Sub Main()
        For Each element As Integer In "Hello World."'BIND:"For Each element As Integer In "Hello World.""
            Console.WriteLine(element)
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: element As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each el ... Next')
  Collection: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Hello World.") (Syntax: '"Hello World."')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each el ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne(element)')
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ne(element)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'element')
              ILocalReferenceExpression: element (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'element')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32006: 'Char' values cannot be converted to 'Integer'. Use 'Microsoft.VisualBasic.AscW' to interpret a character as a Unicode value or 'Microsoft.VisualBasic.Val' to interpret it as a digit.
        For Each element As Integer In "Hello World."'BIND:"For Each element As Integer In "Hello World.""
                                       ~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_WithThrow()
            Dim source = <![CDATA[
Imports System
Class C
    Shared Sub Main()
        Dim arr As String() = New String(1) {}
        arr(0) = "one"
        arr(1) = "two"
        For Each s As String In arr'BIND:"For Each s As String In arr"
            If (s = "one") Then
                Throw New Exception
            End If
            Console.WriteLine(s)
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: s As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each s  ... Next')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'arr')
      ILocalReferenceExpression: arr (OperationKind.LocalReferenceExpression, Type: System.String()) (Syntax: 'arr')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'For Each s  ... Next')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If (s = "on ... End If')
        Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(s = "one")')
            IBinaryOperatorExpression (BinaryOperationKind.StringEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 's = "one"')
              Left: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "one") (Syntax: '"one"')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (s = "on ... End If')
            IThrowStatement (OperationKind.ThrowStatement) (Syntax: 'Throw New Exception')
              IObjectCreationExpression (Constructor: Sub System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'New Exception')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(s)')
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 's')
              ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_FuncCall()
            Dim source = <![CDATA[
Class C1
    Public Function foo() As Integer()
        Return New Integer() {1, 2, 3}
    End Function
End Class

Module M
    Public Sub M()
        Dim used1, used2 As Integer
        For Each used1 In New C1().foo()'BIND:"For Each used1 In New C1().foo()"
            used2 = 23
        Next used1
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: used1 As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each us ... Next used1')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'New C1().foo()')
      IInvocationExpression ( Function C1.foo() As System.Int32()) (OperationKind.InvocationExpression, Type: System.Int32()) (Syntax: 'New C1().foo()')
        Instance Receiver: IObjectCreationExpression (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'New C1()')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each us ... Next used1')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'used2 = 23')
        IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32) (Syntax: 'used2 = 23')
          Left: ILocalReferenceExpression: used2 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'used2')
          Right: ILiteralExpression (Text: 23) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 23) (Syntax: '23')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_WithReturn()
            Dim source = <![CDATA[
Class C
    Private F As Object
    Shared Function M(c As Object()) As Boolean
        For Each o In c'BIND:"For Each o In c"
            If o IsNot Nothing Then Return True
        Next
        Return False
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: o As System.Object) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each o  ... Next')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'c')
      IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: System.Object()) (Syntax: 'c')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each o  ... Next')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If o IsNot  ... Return True')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.ObjectNotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'o IsNot Nothing')
            Left: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
            Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object, Constant: null) (Syntax: 'Nothing')
                ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If o IsNot  ... Return True')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return True')
              ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'True')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_EmptyBody()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main()
        Dim pets As String() = {"dog", "cat", "bird"}

        For Each value As String In pets'BIND:"For Each value As String In pets"
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: value As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each va ... Next')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'pets')
      ILocalReferenceExpression: pets (OperationKind.LocalReferenceExpression, Type: System.String()) (Syntax: 'pets')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For Each va ... Next')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_ImplicitlyTypedArray()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main()
        Dim pets As String() = {"dog", "cat", "bird"}

        For Each value As String In pets'BIND:"For Each value As String In pets"
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: value As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each va ... Next')
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'pets')
      ILocalReferenceExpression: pets (OperationKind.LocalReferenceExpression, Type: System.String()) (Syntax: 'pets')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For Each va ... Next')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

    End Class
End Namespace

