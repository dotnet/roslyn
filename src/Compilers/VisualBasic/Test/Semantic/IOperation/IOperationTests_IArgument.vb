' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: '0.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: '0.0')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: 'M2')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: '1.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '0')
        ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: '1.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(3):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: '1.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument) (Syntax: '2.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(3):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: '1.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument) (Syntax: '2.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(3):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: '2.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: c) (OperationKind.Argument) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: 'M2')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(3):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: 'M2')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument) (Syntax: '2.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub


        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'x')
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'x')
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: 'y')
        ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'y')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(1):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1.0')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: '1.0')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
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
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'x')
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'x')
        InConversion: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'x')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IOperation:  (OperationKind.None) (Syntax: 'x')
        OutConversion: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double) (Syntax: 'x')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IPlaceholderExpression (OperationKind.PlaceholderExpression, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub PositionalArgumentForExtensionMethod()
            Dim source = <![CDATA[
Imports System.Runtime.CompilerServices

Class P
    Sub M1()
        E1(1, 2)'BIND:"E1(1, 2)"
    End Sub
End Class

Module Extensions
    <Extension()>
    Public Sub E1(a As P, Optional b As Integer = 0, Optional c As Integer = 0)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.E1([b As System.Int32 = 0], [c As System.Int32 = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'E1(1, 2)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'E1')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument) (Syntax: '2')
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub NamedArgumentOutOfParameterOrderForExtensionMethod()
            Dim source = <![CDATA[
Imports System.Runtime.CompilerServices

Class P
    Sub M1()
        E1(c:=1, b:=2)'BIND:"E1(c:=1, b:=2)"
    End Sub
End Class

Module Extensions
    <Extension()>
    Public Sub E1(a As P, Optional b As Integer = 0, Optional c As Integer = 0)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.E1([b As System.Int32 = 0], [c As System.Int32 = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'E1(c:=1, b:=2)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'E1')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: '2')
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub



        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ParamsArrayArgumentInNormalForm()
            Dim source = <![CDATA[
Class P
    Sub M1()
        Dim a = New Integer() {1, 2, 3}
        M2(1, a)'BIND:"M2(1, a)"
    End Sub

    Sub M2(x As Integer, ParamArray y As Integer())
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2(x As System.Int32, ParamArray y As System.Int32())) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, a)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'a')
        ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'a')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ParamsArrayArgumentInExpandedForm()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(1, 2, 3)'BIND:"M2(1, 2, 3)"
    End Sub

    Sub M2(x As Integer, ParamArray y As Integer())
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2(x As System.Int32, ParamArray y As System.Int32())) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, 2, 3)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.ParamArray, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'M2(1, 2, 3)')
        IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'M2(1, 2, 3)')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'M2(1, 2, 3)')
          Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: 'M2(1, 2, 3)')
              Element Values(2):
                  ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ParamsArrayArgumentInExpandedFormWithNoArgument()
            Dim source = <![CDATA[
Imports System.Runtime.CompilerServices

Class P
    Sub M1()
        M2(1)'BIND:"M2(1)"
    End Sub

    Sub M2(x As Integer, ParamArray y As Integer())
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub P.M2(x As System.Int32, ParamArray y As System.Int32())) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.ParamArray, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'M2(1)')
        IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'M2(1)')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2(1)')
          Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: 'M2(1)')
              Element Values(0)
        InConversion: null
        OutConversion: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Error_MissingRequiredArgument()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2()'BIND:"M2()"
    End Sub

    Sub M2(x As Integer, Optional y As Integer = 0, Optional z As Integer = 0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'M2()')
  Children(1):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30455: Argument not specified for parameter 'x' of 'Public Sub M2(x As Integer, [y As Integer = 0], [z As Integer = 0])'.
        M2()'BIND:"M2()"
        ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Error_TooManyArguments()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(1, 2)'BIND:"M2(1, 2)"
    End Sub

    Sub M2(x As Integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'M2(1, 2)')
  Children(3):
      IOperation:  (OperationKind.None) (Syntax: 'M2')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30057: Too many arguments to 'Public Sub M2(x As Integer)'.
        M2(1, 2)'BIND:"M2(1, 2)"
              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Error_ExtraOmittedArgument()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(0,,,)'BIND:"M2(0,,,)"
    End Sub

    Sub M2(x As Integer, Optional y As Integer = 0, Optional z As Integer = 0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'M2(0,,,)')
  Children(5):
      IOperation:  (OperationKind.None) (Syntax: 'M2')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IOmittedArgumentExpression (OperationKind.OmittedArgumentExpression, Type: null) (Syntax: '')
      IOmittedArgumentExpression (OperationKind.OmittedArgumentExpression, Type: null) (Syntax: '')
      IOmittedArgumentExpression (OperationKind.OmittedArgumentExpression, Type: null, IsInvalid) (Syntax: '')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30057: Too many arguments to 'Public Sub M2(x As Integer, [y As Integer = 0], [z As Integer = 0])'.
        M2(0,,,)'BIND:"M2(0,,,)"
               ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Error_OmittingParamArrayArgument()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(0, )'BIND:"M2(0, )"
    End Sub

    Sub M2(x As Integer, ParamArray array As Integer())
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'M2(0, )')
  Children(3):
      IOperation:  (OperationKind.None) (Syntax: 'M2')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IOmittedArgumentExpression (OperationKind.OmittedArgumentExpression, Type: null, IsInvalid) (Syntax: '')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30588: Omitted argument cannot match a ParamArray parameter.
        M2(0, )'BIND:"M2(0, )"
              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Error_NamedArgumentMatchingParamArray()
            Dim source = <![CDATA[
Class P
    Sub M1()
        Dim a = New Integer() {}
        M2(x:=0, array:=a)'BIND:"M2(x:=0, array:=a)"
    End Sub

    Sub M2(x As Integer, ParamArray array As Integer())
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'M2(x:=0, array:=a)')
  Children(3):
      IOperation:  (OperationKind.None) (Syntax: 'M2')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'a')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30587: Named argument cannot match a ParamArray parameter.
        M2(x:=0, array:=a)'BIND:"M2(x:=0, array:=a)"
                 ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Error_NamedArgumenNotExist()
            Dim source = <![CDATA[
Class P
    Sub M1()
        M2(y:=1)'BIND:"M2(y:=1)"
    End Sub

    Sub M2(x As Integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'M2(y:=1)')
  Children(2):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M2')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30455: Argument not specified for parameter 'x' of 'Public Sub M2(x As Integer)'.
        M2(y:=1)'BIND:"M2(y:=1)"
        ~~
BC30272: 'y' is not a parameter of 'Public Sub M2(x As Integer)'.
        M2(y:=1)'BIND:"M2(y:=1)"
           ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

    End Class
End Namespace
