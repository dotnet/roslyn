// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ValueTracking;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ValueTracking
{
    [UseExportProvider]
    internal class CSharpValueTrackingTests : AbstractBaseValueTrackingTests
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
            //  |> S = s [Code.cs:8]
            //  |> public string S { get; set; } [Code.cs:4]
            //
            Assert.Equal(2, initialItems.Length);
            Assert.Equal("S = s", GetText(initialItems[0]));
            Assert.Equal(@"public string S { get; set; } = """"", GetText(initialItems[1]));
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
            //  |> _s = s [Code.cs:8]
            //  |> _s = "" [Code.cs:4]
            //
            Assert.Equal(2, initialItems.Length);
            Assert.Equal("_s = s", GetText(initialItems[0]));

            // This is displaying the initializer but not the full line
            // TODO: Fix this to be the whole line?
            Assert.Equal(@"_s = """"", GetText(initialItems[1]));
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
            //  |> z += y [Code.cs:7]
            //  |> var z = x [Code.cs:6]
            //
            Assert.Equal(2, initialItems.Length);
            Assert.Equal(@"z += y", GetText(initialItems[0]));
            Assert.Equal("var z = x", GetText(initialItems[1]));
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
            //  |> x += y [Code.cs:6]
            //  |> Add(int x, int y) [Code.cs:4]
            //
            Assert.Equal(2, initialItems.Length);
            Assert.Equal(@"x += y", GetText(initialItems[0]));

            // This is not whole line, but shows the full variable definition.
            // Display may need to adjust for this, but the service is providing
            // the correct information here
            Assert.Equal("int x", GetText(initialItems[1]));
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
