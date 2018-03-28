// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources
{
    public class TupleNameCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public TupleNameCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider() => new TupleNameCompletionProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterOpenParen()
        {
            await VerifyItemExistsAsync(@"
class Program
{
    static void Main(string[] args)
    {
        (int word, int zword) t = ($$
    }
}", "word:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterOpenParenWithBraceCompletion()
        {
            await VerifyItemExistsAsync(@"
class Program
{
    static void Main(string[] args)
    {
        (int word, int zword) t = ($$)
    }
}", "word:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterOpenParenInTupleExpression()
        {
            await VerifyItemExistsAsync(@"
class Program
{
    static void Main(string[] args)
    {
        (int word, int zword) t = ($$, zword: 2
    }
}", "word:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterOpenParenInTupleExpressionWithBraceCompletion()
        {
            await VerifyItemExistsAsync(@"
class Program
{
    static void Main(string[] args)
    {
        (int word, int zword) t = ($$, zword: 2
    }
}", "word:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterComma()
        {
            await VerifyItemExistsAsync(@"
class Program
{
    static void Main(string[] args)
    {
        (int word, int zword) t = (1, $$
    }
}", "zword:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterCommaWithBraceCompletion()
        {
            await VerifyItemExistsAsync(@"
class Program
{
    static void Main(string[] args)
    {
        (int word, int zword) t = (1, $$)
    }
}", "zword:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InTupleAsArgument()
        {
            await VerifyItemExistsAsync(@"
class Program
{
    static void Main((int word, int zword) args)
    {
         Main(($$))
    }
}", "word:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MultiplePossibleTuples()
        {
            var markup = @"
class Program
{
    static void Main((int number, int znumber) args) { }
    static void Main((string word, int zword) args) {
        Main(($$
    }
}";
            await VerifyItemExistsAsync(markup, "word:");
            await VerifyItemExistsAsync(markup, "number:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MultiplePossibleTuplesAfterComma()
        {
            var markup = @"
class Program
{
    static void Main((int number, int znumber) args) { }
    static void Main((string word, int zword) args) {
        Main((1, $$
    }
}";
            await VerifyItemExistsAsync(markup, "zword:");
            await VerifyItemExistsAsync(markup, "znumber:");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AtIndexGreaterThanNumberOfTupleElements()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        (int word, int zword) t = (1, 2, 3, 4, $$ 
    }
}";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConvertCastToTupleExpression()
        {
            var markup = @"
class C
{
    void goo()
    {
        (int goat, int moat) x = (g$$)1;
    }
}";
            await VerifyItemExistsAsync(markup, "goat:");
        }
    }
}
