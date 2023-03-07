' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Microsoft.CodeAnalysis.Operations

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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertNoDiagnostics(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single()
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperation(node), IUsingOperation)

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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
                                        <expected>
BC30201: Expression expected.
        Using
             ~
                                        </expected>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single()
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperation(node), IUsingOperation)

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
  Locals: Local_1: c1 As Program.C
    Local_2: c2 As Program.C
  Resources: 
    IVariableDeclarationGroupOperation (2 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Using c1 As ... s C = New C')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 As C = New C')
        Declarators:
            IVariableDeclaratorOperation (Symbol: c1 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
              Initializer: 
                null
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c2 As C = New C')
        Declarators:
            IVariableDeclaratorOperation (Symbol: c2 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2')
              Initializer: 
                null
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
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
  Locals: Local_1: c1 As Program.C
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Using c1 As C = New C')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 As C = New C')
        Declarators:
            IVariableDeclaratorOperation (Symbol: c1 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
              Initializer: 
                null
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
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
  Locals: Local_1: c1 As Program.C
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Using c1 As New C')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 As New C')
        Declarators:
            IVariableDeclaratorOperation (Symbol: c1 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
              Initializer: 
                null
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
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
  Locals: Local_1: c1 As Program.C
    Local_2: c2 As Program.C
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Using c1, c2 As New C')
      IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1, c2 As New C')
        Declarators:
            IVariableDeclaratorOperation (Symbol: c1 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
              Initializer: 
                null
            IVariableDeclaratorOperation (Symbol: c2 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2')
              Initializer: 
                null
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Using c1, c ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
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
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Using c1'BI ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
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
  Locals: Local_1: c1 As System.Object
    Local_2: c2 As System.Object
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Using c1, c2')
      IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c1, c2')
        Declarators:
            IVariableDeclaratorOperation (Symbol: c1 As System.Object) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1')
              Initializer: 
                null
            IVariableDeclaratorOperation (Symbol: c2 As System.Object) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c2')
              Initializer: 
                null
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Using c1, c ... End Using')
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
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Using c1'BI ... End Using')
      IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'Using c2 ... End Using')
        Resources: 
          ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c2')
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Using c2 ... End Using')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
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
  Locals: Local_1: c1 As Program.C
    Local_2: c2 As Program.C
    Local_3: c3 As Program.C
  Resources: 
    IVariableDeclarationGroupOperation (2 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Using c1 =  ... c3 As New C')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 = New C')
        Declarators:
            IVariableDeclaratorOperation (Symbol: c1 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
              Initializer: 
                null
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
      IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c2, c3 As New C')
        Declarators:
            IVariableDeclaratorOperation (Symbol: c2 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2')
              Initializer: 
                null
            IVariableDeclaratorOperation (Symbol: c3 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c3')
              Initializer: 
                null
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Using c1 =  ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
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
  Locals: Local_1: c1 As Program.C
    Local_2: c2 As System.Object
  Resources: 
    IVariableDeclarationGroupOperation (2 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Using c1 = New C, c2')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 = New C')
        Declarators:
            IVariableDeclaratorOperation (Symbol: c1 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
              Initializer: 
                null
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c2')
        Declarators:
            IVariableDeclaratorOperation (Symbol: c2 As System.Object) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c2')
              Initializer: 
                null
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Using c1 =  ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
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
  Locals: Local_1: c1 As Program.C
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Using c1 As New C')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c1 As New C')
        Declarators:
            IVariableDeclaratorOperation (Symbol: c1 As Program.C) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1')
              Initializer: 
                null
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: 'As New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C, IsInvalid) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
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
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Using'BIND: ... End Using')
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
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsInvalid, IsImplicit) (Syntax: 'Nothing')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'Nothing')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Using Nothi ... End Using')
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
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Using GetC( ... End Using')
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
IVariableDeclarationGroupOperation (2 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Using c1 =  ...  c2 = New C')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 = New C')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
        IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
          Arguments(0)
          Initializer: 
            null
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c2 = New C')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c2 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2')
          Initializer: 
            null
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

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertNoDiagnostics(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single().UsingStatement

            Assert.Null(comp.GetSemanticModel(node.SyntaxTree).GetOperation(node))
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22362, "https://github.com/dotnet/roslyn/issues/22362")>
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

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 = New C')
  Declarators:
      IVariableDeclaratorOperation (Symbol: c1 As Program.C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
        Initializer: 
          null
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New C')
      IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
        Arguments(0)
        Initializer: 
          null
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

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub UsingFlow_14()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Integer) 'BIND:"Sub M"
        Using c1 As C1 = New C1, s2 As S2 = New S2
            x = 1
        End Using
    End Sub
End Class

Class C1
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        Throw New NotImplementedException()
    End Sub
End Class

Structure S2
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        Throw New NotImplementedException()
    End Sub
End Structure
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [c1 As C1] [s2 As S2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'c1 As C1 = New C1')
              Left: 
                ILocalReferenceOperation: c1 (IsDeclaration: True) (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'c1')
              Right: 
                IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: S2, IsImplicit) (Syntax: 's2 As S2 = New S2')
                  Left: 
                    ILocalReferenceOperation: s2 (IsDeclaration: True) (OperationKind.LocalReference, Type: S2, IsImplicit) (Syntax: 's2')
                  Right: 
                    IObjectCreationOperation (Constructor: Sub S2..ctor()) (OperationKind.ObjectCreation, Type: S2) (Syntax: 'New S2')
                      Arguments(0)
                      Initializer: 
                        null

            Next (Regular) Block[B3]
                Entering: {R4} {R5}

        .try {R4, R5}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 1')
                          Left: 
                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Next (Regular) Block[B8]
                    Finalizing: {R6} {R7}
                    Leaving: {R5} {R4} {R3} {R2} {R1}
        }
        .finally {R6}
        {
            Block[B4] - Block
                Predecessors (0)
                Statements (1)
                    IInvocationOperation (virtual Sub System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 's2')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 's2')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (WideningValue)
                          Operand: 
                            ILocalReferenceOperation: s2 (OperationKind.LocalReference, Type: S2, IsImplicit) (Syntax: 's2')
                      Arguments(0)

                Next (StructuredExceptionHandling) Block[null]
        }
    }
    .finally {R7}
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                  Operand: 
                    ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'c1')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IInvocationOperation (virtual Sub System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'c1')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'c1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C1, IsImplicit) (Syntax: 'c1')
                  Arguments(0)

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B5] [B6]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B8] - Exit
    Predecessors: [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub UsingFlow_15()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        Using Nothing
        End Using
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
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'Nothing')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')

            Next (Regular) Block[B4]
        Block[B4] - Block [UnReachable]
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (virtual Sub System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'Nothing')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, Constant: null, IsImplicit) (Syntax: 'Nothing')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                  Arguments(0)

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

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub UsingFlow_NotDisposable()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        Using New D()
        End Using
    End Sub
