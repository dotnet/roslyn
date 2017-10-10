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

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            CompilationUtils.AssertNoDiagnostics(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single()
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperationInternal(node), IUsingStatement)

            Assert.NotNull(op.Expression.Syntax)
            Assert.Same(node.UsingStatement, op.Expression.Syntax)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        <WorkItem(19887, "https://github.com/dotnet/roslyn/issues/19887")>
        Public Sub UsingDeclarationIncompleteUsingInvalidExpression()
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

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            CompilationUtils.AssertTheseDiagnostics(comp,
                                        <expected>
BC30201: Expression expected.
        Using
             ~
                                        </expected>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single()
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperationInternal(node), IUsingStatement)

            Assert.Equal(OperationKind.InvalidExpression, op.Expression.Kind)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub IUsingStatement_MultipleNewResources()
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
  Expression: 
    IVariableDeclarationStatement (0 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Using c1 As ... s C = New C')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_SingleNewResource()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Using c1 As C = New C'BIND:"Using c1 As C = New C"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement) (Syntax: 'Using c1 As ... End Using')
  Expression: 
    IVariableDeclarationStatement (0 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Using c1 As C = New C')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_SingleAsNewResource()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Using c1 As New C'BIND:"Using c1 As New C"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement) (Syntax: 'Using c1 As ... End Using')
  Expression: 
    IVariableDeclarationStatement (0 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Using c1 As New C')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_MultipleAsNewResources()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Using c1, c2 As New C'BIND:"Using c1, c2 As New C"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement) (Syntax: 'Using c1, c ... End Using')
  Expression: 
    IVariableDeclarationStatement (0 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Using c1, c2 As New C')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Using c1, c ... End Using')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_SingleExistingResource()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Dim c1 As New C
        Using c1'BIND:"Using c1"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement) (Syntax: 'Using c1'BI ... End Using')
  Expression: 
    ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Using c1'BI ... End Using')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_InvalidMultipleExistingResources()
            Dim source = <![CDATA[
Imports System

Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Dim c1, c2 As New C
        Using c1, c2'BIND:"Using c1, c2"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement, IsInvalid) (Syntax: 'Using c1, c ... End Using')
  Expression: 
    IVariableDeclarationStatement (0 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Using c1, c2')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Using c1, c ... End Using')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                  ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'c1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30616: Variable 'c1' hides a variable in an enclosing block.
        Using c1, c2'BIND:"Using c1, c2"
              ~~
BC36011: 'Using' resource variable must have an explicit initialization.
        Using c1, c2'BIND:"Using c1, c2"
              ~~
BC30616: Variable 'c2' hides a variable in an enclosing block.
        Using c1, c2'BIND:"Using c1, c2"
                  ~~
BC42104: Variable 'c1' is used before it has been assigned a value. A null reference exception could result at runtime.
            Console.WriteLine(c1)
                              ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_NestedUsing()
            Dim source = <![CDATA[
Imports System

Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Dim c1, c2 As New C
        Using c1'BIND:"Using c1"
            Using c2
                Console.WriteLine(c1)
            End Using
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement) (Syntax: 'Using c1'BI ... End Using')
  Expression: 
    ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Using c1'BI ... End Using')
      IUsingStatement (OperationKind.UsingStatement) (Syntax: 'Using c2 ... End Using')
        Expression: 
          ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c2')
        Body: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Using c2 ... End Using')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
              Expression: 
                IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_MixedAsNewAndSingleInitializer()
            Dim source = <![CDATA[
Imports System

Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Using c1 = New C, c2, c3 As New C'BIND:"Using c1 = New C, c2, c3 As New C"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement) (Syntax: 'Using c1 =  ... End Using')
  Expression: 
    IVariableDeclarationStatement (0 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Using c1 =  ... c3 As New C')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Using c1 =  ... End Using')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_InvalidNoInitializerOneVariable()
            Dim source = <![CDATA[
Imports System

Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Dim c2 As New C
        Using c1 = New C, c2'BIND:"Using c1 = New C, c2"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement, IsInvalid) (Syntax: 'Using c1 =  ... End Using')
  Expression: 
    IVariableDeclarationStatement (0 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Using c1 = New C, c2')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Using c1 =  ... End Using')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30616: Variable 'c2' hides a variable in an enclosing block.
        Using c1 = New C, c2'BIND:"Using c1 = New C, c2"
                          ~~
BC36011: 'Using' resource variable must have an explicit initialization.
        Using c1 = New C, c2'BIND:"Using c1 = New C, c2"
                          ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_InvalidNonDisposableResource()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Class C
    End Class
    Sub Main(args As String())
        Using c1 As New C'BIND:"Using c1 As New C"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement, IsInvalid) (Syntax: 'Using c1 As ... End Using')
  Expression: 
    IVariableDeclarationStatement (0 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Using c1 As New C')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36010: 'Using' operand of type 'Program.C' must implement 'System.IDisposable'.
        Using c1 As New C'BIND:"Using c1 As New C"
              ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_InvalidEmptyUsingResource()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main(args As String())
        Using'BIND:"Using"
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement, IsInvalid) (Syntax: 'Using'BIND: ... End Using')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Using'BIND: ... End Using')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Using'BIND:"Using"
             ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
