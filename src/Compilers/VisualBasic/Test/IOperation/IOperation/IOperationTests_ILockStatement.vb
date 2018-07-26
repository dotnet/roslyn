' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILockStatement_ObjectLock_FieldReference()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Dim o As New Object
    Public Sub M1()
        SyncLock o'BIND:"SyncLock o"

        End SyncLock
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
  Expression: 
    IFieldReferenceOperation: C1.o As System.Object (OperationKind.FieldReference, Type: System.Object) (Syntax: 'o')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'SyncLock o' ... nd SyncLock')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILockStatement_ObjectLock_LocalReference()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        Dim o As New Object
        SyncLock o'BIND:"SyncLock o"

        End SyncLock
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'SyncLock o' ... nd SyncLock')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILockStatement_ObjectLock_Nothing()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        Dim o As New Object
        SyncLock o'BIND:"SyncLock o"

        End SyncLock
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'SyncLock o' ... nd SyncLock')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILockStatement_ObjectLock_NonReferenceType()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        Dim i As Integer = 1
        SyncLock i'BIND:"SyncLock i"

        End SyncLock
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'SyncLock i' ... nd SyncLock')
  Expression: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'SyncLock i' ... nd SyncLock')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30582: 'SyncLock' operand cannot be of type 'Integer' because 'Integer' is not a reference type.
        SyncLock i'BIND:"SyncLock i"
                 ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILockStatement_MissingLockExpression()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        SyncLock'BIND:"SyncLock"

        End SyncLock
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'SyncLock'BI ... nd SyncLock')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'SyncLock'BI ... nd SyncLock')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        SyncLock'BIND:"SyncLock"
                ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILockStatement_InvalidLockExpression()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        SyncLock InvalidReference'BIND:"SyncLock InvalidReference"

        End SyncLock
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'SyncLock In ... nd SyncLock')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'InvalidReference')
      Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'SyncLock In ... nd SyncLock')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'InvalidReference' is not declared. It may be inaccessible due to its protection level.
        SyncLock InvalidReference'BIND:"SyncLock InvalidReference"
                 ~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILockStatement_MissingEndLock()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        Dim o As New Object
        SyncLock o'BIND:"SyncLock o"

    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'SyncLock o' ... SyncLock o"')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object, IsInvalid) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'SyncLock o' ... SyncLock o"')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30675: 'SyncLock' statement must end with a matching 'End SyncLock'.
        SyncLock o'BIND:"SyncLock o"
        ~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILockStatement_ExpressionBody_ObjectCall()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        Dim o As New Object
        SyncLock o.ToString()'BIND:"SyncLock o.ToString()"
        End SyncLock
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'SyncLock o. ... nd SyncLock')
  Expression: 
    IInvocationOperation (virtual Function System.Object.ToString() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: 'o.ToString()')
      Instance Receiver: 
        ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'SyncLock o. ... nd SyncLock')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILockStatement_ExpressionLock_ClassMethodCall()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        SyncLock M2()'BIND:"SyncLock M2()"
        End SyncLock
    End Sub

    Public Function M2() As Object
        Return New Object
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'SyncLock M2 ... nd SyncLock')
  Expression: 
    IInvocationOperation ( Function C1.M2() As System.Object) (OperationKind.Invocation, Type: System.Object) (Syntax: 'M2()')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'M2')
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'SyncLock M2 ... nd SyncLock')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILockStatement_ExpressionBody_SubMethod()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        SyncLock M2()'BIND:"SyncLock M2()"
        End SyncLock
    End Sub

    Public Sub M2()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'SyncLock M2 ... nd SyncLock')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'M2()')
      Children(1):
          IInvocationOperation ( Sub C1.M2()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M2()')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'M2')
            Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'SyncLock M2 ... nd SyncLock')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30491: Expression does not produce a value.
        SyncLock M2()'BIND:"SyncLock M2()"
                 ~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILockStatement_NonEmptyBody()
            Dim source = <![CDATA[
Option Strict On
Imports System

Public Class C1
    Public Sub M1()
        Dim o As New Object
        SyncLock o'BIND:"SyncLock o"
            Console.WriteLine("Hello World!")
        End SyncLock
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'SyncLock o' ... nd SyncLock')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... lo World!")')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... lo World!")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Hello World!"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LockFlow_04()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(input As Boolean) 'BIND:"Sub M"
        SyncLock Nothing
            input = true
        End SyncLock
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [<anonymous local> As System.Boolean]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Nothing')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningNothingLiteral)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IInvocationOperation (Sub System.Threading.Monitor.Enter(obj As System.Object, ByRef lockTaken As System.Boolean)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'Nothing')
                  Instance Receiver: 
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Nothing')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: lockTaken) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Nothing')
                        ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'Nothing')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input = true')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'input = true')
                      Left: 
                        IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B5]
                ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'Nothing')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (Sub System.Threading.Monitor.Exit(obj As System.Object)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'Nothing')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Nothing')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

    End Class
End Namespace
