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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'lock (o) ... }')
  Expression: 
    IFieldReferenceOperation: System.Object C1.o (OperationKind.FieldReference, IsExpression, Type: System.Object) (Syntax: 'o')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: C1, IsImplicit) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'lock (o) ... }')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, IsExpression, Type: System.Object) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'lock (null) ... }')
  Expression: 
    ILiteralOperation (OperationKind.Literal, IsExpression, Type: null, Constant: null) (Syntax: 'null')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null, IsInvalid) (Syntax: 'lock (i) ... }')
  Expression: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null, IsInvalid) (Syntax: 'lock () ... }')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, IsExpression, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null, IsInvalid) (Syntax: 'lock (inval ... }')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, IsExpression, Type: ?, IsInvalid) (Syntax: 'invalidReference')
      Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null, IsInvalid) (Syntax: 'lock (o)
')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, IsExpression, Type: System.Object) (Syntax: 'o')
  Body: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: '')
      Expression: 
        IInvalidOperation (OperationKind.Invalid, IsExpression, Type: null) (Syntax: '')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'lock (o.ToS ... }')
  Expression: 
    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, IsExpression, Type: System.String) (Syntax: 'o.ToString()')
      Instance Receiver: 
        ILocalReferenceOperation: o (OperationKind.LocalReference, IsExpression, Type: System.Object) (Syntax: 'o')
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'lock (M2()) ... }')
  Expression: 
    IInvocationOperation ( System.Object C1.M2()) (OperationKind.Invocation, IsExpression, Type: System.Object) (Syntax: 'M2()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: C1, IsImplicit) (Syntax: 'M2')
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null, IsInvalid) (Syntax: 'lock (M2()) ... }')
  Expression: 
    IInvocationOperation ( void C1.M2()) (OperationKind.Invocation, IsExpression, Type: System.Void, IsInvalid) (Syntax: 'M2()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: C1, IsInvalid, IsImplicit) (Syntax: 'M2')
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, IsStatement, Type: null) (Syntax: 'lock (new o ... }')
  Expression: 
    IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: System.Object) (Syntax: 'new object()')
      Arguments(0)
      Initializer: 
        null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'Console.Wri ... o World!"");')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'Console.Wri ... lo World!"")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: '""Hello World!""')
                  ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: ""Hello World!"") (Syntax: '""Hello World!""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
