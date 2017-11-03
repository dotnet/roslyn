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
IOperation:  (OperationKind.None, Type: null) (Syntax: 'fixed(int * ... }')
  Children(2):
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
                                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i')
                IgnoredArguments(0)
          Initializer: 
            null
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ...  is {*p}"");')
          Expression: 
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... P is {*p}"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: '$""P is {*p}""')
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
IOperation:  (OperationKind.None, Type: null) (Syntax: 'fixed (int* ... }')
  Children(2):
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
                                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i1')
                IgnoredArguments(0)
              IVariableDeclaratorOperation (Symbol: System.Int32* p2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'p2 = &i2')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= &i2')
                    IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: '&i2')
                      Children(1):
                          IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i2')
                            Reference: 
                              IFieldReferenceOperation: System.Int32 C.i2 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i2')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i2')
                IgnoredArguments(0)
          Initializer: 
            null
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i3 = *p1 + *p2;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i3 = *p1 + *p2')
              Left: 
                ILocalReferenceOperation: i3 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i3')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '*p1 + *p2')
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
IOperation:  (OperationKind.None, Type: null) (Syntax: 'fixed (int* ... }')
  Children(2):
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
                                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i1')
                IgnoredArguments(0)
          Initializer: 
            null
      IOperation:  (OperationKind.None, Type: null) (Syntax: 'fixed (int* ... }')
        Children(2):
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
                                        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i2')
                      IgnoredArguments(0)
                Initializer: 
                  null
            IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i3 = *p1 + *p2;')
                Expression: 
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i3 = *p1 + *p2')
                    Left: 
                      ILocalReferenceOperation: i3 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i3')
                    Right: 
                      IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '*p1 + *p2')
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
IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'fixed (int* ... }')
  Children(2):
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid, IsImplicit) (Syntax: 'int* p1 =')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int* p1 =')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32* p1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'p1 =')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
                    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                      Children(0)
                IgnoredArguments(0)
          Initializer: 
            null
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
IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'fixed (int* ... }')
  Children(2):
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
                                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i1')
                IgnoredArguments(0)
          Initializer: 
            null
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
    }
}
