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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
  Expression: 
    IFieldReferenceOperation: C1.o As System.Object (OperationKind.FieldReference, IsExpression, Type: System.Object) (Syntax: 'o')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: C1, IsImplicit) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, IsExpression, Type: System.Object) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, IsExpression, Type: System.Object) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null, IsInvalid) (Syntax: 'SyncLock i' ... nd SyncLock')
  Expression: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null, IsInvalid) (Syntax: 'SyncLock i' ... nd SyncLock')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null, IsInvalid) (Syntax: 'SyncLock'BI ... nd SyncLock')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, IsExpression, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null, IsInvalid) (Syntax: 'SyncLock'BI ... nd SyncLock')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null, IsInvalid) (Syntax: 'SyncLock In ... nd SyncLock')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, IsExpression, Type: ?, IsInvalid) (Syntax: 'InvalidReference')
      Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null, IsInvalid) (Syntax: 'SyncLock In ... nd SyncLock')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null, IsInvalid) (Syntax: 'SyncLock o' ... SyncLock o"')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, IsExpression, Type: System.Object, IsInvalid) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null, IsInvalid) (Syntax: 'SyncLock o' ... SyncLock o"')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'SyncLock o. ... nd SyncLock')
  Expression: 
    IInvocationOperation (virtual Function System.Object.ToString() As System.String) (OperationKind.Invocation, IsExpression, Type: System.String) (Syntax: 'o.ToString()')
      Instance Receiver: 
        ILocalReferenceOperation: o (OperationKind.LocalReference, IsExpression, Type: System.Object) (Syntax: 'o')
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: 'SyncLock o. ... nd SyncLock')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'SyncLock M2 ... nd SyncLock')
  Expression: 
    IInvocationOperation ( Function C1.M2() As System.Object) (OperationKind.Invocation, IsExpression, Type: System.Object) (Syntax: 'M2()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: C1, IsImplicit) (Syntax: 'M2')
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: 'SyncLock M2 ... nd SyncLock')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null, IsInvalid) (Syntax: 'SyncLock M2 ... nd SyncLock')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, IsExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'M2()')
      Children(1):
          IInvocationOperation ( Sub C1.M2()) (OperationKind.Invocation, IsExpression, Type: System.Void, IsInvalid) (Syntax: 'M2()')
            Instance Receiver: 
              IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: C1, IsInvalid, IsImplicit) (Syntax: 'M2')
            Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null, IsInvalid) (Syntax: 'SyncLock M2 ... nd SyncLock')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, IsExpression, Type: System.Object) (Syntax: 'o')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: 'SyncLock o' ... nd SyncLock')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'Console.Wri ... lo World!")')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'Console.Wri ... lo World!")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Hello World!"')
                  ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SyncLockBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
