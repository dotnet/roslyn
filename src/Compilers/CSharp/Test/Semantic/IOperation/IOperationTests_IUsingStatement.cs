// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
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
              IgnoredArguments(0)
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'c.ToString()')
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
              IgnoredArguments(0)
            IVariableDeclaratorOperation (Symbol: C c2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
              IgnoredArguments(0)
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'c1.ToString()')
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'c.ToString()')
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
              IgnoredArguments(0)
        Initializer: 
          null
  Body: 
    IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'using (var  ... }')
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
                  IgnoredArguments(0)
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
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
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
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
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
              IgnoredArguments(0)
            IVariableDeclaratorOperation (Symbol: C c2) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c2 = new C()')
              Initializer: 
                IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= new C()')
                  IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
                    Arguments(0)
                    Initializer: 
                      null
              IgnoredArguments(0)
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
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
            IgnoredArguments(0)
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
            IgnoredArguments(0)
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
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'c1.ToString ... .ToString()')
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
              IgnoredArguments(0)
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'c1.ToString()')
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'c1.ToString()')
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
              IDynamicMemberReferenceOperation (Member Name: ""WriteLine"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null) (Syntax: 'Console.WriteLine')
                Type Arguments(0)
                Instance Receiver: 
                  IOperation:  (OperationKind.None, Type: null) (Syntax: 'Console')
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
        IgnoredArguments(0)
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
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'c.ToString()')
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
  IgnoredArguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
