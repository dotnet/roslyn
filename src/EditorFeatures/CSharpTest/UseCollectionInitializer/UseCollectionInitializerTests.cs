// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCollectionInitializer
{
    public partial class UseCollectionInitializerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUseCollectionInitializerDiagnosticAnalyzer(),
                new CSharpUseCollectionInitializerCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestOnVariableDeclarator()
        {
            await TestAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [||]new List<int>();
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
            await TestAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [||]new List<int>();
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
            await TestMissingAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [||]new List<int>();
        c[1] = 2;
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestComplexIndexAccess1()
        {
            await TestAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        a.b.c = [||]new List<int>();
        a.b.c[1] = 2;
    }
}",
@"
using System.Collections.Generic;
class C
{
    void M()
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
            await TestAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [||]new List<int>();
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
        var c = new List<int>
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
            await TestAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [||]new List<int>();
        c[1] = 2;
        c[2] = """";
        c[3, 4] = 5;
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
            [1] = 2,
            [2] = """",
            [3, 4] = 5
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestIndexFollowedByInvocation()
        {
            await TestAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [||]new List<int>();
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
            await TestAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [||]new List<int>();
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
            await TestAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [||]new List<int>();
        c.Add(1);
        c.Add(2);
        throw new Exception();
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
        throw new Exception();
        c.Add(3);
        c.Add(4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestMissingBeforeCSharp3()
        {

            await TestMissingAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [||]new List<int>();
        c.Add(1);
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestMissingOnNonIEnumerable()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [||]new C();
        c.Add(1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestMissingOnNonIEnumerableEvenWithAdd()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [||]new C();
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
            await TestAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [||]new List<int>(1);
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
            await TestAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        List<int> c = null;
        c = [||]new List<int>();
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
            await TestMissingAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [||]new List<int>();
        c.Add(ref i);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestComplexInitializer()
        {
            await TestAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        List<int>[] array;
        array[0] = [||]new List<int>();
        array[0].Add(1);
        array[0].Add(2);
    }
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        List<int>[] array;
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
            await TestMissingAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [||]new List<int>();
        c.Add(arg: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestMissingWithExistingInitializer()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = [||]new List<int>() { 1 };
        c.Add(1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestFixAllInDocument1()
        {
            await TestAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        List<int>[] array;
        array[0] = {|FixAllInDocument:new|} List<int>();
        array[0].Add(1);
        array[0].Add(2);
        array[1] = new List<int>();
        array[1].Add(3);
        array[1].Add(4);
    }
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        List<int>[] array;
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
            await TestAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var list1 = {|FixAllInDocument:new|} List<int>(() => {
            var list2 = new List<int>();
            list2.Add(2);
        });
        list1.Add(1);
    }
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var list1 = new List<int>(() => {
            var list2 = new List<int>
            {
                2
            };
        })
        {
            1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestFixAllInDocument3()
        {
            await TestAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var list1 = {|FixAllInDocument:new|} List<int>();
        list1.Add(() => {
            var list2 = new List<int>();
            list2.Add(2);
        });
    }
}",
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var list1 = new List<int>
        {
            () => {
                var list2 = new List<int>
                {
                    2
                };
            }
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestTrivia1()
        {
            await TestAsync(
@"
using System.Collections.Generic;
class C
{
    void M()
    {
        var c = [||]new List<int>();
        c.Add(1); // Foo
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
            1, // Foo
            2 // Bar
        };
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        public async Task TestComplexInitializer2()
        {
            await TestAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var c = new [||]Dictionary<int, string>();
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
            {
                1,
                ""x""
            },
            {
                2,
                ""y""
            }
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
        [WorkItem(16158, "https://github.com/dotnet/roslyn/issues/16158")]
        public async Task TestIncorrectAddName()
        {
            await TestAsync(
@"using System.Collections.Generic;

public class Foo
{
    public static void Bar()
    {
        string item = null;
        var items = new List<string>();

        var values = new [||]List<string>(); // Collection initialization can be simplified
        values.Add(item);
        values.AddRange(items);
    }
}",
@"using System.Collections.Generic;

public class Foo
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
            await TestMissingAsync(
@"
        using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var myStringArray = new string[] { ""Test"", ""123"", ""ABC"" };
        var myStringList = myStringArray?.ToList() ?? new [||]List<string>();
        myStringList.Add(""Done"");
    }
}");
        }
    }
}