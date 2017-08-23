' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase
        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_SimplePropertyAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.F'BIND:"d.F"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'd.F')
  Expression: IDynamicMemberReferenceExpression (Member Name: "F", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.F')
      Type Arguments(0)
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_GenericPropertyAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.F(Of String)'BIND:"d.F(Of String)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'd.F(Of String)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "F", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.F(Of String)')
      Type Arguments(1):
        Symbol: System.String
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_InvalidGenericPropertyAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.F(Of)'BIND:"d.F(Of)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object, IsInvalid) (Syntax: 'd.F(Of)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "F", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object, IsInvalid) (Syntax: 'd.F(Of)')
      Type Arguments(1):
        Symbol: ?
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30182: Type expected.
        d.F(Of)'BIND:"d.F(Of)"
              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_SimpleMethodCall()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.F()'BIND:"d.F()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'd.F()')
  Expression: IDynamicMemberReferenceExpression (Member Name: "F", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.F')
      Type Arguments(0)
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_InvalidMethodCall_MissingParen()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.F('BIND:"d.F("
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object, IsInvalid) (Syntax: 'd.F(')
  Expression: IDynamicMemberReferenceExpression (Member Name: "F", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.F')
      Type Arguments(0)
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(1):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: '')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
            Children(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30198: ')' expected.
        d.F('BIND:"d.F("
            ~
BC30201: Expression expected.
        d.F('BIND:"d.F("
            ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_GenericMethodCall_SingleGeneric()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.GetValue(Of String)()'BIND:"d.GetValue(Of String)()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'd.GetValue(Of String)()')
  Expression: IDynamicMemberReferenceExpression (Member Name: "GetValue", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.GetValue(Of String)')
      Type Arguments(1):
        Symbol: System.String
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_GenericMethodCall_MultipleGeneric()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.GetValue(Of String, Integer)()'BIND:"d.GetValue(Of String, Integer)()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'd.GetValue( ...  Integer)()')
  Expression: IDynamicMemberReferenceExpression (Member Name: "GetValue", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.GetValue( ... g, Integer)')
      Type Arguments(2):
        Symbol: System.String
        Symbol: System.Int32
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_GenericMethodCall_InvalidGenericParameter()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.GetValue(Of String,)()'BIND:"d.GetValue(Of String,)()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object, IsInvalid) (Syntax: 'd.GetValue(Of String,)()')
  Expression: IDynamicMemberReferenceExpression (Member Name: "GetValue", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object, IsInvalid) (Syntax: 'd.GetValue(Of String,)')
      Type Arguments(2):
        Symbol: System.String
        Symbol: ?
      Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30182: Type expected.
        d.GetValue(Of String,)()'BIND:"d.GetValue(Of String,)()"
                             ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_NestedPropertyAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.Prop1.Prop2'BIND:"d.Prop1.Prop2"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'd.Prop1.Prop2')
  Expression: IDynamicMemberReferenceExpression (Member Name: "Prop2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.Prop1.Prop2')
      Type Arguments(0)
      Instance Receiver: IDynamicMemberReferenceExpression (Member Name: "Prop1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.Prop1')
          Type Arguments(0)
          Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_NestedMethodAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.Method1().Method2()'BIND:"d.Method1().Method2()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'd.Method1().Method2()')
  Expression: IDynamicMemberReferenceExpression (Member Name: "Method2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.Method1().Method2')
      Type Arguments(0)
      Instance Receiver: IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'd.Method1()')
          Expression: IDynamicMemberReferenceExpression (Member Name: "Method1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.Method1')
              Type Arguments(0)
              Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'd')
          ApplicableSymbols(0)
          Arguments(0)
          ArgumentNames(0)
          ArgumentRefKinds(0)
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_NestedPropertyAndMethodAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.Prop1.Method2()'BIND:"d.Prop1.Method2()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'd.Prop1.Method2()')
  Expression: IDynamicMemberReferenceExpression (Member Name: "Method2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.Prop1.Method2')
      Type Arguments(0)
      Instance Receiver: IDynamicMemberReferenceExpression (Member Name: "Prop1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'd.Prop1')
          Type Arguments(0)
          Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_LateBoundModuleFunction()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim x As Object = New List(Of Integer)()
        fun(x)'BIND:"fun(x)"
    End Sub

    Sub fun(Of X)(ByVal a As List(Of X))
    End Sub

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'fun(x)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "fun", Containing Type: Module1) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'fun')
      Type Arguments(0)
      Instance Receiver: null
  ApplicableSymbols(1):
    Symbol: Sub Module1.fun(Of X)(a As System.Collections.Generic.List(Of X))
  Arguments(1):
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'x')
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_LateBoundClassFunction()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim x As Object = New List(Of Integer)()
        Dim c1 As New C1
        c1.fun(x)'BIND:"c1.fun(x)"
    End Sub

    Class C1
        Sub fun(Of X)(ByVal a As List(Of X))
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'c1.fun(x)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "fun", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c1.fun')
      Type Arguments(0)
      Instance Receiver: ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Module1.C1) (Syntax: 'c1')
  ApplicableSymbols(1):
    Symbol: Sub Module1.C1.fun(Of X)(a As System.Collections.Generic.List(Of X))
  Arguments(1):
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'x')
  ArgumentNames(0)
  ArgumentRefKinds(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace

