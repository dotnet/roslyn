// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        public void FixedStatement_FixedClassVariableAndPrint()
        {
            string source = @"
using System;

class C
{
    private int i;

    void M1()
    {
        unsafe
        {
            /*<bind>*/fixed(int *p = &i)
            {
                Console.WriteLine($""P is {*p}"");
            }/*</bind>*/
        }
    }
}
";
            string expectedOperationTree = @"
IFixedOperation (OperationKind.None, Type: null) (Syntax: 'fixed(int * ... }')
  Locals: Local_1: System.Int32* p
  Declaration: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int *p = &i')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int *p = &i')
        Declarators:
            IVariableDeclaratorOperation (Symbol: System.Int32* p) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p = &i')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &i')
                  IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&i')
                    Children(1):
                        IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i')
                          Reference: 
                            IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i')
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ...  is {*p}"");')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... P is {*p}"")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""P is {*p}""')
                  IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""P is {*p}""')
                    Parts(2):
                        IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'P is ')
                          Text: 
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""P is "", IsImplicit) (Syntax: 'P is ')
                        IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{*p}')
                          Expression: 
                            IOperation:  (OperationKind.None, Type: null) (Syntax: '*p')
                              Children(1):
                                  ILocalReferenceOperation: p (OperationKind.LocalReference, Type: System.Int32*) (Syntax: 'p')
                          Alignment: 
                            null
                          FormatString: 
                            null
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<FixedStatementSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        public void FixedStatement_MultipleDeclarators()
        {
            string source = @"
using System;

class C
{
    private int i1;
    private int i2;

    void M1()
    {
        int i3;
        unsafe
        {
            /*<bind>*/fixed (int* p1 = &i1, p2 = &i2)
            {
                i3 = *p1 + *p2;
            }/*</bind>*/
        }
    }
}
";
            string expectedOperationTree = @"
IFixedOperation (OperationKind.None, Type: null) (Syntax: 'fixed (int* ... }')
  Locals: Local_1: System.Int32* p1
    Local_2: System.Int32* p2
  Declaration: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int* p1 = &i1, p2 = &i2')
      IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int* p1 = &i1, p2 = &i2')
        Declarators:
            IVariableDeclaratorOperation (Symbol: System.Int32* p1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p1 = &i1')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &i1')
                  IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&i1')
                    Children(1):
                        IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i1')
                          Reference: 
                            IFieldReferenceOperation: System.Int32 C.i1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i1')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i1')
            IVariableDeclaratorOperation (Symbol: System.Int32* p2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p2 = &i2')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &i2')
                  IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&i2')
                    Children(1):
                        IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i2')
                          Reference: 
                            IFieldReferenceOperation: System.Int32 C.i2 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i2')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i2')
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i3 = *p1 + *p2;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i3 = *p1 + *p2')
            Left: 
              ILocalReferenceOperation: i3 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i3')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32) (Syntax: '*p1 + *p2')
                Left: 
                  IOperation:  (OperationKind.None, Type: null) (Syntax: '*p1')
                    Children(1):
                        ILocalReferenceOperation: p1 (OperationKind.LocalReference, Type: System.Int32*) (Syntax: 'p1')
                Right: 
                  IOperation:  (OperationKind.None, Type: null) (Syntax: '*p2')
                    Children(1):
                        ILocalReferenceOperation: p2 (OperationKind.LocalReference, Type: System.Int32*) (Syntax: 'p2')
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<FixedStatementSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeDebugDll);
        }


        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        public void FixedStatement_MultipleFixedStatements()
        {
            string source = @"
using System;

class C
{
    private int i1;
    private int i2;

    void M1()
    {
        int i3;
        unsafe
        {
            /*<bind>*/fixed (int* p1 = &i1)
            fixed (int* p2 = &i2)
            {
                i3 = *p1 + *p2;
            }/*</bind>*/
        }
    }
}
";
            string expectedOperationTree = @"
IFixedOperation (OperationKind.None, Type: null) (Syntax: 'fixed (int* ... }')
  Locals: Local_1: System.Int32* p1
  Declaration: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int* p1 = &i1')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int* p1 = &i1')
        Declarators:
            IVariableDeclaratorOperation (Symbol: System.Int32* p1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p1 = &i1')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &i1')
                  IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&i1')
                    Children(1):
                        IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i1')
                          Reference: 
                            IFieldReferenceOperation: System.Int32 C.i1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i1')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i1')
        Initializer: 
          null
  Body: 
    IFixedOperation (OperationKind.None, Type: null) (Syntax: 'fixed (int* ... }')
      Locals: Local_1: System.Int32* p2
      Declaration: 
        IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int* p2 = &i2')
          IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int* p2 = &i2')
            Declarators:
                IVariableDeclaratorOperation (Symbol: System.Int32* p2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p2 = &i2')
                  Initializer: 
                    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &i2')
                      IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&i2')
                        Children(1):
                            IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i2')
                              Reference: 
                                IFieldReferenceOperation: System.Int32 C.i2 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i2')
                                  Instance Receiver: 
                                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i2')
            Initializer: 
              null
      Body: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i3 = *p1 + *p2;')
            Expression: 
              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i3 = *p1 + *p2')
                Left: 
                  ILocalReferenceOperation: i3 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i3')
                Right: 
                  IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32) (Syntax: '*p1 + *p2')
                    Left: 
                      IOperation:  (OperationKind.None, Type: null) (Syntax: '*p1')
                        Children(1):
                            ILocalReferenceOperation: p1 (OperationKind.LocalReference, Type: System.Int32*) (Syntax: 'p1')
                    Right: 
                      IOperation:  (OperationKind.None, Type: null) (Syntax: '*p2')
                        Children(1):
                            ILocalReferenceOperation: p2 (OperationKind.LocalReference, Type: System.Int32*) (Syntax: 'p2')
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<FixedStatementSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        public void FixedStatement_InvalidVariable()
        {
            string source = @"
using System;

class C
{
    void M1()
    {
        int i3;
        unsafe
        {
            /*<bind>*/fixed (int* p1 =)
            {
                i3 = *p1;
            }/*</bind>*/
        }
    }
}
";
            string expectedOperationTree = @"
IFixedOperation (OperationKind.None, Type: null, IsInvalid) (Syntax: 'fixed (int* ... }')
  Locals: Local_1: System.Int32* p1
  Declaration: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid, IsImplicit) (Syntax: 'int* p1 =')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int* p1 =')
        Declarators:
            IVariableDeclaratorOperation (Symbol: System.Int32* p1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p1 =')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
                  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                    Children(0)
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i3 = *p1;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i3 = *p1')
            Left: 
              ILocalReferenceOperation: i3 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i3')
            Right: 
              IOperation:  (OperationKind.None, Type: null) (Syntax: '*p1')
                Children(1):
                    ILocalReferenceOperation: p1 (OperationKind.LocalReference, Type: System.Int32*) (Syntax: 'p1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //             /*<bind>*/fixed (int* p1 =)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(11, 39),
            };

            VerifyOperationTreeAndDiagnosticsForTest<FixedStatementSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.IOperation)]
        public void FixedStatement_InvalidBody()
        {
            string source = @"
using System;

class C
{
    private int i1;

    void M1()
    {
        int i3;
        unsafe
        {
            /*<bind>*/fixed (int* p1 = &i1)
            {
                i3 = &p1;
            }/*</bind>*/
        }
    }
}
";
            string expectedOperationTree = @"
IFixedOperation (OperationKind.None, Type: null, IsInvalid) (Syntax: 'fixed (int* ... }')
  Locals: Local_1: System.Int32* p1
  Declaration: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int* p1 = &i1')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int* p1 = &i1')
        Declarators:
            IVariableDeclaratorOperation (Symbol: System.Int32* p1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p1 = &i1')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &i1')
                  IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&i1')
                    Children(1):
                        IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i1')
                          Reference: 
                            IFieldReferenceOperation: System.Int32 C.i1 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i1')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i1')
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'i3 = &p1;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'i3 = &p1')
            Left: 
              ILocalReferenceOperation: i3 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i3')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '&p1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32**, IsInvalid) (Syntax: '&p1')
                    Reference: 
                      ILocalReferenceOperation: p1 (OperationKind.LocalReference, Type: System.Int32*, IsInvalid) (Syntax: 'p1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(15,22): error CS0266: Cannot implicitly convert type 'int**' to 'int'. An explicit conversion exists (are you missing a cast?)
                //                 i3 = &p1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "&p1").WithArguments("int**", "int").WithLocation(15, 22)
            };

            VerifyOperationTreeAndDiagnosticsForTest<FixedStatementSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void FixedStatement_01()
        {
            string source = @"
unsafe public class MyClass
{
    int i;
    unsafe void M(bool b)
    /*<bind>*/{
        fixed (int* p = &i)
        {
            System.Console.WriteLine($""P is {p}"");
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int*' to 'object'
                //             System.Console.WriteLine($"P is {p}");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "p").WithArguments("int*", "object").WithLocation(9, 46)
            };

            // https://github.com/dotnet/roslyn/issues/27491: This graph verification was added to verify general handling of operations
            //                      with OperationKind.None. We have special handling for fixed statements now.
            //                      Need to make sure we haven't lost coverage for the general implementation and
            //                      add new tests if necessary.
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32* p]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32*, IsImplicit) (Syntax: 'p = &i')
              Left: 
                ILocalReferenceOperation: p (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32*, IsImplicit) (Syntax: 'p = &i')
              Right: 
                IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&i')
                  Children(1):
                      IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i')
                        Reference: 
                          IFieldReferenceOperation: System.Int32 MyClass.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
                            Instance Receiver: 
                              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MyClass, IsImplicit) (Syntax: 'i')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'System.Cons ... P is {p}"");')
              Expression: 
                IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'System.Cons ... ""P is {p}"")')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '$""P is {p}""')
                        IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""P is {p}""')
                          Parts(2):
                              IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'P is ')
                                Text: 
                                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""P is "", IsImplicit) (Syntax: 'P is ')
                              IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{p}')
                                Expression: 
                                  ILocalReferenceOperation: p (OperationKind.LocalReference, Type: System.Int32*, IsInvalid) (Syntax: 'p')
                                Alignment: 
                                  null
                                FormatString: 
                                  null
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void FixedStatement_02()
        {
            string source = @"
unsafe public class MyClass
{
    int i;
    unsafe void M(bool b)
    /*<bind>*/{
        fixed (int* p = &i)
        {
            if (b)
            {
                System.Console.WriteLine($""P is {p}"");
            }
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int*' to 'object'
                //                 System.Console.WriteLine($"P is {p}");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "p").WithArguments("int*", "object").WithLocation(11, 50)
            };

            // https://github.com/dotnet/roslyn/issues/27491: This graph verification was added to verify general handling of operations
            //                      with OperationKind.None. We have special handling for fixed statements now.
            //                      Need to make sure we haven't lost coverage for the general implementation and
            //                      add new tests if necessary.
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32* p]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32*, IsImplicit) (Syntax: 'p = &i')
              Left: 
                ILocalReferenceOperation: p (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32*, IsImplicit) (Syntax: 'p = &i')
              Right: 
                IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&i')
                  Children(1):
                      IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i')
                        Reference: 
                          IFieldReferenceOperation: System.Int32 MyClass.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
                            Instance Receiver: 
                              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MyClass, IsImplicit) (Syntax: 'i')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'System.Cons ... P is {p}"");')
              Expression: 
                IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'System.Cons ... ""P is {p}"")')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '$""P is {p}""')
                        IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""P is {p}""')
                          Parts(2):
                              IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'P is ')
                                Text: 
                                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""P is "", IsImplicit) (Syntax: 'P is ')
                              IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{p}')
                                Expression: 
                                  ILocalReferenceOperation: p (OperationKind.LocalReference, Type: System.Int32*, IsInvalid) (Syntax: 'p')
                                Alignment: 
                                  null
                                FormatString: 
                                  null
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void FixedStatement_04()
        {
            string source = @"
unsafe public class MyClass
{
    int i1, i2;
    unsafe void M(bool b)
    /*<bind>*/{
        fixed (int* p = b ? &i1 : &i2)
        {
            if (b)
            {
                System.Console.WriteLine($""P is {*p}"");
            }
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         fixed (int* p = b ? &i1 : &i2)
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&i1").WithLocation(7, 29),
                // CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         fixed (int* p = b ? &i1 : &i2)
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&i2").WithLocation(7, 35)
            };

            // https://github.com/dotnet/roslyn/issues/27491: This graph verification was added to verify general handling of operations
            //                      with OperationKind.None. We have special handling for fixed statements now.
            //                      Need to make sure we haven't lost coverage for the general implementation and
            //                      add new tests if necessary.
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [System.Int32* p]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Jump if False (Regular) to Block[B3]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '&i1')
                  Value: 
                    IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*, IsInvalid) (Syntax: '&i1')
                      Reference: 
                        IFieldReferenceOperation: System.Int32 MyClass.i1 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MyClass, IsInvalid, IsImplicit) (Syntax: 'i1')

            Next (Regular) Block[B4]
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '&i2')
                  Value: 
                    IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*, IsInvalid) (Syntax: '&i2')
                      Reference: 
                        IFieldReferenceOperation: System.Int32 MyClass.i2 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'i2')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MyClass, IsInvalid, IsImplicit) (Syntax: 'i2')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32*, IsInvalid, IsImplicit) (Syntax: 'p = b ? &i1 : &i2')
                  Left: 
                    ILocalReferenceOperation: p (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32*, IsInvalid, IsImplicit) (Syntax: 'p = b ? &i1 : &i2')
                  Right: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32*, IsInvalid, IsImplicit) (Syntax: 'b ? &i1 : &i2')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4]
        Statements (0)
        Jump if False (Regular) to Block[B7]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
            Leaving: {R1}

        Next (Regular) Block[B6]
    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ...  is {*p}"");')
              Expression: 
                IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... P is {*p}"")')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""P is {*p}""')
                        IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""P is {*p}""')
                          Parts(2):
                              IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'P is ')
                                Text: 
                                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""P is "", IsImplicit) (Syntax: 'P is ')
                              IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{*p}')
                                Expression: 
                                  IOperation:  (OperationKind.None, Type: null) (Syntax: '*p')
                                    Children(1):
                                        ILocalReferenceOperation: p (OperationKind.LocalReference, Type: System.Int32*) (Syntax: 'p')
                                Alignment: 
                                  null
                                FormatString: 
                                  null
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void FixedFlow_01()
        {
            string source = @"
class P
{
    void M(object x)
/*<bind>*/{
        unsafe
        {
            fixed(int *p = &i)
            {
                x?.ToString();
            }
        }
    }/*</bind>*/

    private int i;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32* p]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32*, IsImplicit) (Syntax: 'p = &i')
              Left: 
                ILocalReferenceOperation: p (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32*, IsImplicit) (Syntax: 'p = &i')
              Right: 
                IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&i')
                  Children(1):
                      IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i')
                        Reference: 
                          IFieldReferenceOperation: System.Int32 P.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
                            Instance Receiver: 
                              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'i')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [0]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'x')
                Leaving: {R2} {R1}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x?.ToString();')
                  Expression: 
                    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'x')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics, compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void FixedFlow_02()
        {
            string source = @"
class P
{
    void M(bool x, P input)
/*<bind>*/{
        unsafe
        {
            fixed(int *p1 = &i, p2 = &(input ?? this).j)
            {
                x = true;
            }
        }
    }/*</bind>*/

    private int i;
    private int j;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32* p1] [System.Int32* p2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32*, IsImplicit) (Syntax: 'p1 = &i')
              Left: 
                ILocalReferenceOperation: p1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32*, IsImplicit) (Syntax: 'p1 = &i')
              Right: 
                IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&i')
                  Children(1):
                      IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i')
                        Reference: 
                          IFieldReferenceOperation: System.Int32 P.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
                            Instance Receiver: 
                              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'i')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [1]
        .locals {R3}
        {
            CaptureIds: [0]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                      Value: 
                        IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: P) (Syntax: 'input')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'input')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                      Value: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'input')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
                  Value: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P) (Syntax: 'this')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32*, IsImplicit) (Syntax: 'p2 = &(input ?? this).j')
                  Left: 
                    ILocalReferenceOperation: p2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32*, IsImplicit) (Syntax: 'p2 = &(input ?? this).j')
                  Right: 
                    IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&(input ?? this).j')
                      Children(1):
                          IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&(input ?? this).j')
                            Reference: 
                              IFieldReferenceOperation: System.Int32 P.j (OperationKind.FieldReference, Type: System.Int32) (Syntax: '(input ?? this).j')
                                Instance Receiver: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'input ?? this')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'x = true')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics, compilationOptions: TestOptions.UnsafeDebugDll);
        }

    }
}
