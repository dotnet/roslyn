' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase
        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_Basic()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As Object, d As Object)
        c.M2(d)'BIND:"c.M2(d)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'c.M2(d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c.M2')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'c')
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_MultipleApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object)
        M2(d)'BIND:"M2(d)"
    End Sub

    Private Sub M2(c As Integer)
    End Sub

    Private Sub M2(c As Long)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'M2(d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'M2')
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_MultipleArgumentsAndApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object, e As Object)
        M2(d, e)'BIND:"M2(d, e)"
    End Sub

    Private Sub M2(c As Integer, d As Object)
    End Sub

    Private Sub M2(c As Long, d As Object)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'M2(d, e)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'M2')
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'e')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_ArgumentNames()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object, e As Object)
        M2(d:=d, c:=e)'BIND:"M2(d:=d, c:=e)"
    End Sub

    Private Sub M2(c As Integer, d As Object)
    End Sub

    Private Sub M2(c As Long, d As Object)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'M2(d:=d, c:=e)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'M2')
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'e')
  ArgumentNames(2):
    "d"
    "c"
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_ArgumentRefKinds()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Runtime.InteropServices

Class C
    Private Sub M(d As Object, e As Object)
        M2(d, e)'BIND:"M2(d, e)"
    End Sub

    Private Sub M2(<Out> ByRef c As Integer, ByRef d As Object)
        c = 0
    End Sub

    Private Sub M2(<Out> ByRef c As Long, ByRef d As Object)
        c = 0
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'M2(d, e)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'M2')
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'e')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_OverloadResolutionFailure()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object)
        M2(d)'BIND:"M2(d)"
    End Sub

    Private Sub M2()
    End Sub

    Private Sub M2(c1 As Integer, c2 As Long)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'M2(d)')
  Children(2):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M2')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30516: Overload resolution failed because no accessible 'M2' accepts this number of arguments.
        M2(d)'BIND:"M2(d)"
        ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_PropertyGroup_MultipleApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object)
        Dim x = c(d)'BIND:"c(d)"
    End Sub

    Default ReadOnly Property P1(x As Integer) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'c(d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_PropertyGroup_MultipleArgumentsAndApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object)
        Dim x = c(d, d)'BIND:"c(d, d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'c(d, d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_PropertyGroup_ArgumentNames()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object, e As Object)
        Dim x = c(x2:=e, x:=d)'BIND:"c(x2:=e, x:=d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'c(x2:=e, x:=d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  Arguments(2):
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'e')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  ArgumentNames(2):
    "x2"
    "x"
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_PropertyGroup_ArgumentRefKinds()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Runtime.InteropServices

Class C
    Private Sub M(c As C, d As Object, e As Object)
        Dim x = c(x2:=e, x:=d)'BIND:"c(x2:=e, x:=d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, <Out> ByRef x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, <Out> ByRef x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'c(x2:=e, x:=d)')
  Expression: IDynamicMemberReferenceExpression (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  Arguments(2):
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'e')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  ArgumentNames(2):
    "x2"
    "x"
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30651: property parameters cannot be declared 'ByRef'.
    Default ReadOnly Property P1(x As Integer, <Out> ByRef x2 As Object) As Integer
                                                     ~~~~~
BC30651: property parameters cannot be declared 'ByRef'.
    Default ReadOnly Property P1(x As String, <Out> ByRef x2 As Object) As Integer
                                                    ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_PropertyGroup_OverloadResolutionFailure()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object)
        Dim x = c(c, d)'BIND:"c(c, d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32, IsInvalid) (Syntax: 'c(c, d)')
  Children(3):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'c')
      IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30518: Overload resolution failed because no accessible 'P1' can be called with these arguments:
    'Public ReadOnly Default Property P1(x As Integer, x2 As Object) As Integer': Value of type 'C' cannot be converted to 'Integer'.
    'Public ReadOnly Default Property P1(x As String, x2 As Object) As Integer': Value of type 'C' cannot be converted to 'String'.
        Dim x = c(c, d)'BIND:"c(c, d)"
                ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_InvokeDelegateWithArgument()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Runtime.InteropServices

Class C
    Delegate Sub MySubDelegate(ByVal x As Integer)

    Private Sub M(c As C, d As Object)
        Dim x = c(d)(d)'BIND:"c(d)(d)"
    End Sub

    Default ReadOnly Property P1(x As Integer) As MySubDelegate
        Get
            Return Nothing
        End Get
    End Property

    Default ReadOnly Property P1(x As String) As MySubDelegate
        Get
            Return Nothing
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'c(d)(d)')
  Expression: IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'c(d)')
      Expression: IDynamicMemberReferenceExpression (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'c')
          Type Arguments(0)
          Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
      Arguments(1):
          IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
      ArgumentNames(0)
      ArgumentRefKinds: null
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace

