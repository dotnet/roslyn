' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact()>
        Public Sub ExplicitSimpleArgument()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(1, 2.0)'BIND:"M2(1, 2.0)"
    End Sub

    Sub M2(x As Integer, y As Double)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2(x As System.Int32, y As System.Double)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, 2.0)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit Matching Parameter: y) (OperationKind.Argument) (Syntax: '2.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub ExplicitSimpleArgumentByName()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(y:=1, x:=2.0)'BIND:"M2(y:=1, x:=2.0)"
    End Sub

    Sub M2(x As Integer, y As Double)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2(x As System.Int32, y As System.Double)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(y:=1, x:=2.0)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: '2.0')
      IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 2) (Syntax: '2.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
    IArgument (ArgumentKind.Explicit Matching Parameter: y) (OperationKind.Argument) (Syntax: '1')
      IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 1) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub XXXX()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(y:=2)'BIND:"M2(y:=2)"
    End Sub

    Sub M2(Optional x As Integer = 0, Optional y As Integer = 0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2([x As System.Int32 = 0], [y As System.Int32 = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(y:=2)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.DefaultValue Matching Parameter: x) (OperationKind.Argument) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2')
    IArgument (ArgumentKind.Explicit Matching Parameter: y) (OperationKind.Argument) (Syntax: '2')
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub DefaultArgument()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(1)'BIND:"M2(1)"
    End Sub

    Sub M2(x As Integer, Optional y As Double = 0.0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2(x As System.Int32, [y As System.Double = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.DefaultValue Matching Parameter: y) (OperationKind.Argument) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub ParamArrayArgument()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(1, 2, 3)'BIND:"M2(1, 2, 3)"
    End Sub

    Sub M2(x As Integer, ParamArray array As Integer())
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2(x As System.Int32, ParamArray array As System.Int32())) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, 2, 3)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.ParamArray Matching Parameter: array) (OperationKind.Argument) (Syntax: 'M2(1, 2, 3)')
      IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'M2(1, 2, 3)')
        Dimension Sizes(1): ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'M2(1, 2, 3)')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: 'M2(1, 2, 3)')
            Element Values(2): ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
              ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub ArrayAsParamsArrayArgument()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(1, New Integer() {2, 3})'BIND:"M2(1, New Integer() {2, 3})"
    End Sub

    Sub M2(x As Integer, ParamArray array As Integer())
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2(x As System.Int32, ParamArray array As System.Int32())) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, New I ... r() {2, 3})')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit Matching Parameter: array) (OperationKind.Argument) (Syntax: 'New Integer() {2, 3}')
      IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'New Integer() {2, 3}')
        Dimension Sizes(1): ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'New Integer() {2, 3}')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: '{2, 3}')
            Element Values(2): ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
              ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

    End Class
End Namespace

