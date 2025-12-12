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
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.FullyQualify;

[Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
public sealed class FullyQualifyTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpFullyQualifyCodeFixProvider());

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    [Theory, CombinatorialData]
    public Task TestTypeFromMultipleNamespaces1(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|IDictionary|] Method()
                {
                    Goo();
                }
            }
            """,
            """
            class Class
            {
                System.Collections.IDictionary Method()
                {
                    Goo();
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestTypeFromMultipleNamespaces2(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|IDictionary|] Method()
                {
                    Goo();
                }
            }
            """,
            """
            class Class
            {
                System.Collections.Generic.IDictionary Method()
                {
                    Goo();
                }
            }
            """,
            index: 1, new(testHost: testHost));

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1889385")]
    public Task TestPreservesIncorrectIndentation1(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                  [|IDictionary|] Method()
                {
                    Goo();
                }
            }
            """,
            """
            class Class
            {
                  System.Collections.IDictionary Method()
                {
                    Goo();
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1889385")]
    public Task TestPreservesIncorrectIndentation2(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
            \t[|IDictionary|] Method()
                {
                    Goo();
                }
            }
            """.Replace(@"\t", "\t"),
            """
            class Class
            {
            \tSystem.Collections.IDictionary Method()
                {
                    Goo();
                }
            }
            """.Replace(@"\t", "\t"), new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestGenericWithNoArgs(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|List|] Method()
                {
                    Goo();
                }
            }
            """,
            """
            class Class
            {
                System.Collections.Generic.List Method()
                {
                    Goo();
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestGenericWithCorrectArgs(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|List<int>|] Method()
                {
                    Goo();
                }
            }
            """,
            """
            class Class
            {
                System.Collections.Generic.List<int> Method()
                {
                    Goo();
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestSmartTagDisplayText(TestHost testHost)
        => TestSmartTagTextAsync(
            """
            class Class
            {
                [|List<int>|] Method()
                {
                    Goo();
                }
            }
            """,
            "System.Collections.Generic.List", new TestParameters(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestGenericWithWrongArgs(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|List<int, string>|] Method()
                {
                    Goo();
                }
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestNotOnVar1(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            namespace N
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
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestNotOnVar2(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            namespace N
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
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestGenericInLocalDeclaration(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Goo()
                {
                    [|List<int>|] a = new List<int>();
                }
            }
            """,
            """
            class Class
            {
                void Goo()
                {
                    System.Collections.Generic.List<int> a = new List<int>();
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestGenericItemType(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Class
            {
                List<[|Int32|]> l;
            }
            """,
            """
            using System.Collections.Generic;

            class Class
            {
                List<System.Int32> l;
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestGenerateWithExistingUsings(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Class
            {
                [|List<int>|] Method()
                {
                    Goo();
                }
            }
            """,
            """
            using System;

            class Class
            {
                System.Collections.Generic.List<int> Method()
                {
                    Goo();
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestGenerateInNamespace(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            namespace N
            {
                class Class
                {
                    [|List<int>|] Method()
                    {
                        Goo();
                    }
                }
            }
            """,
            """
            namespace N
            {
                class Class
                {
                    System.Collections.Generic.List<int> Method()
                    {
                        Goo();
                    }
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestGenerateInNamespaceWithUsings(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            namespace N
            {
                using System;

                class Class
                {
                    [|List<int>|] Method()
                    {
                        Goo();
                    }
                }
            }
            """,
            """
            namespace N
            {
                using System;

                class Class
                {
                    System.Collections.Generic.List<int> Method()
                    {
                        Goo();
                    }
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public async Task TestExistingUsing(TestHost testHost)
    {
        await TestActionCountAsync(
            """
            using System.Collections.Generic;

            class Class
            {
                [|IDictionary|] Method()
                {
                    Goo();
                }
            }
            """,
            count: 1, new TestParameters(testHost: testHost));

        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Class
            {
                [|IDictionary|] Method()
                {
                    Goo();
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Class
            {
                System.Collections.IDictionary Method()
                {
                    Goo();
                }
            }
            """, new(testHost: testHost));
    }

    [Theory, CombinatorialData]
    public Task TestMissingIfUniquelyBound(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Class
            {
                [|String|] Method()
                {
                    Goo();
                }
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestMissingIfUniquelyBoundGeneric(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Class
            {
                [|List<int>|] Method()
                {
                    Goo();
                }
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestOnEnum(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
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
            }
            """,
            """
            class Class
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
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestOnClassInheritance(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class : [|Class2|]
            {
            }

            namespace A
            {
                class Class2
                {
                }
            }
            """,
            """
            class Class : A.Class2
            {
            }

            namespace A
            {
                class Class2
                {
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestOnImplementedInterface(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class : [|IGoo|]
            {
            }

            namespace A
            {
                interface IGoo
                {
                }
            }
            """,
            """
            class Class : A.IGoo
            {
            }

            namespace A
            {
                interface IGoo
                {
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public async Task TestAllInBaseList(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
            """
            class Class : [|IGoo|], Class2
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
            }
            """,
            """
            class Class : B.IGoo, Class2
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
            }
            """, new(testHost: testHost));

        await TestInRegularAndScriptAsync(
            """
            class Class : B.IGoo, [|Class2|]
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
            }
            """,
            """
            class Class : B.IGoo, A.Class2
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
            }
            """, new(testHost: testHost));
    }

    [Theory, CombinatorialData]
    public Task TestAttributeUnexpanded(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            [[|Obsolete|]]
            class Class
            {
            }
            """,
            """
            [System.Obsolete]
            class Class
            {
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestAttributeExpanded(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            [[|ObsoleteAttribute|]]
            class Class
            {
            }
            """,
            """
            [System.ObsoleteAttribute]
            class Class
            {
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527360")]
    public Task TestExtensionMethods(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Goo
            {
                void Bar()
                {
                    var values = new List<int>() { 1, 2, 3 };
                    values.[|Where|](i => i > 1);
                }
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538018")]
    public Task TestAfterNew(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Goo()
                {
                    List<int> l;
                    l = new [|List<int>|]();
                }
            }
            """,
            """
            class Class
            {
                void Goo()
                {
                    List<int> l;
                    l = new System.Collections.Generic.List<int>();
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestArgumentsInMethodCall(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Test()
                {
                    Console.WriteLine([|DateTime|].Today);
                }
            }
            """,
            """
            class Class
            {
                void Test()
                {
                    Console.WriteLine(System.DateTime.Today);
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestCallSiteArgs(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Test([|DateTime|] dt)
                {
                }
            }
            """,
            """
            class Class
            {
                void Test(System.DateTime dt)
                {
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestUsePartialClass(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            namespace A
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
            }
            """,
            """
            namespace A
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
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestGenericClassInNestedNamespace(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            namespace A
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
            }
            """,
            """
            namespace A
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
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestBeforeStaticMethod(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Test()
                {
                    [|Math|].Sqrt();
                }
            """,
            """
            class Class
            {
                void Test()
                {
                    System.Math.Sqrt();
                }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538136")]
    public Task TestBeforeNamespace(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            namespace A
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
            }
            """,
            """
            namespace A
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
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527395")]
    public Task TestSimpleNameWithLeadingTrivia(TestHost testHost)
        => TestInRegularAndScriptAsync(
@"class Class { void Test() { /*goo*/[|Int32|] i; } }",
@"class Class { void Test() { /*goo*/System.Int32 i; } }", new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527395")]
    public Task TestGenericNameWithLeadingTrivia(TestHost testHost)
        => TestInRegularAndScriptAsync(
@"class Class { void Test() { /*goo*/[|List<int>|] l; } }",
@"class Class { void Test() { /*goo*/System.Collections.Generic.List<int> l; } }", new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538740")]
    public Task TestFullyQualifyTypeName(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            public class Program
            {
                public class Inner
                {
                }
            }

            class Test
            {
                [|Inner|] i;
            }
            """,
            """
            public class Program
            {
                public class Inner
                {
                }
            }

            class Test
            {
                Program.Inner i;
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/26887")]
    public Task TestFullyQualifyUnboundIdentifier3(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            public class Program
            {
                public class Inner
                {
                }
            }

            class Test
            {
                public [|Inner|] Name
            }
            """,
            """
            public class Program
            {
                public class Inner
                {
                }
            }

            class Test
            {
                public Program.Inner Name
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538740")]
    public Task TestFullyQualifyTypeName_NotForGenericType(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            class Program<T>
            {
                public class Inner
                {
                }
            }

            class Test
            {
                [|Inner|] i;
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538764")]
    public Task TestFullyQualifyThroughAlias(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            using Alias = System;

            class C
            {
                [|Int32|] i;
            }
            """,
            """
            using Alias = System;

            class C
            {
                Alias.Int32 i;
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538763")]
    public Task TestFullyQualifyPrioritizeTypesOverNamespaces1(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            namespace Outer
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
            }
            """,
            """
            namespace Outer
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
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538763")]
    public Task TestFullyQualifyPrioritizeTypesOverNamespaces2(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            namespace Outer
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
            }
            """,
            """
            namespace Outer
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
            }
            """,
            index: 1, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539853")]
    public Task BugFix5950(TestHost testHost)
        => TestAsync(
            @"using System.Console; WriteLine([|Expression|].Constant(123));",
            @"using System.Console; WriteLine(System.Linq.Expressions.Expression.Constant(123));",
            new(parseOptions: GetScriptOptions(), testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540318")]
    public Task TestAfterAlias(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    System::[|Console|] :: WriteLine("TEST");
                }
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540942")]
    public Task TestMissingOnIncompleteStatement(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.IO;

            class C
            {
                static void Main(string[] args)
                {
                    [|Path|] }
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542643")]
    public Task TestAssemblyAttribute(TestHost testHost)
        => TestInRegularAndScriptAsync(
@"[assembly: [|InternalsVisibleTo|](""Project"")]",
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Project"")]", new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543388")]
    public Task TestMissingOnAliasName(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            using [|GIBBERISH|] = Goo.GIBBERISH;

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
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestMissingOnAttributeOverloadResolutionError(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Runtime.InteropServices;

            class M
            {
                [[|DllImport|]()]
                static extern int? My();
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544950")]
    public Task TestNotOnAbstractConstructor(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            using System.IO;

            class Program
            {
                static void Main(string[] args)
                {
                    var s = new [|Stream|]();
                }
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545774")]
    public Task TestAttributeCount(TestHost testHost)
        => TestActionCountAsync(@"[ assembly : [|Guid|] ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ", 2, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545774")]
    public Task TestAttribute(TestHost testHost)
        => TestInRegularAndScriptAsync(
@"[ assembly : [|Guid|] ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ",
@"[ assembly : System.Runtime.InteropServices.Guid ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ", new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546027")]
    public Task TestGeneratePropertyFromAttribute(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            [AttributeUsage(AttributeTargets.Class)]
            class MyAttrAttribute : Attribute
            {
            }

            [MyAttr(123, [|Version|] = 1)]
            class D
            {
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775448")]
    public Task ShouldTriggerOnCS0308(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            using System.Collections;

            class Test
            {
                static void Main(string[] args)
                {
                    [|IEnumerable<int>|] f;
                }
            }
            """,
            """
            using System.Collections;

            class Test
            {
                static void Main(string[] args)
                {
                    System.Collections.Generic.IEnumerable<int> f;
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/947579")]
    public Task AmbiguousTypeFix(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            using n1;
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
            }
            """,
            """
            using n1;
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
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995857")]
    public async Task NonPublicNamespaces(TestHost testHost)
    {
        await TestInRegularAndScriptAsync(
            """
            namespace MS.Internal.Xaml
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
            }
            """,
            """
            namespace MS.Internal.Xaml
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
            }
            """, new(testHost: testHost));

        await TestInRegularAndScriptAsync(
            """
            namespace MS.Internal.Xaml
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
            }
            """,
            """
            namespace MS.Internal.Xaml
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
            }
            """, index: 1, new(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/11071")]
    public Task AmbiguousFixOrdering(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            using n1;
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
            }
            """,
            """
            using n1;
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
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TupleTest(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                ([|IDictionary|], string) Method()
                {
                    Goo();
                }
            }
            """,
            """
            class Class
            {
                (System.Collections.IDictionary, string) Method()
                {
                    Goo();
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TupleWithOneName(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                ([|IDictionary|] a, string) Method()
                {
                    Goo();
                }
            }
            """,
            """
            class Class
            {
                (System.Collections.IDictionary a, string) Method()
                {
                    Goo();
                }
            }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/18275")]
    public Task TestContextualKeyword1(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
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
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/18623")]
    public Task TestDoNotQualifyToTheSameTypeToFixWrongArity(TestHost testHost)
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program : [|IReadOnlyCollection|]
            {
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/19575")]
    public async Task TestNoNonGenericsWithGenericCodeParsedAsExpression(TestHost testHost)
    {
        var code = """
            class C
            {
                private void GetEvaluationRuleNames()
                {
                    [|IEnumerable|] < Int32 >
                    return ImmutableArray.CreateRange();
                }
            }
            """;
        await TestActionCountAsync(code, count: 1, new TestParameters(testHost: testHost));

        await TestInRegularAndScriptAsync(
code,
"""
class C
{
    private void GetEvaluationRuleNames()
    {
        System.Collections.Generic.IEnumerable < Int32 >
        return ImmutableArray.CreateRange();
    }
}
""", new(testHost: testHost));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/49986")]
    public Task TestInUsingContext_Type(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            using [|Math|];

            class Class
            {
                void Test()
                {
                    Sqrt(1);
                }
            """,
            """
            using static System.Math;

            class Class
            {
                void Test()
                {
                    Sqrt(1);
                }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/49986")]
    public Task TestInUsingContext_Namespace(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            using [|Collections|];

            class Class
            {
                void Test()
                {
                    Sqrt(1);
                }
            """,
            """
            using System.Collections;

            class Class
            {
                void Test()
                {
                    Sqrt(1);
                }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/49986")]
    public Task TestInUsingContext_UsingStatic(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            using static [|Math|];

            class Class
            {
                void Test()
                {
                    Sqrt(1);
                }
            """,
            """
            using static System.Math;

            class Class
            {
                void Test()
                {
                    Sqrt(1);
                }
            """, new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/51274")]
    public Task TestInUsingContext_UsingAlias(TestHost testHost)
        => TestInRegularAndScriptAsync(
@"using M = [|Math|]",
@"using M = System.Math", new(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/54544")]
    public Task TestAddUsingsEditorBrowsableNeverSameProject(TestHost testHost)
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="lib" CommonReferences="true">
                    <Document FilePath="lib.cs">using System.ComponentModel;
            namespace ProjectLib
            {
                [EditorBrowsable(EditorBrowsableState.Never)]
                public class Project
                {
                }
            }</Document>
                    <Document FilePath="Program.cs">class Program
            {
                static void Main(string[] args)
                {
                    Project p = new [|Project()|];
                }
            }</Document>
                </Project>
            </Workspace>
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    Project p = new [|ProjectLib.Project()|];
                }
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/54544")]
    public Task TestAddUsingsEditorBrowsableNeverDifferentProject(TestHost testHost)
        => TestMissingAsync("""
            <Workspace>
                <Project Language="Visual Basic" AssemblyName="lib" CommonReferences="true">
                    <Document FilePath="lib.vb">
            imports System.ComponentModel
            namespace ProjectLib
                &lt;EditorBrowsable(EditorBrowsableState.Never)&gt;
                public class Project
                end class
            end namespace
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Console" CommonReferences="true">
                    <ProjectReference>lib</ProjectReference>
                    <Document FilePath="Program.cs">
            class Program
            {
                static void Main(string[] args)
                {
                    [|Project|] p = new Project();
                }
            }
            </Document>
                </Project>
            </Workspace>
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/54544")]
    public Task TestAddUsingsEditorBrowsableAdvancedDifferentProjectOptionOn(TestHost testHost)
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="Visual Basic" AssemblyName="lib" CommonReferences="true">
                    <Document FilePath="lib.vb">
            imports System.ComponentModel
            namespace ProjectLib
                &lt;EditorBrowsable(EditorBrowsableState.Advanced)&gt;
                public class Project
                end class
            end namespace
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Console" CommonReferences="true">
                    <ProjectReference>lib</ProjectReference>
                    <Document FilePath="Program.cs">class Program
            {
                static void Main(string[] args)
                {
                    [|Project|] p = new Project();
                }
            }</Document>
                </Project>
            </Workspace>
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    ProjectLib.Project p = new Project();
                }
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/54544")]
    public Task TestAddUsingsEditorBrowsableAdvancedDifferentProjectOptionOff(TestHost testHost)
        => TestMissingAsync("""
            <Workspace>
                <Project Language="Visual Basic" AssemblyName="lib" CommonReferences="true">
                    <Document FilePath="lib.vb">
            imports System.ComponentModel
            namespace ProjectLib
                &lt;EditorBrowsable(EditorBrowsableState.Advanced)&gt;
                public class Project
                end class
            end namespace
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Console" CommonReferences="true">
                    <ProjectReference>lib</ProjectReference>
                    <Document FilePath="Program.cs">
            class Program
            {
                static void Main(string[] args)
                {
                    [|Project|] p = new Project();
                }
            }
            </Document>
                </Project>
            </Workspace>
            """, new TestParameters(
            options: Option(MemberDisplayOptionsStorage.HideAdvancedMembers, true),
            testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/79462")]
    public Task TestFullyQualifyWithSourceGeneratedFile(TestHost testHost)
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Console" CommonReferences="true">
                    <Document FilePath="Program.cs">using Goo;

            Something a;
            [|PInvoke|].GetMessage();

            namespace Goo
            {
                class Something { }
            }</Document>
                                    <DocumentFromSourceGenerator>
            namespace Win32
            {
                public class PInvoke
                {
                }
            }
                                    </DocumentFromSourceGenerator>
                </Project>
            </Workspace>
            """, """
            using Goo;
            
            Something a;
            Win32.PInvoke.GetMessage();
            
            namespace Goo
            {
                class Something { }
            }
            """, new TestParameters(testHost: testHost));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/79462")]
    public Task TestWithinSourceGeneratedFile(TestHost testHost)
        => TestMissingAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Console" CommonReferences="true">
            <DocumentFromSourceGenerator>
            using Goo;

            Something a;
            [|PInvoke|].GetMessage();

            namespace Goo
            {
                class Something { }
            }</DocumentFromSourceGenerator>
            <DocumentFromSourceGenerator>
            namespace Win32
            {
                public class PInvoke
                {
                }
            }
            </DocumentFromSourceGenerator>
                </Project>
            </Workspace>
            """, new TestParameters(testHost: testHost));
}
