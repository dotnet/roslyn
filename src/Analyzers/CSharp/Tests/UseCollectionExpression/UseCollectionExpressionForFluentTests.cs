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
    CSharpUseCollectionExpressionForFluentDiagnosticAnalyzer,
    CSharpUseCollectionExpressionForFluentCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionExpression)]
public class UseCollectionExpressionForFluentTests
{
    [Fact]
    public async Task TestNotInCSharp11()
    {

        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        List<int> list = new[] { 1, 2, 3 }.ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12_Net70()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = new[] { 1, 2, 3 }.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12_Net80()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = new[] { 1, 2, 3 }.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestOnlyOnOutermost()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = new[] { 1, 2, 3 }.ToArray().[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExplicitArrayCreation()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = new int[] { 1, 2, 3 }.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestArrayEmpty()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = Array.Empty<int>().[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestImmutableArrayEmpty()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray<int>.Empty.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestImmutableArrayEmptyAndAdd()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray<int>.Empty.Add(1).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotOnNonListEmpty()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableHashSet<int>.Empty.ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotOnNonListEmptyAndAdd()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableHashSet<int>.Empty.Add(1).ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }
}
