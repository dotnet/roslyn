// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class CSharpForSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "for";

        [WpfFact]
        public async Task InsertForSnippetInMethodTest()
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
        for (int i = 0; i < length; i++)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForSnippetInMethodUsedIncrementorTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var i = 0;
        Ins$$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        var i = 0;
        for (int j = 0; j < length; j++)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForSnippetInMethodUsedIncrementorsTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var i, j, k, a, b, c = 0;
        Ins$$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        var i, j, k, a, b, c = 0;
        for (int i1 = 0; i1 < length; i1++)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForSnippetInGlobalContextTest()
        {
            var markupBeforeCommit =
@"Ins$$
";

            var expectedCodeAfterCommit =
@"for (int i = 0; i < length; i++)
{
    $$
}
";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForSnippetInConstructorTest()
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
        for (int i = 0; i < length; i++)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForSnippetInLocalFunctionTest()
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
            for (global::System.Int32 i = 0; (i) < (length); i++)
            {
                $$
            }
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForSnippetInAnonymousFunctionTest()
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
        for (global::System.Int32 i = 0; (i) < (length); i++)
        {
            $$
        }
    };

}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForSnippetInParenthesizedLambdaExpressionTest()
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
    for (global::System.Int32 i = 0; (i) < (length); i++)
    {
        $$
    }

    return x == y;
};";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }

    public class CSharpForSnippetPreferVarCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "for";

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertForSnippetInMethodTest()
        {
            var markupBeforeCommit =
                $@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
    <Document FilePath=""/0/Test0.cs"">
class Program
{{
    public void Method()
    {{
        Ins$$
    }}
}}</Document>
<AnalyzerConfigDocument FilePath=""/.editorconfig"">
root = true
 
[*]
# IDE0008: Use explicit type
csharp_style_var_for_built_in_types = true
    </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            var expectedCodeAfterCommit =
                $@"
class Program
{{
    public void Method()
    {{
        for (var i = 0; i < length; i++)
        {{
            $$
        }}
    }}
}}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
