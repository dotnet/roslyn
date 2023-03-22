// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Test.Utilities.PullMemberUp;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PullMemberUp
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
    public class CSharpPullMemberUpTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
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
            var testText = @"
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
}";
            await TestActionCountAsync(testText, 0);
        }

        [Fact]
        public async Task TestNoRefactoringProvidedWhenPullFieldInInterfaceViaQuickAction()
        {
            var testText = @"
namespace PushUpTest
{
    public interface ITestInterface
    {
    }

    public class TestClass : ITestInterface
    {
        public int yo[||]u = 10086;
    }
}";
            await TestQuickActionNotProvidedAsync(testText);
        }

        [Fact]
        public async Task TestNoRefactoringProvidedWhenMethodDeclarationAlreadyExistsInInterfaceViaQuickAction()
        {
            var methodTest = @"
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
}";
            await TestQuickActionNotProvidedAsync(methodTest);
        }

        [Fact]
        public async Task TestNoRefactoringProvidedWhenPropertyDeclarationAlreadyExistsInInterfaceViaQuickAction()
        {
            var propertyTest1 = @"
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
}";
            await TestQuickActionNotProvidedAsync(propertyTest1);
        }

        [Fact]
        public async Task TestNoRefactoringProvidedWhenEventDeclarationAlreadyExistsToInterfaceViaQuickAction()
        {
            var eventTest = @"
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
}";
            await TestQuickActionNotProvidedAsync(eventTest);
        }

        [Fact]
        public async Task TestNoRefactoringProvidedInNestedTypesViaQuickAction()
        {
            var input = @"
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
}";

            await TestQuickActionNotProvidedAsync(input);
        }

        [Fact]
        public async Task TestPullMethodUpToInterfaceViaQuickAction()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullAbstractMethodToInterfaceViaQuickAction()
        {
            var testText = @"
namespace PushUpTest
{
    public interface IInterface
    {
    }

    public abstract class TestClass : IInterface
    {
        public abstract void TestMeth[||]od();
    }
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullGenericsUpToInterfaceViaQuickAction()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullSingleEventToInterfaceViaQuickAction()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullOneEventFromMultipleEventsToInterfaceViaQuickAction()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullPublicEventWithAccessorsToInterfaceViaQuickAction()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullPropertyWithPrivateSetterToInterfaceViaQuickAction()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullPropertyWithPrivateGetterToInterfaceViaQuickAction()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullMemberFromInterfaceToInterfaceViaQuickAction()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullIndexerWithOnlySetterToInterfaceViaQuickAction()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullIndexerWithOnlyGetterToInterfaceViaQuickAction()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullPropertyToInterfaceWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToInterfaceWithoutAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodWithNewReturnTypeToInterfaceWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodWithNewParamTypeToInterfaceWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullEventToInterfaceWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullPropertyToClassWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullPropertyToClassWithAddUsingsViaQuickAction2()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullPropertyToClassWithoutDuplicatingUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullPropertyWithNewBodyTypeToClassWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodWithNewNonDeclaredBodyTypeToClassWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithOverlappingUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithUnnecessaryFirstUsingViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithUnusedBaseUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithRetainCommentsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithRetainPreImportCommentsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithRetainPostImportCommentsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithLambdaUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithUnusedUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassKeepSystemFirstViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassKeepSystemFirstViaQuickAction2()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithExtensionViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithExtensionViaQuickAction2()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithAliasUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullPropertyToClassWithBaseAliasUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithMultipleNamespacedUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithNestedNamespacedUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithNewNamespaceUsingViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithFileNamespaceUsingViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithUnusedNamespaceUsingViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithMultipleNamespacesAndCommentsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithMultipleNamespacedUsingsAndCommentsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithNamespacedUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodToClassWithDuplicateNamespacedUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodWithNewReturnTypeToClassWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodWithNewParamTypeToClassWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullMethodWithNewBodyTypeToClassWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullEventToClassWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullFieldToClassWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
        public async Task TestPullFieldToClassNoConstructorWithAddUsingsViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestNoRefactoringProvidedWhenPullOverrideMethodUpToClassViaQuickAction()
        {
            var methodTest = @"
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
}";
            await TestQuickActionNotProvidedAsync(methodTest);
        }

        [Fact]
        public async Task TestNoRefactoringProvidedWhenPullOverridePropertyUpToClassViaQuickAction()
        {
            var propertyTest = @"
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
}";

            await TestQuickActionNotProvidedAsync(propertyTest);
        }

        [Fact]
        public async Task TestNoRefactoringProvidedWhenPullOverrideEventUpToClassViaQuickAction()
        {
            var eventTest = @"
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
}";
            await TestQuickActionNotProvidedAsync(eventTest);
        }

        [Fact]
        public async Task TestNoRefactoringProvidedWhenPullSameNameFieldUpToClassViaQuickAction()
        {
            // Fields share the same name will be thought as 'override', since it will cause error
            // if two same name fields exist in one class
            var fieldTest = @"
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
}";
            await TestQuickActionNotProvidedAsync(fieldTest);
        }

        [Fact]
        public async Task TestPullMethodToOrdinaryClassViaQuickAction()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullOneFieldsToClassViaQuickAction()
        {
            var testText = @"
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public int you[||]= 10086;
    }
}";

            var expected = @"
