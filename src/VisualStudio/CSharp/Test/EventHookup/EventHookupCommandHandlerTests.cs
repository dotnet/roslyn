// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EventHookup
{
    [UseExportProvider]
    public class EventHookupCommandHandlerTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task HandlerName_EventInThisClass()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task HandlerName_EventOnLocal()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("Local_MyEvent;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task HandlerName_EventOnFieldOfObject()
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
    void Goo()
    {
        D local = new D();
        local.cfield.MyEvent +$$
    }
}
";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("Cfield_MyEvent;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task NoHookupOnIntegerPlusEquals()
        {
            var markup = @"
class C
{
    void Goo()
    {
        int x = 7;
        x +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertNotShowing();

                // Make sure that sending the tab works correctly. Note the 4 spaces after the +=
                testState.SendTab();
                await testState.WaitForAsynchronousOperationsAsync();
                var expectedCode = @"
class C
{
    void Goo()
    {
        int x = 7;
        x +=    
    }
}";

                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task HandlerName_DefaultHandlerNameAlreadyExistsWithSameNonStaticState()
        {
            var markup = @"
class C
{
    public event System.Action MyEvent;

    void Goo()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task HandlerName_DefaultHandlerNameAlreadyExistsWithDifferentStaticState()
        {
            var markup = @"
class C
{
    public event System.Action MyEvent;

    void Goo()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task HandlerName_DefaultHandlerNameAlreadyExistsAsField()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task HookupInLambdaInLocalDeclaration()
        {
            var markup = @"
class C
{
    public event System.Action MyEvent;

    void Goo()
    {
        Action a = () => MyEvent +$$
    }
}
";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task TypingSpacesDoesNotDismiss()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent;");

                testState.SendTypeChar(' ');
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task TypingLettersDismisses()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent;");

                testState.SendTypeChar('d');
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task TypingEqualsInSessionDismisses()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent;");

                testState.SendTypeChar('=');
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task CancelViaLeftKey()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent;");

                testState.SendTypeChar(' ');
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent;");

                testState.SendLeftKey();
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent;");

                testState.SendLeftKey();
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task CancelViaBackspace()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent;");

                testState.SendTypeChar(' ');
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent;");

                testState.SendBackspace();
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task EventHookupBeforeEventHookup()
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
                await testState.WaitForAsynchronousOperationsAsync();

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
        public async Task EventHookupBeforeComment()
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
                await testState.WaitForAsynchronousOperationsAsync();

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
        public async Task EventHookupInArgument()
        {
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        Goo(() => MyEvent +$$)
    }

    private void Goo(Action a)
    {
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                await testState.WaitForAsynchronousOperationsAsync();

                var expectedCode = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        Goo(() => MyEvent += C_MyEvent;)
    }

    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }

    private void Goo(Action a)
    {
    }
}";
                testState.AssertCodeIs(expectedCode);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task HookupInFieldDeclarationSingleLineLambda()
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
                await testState.WaitForAsynchronousOperationsAsync();

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
        public async Task HookupInFieldDeclarationMultiLineLambda()
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
                await testState.WaitForAsynchronousOperationsAsync();

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
        public async Task EventHookupInUnformattedPosition1()
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
                await testState.WaitForAsynchronousOperationsAsync();

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
        public async Task EventHookupInUnformattedPosition2()
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
                await testState.WaitForAsynchronousOperationsAsync();

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
        public async Task SessionCancelledByCharacterBeforeEventHookupDeterminationCompleted()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task TabBeforeEventHookupDeterminationCompleted()
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

                await testState.WaitForAsynchronousOperationsAsync();
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
        public async Task MoveCaretOutOfSpanBeforeEventHookupDeterminationCompleted()
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

                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task EnsureNameUniquenessInPartialClasses()
        {
            var markup = @"
public partial class C
{
    event System.Action MyEvent;
    public async Task Test()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task EnsureNameUniquenessAgainstBaseClasses()
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
        var goo = Console_CancelKeyPress + 23;
        System.Console.CancelKeyPress +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup))
            {
                testState.SendTypeChar('=');
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("Console_CancelKeyPress1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task EnsureNameUniquenessAgainstParameters()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertShowing("C_MyEvent1;");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task DelegateInvokeMethodReturnsNonVoid()
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
                await testState.WaitForAsynchronousOperationsAsync();

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
        [WorkItem(553660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553660")]
        public async Task PlusEqualsInsideComment()
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
                await testState.WaitForAsynchronousOperationsAsync();
                testState.AssertNotShowing();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        [WorkItem(951664, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951664")]
        public async Task UseInvocationLocationTypeNameWhenEventIsMemberOfBaseType()
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
                await testState.WaitForAsynchronousOperationsAsync();

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        public async Task EventHookupWithQualifiedMethodAccess()
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
            using (var testState = EventHookupTestState.CreateTestState(markup, QualifyMethodAccessWithNotification(NotificationOption.Error)))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                await testState.WaitForAsynchronousOperationsAsync();

                var expectedCode = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent += this.C_MyEvent;
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
        public async Task EventHookupWithQualifiedMethodAccessAndNotificationOptionSilent()
        {
            // This validates the scenario where the user has stated that they prefer `this.` qualification but the
            // notification level is `Silent`, which means existing violations of the rule won't be flagged but newly
            // generated code will conform appropriately.
            var markup = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent +$$
    }
}";
            using (var testState = EventHookupTestState.CreateTestState(markup, QualifyMethodAccessWithNotification(NotificationOption.Silent)))
            {
                testState.SendTypeChar('=');
                testState.SendTab();
                await testState.WaitForAsynchronousOperationsAsync();

                var expectedCode = @"
class C
{
    event System.Action MyEvent;
    void M()
    {
        MyEvent += this.C_MyEvent;
    }

    private void C_MyEvent()
    {
        throw new System.NotImplementedException();
    }
}";
                testState.AssertCodeIs(expectedCode);
            }
        }

        private IDictionary<OptionKey, object> QualifyMethodAccessWithNotification(NotificationOption notification)
            => new Dictionary<OptionKey, object>() { { new OptionKey(CodeStyleOptions.QualifyMethodAccess, LanguageNames.CSharp), new CodeStyleOption<bool>(true, notification) } };
    }
}
