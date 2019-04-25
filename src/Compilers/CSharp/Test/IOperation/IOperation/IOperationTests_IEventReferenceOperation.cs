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
        public void IEventReference_AddEvent_StaticEventAccessOnClass()
        {
            string source = @"
using System;
using System.Collections.Generic;

class C
{
    static event EventHandler Event;

    public static void M()
    {
        /*<bind>*/C.Event/*</bind>*/ += (sender, args) => { };
    }
}
";
            string expectedOperationTree = @"
IEventReferenceOperation: event System.EventHandler C.Event (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'C.Event')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0067: The event 'C.Event' is never used
                //     static event EventHandler Event;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("C.Event").WithLocation(7, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IEventReference_AddEvent_InstanceEventAccessOnClass()
        {
            string source = @"
using System;
using System.Collections.Generic;

class C
{
    event EventHandler Event;

    public static void M()
    {
        /*<bind>*/C.Event/*</bind>*/ += (sender, args) => { };
    }
}
";
            string expectedOperationTree = @"
IEventReferenceOperation: event System.EventHandler C.Event (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 'C.Event')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'C.Event'
                //         /*<bind>*/C.Event/*</bind>*/ += (sender, args) => { };
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.Event").WithArguments("C.Event").WithLocation(11, 19),
                // CS0067: The event 'C.Event' is never used
                //     event EventHandler Event;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("C.Event").WithLocation(7, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IEventReference_AddEvent_StaticEventWithInstanceReceiver()
        {
            string source = @"
using System;
using System.Collections.Generic;

class C
{
    static event EventHandler Event;

    public static void M()
    {
        var c = new C();
        /*<bind>*/c.Event/*</bind>*/ += (sender, args) => { };
    }
}
";
            string expectedOperationTree = @"
IEventReferenceOperation: event System.EventHandler C.Event (Static) (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 'c.Event')
  Instance Receiver: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0176: Member 'C.Event' cannot be accessed with an instance reference; qualify it with a type name instead
                //         /*<bind>*/c.Event/*</bind>*/ += (sender, args) => { };
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "c.Event").WithArguments("C.Event").WithLocation(12, 19),
                // CS0067: The event 'C.Event' is never used
                //     static event EventHandler Event;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("C.Event").WithLocation(7, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IEventReference_AccessEvent_StaticEventAccessOnClass()
        {
            string source = @"
using System;

class C
{
    static event EventHandler Event;

    public static void M()
    {
        /*<bind>*/C.Event/*</bind>*/(null, null);
    }
}
";
            string expectedOperationTree = @"
IEventReferenceOperation: event System.EventHandler C.Event (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'C.Event')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IEventReference_AccessEvent_InstanceEventAccessOnClass()
        {
            string source = @"
using System;

class C
{
    event EventHandler Event;

    public static void M()
    {
        /*<bind>*/C.Event/*</bind>*/(null, null);
    }
}
";
            string expectedOperationTree = @"
IEventReferenceOperation: event System.EventHandler C.Event (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 'C.Event')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'C.Event'
                //         /*<bind>*/C.Event/*</bind>*/(null, null);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.Event").WithArguments("C.Event").WithLocation(10, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IEventReference_AccessEvent_StaticEventWithInstanceReceiver()
        {
            string source = @"
using System;

class C
{
    static event EventHandler Event;

    public static void M()
    {
        var c = new C();
        /*<bind>*/c.Event/*</bind>*/(null, null);
    }
}
";
            string expectedOperationTree = @"
IEventReferenceOperation: event System.EventHandler C.Event (Static) (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 'c.Event')
  Instance Receiver: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0176: Member 'C.Event' cannot be accessed with an instance reference; qualify it with a type name instead
                //         /*<bind>*/c.Event/*</bind>*/(null, null);
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "c.Event").WithArguments("C.Event").WithLocation(11, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void EventReference_NoControlFlow()
        {
            // Verify event references with different kinds of instance references.
            string source = @"
using System;

class C
{
#pragma warning disable CS0067 // The event is never used
    public event EventHandler Event1;
    public static event EventHandler Event2;

    public void M(C c, EventHandler handler1, EventHandler handler2, EventHandler handler3)
    /*<bind>*/
    {
        handler1 = this.Event1;
        c.Event1 = handler2;
        handler3 = C.Event2;
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'handler1 = this.Event1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.EventHandler) (Syntax: 'handler1 = this.Event1')
              Left: 
                IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')
              Right: 
                IEventReferenceOperation: event System.EventHandler C.Event1 (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'this.Event1')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c.Event1 = handler2;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.EventHandler) (Syntax: 'c.Event1 = handler2')
              Left: 
                IEventReferenceOperation: event System.EventHandler C.Event1 (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'c.Event1')
                  Instance Receiver: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
              Right: 
                IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'handler3 = C.Event2;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.EventHandler) (Syntax: 'handler3 = C.Event2')
              Left: 
                IParameterReferenceOperation: handler3 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler3')
              Right: 
                IEventReferenceOperation: event System.EventHandler C.Event2 (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'C.Event2')
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
        public void EventReference_ControlFlowInReceiver()
        {
            string source = @"
using System;

class C
{
#pragma warning disable CS0067 // The event is never used
    private event EventHandler Event1;

    public void M(C c1, C c2, EventHandler handler)
    /*<bind>*/
    {
        handler = (c1 ?? c2).Event1;
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler')
              Value: 
                IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler')

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
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'handler = ( ... c2).Event1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.EventHandler) (Syntax: 'handler = ( ...  c2).Event1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler')
                  Right: 
                    IEventReferenceOperation: event System.EventHandler C.Event1 (OperationKind.EventReference, Type: System.EventHandler) (Syntax: '(c1 ?? c2).Event1')
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
        public void EventReference_ControlFlowInReceiver_StaticEvent()
        {
            string source = @"
using System;

class C
{
#pragma warning disable CS0067 // The event is never used
    private static event EventHandler Event1;

    public void M(C c1, C c2, EventHandler handler1, EventHandler handler2)
    /*<bind>*/
    {
        handler1 = c1.Event1;
        handler2 = (c1 ?? c2).Event1;
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'handler1 = c1.Event1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.EventHandler, IsInvalid) (Syntax: 'handler1 = c1.Event1')
              Left: 
                IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')
              Right: 
                IEventReferenceOperation: event System.EventHandler C.Event1 (Static) (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 'c1.Event1')
                  Instance Receiver: 
                    null

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'handler2 =  ... c2).Event1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.EventHandler, IsInvalid) (Syntax: 'handler2 =  ...  c2).Event1')
              Left: 
                IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')
              Right: 
                IEventReferenceOperation: event System.EventHandler C.Event1 (Static) (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: '(c1 ?? c2).Event1')
                  Instance Receiver: 
                    null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(12,20): error CS0176: Member 'C.Event1' cannot be accessed with an instance reference; qualify it with a type name instead
                //         handler1 = c1.Event1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "c1.Event1").WithArguments("C.Event1").WithLocation(12, 20),
                // file.cs(13,20): error CS0176: Member 'C.Event1' cannot be accessed with an instance reference; qualify it with a type name instead
                //         handler2 = (c1 ?? c2).Event1;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "(c1 ?? c2).Event1").WithArguments("C.Event1").WithLocation(13, 20)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