End Class

Public Class D
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36010: 'Using' operand of type 'D' must implement 'System.IDisposable'.
        Using New D()
              ~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'New D()')
              Value: 
                IObjectCreationOperation (Constructor: Sub D..ctor()) (OperationKind.ObjectCreation, Type: D, IsInvalid) (Syntax: 'New D()')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'New D()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: D, IsInvalid, IsImplicit) (Syntax: 'New D()')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (virtual Sub System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'New D()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'New D()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: D, IsInvalid, IsImplicit) (Syntax: 'New D()')
                  Arguments(0)

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

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub UsingFlow_Missing_IDisposable()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        Using New D()
        End Using
    End Sub
End Class

Public Class D 
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
]]>.Value

            Dim compilation = CreateCompilation(source)
            compilation.MakeMemberMissing(SpecialMember.System_IDisposable__Dispose)

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New D()')
              Value: 
                IObjectCreationOperation (Constructor: Sub D..ctor()) (OperationKind.ObjectCreation, Type: D) (Syntax: 'New D()')
                  Arguments(0)
                  Initializer: 
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'New D()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: D, IsImplicit) (Syntax: 'New D()')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'New D()')
                  Children(1):
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'New D()')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                          (WideningReference)
                        Operand: 
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: D, IsImplicit) (Syntax: 'New D()')
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

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(compilation, expectedFlowGraph, expectedDiagnostics)
        End Sub

    End Class
End Namespace
