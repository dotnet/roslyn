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
    public class CSharpValueTrackingTests
    {
        private static async Task<ImmutableArray<ValueTrackedItem>> GetTrackedItemsAsync(TestWorkspace testWorkspace, CancellationToken cancellationToken = default)
        {
            var cursorDocument = testWorkspace.DocumentWithCursor;
            var document = testWorkspace.CurrentSolution.GetRequiredDocument(cursorDocument.Id);
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken);
            var textSpan = new TextSpan(cursorDocument.CursorPosition!.Value, 0);
            var location = Location.Create(syntaxTree, textSpan);
            var symbol = await GetSelectedSymbolAsync(textSpan, document, cancellationToken);
            var service = testWorkspace.Services.GetRequiredService<IValueTrackingService>();
            return await service.TrackValueSourceAsync(testWorkspace.CurrentSolution, location, symbol, cancellationToken);

        }

        private static async Task<ISymbol> GetSelectedSymbolAsync(TextSpan textSpan, Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectedNode = root.FindNode(textSpan);

            Assert.NotNull(selectedNode);

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var selectedSymbol =
                semanticModel.GetSymbolInfo(selectedNode, cancellationToken).Symbol
                ?? semanticModel.GetDeclaredSymbol(selectedNode, cancellationToken);

            Assert.NotNull(selectedSymbol);
            return selectedSymbol!;
        }

        private static string GetText(ValueTrackedItem item)
        {
            var sourceTree = item.Location.SourceTree;
            var span = item.Location.SourceSpan;

            Assert.NotNull(sourceTree);
            if (sourceTree!.TryGetText(out var text))
            {
                return text!.GetSubText(span).ToString();
            }

            return sourceTree!.ToString();
        }

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
