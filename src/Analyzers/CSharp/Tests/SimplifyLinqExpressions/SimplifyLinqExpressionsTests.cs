// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpressions;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.SimplifyLinqExpressions
{
    public partial class SimplifyLinqExpressionsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpSimplifyLinqExpressionsDiagnosticAnalyzer(), new CSharpSimplifyLinqExpressionsCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestEnumerableTypeSingle()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = [||]Data().Where(x => x==1).Single();
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().Single(x => x==1);
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);

        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestEnumerableTypeSingOrDefault()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = [||]Data().Where(x => x==1).SingleOrDefault();
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().SingleOrDefault(x => x==1);
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);

        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestEnumerableTypeFirst()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = [||]Data().Where(x => x==1).First();
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().First(x => x==1);
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);

        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestEnumerableTypeFirstOrDefault()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = [||]Data().Where(x => x==1).FirstOrDefault();
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().FirstOrDefault(x => x==1);
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);

        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestEnumerableTypeLast()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = [||]Data().Where(x => x==1).Last();
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().Last(x => x==1);
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);

        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestEnumerableTypeLastOrDefault()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = [||]Data().Where(x => x==1).LastOrDefault();
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().LastOrDefault(x => x==1);
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);

        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestEnumerableTypeAny()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = [||]Data().Where(x => x==1).Any();
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().Any(x => x==1);
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);

        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestEnumerableTypeCount()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = [||]Data().Where(x => x==1).Count();
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().Count(x => x==1);
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);

        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestEnumerableFromQueryType()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    private static IEnumerable<int> test1 = from value in Enumerable.Range(0, 10) select value;
    private var test2 = [||]test1.Where(x => x==1).First();
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    private static IEnumerable<int> test1 = from value in Enumerable.Range(0, 10) select value;
    private var test2 = test1.First(x => x==1);
}";
            await TestInRegularAndScriptAsync(source, fixedSource);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestEnumerableListType()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

class Test
{
    private static IEnumerable<string> _test1 = new List<string> { 'hello', 'world', '!' };
    private bool _test2 = [||]_test1.Where(x => x.Equals('!')).Any();
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

class Test
{
    private static IEnumerable<string> _test1 = new List<string> { 'hello', 'world', '!' };
    private bool _test2 = _test1.Any(x => x.Equals('!'));
}";
            await TestInRegularAndScriptAsync(source, fixedSource);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestComplexLambda()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = [||]Data().Where(x => 
        { 
            Console.Write(x);
            return x == 1;
        }).LastOrDefault();
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().LastOrDefault(x => 
        { 
            Console.Write(x);
            return x == 1;
        });
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);

        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestOutsideFunctionCallLambda()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{
    public static bool FooTest(string input)
    {
        return true;
    }

    static IEnumerable<string> test = new List<string> { 'hello', 'world', '!' };
    int result = [||]test.Where(x => FooTest(x)).Count();
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{
    public static bool FooTest(string input)
    {
        return true;
    }

    static IEnumerable<string> test = new List<string> { 'hello', 'world', '!' };
    int result = test.Count(x => FooTest(x));
}";
            await TestInRegularAndScriptAsync(source, fixedSource);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestQueryable()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
namespace demo
{
    class Test
    {
        static List<int> testvar1 = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
        static IQueryable<int> testvar2 = testvar1.AsQueryable().Where(x => x % 2 == 0);
        int output = [||]testvar2.Where(x => x == 4).Count();
    }
}";
            await TestMissingInRegularAndScriptAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestUserDefinedWhere()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
namespace demo
{
    class Test
    {
        public class TestClass4
        {
            private string test;
            public TestClass4() => test = 'hello';

            public TestClass4 Where(Func<string, bool> input)
            {
                return this;
            }

            public string Single()
            {
                return test;
            }
        }
        static void Main()
        {
            TestClass4 Test1 = new TestClass4();
            TestClass4 test = [||]Test1.Where(y => true);
        }
    }
}";
            await TestMissingInRegularAndScriptAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestArgumentsInSecondCall()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
namespace demo
{
    class Test
    {
        static IEnumerable<string> test1 = new List<string> { 'hello', 'world', '!' };
        bool test2 = [||]test1.Where(x => x == '!').Any(x => x.Length == 1);
    }
}";
            await TestMissingInRegularAndScriptAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestUnsupportedFunction()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
namespace demo
{
    class Test
    {
        static IEnumerable<int> test1 = new List<int> { 3, 12, 4, 6, 20 };
        int test2 = [||]test1.Where(x => x > 0).Max();
    }
}";
            await TestMissingInRegularAndScriptAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestSelectFunction()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
namespace demo
{
    class Test
    {
        static IEnumerable<int> test1 = new List<int> { 3, 12, 4, 6, 20 };
        int test2 = [||]test1.Select(x => x > 0).Single();
    }
}";
            await TestMissingInRegularAndScriptAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestExpressionTreeInput()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

class Test
{
    void Main()
    {
        string[] places = { 'Beach', 'Pool', 'Store', 'House',
                   'Car', 'Salon', 'Mall', 'Mountain'};

        IQueryable<String> queryableData = companies.AsQueryable<string>();
        ParameterExpression pe = Expression.Parameter(typeof(string), 'place');

        Expression left = Expression.Call(pe, typeof(string).GetMethod('ToLower', System.Type.EmptyTypes));
        Expression right = Expression.Constant('coho winery');
        Expression e1 = Expression.Equal(left, right);

        left = Expression.Property(pe, typeof(string).GetProperty('Length'));
        right = Expression.Constant(16, typeof(int));
        Expression e2 = Expression.GreaterThan(left, right);

        Expression predicateBody = Expression.OrElse(e1, e2);
        Expression<Func<int, bool>> lambda1 = num => num < 5;

        string result = [||]queryableData.Where(Expression.Lambda<Func<string, bool>>(predicateBody, new ParameterExpression[] { pe })).First();
    }
}";
            await TestMissingInRegularAndScriptAsync(source);
        }
    }
}
