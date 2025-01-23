// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseExplicitArrayInExpressionTree;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExplicitArrayInExpressionTree;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpUseExplicitArrayInExpressionTreeCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitArrayInExpressionTree)]
public sealed class UseExplicitArrayInExpressionTreeTests
{
    [Fact]
    public Task TestNoArgumentsPassedToParams1()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test()|}|};
                    }

                    string Test(params char[] characters) => "";
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => Test(Array.Empty<char>());
                    }

                    string Test(params char[] characters) => "";
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNoArgumentsPassedToParams2()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test(0)|}|};
                    }

                    string Test(int i, params char[] characters) => "";
                    string Test(int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => Test(0, Array.Empty<char>());
                    }

                    string Test(int i, params char[] characters) => "";
                    string Test(int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotWhenReturnTypesDiffer1()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test()|}|};
                    }

                    void Test(params char[] characters) { }
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotWhenReturnTypesDiffer2()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test()|}|};
                    }

                    int Test(params char[] characters) => 0;
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotWhenParameterTypesDiffer1()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test(0)|}|};
                    }

                    string Test(bool b, params char[] characters) => "";
                    string Test(int b, params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestArgumentOfRequiredTypePassed()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test('a')|}|};
                    }

                    string Test(params char[] characters) => "";
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => Test(new[] { 'a' });
                    }

                    string Test(params char[] characters) => "";
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestMultipleArgumentOfRequiredTypePassed1()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test('a', 'b')|}|};
                    }

                    string Test(params char[] characters) => "";
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => Test(new[] { 'a', 'b' });
                    }

                    string Test(params char[] characters) => "";
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestMultipleArgumentOfRequiredTypePassed2()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test(1, 'a', 'b')|}|};
                    }

                    string Test(int i, params char[] characters) => "";
                    string Test(int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => Test(1, new[] { 'a', 'b' });
                    }

                    string Test(int i, params char[] characters) => "";
                    string Test(int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestMultipleArgumentOfRequiredTypePassed3()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test(1, 2, 'a', 'b')|}|};
                    }

                    string Test(int i, int j, params char[] characters) => "";
                    string Test(int i, int j, params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => Test(1, 2, new[] { 'a', 'b' });
                    }

                    string Test(int i, int j, params char[] characters) => "";
                    string Test(int i, int j, params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestArgumentsWithoutTypePassed1()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test(default, default)|}|};
                    }

                    string Test(params char[] characters) => "";
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => Test(new char[] { default, default });
                    }

                    string Test(params char[] characters) => "";
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestArgumentsWithoutTypePassed2()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test(null, null)|}|};
                    }

                    string Test(params string[] characters) => "";
                    string Test(params ReadOnlySpan<string> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => Test(new string[] { null, null });
                    }

                    string Test(params string[] characters) => "";
                    string Test(params ReadOnlySpan<string> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestArgumentsWithMixedTypesPassed()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => {|CS8640:{|CS9226:Test('a', default)|}|};
                    }

                    string Test(params char[] characters) => "";
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<string>> f = () => Test(new[] { 'a', default });
                    }

                    string Test(params char[] characters) => "";
                    string Test(params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNoArgumentsPassedToParams_ExtensionMethod1()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<int, string>> f = i => {|CS8640:{|CS9226:i.Test()|}|};
                    }
                }

                internal static class Extensions
                {
                    public static string Test(this int i, params char[] characters) => "";
                    public static string Test(this int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<int, string>> f = i => i.Test(Array.Empty<char>());
                    }
                }
                
                internal static class Extensions
                {
                    public static string Test(this int i, params char[] characters) => "";
                    public static string Test(this int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNoArgumentsPassedToParams_ExtensionMethod2()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<int, string>> f = i => {|CS8640:{|CS9226:i.Test('a')|}|};
                    }
                }

                internal static class Extensions
                {
                    public static string Test(this int i, params char[] characters) => "";
                    public static string Test(this int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<int, string>> f = i => i.Test(new[] { 'a' });
                    }
                }
                
                internal static class Extensions
                {
                    public static string Test(this int i, params char[] characters) => "";
                    public static string Test(this int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNoArgumentsPassedToParams_ExtensionMethod3()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<int, string>> f = i => {|CS8640:{|CS9226:Extensions.Test(i)|}|};
                    }
                }

                internal static class Extensions
                {
                    public static string Test(this int i, params char[] characters) => "";
                    public static string Test(this int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<int, string>> f = i => Extensions.Test(i, Array.Empty<char>());
                    }
                }
                
                internal static class Extensions
                {
                    public static string Test(this int i, params char[] characters) => "";
                    public static string Test(this int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNoArgumentsPassedToParams_ExtensionMethod4()
        => new VerifyCS.Test()
        {
            TestCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<int, string>> f = i => {|CS8640:{|CS9226:Extensions.Test(i, 'a')|}|};
                    }
                }

                internal static class Extensions
                {
                    public static string Test(this int i, params char[] characters) => "";
                    public static string Test(this int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            FixedCode = """
                using System;
                using System.Linq.Expressions;

                internal class Program
                {
                    void Main()
                    {
                        Expression<Func<int, string>> f = i => Extensions.Test(i, new[] { 'a' });
                    }
                }
                
                internal static class Extensions
                {
                    public static string Test(this int i, params char[] characters) => "";
                    public static string Test(this int i, params ReadOnlySpan<char> characters) => "";
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
}
