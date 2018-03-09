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

        [Fact]
        public async Task NoConversion_MultipleReferences()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    bool M(IEnumerable<int> nums)
    {
        var q =  from int n1 in nums 
                 from int n2 in nums
                 select n1;
        
            return q.Any() && q.All();
    }
}
";

            await TestMissingInRegularAndScriptAsync(source);
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
        return from int n1 in nums 
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
        foreach(int n1 in nums)
        {
            foreach(int n2 in nums)
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
        var q = from int n1 in nums 
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
        foreach(int n1 in nums)
        {
            foreach(int n2 in nums)
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
        public async Task Conversion_AssignList()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = (from int n1 in nums 
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
        var list = new List<int>();
        foreach(int n1 in nums)
        {
            foreach(int n2 in nums)
            {
                yield return n1;
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
        return (from int n1 in nums 
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
        foreach(int n1 in nums)
        {
            foreach(int n2 in nums)
            {
                yield return n1;
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
        return (from int n1 in nums 
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
        var list1 = new List<int>();
        foreach(int n1 in nums)
        {
            foreach(int n2 in nums)
            {
                yield return n1;
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
        var cnt = (from int n1 in nums 
                 from int n2 in nums
                 select n1).Count;
        return cnt;
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
        var cnt = 0;
        foreach(int n1 in nums)
        {
            foreach(int n2 in nums)
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
        return (from int n1 in nums 
                 from int n2 in nums
                 select n1).Count;
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
        var count = 0;
        foreach(int n1 in nums)
        {
            foreach(int n2 in nums)
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
        return (from int n1 in nums 
                 from int n2 in nums
                 select n1).Count;
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
        int count = 1;
        var count1 = 0;
        foreach(int n1 in nums)
        {
            foreach(int n2 in nums)
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
        return (from int n1 in nums 
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
        foreach(int n1 in nums)
        {
            foreach(int n2 in nums)
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
    }
}
