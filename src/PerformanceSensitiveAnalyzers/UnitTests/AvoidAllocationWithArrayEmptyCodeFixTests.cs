// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.ExplicitAllocationAnalyzer,
    Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers.AvoidAllocationWithArrayEmptyCodeFix>;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers.UnitTests
{
    public class AvoidAllocationWithArrayEmptyCodeFixTests
    {
        public static readonly TheoryData<string> CollectionTypes = new TheoryData<string>
        {
            "IEnumerable<int>",
            "IReadOnlyList<int>",
            "IReadOnlyCollection<int>"
        };

        [Theory]
        [InlineData("IEnumerable<int>")]
        [InlineData("IReadOnlyList<int>")]
        [InlineData("IReadOnlyCollection<int>")]
        public async Task ShouldReplaceEmptyListCreationWithArrayEmptyWithReturnType(string type)
        {
            var initial = $@"
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
}}";

            var expected = $@"
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
}}";
            await VerifyCS.VerifyCodeFixAsync(initial, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(13, 20, 13, 35), expected);
        }

        [Fact]
        public async Task ShouldReplaceEmptyListCreationWithArrayEmptyWhenReturnFromMethodArray()
        {
            var before = @"
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
}";
            var after = @"
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
}";

            await VerifyCS.VerifyCodeFixAsync(before, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(13, 20, 13, 30), after);
        }

        [Fact]
        public async Task ShouldReplaceEmptyLisCreationWithArrayEmptyForArrowExpression()
        {
            var before = @"
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
}";
            var after = @"
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
}";

            await VerifyCS.VerifyCodeFixAsync(before, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(11, 48, 11, 63), after);
        }
        [Fact]
        public async Task ShouldReplaceEmptyListCreationWithArrayEmptyForReadonlyProperty()
        {
            var before = @"
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
}";
            var after = @"
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
}";

            await VerifyCS.VerifyCodeFixAsync(before, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(11, 59, 11, 74), after);
        }
        [Fact]
        public async Task ShouldReplaceEmptyListWithCreationWithPredefinedSizeWithArrayEmpty()
        {
            var before = @"
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
}";
            var after = @"
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
}";

            await VerifyCS.VerifyCodeFixAsync(before, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(13, 20, 13, 37), after);
        }
        [Fact]
        public async Task ShouldNotProposeCodeFixWhenNonEmptyListCreated()
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
        public async Task ShouldNotProposeCodeFixWhenReturnTypeInheritFormEnumerable()
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
        public async Task ShouldNotProposeCodeFixWhenForCollectionCreationUsingCopyConstructor()
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
        public async Task ShouldReplacEmptyCollectionCreationWithArrayEmpty()
        {
            var before = @"
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
}";
            var after = @"
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
}";

            await VerifyCS.VerifyCodeFixAsync(before, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(14, 20, 14, 41), after);
        }
        [Fact]
        public async Task ShouldReplaceEmptyArrayCreationWithArrayEmpty()
        {
            var before = @"
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
}";
            var after = @"
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
}";

            await VerifyCS.VerifyCodeFixAsync(before, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(13, 20, 13, 30), after);
        }
        [Fact]
        public async Task ShouldNotProposeCodeFixWhenNonEmptyArrayCreation()
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
        public async Task ShouldReplaceEmptyArrayCreationWithInitBlockWithArrayEmpty()
        {
            var before = @"
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
}";
            var after = @"
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
}";

            await VerifyCS.VerifyCodeFixAsync(before, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(13, 20, 13, 33), after);
        }
        [Fact]
        public async Task ShouldReplaceListCreationAsMethodInvocationParameterWithArrayEmpty()
        {
            var before = @"
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
}";
            var after = @"
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
}";

            await VerifyCS.VerifyCodeFixAsync(before, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(13, 16, 13, 31), after);
        }
        [Fact]
        public async Task ShouldReplaceArrayCreationAsMethodInvocationParameterWithArrayEmpty()
        {
            var before = @"
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
}";
            var after = @"
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
}";

            await VerifyCS.VerifyCodeFixAsync(before, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(13, 16, 13, 26), after);
        }
        [Fact]
        public async Task ShouldReplaceArrayCreationAsDelegateInvocationParameterWithArrayEmpty()
        {
            var before = @"
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
}";
            var after = @"
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
}";

            await VerifyCS.VerifyCodeFixAsync(before, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(13, 19, 13, 29), after);
        }
    }
}