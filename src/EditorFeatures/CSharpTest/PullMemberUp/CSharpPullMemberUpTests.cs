// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PullMemberUp
{
    public class CSharpPullMemberUpTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
        => new CSharpPullMemberUpCodeRefactoringProvider();

        private async Task TestNoRefactoringProvidedAsync(string initialMarkUp)
        {
            var workspace = CreateWorkspaceFromOptions(initialMarkUp, default);
            var (codeActionArray, _) = await GetCodeActionsWorkerAsync(workspace, default);
            Assert.True(codeActionArray.IsEmpty);
        }

        #region destination interface
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullPublicEventWithAccessorsToInterfaceViaDialog()
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
        }

        #endregion destination interface

        #region destination class
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task PullEventWithBodyToClass()
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
        }
        #endregion destination class

        #region cross language


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullMethodUpToVBClass()
        {
            var input = new XElement("Workspace",
                    new XElement("Project",
                        new XAttribute("Language", "C#"),
                        new XAttribute("AssemblyName", "CSAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("ProjectReferences", "VBAssembly"),
                        new XElement("Document", @"
public class TestClass : VBClass
{
    public int Bar[||]bar()
    {
        return 12345;
    }
}")),
                    new XElement("Project",
                        new XAttribute("Language", "Visual Basic"),
                        new XAttribute("AssemblyName", "VBAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("Document", @"
Public Class VBClass
End Class
"))).ToString();

            await TestNoRefactoringProvidedAsync(input);
        }

        //[Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        //public async Task TestPullFieldUpToVBClass()
        //{

        //}

        //[Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        //public async Task TestPullPropertyUpToVBClass()
        //{

        //}

        //[Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        //public async Task TestPullEventUpToVBClass()
        //{

        //}
        
        #endregion cross language
    }
}
