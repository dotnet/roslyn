// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.PullMemberUp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PullMemberUp;

[Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
public sealed class CSharpPullMemberUpTests : AbstractCSharpCodeActionTest
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(EditorTestWorkspace workspace, TestParameters parameters)
        => new CSharpPullMemberUpCodeRefactoringProvider((IPullMemberUpOptionsService)parameters.fixProviderData);

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions) => FlattenActions(actions);

    #region Quick Action

    private async Task TestQuickActionNotProvidedAsync(
        string initialMarkup,
        TestParameters parameters = null)
    {
        var service = new TestPullMemberUpService(null, null);
        var parametersValue = (parameters ?? TestParameters.Default).WithFixProviderData(service);

        using var workspace = CreateWorkspaceFromOptions(initialMarkup, parametersValue);
        var (actions, _) = await GetCodeActionsAsync(workspace, parametersValue);
        if (actions.Length == 1)
        {
            // The dialog shows up, not quick action
            Assert.Equal(actions.First().Title, FeaturesResources.Pull_members_up_to_base_type);
        }
        else if (actions.Length > 1)
        {
            Assert.True(false, "Pull Members Up is provided via quick action");
        }
        else
        {
            Assert.True(true);
        }
    }

    [Fact]
    public async Task TestNoRefactoringProvidedWhenNoOptionsService()
    {
        await TestActionCountAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public void TestM[||]ethod()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}", 0);
    }

    [Fact]
    public async Task TestNoRefactoringProvidedWhenPullFieldInInterfaceViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
namespace PushUpTest
{
    public interface ITestInterface
    {
    }

    public class TestClass : ITestInterface
    {
        public int yo[||]u = 10086;
    }
}");
    }

    [Fact]
    public async Task TestNoRefactoringProvidedWhenMethodDeclarationAlreadyExistsInInterfaceViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
namespace PushUpTest
{
    public interface ITestInterface
    {
        void TestMethod();
    }

    public class TestClass : ITestInterface
    {
        public void TestM[||]ethod()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}");
    }

    [Fact]
    public async Task TestNoRefactoringProvidedWhenPropertyDeclarationAlreadyExistsInInterfaceViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        int TestProperty { get; }
    }

    public class TestClass : IInterface
    {
        public int TestPr[||]operty { get; private set; }
    }
}");
    }

    [Fact]
    public async Task TestNoRefactoringProvidedWhenEventDeclarationAlreadyExistsToInterfaceViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        event EventHandler Event2;
    }

    public class TestClass : IInterface
    {
        public event EventHandler Event1, Eve[||]nt2, Event3;
    }
}");
    }

    [Fact]
    public async Task TestNoRefactoringProvidedInNestedTypesViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
namespace PushUpTest
{
    public interface ITestInterface
    {
        void Foobar();
    }

    public class TestClass : ITestInterface
    {
        public class N[||]estedClass
        {
        }
    }
}");
    }

    [Fact]
    public async Task TestPullMethodUpToInterfaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public void TestM[||]ethod()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        void TestMethod();
    }

    public class TestClass : IInterface
    {
        public void TestMethod()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}");
    }

    [Fact]
    public async Task TestPullAbstractMethodToInterfaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public interface IInterface
    {
    }

    public abstract class TestClass : IInterface
    {
        public abstract void TestMeth[||]od();
    }
}", @"
namespace PushUpTest
{
    public interface IInterface
    {
        void TestMethod();
    }

    public abstract class TestClass : IInterface
    {
        public abstract void TestMethod();
    }
}");
    }

    [Fact]
    public async Task TestPullGenericsUpToInterfaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public void TestMeth[||]od<T>() where T : IDisposable
        {
        }
    }
}", @"
using System;
namespace PushUpTest
{
    public interface IInterface
    {
        void TestMethod<T>() where T : IDisposable;
    }

