// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.SimplifyLinqExpression
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpSimplifyLinqExpressionDiagnosticAnalyzer,
        CSharpSimplifyLinqExpressionCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
    public partial class CSharpSimplifyLinqExpressionTests
    {
        [Fact]
        public async Task FixAllInDocument()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    static void M()
    {
        IEnumerable<string> test = new List<string> { ""hello"", ""world"", ""!"" };
        var test1 = [|test.Where(x => x.Equals('!')).Any()|];
        var test2 = [|test.Where(x => x.Equals('!')).SingleOrDefault()|];
        var test3 = [|test.Where(x => x.Equals('!')).Last()|];
        var test4 = [|test.Where(x => x.Equals('!')).Count()|];
        var test5 = [|test.Where(x => x.Equals('!')).FirstOrDefault()|];
    }
}",
                FixedCode = @"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    static void M()
    {
        IEnumerable<string> test = new List<string> { ""hello"", ""world"", ""!"" };
        var test1 = test.Any(x => x.Equals('!'));
        var test2 = test.SingleOrDefault(x => x.Equals('!'));
        var test3 = test.Last(x => x.Equals('!'));
        var test4 = test.Count(x => x.Equals('!'));
        var test5 = test.FirstOrDefault(x => x.Equals('!'));
    }
}",
            }.RunAsync();
        }

        [Fact]
        public async Task FixAllInDocumentExplicitCall()
        {

            var testCode = @"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    static void M()
    {
        IEnumerable<string> test = new List<string> { ""hello"", ""world"", ""!"" };
        var test1 = [|Enumerable.Where(test, x => x.Equals(""!"")).Any()|];
        var test2 = [|Enumerable.Where(test, x => x.Equals(""!"")).SingleOrDefault()|];
        var test3 = [|Enumerable.Where(test, x => x.Equals(""!"")).Last()|];
        var test4 = [|Enumerable.Where(test, x => x.Equals(""!"")).Count()|];
        var test5 = [|Enumerable.Where(test, x => x.Equals(""!"")).FirstOrDefault()|];
    }
}";
            var fixedCode = @"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    static void M()
    {
        IEnumerable<string> test = new List<string> { ""hello"", ""world"", ""!"" };
        var test1 = Enumerable.Any(test, x => x.Equals(""!""));
        var test2 = Enumerable.SingleOrDefault(test, x => x.Equals(""!""));
        var test3 = Enumerable.Last(test, x => x.Equals(""!""));
        var test4 = Enumerable.Count(test, x => x.Equals(""!""));
        var test5 = Enumerable.FirstOrDefault(test, x => x.Equals(""!""));
    }
}";
            await VerifyCS.VerifyCodeFixAsync(testCode, fixedCode);
        }

        [Fact]
        public async Task NestedInDocument()
        {

            await new VerifyCS.Test
            {
                TestCode = @"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    static void M()
    {
        var test = new List<string> { ""hello"", ""world"", ""!"" };
        var test1 = [|test.Where(x => x.Equals('!')).Any()|];
        var test2 = [|test.Where(x => x.Equals('!')).SingleOrDefault()|];
        var test3 = [|test.Where(x => x.Equals('!')).Last()|];
        var test4 = test.Where(x => x.Equals('!')).Count();
        var test5 = from x in test where x.Equals('!') select x;
        var test6 = [|test.Where(a => [|a.Where(s => s.Equals(""hello"")).FirstOrDefault()|].Equals(""hello"")).FirstOrDefault()|];
    }
}",
                FixedCode = @"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    static void M()
    {
        var test = new List<string> { ""hello"", ""world"", ""!"" };
        var test1 = test.Any(x => x.Equals('!'));
        var test2 = test.SingleOrDefault(x => x.Equals('!'));
        var test3 = test.Last(x => x.Equals('!'));
        var test4 = test.Where(x => x.Equals('!')).Count();
        var test5 = from x in test where x.Equals('!') select x;
        var test6 = test.FirstOrDefault(a => a.FirstOrDefault(s => s.Equals(""hello"")).Equals(""hello""));
    }
}",
            }.RunAsync();
        }
    }
}
