// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class CSharpForEachSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "foreach";

        [WpfFact]
        public async Task InsertForEachSnippetInMethodTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        Ins$$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        foreach (var item in collection)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInMethodItemUsedTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var item = 5;
        Ins$$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        var item = 5;
        foreach (var item1 in collection)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInGlobalContextTest()
        {
            var markupBeforeCommit =
@"Ins$$
";

            var expectedCodeAfterCommit =
@"foreach (var item in collection)
{
    $$
}
";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InserForEachSnippetInConstructorTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public Program()
    {
        $$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public Program()
    {
        foreach (var item in collection)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InserForEachSnippetWithCollectionTest()
        {
            var markupBeforeCommit =
@"using System;
using System.Collections.Generic;

class Program
{
    public Program()
    {
        var list = new List<int> { 1, 2, 3 };
        $$
    }
}";

            var expectedCodeAfterCommit =
@"using System;
using System.Collections.Generic;

class Program
{
    public Program()
    {
        var list = new List<int> { 1, 2, 3 };
        foreach (var item in list)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInLocalFunctionTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var x = 5;
        void LocalMethod()
        {
            $$
        }
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        var x = 5;
        void LocalMethod()
        {
            foreach (var item in collection)
            {
                $$
            }
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInAnonymousFunctionTest()
        {
            var markupBeforeCommit =
@"public delegate void Print(int value);
static void Main(string[] args)
{
    Print print = delegate(int val) {
        $$
    };
}";

            var expectedCodeAfterCommit =
@"public delegate void Print(int value);
static void Main(string[] args)
{
    Print print = delegate(int val) {
        foreach (var item in args)
        {
            $$
        }
    };
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInParenthesizedLambdaExpressionRegularTest()
        {
            var markupBeforeCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    $$
    return x == y;
};";

            var expectedCodeAfterCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    foreach (var item in args)
    {
        $$
    }
    return x == y;
};";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInParenthesizedLambdaExpressionScriptTest()
        {
            var markupBeforeCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    $$
    return x == y;
};";

            var expectedCodeAfterCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    foreach (var item in collection)
    {
        $$
    }
    return x == y;
};";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit, sourceCodeKind: SourceCodeKind.Script);
        }
    }
}
