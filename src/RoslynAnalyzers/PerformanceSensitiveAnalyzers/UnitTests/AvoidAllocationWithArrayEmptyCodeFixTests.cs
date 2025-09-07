// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers.CodeFixes;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers.UnitTests;

using VerifyCS = CSharpPerformanceCodeFixVerifier<
    ExplicitAllocationAnalyzer,
    AvoidAllocationWithArrayEmptyCodeFix>;

public class AvoidAllocationWithArrayEmptyCodeFixTests
{
    [Theory]
    [InlineData("IEnumerable<int>")]
    [InlineData("IReadOnlyList<int>")]
    [InlineData("IReadOnlyCollection<int>")]
    public Task ShouldReplaceEmptyListCreationWithArrayEmptyWithReturnTypeAsync(string type)
        => VerifyCS.VerifyCodeFixAsync($$"""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public {{type}} DoSomething()
                    {
                        return new List<int>();
                    }
                }
            }
            """, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(12, 20, 12, 35), $$"""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public {{type}} DoSomething()
                    {
                        return Array.Empty<int>();
                    }
                }
            }
            """);

    [Fact]
    public Task ShouldReplaceEmptyListCreationWithArrayEmptyWhenReturnFromMethodArrayAsync()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public int[] DoSomething()
                    {
                        return new int[0];
                    }
                }
            }
            """, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(12, 20, 12, 30), """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public int[] DoSomething()
                    {
                        return Array.Empty<int>();
                    }
                }
            }
            """);

    [Fact]
    public Task ShouldReplaceEmptyLisCreationWithArrayEmptyForArrowExpressionAsync()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething => new List<int>();
                }
            }
            """, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(10, 48, 10, 63), """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething => Array.Empty<int>();
                }
            }
            """);
    [Fact]
    public Task ShouldReplaceEmptyListCreationWithArrayEmptyForReadonlyPropertyAsync()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething { get {return new List<int>();}}
                }
            }
            """, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(10, 59, 10, 74), """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething { get {return Array.Empty<int>();}}
                }
            }
            """);
    [Fact]
    public Task ShouldReplaceEmptyListWithCreationWithPredefinedSizeWithArrayEmptyAsync()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething()
                    {
                        return new List<int>(10);
                    }
                }
            }
            """, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(12, 20, 12, 37), """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething()
                    {
                        return Array.Empty<int>();
                    }
                }
            }
            """);
    [Fact]
    public async Task ShouldNotProposeCodeFixWhenNonEmptyListCreatedAsync()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething()
                    {
                        return new List<int>(){1, 2};
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(12, 20, 12, 41), code);
    }
    [Fact]
    public async Task ShouldNotProposeCodeFixWhenReturnTypeInheritFormEnumerableAsync()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public List<int> DoSomething()
                    {
                        return new List<int>();
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(12, 20, 12, 35), code);
    }
    [Fact]
    public async Task ShouldNotProposeCodeFixWhenForCollectionCreationUsingCopyConstructorAsync()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething()
                    {
                        var innerList = new List<int>(){1, 2};
                        return new ReadOnlyCollection<int>(innerList);
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code,
            [
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(13, 29, 13, 50),
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(14, 20, 14, 58),
            ], code);
    }
    [Fact]
    public Task ShouldReplacEmptyCollectionCreationWithArrayEmptyAsync()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething()
                    {
                        return new Collection<int>();
                    }
                }
            }
            """, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(13, 20, 13, 41), """
            using System;
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething()
                    {
                        return Array.Empty<int>();
                    }
                }
            }
            """);
    [Fact]
    public Task ShouldReplaceEmptyArrayCreationWithArrayEmptyAsync()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething()
                    {
                        return new int[0];
                    }
                }
            }
            """, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(12, 20, 12, 30), """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething()
                    {
                        return Array.Empty<int>();
                    }
                }
            }
            """);
    [Fact]
    public async Task ShouldNotProposeCodeFixWhenNonEmptyArrayCreationAsync()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething()
                    {
                        return new int[]{1, 2};
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(12, 20, 12, 35), code);
    }
    [Fact]
    public Task ShouldReplaceEmptyArrayCreationWithInitBlockWithArrayEmptyAsync()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething()
                    {
                        return new int[] { };
                    }
                }
            }
            """, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(12, 20, 12, 33), """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public IEnumerable<int> DoSomething()
                    {
                        return Array.Empty<int>();
                    }
                }
            }
            """);
    [Fact]
    public Task ShouldReplaceListCreationAsMethodInvocationParameterWithArrayEmptyAsync()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public void DoSomething()
                    {
                        Do(new List<int>());
                    }

                    private void Do(IEnumerable<int> a)
                    {

                    }
                }
            }
            """, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithSpan(12, 16, 12, 31), """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public void DoSomething()
                    {
                        Do(Array.Empty<int>());
                    }

                    private void Do(IEnumerable<int> a)
                    {

                    }
                }
            }
            """);
    [Fact]
    public Task ShouldReplaceArrayCreationAsMethodInvocationParameterWithArrayEmptyAsync()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public void DoSomething()
                    {
                        Do(new int[0]);
                    }

                    private void Do(IEnumerable<int> a)
                    {

                    }
                }
            }
            """, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(12, 16, 12, 26), """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public void DoSomething()
                    {
                        Do(Array.Empty<int>());
                    }

                    private void Do(IEnumerable<int> a)
                    {

                    }
                }
            }
            """);
    [Fact]
    public Task ShouldReplaceArrayCreationAsDelegateInvocationParameterWithArrayEmptyAsync()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public void DoSomething(Action<IEnumerable<int>> doSth)
                    {
                        doSth(new int[0]);
                    }
                }
            }
            """, VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithSpan(12, 19, 12, 29), """
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            namespace SampleNamespace
            {
                class SampleClass
                {
                    [PerformanceSensitive("uri")]
                    public void DoSomething(Action<IEnumerable<int>> doSth)
                    {
                        doSth(Array.Empty<int>());
                    }
                }
            }
            """);
}
