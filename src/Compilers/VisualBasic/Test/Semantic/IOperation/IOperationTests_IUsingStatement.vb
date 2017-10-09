' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        <WorkItem(19819, "https://github.com/dotnet/roslyn/issues/19819")>
        Public Sub UsingDeclarationSyntaxNotNull()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Imports System
Module Module1
    Class C1
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub S1()
        Using D1 as New C1()
        End Using
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source)
            CompilationUtils.AssertNoDiagnostics(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single()
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperation(node), IUsingStatement)

            Assert.NotNull(op.Declaration.Syntax)
            Assert.Same(node.UsingStatement, op.Declaration.Syntax)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        <WorkItem(19887, "https://github.com/dotnet/roslyn/issues/19887")>
        Public Sub UsingDeclarationIncompleteUsingNullDeclaration()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Imports System
Module Module1
    Class C1
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub S1()
        Using
        End Using
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
                                        <expected>
BC30201: Expression expected.
        Using
             ~
                                        </expected>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single()
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperation(node), IUsingStatement)

            Assert.Null(op.Declaration)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        <WorkItem(19887, "https://github.com/dotnet/roslyn/issues/19887")>
        Public Sub UsingDeclarationExistingVariableNullDeclaration()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Imports System
Module Module1
    Class C1
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub S1()
        Dim x = New C1()
        Using x
        End Using
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source)
            CompilationUtils.AssertNoDiagnostics(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single()
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperation(node), IUsingStatement)

            Assert.Null(op.Declaration)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestUsingBlock()
            Dim source = <![CDATA[
Imports System

Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Using c1 As C = New C, c2 As C = New C'BIND:"Using c1 As C = New C, c2 As C = New C"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement) (Syntax: 'Using c1 As ... End Using')
  Declaration: 
    IVariableDeclarationStatement (0 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Using c1 As ... s C = New C')
  Value: 
    null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
