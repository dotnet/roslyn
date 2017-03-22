' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidInvocationExpression_BadReceiver()
            Dim source = <![CDATA[
Imports System

Class Program
    Private Shared Sub Main(args As String())
        Console.WriteLine2()'BIND:"Console.WriteLine2()"
    End Sub
End Class
    ]]>.Value

            ' This might change with https://github.com/dotnet/roslyn/issues/18069
            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
    IOperation:  (OperationKind.None)
]]>.Value

            VerifyOperationTreeForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidInvocationExpression_OverloadResolutionFailureBadArgument()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        F(String.Empty)'BIND:"F(String.Empty)"
    End Sub

    Private Sub F(x As Integer)
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.F(x As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid)
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program)
  IArgument (Matching Parameter: x) (OperationKind.Argument)
    IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32)
      IFieldReferenceExpression: System.String.Empty As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String)
]]>.Value

            VerifyOperationTreeForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidInvocationExpression_OverloadResolutionFailureExtraArgument()
            Dim source = <![CDATA[

Class Program
    Private Shared Sub Main(args As String())
        F(String.Empty)'BIND:"F(String.Empty)"
    End Sub

    Private Sub F()
    End Sub
End Class
    ]]>.Value

            ' This might change with https://github.com/dotnet/roslyn/issues/18069
            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid)
  IOperation:  (OperationKind.None)
  IFieldReferenceExpression: System.String.Empty As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String)
]]>.Value

            VerifyOperationTreeForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidFieldReferenceExpression()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim x = New Program()
        Dim y = x.MissingField'BIND:"x.MissingField"
    End Sub

    Private Sub F()
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
]]>.Value

            VerifyOperationTreeForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidConversionExpression_ImplicitCast()
            Dim source = <![CDATA[
Class Program
    Private i1 As Integer
    Private Shared Sub Main(args As String())
        Dim x = New Program()
        Dim y As Program = x.i1'BIND:"x.i1"
    End Sub

    Private Sub F()
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program, IsInvalid)
  IFieldReferenceExpression: Program.i1 As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32)
    Instance Receiver: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
]]>.Value

            VerifyOperationTreeForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidConversionExpression_ExplicitCast()
            Dim source = <![CDATA[
Class Program
    Private i1 As Integer
    Private Shared Sub Main(args As String())
        Dim x = New Program()
        Dim y As Program = DirectCast(x.i1, Program)'BIND:"DirectCast(x.i1, Program)"
    End Sub

    Private Sub F()
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (ConversionKind.Cast, Explicit) (OperationKind.ConversionExpression, Type: Program, IsInvalid)
  IFieldReferenceExpression: Program.i1 As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32)
    Instance Receiver: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
]]>.Value

            VerifyOperationTreeForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidUnaryExpression()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim x = New Program()
        Console.Write(+x)'BIND:"+x"
    End Sub

    Private Sub F()
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: ?, IsInvalid)
  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidBinaryExpression()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim x = New Program()
        Console.Write(x + (y * args.Length))'BIND:"x + (y * args.Length)"
    End Sub

    Private Sub F()
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid)
  Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
  Right: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: ?, IsInvalid)
      IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid)
        Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
        Right: IIndexedPropertyReferenceExpression: ReadOnly Property System.Array.Length As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32)
            Instance Receiver: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String())
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/18073"), WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidLambdaBinding_UnboundLambda()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim x = Function() F()'BIND:"Function() F()"
    End Sub

    Private Shared Sub F()
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
]]>.Value

            VerifyOperationTreeForTest(Of SingleLineLambdaExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidFieldInitializer()
            Dim source = <![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Class Program
    Private x As Integer = Program'BIND:"= Program"
    Private Shared Sub Main(args As String())
        Dim x = New Program() With {
            .x = Program
        }
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializer (Field: Program.x As System.Int32) (OperationKind.FieldInitializerAtDeclaration, IsInvalid)
  IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
    IOperation:  (OperationKind.None, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of EqualsValueSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/18074"), WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidArrayInitializer()
            Dim source = <![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Class Program
    Private Shared Sub Main(args As String())
        Dim x = New Integer(1, 1) {{{1, 1}}, {2, 2}}'BIND:"{{1, 1}}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid)
  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidArrayCreation()
            Dim source = <![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Class Program
    Private Shared Sub Main(args As String())
        Dim x = New X(Program - 1) {{1}}'BIND:"New X(Program - 1) {{1}}"
    End Sub
End Class
    ]]>.Value

            ' The operation tree might get affected with https://github.com/dotnet/roslyn/issues/18074
            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (Dimension sizes: 1, Element Type: X) (OperationKind.ArrayCreationExpression, Type: X(), IsInvalid)
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsInvalid)
    Left: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
        IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid)
          Left: IOperation:  (OperationKind.None, IsInvalid)
          Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid)
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidParameterDefaultValueInitializer()
            Dim source = <![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Class Program
    Private Shared Function M() As Integer
        Return 0
    End Function
    Private Sub F(Optional p As Integer = M())'BIND:"= M()"
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParameterInitializer (Parameter: [p As System.Int32]) (OperationKind.ParameterInitializerAtDeclaration, IsInvalid)
  IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32, IsInvalid)
    IInvocationExpression (static Function Program.M() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of EqualsValueSyntax)(source, expectedOperationTree)
        End Sub
    End Class
End Namespace
