// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCollectionInitializer
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUseCollectionInitializerDiagnosticAnalyzer,
        CSharpUseCollectionInitializerCodeFixProvider>;

    public partial class UseCollectionInitializerTests
    {
        private static async Task TestInRegularAndScriptAsync(string testCode, string fixedCode)
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = Testing.ReferenceAssemblies.NetCore.NetCoreApp31,
                TestCode = testCode,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.Preview,
            }.RunAsync();
        }

        private static async Task TestMissingInRegularAndScriptAsync(string testCode, LanguageVersion? languageVersion = null)
        {
            var test = new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
            };

            if (languageVersion != null)
                test.LanguageVersion = languageVersion.Value;

            await test.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestOnVariableDeclarator()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [|new|] List<int>();
        c.Add(1);
    }
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = new List<int>
        {
            1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestIndexAccess1()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [|new|] List<int>();
        c[1] = 2;
    }
}",
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = new List<int>
        {
            [1] = 2
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestIndexAccess1_NotInCSharp5()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = new List<int>();
        c[1] = 2;
    }
}", LanguageVersion.CSharp5);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestComplexIndexAccess1()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;

class A
{
    public B b;
}

class B
{
    public List<int> c;
}

class C
{
    void M(A a)
    {
        a.b.c = [|new|] List<int>();
        a.b.c[1] = 2;
    }
}",
@"
using System.Collections.Generic;

class A
{
    public B b;
}

class B
{
    public List<int> c;
}

class C
{
    void M(A a)
    {
        a.b.c = new List<int>
        {
            [1] = 2
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestIndexAccess2()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [|new|] List<object>();
        c[1] = 2;
        c[2] = """";
    }
}",
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = new List<object>
        {
            [1] = 2,
            [2] = """"
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestIndexAccess3()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections;

class C
{
    void M()
    {
        var c = [|new|] X();
        c[1] = 2;
        c[2] = """";
        c[3, 4] = 5;
    }
}

class X : IEnumerable
{
    public object this[int i] { get => null; set { } }
    public object this[int i, int j] { get => null; set { } }

    public IEnumerator GetEnumerator() => null;
    public void Add(int i) { }
}
",
@"
using System.Collections;

class C
{
    void M()
    {
        var c = new X
        {
            [1] = 2,
            [2] = """",
            [3, 4] = 5
        };
    }
}

class X : IEnumerable
{
    public object this[int i] { get => null; set { } }
    public object this[int i, int j] { get => null; set { } }

    public IEnumerator GetEnumerator() => null;
    public void Add(int i) { }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestIndexFollowedByInvocation()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [|new|] List<int>();
        c[1] = 2;
        c.Add(0);
    }
}",
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = new List<int>
        {
            [1] = 2
        };
        c.Add(0);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestInvocationFollowedByIndex()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [|new|] List<int>();
        c.Add(0);
        c[1] = 2;
    }
}",
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = new List<int>
        {
            0
        };
        c[1] = 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestWithInterimStatement()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [|new|] List<int>();
        c.Add(1);
        c.Add(2);
        throw new System.Exception();
        c.Add(3);
        c.Add(4);
    }
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = new List<int>
        {
            1,
            2
        };
        throw new System.Exception();
        c.Add(3);
        c.Add(4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestMissingBeforeCSharp3()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        List<int> c = new List<int>();
        c.Add(1);
    }
}", LanguageVersion.CSharp2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestMissingOnNonIEnumerable()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = new C();
        c.Add(1);
    }

    void Add(int i) { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestMissingOnNonIEnumerableEvenWithAdd()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = new C();
        c.Add(1);
    }

    public void Add(int i)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestWithCreationArguments()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [|new|] List<int>(1);
        c.Add(1);
    }
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = new List<int>(1)
        {
            1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestOnAssignmentExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        List<int> c = null;
        c = [|new|] List<int>();
        c.Add(1);
    }
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        List<int> c = null;
        c = new List<int>
        {
            1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestMissingOnRefAdd()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M(int i)
    {
        var c = new List();
        c.Add(ref i);
    }
}


