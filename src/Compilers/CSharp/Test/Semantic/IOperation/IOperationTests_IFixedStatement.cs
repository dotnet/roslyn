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
IOperation:  (OperationKind.None) (Syntax: 'fixed(int * ... }')
  Children(2):
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int *p = &i')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'p = &i')
          Variables: Local_1: System.Int32* p
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= &i')
              IOperation:  (OperationKind.None) (Syntax: '&i')
                Children(1):
                    IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*) (Syntax: '&i')
                      Reference: 
                        IFieldReferenceExpression: System.Int32 C.i (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'i')
                          Instance Receiver: 
                            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit) (Syntax: 'i')
      IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ...  is {*p}"");')
          Expression: 
            IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... P is {*p}"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '$""P is {*p}""')
                    IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""P is {*p}""')
                      Parts(2):
                          IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'P is ')
                            Text: 
                              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""P is "") (Syntax: 'P is ')
                          IInterpolation (OperationKind.Interpolation) (Syntax: '{*p}')
                            Expression: 
                              IOperation:  (OperationKind.None) (Syntax: '*p')
                                Children(1):
                                    ILocalReferenceExpression: p (OperationKind.LocalReferenceExpression, Type: System.Int32*) (Syntax: 'p')
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
IOperation:  (OperationKind.None) (Syntax: 'fixed (int* ... }')
  Children(2):
      IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int* p1 = &i1, p2 = &i2')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'p1 = &i1')
          Variables: Local_1: System.Int32* p1
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= &i1')
              IOperation:  (OperationKind.None) (Syntax: '&i1')
                Children(1):
                    IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*) (Syntax: '&i1')
                      Reference: 
                        IFieldReferenceExpression: System.Int32 C.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'i1')
                          Instance Receiver: 
                            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit) (Syntax: 'i1')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'p2 = &i2')
          Variables: Local_1: System.Int32* p2
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= &i2')
              IOperation:  (OperationKind.None) (Syntax: '&i2')
                Children(1):
                    IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*) (Syntax: '&i2')
                      Reference: 
                        IFieldReferenceExpression: System.Int32 C.i2 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'i2')
                          Instance Receiver: 
                            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit) (Syntax: 'i2')
      IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i3 = *p1 + *p2;')
          Expression: 
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i3 = *p1 + *p2')
              Left: 
                ILocalReferenceExpression: i3 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i3')
              Right: 
                IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '*p1 + *p2')
                  Left: 
                    IOperation:  (OperationKind.None) (Syntax: '*p1')
                      Children(1):
                          ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: System.Int32*) (Syntax: 'p1')
                  Right: 
                    IOperation:  (OperationKind.None) (Syntax: '*p2')
                      Children(1):
                          ILocalReferenceExpression: p2 (OperationKind.LocalReferenceExpression, Type: System.Int32*) (Syntax: 'p2')
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
IOperation:  (OperationKind.None) (Syntax: 'fixed (int* ... }')
  Children(2):
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int* p1 = &i1')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'p1 = &i1')
          Variables: Local_1: System.Int32* p1
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= &i1')
              IOperation:  (OperationKind.None) (Syntax: '&i1')
                Children(1):
                    IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*) (Syntax: '&i1')
                      Reference: 
                        IFieldReferenceExpression: System.Int32 C.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'i1')
                          Instance Receiver: 
                            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit) (Syntax: 'i1')
      IOperation:  (OperationKind.None) (Syntax: 'fixed (int* ... }')
        Children(2):
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int* p2 = &i2')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'p2 = &i2')
                Variables: Local_1: System.Int32* p2
                Initializer: 
                  IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= &i2')
                    IOperation:  (OperationKind.None) (Syntax: '&i2')
                      Children(1):
                          IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*) (Syntax: '&i2')
                            Reference: 
                              IFieldReferenceExpression: System.Int32 C.i2 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'i2')
                                Instance Receiver: 
                                  IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit) (Syntax: 'i2')
            IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i3 = *p1 + *p2;')
                Expression: 
                  ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i3 = *p1 + *p2')
                    Left: 
                      ILocalReferenceExpression: i3 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i3')
                    Right: 
                      IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '*p1 + *p2')
                        Left: 
                          IOperation:  (OperationKind.None) (Syntax: '*p1')
                            Children(1):
                                ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: System.Int32*) (Syntax: 'p1')
                        Right: 
                          IOperation:  (OperationKind.None) (Syntax: '*p2')
                            Children(1):
                                ILocalReferenceExpression: p2 (OperationKind.LocalReferenceExpression, Type: System.Int32*) (Syntax: 'p2')
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
IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'fixed (int* ... }')
  Children(2):
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int* p1 =')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'p1 =')
          Variables: Local_1: System.Int32* p1
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '=')
              IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
                Children(0)
      IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i3 = *p1;')
          Expression: 
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i3 = *p1')
              Left: 
                ILocalReferenceExpression: i3 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i3')
              Right: 
                IOperation:  (OperationKind.None) (Syntax: '*p1')
                  Children(1):
                      ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: System.Int32*) (Syntax: 'p1')
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
IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'fixed (int* ... }')
  Children(2):
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int* p1 = &i1')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'p1 = &i1')
          Variables: Local_1: System.Int32* p1
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= &i1')
              IOperation:  (OperationKind.None) (Syntax: '&i1')
                Children(1):
                    IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*) (Syntax: '&i1')
                      Reference: 
                        IFieldReferenceExpression: System.Int32 C.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'i1')
                          Instance Receiver: 
                            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit) (Syntax: 'i1')
      IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'i3 = &p1;')
          Expression: 
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'i3 = &p1')
              Left: 
                ILocalReferenceExpression: i3 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i3')
              Right: 
                IConversionExpression (TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '&p1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32**, IsInvalid) (Syntax: '&p1')
                      Reference: 
                        ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: System.Int32*, IsInvalid) (Syntax: 'p1')
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
