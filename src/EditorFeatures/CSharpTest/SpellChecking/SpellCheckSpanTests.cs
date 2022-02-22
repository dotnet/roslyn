// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.SpellCheck;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Threading;
using Xunit;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SpellChecking
{
    public abstract class AbstractSpellCheckSpanTests
    {
        protected abstract TestWorkspace CreateWorkspace(string content);

        protected async Task TestAsync(string content)
        {
            using var workspace = CreateWorkspace(content);
            var annotations = workspace.Projects.Single().Documents.Single().AnnotatedSpans;

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var service = document.GetRequiredLanguageService<ISpellCheckSpanService>();

            var actual = await service.GetSpansAsync(document, CancellationToken.None);
            var expected = Flatten(annotations);

            actual = actual.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);
            expected = expected.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);

            Assert.Equal<SpellCheckSpan>(expected, actual);
        }

        private static ImmutableArray<SpellCheckSpan> Flatten(IDictionary<string, ImmutableArray<TextSpan>> annotations)
        {
            return annotations.SelectMany(kvp => kvp.Value.Select(span => new SpellCheckSpan(span, ConvertKind(kvp.Key)))).ToImmutableArray();
        }

        private static SpellCheckKind ConvertKind(string key)
        {
            return key switch
            {
                "Comment" => SpellCheckKind.Comment,
                "Identifier" => SpellCheckKind.Identifier,
                "String" => SpellCheckKind.String,
                _ => throw ExceptionUtilities.UnexpectedValue(key),
            };
        }
    }

    [UseExportProvider]
    public class SpellCheckSpanTests : AbstractSpellCheckSpanTests
    {
        protected override TestWorkspace CreateWorkspace(string content)
            => TestWorkspace.CreateCSharp(content);

        [Fact]
        public async Task TestSingleLineComment1()
        {
            await TestAsync("//{|Comment: Goo|}");
        }

        [Fact]
        public async Task TestSingleLineComment2()
        {
            await TestAsync(@"
//{|Comment: Goo|}");
        }

        [Fact]
        public async Task TestMultiLineComment1()
        {
            await TestAsync("/*{|Comment: Goo |}*/");
        }

        [Fact]
        public async Task TestMultiLineComment2()
        {
            await TestAsync(@"
/*{|Comment:
   Goo
 |}*/");
        }

        [Fact]
        public async Task TestMultiLineComment3()
        {
            await TestAsync(@"
/*{|Comment:
   Goo
 |}");
        }

        [Fact]
        public async Task TestMultiLineComment4()
        {
            await TestAsync(@"
/*{|Comment:|}*/");
        }

        [Fact]
        public async Task TestMultiLineComment5()
        {
            await TestAsync(@"
/*{|Comment:/|}");
        }
    }
}
