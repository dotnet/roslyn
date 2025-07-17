// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCollectionExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCollectionExpressionForNewDiagnosticAnalyzer,
    CSharpUseCollectionExpressionForNewCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionExpression)]
public sealed class UseCollectionExpressionForNewTests
{
    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/75870")]
    [InlineData("List<int>")]
    [InlineData("")]
    public Task TestIEnumerablePassedToListConstructor(string typeName)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    List<int> GetNumbers()
                    {
                        return [|[|new|] {{typeName}}(|]Enumerable.Range(1, 10));
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    List<int> GetNumbers()
                    {
                        return [.. Enumerable.Range(1, 10)];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/75870")]
    [InlineData("List<int>")]
    [InlineData("")]
    public Task TestArrayPassedToListConstructor(string typeName)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    List<int> GetNumbers()
                    {
                        return [|[|new|] {{typeName}}(|]new[] { 1, 2, 3 });
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    List<int> GetNumbers()
                    {
                        return [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76683")]
    public Task TestNotOnStack()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    Stack<T> GetNumbers<T>(T[] values)
                    {
                        return new Stack<T>(values);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77177")]
    public Task TestNotOnQueue()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class Container(IEnumerable<long> items)
                {
                    public Queue<long> Items { get; } = new(items);
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
}