namespace PushUpTest
{
    public class Base
    {
        public int you = 10086;
    }

    public class TestClass : Base
    {
    }
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullGenericsUpToClassViaQuickAction()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullOneFieldFromMultipleFieldsToClassViaQuickAction()
        {
            var testText = @"
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public int you, a[||]nd, someone = 10086;
    }
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullMiddleFieldWithValueToClassViaQuickAction()
        {
            var testText = @"
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public int you, a[||]nd = 4000, someone = 10086;
    }
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullOneEventFromMultipleToClassViaQuickAction()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullEventToClassViaQuickAction()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullEventWithBodyToClassViaQuickAction()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullPropertyToClassViaQuickAction()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullIndexerToClassViaQuickAction()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullMethodUpAcrossProjectViaQuickAction()
        {
            var testText = @"
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
</Workspace>";

            var expected = @"
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
</Workspace>";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullPropertyUpAcrossProjectViaQuickAction()
        {
            var testText = @"
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
</Workspace>";

            var expected = @"
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
</Workspace>";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullFieldUpAcrossProjectViaQuickAction()
        {
            var testText = @"
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
</Workspace>";

            var expected = @"
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
</Workspace>";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestPullMethodUpToVBClassViaQuickAction()
        {
            // Moving member from C# to Visual Basic is not supported currently since the FindMostRelevantDeclarationAsync method in
            // AbstractCodeGenerationService will return null.
            var input = @"
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
</Workspace>";
            await TestQuickActionNotProvidedAsync(input);
        }

        [Fact]
        public async Task TestPullMethodUpToVBInterfaceViaQuickAction()
        {
            var input = @"
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
";
            await TestQuickActionNotProvidedAsync(input);
        }

        [Fact]
        public async Task TestPullFieldUpToVBClassViaQuickAction()
        {
            var input = @"
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
</Workspace>";

            await TestQuickActionNotProvidedAsync(input);
        }

        [Fact]
        public async Task TestPullPropertyUpToVBClassViaQuickAction()
        {
            var input = @"
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
";
            await TestQuickActionNotProvidedAsync(input);
        }

        [Fact]
        public async Task TestPullPropertyUpToVBInterfaceViaQuickAction()
        {
            var input = @"<Workspace>
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
</Workspace>";
            await TestQuickActionNotProvidedAsync(input);
        }

        [Fact]
        public async Task TestPullEventUpToVBClassViaQuickAction()
        {
            var input = @"
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
</Workspace>";
            await TestQuickActionNotProvidedAsync(input);
        }

        [Fact]
        public async Task TestPullEventUpToVBInterfaceViaQuickAction()
        {
            var input = @"
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
</Workspace>";
            await TestQuickActionNotProvidedAsync(input);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55746")]
        public async Task TestPullMethodWithToClassWithAddUsingsInsideNamespaceViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(
                testText,
                expected,
                options: Option(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, AddImportPlacement.InsideNamespace, CodeStyle.NotificationOption2.Silent));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55746")]
        public async Task TestPullMethodWithToClassWithAddUsingsSystemUsingsLastViaQuickAction()
        {
            var testText = @"
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
";
            var expected = @"
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
";
            await TestWithPullMemberDialogAsync(
                testText,
                expected,
                options: new(GetLanguage())
                {
                    { GenerationOptions.PlaceSystemNamespaceFirst, false },
                });
        }

        [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
        public Task TestPullMethodToClassWithDirective()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    #region Hello
    public void G[||]oo() { }
    #endregion
}";
            var expected = @"
public class BaseClass
{
    public void Goo() { }
}

public class Bar : BaseClass
{

    #region Hello
    #endregion
}";
            return TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
        public Task TestPullMethodToClassBeforeDirective()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public void H[||]ello() { }
    #region Hello
    public void Goo() { }
    #endregion
}";
            var expected = @"
public class BaseClass
{
    public void Hello() { }
}

public class Bar : BaseClass
{
    #region Hello
    public void Goo() { }
    #endregion
}";
            return TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
        public Task TestPullMethodToClassBeforeDirective2()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public void Hello() { }

    #region Hello
    public void G[||]oo() { }
    #endregion
}";
            var expected = @"
public class BaseClass
{

    public void Goo() { }
}

public class Bar : BaseClass
{
    public void Hello() { }

    #region Hello
    #endregion
}";
            return TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
        public Task TestPullFieldToClassBeforeDirective1()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int ba[||]r = 10;
    #region Hello
    public int Goo = 10;
    #endregion
}";
            var expected = @"
public class BaseClass
{
    public int bar = 10;
}

public class Bar : BaseClass
{
    #region Hello
    public int Goo = 10;
    #endregion
}";
            return TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
        public Task TestPullFieldToClassBeforeDirective2()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int bar = 10;
    #region Hello
    public int Go[||]o = 10;
    #endregion
}";
            var expected = @"
public class BaseClass
{
    public int Goo = 10;
}

public class Bar : BaseClass
{
    public int bar = 10;

