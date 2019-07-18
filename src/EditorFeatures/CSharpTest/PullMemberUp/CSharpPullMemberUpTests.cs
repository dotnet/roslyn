// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Test.Utilities.PullMemberUp;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PullMemberUp
{
    public class CSharpPullMemberUpTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpPullMemberUpCodeRefactoringProvider((IPullMemberUpOptionsService)parameters.fixProviderData);

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions) => FlattenActions(actions);

        #region Quick Action

        private async Task TestQuickActionNotProvidedAsync(
            string initialMarkup,
            TestParameters parameters = default)
        {
            using var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters);
            var (actions, _) = await GetCodeActionsAsync(workspace, parameters);
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        #endregion Quick Action

        #region Dialog

        internal Task TestWithPullMemberDialogAsync(
            string initialMarkUp,
            string expectedResult,
            IEnumerable<(string name, bool makeAbstract)> selection = null,
            string destinationName = null,
            int index = 0,
            CodeActionPriority? priority = null,
            TestParameters parameters = default)
        {
            var service = new TestPullMemberUpService(selection, destinationName);

            return TestInRegularAndScript1Async(
                initialMarkUp, expectedResult,
                index, priority,
                parameters.WithFixProviderData(service));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
    public abstract class Base2
    {
        private static abstract event EventHandler Event3;
    }

    public abstract class Testclass2 : Base2
    {
        private static abstract event EventHandler Event1, Event4;
    }
}";

            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("Event3", false) });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [WorkItem(34268, "https://github.com/dotnet/roslyn/issues/34268")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        [WorkItem(35180, "https://github.com/dotnet/roslyn/issues/35180")]
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

            await TestInRegularAndScriptAsync(testText, expected);
        }

        #endregion
    }
}
