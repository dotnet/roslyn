// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        TestParameters? parameters = null)
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
    public Task TestNoRefactoringProvidedWhenNoOptionsService()
        => TestActionCountAsync("""
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
                        System.Console.WriteLine("Hello World");
                    }
                }
            }
            """, 0);

    [Fact]
    public Task TestNoRefactoringProvidedWhenPullFieldInInterfaceViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
            namespace PushUpTest
            {
                public interface ITestInterface
                {
                }

                public class TestClass : ITestInterface
                {
                    public int yo[||]u = 10086;
                }
            }
            """);

    [Fact]
    public Task TestNoRefactoringProvidedWhenMethodDeclarationAlreadyExistsInInterfaceViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
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
                        System.Console.WriteLine("Hello World");
                    }
                }
            }
            """);

    [Fact]
    public Task TestNoRefactoringProvidedWhenPropertyDeclarationAlreadyExistsInInterfaceViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact]
    public Task TestNoRefactoringProvidedWhenEventDeclarationAlreadyExistsToInterfaceViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact]
    public Task TestNoRefactoringProvidedInNestedTypesViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact]
    public Task TestPullMethodUpToInterfaceViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
                        System.Console.WriteLine("Hello World");
                    }
                }
            }
            """, """
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
                        System.Console.WriteLine("Hello World");
                    }
                }
            }
            """);

    [Fact]
    public Task TestPullAbstractMethodToInterfaceViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            namespace PushUpTest
            {
                public interface IInterface
                {
                }

                public abstract class TestClass : IInterface
                {
                    public abstract void TestMeth[||]od();
                }
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullGenericsUpToInterfaceViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullSingleEventToInterfaceViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
                            System.Console.Writeline("This is add");
                        }
                        remove
                        {
                            System.Console.Writeline("This is remove");
                        }
                    }
                }
            }
            """, """
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
                            System.Console.Writeline("This is add");
                        }
                        remove
                        {
                            System.Console.Writeline("This is remove");
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestPullOneEventFromMultipleEventsToInterfaceViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullPublicEventWithAccessorsToInterfaceViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
                            System.Console.Writeln("This is add in event1");
                        }
                        remove
                        {
                            System.Console.Writeln("This is remove in event2");
                        }
                    }
                }
            }
            """, """
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
                            System.Console.Writeln("This is add in event1");
                        }
                        remove
                        {
                            System.Console.Writeln("This is remove in event2");
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestPullPropertyWithPrivateSetterToInterfaceViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullPropertyWithPrivateGetterToInterfaceViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullMemberFromInterfaceToInterfaceViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullIndexerWithOnlySetterToInterfaceViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullIndexerWithOnlyGetterToInterfaceViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullPropertyToInterfaceWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public interface IBase
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : IBase
            {
                public Uri En[||]dpoint { get; set; }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using System;

            public interface IBase
            {
                Uri Endpoint { get; set; }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : IBase
            {
                public Uri Endpoint { get; set; }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToInterfaceWithoutAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public interface IBase
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : IBase
            {
                public bool Test[||]Method()
                {
                    var endpoint1 = new Uri("http://localhost");
                    var endpoint2 = new Uri("http://localhost");
                    return endpoint1.Equals(endpoint2);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public interface IBase
            {
                bool TestMethod();
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : IBase
            {
                public bool Test[||]Method()
                {
                    var endpoint1 = new Uri("http://localhost");
                    var endpoint2 = new Uri("http://localhost");
                    return endpoint1.Equals(endpoint2);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodWithNewReturnTypeToInterfaceWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public interface IBase
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : IBase
            {
                public Uri Test[||]Method()
                {
                    return new Uri("http://localhost");
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using System;

            public interface IBase
            {
                Uri TestMethod();
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : IBase
            {
                public Uri TestMethod()
                {
                    return new Uri("http://localhost");
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodWithNewParamTypeToInterfaceWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public interface IBase
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : IBase
            {
                public bool Test[||]Method(Uri endpoint)
                {
                    var localHost = new Uri("http://localhost");
                    return endpoint.Equals(localhost);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using System;

            public interface IBase
            {
                bool TestMethod(Uri endpoint);
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : IBase
            {
                public bool TestMethod(Uri endpoint)
                {
                    var localHost = new Uri("http://localhost");
                    return endpoint.Equals(localhost);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullEventToInterfaceWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public interface IBase
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : IBase
            {
                public event EventHandler Test[||]Event
                {
                    add
                    {
                        Console.WriteLine("adding event...");
                    }
                    remove
                    {
                        Console.WriteLine("removing event...");
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using System;

            public interface IBase
            {
                event EventHandler TestEvent;
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : IBase
            {
                public event EventHandler TestEvent
                {
                    add
                    {
                        Console.WriteLine("adding event...");
                    }
                    remove
                    {
                        Console.WriteLine("removing event...");
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullPropertyToClassWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public Uri En[||]dpoint { get; set; }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System;

            public class Base
            {
                public Uri Endpoint { get; set; }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullPropertyToClassWithAddUsingsViaQuickAction2()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public Uri En[||]dpoint { get; set; }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System;

            public class Base
            {
                public Uri Endpoint { get; set; }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullPropertyToClassWithoutDuplicatingUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using System;

            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public Uri En[||]dpoint { get; set; }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using System;

            public class Base
            {
                public Uri Endpoint { get; set; }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullPropertyWithNewBodyTypeToClassWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public bool Test[||]Property
                {
                    get
                    {
                        var endpoint1 = new Uri("http://localhost");
                        var endpoint2 = new Uri("http://localhost");
                        return endpoint1.Equals(endpoint2);
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System;

            public class Base
            {
                public bool TestProperty
                {
                    get
                    {
                        var endpoint1 = new Uri("http://localhost");
                        var endpoint2 = new Uri("http://localhost");
                        return endpoint1.Equals(endpoint2);
                    }
                }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodWithNewNonDeclaredBodyTypeToClassWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System.Linq;

            public class Base
            {
                public int Test[||]Method()
                {
                    return Enumerable.Range(0, 5).Sum();
                }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System.Linq;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithOverlappingUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
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
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
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
                    <Document FilePath = "File2.cs">
            using System.Linq;
            using System.Threading.Tasks;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithUnnecessaryFirstUsingViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">

            using System.Threading.Tasks;

            public class Base
            {
                public async Task&lt;int&gt; Get5Async()
                {
                    return 5;
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System.Linq;
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
                    <Document FilePath = "File2.cs">
            using System;
            using System.Linq;
            using System.Threading.Tasks;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithUnusedBaseUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using System;
            using System.Threading.Tasks;

            public class Base
            {
                public Uri Endpoint{ get; set; }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
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
                    <Document FilePath = "File2.cs">
            using System.Linq;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithRetainCommentsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            // blah blah

            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            // blah blah

            public class Base
            {
                public int TestMethod()
                {
                    return 5;
                }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System.Linq;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithRetainPreImportCommentsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            // blah blah
            using System.Linq;

            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public Uri End[||]point { get; set; }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            // blah blah
            using System;
            using System.Linq;

            public class Base
            {
                public Uri Endpoint { get; set; }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithRetainPostImportCommentsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using System.Linq;

            // blah blah
            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public Uri End[||]point { get; set; }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using System;
            using System.Linq;

            // blah blah
            public class Base
            {
                public Uri Endpoint { get; set; }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithLambdaUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;
            using System.Linq;

            public class Derived : Base
            {
                public int Test[||]Method()
                {
                    return Enumerable.Range(0, 5).
                        Select((n) => new Uri("http://" + n)).
                        Count((uri) => uri != null);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System;
            using System.Linq;

            public class Base
            {
                public int TestMethod()
                {
                    return Enumerable.Range(0, 5).
                        Select((n) => new Uri("http://" + n)).
                        Count((uri) => uri != null);
                }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;
            using System.Linq;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithUnusedUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using System;

            public class Base
            {
                public Uri Endpoint{ get; set; }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
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
                    <Document FilePath = "File2.cs">
            using System.Linq;
            using System.Threading.Tasks;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassKeepSystemFirstViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace TestNs1
            {
                using System;

                public class Base
                {
                    public Uri Endpoint{ get; set; }
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
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
                    <Document FilePath = "File2.cs">
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassKeepSystemFirstViaQuickAction2()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace TestNs1
            {
                public class Base
                {
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            namespace A_TestNs2
            {
                using System;
                using TestNs1;

                public class Derived : Base
                {
                    public Foo Test[||]Method()
                    {
                        var uri = new Uri("http://localhost");
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System;
            using A_TestNs2;

            namespace TestNs1
            {
                public class Base
                {
                    public Foo TestMethod()
                    {
                        var uri = new Uri("http://localhost");
                        return null;
                    }
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithExtensionViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
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
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using TestNs2;

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
                    <Document FilePath = "File2.cs">
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithExtensionViaQuickAction2()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace TestNs1
            {
                public class Base
                {
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
                    <Document FilePath = "File3.cs">
            namespace TestNs3
            {
                public class Foo
                {
                }
            }
                    </Document>
                    <Document FilePath = "File4.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using TestNs3;
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
                    <Document FilePath = "File2.cs">
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
                    <Document FilePath = "File3.cs">
            namespace TestNs3
            {
                public class Foo
                {
                }
            }
                    </Document>
                    <Document FilePath = "File4.cs">
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithAliasUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using System;

            public class Base
            {
                public Uri Endpoint{ get; set; }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
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
                    <Document FilePath = "File2.cs">
            using Enumer = System.Linq.Enumerable;
            using Sys = System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullPropertyToClassWithBaseAliasUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            using Enumer = System.Linq.Enumerable;

            public class Base
            {
                public void TestMethod()
                {
                    System.Console.WriteLine(Enumer.Range(0, 5).Sum());
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public Uri End[||]point{ get; set; }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
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
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithMultipleNamespacedUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithNestedNamespacedUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithNewNamespaceUsingViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace A.B
            {
                class Base
                {
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using X.Y;

            namespace A.B
            {
                class Base
                {
                    public Other GetOther() => null;
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithFileNamespaceUsingViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace A.B;

            class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using X.Y;

            namespace A.B;

            class Base
            {
                public Other GetOther() => null;
            }
            </Document>
                    <Document FilePath = "File2.cs">
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithUnusedNamespaceUsingViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace A.B
            {
                class Base
                {
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace A.B
            {
                class Base
                {
                    public int GetFive() => 5;
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithMultipleNamespacesAndCommentsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithMultipleNamespacedUsingsAndCommentsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithNamespacedUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace ClassLibrary1
            {
                using System;

                public class Base
                {
                    public Uri Endpoint{ get; set; }
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
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
                    <Document FilePath = "File2.cs">
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodToClassWithDuplicateNamespacedUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace ClassLibrary1
            {
                using System;

                public class Base
                {
                    public Uri Endpoint{ get; set; }
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
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
                    <Document FilePath = "File2.cs">
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodWithNewReturnTypeToClassWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public Uri En[||]dpoint()
                {
                    return new Uri("http://localhost");
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System;

            public class Base
            {
                public Uri Endpoint()
                {
                    return new Uri("http://localhost");
                }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodWithNewParamTypeToClassWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public bool Test[||]Method(Uri endpoint)
                {
                    var localHost = new Uri("http://localhost");
                    return endpoint.Equals(localhost);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System;

            public class Base
            {
                public bool TestMethod(Uri endpoint)
                {
                    var localHost = new Uri("http://localhost");
                    return endpoint.Equals(localhost);
                }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullMethodWithNewBodyTypeToClassWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public bool Test[||]Method()
                {
                    var endpoint1 = new Uri("http://localhost");
                    var endpoint2 = new Uri("http://localhost");
                    return endpoint1.Equals(endpoint2);
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System;

            public class Base
            {
                public bool TestMethod()
                {
                    var endpoint1 = new Uri("http://localhost");
                    var endpoint2 = new Uri("http://localhost");
                    return endpoint1.Equals(endpoint2);
                }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullEventToClassWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public event EventHandler Test[||]Event
                {
                    add
                    {
                        Console.WriteLine("adding event...");
                    }
                    remove
                    {
                        Console.WriteLine("removing event...");
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System;

            public class Base
            {
                public event EventHandler Test[||]Event
                {
                    add
                    {
                        Console.WriteLine("adding event...");
                    }
                    remove
                    {
                        Console.WriteLine("removing event...");
                    }
                }
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullFieldToClassWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
                public var en[||]dpoint = new Uri("http://localhost");
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System;

            public class Base
            {
                public var endpoint = new Uri("http://localhost");
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46010")]
    public Task TestPullFieldToClassNoConstructorWithAddUsingsViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public class Base
            {
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System.Linq;

            public class Derived : Base
            {
                public var ran[||]ge = Enumerable.Range(0, 5);
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using System.Linq;

            public class Base
            {
                public var range = Enumerable.Range(0, 5);
            }
            </Document>
                    <Document FilePath = "File2.cs">
            using System.Linq;

            public class Derived : Base
            {
            }
            </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestNoRefactoringProvidedWhenPullOverrideMethodUpToClassViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
            namespace PushUpTest
            {
                public class Base
                {
                    public virtual void TestMethod() => System.Console.WriteLine("foo bar bar foo");
                }

                public class TestClass : Base
                {
                    public override void TestMeth[||]od()
                    {
                        System.Console.WriteLine("Hello World");
                    }
                }
            }
            """);

    [Fact]
    public Task TestNoRefactoringProvidedWhenPullOverridePropertyUpToClassViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact]
    public Task TestNoRefactoringProvidedWhenPullOverrideEventUpToClassViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
            using System;

            namespace PushUpTest
            {
                public class Base2
                {
                    protected virtual event EventHandler Event3
                    {
                        add
                        {
                            System.Console.WriteLine("Hello");
                        }
                        remove
                        {
                            System.Console.WriteLine("World");
                        }
                    };
                }

                public class TestClass2 : Base2
                {
                    protected override event EventHandler E[||]vent3
                    {
                        add
                        {
                            System.Console.WriteLine("foo");
                        }
                        remove
                        {
                            System.Console.WriteLine("bar");
                        }
                    };
                }
            }
            """);

    [Fact]
    public Task TestNoRefactoringProvidedWhenPullSameNameFieldUpToClassViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact]
    public Task TestPullMethodToOrdinaryClassViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            namespace PushUpTest
            {
                public class Base
                {
                }

                public class TestClass : Base
                {
                    public void TestMeth[||]od()
                    {
                        System.Console.WriteLine("Hello World");
                    }
                }
            }
            """, """
            namespace PushUpTest
            {
                public class Base
                {
                    public void TestMethod()
                    {
                        System.Console.WriteLine("Hello World");
                    }
                }

                public class TestClass : Base
                {
                }
            }
            """);

    [Fact]
    public Task TestPullOneFieldsToClassViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            namespace PushUpTest
            {
                public class Base
                {
                }

                public class TestClass : Base
                {
                    public int you[||]= 10086;
                }
            }
            """, """
            namespace PushUpTest
            {
                public class Base
                {
                    public int you = 10086;
                }

                public class TestClass : Base
                {
                }
            }
            """);

    [Fact]
    public Task TestPullGenericsUpToClassViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullOneFieldFromMultipleFieldsToClassViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            namespace PushUpTest
            {
                public class Base
                {
                }

                public class TestClass : Base
                {
                    public int you, a[||]nd, someone = 10086;
                }
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullMiddleFieldWithValueToClassViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            namespace PushUpTest
            {
                public class Base
                {
                }

                public class TestClass : Base
                {
                    public int you, a[||]nd = 4000, someone = 10086;
                }
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullOneEventFromMultipleToClassViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullEventToClassViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullEventWithBodyToClassViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
                            System.Console.Writeln("Hello");
                        }
                        remove
                        {
                            System.Console.Writeln("World");
                        }
                    };
                }
            }
            """, """
            using System;

            namespace PushUpTest
            {
                public class Base2
                {
                    private static event EventHandler Event3
                    {
                        add
                        {
                            System.Console.Writeln("Hello");
                        }
                        remove
                        {
                            System.Console.Writeln("World");
                        }
                    };
                }

                public class TestClass2 : Base2
                {
                }
            }
            """);

    [Fact]
    public Task TestPullPropertyToClassViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullIndexerToClassViaQuickAction()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestPullMethodUpAcrossProjectViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
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
              <Project Language="C#" AssemblyName="CSAssembly2" CommonReferences="true">
                    <Document>
            namespace Destination
            {
                public interface IInterface
                {
                }
            }
                    </Document>
              </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
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
              <Project Language="C#" AssemblyName="CSAssembly2" CommonReferences="true">
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
            </Workspace>
            """);

    [Fact]
    public Task TestPullPropertyUpAcrossProjectViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
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
              <Project Language="C#" AssemblyName="CSAssembly2" CommonReferences="true">
                    <Document>
            namespace Destination
            {
                public interface IInterface
                {
                }
            }
                    </Document>
              </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
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
              <Project Language="C#" AssemblyName="CSAssembly2" CommonReferences="true">
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
            </Workspace>
            """);

    [Fact]
    public Task TestPullFieldUpAcrossProjectViaQuickAction()
        => TestWithPullMemberDialogAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                    <ProjectReference>CSAssembly2</ProjectReference>
                    <Document>
            using Destination;
            public class TestClass : BaseClass
            {
                private int i, j, [||]k = 10;
            }
                    </Document>
              </Project>
              <Project Language="C#" AssemblyName="CSAssembly2" CommonReferences="true">
                    <Document>
            namespace Destination
            {
                public class BaseClass
                {
                }
            }
                    </Document>
              </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                    <ProjectReference>CSAssembly2</ProjectReference>
                    <Document>
            using Destination;
            public class TestClass : BaseClass
            {
                private int i, j;
            }
                    </Document>
              </Project>
              <Project Language="C#" AssemblyName="CSAssembly2" CommonReferences="true">
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
            </Workspace>
            """);

    [Fact]
    public Task TestPullMethodUpToVBClassViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
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
              <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                    <Document>
                        Public Class VBClass
                        End Class
                    </Document>
              </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestPullMethodUpToVBInterfaceViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
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
                <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                    <Document>
                        Public Interface VBInterface
                        End Interface
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestPullFieldUpToVBClassViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
                    <ProjectReference>VBAssembly</ProjectReference>
                    <Document>
                        using VBAssembly;
                        public class TestClass : VBClass
                        {
                            public int fo[||]obar = 0;
                        }
                    </Document>
              </Project>
                <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                <Document>
                    Public Class VBClass
                    End Class
                </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestPullPropertyUpToVBClassViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
            <Workspace>
              <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
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
              <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                <Document>
                    Public Class VBClass
                    End Class
                </Document>
              </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestPullPropertyUpToVBInterfaceViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
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
                <Project Language = "Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                    <Document>
                        Public Interface VBInterface
                        End Interface
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestPullEventUpToVBClassViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
                    <ProjectReference>VBAssembly</ProjectReference>
                        <Document>
                        using VBAssembly;
                        public class TestClass : VBClass
                        {
                            public event EventHandler BarEve[||]nt;
                        }
                        </Document>
                </Project>
                <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                    <Document>
                        Public Class VBClass
                        End Class
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestPullEventUpToVBInterfaceViaQuickAction()
        => TestQuickActionNotProvidedAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
                <ProjectReference>VBAssembly</ProjectReference>
                <Document>
                    using VBAssembly;
                    public class TestClass : VBInterface
                    {
                        public event EventHandler BarEve[||]nt;
                    }
                </Document>
                </Project>
                <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                <Document>
                    Public Interface VBInterface
                    End Interface
                </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55746")]
    public Task TestPullMethodWithToClassWithAddUsingsInsideNamespaceViaQuickAction()
        => TestWithPullMemberDialogAsync(
            """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace N
            {
                public class Base
                {
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
            using System;

            namespace N
            {
                public class Derived : Base
                {
                    public Uri En[||]dpoint()
                    {
                        return new Uri("http://localhost");
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace N
            {
                using System;

                public class Base
                {
                    public Uri Endpoint()
                    {
                        return new Uri("http://localhost");
                    }
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """,
            options: Option(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, AddImportPlacement.InsideNamespace, CodeStyle.NotificationOption2.Silent));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55746")]
    public Task TestPullMethodWithToClassWithAddUsingsSystemUsingsLastViaQuickAction()
        => TestWithPullMemberDialogAsync(
            """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            namespace N1
            {
                public class Base
                {
                }
            }
                    </Document>
                    <Document FilePath = "File2.cs">
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
            """,
            """
            <Workspace>
                <Project Language = "C#"  LanguageVersion="preview" CommonReferences="true">
                    <Document FilePath = "File1.cs">using N2;
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
                    <Document FilePath = "File2.cs">
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
            """,
            options: new(GetLanguage())
            {
                { GenerationOptions.PlaceSystemNamespaceFirst, false },
            });

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullMethodToClassWithDirective()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                #region Hello
                public void G[||]oo() { }
                #endregion
            }
            """, """
            public class BaseClass
            {
                public void Goo() { }
            }

            public class Bar : BaseClass
            {

                #region Hello
                #endregion
            }
            """);

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullMethodToClassBeforeDirective()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                public void H[||]ello() { }
                #region Hello
                public void Goo() { }
                #endregion
            }
            """, """
            public class BaseClass
            {
                public void Hello() { }
            }

            public class Bar : BaseClass
            {
                #region Hello
                public void Goo() { }
                #endregion
            }
            """);

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullMethodToClassBeforeDirective2()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                public void Hello() { }

                #region Hello
                public void G[||]oo() { }
                #endregion
            }
            """, """
            public class BaseClass
            {

                public void Goo() { }
            }

            public class Bar : BaseClass
            {
                public void Hello() { }

                #region Hello
                #endregion
            }
            """);

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullFieldToClassBeforeDirective1()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                public int ba[||]r = 10;
                #region Hello
                public int Goo = 10;
                #endregion
            }
            """, """
            public class BaseClass
            {
                public int bar = 10;
            }

            public class Bar : BaseClass
            {
                #region Hello
                public int Goo = 10;
                #endregion
            }
            """);

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullFieldToClassBeforeDirective2()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                public int bar = 10;
                #region Hello
                public int Go[||]o = 10;
                #endregion
            }
            """, """
            public class BaseClass
            {
                public int Goo = 10;
            }

            public class Bar : BaseClass
            {
                public int bar = 10;

                #region Hello
                #endregion
            }
            """);

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullFieldToClassBeforeDirective()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                #region Hello
                public int G[||]oo = 100, Hoo;
                #endregion
            }
            """, """
            public class BaseClass
            {
                public int Goo = 100;
            }

            public class Bar : BaseClass
            {
                #region Hello
                public int Hoo;
                #endregion
            }
            """);

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullEventToClassBeforeDirective()
        => TestWithPullMemberDialogAsync("""
            using System;
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                #region Hello
                public event EventHandler e[||]1;
                #endregion
            }
            """, """
            using System;
            public class BaseClass
            {
                public event EventHandler e1;
            }

            public class Bar : BaseClass
            {

                #region Hello
                #endregion
            }
            """);

    [Fact, WorkItem(55746, "https://github.com/dotnet/roslyn/issues/51531")]
    public Task TestPullPropertyToClassBeforeDirective()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                #region Hello
                public int Go[||]o => 1;
                #endregion
            }
            """, """
            public class BaseClass
            {
                public int Goo => 1;
            }

            public class Bar : BaseClass
            {

                #region Hello
                #endregion
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55402")]
    public Task TestPullPropertyToClassOnKeyword()
        => TestWithPullMemberDialogAsync("""
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

    #endregion Quick Action

    #region Dialog

    internal Task TestWithPullMemberDialogAsync(
        string initialMarkUp,
        string expectedResult,
        IEnumerable<(string name, bool makeAbstract)>? selection = null,
        string? destinationName = null,
        int index = 0,
        TestParameters? parameters = null,
        OptionsCollection? options = null)
    {
        var service = new TestPullMemberUpService(selection, destinationName);

        return TestInRegularAndScriptAsync(
            initialMarkUp, expectedResult,
            (parameters ?? TestParameters.Default).WithFixProviderData(service).WithOptions(options).WithIndex(index));
    }

    [Fact]
    public Task PullPartialMethodUpToInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task PullExtendedPartialMethodUpToInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task PullMultipleNonPublicMethodsToInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
                        System.Console.WriteLine("Hello World");
                    }

                    protected void F[||]oo(int i)
                    {
                        // do awesome things
                    }

                    private static string Bar(string x)
                    {}
                }
            }
            """, """
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
                        System.Console.WriteLine("Hello World");
                    }

                    public void Foo(int i)
                    {
                        // do awesome things
                    }

                    public string Bar(string x)
                    {}
                }
            }
            """);

    [Fact]
    public Task PullMultipleNonPublicEventsToInterface()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task PullMethodToInnerInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task PullDifferentMembersFromClassToPartialInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """, index: 1);

    [Fact]
    public Task TestPullAsyncMethod()
        => TestWithPullMemberDialogAsync("""
            using System.Threading.Tasks;

            internal interface IPullUp { }

            internal class PullUp : IPullUp
            {
                internal async Task PullU[||]pAsync()
                {
                    await Task.Delay(1000);
                }
            }
            """, """
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
            }
            """);

    [Fact]
    public Task PullMethodWithAbstractOptionToClassViaDialog()
        => TestWithPullMemberDialogAsync("""
            namespace PushUpTest
            {
                public class Base
                {
                }

                public class TestClass : Base
                {
                    public void TestMeth[||]od()
                    {
                        System.Console.WriteLine("Hello World");
                    }
                }
            }
            """, """
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
                        System.Console.WriteLine("Hello World");
                    }
                }
            }
            """, [("TestMethod", true)], index: 1);

    [Fact]
    public Task PullAbstractMethodToClassViaDialog()
        => TestWithPullMemberDialogAsync("""
            namespace PushUpTest
            {
                public class Base
                {
                }

                public abstract class TestClass : Base
                {
                    public abstract void TestMeth[||]od();
                }
            }
            """, """
            namespace PushUpTest
            {
                public abstract class Base
                {
                    public abstract void TestMethod();
                }

                public abstract class TestClass : Base
                {
                }
            }
            """, [("TestMethod", true)], index: 0);

    [Fact]
    public Task PullMultipleEventsToClassViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """, index: 1);

    [Fact]
    public Task PullMultipleAbstractEventsToInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task PullAbstractEventToClassViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """, [("Event3", false)]);

    [Fact]
    public Task PullNonPublicEventToInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """, [("Event3", false)]);

    [Fact]
    public Task PullSingleNonPublicEventToInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """, [("Event3", false)]);

    [Fact]
    public Task TestPullNonPublicEventWithAddAndRemoveMethodToInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
                            System.Console.Writeline("This is add");
                        }
                        remove
                        {
                            System.Console.Writeline("This is remove");
                        }
                    }
                }
            }
            """, """
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
                            System.Console.Writeline("This is add");
                        }
                        remove
                        {
                            System.Console.Writeline("This is remove");
                        }
                    }
                }
            }
            """, [("Event1", false)]);

    [Fact]
    public Task PullFieldsToClassViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """, index: 1);

    [Fact]
    public Task PullNonPublicPropertyWithArrowToInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task PullNonPublicPropertyToInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task PullNonPublicPropertyWithSingleAccessorToInterfaceViaDialog()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34268")]
    public Task TestPullPropertyToAbstractClassViaDialogWithMakeAbstractOption()
        => TestWithPullMemberDialogAsync("""
            abstract class B
            {
            }

            class D : B
            {
                int [||]X => 7;
            }
            """, """
            abstract class B
            {
                private abstract int X { get; }
            }

            class D : B
            {
                override int X => 7;
            }
            """, selection: [("X", true)], index: 1);

    [Fact]
    public Task PullEventUpToAbstractClassViaDialogWithMakeAbstractOption()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """, selection: [("Event3", true)], index: 1);

    [Fact]
    public Task TestPullEventWithAddAndRemoveMethodToClassViaDialogWithMakeAbstractOption()
        => TestWithPullMemberDialogAsync("""
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
                            System.Console.Writeline("This is add");
                        }
                        remove
                        {
                            System.Console.Writeline("This is remove");
                        }
                    }
                }
            }
            """, """
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
                            System.Console.Writeline("This is add");
                        }
                        remove
                        {
                            System.Console.Writeline("This is remove");
                        }
                    }
                }
            }
            """, [("Event1", true)], index: 1);

    #endregion Dialog

    #region Selections and caret position
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestArgsIsPartOfHeader()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestRefactoringCaretBeforeAttributes()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestMissingRefactoringCaretBetweenAttributes()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestRefactoringSelectionWithAttributes1()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestRefactoringSelectionWithAttributes2()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestRefactoringSelectionWithAttributes3()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestMissingRefactoringInAttributeList()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestMissingRefactoringSelectionAttributeList()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestMissingRefactoringCaretInAttributeList()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestMissingRefactoringCaretBetweenAttributeLists()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestMissingRefactoringSelectionAttributeList2()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestMissingRefactoringSelectAttributeList()
        => TestQuickActionNotProvidedAsync("""
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestRefactoringCaretLocAfterAttributes1()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestRefactoringCaretLocAfterAttributes2()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestRefactoringCaretLoc1()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestRefactoringSelection()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestRefactoringSelectionComments()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestRefactoringSelectionComments2()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
    public Task TestRefactoringSelectionComments3()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionFieldKeyword1_NoAction()
        => TestQuickActionNotProvidedAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                pub[|l|]ic int Goo = 10;
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionFieldKeyword2()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                pub[||]lic int Goo = 10;
            }
            """, """
            public class BaseClass
            {
                public int Goo = 10;
            }

            public class Bar : BaseClass
            {
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionFieldAfterSemicolon()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                public int Goo = 10;[||]
            }
            """, """
            public class BaseClass
            {
                public int Goo = 10;
            }

            public class Bar : BaseClass
            {
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionFieldEntireDeclaration()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                [|public int Goo = 10;|]
            }
            """, """
            public class BaseClass
            {
                public int Goo = 10;
            }

            public class Bar : BaseClass
            {
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionMultipleFieldsInDeclaration1()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                [|public int Goo = 10, Foo = 9;|]
            }
            """, """
            public class BaseClass
            {
                public int Goo = 10;
                public int Foo = 9;
            }

            public class Bar : BaseClass
            {
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionMultipleFieldsInDeclaration2()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                public int Go[||]o = 10, Foo = 9;
            }
            """, """
            public class BaseClass
            {
                public int Goo = 10;
            }

            public class Bar : BaseClass
            {
                public int Foo = 9;
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionMultipleFieldsInDeclaration3()
        => TestWithPullMemberDialogAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                public int Goo = 10, [||]Foo = 9;
            }
            """, """
            public class BaseClass
            {
                public int Foo = 9;
            }

            public class Bar : BaseClass
            {
                public int Goo = 10;
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionMultipleMembers1()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    // Some of these have weird whitespace spacing that might suggest a bug
    [Fact]
    public Task TestRefactoringSelectionMultipleMembers2()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionMultipleMembers3()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionMultipleMembers4()
        => TestWithPullMemberDialogAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionIncompleteField_NoAction1()
        => TestQuickActionNotProvidedAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                publ[||] int Goo = 10;
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionIncompleteField_NoAction2()
        => TestQuickActionNotProvidedAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                [|publicc int Goo = 10;|]
            }
            """);

    [Fact]
    public Task TestRefactoringSelectionIncompleteMethod_NoAction()
        => TestQuickActionNotProvidedAsync("""
            public class BaseClass
            {
            }

            public class Bar : BaseClass
            {
                publ[||] int DoSomething() {
                    return 5;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78113")]
    public Task PullPartialEventUpToClass()
        => TestWithPullMemberDialogAsync("""
            using System;
            namespace PushUpTest
            {
                public class BaseClass
                {
                }

                public partial class TestClass : BaseClass
                {
                    public partial event EventHandler E[||]vent1;
                }

                public partial class TestClass
                {
                    public partial event EventHandler Event1 { add { } remove { } }
                }
            }
            """, """
            using System;
            namespace PushUpTest
            {
                public class BaseClass
                {
                    public partial event EventHandler Event1;
                }

                public partial class TestClass : BaseClass
                {
                }

                public partial class TestClass
                {
                    public partial event EventHandler Event1 { add { } remove { } }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78113")]
    public Task PullPartialPropertyUpToClass()
        => TestWithPullMemberDialogAsync("""
            using System;
            namespace PushUpTest
            {
                public class BaseClass
                {
                }

                public partial class TestClass : BaseClass
                {
                    public partial int Pr[||]op { get; }
                }

                public partial class TestClass
                {
                    public partial int Prop => 42;
                }
            }
            """, """
            using System;
            namespace PushUpTest
            {
                public class BaseClass
                {
                    public partial int Prop { get; }
                }

                public partial class TestClass : BaseClass
                {
                }

                public partial class TestClass
                {
                    public partial int Prop => 42;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78113")]
    public Task PullPartialMethodUpToClass()
        => TestWithPullMemberDialogAsync("""
            using System;
            namespace PushUpTest
            {
                public class BaseClass
                {
                }

                public partial class TestClass : BaseClass
                {
                    public partial void M[||]ethod();
                }

                public partial class TestClass
                {
                    public partial void Method() { }
                }
            }
            """, """
            using System;
            namespace PushUpTest
            {
                public class BaseClass
                {
                    public partial void Method();
                }

                public partial class TestClass : BaseClass
                {
                }

                public partial class TestClass
                {
                    public partial void Method() { }
                }
            }
            """);
    #endregion
}
