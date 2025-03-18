// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.ConvertToExtension;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.ConvertToExtension;

using VerifyCS = CSharpCodeRefactoringVerifier<ConvertToExtensionCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsConvertToExtension)]
public sealed class ConvertToExtensionTests
{
    [Fact]
    public async Task TestBaseCase()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M(this int i) { }
                }
                """,
            FixedCode = """
                static class C
                {
                    extension(int i)
                    {
                        public void M() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotOnCSharp13()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M(this int i) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithMultipleParameters()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M(this int i, string j) { }
                }
                """,
            FixedCode = """
                static class C
                {
                    extension(int i)
                    {
                        public void M(string j) { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInsideNamespace1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N
                {
                    static class C
                    {
                        [||]public static void M(this int i, string j) { }
                    }
                }
                """,
            FixedCode = """
                namespace N
                {
                    static class C
                    {
                        extension(int i)
                        {
                            public void M(string j) { }
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInsideNamespace2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                namespace N;

                static class C
                {
                    [||]public static void M(this int i, string j) { }
                }
                """,
            FixedCode = """
                namespace N;

                static class C
                {
                    extension(int i)
                    {
                        public void M(string j) { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNoParameters()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M() { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInsideStruct()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                struct C
                {
                    [||]public static void M(this int i) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            TestState =
            {
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(1,8): error CS1106: Extension method must be defined in a non-generic static class
                    DiagnosticResult.CompilerError("CS1106").WithSpan(1, 8, 1, 9),
                }
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInsideInstanceClass()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    [||]public static void M(this int i) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            TestState =
            {
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(1,7): error CS1106: Extension method must be defined in a non-generic static class
                    DiagnosticResult.CompilerError("CS1106").WithSpan(1, 7, 1, 8),
                }
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotForInstanceMethod()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public void M(this int i) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            TestState =
            {
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(3,17): error CS0708: 'M': cannot declare instance members in a static class
                    DiagnosticResult.CompilerError("CS0708").WithSpan(3, 17, 3, 18).WithArguments("M"),
                    // /0/Test0.cs(3,17): error CS1105: Extension method must be static
                    DiagnosticResult.CompilerError("CS1105").WithSpan(3, 17, 3, 18),
                }
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotForNestedClass()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class Outer
                {
                    static class C
                    {
                        [||]public void M(this int i) { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            TestState =
            {
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(5,21): error CS0708: 'M': cannot declare instance members in a static class
                    DiagnosticResult.CompilerError("CS0708").WithSpan(5, 21, 5, 22).WithArguments("M"),
                    // /0/Test0.cs(5,21): error CS1109: Extension methods must be defined in a top level static class; C is a nested class
                    DiagnosticResult.CompilerError("CS1109").WithSpan(5, 21, 5, 22).WithArguments("C"),
                }
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithReferencedTypeParameterInOrder1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<T>(this IList<T> list) { }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                static class C
                {
                    extension<T>(IList<T> list)
                    {
                        public void M() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithReferencedTypeParameterInOrder2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<K,V>(this IDictionary<K,V> map) { }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                static class C
                {
                    extension<K, V>(IDictionary<K, V> map)
                    {
                        public void M() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithReferencedTypeParameterInOrder3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<K,V>(this IDictionary<V,K> map) { }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                static class C
                {
                    extension<K, V>(IDictionary<V, K> map)
                    {
                        public void M() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithReferencedTypeParameterInOrder4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<K,V>(this IList<K> list) { }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                static class C
                {
                    extension<K>(IList<K> list)
                    {
                        public void M<V>() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithReferencedTypeParameterNotInOrder1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<K,V>(this IList<V> list) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }
}