class List
{
    public void Add(ref int i)
    {
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestComplexInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M(List<int>[] array)
    {
        array[0] = [|new|] List<int>();
        array[0].Add(1);
        array[0].Add(2);
    }
}",
@"using System.Collections.Generic;

class C
{
    void M(List<int>[] array)
    {
        array[0] = new List<int>
        {
            1,
            2
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestNotOnNamedArg()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = new List<int>();
        c.Add(item: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(39146, "https://github.com/dotnet/roslyn/issues/39146")]
        public async Task TestWithExistingInitializer()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [|new|] List<int>()
        {
            1
        };
        c.Add(1);
    }
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [|new|] List<int>
        {
            1,
            1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(39146, "https://github.com/dotnet/roslyn/issues/39146")]
        public async Task TestWithExistingInitializerWithComma()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [|new|] List<int>()
        {
            1,
        };
        c.Add(1);
    }
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [|new|] List<int>
        {
            1,
            1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestFixAllInDocument1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M(List<int>[] array)
    {
        array[0] = [|new|] List<int>();
        array[0].Add(1);
        array[0].Add(2);
        array[1] = [|new|] List<int>();
        array[1].Add(3);
        array[1].Add(4);
    }
}",
@"using System.Collections.Generic;

class C
{
    void M(List<int>[] array)
    {
        array[0] = new List<int>
        {
            1,
            2
        };
        array[1] = new List<int>
        {
            3,
            4
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestFixAllInDocument2()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
using System.Collections;
using System.Collections.Generic;

class C
{
    void M()
    {
        var list1 = [|new|] Bar(() => {
            var list2 = [|new|] List<int>();
            list2.Add(2);
        });
        list1.Add(1);
    }
}

class Bar : IEnumerable
{
    public Bar(Action action) { }

    public IEnumerator GetEnumerator() => null;
    public void Add(int i) { }
}
",
@"
using System;
using System.Collections;
using System.Collections.Generic;

class C
{
    void M()
    {
        var list1 = new Bar(() =>
        {
            var list2 = new List<int>
            {
                2
            };
        })
        {
            1
        };
    }
}

class Bar : IEnumerable
{
    public Bar(Action action) { }

    public IEnumerator GetEnumerator() => null;
    public void Add(int i) { }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestFixAllInDocument3()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"
using System;
using System.Collections.Generic;

class C
{
    void M()
    {
        var list1 = [|new|] List<Action>();
        list1.Add(() => {
            var list2 = [|new|] List<int>();
            list2.Add(2);
        });
    }
}",
                FixedCode =
@"
using System;
using System.Collections.Generic;

class C
{
    void M()
    {
        var list1 = new List<Action>
        {
            () =>
            {
                var list2 = new List<int> { 2 };
            }
        };
    }
}",
                BatchFixedCode =
@"
using System;
using System.Collections.Generic;

class C
{
    void M()
    {
        var list1 = new List<Action>
        {
            () =>
            {
                var list2 = new List<int>
                {
                    2
                };
            }
        };
    }
}",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [|new|] List<int>();
        c.Add(1); // Goo
        c.Add(2); // Bar
    }
}",
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = new List<int>
        {
            1, // Goo
            2 // Bar
        };
    }
}");
        }

        [WorkItem(46670, "https://github.com/dotnet/roslyn/issues/46670")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestTriviaRemoveLeadingBlankLinesForFirstElement()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [|new|] List<int>();
        
        // Goo
        c.Add(1);

        // Bar
        c.Add(2);
    }
}",
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = new List<int>
        {
            // Goo
            1,

            // Bar
            2
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestComplexInitializer2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [|new|] Dictionary<int, string>();
        c.Add(1, ""x"");
        c.Add(2, ""y"");
    }
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = new Dictionary<int, string>
        {
            { 1, ""x"" },
            { 2, ""y"" }
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(16158, "https://github.com/dotnet/roslyn/issues/16158")]
        public async Task TestIncorrectAddName()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;

public class Goo
{
    public static void Bar()
    {
        string item = null;
        var items = new List<string>();

        var values = [|new|] List<string>(); // Collection initialization can be simplified
        values.Add(item);
        values.AddRange(items);
    }
}",
@"using System.Collections.Generic;

