// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddUsing;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
public sealed partial class AddUsingTests(ITestOutputHelper logger) : AbstractAddUsingTests(logger)
{
    [Theory, CombinatorialData]
    public Task TestTypeFromMultipleNamespaces1(TestHost testHost)
        => TestAsync(
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
            using System.Collections;

            class Class
            {
                IDictionary Method()
                {
                    Goo();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestTypeFromMultipleNamespaces1_FileScopedNamespace_Outer(TestHost testHost)
        => TestAsync(
            """
            namespace N;

            class Class
            {
                [|IDictionary|] Method()
                {
                    Goo();
                }
            }
            """,
            """
            using System.Collections;

            namespace N;

            class Class
            {
                IDictionary Method()
                {
                    Goo();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestTypeFromMultipleNamespaces1_FileScopedNamespace_Inner(TestHost testHost)
        => TestAsync(
            """
            namespace N;

            using System;

            class Class
            {
                [|IDictionary|] Method()
                {
                    Goo();
                }
            }
            """,
            """
            namespace N;

            using System;
            using System.Collections;

            class Class
            {
                IDictionary Method()
                {
                    Goo();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/11241")]
    public Task TestAddImportWithCaseChange(TestHost testHost)
        => TestAsync(
            """
            namespace N1
            {
                public class TextBox
                {
                }
            }

            class Class1 : [|Textbox|]
            {
            }
            """,
            """
            using N1;

            namespace N1
            {
                public class TextBox
                {
                }
            }

            class Class1 : TextBox
            {
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestTypeFromMultipleNamespaces2(TestHost testHost)
        => TestAsync(
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
            using System.Collections.Generic;

            class Class
            {
                IDictionary Method()
                {
                    Goo();
                }
            }
            """,
            testHost, index: 1);

    [Theory, CombinatorialData]
    public Task TestGenericWithNoArgs(TestHost testHost)
        => TestAsync(
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
            using System.Collections.Generic;

            class Class
            {
                List Method()
                {
                    Goo();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestGenericWithCorrectArgs(TestHost testHost)
        => TestAsync(
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
            using System.Collections.Generic;

            class Class
            {
                List<int> Method()
                {
                    Goo();
                }
            }
            """, testHost);

    [Fact]
    public Task TestGenericWithWrongArgs1()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|List<int, string, bool>|] Method()
                {
                    Goo();
                }
            }
            """);

    [Fact]
    public Task TestGenericWithWrongArgs2()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|List<int, string>|] Method()
                {
                    Goo();
                }
            }
            """);

    [Theory, CombinatorialData]
    public Task TestGenericInLocalDeclaration(TestHost testHost)
        => TestAsync(
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
            using System.Collections.Generic;

            class Class
            {
                void Goo()
                {
                    List<int> a = new List<int>();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestGenericItemType(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;

            class Class
            {
                List<[|Int32|]> l;
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class Class
            {
                List<Int32> l;
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestGenerateWithExistingUsings(TestHost testHost)
        => TestAsync(
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
            using System.Collections.Generic;

            class Class
            {
                List<int> Method()
                {
                    Goo();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestGenerateInNamespace(TestHost testHost)
        => TestAsync(
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
            using System.Collections.Generic;

            namespace N
            {
                class Class
                {
                    List<int> Method()
                    {
                        Goo();
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestGenerateInNamespaceWithUsings(TestHost testHost)
        => TestAsync(
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
                using System.Collections.Generic;

                class Class
                {
                    List<int> Method()
                    {
                        Goo();
                    }
                }
            }
            """, testHost);

    [Fact]
    public Task TestExistingUsing_ActionCount()
        => TestActionCountAsync(
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
            count: 1);

    [Theory, CombinatorialData]
    public Task TestExistingUsing(TestHost testHost)
        => TestAsync(
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
            using System.Collections;
            using System.Collections.Generic;

            class Class
            {
                IDictionary Method()
                {
                    Goo();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541730")]
    public Task TestAddUsingForGenericExtensionMethod(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;

            class Class
            {
                void Method(IList<int> args)
                {
                    args.[|Where|]() }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;

            class Class
            {
                void Method(IList<int> args)
                {
                    args.Where() }
            }
            """, testHost);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541730")]
    public Task TestAddUsingForNormalExtensionMethod()
        => TestAsync(
            """
            class Class
            {
                void Method(Class args)
                {
                    args.[|Where|]() }
            }

            namespace N
            {
                static class E
                {
                    public static void Where(this Class c)
                    {
                    }
                }
            }
            """,
            """
            using N;

            class Class
            {
                void Method(Class args)
                {
                    args.Where() }
            }

            namespace N
            {
                static class E
                {
                    public static void Where(this Class c)
                    {
                    }
                }
            }
            """,
            new TestParameters(parseOptions: Options.Regular));

    [Theory, CombinatorialData]
    public Task TestOnEnum(TestHost testHost)
        => TestAsync(
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
            using A;

            class Class
            {
                void Goo()
                {
                    var a = Colors.Red;
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
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestOnClassInheritance(TestHost testHost)
        => TestAsync(
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
            using A;

            class Class : Class2
            {
            }

            namespace A
            {
                class Class2
                {
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestOnImplementedInterface(TestHost testHost)
        => TestAsync(
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
            using A;

            class Class : IGoo
            {
            }

            namespace A
            {
                interface IGoo
                {
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public async Task TestAllInBaseList(TestHost testHost)
    {
        await TestAsync(
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
            using B;

            class Class : IGoo, Class2
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
            """, testHost);

        await TestAsync(
            """
            using B;

            class Class : IGoo, [|Class2|]
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
            using A;
            using B;

            class Class : IGoo, Class2
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
            """, testHost);
    }

    [Theory, CombinatorialData]
    public Task TestAttributeUnexpanded(TestHost testHost)
        => TestAsync(
            """
            [[|Obsolete|]]
            class Class
            {
            }
            """,
            """
            using System;

            [Obsolete]
            class Class
            {
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAttributeExpanded(TestHost testHost)
        => TestAsync(
            """
            [[|ObsoleteAttribute|]]
            class Class
            {
            }
            """,
            """
            using System;

            [ObsoleteAttribute]
            class Class
            {
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538018")]
    public Task TestAfterNew(TestHost testHost)
        => TestAsync(
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
            using System.Collections.Generic;

            class Class
            {
                void Goo()
                {
                    List<int> l;
                    l = new List<int>();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestArgumentsInMethodCall(TestHost testHost)
        => TestAsync(
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
            using System;

            class Class
            {
                void Test()
                {
                    Console.WriteLine(DateTime.Today);
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestCallSiteArgs(TestHost testHost)
        => TestAsync(
            """
            class Class
            {
                void Test([|DateTime|] dt)
                {
                }
            }
            """,
            """
            using System;

            class Class
            {
                void Test(DateTime dt)
                {
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestUsePartialClass(TestHost testHost)
        => TestAsync(
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
            using B;

            namespace A
            {
                public class Class
                {
                    PClass c;
                }
            }

            namespace B
            {
                public partial class PClass
                {
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestGenericClassInNestedNamespace(TestHost testHost)
        => TestAsync(
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
            using A.B;

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
                    GenericClass<int> c;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541730")]
    public Task TestExtensionMethods(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;

            class Goo
            {
                void Bar()
                {
                    var values = new List<int>();
                    values.[|Where|](i => i > 1);
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;

            class Goo
            {
                void Bar()
                {
                    var values = new List<int>();
                    values.Where(i => i > 1);
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541730")]
    public Task TestQueryPatterns(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;

            class Goo
            {
                void Bar()
                {
                    var values = new List<int>();
                    var q = [|from v in values
                            where v > 1
                            select v + 10|];
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;

            class Goo
            {
                void Bar()
                {
                    var values = new List<int>();
                    var q = from v in values
                            where v > 1
                            select v + 10;
                }
            }
            """, testHost);

    // Tests for Insertion Order
    [Theory, CombinatorialData]
    public Task TestSimplePresortedUsings1(TestHost testHost)
        => TestAsync(
            """
            using B;
            using C;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace D
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using B;
            using C;
            using D;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace D
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestSimplePresortedUsings2(TestHost testHost)
        => TestAsync(
            """
            using B;
            using C;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using A;
            using B;
            using C;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestSimpleUnsortedUsings1(TestHost testHost)
        => TestAsync(
            """
            using C;
            using B;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using C;
            using B;
            using A;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestSimpleUnsortedUsings2(TestHost testHost)
        => TestAsync(
            """
            using D;
            using B;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace C
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using D;
            using B;
            using C;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace C
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestMultiplePresortedUsings1(TestHost testHost)
        => TestAsync(
            """
            using B.X;
            using B.Y;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace B
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using B;
            using B.X;
            using B.Y;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace B
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestMultiplePresortedUsings2(TestHost testHost)
        => TestAsync(
            """
            using B.X;
            using B.Y;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace B.A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using B.A;
            using B.X;
            using B.Y;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace B.A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestMultiplePresortedUsings3(TestHost testHost)
        => TestAsync(
            """
            using B.X;
            using B.Y;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace B
            {
                namespace A
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }
                }
            }
            """,
            """
            using B.A;
            using B.X;
            using B.Y;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace B
            {
                namespace A
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestMultipleUnsortedUsings1(TestHost testHost)
        => TestAsync(
            """
            using B.Y;
            using B.X;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace B
            {
                namespace A
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }
                }
            }
            """,
            """
            using B.Y;
            using B.X;
            using B.A;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace B
            {
                namespace A
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestMultipleUnsortedUsings2(TestHost testHost)
        => TestAsync(
            """
            using B.Y;
            using B.X;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace B
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using B.Y;
            using B.X;
            using B;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace B
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """, testHost);

    // System on top cases
    [Theory, CombinatorialData]
    public Task TestSimpleSystemSortedUsings1(TestHost testHost)
        => TestAsync(
            """
            using System;
            using B;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using System;
            using A;
            using B;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    public Task TestSimpleSystemSortedUsings2(TestHost testHost)
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using B;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using A;
            using B;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    public Task TestSimpleSystemSortedUsings3(TestHost testHost)
        => TestAsync(
            """
            using A;
            using B;

            class Class
            {
                void Method()
                {
                    [|Console|].Write(1);
                }
            }
            """,
            """
            using System;
            using A;
            using B;

            class Class
            {
                void Method()
                {
                    Console.Write(1);
                }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    public Task TestSimpleSystemUnsortedUsings1(TestHost testHost)
        => TestAsync(
            """
            using C;
            using B;
            using System;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using C;
            using B;
            using System;
            using A;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    public Task TestSimpleSystemUnsortedUsings2(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;
            using System;
            using B;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System;
            using B;
            using A;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    public Task TestSimpleSystemUnsortedUsings3(TestHost testHost)
        => TestAsync(
            """
            using B;
            using A;

            class Class
            {
                void Method()
                {
                    [|Console|].Write(1);
                }
            }
            """,
            """
            using B;
            using A;
            using System;

            class Class
            {
                void Method()
                {
                    Console.Write(1);
                }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    public Task TestSimpleBogusSystemUsings1(TestHost testHost)
        => TestAsync(
            """
            using A.System;

            class Class
            {
                void Method()
                {
                    [|Console|].Write(1);
                }
            }
            """,
            """
            using System;
            using A.System;

            class Class
            {
                void Method()
                {
                    Console.Write(1);
                }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    public Task TestSimpleBogusSystemUsings2(TestHost testHost)
        => TestAsync(
            """
            using System.System;

            class Class
            {
                void Method()
                {
                    [|Console|].Write(1);
                }
            }
            """,
            """
            using System;
            using System.System;

            class Class
            {
                void Method()
                {
                    Console.Write(1);
                }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    public Task TestUsingsWithComments(TestHost testHost)
        => TestAsync(
            """
            using System./*...*/.Collections.Generic;

            class Class
            {
                void Method()
                {
                    [|Console|].Write(1);
                }
            }
            """,
            """
            using System;
            using System./*...*/.Collections.Generic;

            class Class
            {
                void Method()
                {
                    Console.Write(1);
                }
            }
            """,
            testHost);

    // System Not on top cases
    [Theory, CombinatorialData]
    public Task TestSimpleSystemUnsortedUsings4(TestHost testHost)
        => TestAsync(
            """
            using C;
            using System;
            using B;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using C;
            using System;
            using B;
            using A;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    public Task TestSimpleSystemSortedUsings5(TestHost testHost)
        => TestAsync(
            """
            using B;
            using System;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using A;
            using B;
            using System;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    public Task TestSimpleSystemSortedUsings4(TestHost testHost)
        => TestAsync(
            """
            using A;
            using B;

            class Class
            {
                void Method()
                {
                    [|Console|].Write(1);
                }
            }
            """,
            """
            using A;
            using B;
            using System;

            class Class
            {
                void Method()
                {
                    Console.Write(1);
                }
            }
            """,
            testHost, options: Option(GenerationOptions.PlaceSystemNamespaceFirst, false));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538136")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538763")]
    public Task TestAddUsingForNamespace()
        => TestMissingInRegularAndScriptAsync(
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
            """);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538220")]
    public Task TestAddUsingForFieldWithFormatting(TestHost testHost)
        => TestAsync(
            @"class C { [|DateTime|] t; }",
            """
            using System;

            class C { DateTime t; }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539657")]
    public Task BugFix5688(TestHost testHost)
        => TestAsync(
            @"class Program { static void Main ( string [ ] args ) { [|Console|] . Out . NewLine = ""\r\n\r\n"" ; } }",
            """
            using System;

            class Program { static void Main ( string [ ] args ) { Console . Out . NewLine = "\r\n\r\n" ; } }
            """, testHost);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539853")]
    public Task BugFix5950()
        => TestAsync(
@"using System.Console; WriteLine([|Expression|].Constant(123));",
"""
using System.Console;
using System.Linq.Expressions;

WriteLine(Expression.Constant(123));
""",
new TestParameters(parseOptions: GetScriptOptions()));

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540339")]
    public Task TestAddAfterDefineDirective1(TestHost testHost)
        => TestAsync(
            """
            #define goo

            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    [|Console|].WriteLine();
                }
            }
            """,
            """
            #define goo

            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540339")]
    public Task TestAddAfterDefineDirective2(TestHost testHost)
        => TestAsync(
            """
            #define goo

            class Program
            {
                static void Main(string[] args)
                {
                    [|Console|].WriteLine();
                }
            }
            """,
            """
            #define goo

            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddAfterDefineDirective3(TestHost testHost)
        => TestAsync(
            """
            #define goo

            /// Goo
            class Program
            {
                static void Main(string[] args)
                {
                    [|Console|].WriteLine();
                }
            }
            """,
            """
            #define goo

            using System;

            /// Goo
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddAfterDefineDirective4(TestHost testHost)
        => TestAsync(
            """
            #define goo

            // Goo
            class Program
            {
                static void Main(string[] args)
                {
                    [|Console|].WriteLine();
                }
            }
            """,
            """
            #define goo

            // Goo
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddAfterExistingBanner(TestHost testHost)
        => TestAsync(
            """
            // Banner
            // Banner

            class Program
            {
                static void Main(string[] args)
                {
                    [|Console|].WriteLine();
                }
            }
            """,
            """
            // Banner
            // Banner

            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddAfterExternAlias1(TestHost testHost)
        => TestAsync(
            """
            #define goo

            extern alias Goo;

            class Program
            {
                static void Main(string[] args)
                {
                    [|Console|].WriteLine();
                }
            }
            """,
            """
            #define goo

            extern alias Goo;

            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddAfterExternAlias2(TestHost testHost)
        => TestAsync(
            """
            #define goo

            extern alias Goo;

            using System.Collections;

            class Program
            {
                static void Main(string[] args)
                {
                    [|Console|].WriteLine();
                }
            }
            """,
            """
            #define goo

            extern alias Goo;

            using System;
            using System.Collections;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();
                }
            }
            """, testHost);

    [Fact]
    public async Task TestWithReferenceDirective()
    {
        var resolver = new TestMetadataReferenceResolver(assemblyNames: new Dictionary<string, PortableExecutableReference>()
        {
            { "exprs", AssemblyMetadata.CreateFromImage(Net461.Resources.SystemCore).GetReference() }
        });

        await TestAsync(
            """
            #r "exprs"
            [|Expression|]
            """,
            """
            #r "exprs"
            using System.Linq.Expressions;

            Expression
            """,
            new TestParameters(GetScriptOptions(),
            TestOptions.ReleaseDll.WithMetadataReferenceResolver(resolver)));
    }

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542643")]
    public Task TestAssemblyAttribute(TestHost testHost)
        => TestAsync(
            @"[assembly: [|InternalsVisibleTo|](""Project"")]",
            """
            using System.Runtime.CompilerServices;

            [assembly: InternalsVisibleTo("Project")]
            """, testHost);

    [Fact]
    public Task TestDoNotAddIntoHiddenRegion()
        => TestMissingInRegularAndScriptAsync(
            """
            #line hidden
            using System.Collections.Generic;
            #line default

            class Program
            {
                void Main()
                {
                    [|DateTime|] d;
                }
            }
            """);

    [Theory, CombinatorialData]
    public Task TestAddToVisibleRegion(TestHost testHost)
        => TestAsync(
            """
            #line default
            using System.Collections.Generic;

            #line hidden
            class Program
            {
                void Main()
                {
            #line default
                    [|DateTime|] d;
            #line hidden
                }
            }
            #line default
            """,
            """
            #line default
            using System;
            using System.Collections.Generic;

            #line hidden
            class Program
            {
                void Main()
                {
            #line default
                    DateTime d;
            #line hidden
                }
            }
            #line default
            """, testHost);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545248")]
    public Task TestVenusGeneration1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void Goo()
                {
            #line 1 "Default.aspx"
                    using (new [|StreamReader|]())
                    {
            #line default
            #line hidden
                    }
                }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545774")]
    public Task TestAttribute_ActionCount()
        => TestActionCountAsync(@"[ assembly : [|Guid|] ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ", 2);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545774")]
    public Task TestAttribute(TestHost testHost)
        => TestAsync(
            @"[ assembly : [|Guid|] ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ]",
            """
            using System.Runtime.InteropServices;

            [assembly : Guid ( "9ed54f84-a89d-4fcd-a854-44251e925f09" ) ]
            """, testHost);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546833")]
    public Task TestNotOnOverloadResolutionError()
        => TestMissingInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                class Program
                {
                    void Main()
                    {
                        var test = new [|Test|]("");
                    }
                }

                class Test
                {
                }
            }
            """);

    [Theory, CombinatorialData]
    [WorkItem(17020, "DevDiv_Projects/Roslyn")]
    public Task TestAddUsingForGenericArgument(TestHost testHost)
        => TestAsync(
            """
            namespace ConsoleApplication10
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        var inArgument = new InArgument<[|IEnumerable<int>|]>(new int[] { 1, 2, 3 });
                    }
                }

                public class InArgument<T>
                {
                    public InArgument(T constValue)
                    {
                    }
                }
            }
            """,
            """
            using System.Collections.Generic;

            namespace ConsoleApplication10
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        var inArgument = new InArgument<IEnumerable<int>>(new int[] { 1, 2, 3 });
                    }
                }

                public class InArgument<T>
                {
                    public InArgument(T constValue)
                    {
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775448")]
    public Task ShouldTriggerOnCS0308(TestHost testHost)
        => TestAsync(
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
            using System.Collections.Generic;

            class Test
            {
                static void Main(string[] args)
                {
                    IEnumerable<int> f;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/838253")]
    public Task TestConflictedInaccessibleType(TestHost testHost)
        => TestAsync(
            """
            using System.Diagnostics;

            namespace N
            {
                public class Log
                {
                }
            }

            class C
            {
                static void Main(string[] args)
                {
                    [|Log|] }
            }
            """,
            """
            using System.Diagnostics;
            using N;

            namespace N
            {
                public class Log
                {
                }
            }

            class C
            {
                static void Main(string[] args)
                {
                    Log }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858085")]
    public Task TestConflictedAttributeName(TestHost testHost)
        => TestAsync(
            """
            [[|Description|]]
            class Description
            {
            }
            """,
            """
            using System.ComponentModel;

            [Description]
            class Description
            {
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/872908")]
    public Task TestConflictedGenericName(TestHost testHost)
        => TestAsync(
            """
            using Task = System.AccessViolationException;

            class X
            {
                [|Task<X> x;|]
            }
            """,
            """
            using System.Threading.Tasks;
            using Task = System.AccessViolationException;

            class X
            {
                Task<X> x;
            }
            """, testHost);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/913300")]
    public Task TestNoDuplicateReport_ActionCount()
        => TestActionCountInAllFixesAsync(
            """
            class C
            {
                void M(P p)
                {
                    [|Console|]
                }

                static void Main(string[] args)
                {
                }
            }
            """, count: 1);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/913300")]
    public Task TestNoDuplicateReport(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void M(P p)
                {
                    [|Console|] }

                static void Main(string[] args)
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(P p)
                {
                    Console }

                static void Main(string[] args)
                {
                }
            }
            """, testHost);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/938296")]
    public Task TestNullParentInNode()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class MultiDictionary<K, V> : Dictionary<K, HashSet<V>>
            {
                void M()
                {
                    new HashSet<V>([|Comparer|]);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968303")]
    public Task TestMalformedUsingSection()
        => TestMissingInRegularAndScriptAsync(
            """
            [ class Class
            {
                [|List<|] }
            """);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/875899")]
    public Task TestAddUsingsWithExternAlias(TestHost testHost)
        => TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="lib" CommonReferences="true">
                    <Document FilePath="lib.cs">namespace ProjectLib
            {
                public class Project
                {
                }
            }</Document>
                </Project>
                <Project Language="C#" AssemblyName="Console" CommonReferences="true">
                    <ProjectReference Alias="P">lib</ProjectReference>
                    <Document FilePath="Program.cs">namespace ExternAliases
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Project p = new [|Project()|];
                    }
                }
            }</Document>
                </Project>
            </Workspace>
            """, """
            extern alias P;

            using P::ProjectLib;

            namespace ExternAliases
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Project p = new Project();
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/875899")]
    public Task TestAddUsingsWithPreExistingExternAlias(TestHost testHost)
        => TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="lib" CommonReferences="true">
                    <Document FilePath="lib.cs">namespace ProjectLib
            {
                public class Project
                {
                }
            }

            namespace AnotherNS
            {
                public class AnotherClass
                {
                }
            }</Document>
                </Project>
                <Project Language="C#" AssemblyName="Console" CommonReferences="true">
                    <ProjectReference Alias="P">lib</ProjectReference>
                    <Document FilePath="Program.cs">extern alias P;
            using P::ProjectLib;
            namespace ExternAliases
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Project p = new Project();
                        var x = new [|AnotherClass()|];
                    }
                }
            }</Document>
                </Project>
            </Workspace>
            """, """
            extern alias P;

            using P::AnotherNS;
            using P::ProjectLib;
            namespace ExternAliases
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Project p = new Project();
                        var x = new [|AnotherClass()|];
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/875899")]
    public Task TestAddUsingsWithPreExistingExternAlias_FileScopedNamespace(TestHost testHost)
        => TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="lib" CommonReferences="true">
                    <Document FilePath="lib.cs">namespace ProjectLib;
            {
                public class Project
                {
                }
            }

            namespace AnotherNS
            {
                public class AnotherClass
                {
                }
            }</Document>
                </Project>
                <Project Language="C#" AssemblyName="Console" CommonReferences="true">
                    <ProjectReference Alias="P">lib</ProjectReference>
                    <Document FilePath="Program.cs">extern alias P;
            using P::ProjectLib;
            namespace ExternAliases;

            class Program
            {
                static void Main(string[] args)
                {
                    Project p = new Project();
                    var x = new [|AnotherClass()|];
                }
            }</Document>
                </Project>
            </Workspace>
            """, """
            extern alias P;

            using P::AnotherNS;
            using P::ProjectLib;
            namespace ExternAliases;

            class Program
            {
                static void Main(string[] args)
                {
                    Project p = new Project();
                    var x = new [|AnotherClass()|];
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/875899")]
    public Task TestAddUsingsNoExtern(TestHost testHost)
        => TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="lib" CommonReferences="true">
                    <Document FilePath="lib.cs">namespace AnotherNS
            {
                public class AnotherClass
                {
                }
            }</Document>
                </Project>
                <Project Language="C#" AssemblyName="Console" CommonReferences="true">
                    <ProjectReference Alias="P">lib</ProjectReference>
                    <Document FilePath="Program.cs">using P::AnotherNS;
            namespace ExternAliases
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        var x = new [|AnotherClass()|];
                    }
                }
            }</Document>
                </Project>
            </Workspace>
            """, """
            extern alias P;

            using P::AnotherNS;
            namespace ExternAliases
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        var x = new AnotherClass();
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/875899")]
    public Task TestAddUsingsNoExtern_FileScopedNamespace(TestHost testHost)
        => TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="lib" CommonReferences="true">
                    <Document FilePath="lib.cs">namespace AnotherNS;

            public class AnotherClass
            {
            }</Document>
                </Project>
                <Project Language="C#" AssemblyName="Console" CommonReferences="true">
                    <ProjectReference Alias="P">lib</ProjectReference>
                    <Document FilePath="Program.cs">using P::AnotherNS;
            namespace ExternAliases;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new [|AnotherClass()|];
                }
            }</Document>
                </Project>
            </Workspace>
            """, """
            extern alias P;

            using P::AnotherNS;
            namespace ExternAliases;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new AnotherClass();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/875899")]
    public Task TestAddUsingsNoExternFilterGlobalAlias(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|INotifyPropertyChanged.PropertyChanged|]
                }
            }
            """,
            """
            using System.ComponentModel;

            class Program
            {
                static void Main(string[] args)
                {
                    INotifyPropertyChanged.PropertyChanged
                }
            }
            """, testHost);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916368")]
    public async Task TestAddUsingForCref()
    {
        var options = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose);

        await TestAsync("""
            /// <summary>
            /// This is just like <see cref='[|INotifyPropertyChanged|]'/>, but this one is mine.
            /// </summary>
            interface MyNotifyPropertyChanged { }
            """, """
            using System.ComponentModel;

            /// <summary>
            /// This is just like <see cref='INotifyPropertyChanged'/>, but this one is mine.
            /// </summary>
            interface MyNotifyPropertyChanged { }
            """, new TestParameters(parseOptions: options));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916368")]
    public async Task TestAddUsingForCref2()
    {
        var options = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose);

        await TestAsync("""
            /// <summary>
            /// This is just like <see cref='[|INotifyPropertyChanged.PropertyChanged|]'/>, but this one is mine.
            /// </summary>
            interface MyNotifyPropertyChanged { }
            """, """
            using System.ComponentModel;

            /// <summary>
            /// This is just like <see cref='INotifyPropertyChanged.PropertyChanged'/>, but this one is mine.
            /// </summary>
            interface MyNotifyPropertyChanged { }
            """, new TestParameters(parseOptions: options));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916368")]
    public async Task TestAddUsingForCref3()
    {
        var options = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose);

        await TestAsync("""
            namespace N1
            {
                public class D { }
            }

            public class MyClass
            {
                public static explicit operator N1.D (MyClass f)
                {
                    return default(N1.D);
                }
            }

            /// <seealso cref='MyClass.explicit operator [|D(MyClass)|]'/>
            public class MyClass2
            {
            }
            """, """
            using N1;

            namespace N1
            {
                public class D { }
            }

            public class MyClass
            {
                public static explicit operator N1.D (MyClass f)
                {
                    return default(N1.D);
                }
            }

            /// <seealso cref='MyClass.explicit operator D(MyClass)'/>
            public class MyClass2
            {
            }
            """, new TestParameters(parseOptions: options));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916368")]
    public async Task TestAddUsingForCref4()
    {
        var options = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose);

        await TestAsync("""
            namespace N1
            {
                public class D { }
            }

            /// <seealso cref='[|Test(D)|]'/>
            public class MyClass
            {
                public void Test(N1.D i)
                {
                }
            }
            """, """
            using N1;

            namespace N1
            {
                public class D { }
            }

            /// <seealso cref='Test(D)'/>
            public class MyClass
            {
                public void Test(N1.D i)
                {
                }
            }
            """, new TestParameters(parseOptions: options));
    }

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773614")]
    public Task TestAddStaticType(TestHost testHost)
        => TestAsync("""
            using System;

            public static class Outer
            {
                [AttributeUsage(AttributeTargets.All)]
                public class MyAttribute : Attribute
                {

                }
            }

            [[|My|]]
            class Test
            {}
            """, """
            using System;
            using static Outer;

            public static class Outer
            {
                [AttributeUsage(AttributeTargets.All)]
                public class MyAttribute : Attribute
                {

                }
            }

            [My]
            class Test
            {}
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773614")]
    public Task TestAddStaticType2(TestHost testHost)
        => TestAsync("""
            using System;

            public static class Outer
            {
                public static class Inner
                {
                    [AttributeUsage(AttributeTargets.All)]
                    public class MyAttribute : Attribute
                    {
                    }
                }
            }

            [[|My|]]
            class Test
            {}
            """, """
            using System;
            using static Outer.Inner;

            public static class Outer
            {
                public static class Inner
                {
                    [AttributeUsage(AttributeTargets.All)]
                    public class MyAttribute : Attribute
                    {
                    }
                }
            }

            [My]
            class Test
            {}
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773614")]
    public Task TestAddStaticType3(TestHost testHost)
        => TestAsync(
            """
            using System;

            public static class Outer
            {
                public class Inner
                {
                    [AttributeUsage(AttributeTargets.All)]
                    public class MyAttribute : Attribute
                    {
                    }
                }
            }

            [[|My|]]
            class Test
            {
            }
            """,
            """
            using System;
            using static Outer.Inner;

            public static class Outer
            {
                public class Inner
                {
                    [AttributeUsage(AttributeTargets.All)]
                    public class MyAttribute : Attribute
                    {
                    }
                }
            }

            [My]
            class Test
            {
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773614")]
    public Task TestAddStaticType4(TestHost testHost)
        => TestAsync("""
            using System;
            using Outer;

            public static class Outer
            {
                public static class Inner
                {
                    [AttributeUsage(AttributeTargets.All)]
                    public class MyAttribute : Attribute
                    {
                    }
                }
            }

            [[|My|]]
            class Test
            {}
            """, """
            using System;
            using Outer;
            using static Outer.Inner;

            public static class Outer
            {
                public static class Inner
                {
                    [AttributeUsage(AttributeTargets.All)]
                    public class MyAttribute : Attribute
                    {
                    }
                }
            }

            [My]
            class Test
            {}
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991463")]
    public Task TestAddInsideUsingDirective1(TestHost testHost)
        => TestAsync(
            """
            namespace ns
            {
                using B = [|Byte|];
            }
            """,
            """
            using System;

            namespace ns
            {
                using B = Byte;
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991463")]
    public Task TestAddInsideUsingDirective2(TestHost testHost)
        => TestAsync(
            """
            using System.Collections;

            namespace ns
            {
                using B = [|Byte|];
            }
            """,
            """
            using System;
            using System.Collections;

            namespace ns
            {
                using B = Byte;
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991463")]
    public Task TestAddInsideUsingDirective3(TestHost testHost)
        => TestAsync(
            """
            namespace ns2
            {
                namespace ns3
                {
                    namespace ns
                    {
                        using B = [|Byte|];

                        namespace ns4
                        {
                        }
                    }
                }
            }
            """,
            """
            using System;

            namespace ns2
            {
                namespace ns3
                {
                    namespace ns
                    {
                        using B = Byte;

                        namespace ns4
                        {
                        }
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991463")]
    public Task TestAddInsideUsingDirective4(TestHost testHost)
        => TestAsync(
            """
            namespace ns2
            {
                using System.Collections;

                namespace ns3
                {
                    namespace ns
                    {
                        using System.IO;
                        using B = [|Byte|];
                    }
                }
            }
            """,
            """
            namespace ns2
            {
                using System;
                using System.Collections;

                namespace ns3
                {
                    namespace ns
                    {
                        using System.IO;
                        using B = Byte;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991463")]
    public Task TestAddInsideUsingDirective5(TestHost testHost)
        => TestAsync(
            """
            using System.IO;

            namespace ns2
            {
                using System.Diagnostics;

                namespace ns3
                {
                    using System.Collections;

                    namespace ns
                    {
                        using B = [|Byte|];
                    }
                }
            }
            """,
            """
            using System.IO;

            namespace ns2
            {
                using System.Diagnostics;

                namespace ns3
                {
                    using System;
                    using System.Collections;

                    namespace ns
                    {
                        using B = Byte;
                    }
                }
            }
            """, testHost);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991463")]
    public Task TestAddInsideUsingDirective6()
        => TestMissingInRegularAndScriptAsync(
@"using B = [|Byte|];");

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestAddConditionalAccessExpression(TestHost testHost)
        => TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
                    <Document FilePath = "Program">public class C
            {
                void Main(C a)
                {
                    C x = a?[|.B()|];
                }
            }</Document>
                   <Document FilePath = "Extensions">namespace Extensions
            {
                public static class E
                {
                    public static C B(this C c) { return c; }
                }
            }</Document>
                </Project>
            </Workspace>
            """, """
            using Extensions;

            public class C
            {
                void Main(C a)
                {
                    C x = a?.B();
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestAddConditionalAccessExpression2(TestHost testHost)
        => TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
                    <Document FilePath = "Program">public class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int? x = a?.B.[|C()|];
                }

                public class E
                {
                }
            }</Document>
                   <Document FilePath = "Extensions">namespace Extensions
            {
                public static class D
                {
                    public static C.E C(this C.E c) { return c; }
                }
            }</Document>
                </Project>
            </Workspace>
            """, """
            using Extensions;

            public class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int? x = a?.B.C();
                }

                public class E
                {
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089138")]
    public Task TestAmbiguousUsingName(TestHost testHost)
        => TestAsync(
            """
            namespace ClassLibrary1
            {
                using System;

                public class SomeTypeUser
                {
                    [|SomeType|] field;
                }
            }

            namespace SubNamespaceName
            {
                using System;

                class SomeType
                {
                }
            }

            namespace ClassLibrary1.SubNamespaceName
            {
                using System;

                class SomeOtherFile
                {
                }
            }
            """,
            """
            namespace ClassLibrary1
            {
                using System;
                using global::SubNamespaceName;

                public class SomeTypeUser
                {
                    SomeType field;
                }
            }

            namespace SubNamespaceName
            {
                using System;

                class SomeType
                {
                }
            }

            namespace ClassLibrary1.SubNamespaceName
            {
                using System;

                class SomeOtherFile
                {
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddUsingInDirective(TestHost testHost)
        => TestAsync(
            """
            #define DEBUG
            #if DEBUG
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using System.Text;
            #endif
            class Program
            {
                static void Main(string[] args)
                {
                    var a = [|File|].OpenRead("");
                }
            }
            """,
            """
            #define DEBUG
            #if DEBUG
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using System.Text;
            using System.IO;
            #endif
            class Program
            {
                static void Main(string[] args)
                {
                    var a = File.OpenRead("");
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddUsingInDirective2(TestHost testHost)
        => TestAsync(
            """
            #define DEBUG
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            #if DEBUG
            using System.Text;
            #endif
            class Program { static void Main ( string [ ] args ) { var a = [|File|] . OpenRead ( "" ) ; } }
            """,
            """
            #define DEBUG
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using System.IO;

            #if DEBUG
            using System.Text;
            #endif
            class Program { static void Main ( string [ ] args ) { var a = File . OpenRead ( "" ) ; } }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddUsingInDirective3(TestHost testHost)
        => TestAsync(
            """
            #define DEBUG
            using System;
            using System.Collections.Generic;
            #if DEBUG
            using System.Text;
            #endif
            using System.Linq;
            using System.Threading.Tasks;
            class Program { static void Main ( string [ ] args ) { var a = [|File|] . OpenRead ( "" ) ; } }
            """,
            """
            #define DEBUG
            using System;
            using System.Collections.Generic;
            #if DEBUG
            using System.Text;
            #endif
            using System.Linq;
            using System.Threading.Tasks;
            using System.IO;
            class Program { static void Main ( string [ ] args ) { var a = File . OpenRead ( "" ) ; } }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddUsingInDirective4(TestHost testHost)
        => TestAsync(
            """
            #define DEBUG
            #if DEBUG
            using System;
            #endif
            using System.Collections.Generic;
            using System.Text;
            using System.Linq;
            using System.Threading.Tasks;
            class Program { static void Main ( string [ ] args ) { var a = [|File|] . OpenRead ( "" ) ; } }
            """,
            """
            #define DEBUG
            #if DEBUG
            using System;
            #endif
            using System.Collections.Generic;
            using System.Text;
            using System.Linq;
            using System.Threading.Tasks;
            using System.IO;
            class Program { static void Main ( string [ ] args ) { var a = File . OpenRead ( "" ) ; } }
            """, testHost);

    [Fact]
    public Task TestInaccessibleExtensionMethod()
        => TestMissingInRegularAndScriptAsync("""
            namespace N1
            {
                public static class C
                {
                    private static bool ExtMethod1(this string arg1)
                    {
                        return true;
                    }
                }
            }

            namespace N2
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        var x = "str1".[|ExtMethod1()|];
                    }
                }
            }
            """);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1116011")]
    public Task TestAddUsingForProperty(TestHost testHost)
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                public BindingFlags BindingFlags
                {
                    get
                    {
                        return BindingFlags.[|Instance|];
                    }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Reflection;
            using System.Threading.Tasks;

            class Program
            {
                public BindingFlags BindingFlags
                {
                    get
                    {
                        return BindingFlags.Instance;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1116011")]
    public Task TestAddUsingForField(TestHost testHost)
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                public B B
                {
                    get
                    {
                        return B.[|Instance|];
                    }
                }
            }

            namespace A
            {
                public class B
                {
                    public static readonly B Instance;
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using A;

            class Program
            {
                public B B
                {
                    get
                    {
                        return B.Instance;
                    }
                }
            }

            namespace A
            {
                public class B
                {
                    public static readonly B Instance;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/1893")]
    public Task TestNameSimplification(TestHost testHost)
        => TestAsync(
            """
            namespace A.B
            {
                class T1
                {
                }
            }

            namespace A.C
            {
                using System;

                class T2
                {
                    void Test()
                    {
                        Console.WriteLine();
                        [|T1|] t1;
                    }
                }
            }
            """,
            """
            namespace A.B
            {
                class T1
                {
                }
            }

            namespace A.C
            {
                using System;
                using A.B;

                class T2
                {
                    void Test()
                    {
                        Console.WriteLine();
                        T1 t1;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/935")]
    public Task TestAddUsingWithOtherExtensionsInScope(TestHost testHost)
        => TestAsync(
            """
            using System.Linq;
            using System.Collections;
            using X;

            namespace X
            {
                public static class Ext
                {
                    public static void ExtMethod(this int a)
                    {
                    }
                }
            }

            namespace Y
            {
                public static class Ext
                {
                    public static void ExtMethod(this int a, int v)
                    {
                    }
                }
            }

            public class B
            {
                static void Main()
                {
                    var b = 0;
                    b.[|ExtMethod|](0);
                }
            }
            """,
            """
            using System.Linq;
            using System.Collections;
            using X;
            using Y;

            namespace X
            {
                public static class Ext
                {
                    public static void ExtMethod(this int a)
                    {
                    }
                }
            }

            namespace Y
            {
                public static class Ext
                {
                    public static void ExtMethod(this int a, int v)
                    {
                    }
                }
            }

            public class B
            {
                static void Main()
                {
                    var b = 0;
                    b.ExtMethod(0);
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/935")]
    public Task TestAddUsingWithOtherExtensionsInScope2(TestHost testHost)
        => TestAsync(
            """
            using System.Linq;
            using System.Collections;
            using X;

            namespace X
            {
                public static class Ext
                {
                    public static void ExtMethod(this int? a)
                    {
                    }
                }
            }

            namespace Y
            {
                public static class Ext
                {
                    public static void ExtMethod(this int? a, int v)
                    {
                    }
                }
            }

            public class B
            {
                static void Main()
                {
                    var b = new int?();
                    b?[|.ExtMethod|](0);
                }
            }
            """,
            """
            using System.Linq;
            using System.Collections;
            using X;
            using Y;

            namespace X
            {
                public static class Ext
                {
                    public static void ExtMethod(this int? a)
                    {
                    }
                }
            }

            namespace Y
            {
                public static class Ext
                {
                    public static void ExtMethod(this int? a, int v)
                    {
                    }
                }
            }

            public class B
            {
                static void Main()
                {
                    var b = new int?();
                    b?.ExtMethod(0);
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/562")]
    public Task TestAddUsingWithOtherExtensionsInScope3(TestHost testHost)
        => TestAsync(
            """
            using System.Linq;

            class C
            {
                int i = 0.[|All|]();
            }

            namespace X
            {
                static class E
                {
                    public static int All(this int o) => 0;
                }
            }
            """,
            """
            using System.Linq;
            using X;

            class C
            {
                int i = 0.All();
            }

            namespace X
            {
                static class E
                {
                    public static int All(this int o) => 0;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/562")]
    public Task TestAddUsingWithOtherExtensionsInScope4(TestHost testHost)
        => TestAsync(
            """
            using System.Linq;

            class C
            {
                static void Main(string[] args)
                {
                    var a = new int?();
                    int? i = a?[|.All|]();
                }
            }

            namespace X
            {
                static class E
                {
                    public static int? All(this int? o) => 0;
                }
            }
            """,
            """
            using System.Linq;
            using X;

            class C
            {
                static void Main(string[] args)
                {
                    var a = new int?();
                    int? i = a?.All();
                }
            }

            namespace X
            {
                static class E
                {
                    public static int? All(this int? o) => 0;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/3080")]
    public Task TestNestedNamespaceSimplified(TestHost testHost)
        => TestAsync(
            """
            namespace Microsoft.MyApp
            {
                using Win32;

                class Program
                {
                    static void Main(string[] args)
                    {
                        [|SafeRegistryHandle|] h;
                    }
                }
            }
            """,
            """
            namespace Microsoft.MyApp
            {
                using Microsoft.Win32.SafeHandles;
                using Win32;

                class Program
                {
                    static void Main(string[] args)
                    {
                        SafeRegistryHandle h;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/3080")]
    public Task TestNestedNamespaceSimplified2(TestHost testHost)
        => TestAsync(
            """
            namespace Microsoft.MyApp
            {
                using Zin32;

                class Program
                {
                    static void Main(string[] args)
                    {
                        [|SafeRegistryHandle|] h;
                    }
                }
            }
            """,
            """
            namespace Microsoft.MyApp
            {
                using Microsoft.Win32.SafeHandles;
                using Zin32;

                class Program
                {
                    static void Main(string[] args)
                    {
                        SafeRegistryHandle h;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/3080")]
    public Task TestNestedNamespaceSimplified3(TestHost testHost)
        => TestAsync(
            """
            namespace Microsoft.MyApp
            {
                using System;
                using Win32;

                class Program
                {
                    static void Main(string[] args)
                    {
                        [|SafeRegistryHandle|] h;
                    }
                }
            }
            """,
            """
            namespace Microsoft.MyApp
            {
                using System;
                using Microsoft.Win32.SafeHandles;
                using Win32;

                class Program
                {
                    static void Main(string[] args)
                    {
                        SafeRegistryHandle h;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/3080")]
    public Task TestNestedNamespaceSimplified4(TestHost testHost)
        => TestAsync(
            """
            namespace Microsoft.MyApp
            {
                using System;
                using Zin32;

                class Program
                {
                    static void Main(string[] args)
                    {
                        [|SafeRegistryHandle|] h;
                    }
                }
            }
            """,
            """
            namespace Microsoft.MyApp
            {
                using System;
                using Microsoft.Win32.SafeHandles;
                using Zin32;

                class Program
                {
                    static void Main(string[] args)
                    {
                        SafeRegistryHandle h;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/3080")]
    public Task TestNestedNamespaceSimplified5(TestHost testHost)
        => TestAsync(
            """
            namespace Microsoft.MyApp
            {
            #if true
                using Win32;
            #else
                using System;
            #endif
                class Program
                {
                    static void Main(string[] args)
                    {
                        [|SafeRegistryHandle|] h;
                    }
                }
            }
            """,
            """
            namespace Microsoft.MyApp
            {
                using Microsoft.Win32.SafeHandles;
            #if true
                using Win32;
            #else
                using System;
            #endif
                class Program
                {
                    static void Main(string[] args)
                    {
                        SafeRegistryHandle h;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/3080")]
    public Task TestNestedNamespaceSimplified6(TestHost testHost)
        => TestAsync(
            """
            namespace Microsoft.MyApp
            {
                using System;
            #if false
                using Win32;
            #endif
                using Win32;

                class Program
                {
                    static void Main(string[] args)
                    {
                        [|SafeRegistryHandle|] h;
                    }
                }
            }
            """,
            """
            namespace Microsoft.MyApp
            {
                using System;
                using Microsoft.Win32.SafeHandles;
            #if false
                using Win32;
            #endif
                using Win32;

                class Program
                {
                    static void Main(string[] args)
                    {
                        SafeRegistryHandle h;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddUsingOrdinalUppercase(TestHost testHost)
        => TestAsync(
            """
            namespace A
            {
                class A
                {
                    static void Main(string[] args)
                    {
                        var b = new [|B|]();
                    }
                }
            }

            namespace lowercase
            {
                class b
                {
                }
            }

            namespace Uppercase
            {
                class B
                {
                }
            }
            """,
            """
            using Uppercase;

            namespace A
            {
                class A
                {
                    static void Main(string[] args)
                    {
                        var b = new B();
                    }
                }
            }

            namespace lowercase
            {
                class b
                {
                }
            }

            namespace Uppercase
            {
                class B
                {
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddUsingOrdinalLowercase(TestHost testHost)
        => TestAsync(
            """
            namespace A
            {
                class A
                {
                    static void Main(string[] args)
                    {
                        var a = new [|b|]();
                    }
                }
            }

            namespace lowercase
            {
                class b
                {
                }
            }

            namespace Uppercase
            {
                class B
                {
                }
            }
            """,
            """
            using lowercase;

            namespace A
            {
                class A
                {
                    static void Main(string[] args)
                    {
                        var a = new b();
                    }
                }
            }

            namespace lowercase
            {
                class b
                {
                }
            }

            namespace Uppercase
            {
                class B
                {
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/7443")]
    public Task TestWithExistingIncompatibleExtension(TestHost testHost)
        => TestAsync(
            """
            using N;

            class C
            {
                int x()
                {
                    System.Collections.Generic.IEnumerable<int> x = null;
                    return x.[|Any|]
                }
            }

            namespace N
            {
                static class Extensions
                {
                    public static void Any(this string s)
                    {
                    }
                }
            }
            """,
            """
            using System.Linq;
            using N;

            class C
            {
                int x()
                {
                    System.Collections.Generic.IEnumerable<int> x = null;
                    return x.Any
                }
            }

            namespace N
            {
                static class Extensions
                {
                    public static void Any(this string s)
                    {
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem(1744, @"https://github.com/dotnet/roslyn/issues/1744")]
    public Task TestIncompleteCatchBlockInLambda(TestHost testHost)
        => TestAsync(
            """
            class A
            {
                System.Action a = () => {
                try
                {
                }
                catch ([|Exception|]
            """,
            """
            using System;

            class A
            {
                System.Action a = () => {
                try
                {
                }
                catch (Exception
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1033612")]
    public Task TestAddInsideLambda(TestHost testHost)
        => TestAsync("""
            using System;

            static void Main(string[] args)
            {
                Func<int> f = () => { [|List<int>|]. }
            }
            """, """
            using System;
            using System.Collections.Generic;

            static void Main(string[] args)
            {
                Func<int> f = () => { List<int>. }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1033612")]
    public Task TestAddInsideLambda2(TestHost testHost)
        => TestAsync("""
            using System;

            static void Main(string[] args)
            {
                Func<int> f = () => { [|List<int>|] }
            }
            """, """
            using System;
            using System.Collections.Generic;

            static void Main(string[] args)
            {
                Func<int> f = () => { List<int> }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1033612")]
    public Task TestAddInsideLambda3(TestHost testHost)
        => TestAsync("""
            using System;

            static void Main(string[] args)
            {
                Func<int> f = () => { 
                    var a = 3;
                    [|List<int>|].
                    return a;
                    };
            }
            """, """
            using System;
            using System.Collections.Generic;

            static void Main(string[] args)
            {
                Func<int> f = () => { 
                    var a = 3;
                    List<int>.
                    return a;
                    };
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1033612")]
    public Task TestAddInsideLambda4(TestHost testHost)
        => TestAsync("""
            using System;

            static void Main(string[] args)
            {
                Func<int> f = () => { 
                    var a = 3;
                    [|List<int>|]
                    return a;
                    };
            }
            """, """
            using System;
            using System.Collections.Generic;

            static void Main(string[] args)
            {
                Func<int> f = () => { 
                    var a = 3;
                    List<int>
                    return a;
                    };
            }
            """, testHost);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860648")]
    [CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/902014")]
    public Task TestIncompleteParenthesizedLambdaExpression(TestHost testHost)
        => TestAsync(
            """
            using System;

            class Test
            {
                void Goo()
                {
                    Action a = () => {
                        [|IBindCtx|] };
                    string a;
                }
            }
            """,
            """
            using System;
            using System.Runtime.InteropServices.ComTypes;

            class Test
            {
                void Goo()
                {
                    Action a = () => {
                        IBindCtx };
                    string a;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/7461")]
    public Task TestExtensionWithIncompatibleInstance(TestHost testHost)
        => TestAsync(
            """
            using System.IO;

            namespace Namespace1
            {
                static class StreamExtensions
                {
                    public static void Write(this Stream stream, byte[] bytes)
                    {
                    }
                }
            }

            namespace Namespace2
            {
                class Goo
                {
                    void Bar()
                    {
                        Stream stream = null;
                        stream.[|Write|](new byte[] { 1, 2, 3 });
                    }
                }
            }
            """,
            """
            using System.IO;
            using Namespace1;

            namespace Namespace1
            {
                static class StreamExtensions
                {
                    public static void Write(this Stream stream, byte[] bytes)
                    {
                    }
                }
            }

            namespace Namespace2
            {
                class Goo
                {
                    void Bar()
                    {
                        Stream stream = null;
                        stream.Write(new byte[] { 1, 2, 3 });
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/5499")]
    public Task TestFormattingForNamespaceUsings(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;

                class Program
                {
                    void Main()
                    {
                        [|Task<int>|]
                    }
                }
            }
            """,
            """
            namespace N
            {
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;

                class Program
                {
                    void Main()
                    {
                        Task<int>
                    }
                }
            }
            """, testHost);

    [Fact]
    public Task TestGenericAmbiguityInSameNamespace()
        => TestMissingInRegularAndScriptAsync(
            """
            namespace NS
            {
                class C<T> where T : [|C|].N
                {
                    public class N
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestNotOnVar1()
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
            """);

    [Fact]
    public Task TestNotOnVar2()
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
            """);

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=226826")]
    public Task TestAddUsingWithLeadingDocCommentInFrontOfUsing1(TestHost testHost)
        => TestAsync(
            """
            /// Copyright 2016 - MyCompany 
            /// All Rights Reserved 

            using System;

            class C : [|IEnumerable|]<int>
            {
            }
            """,
            """
            /// Copyright 2016 - MyCompany 
            /// All Rights Reserved 

            using System;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=226826")]
    public Task TestAddUsingWithLeadingDocCommentInFrontOfUsing2(TestHost testHost)
        => TestAsync(
            """
            /// Copyright 2016 - MyCompany 
            /// All Rights Reserved 

            using System.Collections;

            class C
            {
                [|DateTime|] d;
            }
            """,
            """
            /// Copyright 2016 - MyCompany 
            /// All Rights Reserved 

            using System;
            using System.Collections;

            class C
            {
                DateTime d;
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=226826")]
    public Task TestAddUsingWithLeadingDocCommentInFrontOfClass1(TestHost testHost)
        => TestAsync(
            """
            /// Copyright 2016 - MyCompany 
            /// All Rights Reserved 
            class C
            {
                [|DateTime|] d;
            }
            """,
            """
            using System;

            /// Copyright 2016 - MyCompany 
            /// All Rights Reserved 
            class C
            {
                DateTime d;
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestPlaceUsingWithUsings_NotWithAliases(TestHost testHost)
        => TestAsync(
            """
            using System;

            namespace N
            {
                using C = System.Collections;

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
            using System;
            using System.Collections.Generic;

            namespace N
            {
                using C = System.Collections;

                class Class
                {
                    List<int> Method()
                    {
                        Goo();
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/15025")]
    public Task TestPreferSystemNamespaceFirst(TestHost testHost)
        => TestAsync(
            """
            namespace Microsoft
            {
                public class SomeClass { }
            }

            namespace System
            {
                public class SomeClass { }
            }

            namespace N
            {
                class Class
                {
                    [|SomeClass|] c;
                }
            }
            """,
            """
            using System;

            namespace Microsoft
            {
                public class SomeClass { }
            }

            namespace System
            {
                public class SomeClass { }
            }

            namespace N
            {
                class Class
                {
                    SomeClass c;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/15025")]
    public Task TestPreferSystemNamespaceFirst2(TestHost testHost)
        => TestAsync(
            """
            namespace Microsoft
            {
                public class SomeClass { }
            }

            namespace System
            {
                public class SomeClass { }
            }

            namespace N
            {
                class Class
                {
                    [|SomeClass|] c;
                }
            }
            """,
            """
            using Microsoft;

            namespace Microsoft
            {
                public class SomeClass { }
            }

            namespace System
            {
                public class SomeClass { }
            }

            namespace N
            {
                class Class
                {
                    SomeClass c;
                }
            }
            """, testHost, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18275")]
    public Task TestContextualKeyword1()
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
            """);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/19218")]
    public Task TestChangeCaseWithUsingsInNestedNamespace(TestHost testHost)
        => TestAsync(
            """
            namespace VS
            {
                interface IVsStatusbar
                {
                }
            }

            namespace Outer
            {
                using System;

                class C
                {
                    void M()
                    {
                        // Note: IVsStatusBar is cased incorrectly.
                        [|IVsStatusBar|] b;
                    }
                }
            }
            """,
            """
            namespace VS
            {
                interface IVsStatusbar
                {
                }
            }

            namespace Outer
            {
                using System;
                using VS;

                class C
                {
                    void M()
                    {
                        // Note: IVsStatusBar is cased incorrectly.
                        IVsStatusbar b;
                    }
                }
            }
            """, testHost);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19575")]
    public async Task TestNoNonGenericsWithGenericCodeParsedAsExpression()
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
        await TestActionCountAsync(code, count: 1);

        await TestInRegularAndScriptAsync(
code,
"""
using System.Collections.Generic;

class C
{
    private void GetEvaluationRuleNames()
    {
        IEnumerable < Int32 >
        return ImmutableArray.CreateRange();
    }
}
""");
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/19796")]
    public Task TestWhenInRome1(TestHost testHost)
        => TestAsync(
            """
            using B;
            using System;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using A;
            using B;
            using System;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/19796")]
    public Task TestWhenInRome2(TestHost testHost)
        => TestAsync(
            """
            using System;
            using B;

            class Class
            {
                void Method()
                {
                    [|Goo|].Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """,
            """
            using System;
            using A;
            using B;

            class Class
            {
                void Method()
                {
                    Goo.Bar();
                }
            }

            namespace A
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }
            }
            """, testHost);

    [Fact]
    public Task TestExactMatchNoGlyph()
        => TestSmartTagGlyphTagsAsync(
            """
            namespace VS
            {
                interface Other
                {
                }
            }

            class C
            {
                void M()
                {
                    [|Other|] b;
                }
            }
            """, []);

    [Fact]
    public Task TestFuzzyMatchGlyph()
        => TestSmartTagGlyphTagsAsync(
            """
            namespace VS
            {
                interface Other
                {
                }
            }

            class C
            {
                void M()
                {
                    [|Otter|] b;
                }
            }
            """, WellKnownTagArrays.Namespace);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/29313")]
    public Task TestGetAwaiterExtensionMethod1(TestHost testHost)
        => TestAsync(
            """
            namespace A
            {
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                class C
                {
                    async Task M() => await [|Goo|];

                    C Goo { get; set; }
                }
            }

            namespace B
            {
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;
                using A;

                static class Extensions
                {
                    public static Awaiter GetAwaiter(this C scheduler) => null;

                    public class Awaiter : INotifyCompletion
                    {
                        public object GetResult() => null;

                        public void OnCompleted(Action continuation) { }

                        public bool IsCompleted => true;
                    }
                }
            }
            """,
            """
            namespace A
            {
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;
                using B;

                class C
                {
                    async Task M() => await Goo;

                    C Goo { get; set; }
                }
            }

            namespace B
            {
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;
                using A;

                static class Extensions
                {
                    public static Awaiter GetAwaiter(this C scheduler) => null;

                    public class Awaiter : INotifyCompletion
                    {
                        public object GetResult() => null;

                        public void OnCompleted(Action continuation) { }

                        public bool IsCompleted => true;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/29313")]
    public Task TestGetAwaiterExtensionMethod2(TestHost testHost)
        => TestAsync(
            """
            namespace A
            {
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                class C
                {
                    async Task M() => await [|GetC|]();

                    C GetC() => null;
                }
            }

            namespace B
            {
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;
                using A;

                static class Extensions
                {
                    public static Awaiter GetAwaiter(this C scheduler) => null;

                    public class Awaiter : INotifyCompletion
                    {
                        public object GetResult() => null;

                        public void OnCompleted(Action continuation) { }

                        public bool IsCompleted => true;
                    }
                }
            }
            """,
            """
            namespace A
            {
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;
                using B;

                class C
                {
                    async Task M() => await GetC();

                    C GetC() => null;
                }
            }

            namespace B
            {
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;
                using A;

                static class Extensions
                {
                    public static Awaiter GetAwaiter(this C scheduler) => null;

                    public class Awaiter : INotifyCompletion
                    {
                        public object GetResult() => null;

                        public void OnCompleted(Action continuation) { }

                        public bool IsCompleted => true;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/745490")]
    public Task TestAddUsingForAwaitableReturningExtensionMethod(TestHost testHost)
        => TestAsync(
            """
            namespace A
            {
                using System;
                using System.Threading.Tasks;

                class C
                {
                    C Instance { get; }

                    async Task M() => await Instance.[|Foo|]();
                }
            }

            namespace B
            {
                using System;
                using System.Threading.Tasks;
                using A;

                static class Extensions
                {
                    public static Task Foo(this C instance) => null;
                }
            }
            """,
            """
            namespace A
            {
                using System;
                using System.Threading.Tasks;
                using B;

                class C
                {
                    C Instance { get; }

                    async Task M() => await Instance.Foo();
                }
            }

            namespace B
            {
                using System;
                using System.Threading.Tasks;
                using A;

                static class Extensions
                {
                    public static Task Foo(this C instance) => null;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddUsingForExtensionGetEnumeratorReturningIEnumerator(TestHost testHost)
        => TestAsync(
            """
            namespace A
            {
                class C
                {
                    C Instance { get; }

                    void M() { foreach (var i in [|Instance|]); }
                }
            }

            namespace B
            {
                using A;
                using System.Collections.Generic;

                static class Extensions
                {
                    public static IEnumerator<int> GetEnumerator(this C instance) => null;
                }
            }
            """,
            """
            using B;

            namespace A
            {
                class C
                {
                    C Instance { get; }

                    void M() { foreach (var i in Instance); }
                }
            }

            namespace B
            {
                using A;
                using System.Collections.Generic;

                static class Extensions
                {
                    public static IEnumerator<int> GetEnumerator(this C instance) => null;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    public Task TestAddUsingForExtensionGetEnumeratorReturningPatternEnumerator(TestHost testHost)
        => TestAsync(
            """
            namespace A
            {
                class C
                {
                    C Instance { get; }

                    void M() { foreach (var i in [|Instance|]); }
                }
            }

            namespace B
            {
                using A;

                static class Extensions
                {
                    public static Enumerator GetEnumerator(this C instance) => null;
                }

                public class Enumerator
                {
                    public int Current { get; }
                    public bool MoveNext();
                }
            }
            """,
            """
            using B;

            namespace A
            {
                class C
                {
                    C Instance { get; }

                    void M() { foreach (var i in Instance); }
                }
            }

            namespace B
            {
                using A;

                static class Extensions
                {
                    public static Enumerator GetEnumerator(this C instance) => null;
                }

                public class Enumerator
                {
                    public int Current { get; }
                    public bool MoveNext();
                }
            }
            """, testHost);

    [Fact]
    public Task TestMissingForExtensionInvalidGetEnumerator()
        => TestMissingAsync(
            """
            namespace A
            {
                class C
                {
                    C Instance { get; }

                    void M() { foreach (var i in [|Instance|]); }
                }
            }

            namespace B
            {
                using A;

                static class Extensions
                {
                    public static bool GetEnumerator(this C instance) => null;
                }
            }
            """);

    [Theory, CombinatorialData]
    public Task TestAddUsingForExtensionGetEnumeratorReturningPatternEnumeratorWrongAsync(TestHost testHost)
        => TestAsync(
            """
            namespace A
            {
                class C
                {
                    C Instance { get; };

                    void M() { foreach (var i in [|Instance|]); }

                    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
                    {
                        return new Enumerator();
                    }
                    public sealed class Enumerator
                    {
                        public async System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
                        public int Current => throw null;
                    }
                }
            }

            namespace B
            {
                using A;

                static class Extensions
                {
                    public static Enumerator GetEnumerator(this C instance) => null;
                }

                public class Enumerator
                {
                    public int Current { get; }
                    public bool MoveNext();
                }
            }
            """,
            """
            using B;

            namespace A
            {
                class C
                {
                    C Instance { get; };

                    void M() { foreach (var i in Instance); }

                    public Enumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
                    {
                        return new Enumerator();
                    }
                    public sealed class Enumerator
                    {
                        public async System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
                        public int Current => throw null;
                    }
                }
            }

            namespace B
            {
                using A;

                static class Extensions
                {
                    public static Enumerator GetEnumerator(this C instance) => null;
                }

                public class Enumerator
                {
                    public int Current { get; }
                    public bool MoveNext();
                }
            }
            """, testHost);

    [Fact]
    public Task TestMissingForExtensionGetAsyncEnumeratorOnForeach()
        => TestMissingAsync(
            """
            namespace A
            {
                class C
                {
                    C Instance { get; }

                    void M() { foreach (var i in [|Instance|]); }
                }
            }

            namespace B
            {
                using A;
                using System.Collections.Generic;

                static class Extensions
                {
                    public static IAsyncEnumerator<int> GetAsyncEnumerator(this C instance) => null;
                }
            }
            """ + IAsyncEnumerable);

    [Theory, CombinatorialData]
    public Task TestAddUsingForExtensionGetAsyncEnumeratorReturningIAsyncEnumerator(TestHost testHost)
        => TestAsync(
            """
            using System.Threading.Tasks;
            namespace A
            {
                class C
                {
                    C Instance { get; }

                    async Task M() { await foreach (var i in [|Instance|]); }
                }
            }

            namespace B
            {
                using A;
                using System.Collections.Generic;

                static class Extensions
                {
                    public static IAsyncEnumerator<int> GetAsyncEnumerator(this C instance) => null;
                }
            }
            """ + IAsyncEnumerable,
            """
            using System.Threading.Tasks;
            using B;
            namespace A
            {
                class C
                {
                    C Instance { get; }

                    async Task M() { await foreach (var i in Instance); }
                }
            }

            namespace B
            {
                using A;
                using System.Collections.Generic;

                static class Extensions
                {
                    public static IAsyncEnumerator<int> GetAsyncEnumerator(this C instance) => null;
                }
            }
            """ + IAsyncEnumerable, testHost);

    [Theory, CombinatorialData]
    public Task TestAddUsingForExtensionGetAsyncEnumeratorReturningPatternEnumerator(TestHost testHost)
        => TestAsync(
            """
            using System.Threading.Tasks;
            namespace A
            {
                class C
                {
                    C Instance { get; }

                    async Task M() { await foreach (var i in [|Instance|]); }
                }
            }

            namespace B
            {
                using A;

                static class Extensions
                {
                    public static Enumerator GetAsyncEnumerator(this C instance) => null;
                }

                public class Enumerator
                {
                    public int Current { get; }
                    public Task<bool> MoveNextAsync();
                }
            }
            """,
            """
            using System.Threading.Tasks;
            using B;
            namespace A
            {
                class C
                {
                    C Instance { get; }

                    async Task M() { await foreach (var i in Instance); }
                }
            }

            namespace B
            {
                using A;

                static class Extensions
                {
                    public static Enumerator GetAsyncEnumerator(this C instance) => null;
                }

                public class Enumerator
                {
                    public int Current { get; }
                    public Task<bool> MoveNextAsync();
                }
            }
            """, testHost);

    [Fact]
    public Task TestMissingForExtensionInvalidGetAsyncEnumerator()
        => TestMissingAsync(
            """
            using System.Threading.Tasks;

            namespace A
            {
                class C
                {
                    C Instance { get; }

                    async Task M() { await foreach (var i in [|Instance|]); }
                }
            }

            namespace B
            {
                using A;

                static class Extensions
                {
                    public static bool GetAsyncEnumerator(this C instance) => null;
                }
            }
            """);

    [Theory, CombinatorialData]
    public Task TestAddUsingForExtensionGetAsyncEnumeratorReturningPatternEnumeratorWrongAsync(TestHost testHost)
        => TestAsync(
            """
            using System.Threading.Tasks;
            namespace A
            {
                class C
                {
                    C Instance { get; }

                    Task M() { await foreach (var i in [|Instance|]); }

                    public Enumerator GetEnumerator()
                    {
                        return new Enumerator();
                    }

                    public class Enumerator
                    {
                        public int Current { get; }
                        public bool MoveNext();
                    }
                }
            }

            namespace B
            {
                using A;

                static class Extensions
                {
                    public static Enumerator GetAsyncEnumerator(this C instance) => null;
                }

                public sealed class Enumerator
                {
                    public async System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
                    public int Current => throw null;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            using B;
            namespace A
            {
                class C
                {
                    C Instance { get; }

                    Task M() { await foreach (var i in Instance); }

                    public Enumerator GetEnumerator()
                    {
                        return new Enumerator();
                    }

                    public class Enumerator
                    {
                        public int Current { get; }
                        public bool MoveNext();
                    }
                }
            }

            namespace B
            {
                using A;

                static class Extensions
                {
                    public static Enumerator GetAsyncEnumerator(this C instance) => null;
                }

                public sealed class Enumerator
                {
                    public async System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
                    public int Current => throw null;
                }
            }
            """, testHost);

    [Fact]
    public Task TestMissingForExtensionGetEnumeratorOnAsyncForeach()
        => TestMissingAsync(
            """
            using System.Threading.Tasks;

            namespace A
            {
                class C
                {
                    C Instance { get; }

                    Task M() { await foreach (var i in [|Instance|]); }
                }
            }

            namespace B
            {
                using A;
                using System.Collections.Generic;

                static class Extensions
                {
                    public static IEnumerator<int> GetEnumerator(this C instance) => null;
                }
            }
            """);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithStaticUsingInNamespace_WhenNoExistingUsings(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
                using static System.Math;

                class C
                {
                    public [|List<int>|] F;
                }
            }
            """,
            """
            namespace N
            {
                using System.Collections.Generic;
                using static System.Math;

                class C
                {
                    public List<int> F;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithStaticUsingInInnerNestedNamespace_WhenNoExistingUsings(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
                namespace M
                {
                    using static System.Math;

                    class C
                    {
                        public [|List<int>|] F;
                    }
                }
            }
            """,
            """
            namespace N
            {
                namespace M
                {
                    using System.Collections.Generic;
                    using static System.Math;

                    class C
                    {
                        public List<int> F;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithStaticUsingInOuterNestedNamespace_WhenNoExistingUsings(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
                using static System.Math;

                namespace M
                {
                    class C
                    {
                        public [|List<int>|] F;
                    }
                }
            }
            """,
            """
            namespace N
            {
                using System.Collections.Generic;
                using static System.Math;

                namespace M
                {
                    class C
                    {
                        public List<int> F;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithExistingUsingInCompilationUnit_WhenStaticUsingInNamespace(TestHost testHost)
        => TestAsync(
            """
            using System;

            namespace N
            {
                using static System.Math;

                class C
                {
                    public [|List<int>|] F;
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            namespace N
            {
                using static System.Math;

                class C
                {
                    public List<int> F;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithExistingUsing_WhenStaticUsingInInnerNestedNamespace(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
                using System;

                namespace M
                {
                    using static System.Math;

                    class C
                    {
                        public [|List<int>|] F;
                    }
                }
            }
            """,
            """
            namespace N
            {
                using System;
                using System.Collections.Generic;

                namespace M
                {
                    using static System.Math;

                    class C
                    {
                        public List<int> F;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithExistingUsing_WhenStaticUsingInOuterNestedNamespace(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
                using static System.Math;

                namespace M
                {
                    using System;

                    class C
                    {
                        public [|List<int>|] F;
                    }
                }
            }
            """,
            """
            namespace N
            {
                using static System.Math;

                namespace M
                {
                    using System;
                    using System.Collections.Generic;

                    class C
                    {
                        public List<int> F;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithUsingAliasInNamespace_WhenNoExistingUsing(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
                using SAction = System.Action;

                class C
                {
                    public [|List<int>|] F;
                }
            }
            """,
            """
            namespace N
            {
                using System.Collections.Generic;
                using SAction = System.Action;

                class C
                {
                    public List<int> F;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithUsingAliasInInnerNestedNamespace_WhenNoExistingUsing(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
                namespace M
                {
                    using SAction = System.Action;

                    class C
                    {
                        public [|List<int>|] F;
                    }
                }
            }
            """,
            """
            namespace N
            {
                namespace M
                {
                    using System.Collections.Generic;
                    using SAction = System.Action;

                    class C
                    {
                        public List<int> F;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithUsingAliasInOuterNestedNamespace_WhenNoExistingUsing(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
                using SAction = System.Action;

                namespace M
                {
                    class C
                    {
                        public [|List<int>|] F;
                    }
                }
            }
            """,
            """
            namespace N
            {
                using System.Collections.Generic;
                using SAction = System.Action;

                namespace M
                {
                    class C
                    {
                        public List<int> F;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithExistingUsingInCompilationUnit_WhenUsingAliasInNamespace(TestHost testHost)
        => TestAsync(
            """
            using System;

            namespace N
            {
                using SAction = System.Action;

                class C
                {
                    public [|List<int>|] F;
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            namespace N
            {
                using SAction = System.Action;

                class C
                {
                    public List<int> F;
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithExistingUsing_WhenUsingAliasInInnerNestedNamespace(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
                using System;

                namespace M
                {
                    using SAction = System.Action;

                    class C
                    {
                        public [|List<int>|] F;
                    }
                }
            }
            """,
            """
            namespace N
            {
                using System;
                using System.Collections.Generic;

                namespace M
                {
                    using SAction = System.Action;

                    class C
                    {
                        public [|List<int>|] F;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30734")]
    public Task UsingPlacedWithExistingUsing_WhenUsingAliasInOuterNestedNamespace(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
                using SAction = System.Action;

                namespace M
                {
                    using System;

                    class C
                    {
                        public [|List<int>|] F;
                    }
                }
            }
            """,
            """
            namespace N
            {
                using SAction = System.Action;

                namespace M
                {
                    using System;
                    using System.Collections.Generic;

                    class C
                    {
                        public [|List<int>|] F;
                    }
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25003")]
    public Task KeepUsingsGrouped1(TestHost testHost)
        => TestAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    [|Goo|]
                }
            }

            namespace Microsoft
            {
                public class Goo
                {
                }
            }
            """,
            """
            using System;
            using Microsoft;

            class Program
            {
                static void Main(string[] args)
                {
                    Goo
                }
            }

            namespace Microsoft
            {
                public class Goo
                {
                }
            }
            """, testHost);

    [Fact, WorkItem(1239, @"https://github.com/dotnet/roslyn/issues/1239")]
    public Task TestIncompleteLambda1()
        => TestInRegularAndScriptAsync(
            """
            using System.Linq;

            class C
            {
                C()
                {
                    "".Select(() => {
                    new [|Byte|]
            """,
            """
            using System;
            using System.Linq;

            class C
            {
                C()
                {
                    "".Select(() => {
                    new Byte
            """);

    [Fact, WorkItem(1239, @"https://github.com/dotnet/roslyn/issues/1239")]
    public Task TestIncompleteLambda2()
        => TestInRegularAndScriptAsync(
            """
            using System.Linq;

            class C
            {
                C()
                {
                    "".Select(() => {
                        new [|Byte|]() }
            """,
            """
            using System;
            using System.Linq;

            class C
            {
                C()
                {
                    "".Select(() => {
                        new Byte() }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/902014")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860648")]
    public Task TestIncompleteSimpleLambdaExpression()
        => TestInRegularAndScriptAsync(
            """
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    args[0].Any(x => [|IBindCtx|]
                    string a;
                }
            }
            """,
            """
            using System.Linq;
            using System.Runtime.InteropServices.ComTypes;

            class Program
            {
                static void Main(string[] args)
                {
                    args[0].Any(x => IBindCtx
                    string a;
                }
            }
            """);

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1266354")]
    public Task TestAddUsingsEditorBrowsableNeverSameProject(TestHost testHost)
        => TestAsync("""
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
            using ProjectLib;

            class Program
            {
                static void Main(string[] args)
                {
                    Project p = new [|Project()|];
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1266354")]
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

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1266354")]
    public Task TestAddUsingsEditorBrowsableAdvancedDifferentProjectOptionOn(TestHost testHost)
        => TestAsync("""
            <Workspace>
                <Project Language="Visual Basic" AssemblyName="lib" CommonReferences="true">
                    <Document FilePath="lib.vb">imports System.ComponentModel
            namespace ProjectLib
                &lt;EditorBrowsable(EditorBrowsableState.Advanced)&gt;
                public class Project
                end class
            end namespace</Document>
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
            using ProjectLib;

            class Program
            {
                static void Main(string[] args)
                {
                    Project p = new [|Project()|];
                }
            }
            """, testHost);

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1266354")]
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

    /// <summary>
    /// Note that this test verifies the current end of line sequence in using directives is preserved regardless of
    /// whether this matches the end_of_line value in .editorconfig or not.
    /// </summary>
    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/62976")]
    public Task TestAddUsingPreservesNewlines1(TestHost testHost, [CombinatorialValues("\n", "\r\n")] string sourceNewLine, [CombinatorialValues("\n", "\r\n")] string configuredNewLine)
        => TestInRegularAndScriptAsync(
            """
            namespace ANamespace
            {
                public class TheAType { }
            }

            namespace N
            {
                class Class
                {
                    [|TheAType|] a;
                }
            }
            """.ReplaceLineEndings(sourceNewLine),
            """
            using ANamespace;

            namespace ANamespace
            {
                public class TheAType { }
            }

            namespace N
            {
                class Class
                {
                    TheAType a;
                }
            }
            """.ReplaceLineEndings(sourceNewLine),
            index: 0,
            parameters: new TestParameters(options: Option(FormattingOptions2.NewLine, configuredNewLine), testHost: testHost));

    /// <summary>
    /// Note that this test verifies the current end of line sequence in using directives is preserved regardless of
    /// whether this matches the end_of_line value in .editorconfig or not.
    /// </summary>
    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/62976")]
    public Task TestAddUsingPreservesNewlines2(TestHost testHost, [CombinatorialValues("\n", "\r\n")] string sourceNewLine, [CombinatorialValues("\n", "\r\n")] string configuredNewLine)
        => TestInRegularAndScriptAsync(
            """
            using BNamespace;

            namespace ANamespace
            {
                public class TheAType { }
            }

            namespace BNamespace
            {
                public class TheBType { }
            }

            namespace N
            {
                class Class
                {
                    [|TheAType|] a;
                    TheBType b;
                }
            }
            """.ReplaceLineEndings(sourceNewLine),
            """
            using ANamespace;
            using BNamespace;

            namespace ANamespace
            {
                public class TheAType { }
            }

            namespace BNamespace
            {
                public class TheBType { }
            }

            namespace N
            {
                class Class
                {
                    TheAType a;
                    TheBType b;
                }
            }
            """.ReplaceLineEndings(sourceNewLine),
            index: 0,
            parameters: new TestParameters(options: Option(FormattingOptions2.NewLine, configuredNewLine), testHost: testHost));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/62976")]
    public Task TestAddUsingPreservesNewlines3(TestHost testHost, [CombinatorialValues("\n", "\r\n")] string sourceNewLine, [CombinatorialValues("\n", "\r\n")] string configuredNewLine)
        => TestInRegularAndScriptAsync(
            """
            using ANamespace;

            namespace ANamespace
            {
                public class TheAType { }
            }

            namespace BNamespace
            {
                public class TheBType { }
            }

            namespace N
            {
                class Class
                {
                    TheAType a;
                    [|TheBType|] b;
                }
            }
            """.ReplaceLineEndings(sourceNewLine),
            """
            using ANamespace;
            using BNamespace;

            namespace ANamespace
            {
                public class TheAType { }
            }

            namespace BNamespace
            {
                public class TheBType { }
            }

            namespace N
            {
                class Class
                {
                    TheAType a;
                    TheBType b;
                }
            }
            """.ReplaceLineEndings(sourceNewLine),
            index: 0,
            parameters: new TestParameters(options: Option(FormattingOptions2.NewLine, configuredNewLine), testHost: testHost));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/24642")]
    public Task TestAddUsingWithMalformedGeneric(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|List<Y|] x;
            }
            """,
            """
            using System.Collections.Generic;

            class Class
            {
                List<Y x;
            }
            """,
            index: 0,
            parameters: new TestParameters(testHost: testHost));

    [Theory, CombinatorialData]
    public Task TestOutsideOfMethodWithMalformedGenericParameters(TestHost testHost)
        => TestInRegularAndScriptAsync(
            """
            using System;
            
            class Program
            {
                Func<[|FlowControl|] x
            }
            """,
            """
            using System;
            using System.Reflection.Emit;
            
            class Program
            {
                Func<FlowControl x
            }
            """,
            index: 0,
            parameters: new TestParameters(testHost: testHost));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/72022")]
    public Task TestAssemblyLevelAttribute(TestHost testHost)
        => TestAsync(
            """
            [assembly: [|NeutralResourcesLanguage|]("en")]
            """,
            """
            using System.Resources;

            [assembly: NeutralResourcesLanguage("en")]
            """, testHost);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/79462")]
    public Task TestAddUsingsWithSourceGeneratedFile(TestHost testHost)
        => TestAsync("""
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
            using Win32;
            
            Something a;
            PInvoke.GetMessage();
            
            namespace Goo
            {
                class Something { }
            }
            """, testHost);
}
