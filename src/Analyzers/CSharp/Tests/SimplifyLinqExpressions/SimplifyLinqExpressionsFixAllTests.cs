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
@"
using System;
using System.Linq;
using System.Collections.Generic;
 
class C
{
    IEnumerable<string> test = new List<string> { 'hello', 'world', '!' };
    var test1 = {|FixAllInDocument:test.Where(x => x.Equals('!')).Any()|};
    var test2 = test.Where(x => x.Equals('!')).SingleOrDefault();
    var test3 = test.Where(x => x.Equals('!')).Last();
    var test4 = test.Where(x => x.Equals('!')).Count();
    var test5 = test.Where(x => x.Equals('!')).FirstOrDefault();
}",
@"
using System;
using System.Linq;
using System.Collections.Generic;
 
class C
{
    IEnumerable<string> test = new List<string> { 'hello', 'world', '!' };
    var test1 = test.Any(x => x.Equals('!'));
    var test2 = test.SingleOrDefault(x => x.Equals('!'));
    var test3 = test.Last(x => x.Equals('!'));
    var test4 = test.Count(x => x.Equals('!'));
    var test5 = test.FirstOrDefault(x => x.Equals('!'));
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task NestedInDocument()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
using System.Linq;
using System.Collections.Generic;
 
class C
{
    IEnumerable<string> test = new List<string> { 'hello', 'world', '!' };
    var test1 = {|FixAllInDocument:test.Where(x => x.Equals('!')).Any()|};
    var test2 = test.Where(x => x.Equals('!')).SingleOrDefault();
    var test3 = test.Where(x => x.Equals('!')).Last();
    var test4 = test.Where(x => x.Equals('!')).Count();
    var test5 = test.Where(a => a.Where(s => s.Equals('hello').FirstOrDefault()).Equals('hello')).FirstOrDefault();
}",
@"
using System;
using System.Linq;
using System.Collections.Generic;
 
class C
{
    IEnumerable<string> test = new List<string> { 'hello', 'world', '!' };
    var test1 = test.Any(x => x.Equals('!'));
    var test2 = test.SingleOrDefault(x => x.Equals('!'));
    var test3 = test.Last(x => x.Equals('!'));
    var test4 = test.Count(x => x.Equals('!'));
    var test5 = test.FirstOrDefault(a => a.Where(s => s.Equals('hello').FirstOrDefault()).Equals('hello'));
}");
        }
    }
}
