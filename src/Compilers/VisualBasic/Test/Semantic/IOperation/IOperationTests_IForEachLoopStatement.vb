' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_SimpleForLoopsTest()
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
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each s  ... Next')
  Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'arr')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: arr (OperationKind.LocalReferenceExpression, Type: System.String()) (Syntax: 'arr')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each s  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(s)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 's')
                  ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
                  InConversion: null
                  OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_WithList()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim list As New System.Collections.Generic.List(Of String)()
        list.Add("a")
        list.Add("b")
        list.Add("c")
        For Each item As String In list'BIND:"For Each item As String In list"
            System.Console.WriteLine(item)
        Next

    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each it ... Next')
  Collection: ILocalReferenceExpression: list (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List(Of System.String)) (Syntax: 'list')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each it ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... eLine(item)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... eLine(item)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'item')
                  ILocalReferenceExpression: item (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'item')
                  InConversion: null
                  OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_WithBreak()
            Dim source = <![CDATA[
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S
            For Each y As Char In x'BIND:"For Each y As Char In x"
                If y = "B"c Then
                    Exit For
                Else
                    System.Console.WriteLine(y)
                End If
            Next
        Next
    End Sub
End Class

    ]]>.Value

Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each y  ... Next')
  Collection: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'x')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each y  ... Next')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If y = "B"c ... End If')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals, IsChecked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y = "B"c')
            Left: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Char, Constant: B) (Syntax: '"B"c')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If y = "B"c ... End If')
            IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit For')
        IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else ... riteLine(y)')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(y)')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'y')
                        ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
                        InConversion: null
                        OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_WithContinue()
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
End Class

    ]]>.Value

Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next y, x')
  Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'S')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: S (OperationKind.LocalReferenceExpression, Type: System.String()) (Syntax: 'S')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next y, x')
      IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each y  ... Next y, x')
        Collection: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'x')
        Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'For Each y  ... Next y, x')
            IIfStatement (OperationKind.IfStatement) (Syntax: 'If y = "B"c ... End If')
              Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals, IsChecked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y = "B"c')
                  Left: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Char, Constant: B) (Syntax: '"B"c')
              IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If y = "B"c ... End If')
                  IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement) (Syntax: 'Continue For')
              IfFalse: null
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(y)')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'y')
                        ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
                        InConversion: null
                        OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Nested()
            Dim source = <![CDATA[
Class C
    Shared Sub Main()
        Dim c(3)() As Integer
        For Each x As Integer() In c
            ReDim x(3)
            For i As Integer = 0 To 3
                x(i) = i
            Next
            For Each y As Integer In x'BIND:"For Each y As Integer In x"
                System.Console.WriteLine(y)
            Next
        Next
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each y  ... Next')
  Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'x')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'x')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each y  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(y)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'y')
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
                  InConversion: null
                  OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'S')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: S (OperationKind.LocalReferenceExpression, Type: System.String()) (Syntax: 'S')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each y  ... Next')
        Collection: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'x')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each y  ... Next')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(y)')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'y')
                        ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
                        InConversion: null
                        OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Interface()
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
    Implements System.Collections.IEnumerable
    Private Function System_Collections_IEnumerable_GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Dim list As New System.Collections.Generic.List(Of Integer)()
        list.Add(3)
        list.Add(2)
        list.Add(1)
        Return list.GetEnumerator()
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'New Enumerable()')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable) (Syntax: 'New Enumerable()')
          Arguments(0)
          Initializer: null
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(x)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'x')
                  InConversion: null
                  OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_String()
            Dim source = <![CDATA[
Option Infer On
Class Program
    Public Shared Sub Main()
        Const s As String = Nothing
        For Each y In s'BIND:"For Each y In s"
            System.Console.WriteLine(y)
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each y  ... Next')
  Collection: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Constant: null) (Syntax: 's')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each y  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(y)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'y')
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'y')
                  InConversion: null
                  OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_IterateStruct()
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
    Implements IEnumerable
    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return New Integer() {1, 2, 3}.GetEnumerator()
    End Function
End Structure
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'New Enumerable()')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable) (Syntax: 'New Enumerable()')
          Arguments(0)
          Initializer: null
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(x)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'x')
                  InConversion: null
                  OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_QueryExpression()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq

