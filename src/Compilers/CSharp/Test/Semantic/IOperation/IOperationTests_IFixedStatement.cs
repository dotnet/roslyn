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
IOperation:  (OperationKind.None, Language: C#) (Syntax: 'fixed(int * ... }')
  Children(2):
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, Language: C#) (Syntax: 'int *p = &i')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: C#) (Syntax: 'p = &i')
          Variables: Local_1: System.Int32* p
          Initializer: IOperation:  (OperationKind.None, Language: C#) (Syntax: '&i')
              Children(1):
                  IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*, Language: C#) (Syntax: '&i')
                    Reference: IFieldReferenceExpression: System.Int32 C.i (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i')
                        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: C#) (Syntax: 'i')
      IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.Wri ...  is {*p}"");')
          Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.Wri ... P is {*p}"")')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: '$""P is {*p}""')
                    IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String, Language: C#) (Syntax: '$""P is {*p}""')
                      Parts(2):
                          IInterpolatedStringText (OperationKind.InterpolatedStringText, Language: C#) (Syntax: 'P is ')
                            Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""P is "", Language: C#) (Syntax: 'P is ')
                          IInterpolation (OperationKind.Interpolation, Language: C#) (Syntax: '{*p}')
                            Expression: IOperation:  (OperationKind.None, Language: C#) (Syntax: '*p')
                                Children(1):
                                    ILocalReferenceExpression: p (OperationKind.LocalReferenceExpression, Type: System.Int32*, Language: C#) (Syntax: 'p')
                            Alignment: null
                            FormatString: null
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
IOperation:  (OperationKind.None, Language: C#) (Syntax: 'fixed (int* ... }')
  Children(2):
      IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement, Language: C#) (Syntax: 'int* p1 = &i1, p2 = &i2')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: C#) (Syntax: 'p1 = &i1')
          Variables: Local_1: System.Int32* p1
          Initializer: IOperation:  (OperationKind.None, Language: C#) (Syntax: '&i1')
              Children(1):
                  IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*, Language: C#) (Syntax: '&i1')
                    Reference: IFieldReferenceExpression: System.Int32 C.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i1')
                        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: C#) (Syntax: 'i1')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: C#) (Syntax: 'p2 = &i2')
          Variables: Local_1: System.Int32* p2
          Initializer: IOperation:  (OperationKind.None, Language: C#) (Syntax: '&i2')
              Children(1):
                  IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*, Language: C#) (Syntax: '&i2')
                    Reference: IFieldReferenceExpression: System.Int32 C.i2 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i2')
                        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: C#) (Syntax: 'i2')
      IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'i3 = *p1 + *p2;')
          Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: C#) (Syntax: 'i3 = *p1 + *p2')
              Left: ILocalReferenceExpression: i3 (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i3')
              Right: IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Language: C#) (Syntax: '*p1 + *p2')
                  Left: IOperation:  (OperationKind.None, Language: C#) (Syntax: '*p1')
                      Children(1):
                          ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: System.Int32*, Language: C#) (Syntax: 'p1')
                  Right: IOperation:  (OperationKind.None, Language: C#) (Syntax: '*p2')
                      Children(1):
                          ILocalReferenceExpression: p2 (OperationKind.LocalReferenceExpression, Type: System.Int32*, Language: C#) (Syntax: 'p2')
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
IOperation:  (OperationKind.None, Language: C#) (Syntax: 'fixed (int* ... }')
  Children(2):
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, Language: C#) (Syntax: 'int* p1 = &i1')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: C#) (Syntax: 'p1 = &i1')
          Variables: Local_1: System.Int32* p1
          Initializer: IOperation:  (OperationKind.None, Language: C#) (Syntax: '&i1')
              Children(1):
                  IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*, Language: C#) (Syntax: '&i1')
                    Reference: IFieldReferenceExpression: System.Int32 C.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i1')
                        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: C#) (Syntax: 'i1')
      IOperation:  (OperationKind.None, Language: C#) (Syntax: 'fixed (int* ... }')
        Children(2):
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, Language: C#) (Syntax: 'int* p2 = &i2')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: C#) (Syntax: 'p2 = &i2')
                Variables: Local_1: System.Int32* p2
                Initializer: IOperation:  (OperationKind.None, Language: C#) (Syntax: '&i2')
                    Children(1):
                        IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*, Language: C#) (Syntax: '&i2')
                          Reference: IFieldReferenceExpression: System.Int32 C.i2 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i2')
                              Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: C#) (Syntax: 'i2')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
              IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'i3 = *p1 + *p2;')
                Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: C#) (Syntax: 'i3 = *p1 + *p2')
                    Left: ILocalReferenceExpression: i3 (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i3')
                    Right: IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Language: C#) (Syntax: '*p1 + *p2')
                        Left: IOperation:  (OperationKind.None, Language: C#) (Syntax: '*p1')
                            Children(1):
                                ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: System.Int32*, Language: C#) (Syntax: 'p1')
                        Right: IOperation:  (OperationKind.None, Language: C#) (Syntax: '*p2')
                            Children(1):
                                ILocalReferenceExpression: p2 (OperationKind.LocalReferenceExpression, Type: System.Int32*, Language: C#) (Syntax: 'p2')
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
IOperation:  (OperationKind.None, IsInvalid, Language: C#) (Syntax: 'fixed (int* ... }')
  Children(2):
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid, Language: C#) (Syntax: 'int* p1 =')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid, Language: C#) (Syntax: 'p1 =')
          Variables: Local_1: System.Int32* p1
          Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid, Language: C#) (Syntax: '')
              Children(0)
      IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'i3 = *p1;')
          Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: C#) (Syntax: 'i3 = *p1')
              Left: ILocalReferenceExpression: i3 (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i3')
              Right: IOperation:  (OperationKind.None, Language: C#) (Syntax: '*p1')
                  Children(1):
                      ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: System.Int32*, Language: C#) (Syntax: 'p1')
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
IOperation:  (OperationKind.None, IsInvalid, Language: C#) (Syntax: 'fixed (int* ... }')
  Children(2):
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, Language: C#) (Syntax: 'int* p1 = &i1')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: C#) (Syntax: 'p1 = &i1')
          Variables: Local_1: System.Int32* p1
          Initializer: IOperation:  (OperationKind.None, Language: C#) (Syntax: '&i1')
              Children(1):
                  IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*, Language: C#) (Syntax: '&i1')
                    Reference: IFieldReferenceExpression: System.Int32 C.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i1')
                        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: C#) (Syntax: 'i1')
      IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid, Language: C#) (Syntax: '{ ... }')
        IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid, Language: C#) (Syntax: 'i3 = &p1;')
          Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid, Language: C#) (Syntax: 'i3 = &p1')
              Left: ILocalReferenceExpression: i3 (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'i3')
              Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid, IsImplicit, Language: C#) (Syntax: '&p1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32**, IsInvalid, Language: C#) (Syntax: '&p1')
                      Reference: IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32*, IsInvalid, IsImplicit, Language: C#) (Syntax: 'p1')
                          Children(1):
                              ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: System.Int32*, IsInvalid, Language: C#) (Syntax: 'p1')
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
