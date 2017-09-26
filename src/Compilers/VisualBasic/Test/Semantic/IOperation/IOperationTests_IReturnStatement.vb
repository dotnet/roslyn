' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub SimpleRetuenFromRegularMethod()
            Dim source = <![CDATA[
Class C
    Sub M()
        Return'BIND:"Return"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return')
  ReturnedValue: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ReturnWithValueFromRegularMethod()
            Dim source = <![CDATA[
Class C
    Function M() As Boolean
        Return True'BIND:"Return True"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return True')
  ReturnedValue: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'True')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub YieldFromIterator()
            Dim source = <![CDATA[
Class C
    Iterator Function M() As System.Collections.Generic.IEnumerable(Of Integer)
        Yield 0'BIND:"Yield 0"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnStatement (OperationKind.YieldReturnStatement) (Syntax: 'Yield 0')
  ReturnedValue: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of YieldStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ReturnFromIterator()
            Dim source = <![CDATA[
Class C
    Iterator Function M() As System.Collections.Generic.IEnumerable(Of Integer)
        Yield 0
        Return'BIND:"Return"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return')
  ReturnedValue: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub


    End Class
End Namespace
