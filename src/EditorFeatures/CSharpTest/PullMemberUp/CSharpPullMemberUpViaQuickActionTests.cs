// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PullMemberUp
{
    public class CSharpPullMemberUpViaQuickActionTests : CSharpPullMemberUpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpPullMemberUpCodeRefactoringProvider(parameters.fixProviderData as IPullMemberUpOptionsService);

        #region interface
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullMethodUpToInterface()
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
            await TestWithPullMemberDialogAsync(testText, expected, index: 1);
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

        private string Bar(string x)
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
        public async Task PullSingleEventToInterface()
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
                System.Console.Writeln(""This is add"");
            }
            remove
            {
                System.Console.Writeln(""This is remove"");
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
                System.Console.Writeln(""This is add"");
            }
            remove
            {
                System.Console.Writeln(""This is remove"");
            }
        }
    }
}";
          await TestInRegularAndScriptAsync(testText, expected);
          await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("Event1", false) }, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullOneEventFromMultipleEventsToInterface()
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
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("Event2", false) }, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullStaticAndNonPublicEventUpToInterfaceViaDialog()
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
        private static event EventHandler Event1, Eve[||]nt2, Event3;
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
        private static event EventHandler Event1, Event3;
        public event EventHandler Event2;
    }
}";
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("Event2", false) });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullNonPublicEventToInterfaceViaDialog()
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
        private static event EventHandler E[||]vent1;
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
        public event EventHandler Event1;
    }
}";
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullNonPublicEventsWithAccessorsToInterfaceViaDialog()
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
        private static event EventHandler Eve[||]nt2
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
        public async Task PullPropertyWithPrivateSetterToInterface()
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
            await TestWithPullMemberDialogAsync(testText, expected, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullPropertyWithPrivateGetterToInterface()
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
        public int TestPr[||]operty { private get; set; }
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
        public int TestProperty { private get; set; }
    }
}";
            await TestInRegularAndScriptAsync(testText, expected);
            await TestWithPullMemberDialogAsync(testText, expected, index: 1);
        }
        
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullIndexerWithOnlySetterToInterface()
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
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("this[]", false)},  index: 1);
        }
    
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullIndexerWithOnlyGetterToInterface()
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
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("this[]", false)},  index: 1);
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

        private void BarBar()
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
        public int th[||]is[int i]
        {
           get => j = value;
        }

        public void BarBar()
        {}

        public int Foo
        {
            get; set;
        }

        public event EventHandler event1;
        public event EventHandler event2;
    }

    partial interface IInterface
    {
    }
}";
            await TestWithPullMemberDialogAsync(testText, expected, index : 1);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullDifferentMembersFromInterfaceToPartialInterfaceViaDialog()
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
        public int th[||]is[int i]
        {
           get => j = value;
        }
        
        protected static event EventHandler event1, event2;
    }

    public partial class TestClass : IInterface
    {
        private void BarBar()
        {}

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

    public partial class TestClass : IInterface
    {
        public int th[||]is[int i]
        {
           get => j = value;
        }

        public event EventHandler event1;
        public event EventHandler event2;
    }

    public partial class TestClass : IInterface
    {
        public void BarBar()
        {}

        public int Foo
        {
            get; set;
        }
    }


    partial interface IInterface
    {
    }
}";
            await TestWithPullMemberDialogAsync(testText, expected, index : 1);
        }
        #endregion interface

        #region class
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullMethodToClass()
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
            await TestWithPullMemberDialogAsync(testText, expected, index: 1);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullMethodWithAbstractOptionToClass()
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
        public void TestMeth[||]od()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}";
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("TestMethod", true) }, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullAbstractMethodToClass()
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
        public abstract void TestMethod();
    }
}";
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("TestMethod", true) }, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullOneFieldsToClass()
        {
            var testText = @"
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public int yo[||]u = 10086;
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
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullOneFieldFromMultipleFieldsToClass()
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
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("and", false) }, index: 1);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullMiddleFieldWithValueToClass()
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
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("and", false) }, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullOneEventFromMultipleToClass()
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
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("Event3", false) }, index: 1);
        }
            
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullMultipleEventsToClass()
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
        public async Task PullAbstractEventToClass()
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
        private abstract static event EventHandler Event1, Eve[||]nt3, Event4;
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
        private abstract static event EventHandler Event1, Event4;
    }
}";

            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("Event3", false) });
        }
        
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullEventToClass()
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
            await TestWithPullMemberDialogAsync(testText, expected, index : 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullPropertyToClass()
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
            await TestWithPullMemberDialogAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullIndexerToClass()
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
            await TestWithPullMemberDialogAsync(testText, expected, new (string, bool)[] { ("this[]", false) }, index: 1);
        }
        #endregion class
    }
}
