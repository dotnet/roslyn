// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Linq;
using System;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ValueTracking
{
    [UseExportProvider]
    public class CSharpValueTrackingTests : AbstractBaseValueTrackingTests
    {
        [Fact]
        public async Task TestProperty()
        {
            var code =
@"
class C
{
    public string $$S { get; set; } = """""";

    public void SetS(string s)
    {
        S = s;
    }

    public string GetS() => S;
}
";
            using var workspace = TestWorkspace.CreateCSharp(code);

            //
            // property S 
            //  |> S = s [Code.cs:7]
            //  |> public string S { get; set; } [Code.cs:3]
            //
            await ValidateItemsAsync(
                workspace,
                itemInfo: new[]
                {
                    (7, "s"),
                    (3, "public string S { get; set; } = \"\""),
                });
        }

        [Fact]
        public async Task TestField()
        {
            var code =
@"
class C
{
    private string $$_s = """""";

    public void SetS(string s)
    {
        _s = s;
    }

    public string GetS() => _s;
}";
            using var workspace = TestWorkspace.CreateCSharp(code);
            var initialItems = await GetTrackedItemsAsync(workspace);

            //
            // field _s 
            //  |> _s = s [Code.cs:7]
            //  |> string _s = "" [Code.cs:3]
            //
            Assert.Equal(2, initialItems.Length);
            ValidateItem(initialItems[0], 7);
            ValidateItem(initialItems[1], 3);
        }

        [Fact]
        public async Task TestLocal()
        {
            var code =
@"
class C
{
    public int Add(int x, int y)
    {
        var $$z = x;
        z += y;
        return z;
    }
}";
            using var workspace = TestWorkspace.CreateCSharp(code);
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

        [Fact]
        public async Task TestParameter()
        {
            var code =
@"
class C
{
    public int Add(int $$x, int y)
    {
        x += y;
        return x;
    }
}";
            using var workspace = TestWorkspace.CreateCSharp(code);
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

        [Fact]
        public async Task TestMissingOnMethod()
        {
            var code =
@"
class C
{
    public int $$Add(int x, int y)
    {
        x += y;
        return x;
    }
}";
            using var workspace = TestWorkspace.CreateCSharp(code);
            var initialItems = await GetTrackedItemsAsync(workspace);
            Assert.Empty(initialItems);
        }

        [Fact]
        public async Task TestMissingOnClass()
        {
            var code =
@"
class $$C
{
    public int Add(int x, int y)
    {
        x += y;
        return x;
    }
}";
            using var workspace = TestWorkspace.CreateCSharp(code);
            var initialItems = await GetTrackedItemsAsync(workspace);
            Assert.Empty(initialItems);
        }

        [Fact]
        public async Task TestMissingOnNamespace()
        {
            var code =
@"
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
}";
            using var workspace = TestWorkspace.CreateCSharp(code);
            var initialItems = await GetTrackedItemsAsync(workspace);
            Assert.Empty(initialItems);
        }

        [Fact]
        public async Task MethodTracking1()
        {
            var code =
@"
class C
{
    public string S { get; set; } = """""";

    public void SetS(string s)
    {
        S$$ = s;
    }

    public string GetS() => S;
}

class Other
{
    public void CallS(C c, string s)
    {
        c.SetS(s);
    }

    public void CallS(C c)
    {
        CallS(c, CalculateDefault(c));
    }

    private string CalculateDefault(C c)
    {
        if (c is null)
        {
            return ""null"";
        }

        if (string.IsNullOrEmpty(c.S))
        {
            return ""defaultstring"";
        }

        return """";
    }
}
";
            using var workspace = TestWorkspace.CreateCSharp(code);
            var initialItems = await GetTrackedItemsAsync(workspace);

            //
            // S = s; [Code.cs:7]
            //  |> S = [|s|] [Code.cs:7]
            //    |> [|c.SetS(s)|]; [Code.cs:17]
            //    |> c.SetS([|s|]); [Code.cs:17]
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
                childInfo: new[]
                {
                    (17, "s"), // |> c.SetS([|s|]); [Code.cs:17]
                    (17, "c.SetS(s)"), // |> [|c.SetS(s)|]; [Code.cs:17]
                });

            // |> [|c.SetS(s)|]; [Code.cs:17]
            await ValidateChildrenEmptyAsync(workspace, items[1]);

            // |> c.SetS([|s|]); [Code.cs:17]
            items = await ValidateChildrenAsync(
                workspace,
                items[0],
                childInfo: new[]
                {
                    (22, "c" ), // |> CallS([|c|], CalculateDefault(c)) [Code.cs:22]
                    (22, "c" ), // |> CallS(c, CalculateDefault([|c|])) [Code.cs:22]
                    (22, "CalculateDefault(c)" ), // |> CallS(c, [|CalculateDefault(c)|]) [Code.cs:22]
                    (22, "CallS(c, CalculateDefault(c))" ) // |> [|CallS(c, CalculateDefault(c))|] [Code.cs:22]
                });

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
                childInfo: new[]
                {
                    (37, "\"\""), // |>  return "" [Code.cs:37]
                    (34, "\"defaultstring\""), // |>  return "defaultstring" [Code.cs:34]
                    (29, "\"null\""), // |> return "null" [Code.cs:29]
                });

            foreach (var child in children)
            {
                await ValidateChildrenEmptyAsync(workspace, child);
            }
        }

        [Fact]
        public async Task MethodTracking2()
        {
            var code =
@"
class C
{
    public string S { get; set; } = """""";

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
            return ""null"";
        }

        if (string.IsNullOrEmpty(c.S))
        {
            return ""defaultstring"";
        }

        return """";
    }
}

class Program
{
    public static void Main(string[] args)
    {
        var other = new Other(""some value"");
        var c = new C();
        other.CallS(c);
    }
}
";
            using var workspace = TestWorkspace.CreateCSharp(code);
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
                childInfo: new[]
                {
                    (23, "s"), // |> c.SetS([|s|]); [Code.cs:23]
                    (23, "c.SetS(s)"), // |> c.SetS(s); [Code.cs:23]
                });

            // |> c.SetS(s); [Code.cs:23]
            await ValidateChildrenEmptyAsync(workspace, items[1]);

            // |> c.SetS([|s|]); [Code.cs:23]
            items = await ValidateChildrenAsync(
                workspace,
                items[0],
                childInfo: new[]
                {
                    (28, "c" ), // |> CallS([|c|], CalculateDefault(c) + _adornment) [Code.cs:28]
                    (28, "_adornment" ), // |> CallS(c, CalculateDefault(c) + [|_adornment|]) [Code.cs:28]
                    (28, "c" ), // |> CallS(c, CalculateDefault([|c|]) + _adornment) [Code.cs:28]
                    (28, "CalculateDefault(c)" ), // |> CallS(c, [|CalculateDefault|](c) + _adornment) [Code.cs:28]
                    (28, "CallS(c, CalculateDefault(c) + _adornment)" ), // |> [|CallS(c, CalculateDefault(c) + _adornment)|] [Code.cs:28]
                });

            // |> CallS([|c|], CalculateDefault(c) + _adornment) [Code.cs:28]
            var children = await ValidateChildrenAsync(
                workspace,
                items[0],
                childInfo: new[]
                {
                    (53, "other.CallS(c)"), // |> other.CallS([|c|]); [Code.cs:53]
                });

            await ValidateChildrenEmptyAsync(workspace, children);

            // |> CallS(c, CalculateDefault([|c|]) + _adornment) [Code.cs:28]
            children = await ValidateChildrenAsync(
                workspace,
                items[2],
                childInfo: new[]
                {
                    (53, "other.CallS(c)"), // |> other.CallS([|c|]); [Code.cs:53]
                });

            await ValidateChildrenEmptyAsync(workspace, children);

            // |> CallS(c, CalculateDefault(c) + [|_adornment|]) [Code.cs:28]
            children = await ValidateChildrenAsync(
                workspace,
                items[1],
                childInfo: new[]
                {
                    (18, "adornment"), // |> _adornment = [|adornment|] [Code.cs:18]
                });

            children = await ValidateChildrenAsync(
                workspace,
                children.Single(),
                childInfo: new[]
                {
                    (51, "\"some value\"") // |> var other = new Other([|"some value"|]); [Code.cs:51]
                });
            await ValidateChildrenEmptyAsync(workspace, children);

            // |> CallS(c, [|CalculateDefault(c)|] + _adornment) [Code.cs:28]
            children = await ValidateChildrenAsync(
                workspace,
                items[3],
                childInfo: new[]
                {
                    (43, "\"\""), // |>  return "" [Code.cs:37]
                    (40, "\"defaultstring\""), // |>  return "defaultstring" [Code.cs:34]
                    (35, "\"null\""), // |> return "null" [Code.cs:29]
                });

            await ValidateChildrenEmptyAsync(workspace, children);

            // |> [|CallS(c, CalculateDefault(c) + _adornment)|] [Code.cs:28]
            await ValidateChildrenEmptyAsync(workspace, items[4]);
        }

        [Fact]
        public async Task MethodTracking3()
        {
            var code =
@"
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
}";
            //
            //  |> return [|x|] [Code.cs:18]
            //    |> x = await AddAsync([|x|], x) [Code.cs:17]
            //    |> x = await AddAsync(x, [|x|]) [Code.cs:17]
            //    |> x = await [|AddAsync(x, x)|] [Code.cs:17]
            //      |> [|Task.FromResult|](Add(x, y)) [Code.cs:13]
            //      |> Task.FromResult([|Add(x, y)|]) [Code.cs:13]
            //        |> return x [Code.cs:11]
            using var workspace = TestWorkspace.CreateCSharp(code);
            var initialItems = await GetTrackedItemsAsync(workspace);
            Assert.Equal(1, initialItems.Length);
            ValidateItem(initialItems.Single(), 18, "x"); // |> return [|x|] [Code.cs:18]

            var children = await ValidateChildrenAsync(
                workspace,
                initialItems.Single(),
                childInfo: new[]
                {
                    (17, "x"), // |> x = await AddAsync([|x|], x) [Code.cs:17]
                    (17, "x"), // |> x = await AddAsync(x, [|x|]) [Code.cs:17]
                    (17, "AddAsync(x, x)") // |> x = await [|AddAsync(x, x)|] [Code.cs:17]
                });

            // |> x = await [|AddAsync(x, x)|] [Code.cs:17]
            children = await ValidateChildrenAsync(
                workspace,
                children[2],
                childInfo: new[]
                {
                    (13, "x"), // |> Task.FromResult(Add([|x|], y)) [Code.cs:13]
                    (13, "y"), // |> Task.FromResult(Add(x, [|y|])) [Code.cs:13]
                    (13, "Add(x,y)"),  // |> Task.FromResult([|Add(x, y)|]) [Code.cs:13]
                    (13, "Task.FromResult(Add(x,y))"), // |> [|Task.FromResult|](Add(x, y)) [Code.cs:13]
                });

            // |> [|Task.FromResult|](Add(x, y)) [Code.cs:13]
            await ValidateChildrenEmptyAsync(workspace, children[3]);

            // |> Task.FromResult([|Add(x, y)|]) [Code.cs:13]
            children = await ValidateChildrenAsync(
                workspace,
                children[2],
                childInfo: new[]
                {
                    (10, "x") // |> return x [Code.cs:10]
                });
        }

        [Fact]
        public async Task OutParam()
        {
            var code = @"
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
        object o = ""5"";

        if (TryConvertInt(o, out i))
        {
            Console.WriteLine($$i);
        }
        else
        {
            i = 2;
        }
    }
}";

            //
            //  |> Console.WriteLine($$i); [Code.cs:20]
            //    |> i = [|2|] [Code.cs:24]
            //    |> if (TryConvertInt(o, out [|i|])) [Code.cs:18]
            //      |> if (int.TryParse(o.ToString(), out [|i|])) [Code.cs:5]
            using var workspace = TestWorkspace.CreateCSharp(code);
            var initialItems = await GetTrackedItemsAsync(workspace);

            Assert.Equal(1, initialItems.Length);

            // |> Console.WriteLine($$i);[Code.cs:20]
            ValidateItem(initialItems.Single(), 20, "i");

            var children = await ValidateChildrenAsync(
                workspace,
                initialItems.Single(),
                childInfo: new[]
                {
                    (24, "2"), // |> i = [|2|] [Code.cs:24]
                    (18, "i"), // |> if (TryConvertInt(o, out [|i|])) [Code.cs:18]
                });

            await ValidateChildrenEmptyAsync(workspace, children[0]);

            children = await ValidateChildrenAsync(
                workspace,
                children[1],
                childInfo: new[]
                {
                    (5, "i") // |> if (int.TryParse(o.ToString(), out [|i|])) [Code.cs:5]
                });

            await ValidateChildrenEmptyAsync(workspace, children[0]);
        }
    }
}
