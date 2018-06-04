// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void AddEventHandler()
        {
            string source = @"
using System;

class Test
{
    public event EventHandler MyEvent;   
}

class C
{
    void Handler(object sender, EventArgs e)
    {
    } 

    void M()
    {
        var t = new Test();
        /*<bind>*/t.MyEvent += Handler/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void) (Syntax: 't.MyEvent += Handler')
  Event Reference: 
    IEventReferenceOperation: event System.EventHandler Test.MyEvent (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 't.MyEvent')
      Instance Receiver: 
        ILocalReferenceOperation: t (OperationKind.LocalReference, Type: Test) (Syntax: 't')
  Handler: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler, IsImplicit) (Syntax: 'Handler')
      Target: 
        IMethodReferenceOperation: void C.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodReference, Type: null) (Syntax: 'Handler')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
";
            var expectedDiagnostics = new[] {
                // file.cs(6,31): warning CS0067: The event 'Test.MyEvent' is never used
                //     public event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void AddEventHandler_JustHandlerReturnsMethodReference()
        {
            string source = @"
using System;

class Test
{
    public event EventHandler MyEvent;
}

class C
{
    void Handler(object sender, EventArgs e)
    {
    }

    void M()
    {
        var t = new Test();
        t.MyEvent += /*<bind>*/Handler/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IMethodReferenceOperation: void C.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodReference, Type: null) (Syntax: 'Handler')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0067: The event 'Test.MyEvent' is never used
                //     public event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void RemoveEventHandler()
        {
            string source = @"
using System;

class Test
{
    public event EventHandler MyEvent;   
}

class C
{
    void M()
    {
        var t = new Test();
        /*<bind>*/t.MyEvent -= null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: System.Void) (Syntax: 't.MyEvent -= null')
  Event Reference: 
    IEventReferenceOperation: event System.EventHandler Test.MyEvent (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 't.MyEvent')
      Instance Receiver: 
        ILocalReferenceOperation: t (OperationKind.LocalReference, Type: Test) (Syntax: 't')
  Handler: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.EventHandler, Constant: null, IsImplicit) (Syntax: 'null')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new[] {
                // file.cs(6,31): warning CS0067: The event 'Test.MyEvent' is never used
                //     public event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void AddEventHandler_StaticEvent()
        {
            string source = @"
using System;

class Test
{
    public static event EventHandler MyEvent;    
}

class C
{
    void Handler(object sender, EventArgs e)
    {
    } 

    void M()
    {
        /*<bind>*/Test.MyEvent += Handler/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void) (Syntax: 'Test.MyEvent += Handler')
  Event Reference: 
    IEventReferenceOperation: event System.EventHandler Test.MyEvent (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'Test.MyEvent')
      Instance Receiver: 
        null
  Handler: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler, IsImplicit) (Syntax: 'Handler')
      Target: 
        IMethodReferenceOperation: void C.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodReference, Type: null) (Syntax: 'Handler')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
";
            var expectedDiagnostics = new[] {
                // file.cs(6,38): warning CS0067: The event 'Test.MyEvent' is never used
                //     public static event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void RemoveEventHandler_StaticEvent()
        {
            string source = @"
using System;

class Test
{
    public static event EventHandler MyEvent;    
}

class C
{
    void Handler(object sender, EventArgs e)
    {
    } 

    void M()
    {
        /*<bind>*/Test.MyEvent -= Handler/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: System.Void) (Syntax: 'Test.MyEvent -= Handler')
  Event Reference: 
    IEventReferenceOperation: event System.EventHandler Test.MyEvent (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'Test.MyEvent')
      Instance Receiver: 
        null
  Handler: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler, IsImplicit) (Syntax: 'Handler')
      Target: 
        IMethodReferenceOperation: void C.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodReference, Type: null) (Syntax: 'Handler')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
";
            var expectedDiagnostics = new[] {
                // file.cs(6,38): warning CS0067: The event 'Test.MyEvent' is never used
                //     public static event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void AddEventHandler_DelegateTypeMismatch()
        {
            string source = @"
using System;

class Test
{
    public event EventHandler MyEvent;    
}

class C
{
    void Handler(object sender)
    {
    } 

    void M()
    {
        var t = new Test();
        /*<bind>*/t.MyEvent += Handler/*<bind>*/;
    }
}
";
            string expectedOperationTree = @"
IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void, IsInvalid) (Syntax: 't.MyEvent += Handler')
  Event Reference: 
    IEventReferenceOperation: event System.EventHandler Test.MyEvent (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 't.MyEvent')
      Instance Receiver: 
        ILocalReferenceOperation: t (OperationKind.LocalReference, Type: Test, IsInvalid) (Syntax: 't')
  Handler: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler, IsInvalid, IsImplicit) (Syntax: 'Handler')
      Target: 
        IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Handler')
          Children(1):
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'Handler')
";
            var expectedDiagnostics = new[] {
                // file.cs(18,19): error CS0123: No overload for 'Handler' matches delegate 'EventHandler'
                //         /*<bind>*/t.MyEvent += Handler/*<bind>*/;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "t.MyEvent += Handler").WithArguments("Handler", "System.EventHandler").WithLocation(18, 19),
                // file.cs(6,31): warning CS0067: The event 'Test.MyEvent' is never used
                //     public event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void AddEventHandler_AssignToStaticEventOnInstance()
        {
            string source = @"
using System;

class Test
{
    public static event EventHandler MyEvent;    
}

class C
{
    void Handler(object sender, EventArgs e)
    {
    } 

    void M()
    {
        var t = new Test();
        /*<bind>*/t.MyEvent += Handler/*<bind>*/;
    }
}
";
            string expectedOperationTree = @"
IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void, IsInvalid) (Syntax: 't.MyEvent += Handler')
  Event Reference: 
    IEventReferenceOperation: event System.EventHandler Test.MyEvent (Static) (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 't.MyEvent')
      Instance Receiver: 
        ILocalReferenceOperation: t (OperationKind.LocalReference, Type: Test, IsInvalid) (Syntax: 't')
  Handler: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler, IsImplicit) (Syntax: 'Handler')
      Target: 
        IMethodReferenceOperation: void C.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodReference, Type: null) (Syntax: 'Handler')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
";
            var expectedDiagnostics = new[] {
                // file.cs(18,19): error CS0176: Member 'Test.MyEvent' cannot be accessed with an instance reference; qualify it with a type name instead
                //         /*<bind>*/t.MyEvent += Handler/*<bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "t.MyEvent").WithArguments("Test.MyEvent").WithLocation(18, 19),
                // file.cs(6,38): warning CS0067: The event 'Test.MyEvent' is never used
                //     public static event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        [WorkItem(8909, "https://github.com/dotnet/roslyn/issues/8909")]
        public void AddEventHandler_AssignToNonStaticEventOnType()
        {
            string source = @"
using System;

class Test
{
    public event EventHandler MyEvent;    
}

class C
{
    void Handler(object sender, EventArgs e)
    {
    } 

    void M()
    {
        /*<bind>*/Test.MyEvent += Handler/*<bind>*/;
    }
}
";
            string expectedOperationTree = @"
IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void, IsInvalid) (Syntax: 'Test.MyEvent += Handler')
  Event Reference: 
    IEventReferenceOperation: event System.EventHandler Test.MyEvent (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 'Test.MyEvent')
      Instance Receiver: 
        null
  Handler: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler, IsImplicit) (Syntax: 'Handler')
      Target: 
        IMethodReferenceOperation: void C.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodReference, Type: null) (Syntax: 'Handler')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
";
            var expectedDiagnostics = new[] {
                // file.cs(17,19): error CS0120: An object reference is required for the non-static field, method, or property 'Test.MyEvent'
                //         /*<bind>*/Test.MyEvent += Handler/*<bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Test.MyEvent").WithArguments("Test.MyEvent").WithLocation(17, 19),
                // file.cs(6,31): warning CS0067: The event 'Test.MyEvent' is never used
                //     public event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void AddEventHandler_AssignToEventWithoutExplicitReceiver()
        {
            string source = @"
using System;

class Test
{
    public event EventHandler MyEvent;  

    void Handler(object sender, EventArgs e)
    {
    } 

    void M()
    {
        /*<bind>*/MyEvent += Handler/*<bind>*/;
    }  
}
";
            string expectedOperationTree = @"
IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void) (Syntax: 'MyEvent += Handler')
  Event Reference: 
    IEventReferenceOperation: event System.EventHandler Test.MyEvent (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'MyEvent')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Test, IsImplicit) (Syntax: 'MyEvent')
  Handler: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler, IsImplicit) (Syntax: 'Handler')
      Target: 
        IMethodReferenceOperation: void Test.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodReference, Type: null) (Syntax: 'Handler')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Test, IsImplicit) (Syntax: 'Handler')
