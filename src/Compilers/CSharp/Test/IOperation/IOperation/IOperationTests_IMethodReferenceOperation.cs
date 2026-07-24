// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.IOperation)]
    public class IOperationTests_IMethodReferenceOperation : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void MethodReference_NoControlFlow()
        {
            // Verify method references with different kinds of instance references.
            string source = @"
class C
{
    public virtual int M1() => 0;
    public static int M2() => 0;
    void M(C c, System.Func<int> m1, System.Func<int> m2, System.Func<int> m3)
    /*<bind>*/{
        m1 = this.M1;
        m2 = c.M1;
        m3 = M2;
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'm1 = this.M1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func<System.Int32>) (Syntax: 'm1 = this.M1')
              Left: 
                IParameterReferenceOperation: m1 (OperationKind.ParameterReference, Type: System.Func<System.Int32>) (Syntax: 'm1')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32>, IsImplicit) (Syntax: 'this.M1')
                  Target: 
                    IMethodReferenceOperation: System.Int32 C.M1() (IsVirtual) (OperationKind.MethodReference, Type: null) (Syntax: 'this.M1')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'm2 = c.M1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func<System.Int32>) (Syntax: 'm2 = c.M1')
              Left: 
                IParameterReferenceOperation: m2 (OperationKind.ParameterReference, Type: System.Func<System.Int32>) (Syntax: 'm2')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32>, IsImplicit) (Syntax: 'c.M1')
                  Target: 
                    IMethodReferenceOperation: System.Int32 C.M1() (IsVirtual) (OperationKind.MethodReference, Type: null) (Syntax: 'c.M1')
                      Instance Receiver: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'm3 = M2;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func<System.Int32>) (Syntax: 'm3 = M2')
              Left: 
                IParameterReferenceOperation: m3 (OperationKind.ParameterReference, Type: System.Func<System.Int32>) (Syntax: 'm3')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32>, IsImplicit) (Syntax: 'M2')
                  Target: 
                    IMethodReferenceOperation: System.Int32 C.M2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'M2')
                      Instance Receiver: 
                        null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void MethodReference_ControlFlowInReceiver()
        {
            string source = @"
class C
{
    public int M1() => 0;
    void M(C c1, C c2, System.Func<int> m)
    /*<bind>*/{
        m = (c1 ?? c2).M1;
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'm')
              Value: 
                IParameterReferenceOperation: m (OperationKind.ParameterReference, Type: System.Func<System.Int32>) (Syntax: 'm')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'm = (c1 ?? c2).M1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func<System.Int32>) (Syntax: 'm = (c1 ?? c2).M1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Func<System.Int32>, IsImplicit) (Syntax: 'm')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32>, IsImplicit) (Syntax: '(c1 ?? c2).M1')
                      Target: 
                        IMethodReferenceOperation: System.Int32 C.M1() (OperationKind.MethodReference, Type: null) (Syntax: '(c1 ?? c2).M1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1 ?? c2')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void MethodReference_ControlFlowInReceiver_StaticMethod()
        {
            string source = @"
class C
{
    public static int M1() => 0;
    void M(C c1, C c2, System.Func<int> m1, System.Func<int> m2)
    /*<bind>*/{
        m1 = c1.M1;
        m2 = (c1 ?? c2).M1;
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'm1 = c1.M1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func<System.Int32>, IsInvalid) (Syntax: 'm1 = c1.M1')
              Left: 
                IParameterReferenceOperation: m1 (OperationKind.ParameterReference, Type: System.Func<System.Int32>) (Syntax: 'm1')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32>, IsInvalid, IsImplicit) (Syntax: 'c1.M1')
                  Target: 
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'c1.M1')
                      Children(1):
                          IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c1')

    Next (Regular) Block[B2]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'm2')
              Value: 
                IParameterReferenceOperation: m2 (OperationKind.ParameterReference, Type: System.Func<System.Int32>) (Syntax: 'm2')

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c1')

            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'c1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'c1')
                Leaving: {R2}

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'c1')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B3]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c2')

        Next (Regular) Block[B6]
    Block[B6] - Block
        Predecessors: [B4] [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'm2 = (c1 ?? c2).M1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func<System.Int32>, IsInvalid) (Syntax: 'm2 = (c1 ?? c2).M1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Func<System.Int32>, IsImplicit) (Syntax: 'm2')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32>, IsInvalid, IsImplicit) (Syntax: '(c1 ?? c2).M1')
                      Target: 
                        IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '(c1 ?? c2).M1')
                          Children(1):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'c1 ?? c2')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,14): error CS0176: Member 'C.M1()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         m1 = c1.M1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "c1.M1").WithArguments("C.M1()").WithLocation(7, 14),
                // file.cs(8,14): error CS0176: Member 'C.M1()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         m2 = (c1 ?? c2).M1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "(c1 ?? c2).M1").WithArguments("C.M1()").WithLocation(8, 14)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, WorkItem(51715, "https://github.com/dotnet/roslyn/issues/51715")]
        public void MethodReference_LocalFunction_01()
        {
            var code = @"
using System;
Action a = /*<bind>*/localFunction/*</bind>*/;

void localFunction() {}
";

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedTree = @"
IMethodReferenceOperation: void localFunction() (OperationKind.MethodReference, Type: null) (Syntax: 'localFunction')
  Instance Receiver:
    null";

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(code, expectedTree, expectedDiagnostics);
        }

        [Fact, WorkItem(51715, "https://github.com/dotnet/roslyn/issues/51715")]
        public void MethodReference_LocalFunction_02()
        {
            var code = @"
using System;
Action a = /*<bind>*/localFunction/*</bind>*/;

static void localFunction() {}
";

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedTree = @"
IMethodReferenceOperation: void localFunction() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'localFunction')
  Instance Receiver:
    null";

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(code, expectedTree, expectedDiagnostics);
        }

        [Fact, WorkItem(51715, "https://github.com/dotnet/roslyn/issues/51715")]
        public void MethodReference_LocalFunction_03()
        {
            var code = @"
using System;
int local = 1;
Func<int> a = /*<bind>*/localFunction/*</bind>*/;

int localFunction() => local;
";

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedTree = @"
IMethodReferenceOperation: System.Int32 localFunction() (OperationKind.MethodReference, Type: null) (Syntax: 'localFunction')
  Instance Receiver:
    null";

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(code, expectedTree, expectedDiagnostics);
        }

        [Fact, WorkItem(51715, "https://github.com/dotnet/roslyn/issues/51715")]
        public void MethodReference_LocalFunction_04()
        {
            var code = @"
using System;
int local = 1;
Func<int> a = /*<bind>*/localFunction/*</bind>*/;

static int localFunction() => local;
";

            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // (6,31): error CS8421: A static local function cannot contain a reference to 'local'.
                // static int localFunction() => local;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "local").WithArguments("local").WithLocation(6, 31)
            };

            var expectedTree = @"
IMethodReferenceOperation: System.Int32 localFunction() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'localFunction')
  Instance Receiver:
    null";

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(code, expectedTree, expectedDiagnostics);
        }
    }
}
