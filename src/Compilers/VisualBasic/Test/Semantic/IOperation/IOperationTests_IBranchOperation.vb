' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IBranchStatement_ExitNestedLoop()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main() 'BIND:"Private Shared Sub Main()"
        Dim x As Boolean = false
        While True
            Do While True
                If x Then
                    Exit Do
                Else
                    Exit While
                End If
            Loop
        End While

        Exit Sub
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (5 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: 'Private Sha ... End Sub')
  Locals: Local_1: x As System.Boolean
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x As Boolean = false')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x As Boolean = false')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x As System.Boolean) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= false')
          ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
  IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'While True ... End While')
    Condition: 
      ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
    Body: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'While True ... End While')
        IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'Do While Tr ... Loop')
          Condition: 
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
          Body: 
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Do While Tr ... Loop')
              IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If x Then ... End If')
                Condition: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')
                WhenTrue: 
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If x Then ... End If')
                    IBranchOperation (BranchKind.Break, Label Id: 3) (OperationKind.Branch, Type: null) (Syntax: 'Exit Do')
                WhenFalse: 
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else ... Exit While')
                    IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'Exit While')
          IgnoredCondition: 
            null
    IgnoredCondition: 
      null
  IBranchOperation (BranchKind.Break, Label: exit) (OperationKind.Branch, Type: null) (Syntax: 'Exit Sub')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, "")
        End Sub
    End Class
End Namespace
