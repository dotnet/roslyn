// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_SimpleUsingNewVariable()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        /*<bind>*/using (var c = new C())
        {
            Console.WriteLine(c.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (var  ... }')
  Locals: Local_1: C c
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var c = new C()')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                    Instance Receiver: 
                      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_MultipleNewVariable()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        
        /*<bind>*/using (C c1 = new C(), c2 = new C())
        {
            Console.WriteLine(c1.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (C c1 ... }')
  Locals: Local_1: C c1
    Local_2: C c2
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'C c1 = new  ... 2 = new C()')
      IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C c1 = new  ... 2 = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
            IVariableDeclaratorOperation (Symbol: C c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                    Instance Receiver: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_SimpleUsingStatementExistingResource()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        var c = new C();
        /*<bind>*/using (c)
        {
            Console.WriteLine(c.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (c) ... }')
  Resources: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                    Instance Receiver: 
                      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_NestedUsingNewResources()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        /*<bind>*/using (var c1 = new C())
        using (var c2 = new C())
        {
            Console.WriteLine(c1.ToString() + c2.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (var  ... }')
  Locals: Local_1: C c1
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var c1 = new C()')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c1 = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  Body: 
    IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (var  ... }')
      Locals: Local_1: C c2
      Resources: 
        IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var c2 = new C()')
          IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c2 = new C()')
            Declarators:
                IVariableDeclaratorOperation (Symbol: C c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = new C()')
                  Initializer: 
                    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                      IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                        Arguments(0)
                        Initializer: 
                          null
            Initializer: 
              null
      Body: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
            Expression: 
              IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString ... .ToString()')
                      IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
                        Left: 
                          IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                            Instance Receiver: 
                              ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                            Arguments(0)
                        Right: 
                          IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c2.ToString()')
                            Instance Receiver: 
                              ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C) (Syntax: 'c2')
                            Arguments(0)
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_NestedUsingExistingResources()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        var c1 = new C();
        var c2 = new C();
        /*<bind>*/using (c1)
        using (c2)
        {
            Console.WriteLine(c1.ToString() + c2.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (c1) ... }')
  Resources: 
    ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
  Body: 
    IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (c2) ... }')
      Resources: 
        ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C) (Syntax: 'c2')
      Body: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
            Expression: 
              IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString ... .ToString()')
                      IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
                        Left: 
                          IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                            Instance Receiver: 
                              ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                            Arguments(0)
                        Right: 
                          IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c2.ToString()')
                            Instance Receiver: 
                              ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C) (Syntax: 'c2')
                            Arguments(0)
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_InvalidMultipleVariableDeclaration()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        /*<bind>*/using (var c1 = new C(), c2 = new C())
        {
            Console.WriteLine(c1.ToString() + c2.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using (var  ... }')
  Locals: Local_1: C c1
    Local_2: C c2
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid, IsImplicit) (Syntax: 'var c1 = ne ... 2 = new C()')
      IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var c1 = ne ... 2 = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
            IVariableDeclaratorOperation (Symbol: C c2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c2 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString ... .ToString()')
                  IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
                    Left: 
                      IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                        Instance Receiver: 
                          ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                        Arguments(0)
                    Right: 
                      IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c2.ToString()')
                        Instance Receiver: 
                          ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C) (Syntax: 'c2')
                        Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0819: Implicitly-typed variables cannot have multiple declarators
                //         /*<bind>*/using (var c1 = new C(), c2 = new C())
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, "var c1 = new C(), c2 = new C()").WithLocation(12, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IOperationTests_MultipleExistingResourcesPassed()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    /*<bind>*/{
        var c1 = new C();
        var c2 = new C();
        using (c1, c2)
        {
            Console.WriteLine(c1.ToString() + c2.ToString());
        }
    }/*</bind>*/
}
";
            // Capturing the whole block here, to show that the using statement is actually being bound as a using statement, followed by
            // an expression and a separate block, rather than being bound as a using statement with an invalid expression as the resources
            string expectedOperationTree = @"
IBlockOperation (5 statements, 2 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: C c1
    Local_2: C c2
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var c1 = new C();')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c1 = new C()')
      Declarators:
          IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = new C()')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                  Arguments(0)
                  Initializer: 
                    null
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var c2 = new C();')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c2 = new C()')
      Declarators:
          IVariableDeclaratorOperation (Symbol: C c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = new C()')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                  Arguments(0)
                  Initializer: 
                    null
      Initializer: 
        null
  IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using (c1')
    Resources: 
      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c1')
    Body: 
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
            Children(0)
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c2')
    Expression: 
      ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c2')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
      Expression: 
        IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString ... .ToString()')
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
                  Left: 
                    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                      Instance Receiver: 
                        ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                      Arguments(0)
                  Right: 
                    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c2.ToString()')
                      Instance Receiver: 
                        ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: C) (Syntax: 'c2')
                      Arguments(0)
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1026: ) expected
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ",").WithLocation(14, 18),
                // CS1525: Invalid expression term ','
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(14, 18),
                // CS1002: ; expected
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(14, 18),
                // CS1513: } expected
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(14, 18),
                // CS1002: ; expected
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(14, 22),
                // CS1513: } expected
                //         using (c1, c2)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(14, 22)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_InvalidNonDisposableNewResource()
        {
            string source = @"
using System;

class C
{

    public static void M1()
    {
        /*<bind>*/using (var c1 = new C())
        {
            Console.WriteLine(c1.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using (var  ... }')
  Locals: Local_1: C c1
  Resources: 
    IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid, IsImplicit) (Syntax: 'var c1 = new C()')
      IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var c1 = new C()')
        Declarators:
            IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c1 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
        Initializer: 
          null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                    Instance Receiver: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1674: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         /*<bind>*/using (var c1 = new C())
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var c1 = new C()").WithArguments("C").WithLocation(9, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_InvalidNonDisposableExistingResource()
        {
            string source = @"
using System;

class C
{

    public static void M1()
    {
        var c1 = new C();
        /*<bind>*/using (c1)
        {
            Console.WriteLine(c1.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using (c1) ... }')
  Resources: 
    ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1.ToString()')
                  IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c1.ToString()')
                    Instance Receiver: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C) (Syntax: 'c1')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1674: 'C': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         /*<bind>*/using (c1)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "c1").WithArguments("C").WithLocation(10, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_InvalidEmptyUsingResources()
        {
            string source = @"
using System;

class C
{

    public static void M1()
    {
        /*<bind>*/using ()
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null, IsInvalid) (Syntax: 'using () ... }')
  Resources: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //         /*<bind>*/using ()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(9, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_UsingWithoutSavedReference()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        /*<bind>*/using (GetC())
        {
        }/*</bind>*/
    }

    public static C GetC() => new C();
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (GetC ... }')
  Resources: 
    IInvocationOperation (C C.GetC()) (OperationKind.Invocation, Type: C) (Syntax: 'GetC()')
      Instance Receiver: 
        null
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_DynamicArgument()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        dynamic d = null;
        /*<bind>*/using (d)
        {
            Console.WriteLine(d);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (d) ... }')
  Resources: 
    ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(d);')
        Expression: 
          IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'Console.WriteLine(d)')
            Expression: 
              IDynamicMemberReferenceOperation (Member Name: ""WriteLine"", Containing Type: System.Console) (OperationKind.DynamicMemberReference, Type: null) (Syntax: 'Console.WriteLine')
                Type Arguments(0)
                Instance Receiver: 
                  null
            Arguments(1):
                ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
            ArgumentNames(0)
            ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_NullResource()
        {
            string source = @"
using System;

class C
{
    public static void M1()
    {
        /*<bind>*/using (null)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (null ... }')
  Resources: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_UsingStatementSyntax_Declaration()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        using (/*<bind>*/var c = new C()/*</bind>*/)
        {
            Console.WriteLine(c.ToString());
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = new C()')
  Declarators:
      IVariableDeclaratorOperation (Symbol: C c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = new C()')
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
            IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
              Arguments(0)
              Initializer: 
                null
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_UsingStatementSyntax_StatementSyntax()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        using (var c = new C())
        /*<bind>*/{
            Console.WriteLine(c.ToString());
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... oString());')
    Expression: 
      IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ToString())')
        Instance Receiver: 
          null
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.ToString()')
              IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'c.ToString()')
                Instance Receiver: 
                  ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                Arguments(0)
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_UsingStatementSyntax_ExpressionSyntax()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        var c = new C();
        using (/*<bind>*/c/*</bind>*/)
        {
            Console.WriteLine(c.ToString());
        }
    }
}
";
            string expectedOperationTree = @"
ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_UsingStatementSyntax_VariableDeclaratorSyntax()
        {
            string source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }

    public static void M1()
    {
        
        using (C /*<bind>*/c1 = new C()/*</bind>*/, c2 = new C())
        {
            Console.WriteLine(c1.ToString());
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: C c1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1 = new C()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
      IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
        Arguments(0)
        Initializer: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IUsingStatement_OutVarInResource()
        {
            string source = @"
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M(P p)
    {
        /*<bind>*/using (p = M2(out int c))
        {
            c = 1;
        }/*</bind>*/
    }

    P M2(out int c)
    {
        c = 0;
        return new P();
    }
}
";
            string expectedOperationTree = @"
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (p =  ... }')
  Locals: Local_1: System.Int32 c
  Resources: 
    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P) (Syntax: 'p = M2(out int c)')
      Left: 
        IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: P) (Syntax: 'p')
      Right: 
        IInvocationOperation ( P P.M2(out System.Int32 c)) (OperationKind.Invocation, Type: P) (Syntax: 'M2(out int c)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'M2')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: 'out int c')
                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int c')
                  ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = 1;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'c = 1')
            Left: 
              ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<UsingStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_01()
        {
            string source = @"
class P
{
    void M(System.IDisposable input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    System.IDisposable GetDisposable() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IInvocationOperation ( System.IDisposable P.GetDisposable()) (OperationKind.Invocation, Type: System.IDisposable) (Syntax: 'GetDisposable()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.IDisposable) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable() ?? input')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')

    Next (Regular) Block[B5]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B9]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B6] - Block
        Predecessors (0)
        Statements (0)
        Jump if True (Regular) to Block[B8]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Operand: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Arguments(0)

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B9] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_02()
        {
            string source = @"
class P
{
    void M(MyDisposable input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable GetDisposable() => throw null;
}

class MyDisposable : System.IDisposable
{
    public void Dispose() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IInvocationOperation ( MyDisposable P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable) (Syntax: 'GetDisposable()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable() ?? input')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')

    Next (Regular) Block[B5]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B9]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B6] - Block
        Predecessors (0)
        Statements (0)
        Jump if True (Regular) to Block[B8]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Operand: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Arguments(0)

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B9] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_03()
        {
            string source = @"
class P
{
    void M(MyDisposable input, bool b)
/*<bind>*/{
        using (b ? GetDisposable() : input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable GetDisposable() => throw null;
}

struct MyDisposable : System.IDisposable
{
    public void Dispose() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IInvocationOperation ( MyDisposable P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable) (Syntax: 'GetDisposable()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')

    Next (Regular) Block[B5]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B7]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B6] - Block
        Predecessors (0)
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Boxing)
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
              Arguments(0)

        Next (StructuredExceptionHandling) Block[null]
}

Block[B7] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_04()
        {
            string source = @"
class P
{
    void M<MyDisposable>(MyDisposable input, bool b) where MyDisposable : System.IDisposable
/*<bind>*/{
        using (b ? GetDisposable<MyDisposable>() : input)
        {
            b = true;
        }
    }/*</bind>*/

    T GetDisposable<T>() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposab ... sposable>()')
          Value: 
            IInvocationOperation ( MyDisposable P.GetDisposable<MyDisposable>()) (OperationKind.Invocation, Type: MyDisposable) (Syntax: 'GetDisposab ... sposable>()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposab ... Disposable>')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')

    Next (Regular) Block[B5]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B9]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B6] - Block
        Predecessors (0)
        Statements (0)
        Jump if True (Regular) to Block[B8]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
              Operand: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Boxing)
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
              Arguments(0)

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B9] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_05()
        {
            string source = @"
class P
{
    void M(MyDisposable? input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable? GetDisposable() => throw null;
}

struct MyDisposable : System.IDisposable
{
    public void Dispose() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IInvocationOperation ( MyDisposable? P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable?) (Syntax: 'GetDisposable()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable?) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable() ?? input')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsImplicit) (Syntax: 'GetDisposable() ?? input')

    Next (Regular) Block[B5]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B9]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B6] - Block
        Predecessors (0)
        Statements (0)
        Jump if True (Regular) to Block[B8]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Operand: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsImplicit) (Syntax: 'GetDisposable() ?? input')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Boxing)
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Arguments(0)

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B9] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_06()
        {
            string source = @"
class P
{
    void M(dynamic input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    dynamic GetDisposable() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IInvocationOperation ( dynamic P.GetDisposable()) (OperationKind.Invocation, Type: dynamic) (Syntax: 'GetDisposable()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable() ?? input')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (ExplicitDynamic)
              Operand: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'GetDisposable() ?? input')

    Next (Regular) Block[B5]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B9]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B6] - Block
        Predecessors (0)
        Statements (0)
        Jump if True (Regular) to Block[B8]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Operand: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Arguments(0)

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B9] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_07()
        {
            string source = @"
class P
{
    void M(NotDisposable input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    NotDisposable GetDisposable() => throw null;
}

class NotDisposable
{
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IInvocationOperation ( NotDisposable P.GetDisposable()) (OperationKind.Invocation, Type: NotDisposable, IsInvalid) (Syntax: 'GetDisposable()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'GetDisposable')
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: NotDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: NotDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: NotDisposable, IsInvalid) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: NotDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')

    Next (Regular) Block[B5]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B9]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B6] - Block
        Predecessors (0)
        Statements (0)
        Jump if True (Regular) to Block[B8]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Operand: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: NotDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ExplicitReference)
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: NotDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Arguments(0)

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B9] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,16): error CS1674: 'NotDisposable': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (GetDisposable() ?? input)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "GetDisposable() ?? input").WithArguments("NotDisposable").WithLocation(6, 16)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_08()
        {
            string source = @"
class P
{
    void M(MyDisposable input, bool b)
/*<bind>*/{
        using (b ? GetDisposable() : input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable GetDisposable() => throw null;
}

struct MyDisposable
{
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'b')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IInvocationOperation ( MyDisposable P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable, IsInvalid) (Syntax: 'GetDisposable()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'GetDisposable')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable, IsInvalid) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')

    Next (Regular) Block[B5]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B7]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B6] - Block
        Predecessors (0)
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NoConversion)
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... e() : input')
              Arguments(0)

        Next (StructuredExceptionHandling) Block[null]
}

Block[B7] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,16): error CS1674: 'MyDisposable': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (b ? GetDisposable() : input)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "b ? GetDisposable() : input").WithArguments("MyDisposable").WithLocation(6, 16)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_09()
        {
            string source = @"
class P
{
    void M<MyDisposable>(MyDisposable input, bool b)
/*<bind>*/{
        using (b ? GetDisposable<MyDisposable>() : input)
        {
            b = true;
        }
    }/*</bind>*/

    T GetDisposable<T>() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'b')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposab ... sposable>()')
          Value: 
            IInvocationOperation ( MyDisposable P.GetDisposable<MyDisposable>()) (OperationKind.Invocation, Type: MyDisposable, IsInvalid) (Syntax: 'GetDisposab ... sposable>()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'GetDisposab ... Disposable>')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable, IsInvalid) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')

    Next (Regular) Block[B5]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B9]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B6] - Block
        Predecessors (0)
        Statements (0)
        Jump if True (Regular) to Block[B8]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
              Operand: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Unboxing)
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsInvalid, IsImplicit) (Syntax: 'b ? GetDisp ... >() : input')
              Arguments(0)

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B9] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,16): error CS1674: 'MyDisposable': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (b ? GetDisposable<MyDisposable>() : input)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "b ? GetDisposable<MyDisposable>() : input").WithArguments("MyDisposable").WithLocation(6, 16)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_10()
        {
            string source = @"
class P
{
    void M(MyDisposable? input, bool b)
/*<bind>*/{
        using (GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable? GetDisposable() => throw null;
}

struct MyDisposable
{
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IInvocationOperation ( MyDisposable? P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable?, IsInvalid) (Syntax: 'GetDisposable()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsInvalid, IsImplicit) (Syntax: 'GetDisposable')
              Arguments(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsInvalid, IsImplicit) (Syntax: 'GetDisposable()')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable?, IsInvalid) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')

    Next (Regular) Block[B5]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B9]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B6] - Block
        Predecessors (0)
        Statements (0)
        Jump if True (Regular) to Block[B8]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Operand: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NoConversion)
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyDisposable?, IsInvalid, IsImplicit) (Syntax: 'GetDisposable() ?? input')
              Arguments(0)

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B9] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,16): error CS1674: 'MyDisposable?': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (GetDisposable() ?? input)
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "GetDisposable() ?? input").WithArguments("MyDisposable?").WithLocation(6, 16)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_11()
        {
            string source = @"
class P
{
    void M(MyDisposable input, bool b)
/*<bind>*/{
        using (var x = GetDisposable() ?? input)
        {
            b = true;
        }
    }/*</bind>*/

    MyDisposable GetDisposable() => throw null;
}

class MyDisposable : System.IDisposable
{
    public void Dispose() => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [MyDisposable x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
              Value: 
                IInvocationOperation ( MyDisposable P.GetDisposable()) (OperationKind.Invocation, Type: MyDisposable) (Syntax: 'GetDisposable()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'GetDisposable')
                  Arguments(0)

        Jump if True (Regular) to Block[B3]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetDisposable()')
              Operand: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable()')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetDisposable()')
              Value: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable()')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyDisposable) (Syntax: 'input')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyDisposable, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: MyDisposable, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyDisposable, IsImplicit) (Syntax: 'GetDisposable() ?? input')

        Next (Regular) Block[B5]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B9]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B6] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
                  Operand: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: MyDisposable, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: MyDisposable, IsImplicit) (Syntax: 'x = GetDisp ... () ?? input')
                  Arguments(0)

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B9] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_12()
        {
            string source = @"
class P
{
    void M(dynamic input1, dynamic input2, bool b)
/*<bind>*/{
        using (dynamic x = input1, y = input2)
        {
            b = true;
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [dynamic x] [dynamic y]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic, IsImplicit) (Syntax: 'x = input1')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'x = input1')
              Right: 
                IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'input1')

            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x = input1')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x = input1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ExplicitDynamic)
                  Operand: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'x = input1')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic, IsImplicit) (Syntax: 'y = input2')
                  Left: 
                    ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'y = input2')
                  Right: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'input2')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y = input2')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'y = input2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitDynamic)
                      Operand: 
                        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'y = input2')

            Next (Regular) Block[B3]
                Entering: {R4} {R5}

        .try {R4, R5}
        {
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                          Left: 
                            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

                Next (Regular) Block[B10]
                    Finalizing: {R6} {R7}
                    Leaving: {R5} {R4} {R3} {R2} {R1}
        }
        .finally {R6}
        {
            Block[B4] - Block
                Predecessors (0)
                Statements (0)
                Jump if True (Regular) to Block[B6]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y = input2')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'y = input2')

                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y = input2')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'y = input2')
                      Arguments(0)

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (0)
                Next (StructuredExceptionHandling) Block[null]
        }
    }
    .finally {R7}
    {
        Block[B7] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B9]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x = input1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'x = input1')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x = input1')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'x = input1')
                  Arguments(0)

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B10] - Exit
    Predecessors: [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_13()
        {
            string source = @"
class P
{
    void M(System.IDisposable input, object o)
/*<bind>*/{
        using (input)
        {
            o?.ToString();
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.IDisposable) (Syntax: 'input')

    Next (Regular) Block[B2]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o')
              Value: 
                IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')

        Jump if True (Regular) to Block[B7]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o')
              Operand: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o')
            Finalizing: {R3}
            Leaving: {R2} {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'o?.ToString();')
              Expression: 
                IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o')
                  Arguments(0)

        Next (Regular) Block[B7]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B4] - Block
        Predecessors (0)
        Statements (0)
        Jump if True (Regular) to Block[B6]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
              Operand: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'input')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'input')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'input')
              Arguments(0)

        Next (Regular) Block[B6]
    Block[B6] - Block
        Predecessors: [B4] [B5]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B7] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_14()
        {
            string source = @"
class P : System.IDisposable
{
    public void Dispose()
    {
    }

    void M(System.IDisposable input, P p)
    /*<bind>*/{
        using (p = M2(out int c))
        {
            c = 1;
        }
    }/*</bind>*/

    P M2(out int c)
    {
        c = 0;
        return new P();
    }
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p = M2(out int c)')
              Value: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P) (Syntax: 'p = M2(out int c)')
                  Left: 
                    IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: P) (Syntax: 'p')
                  Right: 
                    IInvocationOperation ( P P.M2(out System.Int32 c)) (OperationKind.Invocation, Type: P) (Syntax: 'M2(out int c)')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'M2')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: 'out int c')
                            IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int c')
                              ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = 1;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'c = 1')
                      Left: 
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

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
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'p = M2(out int c)')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'p = M2(out int c)')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'p = M2(out int c)')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'p = M2(out int c)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'p = M2(out int c)')
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
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_15()
        {
            string source = @"
class P
{
    void M(System.IDisposable input, object o)
/*<bind>*/{
        using (input)
        {
            o?.ToString();
        }
    }/*</bind>*/
}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);
            compilation.MakeMemberMissing(SpecialMember.System_IDisposable__Dispose);

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.IDisposable) (Syntax: 'input')

    Next (Regular) Block[B2]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o')
              Value: 
                IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')

        Jump if True (Regular) to Block[B7]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o')
              Operand: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o')
            Finalizing: {R3}
            Leaving: {R2} {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'o?.ToString();')
              Expression: 
                IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o')
                  Arguments(0)

        Next (Regular) Block[B7]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B4] - Block
        Predecessors (0)
        Statements (0)
        Jump if True (Regular) to Block[B6]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
              Operand: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'input')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'input')
              Children(1):
                  IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'input')

        Next (Regular) Block[B6]
    Block[B6] - Block
        Predecessors: [B4] [B5]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B7] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UsingFlow_16()
        {
            string source = @"
class P
{
    void M()
    /*<bind>*/{
        using (null)
        {
        }
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, Constant: null, IsImplicit) (Syntax: 'null')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (DefaultOrNullLiteral)
              Operand: 
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

    Next (Regular) Block[B2]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Regular) Block[B6]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B3] - Block
        Predecessors (0)
        Statements (0)
        Jump if True (Regular) to Block[B5]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'null')
              Operand: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, Constant: null, IsImplicit) (Syntax: 'null')

        Next (Regular) Block[B4]
    Block[B4] - Block [UnReachable]
        Predecessors: [B3]
        Statements (1)
            IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'null')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.IDisposable, Constant: null, IsImplicit) (Syntax: 'null')
              Arguments(0)

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }
    }
}
