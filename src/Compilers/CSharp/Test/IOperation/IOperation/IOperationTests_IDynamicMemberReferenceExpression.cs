// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IDynamicMemberReferenceExpression : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_SimplePropertyAccess()
        {
            string source = @"
using System;

namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            int i = /*<bind>*/d.Prop1/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicMemberReferenceOperation (Member Name: ""Prop1"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Prop1')
  Type Arguments(0)
  Instance Receiver: 
    ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_InvalidPropertyAccess()
        {
            string source = @"
using System;

namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            int i = /*<bind>*/d./*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicMemberReferenceOperation (Member Name: """", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic, IsInvalid) (Syntax: 'd./*</bind>*/')
  Type Arguments(0)
  Instance Receiver: 
    ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //             int i = /*<bind>*/d./*</bind>*/;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(11, 44)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_SimpleMethodCall()
        {
            string source = @"
using System;

namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'd.GetValue()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.GetValue')
      Type Arguments(0)
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_InvalidMethodCall_MissingName()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsInvalid) (Syntax: 'd.()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: """", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic, IsInvalid) (Syntax: 'd.')
      Type Arguments(0)
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //             /*<bind>*/d.()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(9, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_InvalidMethodCall_MissingCloseParen()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue(/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsInvalid) (Syntax: 'd.GetValue(/*</bind>*/')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.GetValue')
      Type Arguments(0)
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1026: ) expected
                //             /*<bind>*/d.GetValue(/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(9, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReference_GenericMethodCall_SingleGeneric()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue<int>()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'd.GetValue<int>()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.GetValue<int>')
      Type Arguments(1):
        Symbol: System.Int32
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReference_GenericMethodCall_MultipleGeneric()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue<int, C1>()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'd.GetValue<int, C1>()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.GetValue<int, C1>')
      Type Arguments(2):
        Symbol: System.Int32
        Symbol: ConsoleApp1.C1
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_GenericPropertyAccess()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue<int, C1>/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicMemberReferenceOperation (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic, IsInvalid) (Syntax: 'd.GetValue<int, C1>')
  Type Arguments(2):
    Symbol: System.Int32
    Symbol: ConsoleApp1.C1
  Instance Receiver: 
    ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic, IsInvalid) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0307: The property 'GetValue' cannot be used with type arguments
                //             /*<bind>*/d.GetValue<int, C1>/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "GetValue<int, C1>").WithArguments("GetValue", "property").WithLocation(9, 25),
                // CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //             /*<bind>*/d.GetValue<int, C1>/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "d.GetValue<int, C1>").WithLocation(9, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_GenericMethodCall_InvalidGenericParam()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.GetValue<int,>()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsInvalid) (Syntax: 'd.GetValue<int,>()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: ""GetValue"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic, IsInvalid) (Syntax: 'd.GetValue<int,>')
      Type Arguments(2):
        Symbol: System.Int32
        Symbol: ?
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1031: Type expected
                //             /*<bind>*/d.GetValue<int,>()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(9, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_NestedDynamicPropertyAccess()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            object o = /*<bind>*/d.Prop1.Prop2/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicMemberReferenceOperation (Member Name: ""Prop2"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Prop1.Prop2')
  Type Arguments(0)
  Instance Receiver: 
    IDynamicMemberReferenceOperation (Member Name: ""Prop1"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Prop1')
      Type Arguments(0)
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_NestedDynamicMethodAccess()
        {
            string source = @"
namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            /*<bind>*/d.Method1().Method2()/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'd.Method1().Method2()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: ""Method2"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Method1().Method2')
      Type Arguments(0)
      Instance Receiver: 
        IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'd.Method1()')
          Expression: 
            IDynamicMemberReferenceOperation (Member Name: ""Method1"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Method1')
              Type Arguments(0)
              Instance Receiver: 
                ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
          Arguments(0)
          ArgumentNames(0)
          ArgumentRefKinds(0)
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicMemberReferenceExpression_NestedDynamicPropertyAndMethodAccess()
        {
            string source = @"
using System;

namespace ConsoleApp1
{
    class C1
    {
        static void M1()
        {
            dynamic d = null;
            int i = /*<bind>*/d.Method1<int>().Prop2/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IDynamicMemberReferenceOperation (Member Name: ""Prop2"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Method1<int>().Prop2')
  Type Arguments(0)
  Instance Receiver: 
    IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'd.Method1<int>()')
      Expression: 
        IDynamicMemberReferenceOperation (Member Name: ""Method1"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Method1<int>')
          Type Arguments(1):
            Symbol: System.Int32
          Instance Receiver: 
            ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
      Arguments(0)
      ArgumentNames(0)
      ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void DynamicMemberReference_NoControlFlow()
        {
            string source = @"
using System;

namespace ConsoleApp1
{
    class C1
    {
        static void M1(dynamic d, dynamic p)
        /*<bind>*/{
            p = d.Prop1;
        }/*</bind>*/
    }
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = d.Prop1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'p = d.Prop1')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'p')
              Right: 
                IDynamicMemberReferenceOperation (Member Name: ""Prop1"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Prop1')
                  Type Arguments(0)
                  Instance Receiver: 
                    IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')

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
        public void DynamicMemberReference_ControlFlowInReceiver()
        {
            string source = @"
using System;

namespace ConsoleApp1
{
    class C1
    {
        static void M1(dynamic d1, dynamic d2, dynamic p)
        /*<bind>*/{
            p = (d1 ?? d2).Prop1;
        }/*</bind>*/
    }
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'p')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'd1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = (d1 ?? d2).Prop1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'p = (d1 ?? d2).Prop1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'p')
                  Right: 
                    IDynamicMemberReferenceOperation (Member Name: ""Prop1"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: '(d1 ?? d2).Prop1')
                      Type Arguments(0)
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'd1 ?? d2')

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
        public void DynamicMemberReference_ControlFlowInReceiver_TypeArguments()
        {
            string source = @"
using System;

namespace ConsoleApp1
{
    class C1
    {
        static void M1(dynamic d1, dynamic d2, dynamic p)
        /*<bind>*/{
            p = (d1 ?? d2).Prop1<int>;
        }/*</bind>*/
    }
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'p')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'd1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = (d1 ??  ... Prop1<int>;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic, IsInvalid) (Syntax: 'p = (d1 ??  ... .Prop1<int>')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'p')
                  Right: 
                    IDynamicMemberReferenceOperation (Member Name: ""Prop1"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic, IsInvalid) (Syntax: '(d1 ?? d2).Prop1<int>')
                      Type Arguments(1):
                        Symbol: System.Int32
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'd1 ?? d2')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(10,28): error CS0307: The property 'Prop1' cannot be used with type arguments
                //             p = (d1 ?? d2).Prop1<int>;
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "Prop1<int>").WithArguments("Prop1", "property").WithLocation(10, 28)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
