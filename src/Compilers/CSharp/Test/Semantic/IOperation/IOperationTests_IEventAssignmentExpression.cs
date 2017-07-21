// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
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
IEventAssignmentExpression (EventAdd)) (OperationKind.EventAssignmentExpression, Type: System.Void) (Syntax: 't.MyEvent += Handler')
  Event Reference: IEventReferenceExpression: event System.EventHandler Test.MyEvent (OperationKind.EventReferenceExpression, Type: System.EventHandler) (Syntax: 't.MyEvent')
      Instance Receiver: ILocalReferenceExpression: t (OperationKind.LocalReferenceExpression, Type: Test) (Syntax: 't')
  Handler: IMethodBindingExpression: void C.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodBindingExpression, Type: System.EventHandler) (Syntax: 'Handler')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'Handler')
";
            var expectedDiagnostics = new[] { 
                // file.cs(6,31): warning CS0067: The event 'Test.MyEvent' is never used
                //     public event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IEventAssignmentExpression (EventRemove)) (OperationKind.EventAssignmentExpression, Type: System.Void) (Syntax: 't.MyEvent -= null')
  Event Reference: IEventReferenceExpression: event System.EventHandler Test.MyEvent (OperationKind.EventReferenceExpression, Type: System.EventHandler) (Syntax: 't.MyEvent')
      Instance Receiver: ILocalReferenceExpression: t (OperationKind.LocalReferenceExpression, Type: Test) (Syntax: 't')
  Handler: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.EventHandler, Constant: null) (Syntax: 'null')
      ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new[] { 
                // file.cs(6,31): warning CS0067: The event 'Test.MyEvent' is never used
                //     public event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IEventAssignmentExpression (EventAdd)) (OperationKind.EventAssignmentExpression, Type: System.Void) (Syntax: 'Test.MyEvent += Handler')
  Event Reference: IEventReferenceExpression: event System.EventHandler Test.MyEvent (Static) (OperationKind.EventReferenceExpression, Type: System.EventHandler) (Syntax: 'Test.MyEvent')
      Instance Receiver: null
  Handler: IMethodBindingExpression: void C.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodBindingExpression, Type: System.EventHandler) (Syntax: 'Handler')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'Handler')
";
            var expectedDiagnostics = new[] { 
                // file.cs(6,38): warning CS0067: The event 'Test.MyEvent' is never used
                //     public static event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IEventAssignmentExpression (EventRemove)) (OperationKind.EventAssignmentExpression, Type: System.Void) (Syntax: 'Test.MyEvent -= Handler')
  Event Reference: IEventReferenceExpression: event System.EventHandler Test.MyEvent (Static) (OperationKind.EventReferenceExpression, Type: System.EventHandler) (Syntax: 'Test.MyEvent')
      Instance Receiver: null
  Handler: IMethodBindingExpression: void C.Handler(System.Object sender, System.EventArgs e) (OperationKind.MethodBindingExpression, Type: System.EventHandler) (Syntax: 'Handler')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'Handler')
";
            var expectedDiagnostics = new[] { 
                // file.cs(6,38): warning CS0067: The event 'Test.MyEvent' is never used
                //     public static event EventHandler MyEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "MyEvent").WithArguments("Test.MyEvent").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IEventAssignmentExpression (EventAdd)) (OperationKind.EventAssignmentExpression, Type: System.Void, IsInvalid) (Syntax: 't.MyEvent += Handler')
  Event Reference: IEventReferenceExpression: event System.EventHandler Test.MyEvent (OperationKind.EventReferenceExpression, Type: System.EventHandler, IsInvalid) (Syntax: 't.MyEvent')
      Instance Receiver: ILocalReferenceExpression: t (OperationKind.LocalReferenceExpression, Type: Test, IsInvalid) (Syntax: 't')
  Handler: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.EventHandler, IsInvalid) (Syntax: 'Handler')
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Handler')
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
    }
}
