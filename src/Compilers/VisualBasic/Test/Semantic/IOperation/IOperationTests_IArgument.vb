' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact()>
        Public Sub PositionalArgument()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(1, 0.0)'BIND:"M2(1, 0.0)"
    End Sub

    Sub M2(a As Integer, b As Double)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2(a As System.Int32, b As System.Double)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, 0.0)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit Matching Parameter: b) (OperationKind.Argument) (Syntax: '0.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: '0.0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub PositionalArgumentWithDefaultValue()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(1)'BIND:"M2(1)"
    End Sub

    Sub M2(a As Integer, Optional b As Double = 0.0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2(a As System.Int32, [b As System.Double = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.DefaultValue Matching Parameter: b) (OperationKind.Argument) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub NamedArgumentListedInParameterOrder()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(a:=1, b:=1.0)'BIND:"M2(a:=1, b:=1.0)"
    End Sub

    Sub M2(a As Integer, Optional b As Double = 0.0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2(a As System.Int32, [b As System.Double = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(a:=1, b:=1.0)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit Matching Parameter: b) (OperationKind.Argument) (Syntax: '1.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub NamedArgumentListedOutOfParameterOrder()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(b:=1.0, a:=0)'BIND:"M2(b:=1.0, a:=0)"
    End Sub

    Sub M2(a As Integer, Optional b As Double = 0.0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2(a As System.Int32, [b As System.Double = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(b:=1.0, a:=0)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: a) (OperationKind.Argument) (Syntax: '0')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IArgument (ArgumentKind.Explicit Matching Parameter: b) (OperationKind.Argument) (Syntax: '1.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub NamedArgumentInParameterOrderWithDefaultValue()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(b:=1.0, c:=2.0)'BIND:"M2(b:=1.0, c:=2.0)"
    End Sub

    Sub M2(Optional a As Integer = 0, Optional b As Double = 0.0, Optional c As Double = 0.0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2([a As System.Int32 = 0], [b As System.Double = 0], [c As System.Double = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(b:=1.0, c:=2.0)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(3): IArgument (ArgumentKind.DefaultValue Matching Parameter: a) (OperationKind.Argument) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2')
    IArgument (ArgumentKind.Explicit Matching Parameter: b) (OperationKind.Argument) (Syntax: '1.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
    IArgument (ArgumentKind.Explicit Matching Parameter: c) (OperationKind.Argument) (Syntax: '2.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub NamedArgumentInParameterOrderWithDefaultValueUsingOmittedSyntax()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(, b:=1.0, c:=2.0)'BIND:"M2(, b:=1.0, c:=2.0)"
    End Sub

    Sub M2(Optional a As Integer = 0, Optional b As Double = 0.0, Optional c As Double = 0.0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2([a As System.Int32 = 0], [b As System.Double = 0], [c As System.Double = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(, b:=1.0, c:=2.0)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(3): IArgument (ArgumentKind.DefaultValue Matching Parameter: a) (OperationKind.Argument) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2')
    IArgument (ArgumentKind.Explicit Matching Parameter: b) (OperationKind.Argument) (Syntax: '1.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
    IArgument (ArgumentKind.Explicit Matching Parameter: c) (OperationKind.Argument) (Syntax: '2.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub NamedArgumentOutOfParameterOrderWithDefaultValue()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(b:=2.0)'BIND:"M2(b:=2.0)"
    End Sub

    Sub M2(Optional a As Integer = 0, Optional b As Double = 0.0, Optional c As Double = 0.0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2([a As System.Int32 = 0], [b As System.Double = 0], [c As System.Double = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(b:=2.0)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(3): IArgument (ArgumentKind.DefaultValue Matching Parameter: a) (OperationKind.Argument) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2')
    IArgument (ArgumentKind.Explicit Matching Parameter: b) (OperationKind.Argument) (Syntax: '2.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
    IArgument (ArgumentKind.DefaultValue Matching Parameter: c) (OperationKind.Argument) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub NamedAndPositionalArgumentsWithDefaultValue()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(1, c:=2.0)'BIND:"M2(1, c:=2.0)"
    End Sub

    Sub M2(Optional a As Integer = 0, Optional b As Double = 0.0, Optional c As Double = 0.0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2([a As System.Int32 = 0], [b As System.Double = 0], [c As System.Double = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, c:=2.0)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(3): IArgument (ArgumentKind.Explicit Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.DefaultValue Matching Parameter: b) (OperationKind.Argument) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: 'M2')
    IArgument (ArgumentKind.Explicit Matching Parameter: c) (OperationKind.Argument) (Syntax: '2.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub PositionalByRefNonModifiableArgument()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(1)'BIND:"M2(1)"
    End Sub

    Sub M2(ByRef Optional a As Integer = 0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2([ByRef a As System.Int32 = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(1): IArgument (ArgumentKind.Explicit Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub


        <Fact()>
        Public Sub PositionalByRefModifiableArgument()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        Dim x = 1
        M2(x)'BIND:"M2(x)"
    End Sub

    Sub M2(ByRef Optional a As Integer = 0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2([ByRef a As System.Int32 = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(x)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(1): IArgument (ArgumentKind.Explicit Matching Parameter: a) (OperationKind.Argument) (Syntax: 'x')
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub NamedByRefModifiableArgumentsOutOfParameterOrder()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        Dim x = 1
        Dim y = 1.0
        M2(b:=y, a:=x)'BIND:"M2(b:=y, a:=x)"
    End Sub

    Sub M2(ByRef Optional a As Integer = 0, ByRef Optional b As Double = 0.0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2([ByRef a As System.Int32 = 0], [ByRef b As System.Double = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(b:=y, a:=x)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: a) (OperationKind.Argument) (Syntax: 'x')
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
    IArgument (ArgumentKind.Explicit Matching Parameter: b) (OperationKind.Argument) (Syntax: 'y')
      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub DefaultValueForByRefParameter()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        Dim x = 1.0
        M2()'BIND:"M2()"
    End Sub

    Sub M2(ByRef Optional a As Integer = 0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2([ByRef a As System.Int32 = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2()')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(1): IArgument (ArgumentKind.DefaultValue Matching Parameter: a) (OperationKind.Argument) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub PositionalByRefNonModifiableArgumentWithConversion()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(1.0)'BIND:"M2(1.0)"
    End Sub

    Sub M2(ByRef Optional a As Integer = 0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2([ByRef a As System.Int32 = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1.0)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(1): IArgument (ArgumentKind.Explicit Matching Parameter: a) (OperationKind.Argument) (Syntax: '1.0')
      IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: '1.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub PositionalByRefModifiableArgumentWithConversion()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        Dim x = 1.0
        M2(x)'BIND:"M2(x)"
    End Sub

    Sub M2(ByRef Optional a As Integer = 0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2([ByRef a As System.Int32 = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(x)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
  Arguments(1): IArgument (ArgumentKind.Explicit Matching Parameter: a) (OperationKind.Argument) (Syntax: 'x')
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'x')
      InConversion: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'x')
          IOperation:  (OperationKind.None) (Syntax: 'x')
      OutConversion: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Double) (Syntax: 'x')
          IPlaceholderExpression (OperationKind.PlaceholderExpression, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub



    End Class
End Namespace