";
            var expectedDiagnostics = new[] {
                      // file.cs(6,31): warning CS0067: The event 'Test.MyEvent' is never used
                      //     public event EventHandler MyEvent;
                      Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void EventAssignment_NoControlFlow()
        {
            string source = @"
using System;

class C
{
#pragma warning disable CS0067 // Event is unused.
    public event EventHandler MyEvent;

    void M(C c, EventHandler handler1, EventHandler handler2)
    /*<bind>*/{
        c.MyEvent += handler1;
        MyEvent -= handler2;
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c.MyEvent += handler1;')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void) (Syntax: 'c.MyEvent += handler1')
              Event Reference: 
                IEventReferenceOperation: event System.EventHandler C.MyEvent (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'c.MyEvent')
                  Instance Receiver: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
              Handler: 
                IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'MyEvent -= handler2;')
          Expression: 
            IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: System.Void) (Syntax: 'MyEvent -= handler2')
              Event Reference: 
                IEventReferenceOperation: event System.EventHandler C.MyEvent (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'MyEvent')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'MyEvent')
              Handler: 
                IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

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
        public void EventAssignment_ControlFlowInEventReference()
        {
            string source = @"
using System;

class C
{
#pragma warning disable CS0067 // Event is unused.
    public event EventHandler MyEvent;

    void M(C c1, C c2, EventHandler handler)
    /*<bind>*/
    {
        (c1 ?? c2).MyEvent += handler;
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
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
          Value: 
            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
          Value: 
            IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(c1 ?? c2). ... += handler;')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void) (Syntax: '(c1 ?? c2). ...  += handler')
              Event Reference: 
                IEventReferenceOperation: event System.EventHandler C.MyEvent (OperationKind.EventReference, Type: System.EventHandler) (Syntax: '(c1 ?? c2).MyEvent')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1 ?? c2')
              Handler: 
                IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void EventAssignment_ControlFlowInEventReference_StaticEvent()
        {
            string source = @"
using System;

class C
{
#pragma warning disable CS0067 // Event is unused.
    public static event EventHandler MyEvent;

    void M(C c1, C c2, EventHandler handler)
    /*<bind>*/
    {
        (c1 ?? c2).MyEvent += handler;
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '(c1 ?? c2). ... += handler;')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void, IsInvalid) (Syntax: '(c1 ?? c2). ...  += handler')
              Event Reference: 
                IEventReferenceOperation: event System.EventHandler C.MyEvent (Static) (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: '(c1 ?? c2).MyEvent')
                  Instance Receiver: 
                    null
              Handler: 
                IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {                
                // file.cs(12,9): error CS0176: Member 'C.MyEvent' cannot be accessed with an instance reference; qualify it with a type name instead
                //         (c1 ?? c2).MyEvent += handler;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "(c1 ?? c2).MyEvent").WithArguments("C.MyEvent").WithLocation(12, 9)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void EventAssignment_ControlFlowInHandler_InstanceReceiver()
        {
            string source = @"
using System;

class C
{
#pragma warning disable CS0067 // Event is unused.
    public event EventHandler MyEvent;

    void M(EventHandler handler1, EventHandler handler2)
    /*<bind>*/
    {
        MyEvent -= handler1 ?? handler2;
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
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'MyEvent')
          Value: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'MyEvent')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
          Value: 
            IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'handler1')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler2')
          Value: 
            IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'MyEvent -=  ... ? handler2;')
          Expression: 
            IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: System.Void) (Syntax: 'MyEvent -=  ... ?? handler2')
              Event Reference: 
                IEventReferenceOperation: event System.EventHandler C.MyEvent (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'MyEvent')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'MyEvent')
              Handler: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1 ?? handler2')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void EventAssignment_ControlFlowInHandler_NullReceiver()
        {
            string source = @"
using System;

class C
{
#pragma warning disable CS0067 // Event is unused.
    public static event EventHandler MyEvent;

    void M(EventHandler handler1, EventHandler handler2)
    /*<bind>*/
    {
        MyEvent -= handler1 ?? handler2;
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
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
          Value: 
            IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'handler1')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler2')
          Value: 
            IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'MyEvent -=  ... ? handler2;')
          Expression: 
            IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: System.Void) (Syntax: 'MyEvent -=  ... ?? handler2')
              Event Reference: 
                IEventReferenceOperation: event System.EventHandler C.MyEvent (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'MyEvent')
                  Instance Receiver: 
                    null
              Handler: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1 ?? handler2')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void EventAssignment_ControlFlowInEventReferenceAndHandler()
        {
            string source = @"
using System;

class C
{
#pragma warning disable CS0067 // Event is unused.
    public event EventHandler MyEvent;

    void M(C c1, C c2, EventHandler handler1, EventHandler handler2)
    /*<bind>*/
    {
        (c1 ?? c2).MyEvent += handler1 ?? handler2;
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
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
          Value: 
            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
          Value: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
          Value: 
            IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
          Value: 
            IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'handler1')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
          Value: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler2')
          Value: 
            IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(c1 ?? c2). ... ? handler2;')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void) (Syntax: '(c1 ?? c2). ... ?? handler2')
              Event Reference: 
                IEventReferenceOperation: event System.EventHandler C.MyEvent (OperationKind.EventReference, Type: System.EventHandler) (Syntax: '(c1 ?? c2).MyEvent')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1 ?? c2')
              Handler: 
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1 ?? handler2')

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void EventAssignment_NoControlFlow_NotAStatement()
        {
            string source = @"
using System;

class C
{
#pragma warning disable CS0067 // Event is unused.
    public event EventHandler MyEvent;

    void M(C c, EventHandler handler)
    /*<bind>*/{
        (c.MyEvent += handler) = 0;
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '(c.MyEvent  ... ndler) = 0;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void, IsInvalid) (Syntax: '(c.MyEvent  ... andler) = 0')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'c.MyEvent += handler')
                  Children(1):
                      IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void, IsInvalid) (Syntax: 'c.MyEvent += handler')
                        Event Reference: 
                          IEventReferenceOperation: event System.EventHandler C.MyEvent (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 'c.MyEvent')
                            Instance Receiver: 
                              IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
                        Handler: 
                          IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler, IsInvalid) (Syntax: 'handler')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {                
                // file.cs(11,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (c.MyEvent += handler) = 0;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "c.MyEvent += handler").WithLocation(11, 10)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void EventAssignment_ControlFlow_NotAStatement()
        {
            string source = @"
using System;

class C
{
#pragma warning disable CS0067 // Event is unused.
    public event EventHandler MyEvent;

    void M(C c, EventHandler handler, int? x1, int x2)
    /*<bind>*/{
        (c.MyEvent += handler) = x1 ?? x2;
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
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c.MyEvent += handler')
          Value: 
            IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'c.MyEvent += handler')
              Children(1):
                  IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: System.Void, IsInvalid) (Syntax: 'c.MyEvent += handler')
                    Event Reference: 
                      IEventReferenceOperation: event System.EventHandler C.MyEvent (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 'c.MyEvent')
                        Instance Receiver: 
                          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
                    Handler: 
                      IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler, IsInvalid) (Syntax: 'handler')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
          Value: 
            IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x1')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'x1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x1')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
          Value: 
            IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '(c.MyEvent  ... = x1 ?? x2;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void, IsInvalid) (Syntax: '(c.MyEvent  ...  = x1 ?? x2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'c.MyEvent += handler')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x1 ?? x2')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(11,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (c.MyEvent += handler) = x1 ?? x2;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "c.MyEvent += handler").WithLocation(11, 10)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