    public class TestClass : IInterface
    {
        public void TestMeth[||]od<T>() where T : IDisposable
        {
        }
    }
}");
    }

    [Fact]
    public async Task TestPullSingleEventToInterfaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public event EventHandler Eve[||]nt1
        {
            add
            {
                System.Console.Writeline(""This is add"");
            }
            remove
            {
                System.Console.Writeline(""This is remove"");
            }
        }
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        event EventHandler Event1;
    }

    public class TestClass : IInterface
    {
        public event EventHandler Event1
        {
            add
            {
                System.Console.Writeline(""This is add"");
            }
            remove
            {
                System.Console.Writeline(""This is remove"");
            }
        }
    }
}");
    }

    [Fact]
    public async Task TestPullOneEventFromMultipleEventsToInterfaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public event EventHandler Event1, Eve[||]nt2, Event3;
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        event EventHandler Event2;
    }

    public class TestClass : IInterface
    {
        public event EventHandler Event1, Event2, Event3;
    }
}");
    }

    [Fact]
    public async Task TestPullPublicEventWithAccessorsToInterfaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public event EventHandler Eve[||]nt2
        {
            add
            {
                System.Console.Writeln(""This is add in event1"");
            }
            remove
            {
                System.Console.Writeln(""This is remove in event2"");
            }
        }
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        event EventHandler Event2;
    }

    public class TestClass : IInterface
    {
        public event EventHandler Event2
        {
            add
            {
                System.Console.Writeln(""This is add in event1"");
            }
            remove
            {
                System.Console.Writeln(""This is remove in event2"");
            }
        }
    }
}");
    }

    [Fact]
    public async Task TestPullPropertyWithPrivateSetterToInterfaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public int TestPr[||]operty { get; private set; }
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        int TestProperty { get; }
    }

    public class TestClass : IInterface
    {
        public int TestProperty { get; private set; }
    }
}");
    }

    [Fact]
    public async Task TestPullPropertyWithPrivateGetterToInterfaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public int TestProperty[||]{ private get; set; }
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        int TestProperty { set; }
    }

    public class TestClass : IInterface
    {
        public int TestProperty{ private get; set; }
    }
}");
    }

    [Fact]
    public async Task TestPullMemberFromInterfaceToInterfaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    interface FooInterface : IInterface
    {
        int TestPr[||]operty { set; }
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        int TestProperty { set; }
    }

    interface FooInterface : IInterface
    {
    }
}");
    }

    [Fact]
    public async Task TestPullIndexerWithOnlySetterToInterfaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        private int j;
        public int th[||]is[int i]
        {
           set => j = value;
        }
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        int this[int i] { set; }
    }

    public class TestClass : IInterface
    {
        private int j;
        public int this[int i]
        {
           set => j = value;
        }
    }
}");
    }

    [Fact]
    public async Task TestPullIndexerWithOnlyGetterToInterfaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        private int j;
        public int th[||]is[int i]
        {
           get => j = value;
        }
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        int this[int i] { get; }
    }

    public class TestClass : IInterface
    {
        private int j;
        public int this[int i]
        {
           get => j = value;
        }
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullPropertyToInterfaceWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public interface IBase
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : IBase
{
    public Uri En[||]dpoint { get; set; }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;

public interface IBase
{
    Uri Endpoint { get; set; }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : IBase
{
    public Uri Endpoint { get; set; }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToInterfaceWithoutAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public interface IBase
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : IBase
{
    public bool Test[||]Method()
    {
        var endpoint1 = new Uri(""http://localhost"");
        var endpoint2 = new Uri(""http://localhost"");
        return endpoint1.Equals(endpoint2);
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public interface IBase
{
    bool TestMethod();
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : IBase
{
    public bool Test[||]Method()
    {
        var endpoint1 = new Uri(""http://localhost"");
        var endpoint2 = new Uri(""http://localhost"");
        return endpoint1.Equals(endpoint2);
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodWithNewReturnTypeToInterfaceWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public interface IBase
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : IBase
{
    public Uri Test[||]Method()
    {
        return new Uri(""http://localhost"");
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;

public interface IBase
{
    Uri TestMethod();
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : IBase
{
    public Uri TestMethod()
    {
        return new Uri(""http://localhost"");
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodWithNewParamTypeToInterfaceWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public interface IBase
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : IBase
{
    public bool Test[||]Method(Uri endpoint)
    {
        var localHost = new Uri(""http://localhost"");
        return endpoint.Equals(localhost);
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;

public interface IBase
{
    bool TestMethod(Uri endpoint);
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : IBase
{
    public bool TestMethod(Uri endpoint)
    {
        var localHost = new Uri(""http://localhost"");
        return endpoint.Equals(localhost);
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullEventToInterfaceWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public interface IBase
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : IBase
{
    public event EventHandler Test[||]Event
    {
        add
        {
            Console.WriteLine(""adding event..."");
        }
        remove
        {
            Console.WriteLine(""removing event..."");
        }
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;

public interface IBase
{
    event EventHandler TestEvent;
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : IBase
{
    public event EventHandler TestEvent
    {
        add
        {
            Console.WriteLine(""adding event..."");
        }
        remove
        {
            Console.WriteLine(""removing event..."");
        }
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullPropertyToClassWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public Uri En[||]dpoint { get; set; }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System;

public class Base
{
    public Uri Endpoint { get; set; }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullPropertyToClassWithAddUsingsViaQuickAction2()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public Uri En[||]dpoint { get; set; }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System;

public class Base
{
    public Uri Endpoint { get; set; }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullPropertyToClassWithoutDuplicatingUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;

public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public Uri En[||]dpoint { get; set; }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;

public class Base
{
    public Uri Endpoint { get; set; }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullPropertyWithNewBodyTypeToClassWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public bool Test[||]Property
    {
        get
        {
            var endpoint1 = new Uri(""http://localhost"");
            var endpoint2 = new Uri(""http://localhost"");
            return endpoint1.Equals(endpoint2);
        }
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System;

public class Base
{
    public bool TestProperty
    {
        get
        {
            var endpoint1 = new Uri(""http://localhost"");
            var endpoint2 = new Uri(""http://localhost"");
            return endpoint1.Equals(endpoint2);
        }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodWithNewNonDeclaredBodyTypeToClassWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;

public class Derived : Base
{
    public int Test[||]Method()
    {
        return Enumerable.Range(0, 5).Sum();
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System.Linq;

public class Base
{
    public int Test[||]Method()
    {
        return Enumerable.Range(0, 5).Sum();
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithOverlappingUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;
using System.Threading.Tasks;

public class Base
{
    public Uri Endpoint{ get; set; }

    public async Task&lt;int&gt; Get5Async()
    {
        return 5;
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;
using System.Threading.Tasks;

public class Derived : Base
{
    public async Task&lt;int&gt; Test[||]Method()
    {
        return Enumerable.Range(0, 5).Sum();
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;
using System.Linq;
using System.Threading.Tasks;

public class Base
{
    public Uri Endpoint{ get; set; }

    public async Task&lt;int&gt; Get5Async()
    {
        return 5;
    }
    public async Task&lt;int&gt; Test[||]Method()
    {
        return Enumerable.Range(0, 5).Sum();
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;
using System.Threading.Tasks;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithUnnecessaryFirstUsingViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">

using System.Threading.Tasks;

public class Base
{
    public async Task&lt;int&gt; Get5Async()
    {
        return 5;
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;
using System.Linq;
using System.Threading.Tasks;

public class Derived : Base
{
    public async Task&lt;int&gt; Test[||]Method()
    {
        return Enumerable.Range(0, 5).Sum();
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System.Linq;
using System.Threading.Tasks;

public class Base
{
    public async Task&lt;int&gt; Get5Async()
    {
        return 5;
    }
    public async Task&lt;int&gt; Test[||]Method()
    {
        return Enumerable.Range(0, 5).Sum();
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;
using System.Linq;
using System.Threading.Tasks;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithUnusedBaseUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;
using System.Threading.Tasks;

public class Base
{
    public Uri Endpoint{ get; set; }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;

public class Derived : Base
{
    public int Test[||]Method()
    {
        return Enumerable.Range(0, 5).Sum();
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;
using System.Linq;
using System.Threading.Tasks;

public class Base
{
    public Uri Endpoint{ get; set; }
    public int TestMethod()
    {
        return Enumerable.Range(0, 5).Sum();
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithRetainCommentsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
// blah blah

public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;

public class Derived : Base
{
    public int Test[||]Method()
    {
        return 5;
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
// blah blah

public class Base
{
    public int TestMethod()
    {
        return 5;
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithRetainPreImportCommentsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
// blah blah
using System.Linq;

public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public Uri End[||]point { get; set; }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
// blah blah
using System;
using System.Linq;

public class Base
{
    public Uri Endpoint { get; set; }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithRetainPostImportCommentsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System.Linq;

// blah blah
public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public Uri End[||]point { get; set; }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;
using System.Linq;

// blah blah
public class Base
{
    public Uri Endpoint { get; set; }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithLambdaUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;
using System.Linq;

public class Derived : Base
{
    public int Test[||]Method()
    {
        return Enumerable.Range(0, 5).
            Select((n) => new Uri(""http://"" + n)).
            Count((uri) => uri != null);
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System;
using System.Linq;

public class Base
{
    public int TestMethod()
    {
        return Enumerable.Range(0, 5).
            Select((n) => new Uri(""http://"" + n)).
            Count((uri) => uri != null);
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;
using System.Linq;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithUnusedUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;

public class Base
{
    public Uri Endpoint{ get; set; }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;
using System.Threading.Tasks;

public class Derived : Base
{
    public int Test[||]Method()
    {
        return Enumerable.Range(0, 5).Sum();
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;
using System.Linq;

public class Base
{
    public Uri Endpoint{ get; set; }
    public int TestMethod()
    {
        return Enumerable.Range(0, 5).Sum();
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;
using System.Threading.Tasks;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassKeepSystemFirstViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace TestNs1
{
    using System;

    public class Base
    {
        public Uri Endpoint{ get; set; }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace A_TestNs2
{
    using TestNs1;

    public class Derived : Base
    {
        public Foo Test[||]Method()
        {
            return null;
        }
    }

    public class Foo
    {
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace TestNs1
{
    using System;
    using A_TestNs2;

    public class Base
    {
        public Uri Endpoint{ get; set; }
        public Foo TestMethod()
        {
            return null;
        }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace A_TestNs2
{
    using TestNs1;

    public class Derived : Base
    {
    }

    public class Foo
    {
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassKeepSystemFirstViaQuickAction2()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace TestNs1
{
    public class Base
    {
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace A_TestNs2
{
    using System;
    using TestNs1;

    public class Derived : Base
    {
        public Foo Test[||]Method()
        {
            var uri = new Uri(""http://localhost"");
            return null;
        }
    }

    public class Foo
    {
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System;
using A_TestNs2;

namespace TestNs1
{
    public class Base
    {
        public Foo TestMethod()
        {
            var uri = new Uri(""http://localhost"");
            return null;
        }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace A_TestNs2
{
    using System;
    using TestNs1;

    public class Derived : Base
    {
    }

    public class Foo
    {
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithExtensionViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace TestNs1
{
    public class Base
    {
    }

    public class Foo
    {
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace TestNs2
{
    using TestNs1;

    public class Derived : Base
    {
        public int Test[||]Method()
        {
            var foo = new Foo();
            return foo.FooBar();
        }
    }

    public static class FooExtensions
    {
        public static int FooBar(this Foo foo) 
        {
            return 5;
        }
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using TestNs2;

namespace TestNs1
{
    public class Base
    {
        public int TestMethod()
        {
            var foo = new Foo();
            return foo.FooBar();
        }
    }

    public class Foo
    {
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace TestNs2
{
    using TestNs1;

    public class Derived : Base
    {
    }

    public static class FooExtensions
    {
        public static int FooBar(this Foo foo) 
        {
            return 5;
        }
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithExtensionViaQuickAction2()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace TestNs1
{
    public class Base
    {
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using TestNs1;
using TestNs3;
using TestNs4;

namespace TestNs2
{
    public class Derived : Base
    {
        public int Test[||]Method()
        {
            var foo = new Foo();
            return foo.FooBar();
        }
    }
}
        </Document>
        <Document FilePath = ""File3.cs"">
namespace TestNs3
{
    public class Foo
    {
    }
}
        </Document>
        <Document FilePath = ""File4.cs"">
using TestNs3;

namespace TestNs4
{
    public static class FooExtensions
    {
        public static int FooBar(this Foo foo) 
        {
            return 5;
        }
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using TestNs3;
using TestNs4;

namespace TestNs1
{
    public class Base
    {
        public int TestMethod()
        {
            var foo = new Foo();
            return foo.FooBar();
        }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using TestNs1;
using TestNs3;
using TestNs4;

namespace TestNs2
{
    public class Derived : Base
    {
    }
}
        </Document>
        <Document FilePath = ""File3.cs"">
namespace TestNs3
{
    public class Foo
    {
    }
}
        </Document>
        <Document FilePath = ""File4.cs"">
using TestNs3;

namespace TestNs4
{
    public static class FooExtensions
    {
        public static int FooBar(this Foo foo) 
        {
            return 5;
        }
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithAliasUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;

public class Base
{
    public Uri Endpoint{ get; set; }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using Enumer = System.Linq.Enumerable;
using Sys = System;

public class Derived : Base
{
    public void Test[||]Method()
    {
        Sys.Console.WriteLine(Enumer.Range(0, 5).Sum());
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;
using Enumer = System.Linq.Enumerable;
using Sys = System;

public class Base
{
    public Uri Endpoint{ get; set; }
    public void TestMethod()
    {
        Sys.Console.WriteLine(Enumer.Range(0, 5).Sum());
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using Enumer = System.Linq.Enumerable;
using Sys = System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullPropertyToClassWithBaseAliasUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using Enumer = System.Linq.Enumerable;

public class Base
{
    public void TestMethod()
    {
        System.Console.WriteLine(Enumer.Range(0, 5).Sum());
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public Uri End[||]point{ get; set; }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
using System;
using Enumer = System.Linq.Enumerable;

public class Base
{
    public Uri Endpoint{ get; set; }
    public void TestMethod()
    {
        System.Console.WriteLine(Enumer.Range(0, 5).Sum());
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithMultipleNamespacedUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
namespace TestNs1
{
    using System;

    public class Base
    {
        public Uri Endpoint{ get; set; }
    }
}
namespace TestNs2
{
    using System.Linq;
    using TestNs1;

    public class Derived : Base
    {
        public int Test[||]Method()
        {
            return Enumerable.Range(0, 5).Sum();
        }
    }
}
", @"
namespace TestNs1
{
    using System;
    using System.Linq;

    public class Base
    {
        public Uri Endpoint{ get; set; }
        public int TestMethod()
        {
            return Enumerable.Range(0, 5).Sum();
        }
    }
}
namespace TestNs2
{
    using System.Linq;
    using TestNs1;

    public class Derived : Base
    {
    }
}
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithNestedNamespacedUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
namespace TestNs1
{
    namespace InnerNs1
    {
        using System;

        public class Base
        {
            public Uri Endpoint { get; set; }
        }
    }
}
namespace TestNs2
{
    namespace InnerNs2
    {
        using System.Linq;
        using TestNs1.InnerNs1;

        public class Derived : Base
        {
            public int Test[||]Method()
            {
                return Foo.Bar(Enumerable.Range(0, 5).Sum());
            }
        }

        public class Foo
        {
            public static int Bar(int num)
            {
                return num + 1;
            }
        }
    }
}
", @"
namespace TestNs1
{
    namespace InnerNs1
    {
        using System;
        using System.Linq;
        using TestNs2.InnerNs2;

        public class Base
        {
            public Uri Endpoint { get; set; }
            public int TestMethod()
            {
                return Foo.Bar(Enumerable.Range(0, 5).Sum());
            }
        }
    }
}
namespace TestNs2
{
    namespace InnerNs2
    {
        using System.Linq;
        using TestNs1.InnerNs1;

        public class Derived : Base
        {
        }

        public class Foo
        {
            public static int Bar(int num)
            {
                return num + 1;
            }
        }
    }
}
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithNewNamespaceUsingViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace A.B
{
    class Base
    {
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace X.Y
{
    class Derived : A.B.Base
    {
        public Other Get[||]Other() => null;
    }

    class Other
    {
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using X.Y;

namespace A.B
{
    class Base
    {
        public Other GetOther() => null;
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace X.Y
{
    class Derived : A.B.Base
    {
    }

    class Other
    {
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithFileNamespaceUsingViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace A.B;

class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace X.Y;
class Derived : A.B.Base
{
    public Other Get[||]Other() => null;
}

class Other
{
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using X.Y;

namespace A.B;

class Base
{
    public Other GetOther() => null;
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace X.Y;
class Derived : A.B.Base
{
}

class Other
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithUnusedNamespaceUsingViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace A.B
{
    class Base
    {
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace X.Y
{
    class Derived : A.B.Base
    {
        public int Get[||]Five() => 5;
    }

    class Other
    {
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace A.B
{
    class Base
    {
        public int GetFive() => 5;
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace X.Y
{
    class Derived : A.B.Base
    {
    }

    class Other
    {
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithMultipleNamespacesAndCommentsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
// comment 1

namespace TestNs1
{
    // comment 2

    // comment 3 
    public class Base
    {
    }
}
namespace TestNs2
{
    // comment 4
    using System.Linq;
    using TestNs1;

    public class Derived : Base
    {
        public int Test[||]Method()
        {
            return 5;
        }
    }
}
", @"
// comment 1

namespace TestNs1
{
    // comment 2

    // comment 3 
    public class Base
    {
        public int TestMethod()
        {
            return 5;
        }
    }
}
namespace TestNs2
{
    // comment 4
    using System.Linq;
    using TestNs1;

    public class Derived : Base
    {
    }
}
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithMultipleNamespacedUsingsAndCommentsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
// comment 1

namespace TestNs1
{
    // comment 2
    using System;

    // comment 3 
    public class Base
    {
    }
}
namespace TestNs2
{
    // comment 4
    using System.Linq;
    using TestNs1;

    public class Derived : Base
    {
        public int Test[||]Method()
        {
            return Enumerable.Range(0, 5).Sum();
        }
    }
}
", @"
// comment 1

namespace TestNs1
{
    // comment 2
    using System;
    using System.Linq;

    // comment 3 
    public class Base
    {
        public int TestMethod()
        {
            return Enumerable.Range(0, 5).Sum();
        }
    }
}
namespace TestNs2
{
    // comment 4
    using System.Linq;
    using TestNs1;

    public class Derived : Base
    {
    }
}
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithNamespacedUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace ClassLibrary1
{
    using System;

    public class Base
    {
        public Uri Endpoint{ get; set; }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace ClassLibrary1
{
    using System.Linq;

    public class Derived : Base
    {
        public int Test[||]Method()
        {
            return Enumerable.Range(0, 5).Sum();
        }
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace ClassLibrary1
{
    using System;
    using System.Linq;

    public class Base
    {
        public Uri Endpoint{ get; set; }
        public int Test[||]Method()
        {
            return Enumerable.Range(0, 5).Sum();
        }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
namespace ClassLibrary1
{
    using System.Linq;

    public class Derived : Base
    {
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodToClassWithDuplicateNamespacedUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace ClassLibrary1
{
    using System;

    public class Base
    {
        public Uri Endpoint{ get; set; }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;
namespace ClassLibrary1
{
    using System.Linq;

    public class Derived : Base
    {
        public int Test[||]Method()
        {
            return Enumerable.Range(0, 5).Sum();
        }
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace ClassLibrary1
{
    using System;
    using System.Linq;

    public class Base
    {
        public Uri Endpoint{ get; set; }
        public int Test[||]Method()
        {
            return Enumerable.Range(0, 5).Sum();
        }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;
namespace ClassLibrary1
{
    using System.Linq;

    public class Derived : Base
    {
    }
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodWithNewReturnTypeToClassWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public Uri En[||]dpoint()
    {
        return new Uri(""http://localhost"");
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System;

public class Base
{
    public Uri Endpoint()
    {
        return new Uri(""http://localhost"");
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodWithNewParamTypeToClassWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public bool Test[||]Method(Uri endpoint)
    {
        var localHost = new Uri(""http://localhost"");
        return endpoint.Equals(localhost);
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System;

public class Base
{
    public bool TestMethod(Uri endpoint)
    {
        var localHost = new Uri(""http://localhost"");
        return endpoint.Equals(localhost);
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullMethodWithNewBodyTypeToClassWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public bool Test[||]Method()
    {
        var endpoint1 = new Uri(""http://localhost"");
        var endpoint2 = new Uri(""http://localhost"");
        return endpoint1.Equals(endpoint2);
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System;

public class Base
{
    public bool TestMethod()
    {
        var endpoint1 = new Uri(""http://localhost"");
        var endpoint2 = new Uri(""http://localhost"");
        return endpoint1.Equals(endpoint2);
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullEventToClassWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public event EventHandler Test[||]Event
    {
        add
        {
            Console.WriteLine(""adding event..."");
        }
        remove
        {
            Console.WriteLine(""removing event..."");
        }
    }
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System;

public class Base
{
    public event EventHandler Test[||]Event
    {
        add
        {
            Console.WriteLine(""adding event..."");
        }
        remove
        {
            Console.WriteLine(""removing event..."");
        }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullFieldToClassWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
    public var en[||]dpoint = new Uri(""http://localhost"");
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System;

public class Base
{
    public var endpoint = new Uri(""http://localhost"");
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public async Task TestPullFieldToClassNoConstructorWithAddUsingsViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
public class Base
{
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;

public class Derived : Base
{
    public var ran[||]ge = Enumerable.Range(0, 5);
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using System.Linq;

public class Base
{
    public var range = Enumerable.Range(0, 5);
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System.Linq;

public class Derived : Base
{
}
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact]
    public async Task TestNoRefactoringProvidedWhenPullOverrideMethodUpToClassViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
namespace PushUpTest
{
    public class Base
    {
        public virtual void TestMethod() => System.Console.WriteLine(""foo bar bar foo"");
    }

    public class TestClass : Base
    {
        public override void TestMeth[||]od()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}");
    }

    [Fact]
    public async Task TestNoRefactoringProvidedWhenPullOverridePropertyUpToClassViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
using System;
namespace PushUpTest
{
    public class Base
    {
        public virtual int TestProperty { get => 111; private set; }
    }

    public class TestClass : Base
    {
        public override int TestPr[||]operty { get; private set; }
    }
}");
    }

    [Fact]
    public async Task TestNoRefactoringProvidedWhenPullOverrideEventUpToClassViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
using System;

namespace PushUpTest
{
    public class Base2
    {
        protected virtual event EventHandler Event3
        {
            add
            {
                System.Console.WriteLine(""Hello"");
            }
            remove
            {
                System.Console.WriteLine(""World"");
            }
        };
    }

    public class TestClass2 : Base2
    {
        protected override event EventHandler E[||]vent3
        {
            add
            {
                System.Console.WriteLine(""foo"");
            }
            remove
            {
                System.Console.WriteLine(""bar"");
            }
        };
    }
}");
    }

    [Fact]
    public async Task TestNoRefactoringProvidedWhenPullSameNameFieldUpToClassViaQuickAction()
    {
        // Fields share the same name will be thought as 'override', since it will cause error
        // if two same name fields exist in one class
        await TestQuickActionNotProvidedAsync(@"
namespace PushUpTest
{
    public class Base
    {
        public int you = -100000;
    }

    public class TestClass : Base
    {
        public int y[||]ou = 10086;
    }
}");
    }

    [Fact]
    public async Task TestPullMethodToOrdinaryClassViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public void TestMeth[||]od()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}", @"
namespace PushUpTest
{
    public class Base
    {
        public void TestMethod()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }

    public class TestClass : Base
    {
    }
}");
    }

    [Fact]
    public async Task TestPullOneFieldsToClassViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public int you[||]= 10086;
    }
}", @"
namespace PushUpTest
{
    public class Base
    {
        public int you = 10086;
    }

    public class TestClass : Base
    {
    }
}");
    }

    [Fact]
    public async Task TestPullGenericsUpToClassViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public class BaseClass
    {
    }

    public class TestClass : BaseClass
    {
        public void TestMeth[||]od<T>() where T : IDisposable
        {
        }
    }
}", @"
using System;
namespace PushUpTest
{
    public class BaseClass
    {
        public void TestMethod<T>() where T : IDisposable
        {
        }
    }

    public class TestClass : BaseClass
    {
    }
}");
    }

    [Fact]
    public async Task TestPullOneFieldFromMultipleFieldsToClassViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public int you, a[||]nd, someone = 10086;
    }
}", @"
namespace PushUpTest
{
    public class Base
    {
        public int and;
    }

    public class TestClass : Base
    {
        public int you, someone = 10086;
    }
}");
    }

    [Fact]
    public async Task TestPullMiddleFieldWithValueToClassViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public int you, a[||]nd = 4000, someone = 10086;
    }
}", @"
namespace PushUpTest
{
    public class Base
    {
        public int and = 4000;
    }

    public class TestClass : Base
    {
        public int you, someone = 10086;
    }
}");
    }

    [Fact]
    public async Task TestPullOneEventFromMultipleToClassViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;

namespace PushUpTest
{
    public class Base2
    {
    }

    public class Testclass2 : Base2
    {
        private static event EventHandler Event1, Eve[||]nt3, Event4;
    }
}", @"
using System;

namespace PushUpTest
{
    public class Base2
    {
        private static event EventHandler Event3;
    }

    public class Testclass2 : Base2
    {
        private static event EventHandler Event1, Event4;
    }
}");
    }

    [Fact]
    public async Task TestPullEventToClassViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;

namespace PushUpTest
{
    public class Base2
    {
    }

    public class TestClass2 : Base2
    {
        private static event EventHandler Eve[||]nt3;
    }
}", @"
using System;

namespace PushUpTest
{
    public class Base2
    {
        private static event EventHandler Event3;
    }

    public class TestClass2 : Base2
    {
    }
}");
    }

    [Fact]
    public async Task TestPullEventWithBodyToClassViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;

namespace PushUpTest
{
    public class Base2
    {
    }

    public class TestClass2 : Base2
    {
        private static event EventHandler Eve[||]nt3
        {
            add
            {
                System.Console.Writeln(""Hello"");
            }
            remove
            {
                System.Console.Writeln(""World"");
            }
        };
    }
}", @"
using System;

namespace PushUpTest
{
    public class Base2
    {
        private static event EventHandler Event3
        {
            add
            {
                System.Console.Writeln(""Hello"");
            }
            remove
            {
                System.Console.Writeln(""World"");
            }
        };
    }

    public class TestClass2 : Base2
    {
    }
}");
    }

    [Fact]
    public async Task TestPullPropertyToClassViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public int TestPr[||]operty { get; private set; }
    }
}", @"
using System;
namespace PushUpTest
{
    public class Base
    {
        public int TestProperty { get; private set; }
    }

    public class TestClass : Base
    {
    }
}");
    }

    [Fact]
    public async Task TestPullIndexerToClassViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class Base 
    {
    }

    public class TestClass : Base
    {
        private int j;
        public int th[||]is[int i]
        {
            get => j;
            set => j = value;
        }
    }
}", @"
namespace PushUpTest
{
    public class Base
    {
        public int this[int i]
        {
            get => j;
            set => j = value;
        }
    }

    public class TestClass : Base
    {
        private int j;
    }
}");
    }

    [Fact]
    public async Task TestPullMethodUpAcrossProjectViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <ProjectReference>CSAssembly2</ProjectReference>
        <Document>
using Destination;
public class TestClass : IInterface
{
    public int Bar[||]Bar()
    {
        return 12345;
    }
}
        </Document>
  </Project>
  <Project Language=""C#"" AssemblyName=""CSAssembly2"" CommonReferences=""true"">
        <Document>
namespace Destination
{
    public interface IInterface
    {
    }
}
        </Document>
  </Project>
</Workspace>", @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <ProjectReference>CSAssembly2</ProjectReference>
        <Document>
using Destination;
public class TestClass : IInterface
{
    public int Bar[||]Bar()
    {
        return 12345;
    }
}
        </Document>
  </Project>
  <Project Language=""C#"" AssemblyName=""CSAssembly2"" CommonReferences=""true"">
        <Document>
namespace Destination
{
    public interface IInterface
    {
        int BarBar();
    }
}
        </Document>
  </Project>
</Workspace>");
    }

    [Fact]
    public async Task TestPullPropertyUpAcrossProjectViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <ProjectReference>CSAssembly2</ProjectReference>
        <Document>
using Destination;
public class TestClass : IInterface
{
    public int F[||]oo
    {
        get;
        set;
    }
}
        </Document>
  </Project>
  <Project Language=""C#"" AssemblyName=""CSAssembly2"" CommonReferences=""true"">
        <Document>
namespace Destination
{
    public interface IInterface
    {
    }
}
        </Document>
  </Project>
</Workspace>", @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <ProjectReference>CSAssembly2</ProjectReference>
        <Document>
using Destination;
public class TestClass : IInterface
{
    public int Foo
    {
        get;
        set;
    }
}
        </Document>
  </Project>
  <Project Language=""C#"" AssemblyName=""CSAssembly2"" CommonReferences=""true"">
        <Document>
namespace Destination
{
    public interface IInterface
    {
        int Foo { get; set; }
    }
}
        </Document>
  </Project>
</Workspace>");
    }

    [Fact]
    public async Task TestPullFieldUpAcrossProjectViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <ProjectReference>CSAssembly2</ProjectReference>
        <Document>
using Destination;
public class TestClass : BaseClass
{
    private int i, j, [||]k = 10;
}
        </Document>
  </Project>
  <Project Language=""C#"" AssemblyName=""CSAssembly2"" CommonReferences=""true"">
        <Document>
namespace Destination
{
    public class BaseClass
    {
    }
}
        </Document>
  </Project>
</Workspace>", @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <ProjectReference>CSAssembly2</ProjectReference>
        <Document>
using Destination;
public class TestClass : BaseClass
{
    private int i, j;
}
        </Document>
  </Project>
  <Project Language=""C#"" AssemblyName=""CSAssembly2"" CommonReferences=""true"">
        <Document>
namespace Destination
{
    public class BaseClass
    {
        private int k = 10;
    }
}
        </Document>
  </Project>
</Workspace>");
    }

    [Fact]
    public async Task TestPullMethodUpToVBClassViaQuickAction()
    {
        // Moving member from C# to Visual Basic is not supported currently since the FindMostRelevantDeclarationAsync method in
        // AbstractCodeGenerationService will return null.
        await TestQuickActionNotProvidedAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
            using VBAssembly;
            public class TestClass : VBClass
            {
                public int Bar[||]bar()
                {
                    return 12345;
                }
            }
        </Document>
  </Project>
  <Project Language=""Visual Basic"" AssemblyName=""VBAssembly"" CommonReferences=""true"">
        <Document>
            Public Class VBClass
            End Class
        </Document>
  </Project>
</Workspace>");
    }

    [Fact]
    public async Task TestPullMethodUpToVBInterfaceViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
    <ProjectReference>VBAssembly</ProjectReference>
    <Document>
        public class TestClass : VBInterface
        {
            public int Bar[||]bar()
            {
                return 12345;
            }
        }
    </Document>
  </Project>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly"" CommonReferences=""true"">
        <Document>
            Public Interface VBInterface
            End Interface
        </Document>
    </Project>
</Workspace>
");
    }

    [Fact]
    public async Task TestPullFieldUpToVBClassViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
            using VBAssembly;
            public class TestClass : VBClass
            {
                public int fo[||]obar = 0;
            }
        </Document>
  </Project>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly"" CommonReferences=""true"">
    <Document>
        Public Class VBClass
        End Class
    </Document>
    </Project>
</Workspace>");
    }

    [Fact]
    public async Task TestPullPropertyUpToVBClassViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
<Workspace>
  <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
    <ProjectReference>VBAssembly</ProjectReference>
    <Document>
using VBAssembly;
public class TestClass : VBClass
{
    public int foo[||]bar
    {
        get;
        set;
    }
}</Document>
  </Project>
  <Project Language=""Visual Basic"" AssemblyName=""VBAssembly"" CommonReferences=""true"">
    <Document>
        Public Class VBClass
        End Class
    </Document>
  </Project>
</Workspace>
");
    }

    [Fact]
    public async Task TestPullPropertyUpToVBInterfaceViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
using VBAssembly;
public class TestClass : VBInterface
        {
            public int foo[||]bar
            {
                get;
                set;
            }
        }
        </Document>
  </Project>
    <Project Language = ""Visual Basic"" AssemblyName=""VBAssembly"" CommonReferences=""true"">
        <Document>
            Public Interface VBInterface
            End Interface
        </Document>
    </Project>
</Workspace>");
    }

    [Fact]
    public async Task TestPullEventUpToVBClassViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <ProjectReference>VBAssembly</ProjectReference>
            <Document>
            using VBAssembly;
            public class TestClass : VBClass
            {
                public event EventHandler BarEve[||]nt;
            }
            </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly"" CommonReferences=""true"">
        <Document>
            Public Class VBClass
            End Class
        </Document>
    </Project>
</Workspace>");
    }

    [Fact]
    public async Task TestPullEventUpToVBInterfaceViaQuickAction()
    {
        await TestQuickActionNotProvidedAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
    <ProjectReference>VBAssembly</ProjectReference>
    <Document>
        using VBAssembly;
        public class TestClass : VBInterface
        {
            public event EventHandler BarEve[||]nt;
        }
    </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly"" CommonReferences=""true"">
    <Document>
        Public Interface VBInterface
        End Interface
    </Document>
    </Project>
</Workspace>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55746")]
    public async Task TestPullMethodWithToClassWithAddUsingsInsideNamespaceViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(
            @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace N
{
    public class Base
    {
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

namespace N
{
    public class Derived : Base
    {
        public Uri En[||]dpoint()
        {
            return new Uri(""http://localhost"");
        }
    }
}
        </Document>
    </Project>
</Workspace>
",
            @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace N
{
    using System;

    public class Base
    {
        public Uri Endpoint()
        {
            return new Uri(""http://localhost"");
        }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;

namespace N
{
    public class Derived : Base
    {
    }
}
        </Document>
    </Project>
</Workspace>
",
            options: Option(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, AddImportPlacement.InsideNamespace, CodeStyle.NotificationOption2.Silent));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55746")]
    public async Task TestPullMethodWithToClassWithAddUsingsSystemUsingsLastViaQuickAction()
    {
        await TestWithPullMemberDialogAsync(
            @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">
namespace N1
{
    public class Base
    {
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;
using N2;

namespace N1
{
    public class Derived : Base
    {
        public Goo Ge[||]tGoo()
        {
            return new Goo(String.Empty);
        }
    }
}

namespace N2
{
    public class Goo
    {
        public Goo(String s)
        {
        }
    }
}
        </Document>
    </Project>
</Workspace>
",
            @"
<Workspace>
    <Project Language = ""C#""  LanguageVersion=""preview"" CommonReferences=""true"">
        <Document FilePath = ""File1.cs"">using N2;
using System;

namespace N1
{
    public class Base
    {
        public Goo GetGoo()
        {
            return new Goo(String.Empty);
        }
    }
}
        </Document>
        <Document FilePath = ""File2.cs"">
using System;
using N2;

namespace N1
{
    public class Derived : Base
    {
    }
}

namespace N2
{
    public class Goo
    {
        public Goo(String s)
        {
        }
    }
}
        </Document>
    </Project>
</Workspace>
",
            options: new(GetLanguage())
            {
                { GenerationOptions.PlaceSystemNamespaceFirst, false },
            });
    }

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullMethodToClassWithDirective()
    {
        return TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    #region Hello
    public void G[||]oo() { }
    #endregion
}", @"
public class BaseClass
{
    public void Goo() { }
}

public class Bar : BaseClass
{

    #region Hello
    #endregion
}");
    }

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullMethodToClassBeforeDirective()
    {
        return TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public void H[||]ello() { }
    #region Hello
    public void Goo() { }
    #endregion
}", @"
public class BaseClass
{
    public void Hello() { }
}

public class Bar : BaseClass
{
    #region Hello
    public void Goo() { }
    #endregion
}");
    }

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullMethodToClassBeforeDirective2()
    {
        return TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public void Hello() { }

    #region Hello
    public void G[||]oo() { }
    #endregion
}", @"
public class BaseClass
{

    public void Goo() { }
}

public class Bar : BaseClass
{
    public void Hello() { }

    #region Hello
    #endregion
}");
    }

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullFieldToClassBeforeDirective1()
    {
        return TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int ba[||]r = 10;
    #region Hello
    public int Goo = 10;
    #endregion
}", @"
public class BaseClass
{
    public int bar = 10;
}

public class Bar : BaseClass
{
    #region Hello
    public int Goo = 10;
    #endregion
}");
    }

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullFieldToClassBeforeDirective2()
    {
        return TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int bar = 10;
    #region Hello
    public int Go[||]o = 10;
    #endregion
}", @"
public class BaseClass
{
    public int Goo = 10;
}

public class Bar : BaseClass
{
    public int bar = 10;

    #region Hello
    #endregion
}");
    }

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullFieldToClassBeforeDirective()
    {
        return TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    #region Hello
    public int G[||]oo = 100, Hoo;
    #endregion
}", @"
public class BaseClass
{
    public int Goo = 100;
}

public class Bar : BaseClass
{
    #region Hello
    public int Hoo;
    #endregion
}");
    }

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullEventToClassBeforeDirective()
    {
        return TestWithPullMemberDialogAsync(@"
using System;
public class BaseClass
{
}

public class Bar : BaseClass
{
    #region Hello
    public event EventHandler e[||]1;
    #endregion
}", @"
using System;
public class BaseClass
{
    public event EventHandler e1;
}

public class Bar : BaseClass
{

    #region Hello
    #endregion
}");
    }

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullPropertyToClassBeforeDirective()
    {
        return TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    #region Hello
    public int Go[||]o => 1;
    #endregion
}", @"
public class BaseClass
{
    public int Goo => 1;
}

public class Bar : BaseClass
{

    #region Hello
    #endregion
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55402")]
    public Task TestPullPropertyToClassOnKeyword()
    {
        return TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Derived : BaseClass
            {
                $$public int I => 1;
            }
            """, """
            public class BaseClass
            {
                $$public int I => 1;
            }
            
            public class Derived : BaseClass
            {
            }
            """);
    }

    #endregion Quick Action

    #region Dialog

    internal Task TestWithPullMemberDialogAsync(
        string initialMarkUp,
        string expectedResult,
        IEnumerable<(string name, bool makeAbstract)> selection = null,
        string destinationName = null,
        int index = 0,
        TestParameters parameters = null,
        OptionsCollection options = null)
    {
        var service = new TestPullMemberUpService(selection, destinationName);

        return TestInRegularAndScript1Async(
            initialMarkUp, expectedResult,
            (parameters ?? TestParameters.Default).WithFixProviderData(service).WithOptions(options).WithIndex(index));
    }

    [Fact]
    public async Task PullPartialMethodUpToInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    partial interface IInterface
    {
    }

    public partial class TestClass : IInterface
    {
        partial void Bar[||]Bar()
    }

    public partial class TestClass
    {
        partial void BarBar()
        {}
    }

    partial interface IInterface
    {
    }
}", @"
using System;
namespace PushUpTest
{
    partial interface IInterface
    {
        void BarBar();
    }

    public partial class TestClass : IInterface
    {
        void BarBar()
    }

    public partial class TestClass
    {
        partial void BarBar()
        {}
    }

    partial interface IInterface
    {
    }
}");
    }

    [Fact]
    public async Task PullExtendedPartialMethodUpToInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    partial interface IInterface
    {
    }

    public partial class TestClass : IInterface
    {
        public partial void Bar[||]Bar()
    }

    public partial class TestClass
    {
        public partial void BarBar()
        {}
    }

    partial interface IInterface
    {
    }
}", @"
using System;
namespace PushUpTest
{
    partial interface IInterface
    {
        void BarBar();
    }

    public partial class TestClass : IInterface
    {
        public partial void BarBar()
    }

    public partial class TestClass
    {
        public partial void BarBar()
        {}
    }

    partial interface IInterface
    {
    }
}");
    }

    [Fact]
    public async Task PullMultipleNonPublicMethodsToInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public void TestMethod()
        {
            System.Console.WriteLine(""Hello World"");
        }

        protected void F[||]oo(int i)
        {
            // do awesome things
        }

        private static string Bar(string x)
        {}
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        string Bar(string x);
        void Foo(int i);
        void TestMethod();
    }

    public class TestClass : IInterface
    {
        public void TestMethod()
        {
            System.Console.WriteLine(""Hello World"");
        }

        public void Foo(int i)
        {
            // do awesome things
        }

        public string Bar(string x)
        {}
    }
}");
    }

    [Fact]
    public async Task PullMultipleNonPublicEventsToInterface()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        private event EventHandler Event1, Eve[||]nt2, Event3;
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        event EventHandler Event1;
        event EventHandler Event2;
        event EventHandler Event3;
    }

    public class TestClass : IInterface
    {
        public event EventHandler Event1;
        public event EventHandler Event2;
        public event EventHandler Event3;
    }
}");
    }

    [Fact]
    public async Task PullMethodToInnerInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public class TestClass : TestClass.IInterface
    {
        private void Bar[||]Bar()
        {
        }
        interface IInterface
        {
        }
    }
}", @"
using System;
namespace PushUpTest
{
    public class TestClass : TestClass.IInterface
    {
        public void BarBar()
        {
        }
        interface IInterface
        {
            void BarBar();
        }
    }
}");
    }

    [Fact]
    public async Task PullDifferentMembersFromClassToPartialInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    partial interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public int th[||]is[int i]
        {
            get => j = value;
        }

        private static void BarBar()
        {}
        
        protected static event EventHandler event1, event2;

        internal static int Foo
        {
            get; set;
        }
    }
    partial interface IInterface
    {
    }
}", @"
using System;
namespace PushUpTest
{
    partial interface IInterface
    {
        int this[int i] { get; }

        int Foo { get; set; }

        event EventHandler event1;
        event EventHandler event2;

        void BarBar();
    }

    public class TestClass : IInterface
    {
        public int this[int i]
        {
            get => j = value;
        }

        public void BarBar()
        {}

        public event EventHandler event1;
        public event EventHandler event2;

        public int Foo
        {
            get; set;
        }
    }
    partial interface IInterface
    {
    }
}", index: 1);
    }

    [Fact]
    public async Task TestPullAsyncMethod()
    {
        await TestWithPullMemberDialogAsync(@"
using System.Threading.Tasks;

internal interface IPullUp { }

internal class PullUp : IPullUp
{
    internal async Task PullU[||]pAsync()
    {
        await Task.Delay(1000);
    }
}", @"
using System.Threading.Tasks;

internal interface IPullUp
{
    Task PullUpAsync();
}

internal class PullUp : IPullUp
{
    public async Task PullUpAsync()
    {
        await Task.Delay(1000);
    }
}");
    }

    [Fact]
    public async Task PullMethodWithAbstractOptionToClassViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public void TestMeth[||]od()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}", @"
namespace PushUpTest
{
    public abstract class Base
    {
        public abstract void TestMethod();
    }

    public class TestClass : Base
    {
        public override void TestMeth[||]od()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}", [("TestMethod", true)], index: 1);
    }

    [Fact]
    public async Task PullAbstractMethodToClassViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class Base
    {
    }

    public abstract class TestClass : Base
    {
        public abstract void TestMeth[||]od();
    }
}", @"
namespace PushUpTest
{
    public abstract class Base
    {
        public abstract void TestMethod();
    }

    public abstract class TestClass : Base
    {
    }
}", [("TestMethod", true)], index: 0);
    }

    [Fact]
    public async Task PullMultipleEventsToClassViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public class Base2
    {
    }

    public class Testclass2 : Base2
    {
        private static event EventHandler Event1, Eve[||]nt3, Event4;
    }
}", @"
using System;
namespace PushUpTest
{
    public class Base2
    {
        private static event EventHandler Event1;
        private static event EventHandler Event3;
        private static event EventHandler Event4;
    }

    public class Testclass2 : Base2
    {
    }
}", index: 1);
    }

    [Fact]
    public async Task PullMultipleAbstractEventsToInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public interface ITest 
    {
    }

    public abstract class Testclass2 : ITest
    {
        protected abstract event EventHandler Event1, Eve[||]nt3, Event4;
    }
}", @"
using System;
namespace PushUpTest
{
    public interface ITest
    {
        event EventHandler Event1;
        event EventHandler Event3;
        event EventHandler Event4;
    }

    public abstract class Testclass2 : ITest
    {
        public abstract event EventHandler Event1;
        public abstract event EventHandler Event3;
        public abstract event EventHandler Event4;
    }
}");
    }

    [Fact]
    public async Task PullAbstractEventToClassViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public class Base2
    {
    }

    public abstract class Testclass2 : Base2
    {
        private static abstract event EventHandler Event1, Eve[||]nt3, Event4;
    }
}", @"
using System;
namespace PushUpTest
{
    public class Base2
    {
        private static event EventHandler Event3;
    }

    public abstract class Testclass2 : Base2
    {
        private static abstract event EventHandler Event1, Event4;
    }
}", [("Event3", false)]);
    }

    [Fact]
    public async Task PullNonPublicEventToInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public interface ITest
    {
    }

    public class Testclass2 : ITest
    {
        private event EventHandler Eve[||]nt3;
    }
}", @"
using System;
namespace PushUpTest
{
    public interface ITest
    {
        event EventHandler Event3;
    }

    public class Testclass2 : ITest
    {
        public event EventHandler Event3;
    }
}", [("Event3", false)]);
    }

    [Fact]
    public async Task PullSingleNonPublicEventToInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public interface ITest
    {
    }

    public abstract class TestClass2 : ITest
    {
        protected event EventHandler Eve[||]nt3;
    }
}", @"
using System;
namespace PushUpTest
{
    public interface ITest
    {
        event EventHandler Event3;
    }

    public abstract class TestClass2 : ITest
    {
        public event EventHandler Event3;
    }
}", [("Event3", false)]);
    }

    [Fact]
    public async Task TestPullNonPublicEventWithAddAndRemoveMethodToInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        private event EventHandler Eve[||]nt1
        {
            add
            {
                System.Console.Writeline(""This is add"");
            }
            remove
            {
                System.Console.Writeline(""This is remove"");
            }
        }
    }
}", @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        event EventHandler Event1;
    }

    public class TestClass : IInterface
    {
        public event EventHandler Event1
        {
            add
            {
                System.Console.Writeline(""This is add"");
            }
            remove
            {
                System.Console.Writeline(""This is remove"");
            }
        }
    }
}", [("Event1", false)]);
    }

    [Fact]
    public async Task PullFieldsToClassViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public class Base2
    {
    }

    public class Testclass2 : Base2
    {
        public int i, [||]j = 10, k = 100;
    }
}", @"
using System;
namespace PushUpTest
{
    public class Base2
    {
        public int i;
        public int j = 10;
        public int k = 100;
    }

    public class Testclass2 : Base2
    {
    }
}", index: 1);
    }

    [Fact]
    public async Task PullNonPublicPropertyWithArrowToInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public interface ITest
    {
    }

    public class Testclass2 : ITest
    {
        private double Test[||]Property => 2.717;
    }
}", @"
using System;
namespace PushUpTest
{
    public interface ITest
    {
        double TestProperty { get; }
    }

    public class Testclass2 : ITest
    {
        public readonly double TestProperty => 2.717;
    }
}");
    }

    [Fact]
    public async Task PullNonPublicPropertyToInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public interface ITest
    {
    }

    public class Testclass2 : ITest
    {
        private double Test[||]Property
        {
            get;
            set;
        }
    }
}", @"
using System;
namespace PushUpTest
{
    public interface ITest
    {
        double TestProperty { get; set; }
    }

    public class Testclass2 : ITest
    {
        public double TestProperty
        {
            get;
            set;
        }
    }
}");
    }

    [Fact]
    public async Task PullNonPublicPropertyWithSingleAccessorToInterfaceViaDialog()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public interface ITest
    {
    }

    public class Testclass2 : ITest
    {
        private static double Test[||]Property
        {
            set;
        }
    }
}", @"
using System;
namespace PushUpTest
{
    public interface ITest
    {
        double TestProperty { set; }
    }

    public class Testclass2 : ITest
    {
        public double Test[||]Property
        {
            set;
        }
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34268")]
    public async Task TestPullPropertyToAbstractClassViaDialogWithMakeAbstractOption()
    {
        await TestWithPullMemberDialogAsync(@"
abstract class B
{
}

class D : B
{
    int [||]X => 7;
}", @"
abstract class B
{
    private abstract int X { get; }
}

class D : B
{
    override int X => 7;
}", selection: [("X", true)], index: 1);
    }

    [Fact]
    public async Task PullEventUpToAbstractClassViaDialogWithMakeAbstractOption()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public class Base2
    {
    }

    public class Testclass2 : Base2
    {
        private event EventHandler Event1, Eve[||]nt3, Event4;
    }
}", @"
using System;
namespace PushUpTest
{
    public abstract class Base2
    {
        private abstract event EventHandler Event3;
    }

    public class Testclass2 : Base2
    {
        private event EventHandler Event1;
        private override event EventHandler Eve[||]nt3;
        private event EventHandler Event4;
    }
}", selection: [("Event3", true)], index: 1);
    }

    [Fact]
    public async Task TestPullEventWithAddAndRemoveMethodToClassViaDialogWithMakeAbstractOption()
    {
        await TestWithPullMemberDialogAsync(@"
using System;
namespace PushUpTest
{
    public class BaseClass
    {
    }

    public class TestClass : BaseClass
    {
        public event EventHandler Eve[||]nt1
        {
            add
            {
                System.Console.Writeline(""This is add"");
            }
            remove
            {
                System.Console.Writeline(""This is remove"");
            }
        }
    }
}", @"
using System;
namespace PushUpTest
{
    public abstract class BaseClass
    {
        public abstract event EventHandler Event1;
    }

    public class TestClass : BaseClass
    {
        public override event EventHandler Event1
        {
            add
            {
                System.Console.Writeline(""This is add"");
            }
            remove
            {
                System.Console.Writeline(""This is remove"");
            }
        }
    }
}", [("Event1", true)], index: 1);
    }

    #endregion Dialog

    #region Selections and caret position
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestArgsIsPartOfHeader()
    {
        await TestWithPullMemberDialogAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    class Test2Attribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [Test]
        [Test2]
        void C([||])
        {
        }
    }
}", @"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    class Test2Attribute : Attribute { }
    public class A
    {
        [Test]
        [Test2]
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestRefactoringCaretBeforeAttributes()
    {
        await TestWithPullMemberDialogAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    class Test2Attribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [||][Test]
        [Test2]
        void C()
        {
        }
    }
}", @"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    class Test2Attribute : Attribute { }
    public class A
    {
        [Test]
        [Test2]
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestMissingRefactoringCaretBetweenAttributes()
    {
        await TestQuickActionNotProvidedAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    class Test2Attribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [Test]
        [||][Test2]
        void C()
        {
        }
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestRefactoringSelectionWithAttributes1()
    {
        await TestWithPullMemberDialogAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [Test]
        [|void C()
        {
        }|]
    }
}", @"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    public class A
    {
        [Test]
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestRefactoringSelectionWithAttributes2()
    {
        await TestWithPullMemberDialogAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [|[Test]
        void C()
        {
        }|]
    }
}", @"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    public class A
    {
        [Test]
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestRefactoringSelectionWithAttributes3()
    {
        await TestWithPullMemberDialogAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [Test][|
        void C()
        {
        }
        |]
    }
}", @"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    public class A
    {
        [Test]
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestMissingRefactoringInAttributeList()
    {
        await TestQuickActionNotProvidedAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [[||]Test]
        void C()
        {
        }
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestMissingRefactoringSelectionAttributeList()
    {
        await TestQuickActionNotProvidedAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    class Test2Attribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [|[Test]
        [Test2]|]
        void C()
        {
        }
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestMissingRefactoringCaretInAttributeList()
    {
        await TestQuickActionNotProvidedAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    class Test2Attribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [[||]Test]
        [Test2]
        void C()
        {
        }
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestMissingRefactoringCaretBetweenAttributeLists()
    {
        await TestQuickActionNotProvidedAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    class Test2Attribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [Test]
        [||][Test2]
        void C()
        {
        }
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestMissingRefactoringSelectionAttributeList2()
    {
        await TestQuickActionNotProvidedAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    class Test2Attribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [|[Test]|]
        [Test2]
        void C()
        {
        }
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestMissingRefactoringSelectAttributeList()
    {
        await TestQuickActionNotProvidedAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [|[Test]|]
        void C()
        {
        }
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestRefactoringCaretLocAfterAttributes1()
    {
        await TestWithPullMemberDialogAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [Test]
        [||]void C()
        {
        }
    }
}", @"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    public class A
    {
        [Test]
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestRefactoringCaretLocAfterAttributes2()
    {
        await TestWithPullMemberDialogAsync(@"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    class Test2Attribute : Attribute { }
    public class A
    {
    }

    public class B : A
    {
        [Test]
        // Comment1
        [Test2]
        // Comment2
        [||]void C()
        {
        }
    }
}", @"
using System;

namespace PushUpTest
{
    class TestAttribute : Attribute { }
    class Test2Attribute : Attribute { }
    public class A
    {
        [Test]
        // Comment1
        [Test2]
        // Comment2
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestRefactoringCaretLoc1()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class A
    {
    }

    public class B : A
    {
        [||]void C()
        {
        }
    }
}", @"
namespace PushUpTest
{
    public class A
    {
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestRefactoringSelection()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class A
    {
    }

    public class B : A
    {
        [|void C()
        {
        }|]
    }
}", @"
namespace PushUpTest
{
    public class A
    {
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestRefactoringSelectionComments()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class A
    {
    }

    public class B : A
    {  [|
        // Comment1
        void C()
        {
        }|]
    }
}", @"
namespace PushUpTest
{
    public class A
    {
        // Comment1
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestRefactoringSelectionComments2()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class A
    {
    }

    public class B : A
    {  
        [|/// <summary>
        /// Test
        /// </summary>
        void C()
        {
        }|]
    }
}", @"
namespace PushUpTest
{
    public class A
    {
        /// <summary>
        /// Test
        /// </summary>
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public async Task TestRefactoringSelectionComments3()
    {
        await TestWithPullMemberDialogAsync(@"
namespace PushUpTest
{
    public class A
    {
    }

    public class B : A
    {  
        /// <summary>
        [|/// Test
        /// </summary>
        void C()
        {
        }|]
    }
}", @"
namespace PushUpTest
{
    public class A
    {
        /// <summary>
        /// Test
        /// </summary>
        void C()
        {
        }
    }

    public class B : A
    {
    }
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionFieldKeyword1_NoAction()
    {
        await TestQuickActionNotProvidedAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    pub[|l|]ic int Goo = 10;
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionFieldKeyword2()
    {
        await TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    pub[||]lic int Goo = 10;
}", @"
public class BaseClass
{
    public int Goo = 10;
}

public class Bar : BaseClass
{
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionFieldAfterSemicolon()
    {
        await TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int Goo = 10;[||]
}", @"
public class BaseClass
{
    public int Goo = 10;
}

public class Bar : BaseClass
{
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionFieldEntireDeclaration()
    {
        await TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    [|public int Goo = 10;|]
}", @"
public class BaseClass
{
    public int Goo = 10;
}

public class Bar : BaseClass
{
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionMultipleFieldsInDeclaration1()
    {
        await TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    [|public int Goo = 10, Foo = 9;|]
}", @"
public class BaseClass
{
    public int Goo = 10;
    public int Foo = 9;
}

public class Bar : BaseClass
{
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionMultipleFieldsInDeclaration2()
    {
        await TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int Go[||]o = 10, Foo = 9;
}", @"
public class BaseClass
{
    public int Goo = 10;
}

public class Bar : BaseClass
{
    public int Foo = 9;
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionMultipleFieldsInDeclaration3()
    {
        await TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int Goo = 10, [||]Foo = 9;
}", @"
public class BaseClass
{
    public int Foo = 9;
}

public class Bar : BaseClass
{
    public int Goo = 10;
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionMultipleMembers1()
    {
        await TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    [|public int Goo = 10, Foo = 9;

    public int DoSomething()
    {
        return 5;
    }|]
}", @"
public class BaseClass
{
    public int Goo = 10;
    public int Foo = 9;

    public int DoSomething()
    {
        return 5;
    }
}

public class Bar : BaseClass
{
}");
    }

    // Some of these have weird whitespace spacing that might suggest a bug
    [Fact]
    public async Task TestRefactoringSelectionMultipleMembers2()
    {
        await TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int DoSomething()
    {
        [|return 5;
    }


    public int Goo = 10, Foo = 9;|]
}", @"
public class BaseClass
{


    public int Goo = 10;


    public int Foo = 9;
}

public class Bar : BaseClass
{
    public int DoSomething()
    {
        return 5;
    }
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionMultipleMembers3()
    {
        await TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int DoSom[|ething()
    {
        return 5;
    }


    public int Go|]o = 10, Foo = 9;
}", @"
public class BaseClass
{


    public int Goo = 10;
    public int DoSomething()
    {
        return 5;
    }
}

public class Bar : BaseClass
{
    public int Foo = 9;
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionMultipleMembers4()
    {
        await TestWithPullMemberDialogAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int DoSomething()[|
    {
        return 5;
    }


    public int Goo = 10, F|]oo = 9;
}", @"
public class BaseClass
{


    public int Goo = 10;


    public int Foo = 9;
}

public class Bar : BaseClass
{
    public int DoSomething()
    {
        return 5;
    }
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionIncompleteField_NoAction1()
    {
        // we expect a diagnostic/error, but also we shouldn't provide the refactoring
        await TestQuickActionNotProvidedAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    publ[||] int Goo = 10;
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionIncompleteField_NoAction2()
    {
        // we expect a diagnostic/error, but also we shouldn't provide the refactoring
        await TestQuickActionNotProvidedAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    [|publicc int Goo = 10;|]
}");
    }

    [Fact]
    public async Task TestRefactoringSelectionIncompleteMethod_NoAction()
    {
        // we expect a diagnostic/error, but also we shouldn't provide the refactoring
        await TestQuickActionNotProvidedAsync(@"
public class BaseClass
{
}

public class Bar : BaseClass
{
    publ[||] int DoSomething() {
        return 5;
    }
}");
    }
    #endregion
}
