// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    public class CSharpIfSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "if";

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertIfSnippetInMethodTest()
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
        if (true)
        {$$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertIfSnippetInGlobalContextTest()
        {
            var markupBeforeCommit =
@"Ins$$
";

            var expectedCodeAfterCommit =
@"if (true)
{$$
}
";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoIfSnippetInBlockNamespaceTest()
        {
            var markupBeforeCommit =
@"
namespace Namespace
{
    $$
    class Program
    {
        public async Task MethodAsync()
        {
        }
    }
}";
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoIfSnippetInFileScopedNamespaceTest()
        {
            var markupBeforeCommit =
@"
namespace Namespace;
$$
class Program
{
    public async Task MethodAsync()
    {
    }
}
";
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertIfSnippetInConstructorTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public Program()
    {
        var x = 5;
        $$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public Program()
    {
        var x = 5;
        if (true)
        {$$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertIfSnippettInLocalFunctionTest()
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
            if (true)
            {$$
            }
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertIfSnippetInAnonymousFunctionTest()
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
        if (true)
        {$$
        }
    };

}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertIfSnippetInParenthesizedLambdaExpressionTest()
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
    if (true)
    {$$
    }

    return x == y;
};";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoIfSnippetInSwitchExpression()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
       var operation = 2;  
  
        var result = operation switch  
        {
            $$
            1 => ""Case 1"",  
            2 => ""Case 2"",  
            3 => ""Case 3"",  
            4 => ""Case 4"",  
        };
    }
}";
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoIfSnippetInSingleLambdaExpression()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
       Func<int, int> f = x => $$;
    }
}";
            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoIfSnippetInStringTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var str = ""$$"";
    }
}";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoIfSnippetInObjectInitializerTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var str = new Test($$);
    }
}

class Test
{
    private string val;

    public Test(string val)
    {
        this.val = val;
    }
}";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoIfSnippetInParameterListTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method(int x, $$)
    {
    }
}";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoIfSnippetInRecordDeclarationTest()
        {
            var markupBeforeCommit =
@"public record Person
{
    $$
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
};";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoIfSnippetInVariableDeclarationTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var x = $$
    }
}";

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertIfSnippetWithInvocationBeforeAndAfterCursorTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        Wr$$Blah
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        if (true)
        {$$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertIfSnippetWithInvocationUnderscoreBeforeAndAfterCursorTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        _Wr$$Blah_
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        if (true)
        {$$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
