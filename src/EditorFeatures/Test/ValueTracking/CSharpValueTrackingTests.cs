// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ValueTracking;

[UseExportProvider]
public sealed class CSharpValueTrackingTests : AbstractBaseValueTrackingTests
{
    protected override TestWorkspace CreateWorkspace(string code, TestComposition composition)
        => TestWorkspace.CreateCSharp(code, composition: composition);

    [Theory, CombinatorialData]
    public async Task TestProperty(TestHost testHost)
    {
        var code =
            """

            class C
            {
                public string $$S { get; set; } = "";

                public void SetS(string s)
                {
                    S = s;
                }

                public string GetS() => S;
            }

            """;
        using var workspace = CreateWorkspace(code, testHost);

        //
        // property S 
        //  |> S = s [Code.cs:7]
        //  |> public string S { get; set; } [Code.cs:3]
        //
        await ValidateItemsAsync(
            workspace,
            itemInfo:
            [
                (7, "s"),
                (3, "public string S { get; set; } = \"\";"),
            ]);
    }

    [Theory, CombinatorialData]
    public async Task TestPropertyWithThis(TestHost testHost)
    {
        var code =
            """

            class C
            {
                public string $$S { get; set; } = "";

                public void SetS(string s)
                {
                    this.S = s;
                }

                public string GetS() => this.S;
            }

            """;
        using var workspace = CreateWorkspace(code, testHost);

        //
        // property S 
        //  |> S = s [Code.cs:7]
        //  |> public string S { get; set; } [Code.cs:3]
        //
        await ValidateItemsAsync(
            workspace,
            itemInfo:
            [
                (7, "s"),
                (3, "public string S { get; set; } = \"\";"),
            ]);
    }

    [Theory, CombinatorialData]
    public async Task TestField(TestHost testHost)
    {
        var code =
            """

            class C
            {
                private string $$_s = "";

                public void SetS(string s)
                {
                    _s = s;
                }

                public string GetS() => _s;
            }
            """;
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);