    #region Hello
    #endregion
}";
            return TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
        public Task TestPullFieldToClassBeforeDirective()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    #region Hello
    public int G[||]oo = 100, Hoo;
    #endregion
}";
            var expected = @"
public class BaseClass
{
    public int Goo = 100;
}

public class Bar : BaseClass
{
    #region Hello
    public int Hoo;
    #endregion
}";
            return TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
        public Task TestPullEventToClassBeforeDirective()
        {
            var text = @"
using System;
public class BaseClass
{
}

public class Bar : BaseClass
{
    #region Hello
    public event EventHandler e[||]1;
    #endregion
}";
            var expected = @"
using System;
public class BaseClass
{
    public event EventHandler e1;
}

public class Bar : BaseClass
{

    #region Hello
    #endregion
}";
            return TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
        public Task TestPullPropertyToClassBeforeDirective()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    #region Hello
    public int Go[||]o => 1;
    #endregion
}";
            var expected = @"
public class BaseClass
{
    public int Goo => 1;
}

public class Bar : BaseClass
{

    #region Hello
    #endregion
}";
            return TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55402")]
        public Task TestPullPropertyToClassOnKeyword()
        {
            var text = """
                public class BaseClass
                {
                }

                public class Derived : BaseClass
                {
                    $$public int I => 1;
                }
                """;

            var expected = """
                public class BaseClass
                {
                    $$public int I => 1;
                }
                
                public class Derived : BaseClass
                {
                }
                """;

            return TestWithPullMemberDialogAsync(text, expected);
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
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task PullExtendedPartialMethodUpToInterfaceViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task PullMultipleNonPublicMethodsToInterfaceViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task PullMultipleNonPublicEventsToInterface()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task PullMethodToInnerInterfaceViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task PullDifferentMembersFromClassToPartialInterfaceViaDialog()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected, index: 1);
        }

        [Fact]
        public async Task TestPullAsyncMethod()
        {
            var testText = @"
using System.Threading.Tasks;

internal interface IPullUp { }

internal class PullUp : IPullUp
{
    internal async Task PullU[||]pAsync()
    {
        await Task.Delay(1000);
    }
}";
            var expectedText = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expectedText);
        }

        [Fact]
        public async Task PullMethodWithAbstractOptionToClassViaDialog()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("TestMethod", true) }, index: 1);
        }

        [Fact]
        public async Task PullAbstractMethodToClassViaDialog()
        {
            var testText = @"
namespace PushUpTest
{
    public class Base
    {
    }

    public abstract class TestClass : Base
    {
        public abstract void TestMeth[||]od();
    }
}";

            var expected = @"
namespace PushUpTest
{
    public abstract class Base
    {
        public abstract void TestMethod();
    }

    public abstract class TestClass : Base
    {
    }
}";
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("TestMethod", true) }, index: 0);
        }

        [Fact]
        public async Task PullMultipleEventsToClassViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected, index: 1);
        }

        [Fact]
        public async Task PullMultipleAbstractEventsToInterfaceViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task PullAbstractEventToClassViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("Event3", false) });
        }

        [Fact]
        public async Task PullNonPublicEventToInterfaceViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("Event3", false) });
        }

        [Fact]
        public async Task PullSingleNonPublicEventToInterfaceViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("Event3", false) });
        }

        [Fact]
        public async Task TestPullNonPublicEventWithAddAndRemoveMethodToInterfaceViaDialog()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("Event1", false) });
        }

        [Fact]
        public async Task PullFieldsToClassViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected, index: 1);
        }

        [Fact]
        public async Task PullNonPublicPropertyWithArrowToInterfaceViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task PullNonPublicPropertyToInterfaceViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task PullNonPublicPropertyWithSingleAccessorToInterfaceViaDialog()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34268")]
        public async Task TestPullPropertyToAbstractClassViaDialogWithMakeAbstractOption()
        {
            var testText = @"
abstract class B
{
}

class D : B
{
    int [||]X => 7;
}";
            var expected = @"
abstract class B
{
    private abstract int X { get; }
}

class D : B
{
    override int X => 7;
}";
            await TestWithPullMemberDialogAsync(testText, expected, selection: new[] { ("X", true) }, index: 1);
        }

        [Fact]
        public async Task PullEventUpToAbstractClassViaDialogWithMakeAbstractOption()
        {
            var testText = @"
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
}";
            var expected = @"
