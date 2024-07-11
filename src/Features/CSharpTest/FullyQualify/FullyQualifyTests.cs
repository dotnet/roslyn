// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.FullyQualify;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.FullyQualify;

[Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
public class FullyQualifyTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public FullyQualifyTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpFullyQualifyCodeFixProvider());

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    [Theory, CombinatorialData]
    public async Task TestTypeFromMultipleNamespaces1(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    [|IDictionary|] Method()
    {
        Goo();
    }
}",
@"class Class
{
    System.Collections.IDictionary Method()
    {
        Goo();
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestTypeFromMultipleNamespaces2(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    [|IDictionary|] Method()
    {
        Goo();
    }
}",
@"class Class
{
    System.Collections.Generic.IDictionary Method()
    {
        Goo();
    }
}",
index: 1, testHost: testHost);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1889385")]
    public async Task TestPreservesIncorrectIndentation1(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
      [|IDictionary|] Method()
    {
        Goo();
    }
}",
@"class Class
{
      System.Collections.IDictionary Method()
    {
        Goo();
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1889385")]
    public async Task TestPreservesIncorrectIndentation2(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
\t[|IDictionary|] Method()
    {
        Goo();
    }
}".Replace(@"\t", "\t"),
@"class Class
{
\tSystem.Collections.IDictionary Method()
    {
        Goo();
    }
}".Replace(@"\t", "\t"), testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestGenericWithNoArgs(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    [|List|] Method()
    {
        Goo();
    }
}",
@"class Class
{
    System.Collections.Generic.List Method()
    {
        Goo();
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestGenericWithCorrectArgs(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    [|List<int>|] Method()
    {
        Goo();
    }
}",
@"class Class
{
    System.Collections.Generic.List<int> Method()
    {
        Goo();
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestSmartTagDisplayText(TestHost testHost)
    {
        await TestSmartTagTextAsync(
@"class Class
{
    [|List<int>|] Method()
    {
        Goo();
    }
}",
"System.Collections.Generic.List", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData]
    public async Task TestGenericWithWrongArgs(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"class Class
{
    [|List<int, string>|] Method()
    {
        Goo();
    }
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData]
    public async Task TestNotOnVar1(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"namespace N
{
    class var { }
}

class C
{
    void M()
    {
        [|var|]
    }
}
", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData]
    public async Task TestNotOnVar2(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"namespace N
{
    class Bar { }
}

class C
{
    void M()
    {
        [|var|]
    }
}
", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData]
    public async Task TestGenericInLocalDeclaration(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    void Goo()
    {
        [|List<int>|] a = new List<int>();
    }
}",
@"class Class
{
    void Goo()
    {
        System.Collections.Generic.List<int> a = new List<int>();
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestGenericItemType(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Class
{
    List<[|Int32|]> l;
}",
@"using System.Collections.Generic;

class Class
{
    List<System.Int32> l;
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestGenerateWithExistingUsings(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"using System;

class Class
{
    [|List<int>|] Method()
    {
        Goo();
    }
}",
@"using System;

class Class
{
    System.Collections.Generic.List<int> Method()
    {
        Goo();
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestGenerateInNamespace(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"namespace N
{
    class Class
    {
        [|List<int>|] Method()
        {
            Goo();
        }
    }
}",
@"namespace N
{
    class Class
    {
        System.Collections.Generic.List<int> Method()
        {
            Goo();
        }
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestGenerateInNamespaceWithUsings(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"namespace N
{
    using System;

    class Class
    {
        [|List<int>|] Method()
        {
            Goo();
        }
    }
}",
@"namespace N
{
    using System;

    class Class
    {
        System.Collections.Generic.List<int> Method()
        {
            Goo();
        }
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestExistingUsing(TestHost testHost)
    {
        await TestActionCountAsync(
@"using System.Collections.Generic;

class Class
{
    [|IDictionary|] Method()
    {
        Goo();
    }
}",
count: 1, new TestParameters(testHost: testHost));

        await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Class
{
    [|IDictionary|] Method()
    {
        Goo();
    }
}",
@"using System.Collections.Generic;

class Class
{
    System.Collections.IDictionary Method()
    {
        Goo();
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestMissingIfUniquelyBound(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"using System;

class Class
{
    [|String|] Method()
    {
        Goo();
    }
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData]
    public async Task TestMissingIfUniquelyBoundGeneric(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Class
{
    [|List<int>|] Method()
    {
        Goo();
    }
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData]
    public async Task TestOnEnum(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    void Goo()
    {
        var a = [|Colors|].Red;
    }
}

namespace A
{
    enum Colors
    {
        Red,
        Green,
        Blue
    }
}",
@"class Class
{
    void Goo()
    {
        var a = A.Colors.Red;
    }
}

namespace A
{
    enum Colors
    {
        Red,
        Green,
        Blue
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestOnClassInheritance(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class : [|Class2|]
{
}

namespace A
{
    class Class2
    {
    }
}",
@"class Class : A.Class2
{
}

namespace A
{
    class Class2
    {
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestOnImplementedInterface(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class : [|IGoo|]
{
}

namespace A
{
    interface IGoo
    {
    }
}",
@"class Class : A.IGoo
{
}

namespace A
{
    interface IGoo
    {
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestAllInBaseList(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class : [|IGoo|], Class2
{
}

namespace A
{
    class Class2
    {
    }
}

namespace B
{
    interface IGoo
    {
    }
}",
@"class Class : B.IGoo, Class2
{
}

namespace A
{
    class Class2
    {
    }
}

namespace B
{
    interface IGoo
    {
    }
}", testHost: testHost);

        await TestInRegularAndScriptAsync(
@"class Class : B.IGoo, [|Class2|]
{
}

namespace A
{
    class Class2
    {
    }
}

namespace B
{
    interface IGoo
    {
    }
}",
@"class Class : B.IGoo, A.Class2
{
}

namespace A
{
    class Class2
    {
    }
}

namespace B
{
    interface IGoo
    {
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestAttributeUnexpanded(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"[[|Obsolete|]]
class Class
{
}",
@"[System.Obsolete]
class Class
{
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestAttributeExpanded(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"[[|ObsoleteAttribute|]]
class Class
{
}",
@"[System.ObsoleteAttribute]
class Class
{
}", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527360")]
    public async Task TestExtensionMethods(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class Goo
{
    void Bar()
    {
        var values = new List<int>() { 1, 2, 3 };
        values.[|Where|](i => i > 1);
    }
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538018")]
    public async Task TestAfterNew(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    void Goo()
    {
        List<int> l;
        l = new [|List<int>|]();
    }
}",
@"class Class
{
    void Goo()
    {
        List<int> l;
        l = new System.Collections.Generic.List<int>();
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestArgumentsInMethodCall(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    void Test()
    {
        Console.WriteLine([|DateTime|].Today);
    }
}",
@"class Class
{
    void Test()
    {
        Console.WriteLine(System.DateTime.Today);
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestCallSiteArgs(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    void Test([|DateTime|] dt)
    {
    }
}",
@"class Class
{
    void Test(System.DateTime dt)
    {
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestUsePartialClass(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"namespace A
{
    public class Class
    {
        [|PClass|] c;
    }
}

namespace B
{
    public partial class PClass
    {
    }
}",
@"namespace A
{
    public class Class
    {
        B.PClass c;
    }
}

namespace B
{
    public partial class PClass
    {
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestGenericClassInNestedNamespace(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"namespace A
{
    namespace B
    {
        class GenericClass<T>
        {
        }
    }
}

namespace C
{
    class Class
    {
        [|GenericClass<int>|] c;
    }
}",
@"namespace A
{
    namespace B
    {
        class GenericClass<T>
        {
        }
    }
}

namespace C
{
    class Class
    {
        A.B.GenericClass<int> c;
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TestBeforeStaticMethod(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    void Test()
    {
        [|Math|].Sqrt();
    }",
@"class Class
{
    void Test()
    {
        System.Math.Sqrt();
    }", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538136")]
    public async Task TestBeforeNamespace(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"namespace A
{
    class Class
    {
        [|C|].Test t;
    }
}

namespace B
{
    namespace C
    {
        class Test
        {
        }
    }
}",
@"namespace A
{
    class Class
    {
        B.C.Test t;
    }
}

namespace B
{
    namespace C
    {
        class Test
        {
        }
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527395")]
    public async Task TestSimpleNameWithLeadingTrivia(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class { void Test() { /*goo*/[|Int32|] i; } }",
@"class Class { void Test() { /*goo*/System.Int32 i; } }", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527395")]
    public async Task TestGenericNameWithLeadingTrivia(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class { void Test() { /*goo*/[|List<int>|] l; } }",
@"class Class { void Test() { /*goo*/System.Collections.Generic.List<int> l; } }", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538740")]
    public async Task TestFullyQualifyTypeName(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"public class Program
{
    public class Inner
    {
    }
}

class Test
{
    [|Inner|] i;
}",
@"public class Program
{
    public class Inner
    {
    }
}

class Test
{
    Program.Inner i;
}", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/26887")]
    public async Task TestFullyQualifyUnboundIdentifier3(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"public class Program
{
    public class Inner
    {
    }
}

class Test
{
    public [|Inner|] Name
}",
@"public class Program
{
    public class Inner
    {
    }
}

class Test
{
    public Program.Inner Name
}", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538740")]
    public async Task TestFullyQualifyTypeName_NotForGenericType(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"class Program<T>
{
    public class Inner
    {
    }
}

class Test
{
    [|Inner|] i;
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538764")]
    public async Task TestFullyQualifyThroughAlias(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"using Alias = System;

class C
{
    [|Int32|] i;
}",
@"using Alias = System;

class C
{
    Alias.Int32 i;
}", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538763")]
    public async Task TestFullyQualifyPrioritizeTypesOverNamespaces1(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"namespace Outer
{
    namespace C
    {
        class C
        {
        }
    }
}

class Test
{
    [|C|] c;
}",
@"namespace Outer
{
    namespace C
    {
        class C
        {
        }
    }
}

class Test
{
    Outer.C.C c;
}", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538763")]
    public async Task TestFullyQualifyPrioritizeTypesOverNamespaces2(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"namespace Outer
{
    namespace C
    {
        class C
        {
        }
    }
}

class Test
{
    [|C|] c;
}",
@"namespace Outer
{
    namespace C
    {
        class C
        {
        }
    }
}

class Test
{
    Outer.C c;
}",
index: 1, testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539853")]
    public async Task BugFix5950(TestHost testHost)
    {
        await TestAsync(
@"using System.Console; WriteLine([|Expression|].Constant(123));",
@"using System.Console; WriteLine(System.Linq.Expressions.Expression.Constant(123));",
parseOptions: GetScriptOptions(), testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540318")]
    public async Task TestAfterAlias(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        System::[|Console|] :: WriteLine(""TEST"");
    }
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540942")]
    public async Task TestMissingOnIncompleteStatement(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"using System;
using System.IO;

class C
{
    static void Main(string[] args)
    {
        [|Path|] }
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542643")]
    public async Task TestAssemblyAttribute(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"[assembly: [|InternalsVisibleTo|](""Project"")]",
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Project"")]", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543388")]
    public async Task TestMissingOnAliasName(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"using [|GIBBERISH|] = Goo.GIBBERISH;

class Program
{
    static void Main(string[] args)
    {
        GIBBERISH x;
    }
}

namespace Goo
{
    public class GIBBERISH
    {
    }
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData]
    public async Task TestMissingOnAttributeOverloadResolutionError(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"using System.Runtime.InteropServices;

class M
{
    [[|DllImport|]()]
    static extern int? My();
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544950")]
    public async Task TestNotOnAbstractConstructor(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"using System.IO;

class Program
{
    static void Main(string[] args)
    {
        var s = new [|Stream|]();
    }
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545774")]
    public async Task TestAttributeCount(TestHost testHost)
    {
        await TestActionCountAsync(@"[ assembly : [|Guid|] ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ", 2, new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545774")]
    public async Task TestAttribute(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"[ assembly : [|Guid|] ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ",
@"[ assembly : System.Runtime.InteropServices.Guid ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546027")]
    public async Task TestGeneratePropertyFromAttribute(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"using System;

[AttributeUsage(AttributeTargets.Class)]
class MyAttrAttribute : Attribute
{
}

[MyAttr(123, [|Version|] = 1)]
class D
{
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775448")]
    public async Task ShouldTriggerOnCS0308(TestHost testHost)
    {
        // CS0308: The non-generic type 'A' cannot be used with type arguments
        await TestInRegularAndScriptAsync(
@"using System.Collections;

class Test
{
    static void Main(string[] args)
    {
        [|IEnumerable<int>|] f;
    }
}",
@"using System.Collections;

class Test
{
    static void Main(string[] args)
    {
        System.Collections.Generic.IEnumerable<int> f;
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/947579")]
    public async Task AmbiguousTypeFix(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"using n1;
using n2;

class B
{
    void M1()
    {
        [|var a = new A();|]
    }
}

namespace n1
{
    class A
    {
    }
}

namespace n2
{
    class A
    {
    }
}",
@"using n1;
using n2;

class B
{
    void M1()
    {
        var a = new n1.A();
    }
}

namespace n1
{
    class A
    {
    }
}

namespace n2
{
    class A
    {
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995857")]
    public async Task NonPublicNamespaces(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"namespace MS.Internal.Xaml
{
    private class A
    {
    }
}

namespace System.Xaml
{
    public class A
    {
    }
}

public class Program
{
    static void M()
    {
        [|Xaml|]
    }
}",
@"namespace MS.Internal.Xaml
{
    private class A
    {
    }
}

namespace System.Xaml
{
    public class A
    {
    }
}

public class Program
{
    static void M()
    {
        System.Xaml
    }
}", testHost: testHost);

        await TestInRegularAndScriptAsync(
@"namespace MS.Internal.Xaml
{
    public class A
    {
    }
}

namespace System.Xaml
{
    public class A
    {
    }
}

public class Program
{
    static void M()
    {
        [|Xaml|]
    }
}",
@"namespace MS.Internal.Xaml
{
    public class A
    {
    }
}

namespace System.Xaml
{
    public class A
    {
    }
}

public class Program
{
    static void M()
    {
        MS.Internal.Xaml
    }
}", index: 1, testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/11071")]
    public async Task AmbiguousFixOrdering(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"using n1;
using n2;

[[|Inner|].C]
class B
{
}

namespace n1
{
    namespace Inner
    {
    }
}

namespace n2
{
    namespace Inner
    {
        class CAttribute
        {
        }
    }
}",
@"using n1;
using n2;

[n2.Inner.C]
class B
{
}

namespace n1
{
    namespace Inner
    {
    }
}

namespace n2
{
    namespace Inner
    {
        class CAttribute
        {
        }
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TupleTest(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    ([|IDictionary|], string) Method()
    {
        Goo();
    }
}",
@"class Class
{
    (System.Collections.IDictionary, string) Method()
    {
        Goo();
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData]
    public async Task TupleWithOneName(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"class Class
{
    ([|IDictionary|] a, string) Method()
    {
        Goo();
    }
}",
@"class Class
{
    (System.Collections.IDictionary a, string) Method()
    {
        Goo();
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/18275")]
    public async Task TestContextualKeyword1(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"
namespace N
{
    class nameof
    {
    }
}

class C
{
    void M()
    {
        [|nameof|]
    }
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/18623")]
    public async Task TestDoNotQualifyToTheSameTypeToFixWrongArity(TestHost testHost)
    {
        await TestMissingInRegularAndScriptAsync(
@"
using System.Collections.Generic;

class Program : [|IReadOnlyCollection|]
{
}", new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/19575")]
    public async Task TestNoNonGenericsWithGenericCodeParsedAsExpression(TestHost testHost)
    {
        var code = @"
class C
{
    private void GetEvaluationRuleNames()
    {
        [|IEnumerable|] < Int32 >
        return ImmutableArray.CreateRange();
    }
}";
        await TestActionCountAsync(code, count: 1, new TestParameters(testHost: testHost));

        await TestInRegularAndScriptAsync(
code,
@"
class C
{
    private void GetEvaluationRuleNames()
    {
        System.Collections.Generic.IEnumerable < Int32 >
        return ImmutableArray.CreateRange();
    }
}", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/49986")]
    public async Task TestInUsingContext_Type(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"using [|Math|];

class Class
{
    void Test()
    {
        Sqrt(1);
    }",
@"using static System.Math;

class Class
{
    void Test()
    {
        Sqrt(1);
    }", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/49986")]
    public async Task TestInUsingContext_Namespace(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"using [|Collections|];

class Class
{
    void Test()
    {
        Sqrt(1);
    }",
@"using System.Collections;

class Class
{
    void Test()
    {
        Sqrt(1);
    }", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/49986")]
    public async Task TestInUsingContext_UsingStatic(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"using static [|Math|];

class Class
{
    void Test()
    {
        Sqrt(1);
    }",
@"using static System.Math;

class Class
{
    void Test()
    {
        Sqrt(1);
    }", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/51274")]
    public async Task TestInUsingContext_UsingAlias(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
@"using M = [|Math|]",
@"using M = System.Math", testHost: testHost);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/54544")]
    public async Task TestAddUsingsEditorBrowsableNeverSameProject(TestHost testHost)
    {
        const string InitialWorkspace = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""lib"" CommonReferences=""true"">
        <Document FilePath=""lib.cs"">
using System.ComponentModel;
namespace ProjectLib
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class Project
    {
    }
}
        </Document>
        <Document FilePath=""Program.cs"">
class Program
{
    static void Main(string[] args)
    {
        Project p = new [|Project()|];
    }
}
</Document>
    </Project>
</Workspace>";

        const string ExpectedDocumentText = @"
class Program
{
    static void Main(string[] args)
    {
        Project p = new [|ProjectLib.Project()|];
    }
}
";

        await TestInRegularAndScript1Async(InitialWorkspace, ExpectedDocumentText, new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/54544")]
    public async Task TestAddUsingsEditorBrowsableNeverDifferentProject(TestHost testHost)
    {
        const string InitialWorkspace = @"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""lib"" CommonReferences=""true"">
        <Document FilePath=""lib.vb"">
imports System.ComponentModel
namespace ProjectLib
    &lt;EditorBrowsable(EditorBrowsableState.Never)&gt;
    public class Project
    end class
end namespace
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Console"" CommonReferences=""true"">
        <ProjectReference>lib</ProjectReference>
        <Document FilePath=""Program.cs"">
class Program
{
    static void Main(string[] args)
    {
        [|Project|] p = new Project();
    }
}
</Document>
    </Project>
</Workspace>";
        await TestMissingAsync(InitialWorkspace, new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/54544")]
    public async Task TestAddUsingsEditorBrowsableAdvancedDifferentProjectOptionOn(TestHost testHost)
    {
        const string InitialWorkspace = @"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""lib"" CommonReferences=""true"">
        <Document FilePath=""lib.vb"">
imports System.ComponentModel
namespace ProjectLib
    &lt;EditorBrowsable(EditorBrowsableState.Advanced)&gt;
    public class Project
    end class
end namespace
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Console"" CommonReferences=""true"">
        <ProjectReference>lib</ProjectReference>
        <Document FilePath=""Program.cs"">
class Program
{
    static void Main(string[] args)
    {
        [|Project|] p = new Project();
    }
}
</Document>
    </Project>
</Workspace>";

        const string ExpectedDocumentText = @"
class Program
{
    static void Main(string[] args)
    {
        ProjectLib.Project p = new Project();
    }
}
";
        await TestInRegularAndScript1Async(InitialWorkspace, ExpectedDocumentText, new TestParameters(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/54544")]
    public async Task TestAddUsingsEditorBrowsableAdvancedDifferentProjectOptionOff(TestHost testHost)
    {
        var initialWorkspace = @"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""lib"" CommonReferences=""true"">
        <Document FilePath=""lib.vb"">
imports System.ComponentModel
namespace ProjectLib
    &lt;EditorBrowsable(EditorBrowsableState.Advanced)&gt;
    public class Project
    end class
end namespace
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Console"" CommonReferences=""true"">
        <ProjectReference>lib</ProjectReference>
        <Document FilePath=""Program.cs"">
class Program
{
    static void Main(string[] args)
    {
        [|Project|] p = new Project();
    }
}
</Document>
    </Project>
</Workspace>";

        await TestMissingAsync(initialWorkspace, new TestParameters(
            globalOptions: Option(MemberDisplayOptionsStorage.HideAdvancedMembers, true),
            testHost: testHost));
    }
}
