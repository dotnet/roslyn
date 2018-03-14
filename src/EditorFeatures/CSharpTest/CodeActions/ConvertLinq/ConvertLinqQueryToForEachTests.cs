// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertLinq
{
    public class ConvertLinqQueryToForEachTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
          => new CodeAnalysis.CSharp.ConvertLinq.CSharpConvertLinqQueryToLinqMethodProvider();

        // TODO anonymous type test
        // a. for local function, expected - impossible.
        // b. for some other refactoring where it is possible.
        // TODO anonymous type inside a type test
        // a. for local function, expected - impossible.
        // b. for some other refactoring where it is possible.
        // TODO test for multiple assignment expression
        // TODO consider method/property declaration (not local), i.e. enumerable is a property
        // TODO consider ienumerable return as an embedded function result
        // TODO consider ienumerable return as an embedded local function result
        // TODO multiple variable declaration
        // TODO why it works with void M() where it requires 'nums' inside.
        // TODO mymethod() => (from a in b select a).ToList();
        // TODO list = (from a in q select a * a).ToList(), max = list.Max();
        // TODO return (from a in q select a * a).ToList(), 1);
        // TODO what if a.b.c = (from a in q select a * a).ToList();
        // TODO .Count(x => x > 5)

        [Fact]
        public async Task Conversion_MultipleReferences()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    bool M(IEnumerable<int> nums)
    {
        var q = [||]from int n1 in nums 
                from int n2 in nums
                select n1;

        return q.Any() && q.All();
    }
}
";
            var output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    bool M(IEnumerable<int> nums)
    {
        IEnumerable<Int32> localFunction()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        var q = localFunction();

        return q.Any() && q.All();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_ReturnIEnumerable()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        return [||]from int n1 in nums 
                 from int n2 in nums
                 select n1;
    }
}
";

            var output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                yield return n1;
            }
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_AssignAndReturnIEnumerable()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        var q = [||]from int n1 in nums
                from int n2 in nums
                select n1;
        return q;
    }
}
";

            var output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        IEnumerable<Int32> localFunction()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        var q = localFunction();
        return q;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        // TODO list or list1?
        [Fact]
        public async Task Conversion_AssignList()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = ([||]from int n1 in nums 
                 from int n2 in nums
                 select n1).ToList();
        return list;
    }
}
";

            var output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = new List<Int32>();
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }

        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_ReturnList()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        return ([||]from int n1 in nums 
                 from int n2 in nums
                 select n1).ToList();
    }
}
";

            var output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = new List<Int32>();
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }

        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_ReturnListNameGeneration()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = new List<int>();
        return ([||]from int n1 in nums 
                 from int n2 in nums
                 select n1).ToList();
    }
}
";

            var output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = new List<int>();
        var list1 = new List<Int32>();
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list1.Add(n1);
            }
        }

        return list1;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_AssignCount()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        var cnt = ([||]from int n1 in nums 
                 from int n2 in nums
                 select n1).Count();
        return cnt;
    }
}
";

            var output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        var cnt = 0;
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                cnt++;
            }
        }

        return cnt;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_ReturnCount()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        return ([||]from int n1 in nums 
                 from int n2 in nums
                 select n1).Count();
    }
}
";

            var output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        var count = 0;
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                count++;
            }
        }

        return count;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_ReturnCountNameGeneration()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int count = 1;
        return ([||]from int n1 in nums 
                 from int n2 in nums
                 select n1).Count();
    }
}
";

            var output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int count = 1;
        var count1 = 0;
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                count1++;
            }
        }

        return count1;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_ReturnFirstOrDefault()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    T M<T>(IEnumerable<T> nums)
    {
        return ([||]from int n1 in nums 
                 from int n2 in nums
                 select n1).FirstOrDefault();
    }
}
";

            var output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    T M<T>(IEnumerable<T> nums)
    {
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                return n1;
            }
        }

        return default; // TODO do we need to return null in some cases?
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_UsageInForEach()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        var q = [||]from int n1 in nums 
                from int n2 in nums
                select n1;
        foreach(var b in q)
        {
            Console.WriteLine(b);
        }
    }
}
";

            var output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        IEnumerable<Int32> localFunction()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        var q = localFunction();
        foreach(var b in q)
        {
            Console.WriteLine(b);
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_UsageInForEachSameVariableName()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        var q = [||]from int n1 in nums 
                from int n2 in nums
                select n1;
        foreach(var n1 in q)
        {
            Console.WriteLine(n1);
        }
    }
}
";

            var output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        IEnumerable<Int32> localFunction()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        var q = localFunction();
        foreach(var n1 in q)
        {
            Console.WriteLine(n1);
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_LinqDefinedInForEach()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        foreach(var b in [||]from int n1 in nums 
                from int n2 in nums
                select n1)
        {
            Console.WriteLine(b);
        }
    }
}
";

            var output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                var b = n1;
                Console.WriteLine(b);
            }
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_LinqDefinedInForEachSameVariableName()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        foreach(var n1 in [||]from int n1 in nums 
                          from int n2 in nums
                          select n1)
        {
            Console.WriteLine(n1);
        }
    }
}
";

            var output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                Console.WriteLine(n1);
            }
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task Conversion_CallingMethodWithIEnumerable()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        var q = [||]from int n1 in nums 
                from int n2 in nums
                select n1;
        N(q);
    }

    void N(IEnumerable<int> q) {}
}
";

            var output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        IEnumerable<Int32> localFunction()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        var q = localFunction();
        N(q);
    }

    void N(IEnumerable<int> q) {}
}
";
            await TestInRegularAndScriptAsync(source, output);
        }


        [Fact]
        // TODO Int32 to int
        public async Task Conversion_AssignmentExpression()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        IEnumerable<int> q;
        q = [||]from int n1 in nums 
                from int n2 in nums
                select n1;

        N(q);
    }

    void N(IEnumerable<int> q) {}
}
";

            var output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<T> nums)
    {
        IEnumerable<int> q;
        IEnumerable<Int32> localFunction()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        q = localFunction();

        N(q);
    }

    void N(IEnumerable<int> q) {}
}
";
            await TestInRegularAndScriptAsync(source, output);
        }
    }
}