public class Goo
{
    public static void Bar()
    {
        string item = null;
        var items = new List<string>();

        var values = new List<string>
        {
            item
        }; // Collection initialization can be simplified
        values.AddRange(items);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(16241, "https://github.com/dotnet/roslyn/issues/16241")]
        public async Task TestNestedCollectionInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"
        using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var myStringArray = new string[] { ""Test"", ""123"", ""ABC"" };
        var myStringList = myStringArray?.ToList() ?? new List<string>();
        myStringList.Add(""Done"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(17823, "https://github.com/dotnet/roslyn/issues/17823")]
        public async Task TestMissingWhenReferencedInInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System.Collections.Generic;

class C
{
    static void M()
    {
        var items = new List<object>();
        items[0] = items[0];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(17823, "https://github.com/dotnet/roslyn/issues/17823")]
        public async Task TestWhenReferencedInInitializer_LocalVar()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;

class C
{
    static void M()
    {
        var items = [|new|] List<object>();
        items[0] = 1;
        items[1] = items[0];
    }
}",
@"
using System.Collections.Generic;

class C
{
    static void M()
    {
        var items = [|new|] List<object>
        {
            [0] = 1
        };
        items[1] = items[0];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(17823, "https://github.com/dotnet/roslyn/issues/17823")]
        public async Task TestWhenReferencedInInitializer_LocalVar2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System.Collections.Generic;
using System.Linq;

class C
{
    void M()
    {
        var t = new List<int>(new int[] { 1, 2, 3 });
        t.Add(t.Min() - 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(18260, "https://github.com/dotnet/roslyn/issues/18260")]
        public async Task TestWhenReferencedInInitializer_Assignment()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;

class C
{
    static void M()
    {
        List<object> items = null;
        items = [|new|] List<object>();
        items[0] = 1;
        items[1] = items[0];
    }
}",
@"
using System.Collections.Generic;

class C
{
    static void M()
    {
        List<object> items = null;
        items = [|new|] List<object>
        {
            [0] = 1
        };
        items[1] = items[0];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(18260, "https://github.com/dotnet/roslyn/issues/18260")]
        public async Task TestWhenReferencedInInitializer_Assignment2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;
using System.Linq;

class C
{
    void M()
    {
        List<int> t = null;
        t = new List<int>(new int[] { 1, 2, 3 });
        t.Add(t.Min() - 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(18260, "https://github.com/dotnet/roslyn/issues/18260")]
        public async Task TestFieldReference()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Collections.Generic;

class C
{
    private List<int> myField;
    void M()
    {
        myField = new List<int>();
        myField.Add(this.myField.Count);
    }
}");
        }

        [WorkItem(17853, "https://github.com/dotnet/roslyn/issues/17853")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestMissingForDynamic()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Dynamic;

class C
{
    void Goo()
    {
        dynamic body = new ExpandoObject();
        body[0] = new ExpandoObject();
    }
}");
        }

        [WorkItem(17953, "https://github.com/dotnet/roslyn/issues/17953")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestMissingAcrossPreprocessorDirective()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System.Collections.Generic;

public class Goo
{
    public void M()
    {
        var items = new List<object>();
#if true
        items.Add(1);
#endif
    }
}");
        }

        [WorkItem(17953, "https://github.com/dotnet/roslyn/issues/17953")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestAvailableInsidePreprocessorDirective()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;

public class Goo
{
    public void M()
    {
#if true
        var items = [|new|] List<object>();
        items.Add(1);
#endif
    }
}",
@"
using System.Collections.Generic;

public class Goo
{
    public void M()
    {
#if true
        var items = new List<object>
        {
            1
        };
#endif
    }
}");
        }

        [WorkItem(18242, "https://github.com/dotnet/roslyn/issues/18242")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestObjectInitializerAssignmentAmbiguity()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;

public class Goo
{
    public void M()
    {
        int lastItem;
        var list = [|new|] List<int>();
        list.Add(lastItem = 5);
    }
}",
@"
using System.Collections.Generic;

public class Goo
{
    public void M()
    {
        int lastItem;
        var list = new List<int>
        {
            (lastItem = 5)
        };
    }
}");
        }

        [WorkItem(18242, "https://github.com/dotnet/roslyn/issues/18242")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestObjectInitializerCompoundAssignment()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;

public class Goo
{
    public void M()
    {
        int lastItem = 0;
        var list = [|new|] List<int>();
        list.Add(lastItem += 5);
    }
}",
@"
using System.Collections.Generic;

public class Goo
{
    public void M()
    {
        int lastItem = 0;
        var list = new List<int>
        {
            (lastItem += 5)
        };
    }
}");
        }

        [WorkItem(19253, "https://github.com/dotnet/roslyn/issues/19253")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestKeepBlankLinesAfter()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;

class MyClass
{
    public void Main()
    {
        var list = [|new|] List<int>();
        list.Add(1);

        int horse = 1;
    }
}",
@"
using System.Collections.Generic;

class MyClass
{
    public void Main()
    {
        var list = new List<int>
        {
            1
        };

        int horse = 1;
    }
}");
        }

        [WorkItem(23672, "https://github.com/dotnet/roslyn/issues/23672")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestMissingWithExplicitImplementedAddMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"
using System.Collections.Generic;
using System.Dynamic;

public class Goo
{
    public void M()
    {
        IDictionary<string, object> obj = new ExpandoObject();
        obj.Add(""string"", ""v"");
        obj.Add(""int"", 1);
        obj.Add("" object"", new { X = 1, Y = 2 });
        }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(47632, "https://github.com/dotnet/roslyn/issues/47632")]
        public async Task TestWhenReferencedInInitializerLeft()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;

class C
{
    static void M()
    {
        List<object> items = [|new|] List<object>();
        items[0] = 1;
        items[items.Count - 1] = 2;
    }
}",
@"
using System.Collections.Generic;

class C
{
    static void M()
    {
        List<object> items = [|new|] List<object>
        {
            [0] = 1
        };
        items[items.Count - 1] = 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(47632, "https://github.com/dotnet/roslyn/issues/47632")]
        public async Task TestWithIndexerInInitializerLeft()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;

class C
{
    static void M()
    {
        List<object> items = [|new|] List<object>();
        items[0] = 1;
        items[^1] = 2;
    }
}",
@"
using System.Collections.Generic;

class C
{
    static void M()
    {
        List<object> items = new List<object>
        {
            [0] = 1
        };
        items[^1] = 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(47632, "https://github.com/dotnet/roslyn/issues/47632")]
        public async Task TestWithImplicitObjectCreation()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;

class C
{
    static void M()
    {
        List<object> items = [|new|]();
        items[0] = 1;
    }
}",
@"
using System.Collections.Generic;

class C
{
    static void M()
    {
        List<object> items = new()
        {
            [0] = 1
        };
    }
}");
        }
    }
}