using System;
namespace PushUpTest
{
    public abstract class Base2
    {
        private abstract event EventHandler Event3;
    }

    public class Testclass2 : Base2
    {
        private event EventHandler Event1, Eve[||]nt3, Event4;
    }
}";
            await TestWithPullMemberDialogAsync(testText, expected, selection: new[] { ("Event3", true) }, index: 1);
        }

        [Fact]
        public async Task TestPullEventWithAddAndRemoveMethodToClassViaDialogWithMakeAbstractOption()
        {
            var testText = @"
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
}";

            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("Event1", true) }, index: 1);
        }

        #endregion Dialog

        #region Selections and caret position
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestArgsIsPartOfHeader()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestRefactoringCaretBeforeAttributes()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMissingRefactoringCaretBetweenAttributes()
        {
            var testText = @"
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
}";
            await TestQuickActionNotProvidedAsync(testText);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestRefactoringSelectionWithAttributes1()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestRefactoringSelectionWithAttributes2()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestRefactoringSelectionWithAttributes3()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMissingRefactoringInAttributeList()
        {
            var testText = @"
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
}";
            await TestQuickActionNotProvidedAsync(testText);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMissingRefactoringSelectionAttributeList()
        {
            var testText = @"
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
}";
            await TestQuickActionNotProvidedAsync(testText);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMissingRefactoringCaretInAttributeList()
        {
            var testText = @"
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
}";
            await TestQuickActionNotProvidedAsync(testText);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMissingRefactoringCaretBetweenAttributeLists()
        {
            var testText = @"
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
}";
            await TestQuickActionNotProvidedAsync(testText);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMissingRefactoringSelectionAttributeList2()
        {
            var testText = @"
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
}";
            await TestQuickActionNotProvidedAsync(testText);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMissingRefactoringSelectAttributeList()
        {
            var testText = @"
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
}";
            await TestQuickActionNotProvidedAsync(testText);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestRefactoringCaretLocAfterAttributes1()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestRefactoringCaretLocAfterAttributes2()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestRefactoringCaretLoc1()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestRefactoringSelection()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestRefactoringSelectionComments()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestRefactoringSelectionComments2()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestRefactoringSelectionComments3()
        {
            var testText = @"
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
}";
            var expected = @"
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
}";

            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact]
        public async Task TestRefactoringSelectionFieldKeyword1_NoAction()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    pub[|l|]ic int Goo = 10;
}";
            await TestQuickActionNotProvidedAsync(text);
        }

        [Fact]
        public async Task TestRefactoringSelectionFieldKeyword2()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    pub[||]lic int Goo = 10;
}";
            var expected = @"
