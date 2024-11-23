// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EventHookup;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.EventHookup)]
public class EventHookupCommandHandlerTests
{
    private readonly NamingStylesTestOptionSets _namingOptions = new NamingStylesTestOptionSets(LanguageNames.CSharp);

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/20999")]
    public async Task HandlerName_EventInThisClass_CamelCaseRule()
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
        using var testState = new EventHookupTestState(
            EventHookupTestState.GetWorkspaceXml(markup), _namingOptions.MethodNamesAreCamelCase);

        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("c_MyEvent");
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("Local_MyEvent");
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("Cfield_MyEvent");
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
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

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent1");
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent1");
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent1");
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");

        testState.SendTypeChar(' ');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");

        testState.SendTypeChar('d');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertNotShowing();
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");

        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertNotShowing();
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");

        testState.SendTypeChar(' ');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");

        testState.SendLeftKey();
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");

        testState.SendLeftKey();
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertNotShowing();
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);

        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");

        testState.SendTypeChar(' ');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");

        testState.SendBackspace();
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertNotShowing();
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
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

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
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

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
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

    [WpfFact]
    public async Task HookupInFieldDeclarationSingleLineLambda()
    {
        var markup = @"
class C
{
    static event System.Action MyEvent;
    System.Action A = () => MyEvent +$$
}";
        using var testState = EventHookupTestState.CreateTestState(markup);
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

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
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

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
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

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');

        for (var i = 0; i < 20; i++)
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

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SetEventHookupCheckMutex();

        testState.SendTypeChar('=');
        testState.SendTypeChar('z');

        testState.ReleaseEventHookupCheckMutex();
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertNotShowing();
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
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

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SetEventHookupCheckMutex();

        testState.SendTypeChar('=');
        testState.SendLeftKey();
        testState.ReleaseEventHookupCheckMutex();

        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertNotShowing();
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent1");
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("Console_CancelKeyPress1");
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent1");
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
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

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553660")]
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
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        testState.SendTab();
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertNotShowing();
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951664")]
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

        using var testState = EventHookupTestState.CreateTestState(markup);
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

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup, QualifyMethodAccessWithNotification(NotificationOption2.Error));
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

    [WpfFact]
    public async Task EventHookupRemovesInaccessibleAttributes()
    {
        var workspaceXml = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""A"" CommonReferences=""true"">
        <Document>
using System;

public static class C
{
    public static event DelegateType E;

    public delegate void DelegateType([ShouldBeRemovedInternalAttribute] object o);
}

internal class ShouldBeRemovedInternalAttribute : Attribute { }
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"">
        <ProjectReference>A</ProjectReference>
        <Document>
class D
{
    void M()
    {
        C.E +$$
    }
}</Document>
    </Project>
</Workspace>";

        using var testState = new EventHookupTestState(XElement.Parse(workspaceXml), options: null);
        testState.SendTypeChar('=');
        testState.SendTab();
        await testState.WaitForAsynchronousOperationsAsync();

        var expectedCode = @"
class D
{
    void M()
    {
        C.E += C_E;
    }

    private void C_E(object o)
    {
        throw new System.NotImplementedException();
    }
}";
        testState.AssertCodeIs(expectedCode);
    }

    [WpfFact]
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
        using var testState = EventHookupTestState.CreateTestState(markup, QualifyMethodAccessWithNotification(NotificationOption2.Silent));
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/58474")]
    public async Task EventHookupInTopLevelCode()
    {
        var markup = @"

System.AppDomain.CurrentDomain.UnhandledException +$$

";
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        testState.SendTab();
        await testState.WaitForAsynchronousOperationsAsync();

        var expectedCode = @"

System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
{
    throw new System.NotImplementedException();
}";
        testState.AssertCodeIs(expectedCode);
    }

    [WpfFact]
    public async Task EventHookupAtEndOfDocument()
    {
        var markup = @"

System.AppDomain.CurrentDomain.UnhandledException +$$";
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');

        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("CurrentDomain_UnhandledException");

        var expectedCode = @"

System.AppDomain.CurrentDomain.UnhandledException +=";
        testState.AssertCodeIs(expectedCode);

        testState.SendTab();
        await testState.WaitForAsynchronousOperationsAsync();

        expectedCode = @"

System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
{
    throw new System.NotImplementedException();
}";
        testState.AssertCodeIs(expectedCode);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59935")]
    public async Task HandlerName_EventInGenericClass()
    {
        var markup = @"
using System;

class C
{
    void M()
    {
        Generic&lt;int&gt;.MyEvent +$$
    }
}

class Generic&lt;T&gt;
{
    public static event EventHandler MyEvent;
}";
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("Generic_MyEvent");
    }

    [WpfFact]
    public async Task HandlerName_GlobalAlias01()
    {
        var markup = @"
using System;

class C
{
    void M()
    {
        global::D.MyEvent +$$
    }
}

class D
{
    public static event EventHandler MyEvent;
}";
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("D_MyEvent");
    }

    [WpfFact]
    public async Task HandlerName_GlobalAlias02()
    {
        var markup = @"
using System;

class C
{
    void M()
    {
        global::Generic&lt;int&gt;.MyEvent +$$
    }
}

class Generic&lt;T&gt;
{
    public static event EventHandler MyEvent;
}";
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("Generic_MyEvent");
    }

    [WpfFact]
    public async Task HandlerName_GlobalAlias03()
    {
        var markup = @"
class Program
{
    void Main(string[] args)
    {
        global::System.Console.CancelKeyPress +$$
    }
}";
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("Console_CancelKeyPress");
    }

    [WpfFact]
    public async Task HandlerName_InvocationExpression()
    {
        var markup = @"
using System;

class C
{
    public event EventHandler MyEvent;
    
    public static C CreateC()
    {
        return new C();
    }
    
    public void M2()
    {
        CreateC().MyEvent +$$
    }
}";
        using var testState = EventHookupTestState.CreateTestState(markup);
        testState.SendTypeChar('=');
        await testState.WaitForAsynchronousOperationsAsync();
        testState.AssertShowing("C_MyEvent");
    }

    private static OptionsCollection QualifyMethodAccessWithNotification(NotificationOption2 notification)
        => new OptionsCollection(LanguageNames.CSharp) { { CodeStyleOptions2.QualifyMethodAccess, true, notification } };
}
