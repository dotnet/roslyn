// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
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
        public async Task TestNoRefactoringProvidedWhenPullFieldInInterface()
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
        public async Task TestNoRefactoringProvidedWhenMethodDeclarationAlreadyExistsInInterface()
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
            await TestMissingAsync(methodTest);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestNoRefactoringProvidedWhenPropertyDeclarationAlreadyExistsInInterface()
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
            await TestMissingAsync(propertyTest1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestNoRefactoringProvidedWhenEventDeclarationAlreadyExistsToInterface()
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
            await TestMissingAsync(eventTest);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestNoRefactoringProvidedInNestedTypes()
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

            await TestMissingAsync(input);
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
        public async Task TestPullGenericsUpToInterface()
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
        public async Task TestPullPublicEventWithAccessorsToInterface()
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
        public async Task TestNoRefactoringProvidedWhenPullOverrideMethodUpToClass()
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
            await TestMissingAsync(methodTest);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestNoRefactoringProvidedWhenPullOverridePropertyUpToClass()
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

            await TestMissingAsync(propertyTest);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestNoRefactoringProvidedWhenPullOverrideEventUpToClass()
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
            await TestMissingAsync(eventTest);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestNoRefactoringProvidedWhenPullSameNameFieldUpToClass()
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
        public async Task TestPullGenericsUpToClass()
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
            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullMethodUpToVBInterface()
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
            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullFieldUpToVBClass()
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

            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullPropertyUpToVBClass()
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
            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullPropertyUpToVBInterface()
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
            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullEventUpToVBClass()
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
            await TestMissingAsync(input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)]
        public async Task TestPullEventUpToVBInterface()
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
            await TestMissingAsync(input);
        }
        
        #endregion cross language
    }
}
