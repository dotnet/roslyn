' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Operations
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
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperationInternal(node), IUsingOperation)

            Assert.NotNull(op.Resources.Syntax)
            Assert.Same(node.UsingStatement, op.Resources.Syntax)
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
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperationInternal(node), IUsingOperation)

            Assert.Equal(OperationKind.Invalid, op.Resources.Kind)
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
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'Using c1 As ... End Using')
  Resources: 
    IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Using c1 As ... s C = New C')
      IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1')
        Variables: Local_1: c1 As Program.C
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
      IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c2')
        Variables: Local_1: c2 As Program.C
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
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
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'Using c1 As ... End Using')
  Resources: 
    IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Using c1 As C = New C')
      IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1')
        Variables: Local_1: c1 As Program.C
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
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
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'Using c1 As ... End Using')
  Resources: 
    IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Using c1 As New C')
      IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1')
        Variables: Local_1: c1 As Program.C
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
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
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'Using c1, c ... End Using')
  Resources: 
    IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Using c1, c2 As New C')
      IVariableDeclarationOperation (2 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1, c2 As New C')
        Variables: Local_1: c1 As Program.C
          Local_2: c2 As Program.C
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Using c1, c ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
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
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'Using c1'BI ... End Using')
  Resources: 
    ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Using c1'BI ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
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
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'Using c1, c ... End Using')
  Resources: 
    IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Using c1, c2')
      IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c1')
        Variables: Local_1: c1 As System.Object
        Initializer: 
          null
      IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c2')
        Variables: Local_1: c2 As System.Object
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Using c1, c ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: System.Object) (Syntax: 'c1')
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
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'Using c1'BI ... End Using')
  Resources: 
    ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Using c1'BI ... End Using')
      IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'Using c2 ... End Using')
        Resources: 
          ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c2')
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Using c2 ... End Using')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
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
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'Using c1 =  ... End Using')
  Resources: 
    IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Using c1 =  ... c3 As New C')
      IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1')
        Variables: Local_1: c1 As Program.C
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
      IVariableDeclarationOperation (2 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c2, c3 As New C')
        Variables: Local_1: c2 As Program.C
          Local_2: c3 As Program.C
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Using c1 =  ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
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
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'Using c1 =  ... End Using')
  Resources: 
    IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Using c1 = New C, c2')
      IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1')
        Variables: Local_1: c1 As Program.C
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
      IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c2')
        Variables: Local_1: c2 As System.Object
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Using c1 =  ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
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
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'Using c1 As ... End Using')
  Resources: 
    IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Using c1 As New C')
      IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c1')
        Variables: Local_1: c1 As Program.C
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: 'As New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C, IsInvalid) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
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
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'Using'BIND: ... End Using')
  Resources: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Using'BIND: ... End Using')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Using'BIND:"Using"
             ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_InvalidNothingResources()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main(args As String())
        Using Nothing'BIND:"Using Nothing"
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'Using Nothi ... End Using')
  Resources: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsInvalid, IsImplicit) (Syntax: 'Nothing')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'Nothing')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Using Nothi ... End Using')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36010: 'Using' operand of type 'Object' must implement 'System.IDisposable'.
        Using Nothing'BIND:"Using Nothing"
              ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_UsingWithoutSavedReference()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main(args As String())
        Using GetC()'BIND:"Using GetC()"
        End Using
    End Sub

    Function GetC() As C
        Return New C
    End Function

    Class C
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'Using GetC( ... End Using')
  Resources: 
    IInvocationOperation (Function Program.GetC() As Program.C) (OperationKind.Invocation, Type: Program.C) (Syntax: 'GetC()')
      Instance Receiver: 
        null
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: 'Using GetC( ... End Using')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_UsingBlockSyntax_UsingStatementSyntax_WithDeclarationResource()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main(args As String())
        Using c1 = New C, c2 = New C'BIND:"Using c1 = New C, c2 = New C"
        End Using
    End Sub

    Class C
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Using c1 =  ...  c2 = New C')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1')
    Variables: Local_1: c1 As Program.C
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
        IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
          Arguments(0)
          Initializer: 
            null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c2')
    Variables: Local_1: c2 As Program.C
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
        IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
          Arguments(0)
          Initializer: 
            null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_UsingBlockSyntax_UsingStatementSyntax_WithExpressionResource()
            Dim source = <compilation>
                             <file Name="a.vb">
                                 <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main(args As String())
        Dim c1 As New C
        Using c1'BIND:"Using c1"
            Console.WriteLine()
        End Using
    End Sub

    Class C
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Module]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            CompilationUtils.AssertNoDiagnostics(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single().UsingStatement

            Assert.Null(comp.GetSemanticModel(node.SyntaxTree).GetOperation(node))
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/22362")>
        Public Sub IUsingStatement_UsingStatementSyntax_VariablesSyntax()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main(args As String())
        Using c1 = New C, c2 = New C'BIND:"c1 = New C"
        End Using
    End Sub

    Class C
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Module]]>.Value

            ' This should be returning a variable declaration, but the associated variable declarator operation has a
            ' ModifiedIdentifierSyntax as the associated syntax node. Fixing is tracked by
            ' https://github.com/dotnet/roslyn/issues/22362
            Dim expectedOperationTree = <![CDATA[
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of VariableDeclaratorSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_UsingStatementSyntax_Expression()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main(args As String())
        Dim c1 As New C
        Using c1'BIND:"c1"
            Console.WriteLine()
        End Using
    End Sub

    Class C
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IUsingStatement_UsingBlockSyntax_StatementsSyntax()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main(args As String())
        Using c1 = New C, c2 = New C
            Console.WriteLine()'BIND:"Console.WriteLine()"
        End Using
    End Sub

    Class C
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
  Expression: 
    IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
      Instance Receiver: 
        null
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ExpressionStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
