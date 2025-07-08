// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.ExplicitAllocationAnalyzer,
    Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers.CodeFixes.AvoidAllocationWithArrayEmptyCodeFix>;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers.UnitTests
{
    public class AvoidAllocationWithArrayEmptyCodeFixTests
    {
        [Theory]
        [InlineData("IEnumerable<int>")]
        [InlineData("IReadOnlyList<int>")]
        [InlineData("IReadOnlyCollection<int>")]
        public async Task ShouldReplaceEmptyListCreationWithArrayEmptyWithReturnTypeAsync(string type)
        {
            await VerifyCS.VerifyCodeFixAsync($@"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{{
    class SampleClass
    {{
        [PerformanceSensitive(""uri"")]
        public {type} DoSomething()
        {{
            return new List<int>();
        }}
    }}
}}", VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(13, 20, 13, 35), $@"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{{
    class SampleClass
    {{
        [PerformanceSensitive(""uri"")]
        public {type} DoSomething()
        {{
            return Array.Empty<int>();
        }}
    }}
}}");
        }

        [Fact]
        public async Task ShouldReplaceEmptyListCreationWithArrayEmptyWhenReturnFromMethodArrayAsync()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public int[] DoSomething()
        {
            return new int[0];
        }
    }
}", VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(13, 20, 13, 30), @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public int[] DoSomething()
        {
            return Array.Empty<int>();
        }
    }
}");
        }

        [Fact]
        public async Task ShouldReplaceEmptyLisCreationWithArrayEmptyForArrowExpressionAsync()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething => new List<int>();
    }
}", VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(11, 48, 11, 63), @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething => Array.Empty<int>();
    }
}");
        }
        [Fact]
        public async Task ShouldReplaceEmptyListCreationWithArrayEmptyForReadonlyPropertyAsync()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething { get {return new List<int>();}}
    }
}", VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(11, 59, 11, 74), @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething { get {return Array.Empty<int>();}}
    }
}");
        }
        [Fact]
        public async Task ShouldReplaceEmptyListWithCreationWithPredefinedSizeWithArrayEmptyAsync()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething()
        {
            return new List<int>(10);
        }
    }
}", VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(13, 20, 13, 37), @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething()
        {
            return Array.Empty<int>();
        }
    }
}");
        }
        [Fact]
        public async Task ShouldNotProposeCodeFixWhenNonEmptyListCreatedAsync()
        {
            var code = @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething()
        {
            return new List<int>(){1, 2};
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(13, 20, 13, 41), code);
        }
        [Fact]
        public async Task ShouldNotProposeCodeFixWhenReturnTypeInheritFormEnumerableAsync()
        {
            var code = @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public List<int> DoSomething()
        {
            return new List<int>();
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(13, 20, 13, 35), code);
        }
        [Fact]
        public async Task ShouldNotProposeCodeFixWhenForCollectionCreationUsingCopyConstructorAsync()
        {
            var code = @"
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething()
        {
            var innerList = new List<int>(){1, 2};
            return new ReadOnlyCollection<int>(innerList);
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code,
                new[]
                {
                    VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(14, 29, 14, 50),
                    VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(15, 20, 15, 58),
                }, code);
        }
        [Fact]
        public async Task ShouldReplacEmptyCollectionCreationWithArrayEmptyAsync()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething()
        {
            return new Collection<int>();
        }
    }
}", VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(14, 20, 14, 41), @"
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething()
        {
            return Array.Empty<int>();
        }
    }
}");
        }
        [Fact]
        public async Task ShouldReplaceEmptyArrayCreationWithArrayEmptyAsync()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething()
        {
            return new int[0];
        }
    }
}", VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(13, 20, 13, 30), @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething()
        {
            return Array.Empty<int>();
        }
    }
}");
        }
        [Fact]
        public async Task ShouldNotProposeCodeFixWhenNonEmptyArrayCreationAsync()
        {
            var code = @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething()
        {
            return new int[]{1, 2};
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(13, 20, 13, 35), code);
        }
        [Fact]
        public async Task ShouldReplaceEmptyArrayCreationWithInitBlockWithArrayEmptyAsync()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething()
        {
            return new int[] { };
        }
    }
}", VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(13, 20, 13, 33), @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public IEnumerable<int> DoSomething()
        {
            return Array.Empty<int>();
        }
    }
}");
        }
        [Fact]
        public async Task ShouldReplaceListCreationAsMethodInvocationParameterWithArrayEmptyAsync()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public void DoSomething()
        {
            Do(new List<int>());
        }
        
        private void Do(IEnumerable<int> a)
        {

        }
    }
}", VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(13, 16, 13, 31), @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public void DoSomething()
        {
            Do(Array.Empty<int>());
        }
        
        private void Do(IEnumerable<int> a)
        {

        }
    }
}");
        }
        [Fact]
        public async Task ShouldReplaceArrayCreationAsMethodInvocationParameterWithArrayEmptyAsync()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public void DoSomething()
        {
            Do(new int[0]);
        }
        
        private void Do(IEnumerable<int> a)
        {

        }
    }
}", VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(13, 16, 13, 26), @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public void DoSomething()
        {
            Do(Array.Empty<int>());
        }
        
        private void Do(IEnumerable<int> a)
        {

        }
    }
}");
        }
        [Fact]
        public async Task ShouldReplaceArrayCreationAsDelegateInvocationParameterWithArrayEmptyAsync()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public void DoSomething(Action<IEnumerable<int>> doSth)
        {
            doSth(new int[0]);
        }
    }
}", VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(13, 19, 13, 29), @"
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace SampleNamespace
{
    class SampleClass
    {
        [PerformanceSensitive(""uri"")]
        public void DoSomething(Action<IEnumerable<int>> doSth)
        {
            doSth(Array.Empty<int>());
        }
    }
}");
        }
    }
}