        //
        // field _s 
        //  |> _s = s [Code.cs:7]
        //  |> string _s = "" [Code.cs:3]
        //
        await ValidateItemsAsync(
            workspace,
            itemInfo:
            [
                (7, "s"),
                (3, """
                _s = ""
                """)
            ]);
    }

    [Theory, CombinatorialData]
    public async Task TestFieldWithThis(TestHost testHost)
    {
        var code =
            """

            class C
            {
                private string $$_s = "";

                public void SetS(string s)
                {
                    this._s = s;
                }

                public string GetS() => this._s;
            }
            """;
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);

        //
        // field _s 
        //  |> this._s = s [Code.cs:7]
        //  |> string _s = "" [Code.cs:3]
        //
        await ValidateItemsAsync(
            workspace,
            itemInfo:
            [
                (7, "s"),
                (3, """
                _s = ""
                """)
            ]);
    }

    [Theory, CombinatorialData]
    public async Task TestLocal(TestHost testHost)
    {
        var code =
            """

            class C
            {
                public int Add(int x, int y)
                {
                    var $$z = x;
                    z += y;
                    return z;
                }
            }
            """;
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);

        //
        // local variable z
        //  |> z += y [Code.cs:6]
        //  |> var z = x [Code.cs:5]
        //
        Assert.Equal(2, initialItems.Length);
        ValidateItem(initialItems[0], 6);
        ValidateItem(initialItems[1], 5);
    }

    [Theory, CombinatorialData]
    public async Task TestParameter(TestHost testHost)
    {
        var code =
            """

            class C
            {
                public int Add(int $$x, int y)
                {
                    x += y;
                    return x;
                }
            }
            """;
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);

        //
        // parameter x 
        //  |> x += y [Code.cs:5]
        //  |> Add(int x, int y) [Code.cs:3]
        //
        Assert.Equal(2, initialItems.Length);
        ValidateItem(initialItems[0], 5);
        ValidateItem(initialItems[1], 3);
    }

    [Theory, CombinatorialData]
    public async Task TestParameter2(TestHost testHost)
    {
        var code =
            """

            class C
            {
                public void InvokeM(string arg)
                {
                    M(arg);
                    M();
                    M("test");
                }

                public void M(string? $$x = null)
                {
                }
            }
            """;
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);

        //
        // public void M(|string? x = null|)
        //  M(|"test"|)
        //  |M("test")|
        //  M(|arg|)
        //  |M(arg)|
        // 
        Assert.Equal(1, initialItems.Length);
        ValidateItem(initialItems[0], 10);

        var items = await ValidateChildrenAsync(
            workspace,
            initialItems.Single(),
            childInfo:
            [
                (7, """
                "test"
                """),           // M(|"test"|)
                (7, """
                M("test")
                """),           // |M("test")|
                (5, "arg"),     // M(|arg|)
                (5, "M(arg)"),  // |M(arg)|
            ]);
    }

    [Theory, CombinatorialData]
    public async Task TestMissingOnMethod(TestHost testHost)
    {
        var code =
            """

            class C
            {
                public int $$Add(int x, int y)
                {
                    x += y;
                    return x;
                }
            }
            """;
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);
        Assert.Empty(initialItems);
    }

    [Theory, CombinatorialData]
    public async Task TestMissingOnClass(TestHost testHost)
    {
        var code =
            """

            class $$C
            {
                public int Add(int x, int y)
                {
                    x += y;
                    return x;
                }
            }
            """;
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);
        Assert.Empty(initialItems);
    }

    [Theory, CombinatorialData]
    public async Task TestMissingOnNamespace(TestHost testHost)
    {
        var code =
            """

            namespace $$N
            {
                class C
                {
                    public int Add(int x, int y)
                    {
                        x += y;
                        return x;
                    }
                }
            }
            """;
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);
        Assert.Empty(initialItems);
    }

    [Theory, CombinatorialData]
    public async Task MethodTracking1(TestHost testHost)
    {
        var code =
            """

            class C
            {
                public string S { get; set; } = "";

                public void SetS(string s)
                {
                    S$$ = s;
                }

                public string GetS() => S;
            }

            class Other
            {
                public void CallS(C c, string str)
                {
                    c.SetS(str);
                }

                public void CallS(C c)
                {
                    CallS(c, CalculateDefault(c));
                }

                private string CalculateDefault(C c)
                {
                    if (c is null)
                    {
                        return "null";
                    }

                    if (string.IsNullOrEmpty(c.S))
                    {
                        return "defaultstring";
                    }

                    return "";
                }
            }

            """;
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);

        //
        // S = s; [Code.cs:7]
        //  |> S = [|s|] [Code.cs:7]
        //    |> [|c.SetS(str)|]; [Code.cs:17]
        //    |> c.SetS([|str|]); [Code.cs:17]
        //      |> CallS([|c|], CalculateDefault(c)) [Code.cs:22]
        //      |> CallS(c, [|CalculateDefault(c)|]) [Code.cs:22]
        //         |>  return "" [Code.cs:37]
        //         |>  return "defaultstring" [Code.cs:34]
        //         |> return "null" [Code.cs:29]
        // 
        Assert.Equal(1, initialItems.Length);
        ValidateItem(initialItems[0], 7);

        var items = await ValidateChildrenAsync(
            workspace,
            initialItems.Single(),
            childInfo:
            [
                (17, "str"), // |> c.SetS([|str|]); [Code.cs:17]
                (17, "c.SetS(str)"), // |> [|c.SetS(str)|]; [Code.cs:17]
            ]);

        // |> [|c.SetS(s)|]; [Code.cs:17]
        await ValidateChildrenEmptyAsync(workspace, items[1]);

        // |> c.SetS([|s|]); [Code.cs:17]
        items = await ValidateChildrenAsync(
            workspace,
            items[0],
            childInfo:
            [
                (22, "c" ), // |> CallS([|c|], CalculateDefault(c)) [Code.cs:22]
                (22, "c" ), // |> CallS(c, CalculateDefault([|c|])) [Code.cs:22]
                (22, "CalculateDefault(c)" ), // |> CallS(c, [|CalculateDefault(c)|]) [Code.cs:22]
                (22, "CallS(c, CalculateDefault(c))" ) // |> [|CallS(c, CalculateDefault(c))|] [Code.cs:22]
            ]);

        // |> CallS([|c|], CalculateDefault(c)) [Code.cs:22]
        await ValidateChildrenEmptyAsync(workspace, items[0]);
        // |> CallS(c, CalculateDefault([|c|])) [Code.cs:22]
        await ValidateChildrenEmptyAsync(workspace, items[1]);
        // |> CallS(c, [|CalculateDefault(c)|]) [Code.cs:22]
        await ValidateChildrenEmptyAsync(workspace, items[3]);

        // |> CallS(c, [|CalculateDefault(c)|]) [Code.cs:22]
        var children = await ValidateChildrenAsync(
            workspace,
            items[2],
            childInfo:
            [
                (37, """
                ""
                """), // |>  return "" [Code.cs:37]
                (34, """
                "defaultstring"
                """), // |>  return "defaultstring" [Code.cs:34]
                (29, """
                "null"
                """), // |> return "null" [Code.cs:29]
            ]);

        foreach (var child in children)
        {
            await ValidateChildrenEmptyAsync(workspace, child);
        }
    }

    [Theory, CombinatorialData]
    public async Task MethodTracking2(TestHost testHost)
    {
        var code =
            """

            class C
            {
                public string S { get; set; } = "";

                public void SetS(string s)
                {
                    S$$ = s;
                }

                public string GetS() => S;
            }

            class Other
            {
                private readonly string _adornment;
                public Other(string adornment)
                {
                    _adornment = adornment;
                }

                public void CallS(C c, string s)
                {
                    c.SetS(s);
                }

                public void CallS(C c)
                {
                    CallS(c, CalculateDefault(c) + _adornment);
                }

                private string CalculateDefault(C c)
                {
                    if (c is null)
                    {
                        return "null";
                    }

                    if (string.IsNullOrEmpty(c.S))
                    {
                        return "defaultstring";
                    }

                    return "";
                }
            }

            class Program
            {
                public static void Main(string[] args)
                {
                    var other = new Other("some value");
                    var c = new C();
                    other.CallS(c);
                }
            }

            """;
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);

        //
        // S = s; [Code.cs:7]
        //  |> S = [|s|] [Code.cs:7]
        //    |> [|c.SetS(s)|]; [Code.cs:23]
        //    |> c.SetS([|s|]); [Code.cs:23]
        //      |> CallS([|c|], CalculateDefault(c) + _adornment) [Code.cs:28]
        //        |> other.CallS([|c|]); [Code.cs:53]
        //      |> CallS(c, CalculateDefault(c) + [|_adornment|]) [Code.cs:28]
        //        |> _adornment = [|adornment|]; [Code.cs:18]
        //          |> var other = new Other([|"some value"|]); [Code.cs:51]
        //      |> CallS(c, CalculateDefault([|c|]) + _adornment) [Code.cs:28]
        //        |> other.CallS([|c|]); [Code.cs:53]
        //      |> CallS(c, [|CalculateDefault(c)|] + _adornment) [Code.cs:28]
        //        |>  return "" [Code.cs:37]
        //        |>  return "defaultstring" [Code.cs:34]
        //        |> return "null" [Code.cs:29]
        //      |> [|CallS(c, CalculateDefault(c) + _adornment)|] [Code.cs:28]
        //
        Assert.Equal(1, initialItems.Length);
        ValidateItem(initialItems[0], 7);

        var items = await ValidateChildrenAsync(
            workspace,
            initialItems.Single(),
            childInfo:
            [
                (23, "s"), // |> c.SetS([|s|]); [Code.cs:23]
                (23, "c.SetS(s)"), // |> c.SetS(s); [Code.cs:23]
            ]);

        // |> c.SetS(s); [Code.cs:23]
        await ValidateChildrenEmptyAsync(workspace, items[1]);

        // |> c.SetS([|s|]); [Code.cs:23]
        items = await ValidateChildrenAsync(
            workspace,
            items[0],
            childInfo:
            [
                (28, "c" ), // |> CallS([|c|], CalculateDefault(c) + _adornment) [Code.cs:28]
                (28, "_adornment" ), // |> CallS(c, CalculateDefault(c) + [|_adornment|]) [Code.cs:28]
                (28, "c" ), // |> CallS(c, CalculateDefault([|c|]) + _adornment) [Code.cs:28]
                (28, "CalculateDefault(c)" ), // |> CallS(c, [|CalculateDefault|](c) + _adornment) [Code.cs:28]
                (28, "CallS(c, CalculateDefault(c) + _adornment)" ), // |> [|CallS(c, CalculateDefault(c) + _adornment)|] [Code.cs:28]
            ]);

        // |> CallS([|c|], CalculateDefault(c) + _adornment) [Code.cs:28]
        var children = await ValidateChildrenAsync(
            workspace,
            items[0],
            childInfo:
            [
                (53, "other.CallS(c)"), // |> other.CallS([|c|]); [Code.cs:53]
            ]);

        await ValidateChildrenEmptyAsync(workspace, children);

        // |> CallS(c, CalculateDefault([|c|]) + _adornment) [Code.cs:28]
        children = await ValidateChildrenAsync(
            workspace,
            items[2],
            childInfo:
            [
                (53, "other.CallS(c)"), // |> other.CallS([|c|]); [Code.cs:53]
            ]);

        await ValidateChildrenEmptyAsync(workspace, children);

        // |> CallS(c, CalculateDefault(c) + [|_adornment|]) [Code.cs:28]
        children = await ValidateChildrenAsync(
            workspace,
            items[1],
            childInfo:
            [
                (18, "adornment"), // |> _adornment = [|adornment|] [Code.cs:18]
            ]);

        children = await ValidateChildrenAsync(
            workspace,
            children.Single(),
            childInfo:
            [
                (51, """
                "some value"
                """) // |> var other = new Other([|"some value"|]); [Code.cs:51]
            ]);
        await ValidateChildrenEmptyAsync(workspace, children);

        // |> CallS(c, [|CalculateDefault(c)|] + _adornment) [Code.cs:28]
        children = await ValidateChildrenAsync(
            workspace,
            items[3],
            childInfo:
            [
                (43, """
                ""
                """), // |>  return "" [Code.cs:37]
                (40, """
                "defaultstring"
                """), // |>  return "defaultstring" [Code.cs:34]
                (35, """
                "null"
                """), // |> return "null" [Code.cs:29]
            ]);

        await ValidateChildrenEmptyAsync(workspace, children);

        // |> [|CallS(c, CalculateDefault(c) + _adornment)|] [Code.cs:28]
        await ValidateChildrenEmptyAsync(workspace, items[4]);
    }

    [Theory, CombinatorialData]
    public async Task MethodTracking3(TestHost testHost)
    {
        var code =
            """

            using System.Threading.Tasks;

            namespace N
            {
                class C
                {
                    public int Add(int x, int y)
                    {
                        x += y;
                        return x;
                    }

                    public Task<int> AddAsync(int x, int y) => Task.FromResult(Add(x,y));

                    public async Task<int> Double(int x)
                    {
                        x = await AddAsync(x, x);
                        return $$x;
                    }
                }
            }
            """;
        //
        //  |> return [|x|] [Code.cs:18]
        //    |> x = await AddAsync([|x|], x) [Code.cs:17]
        //    |> x = await AddAsync(x, [|x|]) [Code.cs:17]
        //    |> x = await [|AddAsync(x, x)|] [Code.cs:17]
        //      |> [|Task.FromResult|](Add(x, y)) [Code.cs:13]
        //      |> Task.FromResult([|Add(x, y)|]) [Code.cs:13]
        //        |> return x [Code.cs:11]
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);
        Assert.Equal(1, initialItems.Length);
        ValidateItem(initialItems.Single(), 18, "x"); // |> return [|x|] [Code.cs:18]

        var children = await ValidateChildrenAsync(
            workspace,
            initialItems.Single(),
            childInfo:
            [
                (17, "x"), // |> x = await AddAsync([|x|], x) [Code.cs:17]
                (17, "x"), // |> x = await AddAsync(x, [|x|]) [Code.cs:17]
                (17, "AddAsync(x, x)") // |> x = await [|AddAsync(x, x)|] [Code.cs:17]
            ]);

        // |> x = await [|AddAsync(x, x)|] [Code.cs:17]
        children = await ValidateChildrenAsync(
            workspace,
            children[2],
            childInfo:
            [
                (13, "x"), // |> Task.FromResult(Add([|x|], y)) [Code.cs:13]
                (13, "y"), // |> Task.FromResult(Add(x, [|y|])) [Code.cs:13]
                (13, "Add(x,y)"),  // |> Task.FromResult([|Add(x, y)|]) [Code.cs:13]
                (13, "Task.FromResult(Add(x,y))"), // |> [|Task.FromResult|](Add(x, y)) [Code.cs:13]
            ]);

        // |> [|Task.FromResult|](Add(x, y)) [Code.cs:13]
        await ValidateChildrenEmptyAsync(workspace, children[3]);

        // |> Task.FromResult([|Add(x, y)|]) [Code.cs:13]
        children = await ValidateChildrenAsync(
            workspace,
            children[2],
            childInfo:
            [
                (10, "x") // |> return x [Code.cs:10]
            ]);
    }

    [Theory, CombinatorialData]
    public async Task OutParam(TestHost testHost)
    {
        var code = """

            class C
            {
                bool TryConvertInt(object o, out int i)
                {
                    if (int.TryParse(o.ToString(), out i))
                    {
                        return true;
                    }

                    return false;
                }

                void M()
                {
                    int i = 0;
                    object o = "5";

                    if (TryConvertInt(o, out i))
                    {
                        Console.WriteLine($$i);
                    }
                    else
                    {
                        i = 2;
                    }
                }
            }
            """;

        //
        //  |> Console.WriteLine($$i); [Code.cs:20]
        //    |> i = [|2|] [Code.cs:24]
        //    |> if (TryConvertInt(o, out [|i|])) [Code.cs:18]
        //      |> if (int.TryParse(o.ToString(), out [|i|])) [Code.cs:5]
        //    |> int i = 0 [Code.cs:15]
        using var workspace = CreateWorkspace(code, testHost);
        var initialItems = await GetTrackedItemsAsync(workspace);

        Assert.Equal(1, initialItems.Length);

        // |> Console.WriteLine($$i);[Code.cs:20]
        ValidateItem(initialItems.Single(), 20, "i");

        var children = await ValidateChildrenAsync(
            workspace,
            initialItems.Single(),
            childInfo:
            [
                (24, "2"), // |> i = [|2|] [Code.cs:24]
                (18, "i"), // |> if (TryConvertInt(o, out [|i|])) [Code.cs:18]
                (15, "0"), // |> int i = 0 [Code.cs:15]
            ]);

        // |> i = [|2|] [Code.cs:24]
        await ValidateChildrenEmptyAsync(workspace, children[0]);

        // |> if (TryConvertInt(o, out [|i|])) [Code.cs:18]
        children = await ValidateChildrenAsync(
            workspace,
            children[1],
            childInfo:
            [
                (5, "i") // |> if (int.TryParse(o.ToString(), out [|i|])) [Code.cs:5]
            ]);

        await ValidateChildrenEmptyAsync(workspace, children.Single());
    }

    [Theory, CombinatorialData]
    public async Task TestVariableReferenceStart(TestHost testHost)
    {
        var code =
            """

            class Test
            {
                public static void M()
                {
                    int x = GetM();
                    Console.Write(x);
                    var y = $$x + 1;
                }

                public static int GetM()
                {
                    var x = 0;
                    return x;
                }
            }
            """;

        //
        //  |> var y = x + 1; [Code.cs:7]
        //    |> int x = GetM() [Code.cs:5]
        //      |> return x; [Code.cs:13]
        //        |> var x = 0; [Code.cs:12]
        using var workspace = CreateWorkspace(code, testHost);

        var items = await ValidateItemsAsync(
            workspace,
            itemInfo:
            [
                (7, "x") // |> var y = [|x|] + 1; [Code.cs:7]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (5, "GetM()") // |> int x = [|GetM()|] [Code.cs:5]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (13, "x") // |> return [|x|]; [Code.cs:13]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (12, "0") // |> var x = [|0|]; [Code.cs:12]
            ]);

        await ValidateChildrenEmptyAsync(workspace, items.Single());
    }

    [Theory, CombinatorialData]
    public async Task TestVariableReferenceStart2(TestHost testHost)
    {
        var code =
            """

            class Test
            {
                public static void M()
                {
                    int x = GetM();
                    Console.Write($$x);
                    var y = x + 1;
                }

                public static int GetM()
                {
                    var x = 0;
                    return x;
                }
            }
            """;

        //
        //  |> Console.Write(x); [Code.cs:6]
        //    |> int x = GetM() [Code.cs:5]
        //      |> return x; [Code.cs:13]
        //        |> var x = 0; [Code.cs:12]
        using var workspace = CreateWorkspace(code, testHost);

        var items = await ValidateItemsAsync(
            workspace,
            itemInfo:
            [
                (6, "x") // |> Console.Write([|x|]); [Code.cs:7]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (5, "GetM()") // |> int x = [|GetM()|] [Code.cs:5]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (13, "x") // |> return [|x|]; [Code.cs:13]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (12, "0") // |> var x = [|0|]; [Code.cs:12]
            ]);

        await ValidateChildrenEmptyAsync(workspace, items.Single());
    }

    [Theory, CombinatorialData]
    public async Task TestVariableReferenceStart3(TestHost testHost)
    {
        var code =
            """

            class Test
            {
                public static void M()
                {
                    int x = GetM();
                    Console.Write($$x);
                    var y = x + 1;
                    x += 1;
                    Console.Write(x);
                    Console.Write(y);
                }

                public static int GetM()
                {
                    var x = 0;
                    return x;
                }
            }
            """;

        //
        //  |> Console.Write(x); [Code.cs:6]
        //    |> int x = GetM() [Code.cs:5]
        //      |> return x; [Code.cs:13]
        //        |> var x = 0; [Code.cs:12]
        //    |> x += 1 [Code.cs:8]
        using var workspace = CreateWorkspace(code, testHost);

        var items = await ValidateItemsAsync(
            workspace,
            itemInfo:
            [
                (6, "x") // |> Console.Write([|x|]); [Code.cs:7]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (8, "1"),      // |> x += 1; [Codec.s:8]
                (5, "GetM()"), // |> int x = [|GetM()|] [Code.cs:5]
            ]);

        await ValidateChildrenEmptyAsync(workspace, items[0]);

        items = await ValidateChildrenAsync(
            workspace,
            items[1],
            childInfo:
            [
                (16, "x") // |> return [|x|]; [Code.cs:13]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (15, "0") // |> var x = [|0|]; [Code.cs:12]
            ]);

        await ValidateChildrenEmptyAsync(workspace, items.Single());
    }

    [Theory, CombinatorialData]
    public async Task TestMultipleDeclarators(TestHost testHost)
    {
        var code =
            """

            class Test
            {
                public static void M()
                {
                    int x = GetM(), z = 5;
                    Console.Write($$x);
                    var y = x + 1 + z;
                    x += 1;
                    Console.Write(x);
                    Console.Write(y);
                }

                public static int GetM()
                {
                    var x = 0;
                    return x;
                }
            }
            """;

        //
        //  |> Console.Write(x); [Code.cs:6]
        //    |> int x = GetM() [Code.cs:5]
        //      |> return x; [Code.cs:13]
        //        |> var x = 0; [Code.cs:12]
        //    |> x += 1 [Code.cs:8]
        using var workspace = CreateWorkspace(code, testHost);

        var items = await ValidateItemsAsync(
            workspace,
            itemInfo:
            [
                (6, "x") // |> Console.Write([|x|]); [Code.cs:7]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (8, "1"),      // |> x += 1; [Codec.s:8]
                (5, "GetM()"), // |> int x = [|GetM()|] [Code.cs:5]
            ]);

        await ValidateChildrenEmptyAsync(workspace, items[0]);

        items = await ValidateChildrenAsync(
            workspace,
            items[1],
            childInfo:
            [
                (16, "x") // |> return [|x|]; [Code.cs:13]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (15, "0") // |> var x = [|0|]; [Code.cs:12]
            ]);

        await ValidateChildrenEmptyAsync(workspace, items.Single());
    }

    [Theory, CombinatorialData]
    public async Task TestIndex(TestHost testHost)
    {
        var code =
            """

            class Test
            {
                public int this[string $$key] => 0;

                public int M(Test localTest)
                {
                    var assignedVariable = this["test"];
                    System.Console.WriteLine(this["test"]);
                    
                    return localTest["test"];
                }
            }
            """;

        //
        //  |> public int this[string [|key|]] => 0; [Code.cs:4]
        //    |> return [|localTest|][[|"test"|]]; [Code.cs:10]
        //    |> System.Console.WriteLine(this[[|"test"|]]); [Code.cs:8]
        //    |> var [|assignedVariable = this["test"]|]; [Code.cs:7]
        using var workspace = CreateWorkspace(code, testHost);

        var items = await ValidateItemsAsync(
            workspace,
            itemInfo:
            [
                (3, "string key") // |>public int this[[|string key|]] => 0; [Code.cs:4]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (10, "localTest"), // return [|localTest|]["test"]; [Code.cs:10] (This is included because it is part of a return statement, and follows same logic as other references for if it is tracked)
                (10, """
                "test"
                """),  // return localTest[[|"test"|]]; [Code.cs:10]
                (8, """
                "test"
                """),   // System.Console.WriteLine(this[[|"test"|]]); [Code.cs:8]
                (7, """
                "test"
                """),   // var assignedVariable = this[[|"test"|]]; [Code.cs:7]
            ]);

        await ValidateChildrenEmptyAsync(workspace, items[0]);
        await ValidateChildrenEmptyAsync(workspace, items[1]);
        await ValidateChildrenEmptyAsync(workspace, items[2]);
        await ValidateChildrenEmptyAsync(workspace, items[3]);
    }

    [Theory, CombinatorialData]
    public async Task TestPropertyValue(TestHost testHost)
    {
        var code =
            """

            class Test
            {
                private int _i;
                public int I 
                {
                    get => _i;
                    set 
                    {
                        _i = $$value;
                    }
                }

                public int M(Test localTest)
                {
                    localTest.I = 5;
                }
            }
            """;
        //  _i = [|value|]; [Code.cs:9]
        //    |> localTest.I = [|5|]; [Code.cs:15]
        using var workspace = CreateWorkspace(code, testHost);

        var items = await ValidateItemsAsync(
            workspace,
            itemInfo:
            [
                (9, "value") // _i = [|value|]; [Code.cs:9]
            ]);

        items = await ValidateChildrenAsync(
            workspace,
            items.Single(),
            childInfo:
            [
                (15, "5") // localTest.I = [|5|]; [Code.cs:15]
            ]);

        await ValidateChildrenEmptyAsync(workspace, items.Single());
    }
}
