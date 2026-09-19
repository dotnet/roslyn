// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_ILockStatement : SemanticModelTestBase
    {
        private const string LockTypeDefinition = """
            namespace System.Threading
            {
                public class Lock
                {
                    public Scope EnterScope() => new Scope();

                    public ref struct Scope
                    {
                        public void Dispose() { }
                    }
                }
            }
            """;

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
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'lock (o) ... }')
  Expression: 
    IFieldReferenceOperation: System.Object C1.o (OperationKind.FieldReference, Type: System.Object) (Syntax: 'o')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'lock (o) ... }')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'lock (null) ... }')
  Expression: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'lock (i) ... }')
  Expression: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'lock () ... }')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'lock (inval ... }')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'invalidReference')
      Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'lock (o)')
  Expression: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  Body: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '')
      Expression: 
        IInvalidOperation (OperationKind.Invalid, Type: null) (Syntax: '')
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
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'lock (o.ToS ... }')
  Expression: 
    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'o.ToString()')
      Instance Receiver: 
        ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'lock (M2()) ... }')
  Expression: 
    IInvocationOperation ( System.Object C1.M2()) (OperationKind.Invocation, Type: System.Object) (Syntax: 'M2()')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'M2')
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'lock (M2()) ... }')
  Expression: 
    IInvocationOperation ( void C1.M2()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M2()')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'M2')
      Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'lock (new o ... }')
  Expression: 
    IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object) (Syntax: 'new object()')
      Arguments(0)
      Initializer: 
        null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... o World!"");')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... lo World!"")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""Hello World!""')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Hello World!"") (Syntax: '""Hello World!""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation)]
        public void ILockStatement_LockObject()
        {
            var source = """
                using System;
                using System.Threading;

                Lock l = new Lock();
                /*<bind>*/lock (l)
                {
                }/*</bind>*/
                """;

            var expectedOperationTree = """
                ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'lock (l) ... }')
                  Expression:
                    ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock) (Syntax: 'l')
                  Body:
                    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                """;

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>([source, LockTypeDefinition], expectedOperationTree, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation)]
        public void ILockStatement_LockObject_MissingEnterScope()
        {
            var source = """
                using System;
                using System.Threading;

                Lock l = new Lock();
                /*<bind>*/lock (l)
                {
                }/*</bind>*/

                namespace System.Threading
                {
                    public class Lock { }
                }
                """;

            var expectedOperationTree = """
                ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'lock (l) ... }')
                  Expression:
                    ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock, IsInvalid) (Syntax: 'l')
                  Body:
                    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                """;

            var expectedDiagnostics = new[]
            {
                // (5,17): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
                // /*<bind>*/lock (l)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(5, 17)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation)]
        public void ILockStatement_LockObject_MissingScopeDispose()
        {
            var source = """
                using System;
                using System.Threading;

                Lock l = new Lock();
                /*<bind>*/lock (l)
                {
                }/*</bind>*/

                namespace System.Threading
                {
                    public class Lock
                    {
                        public Scope EnterScope() => new Scope();

                        public ref struct Scope { }
                    }
                }
                """;

            var expectedOperationTree = """
                ILockOperation (OperationKind.Lock, Type: null, IsInvalid) (Syntax: 'lock (l) ... }')
                  Expression:
                    ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock, IsInvalid) (Syntax: 'l')
                  Body:
                    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                """;

            var expectedDiagnostics = new[]
            {
                // (5,17): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
                // /*<bind>*/lock (l)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(5, 17)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation)]
        public void ILockStatement_LockObject_EnterScopeVirtual()
        {
            var source = """
                using System;
                using System.Threading;

                Lock l = new LockDerived();
                /*<bind>*/lock (l)
                {
                }/*</bind>*/

                namespace System.Threading
                {
                    public class Lock
                    {
                        public virtual Scope EnterScope() => new();

                        public ref struct Scope
                        {
                            public void Dispose() { }
                        }
                    }

                    public class LockDerived : Lock
                    {
                        public override Scope EnterScope() => new();
                    }
                }
                """;

            var expectedOperationTree = """
                ILockOperation (OperationKind.Lock, Type: null) (Syntax: 'lock (l) ... }')
                  Expression:
                    ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock) (Syntax: 'l')
                  Body:
                    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
                """;

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LockStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LockFlow_01()
        {
            string source = @"
class P
{
    void M(object source1, object source2, object source3)
/*<bind>*/{
        lock (source1 ?? source2)
            source3?.ToString();
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [System.Boolean ?]
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'source1')
                  Value: 
                    IParameterReferenceOperation: source1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'source1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'source1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'source1')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'source1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'source1')

            Next (Regular) Block[B4]
                Leaving: {R2}
                Entering: {R3} {R4}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'source2')
              Value: 
                IParameterReferenceOperation: source2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'source2')

        Next (Regular) Block[B4]
            Entering: {R3} {R4}

    .try {R3, R4}
    {
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IInvocationOperation (void System.Threading.Monitor.Enter(System.Object obj, ref System.Boolean lockTaken)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'source1 ?? source2')
                  Instance Receiver: 
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'source1 ?? source2')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'source1 ?? source2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: lockTaken) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'source1 ?? source2')
                        ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'source1 ?? source2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B5]
                Entering: {R5}

        .locals {R5}
        {
            CaptureIds: [2]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'source3')
                      Value: 
                        IParameterReferenceOperation: source3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'source3')

                Jump if True (Regular) to Block[B10]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'source3')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'source3')
                    Finalizing: {R6}
                    Leaving: {R5} {R4} {R3} {R1}

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'source3?.ToString();')
                      Expression: 
                        IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'source3')
                          Arguments(0)

                Next (Regular) Block[B10]
                    Finalizing: {R6}
                    Leaving: {R5} {R4} {R3} {R1}
        }
    }
    .finally {R6}
    {
        Block[B7] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B9]
                ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'source1 ?? source2')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (1)
                IInvocationOperation (void System.Threading.Monitor.Exit(System.Object obj)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'source1 ?? source2')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'source1 ?? source2')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'source1 ?? source2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B10] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LockFlow_02()
        {
            string source = @"
class P
{
    void M(string input1, bool input2)
/*<bind>*/{
        lock (input1)
            input2 = true;
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'input1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input1')

            IInvocationOperation (void System.Threading.Monitor.Enter(System.Object obj)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'input1')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'input1')
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input2 = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'input2 = true')
                      Left: 
                        IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input2')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B4]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (void System.Threading.Monitor.Exit(System.Object obj)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'input1')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'input1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics,
                                                              targetFramework: Roslyn.Test.Utilities.TargetFramework.Empty,
                                                              references: new[] { MscorlibRef_v20 });
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LockFlow_03()
        {
            string source = @"
class P
{
    void M(int input1, bool input2)
/*<bind>*/{
        lock (input1)
            input2 = true;
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
    Locals: [System.Boolean ?]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input1')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'input1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Boxing)
                  Operand: 
                    IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'input1')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IInvocationOperation (void System.Threading.Monitor.Enter(System.Object obj, ref System.Boolean lockTaken)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'input1')
                  Instance Receiver: 
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'input1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'input1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: lockTaken) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'input1')
                        ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'input1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input2 = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'input2 = true')
                      Left: 
                        IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input2')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B5]
                ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'input1')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (void System.Threading.Monitor.Exit(System.Object obj)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'input1')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'input1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'input1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
            var expectedDiagnostics = new[] {
                // file.cs(6,15): error CS0185: 'int' is not a reference type as required by the lock statement
                //         lock (input1)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "input1").WithArguments("int").WithLocation(6, 15)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LockFlow_04()
        {
            string source = @"
class P
{
    void M(bool input2)
/*<bind>*/{
        lock (null)
            input2 = true;
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
    Locals: [System.Boolean ?]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NullLiteral)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IInvocationOperation (void System.Threading.Monitor.Enter(System.Object obj, ref System.Boolean lockTaken)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'null')
                  Instance Receiver: 
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'null')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: lockTaken) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'null')
                        ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'null')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input2 = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'input2 = true')
                      Left: 
                        IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input2')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B5]
                ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'null')
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvocationOperation (void System.Threading.Monitor.Exit(System.Object obj)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'null')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'null')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
        public void LockFlow_05()
        {
            string source = @"
class P
{
    void M(P input, bool b)
    /*<bind>*/{
        lock (input)
            b = true;
    }/*</bind>*/
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter);
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter2);

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: P) (Syntax: 'input')

            IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'input')
              Children(1):
                  IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B4]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (void System.Threading.Monitor.Exit(System.Object obj)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'input')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'input')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LockFlow_06()
        {
            string source = @"
class P
{
    void M(P input, bool b)
    /*<bind>*/{
        lock (input)
            b = true;
    }/*</bind>*/
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Exit);

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Boolean ?]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: P) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IInvocationOperation (void System.Threading.Monitor.Enter(System.Object obj, ref System.Boolean lockTaken)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'input')
                  Instance Receiver: 
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'input')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: lockTaken) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'input')
                        ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = true')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B6]
                Finalizing: {R4}
                Leaving: {R3} {R2} {R1}
    }
    .finally {R4}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Jump if False (Regular) to Block[B5]
                ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'input')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'input')
                  Children(1):
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')

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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void LockFlow_LockObject()
        {
            var source = """
                using System;
                using System.Threading;

                class C
                {
                    void M()
                    /*<bind>*/{
                        Lock l = new Lock();
                        lock (l)
                        {
                        }
                    }/*</bind>*/
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1}
                .locals {R1}
                {
                    Locals: [System.Threading.Lock l]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Left:
                                ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Right:
                                IObjectCreationOperation (Constructor: System.Threading.Lock..ctor()) (OperationKind.ObjectCreation, Type: System.Threading.Lock) (Syntax: 'new Lock()')
                                  Arguments(0)
                                  Initializer:
                                    null
                        Next (Regular) Block[B2]
                            Entering: {R2}
                    .locals {R2}
                    {
                        CaptureIds: [0]
                        Block[B2] - Block
                            Predecessors: [B1]
                            Statements (1)
                                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l')
                                  Value:
                                    IInvocationOperation ( System.Threading.Lock.Scope System.Threading.Lock.EnterScope()) (OperationKind.Invocation, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'l')
                                      Instance Receiver:
                                        ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock) (Syntax: 'l')
                                      Arguments(0)
                            Next (Regular) Block[B3]
                                Entering: {R3} {R4}
                        .try {R3, R4}
                        {
                            Block[B3] - Block
                                Predecessors: [B2]
                                Statements (0)
                                Next (Regular) Block[B5]
                                    Finalizing: {R5}
                                    Leaving: {R4} {R3} {R2} {R1}
                        }
                        .finally {R5}
                        {
                            Block[B4] - Block
                                Predecessors (0)
                                Statements (1)
                                    IInvocationOperation ( void System.Threading.Lock.Scope.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'l')
                                      Instance Receiver:
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'l')
                                      Arguments(0)
                                Next (StructuredExceptionHandling) Block[null]
                        }
                    }
                }
                Block[B5] - Exit
                    Predecessors: [B3]
                    Statements (0)
                """;

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>([source, LockTypeDefinition], expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void LockFlow_LockObject_NonEmptyBody()
        {
            var source = """
                using System;
                using System.Threading;

                class C
                {
                    void M()
                    /*<bind>*/{
                        Lock l = new Lock();
                        lock (l)
                        {
                            Console.Write("Body");
                        }
                    }/*</bind>*/
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1}
                .locals {R1}
                {
                    Locals: [System.Threading.Lock l]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Left:
                                ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Right:
                                IObjectCreationOperation (Constructor: System.Threading.Lock..ctor()) (OperationKind.ObjectCreation, Type: System.Threading.Lock) (Syntax: 'new Lock()')
                                  Arguments(0)
                                  Initializer:
                                    null
                        Next (Regular) Block[B2]
                            Entering: {R2}
                    .locals {R2}
                    {
                        CaptureIds: [0]
                        Block[B2] - Block
                            Predecessors: [B1]
                            Statements (1)
                                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l')
                                  Value:
                                    IInvocationOperation ( System.Threading.Lock.Scope System.Threading.Lock.EnterScope()) (OperationKind.Invocation, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'l')
                                      Instance Receiver:
                                        ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock) (Syntax: 'l')
                                      Arguments(0)
                            Next (Regular) Block[B3]
                                Entering: {R3} {R4}
                        .try {R3, R4}
                        {
                            Block[B3] - Block
                                Predecessors: [B2]
                                Statements (1)
                                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Write("Body");')
                                      Expression:
                                        IInvocationOperation (void System.Console.Write(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Write("Body")')
                                          Instance Receiver:
                                            null
                                          Arguments(1):
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Body"')
                                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Body") (Syntax: '"Body"')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                Next (Regular) Block[B5]
                                    Finalizing: {R5}
                                    Leaving: {R4} {R3} {R2} {R1}
                        }
                        .finally {R5}
                        {
                            Block[B4] - Block
                                Predecessors (0)
                                Statements (1)
                                    IInvocationOperation ( void System.Threading.Lock.Scope.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'l')
                                      Instance Receiver:
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'l')
                                      Arguments(0)
                                Next (StructuredExceptionHandling) Block[null]
                        }
                    }
                }
                Block[B5] - Exit
                    Predecessors: [B3]
                    Statements (0)
                """;

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>([source, LockTypeDefinition], expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void LockFlow_LockObject_ConditionalBody()
        {
            var source = """
                using System;
                using System.Threading;

                class C
                {
                    void M(bool b)
                    /*<bind>*/{
                        Lock l = new Lock();
                        lock (l)
                        {
                            if (b)
                            {
                                Console.Write("Body");
                            }
                            else
                            {
                                Console.Write("Else");
                            }
                        }
                    }/*</bind>*/
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1}
                .locals {R1}
                {
                    Locals: [System.Threading.Lock l]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Left:
                                ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Right:
                                IObjectCreationOperation (Constructor: System.Threading.Lock..ctor()) (OperationKind.ObjectCreation, Type: System.Threading.Lock) (Syntax: 'new Lock()')
                                  Arguments(0)
                                  Initializer:
                                    null
                        Next (Regular) Block[B2]
                            Entering: {R2}
                    .locals {R2}
                    {
                        CaptureIds: [0]
                        Block[B2] - Block
                            Predecessors: [B1]
                            Statements (1)
                                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l')
                                  Value:
                                    IInvocationOperation ( System.Threading.Lock.Scope System.Threading.Lock.EnterScope()) (OperationKind.Invocation, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'l')
                                      Instance Receiver:
                                        ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock) (Syntax: 'l')
                                      Arguments(0)
                            Next (Regular) Block[B3]
                                Entering: {R3} {R4}
                        .try {R3, R4}
                        {
                            Block[B3] - Block
                                Predecessors: [B2]
                                Statements (0)
                                Jump if False (Regular) to Block[B5]
                                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                                Next (Regular) Block[B4]
                            Block[B4] - Block
                                Predecessors: [B3]
                                Statements (1)
                                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Write("Body");')
                                      Expression:
                                        IInvocationOperation (void System.Console.Write(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Write("Body")')
                                          Instance Receiver:
                                            null
                                          Arguments(1):
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Body"')
                                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Body") (Syntax: '"Body"')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                Next (Regular) Block[B7]
                                    Finalizing: {R5}
                                    Leaving: {R4} {R3} {R2} {R1}
                            Block[B5] - Block
                                Predecessors: [B3]
                                Statements (1)
                                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Write("Else");')
                                      Expression:
                                        IInvocationOperation (void System.Console.Write(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Write("Else")')
                                          Instance Receiver:
                                            null
                                          Arguments(1):
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Else"')
                                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Else") (Syntax: '"Else"')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                Next (Regular) Block[B7]
                                    Finalizing: {R5}
                                    Leaving: {R4} {R3} {R2} {R1}
                        }
                        .finally {R5}
                        {
                            Block[B6] - Block
                                Predecessors (0)
                                Statements (1)
                                    IInvocationOperation ( void System.Threading.Lock.Scope.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'l')
                                      Instance Receiver:
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'l')
                                      Arguments(0)
                                Next (StructuredExceptionHandling) Block[null]
                        }
                    }
                }
                Block[B7] - Exit
                    Predecessors: [B4] [B5]
                    Statements (0)
                """;

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>([source, LockTypeDefinition], expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void LockFlow_LockObject_Conditional()
        {
            var source = """
                using System;
                using System.Threading;

                class C
                {
                    void M(bool b, Lock l1, Lock l2)
                    /*<bind>*/{
                        lock (b ? l1 : l2)
                        {
                        }
                    }/*</bind>*/
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1} {R2}
                .locals {R1}
                {
                    CaptureIds: [1]
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
                                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l1')
                                  Value:
                                    IParameterReferenceOperation: l1 (OperationKind.ParameterReference, Type: System.Threading.Lock) (Syntax: 'l1')
                            Next (Regular) Block[B4]
                        Block[B3] - Block
                            Predecessors: [B1]
                            Statements (1)
                                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l2')
                                  Value:
                                    IParameterReferenceOperation: l2 (OperationKind.ParameterReference, Type: System.Threading.Lock) (Syntax: 'l2')
                            Next (Regular) Block[B4]
                        Block[B4] - Block
                            Predecessors: [B2] [B3]
                            Statements (1)
                                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b ? l1 : l2')
                                  Value:
                                    IInvocationOperation ( System.Threading.Lock.Scope System.Threading.Lock.EnterScope()) (OperationKind.Invocation, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'b ? l1 : l2')
                                      Instance Receiver:
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'b ? l1 : l2')
                                      Arguments(0)
                            Next (Regular) Block[B5]
                                Leaving: {R2}
                                Entering: {R3} {R4}
                    }
                    .try {R3, R4}
                    {
                        Block[B5] - Block
                            Predecessors: [B4]
                            Statements (0)
                            Next (Regular) Block[B7]
                                Finalizing: {R5}
                                Leaving: {R4} {R3} {R1}
                    }
                    .finally {R5}
                    {
                        Block[B6] - Block
                            Predecessors (0)
                            Statements (1)
                                IInvocationOperation ( void System.Threading.Lock.Scope.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b ? l1 : l2')
                                  Instance Receiver:
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'b ? l1 : l2')
                                  Arguments(0)
                            Next (StructuredExceptionHandling) Block[null]
                    }
                }
                Block[B7] - Exit
                    Predecessors: [B5]
                    Statements (0)
                """;

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>([source, LockTypeDefinition], expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void LockFlow_LockObject_Conditional_Object()
        {
            var source = """
                using System;
                using System.Threading;

                class C
                {
                    void M(bool b, object o)
                    /*<bind>*/{
                        Lock l = new Lock();
                        lock (b ? l : o)
                        {
                        }
                    }/*</bind>*/
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1}
                .locals {R1}
                {
                    Locals: [System.Threading.Lock l]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Left:
                                ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Right:
                                IObjectCreationOperation (Constructor: System.Threading.Lock..ctor()) (OperationKind.ObjectCreation, Type: System.Threading.Lock) (Syntax: 'new Lock()')
                                  Arguments(0)
                                  Initializer:
                                    null
                        Next (Regular) Block[B2]
                            Entering: {R2}
                    .locals {R2}
                    {
                        Locals: [System.Boolean ?]
                        CaptureIds: [0]
                        Block[B2] - Block
                            Predecessors: [B1]
                            Statements (0)
                            Jump if False (Regular) to Block[B4]
                                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
                            Next (Regular) Block[B3]
                        Block[B3] - Block
                            Predecessors: [B2]
                            Statements (1)
                                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l')
                                  Value:
                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'l')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                        (ImplicitReference)
                                      Operand:
                                        ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock) (Syntax: 'l')
                            Next (Regular) Block[B5]
                                Entering: {R3} {R4}
                        Block[B4] - Block
                            Predecessors: [B2]
                            Statements (1)
                                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o')
                                  Value:
                                    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
                            Next (Regular) Block[B5]
                                Entering: {R3} {R4}
                        .try {R3, R4}
                        {
                            Block[B5] - Block
                                Predecessors: [B3] [B4]
                                Statements (1)
                                    IInvocationOperation (void System.Threading.Monitor.Enter(System.Object obj, ref System.Boolean lockTaken)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b ? l : o')
                                      Instance Receiver:
                                        null
                                      Arguments(2):
                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b ? l : o')
                                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'b ? l : o')
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: lockTaken) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b ? l : o')
                                            ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'b ? l : o')
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                Next (Regular) Block[B9]
                                    Finalizing: {R5}
                                    Leaving: {R4} {R3} {R2} {R1}
                        }
                        .finally {R5}
                        {
                            Block[B6] - Block
                                Predecessors (0)
                                Statements (0)
                                Jump if False (Regular) to Block[B8]
                                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'b ? l : o')
                                Next (Regular) Block[B7]
                            Block[B7] - Block
                                Predecessors: [B6]
                                Statements (1)
                                    IInvocationOperation (void System.Threading.Monitor.Exit(System.Object obj)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b ? l : o')
                                      Instance Receiver:
                                        null
                                      Arguments(1):
                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b ? l : o')
                                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'b ? l : o')
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                Next (Regular) Block[B8]
                            Block[B8] - Block
                                Predecessors: [B6] [B7]
                                Statements (0)
                                Next (StructuredExceptionHandling) Block[null]
                        }
                    }
                }
                Block[B9] - Exit
                    Predecessors: [B5]
                    Statements (0)
                """;

            var expectedDiagnostics = new[]
            {
                // (9,19): warning CS9214: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                //         lock (b ? l : o)
                Diagnostic(ErrorCode.WRN_ConvertingLock, "l").WithLocation(9, 19)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>([source, LockTypeDefinition], expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void LockFlow_LockObject_Coalesce()
        {
            var source = """
                using System;
                using System.Threading;

                class C
                {
                    void M(Lock l1, Lock l2)
                    /*<bind>*/{
                        lock (l1 ?? l2)
                        {
                        }
                    }/*</bind>*/
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1} {R2} {R3}
                .locals {R1}
                {
                    CaptureIds: [2]
                    .locals {R2}
                    {
                        CaptureIds: [1]
                        .locals {R3}
                        {
                            CaptureIds: [0]
                            Block[B1] - Block
                                Predecessors: [B0]
                                Statements (1)
                                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l1')
                                      Value:
                                        IParameterReferenceOperation: l1 (OperationKind.ParameterReference, Type: System.Threading.Lock) (Syntax: 'l1')
                                Jump if True (Regular) to Block[B3]
                                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'l1')
                                      Operand:
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l1')
                                    Leaving: {R3}
                                Next (Regular) Block[B2]
                            Block[B2] - Block
                                Predecessors: [B1]
                                Statements (1)
                                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l1')
                                      Value:
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l1')
                                Next (Regular) Block[B4]
                                    Leaving: {R3}
                        }
                        Block[B3] - Block
                            Predecessors: [B1]
                            Statements (1)
                                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l2')
                                  Value:
                                    IParameterReferenceOperation: l2 (OperationKind.ParameterReference, Type: System.Threading.Lock) (Syntax: 'l2')
                            Next (Regular) Block[B4]
                        Block[B4] - Block
                            Predecessors: [B2] [B3]
                            Statements (1)
                                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l1 ?? l2')
                                  Value:
                                    IInvocationOperation ( System.Threading.Lock.Scope System.Threading.Lock.EnterScope()) (OperationKind.Invocation, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'l1 ?? l2')
                                      Instance Receiver:
                                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l1 ?? l2')
                                      Arguments(0)
                            Next (Regular) Block[B5]
                                Leaving: {R2}
                                Entering: {R4} {R5}
                    }
                    .try {R4, R5}
                    {
                        Block[B5] - Block
                            Predecessors: [B4]
                            Statements (0)
                            Next (Regular) Block[B7]
                                Finalizing: {R6}
                                Leaving: {R5} {R4} {R1}
                    }
                    .finally {R6}
                    {
                        Block[B6] - Block
                            Predecessors (0)
                            Statements (1)
                                IInvocationOperation ( void System.Threading.Lock.Scope.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'l1 ?? l2')
                                  Instance Receiver:
                                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'l1 ?? l2')
                                  Arguments(0)
                            Next (StructuredExceptionHandling) Block[null]
                    }
                }
                Block[B7] - Exit
                    Predecessors: [B5]
                    Statements (0)
                """;

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>([source, LockTypeDefinition], expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void LockFlow_LockObject_MissingEnterScope()
        {
            var source = """
                using System;
                using System.Threading;

                class C
                {
                    void M()
                    /*<bind>*/{
                        Lock l = new Lock();
                        lock (l)
                        {
                        }
                    }/*</bind>*/
                }

                namespace System.Threading
                {
                    public class Lock { }
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1}
                .locals {R1}
                {
                    Locals: [System.Threading.Lock l]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (2)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Left:
                                ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Right:
                                IObjectCreationOperation (Constructor: System.Threading.Lock..ctor()) (OperationKind.ObjectCreation, Type: System.Threading.Lock) (Syntax: 'new Lock()')
                                  Arguments(0)
                                  Initializer:
                                    null
                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'lock (l) ... }')
                              Expression:
                                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'l')
                                  Children(1):
                                      ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock, IsInvalid) (Syntax: 'l')
                        Next (Regular) Block[B2]
                            Leaving: {R1}
                }
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """;

            var expectedDiagnostics = new[]
            {
                // (9,15): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
                //         lock (l)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(9, 15)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void LockFlow_LockObject_MissingEnterScope_NonEmptyBody()
        {
            var source = """
                using System;
                using System.Threading;

                class C
                {
                    void M()
                    /*<bind>*/{
                        Lock l = new Lock();
                        lock (l)
                        {
                            Console.Write("Body");
                        }
                    }/*</bind>*/
                }

                namespace System.Threading
                {
                    public class Lock { }
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1}
                .locals {R1}
                {
                    Locals: [System.Threading.Lock l]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (3)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Left:
                                ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Right:
                                IObjectCreationOperation (Constructor: System.Threading.Lock..ctor()) (OperationKind.ObjectCreation, Type: System.Threading.Lock) (Syntax: 'new Lock()')
                                  Arguments(0)
                                  Initializer:
                                    null
                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'lock (l) ... }')
                              Expression:
                                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'l')
                                  Children(1):
                                      ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock, IsInvalid) (Syntax: 'l')
                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Write("Body");')
                              Expression:
                                IInvocationOperation (void System.Console.Write(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Write("Body")')
                                  Instance Receiver:
                                    null
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Body"')
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Body") (Syntax: '"Body"')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Next (Regular) Block[B2]
                            Leaving: {R1}
                }
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """;

            var expectedDiagnostics = new[]
            {
                // (9,15): error CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
                //         lock (l)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock", "EnterScope").WithLocation(9, 15)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void LockFlow_LockObject_MissingScopeDispose()
        {
            var source = """
                using System;
                using System.Threading;

                class C
                {
                    void M()
                    /*<bind>*/{
                        Lock l = new Lock();
                        lock (l)
                        {
                        }
                    }/*</bind>*/
                }

                namespace System.Threading
                {
                    public class Lock
                    {
                        public Scope EnterScope() => new Scope();

                        public ref struct Scope { }
                    }
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1}
                .locals {R1}
                {
                    Locals: [System.Threading.Lock l]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (2)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Left:
                                ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new Lock()')
                              Right:
                                IObjectCreationOperation (Constructor: System.Threading.Lock..ctor()) (OperationKind.ObjectCreation, Type: System.Threading.Lock) (Syntax: 'new Lock()')
                                  Arguments(0)
                                  Initializer:
                                    null
                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'lock (l) ... }')
                              Expression:
                                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'l')
                                  Children(1):
                                      ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock, IsInvalid) (Syntax: 'l')
                        Next (Regular) Block[B2]
                            Leaving: {R1}
                }
                Block[B2] - Exit
                    Predecessors: [B1]
                    Statements (0)
                """;

            var expectedDiagnostics = new[]
            {
                // (9,15): error CS0656: Missing compiler required member 'System.Threading.Lock+Scope.Dispose'
                //         lock (l)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "l").WithArguments("System.Threading.Lock+Scope", "Dispose").WithLocation(9, 15)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void LockFlow_LockObject_EnterScopeVirtual()
        {
            var source = """
                using System;
                using System.Threading;

                class C
                {
                    void M()
                    /*<bind>*/{
                        Lock l = new LockDerived();
                        lock (l)
                        {
                        }
                    }/*</bind>*/
                }

                namespace System.Threading
                {
                    public class Lock
                    {
                        public virtual Scope EnterScope() => new();

                        public ref struct Scope
                        {
                            public void Dispose() { }
                        }
                    }

                    public class LockDerived : Lock
                    {
                        public override Scope EnterScope() => new();
                    }
                }
                """;

            var expectedFlowGraph = """
                Block[B0] - Entry
                    Statements (0)
                    Next (Regular) Block[B1]
                        Entering: {R1}
                .locals {R1}
                {
                    Locals: [System.Threading.Lock l]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new LockDerived()')
                              Left:
                                ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Threading.Lock, IsImplicit) (Syntax: 'l = new LockDerived()')
                              Right:
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Threading.Lock, IsImplicit) (Syntax: 'new LockDerived()')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                    (ImplicitReference)
                                  Operand:
                                    IObjectCreationOperation (Constructor: System.Threading.LockDerived..ctor()) (OperationKind.ObjectCreation, Type: System.Threading.LockDerived) (Syntax: 'new LockDerived()')
                                      Arguments(0)
                                      Initializer:
                                        null
                        Next (Regular) Block[B2]
                            Entering: {R2}
                    .locals {R2}
                    {
                        CaptureIds: [0]
                        Block[B2] - Block
                            Predecessors: [B1]
                            Statements (1)
                                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l')
                                  Value:
                                    IInvocationOperation (virtual System.Threading.Lock.Scope System.Threading.Lock.EnterScope()) (OperationKind.Invocation, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'l')
                                      Instance Receiver:
                                        ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Threading.Lock) (Syntax: 'l')
                                      Arguments(0)
                            Next (Regular) Block[B3]
                                Entering: {R3} {R4}
                        .try {R3, R4}
                        {
                            Block[B3] - Block
                                Predecessors: [B2]
                                Statements (0)
                                Next (Regular) Block[B5]
                                    Finalizing: {R5}
                                    Leaving: {R4} {R3} {R2} {R1}
                        }
                        .finally {R5}
                        {
                            Block[B4] - Block
                                Predecessors (0)
                                Statements (1)
                                    IInvocationOperation ( void System.Threading.Lock.Scope.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'l')
                                      Instance Receiver:
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Threading.Lock.Scope, IsImplicit) (Syntax: 'l')
                                      Arguments(0)
                                Next (StructuredExceptionHandling) Block[null]
                        }
                    }
                }
                Block[B5] - Exit
                    Predecessors: [B3]
                    Statements (0)
                """;

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
