// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;

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
            var initialItems = await GetTrackedItemsAsync(workspace);

            //
            // property S 
            //  |> S = s [Code.cs:7]
            //  |> public string S { get; set; } [Code.cs:3]
            //
            Assert.Equal(2, initialItems.Length);
            ValidateItem(initialItems[0], 7);
            ValidateItem(initialItems[1], 3);
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
    }
}
