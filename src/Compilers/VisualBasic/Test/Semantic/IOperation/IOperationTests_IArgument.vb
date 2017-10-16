' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Semantics

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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: '0.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: '0.0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument, IsImplicit) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0, IsImplicit) (Syntax: 'M2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'a:=1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: 'b:=1.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'a:=0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: 'b:=1.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(3):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, IsImplicit) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'M2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: 'b:=1.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument) (Syntax: 'c:=2.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(3):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, IsImplicit) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'M2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: 'b:=1.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument) (Syntax: 'c:=2.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(3):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, IsImplicit) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'M2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: 'b:=2.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: c) (OperationKind.Argument, IsImplicit) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0, IsImplicit) (Syntax: 'M2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(3):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument, IsImplicit) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0, IsImplicit) (Syntax: 'M2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument) (Syntax: 'c:=2.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'x')
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'a:=x')
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: 'b:=y')
        ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, IsImplicit) (Syntax: 'M2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'M2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1.0')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1.0')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, IsImplicit) (Syntax: 'x')
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'x')
        InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit) (Syntax: 'E1')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument) (Syntax: '2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit) (Syntax: 'E1')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: 'b:=2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument) (Syntax: 'c:=1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'a')
        ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'a')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.ParamArray, Matching Parameter: y) (OperationKind.Argument, IsImplicit) (Syntax: 'M2(1, 2, 3)')
        IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(), IsImplicit) (Syntax: 'M2(1, 2, 3)')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'M2(1, 2, 3)')
          Initializer: 
            IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsImplicit) (Syntax: 'M2(1, 2, 3)')
              Element Values(2):
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.ParamArray, Matching Parameter: y) (OperationKind.Argument, IsImplicit) (Syntax: 'M2(1)')
        IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(), IsImplicit) (Syntax: 'M2(1)')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'M2(1)')
          Initializer: 
            IArrayInitializer (0 elements) (OperationKind.ArrayInitializer, IsImplicit) (Syntax: 'M2(1)')
              Element Values(0)
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsInvalid, IsImplicit) (Syntax: 'M2')
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
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
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
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
        Public Sub TestValidDynamicInvocation_OmittedArgument()
            Dim source = <![CDATA[
Option Strict Off

Class P
    Sub M1(o As Object)
        M2(o,,)'BIND:"M2(o,,)"
    End Sub

    Sub M2(x As Integer, Optional y As Integer = 0, Optional z As Integer = 0)
    End Sub

    Sub M2(x As Double, Optional y As Integer = 0, Optional z As Integer = 0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'M2(o,,)')
  Expression: 
    IDynamicMemberReferenceExpression (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit) (Syntax: 'M2')
  Arguments(3):
      IParameterReferenceExpression: o (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'o')
      IOmittedArgumentExpression (OperationKind.OmittedArgumentExpression, Type: System.Object) (Syntax: '')
      IOmittedArgumentExpression (OperationKind.OmittedArgumentExpression, Type: System.Object) (Syntax: '')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

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
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsImplicit) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: P, IsInvalid, IsImplicit) (Syntax: 'M2')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub InOutConversion()
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
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, IsImplicit) (Syntax: 'x')
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'x')
        InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub InOutConversionUserDefined()
            Dim source = <![CDATA[
Class C
    Public Shared Widening Operator CType(ByVal c As C) As Integer
        Return 0
    End Operator

    Public Shared Narrowing Operator CType(ByVal i As Integer) As C
        Return New C()
    End Operator
End Class

Class Program
    Sub M1()
        Dim x = New C()
        M2(x)'BIND:"M2(x)"
    End Sub

    Sub M2(ByRef a As Integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2(ByRef a As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(x)')
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, IsImplicit) (Syntax: 'x')
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: C) (Syntax: 'x')
        InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Implicit(c As C) As System.Int32)
        OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Explicit(i As System.Int32) As C)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub InOutConversionUserDefinedWithIntermediateConversion()
            Dim source = <![CDATA[
Class C
    Public Shared Widening Operator CType(ByVal c As C) As Integer
        Return 0
    End Operator

    Public Shared Narrowing Operator CType(ByVal i As Integer) As C
        Return New C()
    End Operator
End Class

Class Program
    Sub M1()
        Dim x = 2.0
        M2(x)'BIND:"M2(x)"
    End Sub

    Sub M2(ByRef c As C)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationExpression ( Sub Program.M2(ByRef c As C)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(x)')
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, IsImplicit) (Syntax: 'x')
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'x')
        InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Explicit(i As System.Int32) As C)
        OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Implicit(c As C) As System.Int32)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub InOutConversionUserDefinedMissingOperator()
            Dim source = <![CDATA[
Class C
    Public Shared Widening Operator CType(ByVal c As C) As Integer
        Return 0
    End Operator
End Class

Class Program
    Sub M1()
        Dim x = New C()
        M2(x)'BIND:"M2(x)"
    End Sub

    Sub M2(ByRef a As Integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'M2(x)')
  Children(2):
      IOperation:  (OperationKind.None) (Syntax: 'M2')
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: C, IsInvalid) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC33037: Cannot copy the value of 'ByRef' parameter 'a' back to the matching argument because type 'Integer' cannot be converted to type 'C'.
        M2(x)'BIND:"M2(x)"
           ~
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GettingInOutConverionFromVBArgument()
            Dim source = <![CDATA[
Class C
    Public Shared Widening Operator CType(ByVal c As C) As Integer
        Return 0
    End Operator

    Public Shared Narrowing Operator CType(ByVal i As Integer) As C
        Return New C()
    End Operator
End Class

Class Program
    Sub M1()
        Dim x = New C()
        M2(x)'BIND:"M2(x)"
    End Sub

    Sub M2(ByRef a As Integer)
    End Sub
End Class]]>.Value

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName, options:=Nothing)

            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime({syntaxTree}, DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef}))
            Dim result = GetOperationAndSyntaxForTest(Of InvocationExpressionSyntax)(compilation, fileName)

            Dim expectedInKind = ConversionKind.Widening Or ConversionKind.UserDefined
            Dim exptectedInMethod = compilation.GetSymbolsWithName(Function (name As string)
                                                                       Return name = "op_Implicit"
                                                                   End Function, SymbolFilter.Member).Single()

            Dim expectedOutKind = ConversionKind.Narrowing Or ConversionKind.UserDefined
            Dim expectedOutMethod = compilation.GetSymbolsWithName(Function (name As string)
                                                                       Return name = "op_Explicit"
                                                                   End Function, SymbolFilter.Member).Single()

            Dim invocation = CType(result.operation, IInvocationExpression)
            Dim argument = invocation.ArgumentsInEvaluationOrder(0)

            Dim inConversion = argument.GetInConversion()
            Assert.Same(exptectedInMethod, inConversion.MethodSymbol)
            Assert.Equal(expectedInKind, inConversion.Kind)

            Dim outConversion = argument.GetOutConversion()
            Assert.Same(expectedOutMethod, outConversion.MethodSymbol)
            Assert.Equal(expectedOutKind, outConversion.Kind)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestCloneInOutConversion()
            Dim source = <![CDATA[
Class C
    Public Shared Widening Operator CType(ByVal c As C) As Integer
        Return 0
    End Operator

    Public Shared Narrowing Operator CType(ByVal i As Integer) As C
        Return New C()
    End Operator
End Class

Class Program
    Sub M1()
        Dim x = New C()
        Dim y = New C()
        Dim z = New C()
        M2(x, y, z)
    End Sub

    Sub M2(ByRef a As Integer, ByRef b As Double, ByRef c As C)
    End Sub
End Class]]>.Value

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName, options:=Nothing)

            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime({syntaxTree}, DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef}))
            Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()
            Dim model = compilation.GetSemanticModel(tree)

            VerifyClone(model)
        End Sub

        <Fact>
        Public Sub DirectlyBindArgument_InvocationExpression()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(1)'BIND:"1"
    End Sub

    Sub M2(a As Integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArgumentSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub DirectlyBindParamsArgument1_InvocationExpression()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(1)'BIND:"M2(1)"
    End Sub

    Sub M2(paramarray a As Integer())
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'M2(1)')
  Expression: 
    IInvocationExpression ( Sub Program.M2(ParamArray a As System.Int32())) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
      Arguments(1):
          IArgument (ArgumentKind.ParamArray, Matching Parameter: a) (OperationKind.Argument, IsImplicit) (Syntax: 'M2(1)')
            IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(), IsImplicit) (Syntax: 'M2(1)')
              Dimension Sizes(1):
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'M2(1)')
              Initializer: 
                IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsImplicit) (Syntax: 'M2(1)')
                  Element Values(1):
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ExpressionStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub DirectlyBindParamsArgument2_InvocationExpression()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(0, 1)'BIND:"M2(0, 1)"
    End Sub

    Sub M2(paramarray a As Integer())
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'M2(0, 1)')
  Expression: 
    IInvocationExpression ( Sub Program.M2(ParamArray a As System.Int32())) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(0, 1)')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
      Arguments(1):
          IArgument (ArgumentKind.ParamArray, Matching Parameter: a) (OperationKind.Argument, IsImplicit) (Syntax: 'M2(0, 1)')
            IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(), IsImplicit) (Syntax: 'M2(0, 1)')
              Dimension Sizes(1):
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'M2(0, 1)')
              Initializer: 
                IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsImplicit) (Syntax: 'M2(0, 1)')
                  Element Values(2):
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ExpressionStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub DirectlyBindOmittedArgument_InvocationExpression()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(1, , 2)'BIND:"M2(1, , 2)"
    End Sub

    Sub M2(a As Integer, Optional b As Integer = 0, Optional c As Integer = 0)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'M2(1, , 2)')
  Expression: 
    IInvocationExpression ( Sub Program.M2(a As System.Int32, [b As System.Int32 = 0], [c As System.Int32 = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, , 2)')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsImplicit) (Syntax: 'M2')
      Arguments(3):
          IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument, IsImplicit) (Syntax: 'M2')
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'M2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument) (Syntax: '2')
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ExpressionStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub DirectlyBindNAmedArgument1_InvocationExpression()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(b:=1, a:=1)'BIND:"b:=1"
    End Sub

    Sub M2(a As Integer, b as integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArgument (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument) (Syntax: 'b:=1')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArgumentSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub DirectlyBindNAmedArgument2_InvocationExpression()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        M2(b:=1, a:=1)'BIND:"a:=1"
    End Sub

    Sub M2(a As Integer, b as integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: 'a:=1')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArgumentSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub DirectlyBindArgument_ObjectCreation()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        dim p = new Program(1)'BIND:"1"
    End Sub

    Sub new(a As Integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArgument (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument) (Syntax: '1')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArgumentSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub DirectlyBindParamsArgument1_ObjectCreation()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        Dim p = New Program(1)'BIND:"New Program(1)"
    End Sub

    Sub new(paramarray a As Integer())
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationExpression (Constructor: Sub Program..ctor(ParamArray a As System.Int32())) (OperationKind.ObjectCreationExpression, Type: Program) (Syntax: 'New Program(1)')
  Arguments(1):
      IArgument (ArgumentKind.ParamArray, Matching Parameter: a) (OperationKind.Argument, IsImplicit) (Syntax: 'Program')
        IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(), IsImplicit) (Syntax: 'Program')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'Program')
          Initializer: 
            IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsImplicit) (Syntax: 'Program')
              Element Values(1):
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Initializer: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub DirectlyBindParamsArgument2_ObjectCreation()
            Dim source = <![CDATA[
Class Program
    Sub M1()
        dim p = new Program(0, 1)'BIND:"new Program(0, 1)"
    End Sub

    Sub new(paramarray a As Integer())
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationExpression (Constructor: Sub Program..ctor(ParamArray a As System.Int32())) (OperationKind.ObjectCreationExpression, Type: Program) (Syntax: 'new Program(0, 1)')
  Arguments(1):
      IArgument (ArgumentKind.ParamArray, Matching Parameter: a) (OperationKind.Argument, IsImplicit) (Syntax: 'Program')
        IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(), IsImplicit) (Syntax: 'Program')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'Program')
          Initializer: 
            IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsImplicit) (Syntax: 'Program')
              Element Values(2):
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Initializer: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub DirectlyBindArgument_RangeArgument()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim a(0 To 20) As Integer'BIND:"Dim a(0 To 20) As Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a(0 To  ...  As Integer')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a(0 To 20)')
    Variables: Local_1: a As System.Int32()
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsImplicit) (Syntax: 'a(0 To 20)')
        IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'a(0 To 20)')
          Dimension Sizes(1):
              IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 21, IsImplicit) (Syntax: '0 To 20')
                Left: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
                Right: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '0 To 20')
          Initializer: 
            null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
