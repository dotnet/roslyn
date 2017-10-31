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
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
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
    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
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
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
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
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
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
              IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'Handler')
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
        null
  Handler: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler, IsImplicit) (Syntax: 'Handler')
      Target: 
        IMethodReferenceOperation: void C.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodReference, Type: null) (Syntax: 'Handler')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
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
        IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Test')
  Handler: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler, IsImplicit) (Syntax: 'Handler')
      Target: 
        IMethodReferenceOperation: void C.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodReference, Type: null) (Syntax: 'Handler')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Handler')
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
        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Test, IsImplicit) (Syntax: 'MyEvent')
  Handler: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler, IsImplicit) (Syntax: 'Handler')
      Target: 
        IMethodReferenceOperation: void Test.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodReference, Type: null) (Syntax: 'Handler')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Test, IsImplicit) (Syntax: 'Handler')
";
            var expectedDiagnostics = new[] {
                      // file.cs(6,31): warning CS0067: The event 'Test.MyEvent' is never used
                      //     public event EventHandler MyEvent;
                      Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
