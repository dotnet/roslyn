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
        public void ILockStatement_ObjectLock_FieldReference()
        {
            string source = @"
public class C1
{
    object o = new object();

    public void M()
    {
        /*<bind>*/lock (o)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ILockStatement (OperationKind.LockStatement) (Syntax: 'lock (o) ... }')
  Expression: IFieldReferenceExpression: System.Object C1.o (OperationKind.FieldReferenceExpression, Type: System.Object) (Syntax: 'o')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C1) (Syntax: 'o')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILockStatement_ObjectLock_LocalReference()
        {
            string source = @"
public class C1
{
    public void M()
    {
        object o = new object();
        /*<bind>*/lock (o)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ILockStatement (OperationKind.LockStatement) (Syntax: 'lock (o) ... }')
  Expression: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILockStatement_ObjectLock_Null()
        {
            string source = @"
public class C1
{
    public void M()
    {
        /*<bind>*/lock (null)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ILockStatement (OperationKind.LockStatement) (Syntax: 'lock (null) ... }')
  Expression: ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILockStatement_ObjectLock_NonReferenceType()
        {
            string source = @"
public class C1
{
    public void M()
    {
        int i = 1;
        /*<bind>*/lock (i)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ILockStatement (OperationKind.LockStatement, IsInvalid) (Syntax: 'lock (i) ... }')
  Expression: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0185: 'int' is not a reference type as required by the lock statement
                //         /*<bind>*/lock (i)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "i").WithArguments("int").WithLocation(7, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILockStatement_MissingLockExpression()
        {
            string source = @"
public class C1
{
    public void M()
    {
        /*<bind>*/lock ()
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ILockStatement (OperationKind.LockStatement, IsInvalid) (Syntax: 'lock () ... }')
  Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
      Children(0)
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //         /*<bind>*/lock ()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILockStatement_InvalidLockStatement()
        {
            string source = @"
using System;

public class C1
{
    public void M()
    {
        /*<bind>*/lock (invalidReference)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ILockStatement (OperationKind.LockStatement, IsInvalid) (Syntax: 'lock (inval ... }')
  Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'invalidReference')
      Children(0)
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'invalidReference' does not exist in the current context
                //         /*<bind>*/lock (invalidReference)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "invalidReference").WithArguments("invalidReference").WithLocation(8, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILockStatement_MissingBody()
        {
            string source = @"
public class C1
{
    public void M()
    {
        object o = new object();
        /*<bind>*/lock (o)
/*</bind>*/    }
}
";
            string expectedOperationTree = @"
ILockStatement (OperationKind.LockStatement, IsInvalid) (Syntax: 'lock (o)
')
  Expression: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
  Body: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '')
      Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: ?) (Syntax: '')
          Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term '}'
                //         /*<bind>*/lock (o)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(7, 27),
                // CS1002: ; expected
                //         /*<bind>*/lock (o)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(7, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILockStatement_ExpressionLock_ObjectMethodCall()
        {
            string source = @"
public class C1
{
    public void M()
    {
        object o = new object();
        /*<bind>*/lock (o.ToString())
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ILockStatement (OperationKind.LockStatement) (Syntax: 'lock (o.ToS ... }')
  Expression: IInvocationExpression (virtual System.String System.Object.ToString()) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'o.ToString()')
      Instance Receiver: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
      Arguments(0)
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILockStatement_ExpressionLock_ClassMethodCall()
        {
            string source = @"
public class C1
{
    public void M()
    {
        /*<bind>*/lock (M2())
        {
        }/*</bind>*/
    }

    public object M2()
    {
        return new object();
    }
}
";
            string expectedOperationTree = @"
ILockStatement (OperationKind.LockStatement) (Syntax: 'lock (M2()) ... }')
  Expression: IInvocationExpression ( System.Object C1.M2()) (OperationKind.InvocationExpression, Type: System.Object) (Syntax: 'M2()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C1) (Syntax: 'M2')
      Arguments(0)
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILockStatement_ExpressionCall_VoidMethodCall()
        {
            string source = @"
public class C1
{
    public void M()
    {
        /*<bind>*/lock (M2())
        {
        }/*</bind>*/
    }

    public void M2() { }
}
";
            string expectedOperationTree = @"
ILockStatement (OperationKind.LockStatement, IsInvalid) (Syntax: 'lock (M2()) ... }')
  Expression: IInvocationExpression ( void C1.M2()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'M2()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C1, IsInvalid) (Syntax: 'M2')
      Arguments(0)
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0185: 'void' is not a reference type as required by the lock statement
                //         /*<bind>*/lock (M2())
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "M2()").WithArguments("void").WithLocation(6, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILockStatement_NonEmptybody()
        {
            string source = @"
using System;

public class C1
{
    public void M()
    {
        /*<bind>*/lock (new object())
        {
            Console.WriteLine(""Hello World!"");
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ILockStatement (OperationKind.LockStatement) (Syntax: 'lock (new o ... }')
  Expression: IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'new object()')
      Arguments(0)
      Initializer: null
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... o World!"");')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... lo World!"")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""Hello World!""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Hello World!"") (Syntax: '""Hello World!""')
                  InConversion: null
                  OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
