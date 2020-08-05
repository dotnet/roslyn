// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.SimplifyLinqExpressions
{
    public partial class SimplifyLinqExpressionsTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    IEnumerable<string> test = new List<string> { 'hello', 'world', '!' };
    var test1 = {|FixAllInDocument:test.Where(x => x == '!').Any()|};
    var test2 = test.Where(x => x == '!').SingleOrDefault();
    var test3 = test.Where(x => x == '!').Last();
    var test4 = test.Where(x => x == '!').Count();
    var test5 = test.Where(x => x == '!').FirstOrDefault();
}",
@"class C
{
    IEnumerable<string> test = new List<string> { 'hello', 'world', '!' };
    var test1 = test.Any(x => x == '!');
    var test2 = test.SingleOrDefault(x => x == '!');
    var test3 = test.Last(x => x == '!');
    var test4 = test.Count(x => x == '!');
    var test5 = test.FirstOrDefault(x => x == '!');
}");
        }
    }
}
