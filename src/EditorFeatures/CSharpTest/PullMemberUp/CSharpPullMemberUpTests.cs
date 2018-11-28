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

        #region destination interface
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestNoRefactoringProvidedWhenPullFieldToInterface()
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

            await TestMissingAsync(testText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestNoRefactoringProvidedWhenDeclarationAlreadyExists()
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

            var propertyTest2 = @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        int TestProperty { get; set; }
    }

    public class TestClass : IInterface
    {
        public int TestPr[||]operty { get; set; }
    }
}";

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
            await TestMissingAsync(methodTest);
            await TestMissingAsync(propertyTest1);
            await TestMissingAsync(propertyTest2);
            await TestMissingAsync(eventTest);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullMethodUpToInterface()
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
        public async Task TestPullAbstractMethodToInterface()
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
        public async Task TestPullSingleEventToInterface()
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
        public async Task TestPullOneEventFromMultipleEventsToInterface()
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
        public async Task TestPullPublicEventWithAccessorsToInterfaceViaDialog()
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
        public async Task TestPullPropertyWithPrivateSetterToInterface()
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
        public async Task TestPullPropertyWithPrivateGetterToInterface()
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
        public async Task TestPullIndexerWithOnlySetterToInterface()
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
        public async Task TestPullIndexerWithOnlyGetterToInterface()
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
        public async Task TestNoRefactoringProvidedWhenPullOverrideMethodUp()
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
            await TestMissingAsync(methodTest);
            await TestMissingAsync(propertyTest);
            await TestMissingAsync(eventTest);
            await TestMissingAsync(fieldTest);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullMethodToOrdinaryClass()
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
        public async Task TestPullOneFieldsToClass()
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
        public async Task TestPullOneFieldFromMultipleFieldsToClass()
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
        public async Task TestPullMiddleFieldWithValueToClass()
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
        public async Task TestPullOneEventFromMultipleToClass()
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
        public async Task TestPullEventToClass()
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
        public async Task TestPullEventWithBodyToClass()
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
        public async Task TestPullPropertyToClass()
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
        public async Task TestPullIndexerToClass()
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
            // Moving member from C# to Visual Basic is not supported currently since the FindMostRelevantDeclarationAsync method in 
            // AbstractCodeGenerationService will return null.
            var input = new XElement("Workspace",
                    new XElement("Project",
                        new XAttribute("Language", "C#"),
                        new XAttribute("AssemblyName", "CSAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("ProjectReferences", "VBAssembly"),
                        new XElement("Document", @"
using VBAssembly;
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

            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullMethodUpToVBInterface()
        {
            var input = new XElement("Workspace",
                    new XElement("Project",
                        new XAttribute("Language", "C#"),
                        new XAttribute("AssemblyName", "CSAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("ProjectReferences", "VBAssembly"),
                        new XElement("Document", @"
using VBAssembly;
public class TestClass : VBInterface
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
Public Interface VBInterface
End Interface
"))).ToString();

            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullFieldUpToVBClass()
        {
            var input = new XElement("Workspace",
                    new XElement("Project",
                        new XAttribute("Language", "C#"),
                        new XAttribute("AssemblyName", "CSAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("ProjectReferences", "VBAssembly"),
                        new XElement("Document", @"
using VBAssembly;
public class TestClass : VBClass
{
    public int fo[||]obar = 0;
}")),
                    new XElement("Project",
                        new XAttribute("Language", "Visual Basic"),
                        new XAttribute("AssemblyName", "VBAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("Document", @"
Public Class VBClass
End Class
"))).ToString();

            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullPropertyUpToVBClass()
        {
            var input = new XElement("Workspace",
                    new XElement("Project",
                        new XAttribute("Language", "C#"),
                        new XAttribute("AssemblyName", "CSAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("ProjectReferences", "VBAssembly"),
                        new XElement("Document", @"
using VBAssembly;
public class TestClass : VBClass
{
    public int foo[||]bar
    {
        get;
        set;
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

            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullPropertyUpToVBInterface()
        {
            var input = new XElement("Workspace",
                    new XElement("Project",
                        new XAttribute("Language", "C#"),
                        new XAttribute("AssemblyName", "CSAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("ProjectReferences", "VBAssembly"),
                        new XElement("Document", @"
using VBAssembly;
public class TestClass : VBInterface
{
    public int foo[||]bar
    {
        get;
        set;
    }
}")),
                    new XElement("Project",
                        new XAttribute("Language", "Visual Basic"),
                        new XAttribute("AssemblyName", "VBAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("Document", @"
Public Interface VBInterface
End Interface
"))).ToString();

            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullEventUpToVBClass()
        {
            var input = new XElement("Workspace",
                    new XElement("Project",
                        new XAttribute("Language", "C#"),
                        new XAttribute("AssemblyName", "CSAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("ProjectReferences", "VBAssembly"),
                        new XElement("Document", @"
using VBAssembly;
public class TestClass : VBClass
{
    public event EventHandler BarEve[||]nt;
}")),
                    new XElement("Project",
                        new XAttribute("Language", "Visual Basic"),
                        new XAttribute("AssemblyName", "VBAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("Document", @"
Public Class VBClass
End Class
"))).ToString();

            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullEventUpToVBInterface()
        {
            var input = new XElement("Workspace",
                    new XElement("Project",
                        new XAttribute("Language", "C#"),
                        new XAttribute("AssemblyName", "CSAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("ProjectReferences", "VBAssembly"),
                        new XElement("Document", @"
using VBAssembly;
public class TestClass : VBInterface
{
    public event EventHandler BarEve[||]nt;
}")),
                    new XElement("Project",
                        new XAttribute("Language", "Visual Basic"),
                        new XAttribute("AssemblyName", "VBAssembly"),
                        new XAttribute("CommonReferences", "true"),
                        new XElement("Document", @"
Public Interface VBInterface
End Interface
"))).ToString();

            await TestMissingAsync(input);
        }
        
        #endregion cross language
    }
}
