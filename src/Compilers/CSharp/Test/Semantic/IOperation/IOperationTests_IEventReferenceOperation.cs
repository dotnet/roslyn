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

    }
}
