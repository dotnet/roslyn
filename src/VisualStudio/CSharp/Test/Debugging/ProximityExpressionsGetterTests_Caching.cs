// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.CSharp.Debugging;
using Microsoft.VisualStudio.LanguageServices.Implementation.Debugging;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Debugging
{
    public partial class ProximityExpressionsGetterTests
    {
        private async Task TestCachingAsync(string markup, params string[][] expectedArray)
        {
            using (var workspace = await TestWorkspaceFactory.CreateCSharpAsync(markup))
            {
                var testDocument = workspace.Documents.Single();
                var spans = testDocument.AnnotatedSpans;
                var snapshot = testDocument.TextBuffer.CurrentSnapshot;
                var languageDebugInfo = new CachedProximityExpressionsGetter(new CSharpProximityExpressionsService());
                var document = workspace.CurrentSolution.GetDocument(testDocument.Id);

                for (var i = 0; i < expectedArray.Length; i++)
                {
                    int position;
                    var key = spans.Keys.FirstOrDefault(k => k.StartsWith(i + "-", StringComparison.Ordinal));
                    if (key != null)
                    {
                        var parts = key.Split('-');
                        if (parts[1] == "OnDebugModeChanged")
                        {
                            languageDebugInfo.OnDebugModeChanged((DebugMode)Enum.Parse(typeof(DebugMode), parts[2]));
                        }

                        position = spans[key].First().Start;
                    }
                    else
                    {
                        position = spans[i.ToString()].First().Start;
                    }

                    var expected = expectedArray[i];

                    var result = await languageDebugInfo.DoAsync(document, position, string.Empty, CancellationToken.None);
                    AssertEx.Equal(expectedArray[i], result);
                }
            }
        }

        // The intention of these tests is to verify that a local variable
        // comes into the purview of the debugger when stepping reaches the
        // statement declaring the local. We do that by analyzing whether an
        // expression containing the local can be successfully bound at the
        // beginning of various statements.
        //
        // These three tests are disabled right now because turning on the
        // detection of "local variable used before it is declared" breaks
        // these tests. The tests assume that a local variable that is in
        // scope can be observed to be used without error starting from
        // the start of the declaration, but the local cannot be used until
        // lexically after its declaration.
        //
        // We should figure out some better way to test the feature.
        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestCaching1()
        {
            var input = @"
class Class
{
    void Method(string args)
    {
        {|0:|}
        {|1:|}int i = 0;
        {|2:|}int j = 1, k = 2;
    }
}";

            await TestCachingAsync(input, new[] { "args", "this" }, new[] { "i", "args", "this" }, new[] { "i", "j", "k", "this", "args" });
        }

        [WorkItem(538259)]
        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestCaching2()
        {
            var input = @"
class Program
{
    static void Main(string[] args)
    {
        Foo();
    }
    private static void Foo()
    {
        {|0:|}int i = 0;
        {|1:|}int j = 1;
        {|2:|}{
            {|3:|}int k = 2;
        {|4:|}}
    {|5:|}}
}
";

            await TestCachingAsync(input, new[] { "i" }, new[] { "i", "j" }, new[] { "j", "i" }, new[] { "k", "j" }, new[] { "k", "j" }, new[] { "j" });
        }

        [WorkItem(538259)]
        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestCaching3()
        {
            var input = @"
class Program
{
    static void Main(string[] args)
    {
        Foo();
    }
    private static void Foo()
    {
        {|0:|}int i = 0;
        {|1:|}int j = 1;
        {|2:|}{
            {|3-OnDebugModeChanged-Design:|}int k = 2;
        {|4:|}}
    {|5:|}}
}
";

            await TestCachingAsync(input, new[] { "i" }, new[] { "i", "j" }, new[] { "j", "i" }, new[] { "k", }, new[] { "k", }, Array.Empty<string>());
        }
    }
}