public class BaseClass
{
    public int Goo = 10;
}

public class Bar : BaseClass
{
}";
            await TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact]
        public async Task TestRefactoringSelectionFieldAfterSemicolon()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int Goo = 10;[||]
}";
            var expected = @"
public class BaseClass
{
    public int Goo = 10;
}

public class Bar : BaseClass
{
}";
            await TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact]
        public async Task TestRefactoringSelectionFieldEntireDeclaration()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    [|public int Goo = 10;|]
}";
            var expected = @"
public class BaseClass
{
    public int Goo = 10;
}

public class Bar : BaseClass
{
}";
            await TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact]
        public async Task TestRefactoringSelectionMultipleFieldsInDeclaration1()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    [|public int Goo = 10, Foo = 9;|]
}";
            var expected = @"
public class BaseClass
{
    public int Goo = 10;
    public int Foo = 9;
}

public class Bar : BaseClass
{
}";
            await TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact]
        public async Task TestRefactoringSelectionMultipleFieldsInDeclaration2()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int Go[||]o = 10, Foo = 9;
}";
            var expected = @"
public class BaseClass
{
    public int Goo = 10;
}

public class Bar : BaseClass
{
    public int Foo = 9;
}";
            await TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact]
        public async Task TestRefactoringSelectionMultipleFieldsInDeclaration3()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    public int Goo = 10, [||]Foo = 9;
}";
            var expected = @"
public class BaseClass
{
    public int Foo = 9;
}

public class Bar : BaseClass
{
    public int Goo = 10;
}";
            await TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact]
        public async Task TestRefactoringSelectionMultipleMembers1()
        {
            var text = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(text, expected);
        }

        // Some of these have weird whitespace spacing that might suggest a bug
        [Fact]
        public async Task TestRefactoringSelectionMultipleMembers2()
        {
            var text = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact]
        public async Task TestRefactoringSelectionMultipleMembers3()
        {
            var text = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact]
        public async Task TestRefactoringSelectionMultipleMembers4()
        {
            var text = @"
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
}";
            var expected = @"
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
}";
            await TestWithPullMemberDialogAsync(text, expected);
        }

        [Fact]
        public async Task TestRefactoringSelectionIncompleteField_NoAction1()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    publ[||] int Goo = 10;
}";
            // we expect a diagnostic/error, but also we shouldn't provide the refactoring
            await TestQuickActionNotProvidedAsync(text);
        }

        [Fact]
        public async Task TestRefactoringSelectionIncompleteField_NoAction2()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    [|publicc int Goo = 10;|]
}";
            // we expect a diagnostic/error, but also we shouldn't provide the refactoring
            await TestQuickActionNotProvidedAsync(text);
        }

        [Fact]
        public async Task TestRefactoringSelectionIncompleteMethod_NoAction()
        {
            var text = @"
public class BaseClass
{
}

public class Bar : BaseClass
{
    publ[||] int DoSomething() {
        return 5;
    }
}";
            // we expect a diagnostic/error, but also we shouldn't provide the refactoring
            await TestQuickActionNotProvidedAsync(text);
        }
        #endregion
    }
}
