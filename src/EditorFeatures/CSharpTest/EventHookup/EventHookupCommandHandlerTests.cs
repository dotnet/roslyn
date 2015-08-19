// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EventHookup
{
    public class EventHookupCommandHandlerTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void HandlerName_EventInThisClass()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void HandlerName_EventOnLocal()
        {
            var markup = @"
class C
{
    public event System.Action MyEvent;
}

class D
{
    void M()
    {
        C local = new C();
        local.MyEvent +$$
    }
}
";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("Local_MyEvent;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void HandlerName_EventOnFieldOfObject()
        {
            var markup = @"
class C
{
    public event System.Action MyEvent;
}

class D
{
    public C cfield = new C();
}

class E
{
    void Foo()
    {
        D local = new D();
        local.cfield.MyEvent +$$
    }
}
";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("Cfield_MyEvent;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void NoHookupOnIntegerPlusEquals()
        {
            var markup = @"
class C
{
    void Foo()
    {
        int x = 7;
        x +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertNotShowing();

                // Make sure that sending the tab works correctly. Note the 4 spaces after the +=
                testState.SendTab();
                testState.WaitForAsynchronousOperations();
                var expectedCode = @"
class C
{
    void Foo()
    {
        int x = 7;
        x +=    
    }
}";

                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void HandlerName_DefaultHandlerNameAlreadyExistsWithSameNonStaticState()
        {
            var markup = @"
class C
{
    public event System.Action MyEvent;

    void Foo()
    {
        MyEvent +$$
    }

    private void C_MyEvent()
    {
    }
}
";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void HandlerName_DefaultHandlerNameAlreadyExistsWithDifferentStaticState()
        {
            var markup = @"
class C
{
    public event System.Action MyEvent;

    void Foo()
    {
        MyEvent +$$
    }

    private static void C_MyEvent()
    {
    }
}
";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void HandlerName_DefaultHandlerNameAlreadyExistsAsField()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    int C_MyEvent;

    void M(string[] args)
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void HookupInLambdaInLocalDeclaration()
        {
            var markup = @"
class C
{
    public event System.Action MyEvent;

    void Foo()
    {
        Action a = () => MyEvent +$$
    }
}
";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void TypingSpacesDoesNotDismiss()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent;");

                testState.SendTypeChar(' ');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void TypingLettersDismisses()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent;");

                testState.SendTypeChar('d');
                testState.WaitForAsynchronousOperations();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void TypingEqualsInSessionDismisses()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent;");

                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void CancelViaLeftKey()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent;");

                testState.SendTypeChar(' ');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent;");

                testState.SendLeftKey();
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent;");

                testState.SendLeftKey();
                testState.WaitForAsynchronousOperations();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void CancelViaBackspace()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent;");

                testState.SendTypeChar(' ');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent;");

                testState.SendBackspace();
                testState.WaitForAsynchronousOperations();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void EventHookupBeforeEventHookup()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$
        MyEvent += C_MyEvent;
    }

    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                testState.WaitForAsynchronousOperations();

                var expectedCode = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent += C_MyEvent1;
        MyEvent += C_MyEvent;
    }

    private void C_MyEvent1()
    {
        throw new System.NotImplementedException();
    }

    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";
                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void EventHookupBeforeComment()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$ // Awesome Comment!
        MyEvent += C_MyEvent;
    }

    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                testState.WaitForAsynchronousOperations();

                var expectedCode = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent += C_MyEvent1; // Awesome Comment!
        MyEvent += C_MyEvent;
    }

    private void C_MyEvent1()
    {
        throw new System.NotImplementedException();
    }

    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";
                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void EventHookupInArgument()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        Foo(() => MyEvent +$$)
    }

    private void Foo(Action a)
    {
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                testState.WaitForAsynchronousOperations();

                var expectedCode = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        Foo(() => MyEvent += C_MyEvent;)
    }

    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }

    private void Foo(Action a)
    {
    }
}";
                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void HookupInFieldDeclarationSingleLineLambda()
        {
            var markup = @"
class C
{
    static event System.Action MyEvent;
    System.Action A = () => MyEvent +$$
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                testState.WaitForAsynchronousOperations();

                var expectedCode = @"
class C
{
    static event System.Action MyEvent;
    System.Action A = () => MyEvent += C_MyEvent;

    private static void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";
                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void HookupInFieldDeclarationMultiLineLambda()
        {
            var markup = @"
class C
{
    static event System.Action MyEvent;
    System.Action A = () =>
    {
        MyEvent +$$
    };
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                testState.WaitForAsynchronousOperations();

                var expectedCode = @"
class C
{
    static event System.Action MyEvent;
    System.Action A = () =>
    {
        MyEvent += C_MyEvent;
    };

    private static void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";
                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void EventHookupInUnformattedPosition1()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                testState.WaitForAsynchronousOperations();

                var expectedCode = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent += C_MyEvent;
    }

    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";
                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void EventHookupInUnformattedPosition2()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {MyEvent                     +$$
        MyEvent += C_MyEvent;
    }

    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');

                for (int i = 0; i < 20; i++)
                {
                    testState.SendTypeChar(' ');
                }

                testState.SendTab();
                testState.WaitForAsynchronousOperations();

                var expectedCode = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent += C_MyEvent1;
        MyEvent += C_MyEvent;
    }

    private void C_MyEvent1()
    {
        throw new System.NotImplementedException();
    }

    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";
                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void SessionCancelledByCharacterBeforeEventHookupDeterminationCompleted()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SetEventHookupCheckMutex();

                testState.SendTypeChar('=');
                testState.SendTypeChar('z');

                testState.ReleaseEventHookupCheckMutex();
                testState.WaitForAsynchronousOperations();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void TabBeforeEventHookupDeterminationCompleted()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SetEventHookupCheckMutex();

                testState.SendTypeChar('=');

                // tab releases the mutex
                testState.SendTab();

                testState.WaitForAsynchronousOperations();
                testState.AssertNotShowing();

                var expectedCode = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent += C_MyEvent;
    }

    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";

                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void MoveCaretOutOfSpanBeforeEventHookupDeterminationCompleted()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SetEventHookupCheckMutex();

                testState.SendTypeChar('=');
                testState.SendLeftKey();
                testState.ReleaseEventHookupCheckMutex();

                testState.WaitForAsynchronousOperations();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void EnsureNameUniquenessInPartialClasses()
        {
            var markup = @"
public partial class C
{
    event System.Action MyEvent;
    public void Test()
    {
        MyEvent +$$
    }
}

public partial class C
{
    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void EnsureNameUniquenessAgainstBaseClasses()
        {
            var markup = @"
class Base
{
    protected int Console_CancelKeyPress;
}
class Program : Base
{
    void Main(string[] args)
    {
        var foo = Console_CancelKeyPress + 23;
        System.Console.CancelKeyPress +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("Console_CancelKeyPress1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void EnsureNameUniquenessAgainstParameters()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;

    void M(int C_MyEvent)
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.WaitForAsynchronousOperations();
                testState.AssertShowing("C_MyEvent1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public void DelegateInvokeMethodReturnsNonVoid()
        {
            var markup = @"
class C
{
    delegate int D(double d);
    event D MyEvent;

    void M()
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                testState.WaitForAsynchronousOperations();

                var expectedCode = @"
class C
{
    delegate int D(double d);
    event D MyEvent;

    void M()
    {
        MyEvent += C_MyEvent;
    }

    private int C_MyEvent(double d)
    {
        throw new System.NotImplementedException();
    }
}";
                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        [WorkItem(553660)]
        public void PlusEqualsInsideComment()
        {
            var markup = @"
class C
{
    void M()
    {
        // +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                testState.WaitForAsynchronousOperations();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        [WorkItem(951664)]
        public void UseInvocationLocationTypeNameWhenEventIsMemberOfBaseType()
        {
            var markup = @"
namespace Scenarios
{
    public class DelegateTest_Generics_NonGenericClass
    {
        public delegate void D1&lt;T&gt;();
        public event D1&lt;string&gt; E1;
    }
}
 
class TestClass_T1_S1_4 : Scenarios.DelegateTest_Generics_NonGenericClass
{
    void Method()
    {
        E1 +$$
    }
}";

            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                testState.WaitForAsynchronousOperations();

                var expectedCode = @"
namespace Scenarios
{
    public class DelegateTest_Generics_NonGenericClass
    {
        public delegate void D1<T>();
        public event D1<string> E1;
    }
}
 
class TestClass_T1_S1_4 : Scenarios.DelegateTest_Generics_NonGenericClass
{
    void Method()
    {
        E1 += TestClass_T1_S1_4_E1;
    }

    private void TestClass_T1_S1_4_E1()
    {
        throw new System.NotImplementedException();
    }
}";
                testState.AssertCodeIs(expectedCode);
            }
        }
    }
}
