' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact()>
        Public Sub ExplicitSimpleArgument()
            Dim source = <![CDATA[
Module P
    Sub M1()
        M2(1, "")'BIND:"M2(1, "")"
    End Sub

    Sub M2(x As Integer, y As String)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression (static Sub P.M2(x As System.Int32, y As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.Positional Matching Parameter: x) (OperationKind.Argument)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArgument (ArgumentKind.Positional Matching Parameter: y) (OperationKind.Argument)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
]]>.Value

            VerifyOperationTreeForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact()>
        Public Sub ExplicitSimpleArgumentByName()
            Dim source = <![CDATA[   
Class P
    Sub M1()
        M2(y:=1, x:=2)'BIND:"M2(y:=1, x:=2)"
    End Sub

    Sub M2(x As Integer, y As Integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2(x As System.Int32, y As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P)
  IArgument (ArgumentKind.Positional Matching Parameter: x) (OperationKind.Argument)
    ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  IArgument (ArgumentKind.Positional Matching Parameter: y) (OperationKind.Argument)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact()>
        Public Sub DefaultArgument()
            Dim source = <![CDATA[  
Class P
    Sub M1()
        M2(1)'BIND:"M2(1)"
    End Sub

    Sub M2(x As Integer, Optional y As Integer = 10)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2(x As System.Int32, [y As System.Int32 = 10])) (OperationKind.InvocationExpression, Type: System.Void)
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P)
  IArgument (ArgumentKind.Positional Matching Parameter: x) (OperationKind.Argument)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArgument (ArgumentKind.Positional Matching Parameter: y) (OperationKind.Argument)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
]]>.Value

            VerifyOperationTreeForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact()>
        Public Sub ParamArrayArgument()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(1, 2, 3)'BIND:"M2(1, 2, 3)"
    End Sub

    Sub M2(a As Integer, ParamArray c As Integer())
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2(a As System.Int32, ParamArray c As System.Int32())) (OperationKind.InvocationExpression, Type: System.Void)
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P)
  IArgument (ArgumentKind.Positional Matching Parameter: a) (OperationKind.Argument)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArgument (ArgumentKind.ParamArray Matching Parameter: c) (OperationKind.Argument)
    IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32())
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      IArrayInitializer (OperationKind.ArrayInitializer)
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
        ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
]]>.Value

            VerifyOperationTreeForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree)
        End Sub



    End Class
End Namespace