Module Program
    Sub Main(args As String())
        ' Obtain a list of customers.
        Dim customers As List(Of Customer) = GetCustomers()

        ' Return customers that are grouped based on country.
        Dim countries = From cust In customers
                        Order By cust.Country, cust.City
                        Group By CountryName = cust.Country
                        Into CustomersInCountry = Group, Count()
                        Order By CountryName

        ' Output the results.
        For Each country In countries'BIND:"For Each country In countries"
            Debug.WriteLine(country.CountryName & " count=" & country.Count)

            For Each customer In country.CustomersInCountry
                Debug.WriteLine("   " & customer.CompanyName & "  " & customer.City)
            Next
        Next
    End Sub

    Private Function GetCustomers() As List(Of Customer)
        Return New List(Of Customer) From
            {
                New Customer With {.CustomerID = 1, .CompanyName = "C", .City = "H", .Country = "C"},
                New Customer With {.CustomerID = 2, .CompanyName = "M", .City = "R", .Country = "U"},
                New Customer With {.CustomerID = 3, .CompanyName = "F", .City = "V", .Country = "C"}
            }
    End Function
End Module

Class Customer
    Public Property CustomerID As Integer
    Public Property CompanyName As String
    Public Property City As String
    Public Property Country As String
End Class

]]>.Value

Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each co ... Next')
  Collection: ILocalReferenceExpression: countries (OperationKind.LocalReferenceExpression, Type: System.Linq.IOrderedEnumerable(Of <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>)) (Syntax: 'countries')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'For Each co ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Debug.Write ... ntry.Count)')
        Expression: IInvocationExpression (Sub System.Diagnostics.Debug.WriteLine(message As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Debug.Write ... ntry.Count)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument) (Syntax: 'country.Cou ... untry.Count')
                  IBinaryOperatorExpression (BinaryOperatorKind.Invalid, IsChecked) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: 'country.Cou ... untry.Count')
                    Left: IBinaryOperatorExpression (BinaryOperatorKind.Invalid, IsChecked) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: 'country.Cou ... & " count="')
                        Left: IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>.CountryName As System.String (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'country.CountryName')
                            Instance Receiver: ILocalReferenceExpression: country (OperationKind.LocalReferenceExpression, Type: <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>) (Syntax: 'country')
                        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: " count=") (Syntax: '" count="')
                    Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'country.Count')
                        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>.Count As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'country.Count')
                            Instance Receiver: ILocalReferenceExpression: country (OperationKind.LocalReferenceExpression, Type: <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>) (Syntax: 'country')
                  InConversion: null
                  OutConversion: null
      IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each cu ... Next')
        Collection: IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>.CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer) (OperationKind.PropertyReferenceExpression, Type: System.Collections.Generic.IEnumerable(Of Customer)) (Syntax: 'country.Cus ... rsInCountry')
            Instance Receiver: ILocalReferenceExpression: country (OperationKind.LocalReferenceExpression, Type: <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>) (Syntax: 'country')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each cu ... Next')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Debug.Write ... tomer.City)')
              Expression: IInvocationExpression (Sub System.Diagnostics.Debug.WriteLine(message As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Debug.Write ... tomer.City)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument) (Syntax: '"   " & cus ... stomer.City')
                        IBinaryOperatorExpression (BinaryOperatorKind.Invalid, IsChecked) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: '"   " & cus ... stomer.City')
                          Left: IBinaryOperatorExpression (BinaryOperatorKind.Invalid, IsChecked) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: '"   " & cus ... Name & "  "')
                              Left: IBinaryOperatorExpression (BinaryOperatorKind.Invalid, IsChecked) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: '"   " & cus ... CompanyName')
                                  Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "   ") (Syntax: '"   "')
                                  Right: IPropertyReferenceExpression: Property Customer.CompanyName As System.String (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'customer.CompanyName')
                                      Instance Receiver: ILocalReferenceExpression: customer (OperationKind.LocalReferenceExpression, Type: Customer) (Syntax: 'customer')
                              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "  ") (Syntax: '"  "')
                          Right: IPropertyReferenceExpression: Property Customer.City As System.String (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'customer.City')
                              Instance Receiver: ILocalReferenceExpression: customer (OperationKind.LocalReferenceExpression, Type: Customer) (Syntax: 'customer')
                        InConversion: null
                        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub



        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Multidimensional()
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
End Module

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each [C ... Next')
  Collection: ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32(,)) (Syntax: 'k')
  Body: IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'For Each [C ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... (Integer)))')
        Expression: IInvocationExpression (Sub System.Console.Write(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... (Integer)))')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'VerifyStati ... e(Integer))')
                  IInvocationExpression (Function Program.VerifyStaticType(Of System.Int32)(x As System.Int32, y As System.Type) As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'VerifyStati ... e(Integer))')
                    Instance Receiver: null
                    Arguments(2):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '[Custom]')
                          ILocalReferenceExpression: Custom (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '[Custom]')
                          InConversion: null
                          OutConversion: null
                        IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'GetType(Integer)')
                          IOperation:  (OperationKind.None) (Syntax: 'GetType(Integer)')
                          InConversion: null
                          OutConversion: null
                  InConversion: null
                  OutConversion: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... e(Object)))')
        Expression: IInvocationExpression (Sub System.Console.Write(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... e(Object)))')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'VerifyStati ... pe(Object))')
                  IInvocationExpression (Function Program.VerifyStaticType(Of System.Int32)(x As System.Int32, y As System.Type) As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'VerifyStati ... pe(Object))')
                    Instance Receiver: null
                    Arguments(2):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '[Custom]')
                          ILocalReferenceExpression: Custom (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '[Custom]')
                          InConversion: null
                          OutConversion: null
                        IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'GetType(Object)')
                          IOperation:  (OperationKind.None) (Syntax: 'GetType(Object)')
                          InConversion: null
                          OutConversion: null
                  InConversion: null
                  OutConversion: null
      IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit For')
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'o')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(x)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(x)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'x')
                  InConversion: null
                  OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Pattern()
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
        Return System.Threading.Interlocked.Increment(x) & lt; 4
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each x  ... Next')
  Collection: IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable) (Syntax: 'New Enumerable()')
      Arguments(0)
      Initializer: null
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(x)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                  InConversion: null
                  OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_lamda()
            Dim source = <![CDATA[
Option Strict On
Option Infer On

Imports System

Class C1
    Private element_lambda_field As Integer

    Public Shared Sub Main()
        Dim c1 As New C1()
        c1.DoStuff()
    End Sub

    Public Sub DoStuff()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        Dim myDelegate As Action = Sub()
                                       Dim element_lambda_local As Integer
                                       For Each element_lambda_local In arr'BIND:"For Each element_lambda_local In arr"
                                           Console.WriteLine(element_lambda_local)
                                       Next element_lambda_local


                                   End Sub

        myDelegate.Invoke()
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each el ... ambda_local')
  Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'arr')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: arr (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'arr')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each el ... ambda_local')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... mbda_local)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... mbda_local)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'element_lambda_local')
                  ILocalReferenceExpression: element_lambda_local (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'element_lambda_local')
                  InConversion: null
                  OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_InvalidConversion()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Shared Sub Main()
        For Each element As Integer In "Hello World."'BIND:"For Each element As Integer In "Hello World.""
            Console.WriteLine(element)
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'For Each el ... Next')
  Collection: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Hello World.", IsInvalid) (Syntax: '"Hello World."')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'For Each el ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne(element)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ne(element)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'element')
                  ILocalReferenceExpression: element (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'element')
                  InConversion: null
                  OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Throw()
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
End Class
    ]]>.Value

Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each s  ... Next')
  Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'arr')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: arr (OperationKind.LocalReferenceExpression, Type: System.String()) (Syntax: 'arr')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'For Each s  ... Next')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If (s = "on ... End If')
        Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(s = "one")')
            Operand: IBinaryOperatorExpression (BinaryOperatorKind.Equals, IsChecked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 's = "one"')
                Left: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
                Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "one") (Syntax: '"one"')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (s = "on ... End If')
            IThrowStatement (OperationKind.ThrowStatement) (Syntax: 'Throw New Exception')
              ThrownObject: IObjectCreationExpression (Constructor: Sub System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'New Exception')
                  Arguments(0)
                  Initializer: null
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(s)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 's')
                  ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
                  InConversion: null
                  OutConversion: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Class
    ]]>.Value

Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'For Each o  ... Next')
  Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'c')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: System.Object()) (Syntax: 'c')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For Each o  ... Next')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If o IsNot  ... Return True')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'o IsNot Nothing')
            Left: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null) (Syntax: 'Nothing')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If o IsNot  ... Return True')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return True')
              ReturnedValue: ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'True')
        IfFalse: null
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

    End Class
End Namespace
