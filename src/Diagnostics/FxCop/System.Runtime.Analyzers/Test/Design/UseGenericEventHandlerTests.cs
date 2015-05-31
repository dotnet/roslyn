// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public class UseGenericEventHandlerTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicCA1003DiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpCA1003DiagnosticAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestAlreadyUsingGenericEventHandlerCSharp()
        {
            VerifyCSharp(@"
public class C
{
    public event System.EventHandler<System.EventArgs> E1;
    public event System.EventHandler E2;
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestAlreadyUsingGenericEventHandlerBasic()
        {
            VerifyBasic(@"
Public Class C
    Public Event E1 As System.EventHandler(Of System.EventArgs)
    Public Event E2 As System.EventHandler
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestUsingStructAsEventArgsForOptimizationCSharp()
        {
            VerifyCSharp(@"
public struct SpecialCaseStructEventArgs
{
}

public struct BadArgs
{
}

public class C
{
    public event System.EventHandler<SpecialCaseStructEventArgs> E1;
    public event System.EventHandler<BadArgs> E2;
}
",
                GetCA1003CSharpResultAt(13, 47));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestUsingStructAsEventArgsForOptimizationBasic()
        {
            VerifyBasic(@"
Public Structure SpecialCaseStructEventArgs
End Structure

Public Structure BadArgs
End Structure

Public Class C
    Public Event E1 As System.EventHandler(Of SpecialCaseStructEventArgs)
    Public Event E2 As System.EventHandler(Of BadArgs)
End Class
",
                GetCA1003BasicResultAt(10, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestGeneratedEventHandlersBasic()
        {
            VerifyBasic(@"
Public Class C
    Public Event E1()
    Public Event E2(args As System.EventArgs)
    Public Event E3(sender As Object)
    Public Event E4(sender As Object, args As Integer)
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestNonPublicEvent()
        {
            VerifyCSharp(@"
public delegate void BadEventHandler(object senderId, System.EventArgs e);

public class EventsClass
{
    internal event BadEventHandler E;
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestPublicEventInNonPublicType()
        {
            VerifyCSharp(@"
public delegate void BadEventHandler(object senderId, EventArgs e);

internal class EventsClass
{
    public event BadEventHandler E;
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestOverrideEvent()
        {
            VerifyCSharp(@"
public delegate void BadHandler();

public class C
{
    public virtual event BadHandler E;
}

public class D : C
{
    public override event BadHandler E;
}
",
                GetCA1003CSharpResultAt(6, 37));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestComSourceInterfaceEvent()
        {
            VerifyCSharp(@"
public delegate void BadHandler();

[System.Runtime.InteropServices.ComSourceInterfaces(""C"")]
public class C
{
    public event BadHandler E;
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestViolatingEventsCSharp()
        {
            VerifyCSharp(@"
using System;

public delegate void BadHandler1();
public delegate int BadHandler2();
public delegate void BadHandler3(object sender);
public delegate void BadHandler4(EventArgs args);
public delegate void BadHandler5(object sender, EventArgs args);

public class C
{
    public event BadHandler1 E1;
    public event BadHandler2 E2;
    public event BadHandler3 E3;
    public event BadHandler4 E4;
    public event BadHandler5 E5;
    public event EventHandler<int> E6;
}
",
                GetCA1003CSharpResultAt(12, 30),
                GetCA1003CSharpResultAt(13, 30),
                GetCA1003CSharpResultAt(14, 30),
                GetCA1003CSharpResultAt(15, 30),
                GetCA1003CSharpResultAt(16, 30),
                GetCA1003CSharpResultAt(17, 36));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestViolatingEventsBasic()
        {
            VerifyBasic(@"
Public Delegate Sub BadHandler(sender As Object, args As System.EventArgs)

Public Class C
    Public Event E1 As BadHandler
    Public Event E2(sender As Object, e As System.EventArgs)
    Public Event E3(sender As Object, e As MyEventArgs)
End Class

Public Structure MyEventArgs
End Structure
",
                GetCA1003BasicResultAt(5, 18),
                GetCA1003BasicResultAt(6, 18),
                GetCA1003BasicResultAt(7, 18));
        }

        private static DiagnosticResult GetCA1003BasicResultAt(int line, int col)
        {
            return GetBasicResultAt(line, col, UseGenericEventHandler.RuleId, SystemRuntimeAnalyzersResources.UseGenericEventHandlerInstances);
        }

        private static DiagnosticResult GetCA1003CSharpResultAt(int line, int col)
        {
            return GetCSharpResultAt(line, col, UseGenericEventHandler.RuleId, SystemRuntimeAnalyzersResources.UseGenericEventHandlerInstances);
        }
    }
}
