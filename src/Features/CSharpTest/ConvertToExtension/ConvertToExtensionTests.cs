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
    public async Task TestRefReceiver()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M(this ref int i) { }
                }
                """,
            FixedCode = """
                static class C
                {
                    extension(ref int i)
                    {
                        public void M() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInReceiver()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M(this in int i) { }
                }
                """,
            FixedCode = """
                static class C
                {
                    extension(in int i)
                    {
                        public void M() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRefReadonlyReceiver()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M(this ref readonly int i) { }
                }
                """,
            FixedCode = """
                static class C
                {
                    extension(ref readonly int i)
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

    [Fact]
    public async Task TestSimpleGrouping1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M(this int i) { }
                    public static void N(this int i) { }
                }
                """,
            FixedCode = """
                static class C
                {
                    extension(int i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping1_A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    public static void M(this int i) { }
                    [||]public static void N(this int i) { }
                }
                """,
            FixedCode = """
                static class C
                {
                    extension(int i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M(this int i) { }
                    public static void N(this int i, string s) { }
                }
                """,
            FixedCode = """
                static class C
                {
                    extension(int i)
                    {
                        public void M() { }
                        public void N(string s) { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping2_A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    public static void M(this int i) { }
                    [||]public static void N(this int i, string s) { }
                }
                """,
            FixedCode = """
                static class C
                {
                    extension(int i)
                    {
                        public void M() { }
                        public void N(string s) { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_MatchingAttributes1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class XAttribute : Attribute { }

                static class C
                {
                    public static void M([X] this int i) { }
                    [||]public static void N([X] this int i) { }
                }
                """,
            FixedCode = """
                using System;
                
                class XAttribute : Attribute { }
                
                static class C
                {
                    extension([X] int i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_MatchingAttributes2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class XAttribute : Attribute { }

                static class C
                {
                    public static void M([X] this int i) { }
                    [||]public static void N([XAttribute] this int i) { }
                }
                """,
            FixedCode = """
                using System;
                
                class XAttribute : Attribute { }
                
                static class C
                {
                    extension([X] int i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_MatchingAttributes3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class XAttribute : Attribute
                {
                    public XAttribute(int i) { }
                }

                static class C
                {
                    public static void M([X(0)] this int i) { }
                    [||]public static void N([X(0)] this int i) { }
                }
                """,
            FixedCode = """
                using System;
                
                class XAttribute : Attribute
                {
                    public XAttribute(int i) { }
                }
                
                static class C
                {
                    extension([X(0)] int i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_MatchingAttributes4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class XAttribute : Attribute
                {
                    public int I;
                }

                static class C
                {
                    public static void M([X(I=1)] this int i) { }
                    [||]public static void N([X(I=1)] this int i) { }
                }
                """,
            FixedCode = """
                using System;
                
                class XAttribute : Attribute
                {
                    public int I;
                }
                
                static class C
                {
                    extension([X(I = 1)] int i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_NonMatchingAttributes1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class XAttribute : Attribute { }

                static class C
                {
                    public static void M([X] this int i) { }
                    [||]public static void N(this int i) { }
                }
                """,
            FixedCode = """
                using System;
                
                class XAttribute : Attribute { }
                
                static class C
                {
                    public static void M([X] this int i) { }
                    extension(int i)
                    {
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_NonMatchingAttributes2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class XAttribute : Attribute { }
                class YAttribute : Attribute { }

                static class C
                {
                    public static void M([X] this int i) { }
                    [||]public static void N([Y] this int i) { }
                }
                """,
            FixedCode = """
                using System;

                class XAttribute : Attribute { }
                class YAttribute : Attribute { }

                static class C
                {
                    public static void M([X] this int i) { }
                    extension([Y] int i)
                    {
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_NonMatchingAttributes3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class XAttribute : Attribute
                {
                    public XAttribute(int i) { }
                }

                static class C
                {
                    public static void M([X(0)] this int i) { }
                    [||]public static void N([X(1)] this int i) { }
                }
                """,
            FixedCode = """
                using System;

                class XAttribute : Attribute
                {
                    public XAttribute(int i) { }
                }

                static class C
                {
                    public static void M([X(0)] this int i) { }
                    extension([X(1)] int i)
                    {
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_NonMatchingAttributes4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class XAttribute : Attribute
                {
                    public int I;
                }

                static class C
                {
                    public static void M([X(I=1)] this int i) { }
                    [||]public static void N([X(I=2)] this int i) { }
                }
                """,
            FixedCode = """
                using System;

                class XAttribute : Attribute
                {
                    public int I;
                }

                static class C
                {
                    public static void M([X(I=1)] this int i) { }
                    extension([X(I = 2)] int i)
                    {
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_UnrelatedAttributes()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class XAttribute : Attribute { }
                class YAttribute : Attribute { }

                static class C
                {
                    public static void M(this int i, [X] string s) { }
                    [||]public static void N(this int i, [Y] string s) { }
                }
                """,
            FixedCode = """
                using System;
            
                class XAttribute : Attribute { }
                class YAttribute : Attribute { }
            
                static class C
                {
                    extension(int i)
                    {
                        public void M([X] string s) { }
                        public void N([Y] string s) { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_TypeParameters1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class XAttribute : Attribute { }
                class YAttribute : Attribute { }

                static class C
                {
                    [||]public static void M<T>(this IList<T> list) { }
                    public static void N<T>(this IList<T> list) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                class XAttribute : Attribute { }
                class YAttribute : Attribute { }
            
                static class C
                {
                    extension<T>(IList<T> list)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_TypeParameters_DifferentName()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class XAttribute : Attribute { }
                class YAttribute : Attribute { }

                static class C
                {
                    [||]public static void M<T>(this IList<T> list) { }
                    public static void N<X>(this IList<X> list) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                class XAttribute : Attribute { }
                class YAttribute : Attribute { }
            
                static class C
                {
                    extension<T>(IList<T> list)
                    {
                        public void M() { }
                    }

                    public static void N<X>(this IList<X> list) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_TypeParameters_SameAttributes()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class XAttribute : Attribute { }
                class YAttribute : Attribute { }

                static class C
                {
                    [||]public static void M<T>([X] this IList<T> list) { }
                    public static void N<T>([X] this IList<T> list) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                class XAttribute : Attribute { }
                class YAttribute : Attribute { }
            
                static class C
                {
                    extension<T>([X] IList<T> list)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_TypeParameters_DifferentAttributes()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class XAttribute : Attribute { }
                class YAttribute : Attribute { }

                static class C
                {
                    [||]public static void M<T>([X] this IList<T> list) { }
                    public static void N<T>([Y] this IList<T> list) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                class XAttribute : Attribute { }
                class YAttribute : Attribute { }
            
                static class C
                {
                    extension<T>([X] IList<T> list)
                    {
                        public void M() { }
                    }

                    public static void N<T>([Y] this IList<T> list) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_TypeParameters_SameConstructorConstraint()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<T>(this IList<T> list) where T : new() { }
                    public static void N<T>(this IList<T> list) where T : new() { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension<T>(IList<T> list) where T : new()
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_TypeParameters_DifferentConstructorConstraint()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<T>(this IList<T> list) where T : new() { }
                    public static void N<T>(this IList<T> list) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension<T>(IList<T> list) where T : new()
                    {
                        public void M() { }
                    }

                    public static void N<T>(this IList<T> list) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_TypeParameters_SameClassConstraint()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<T>(this IList<T> list) where T : class { }
                    public static void N<T>(this IList<T> list) where T : class { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension<T>(IList<T> list) where T : class
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_TypeParameters_DifferentClassConstraint()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<T>(this IList<T> list) where T : class { }
                    public static void N<T>(this IList<T> list) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension<T>(IList<T> list) where T : class
                    {
                        public void M() { }
                    }

                    public static void N<T>(this IList<T> list) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_TypeParameters_SameTypeConstraint()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<T>(this IList<T> list) where T : IList<T> { }
                    public static void N<T>(this IList<T> list) where T : IList<T> { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension<T>(IList<T> list) where T : IList<T>
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_TypeParameters_DifferentTypeConstraint()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<T>(this IList<T> list) where T : IList<T> { }
                    public static void N<T>(this IList<T> list) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension<T>(IList<T> list) where T : IList<T>
                    {
                        public void M() { }
                    }

                    public static void N<T>(this IList<T> list) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_Parameters_SameParameterRef()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M(this ref int i) { }
                    public static void N(this ref int i) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(ref int i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_Parameters_DifferentParameterRef()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M(this ref int i) { }
                    public static void N(this int i) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(ref int i)
                    {
                        public void M() { }
                    }

                    public static void N(this int i) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_Parameters_SameParameterName()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M(this int i) { }
                    public static void N(this int i) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(int i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_Parameters_DifferentParameterName()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M(this int i) { }
                    public static void N(this int j) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(int i)
                    {
                        public void M() { }
                    }

                    public static void N(this int j) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_Parameters_SameParameterTupleNames()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M(this (int i, int j) i) { }
                    public static void N(this (int i, int j) i) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension((int i, int j) i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_Parameters_DifferentParameterTupleNames()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M(this (int i, int j) i) { }
                    public static void N(this (int k, int l) i) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension((int i, int j) i)
                    {
                        public void M() { }
                    }

                    public static void N(this (int k, int l) i) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_Parameters_SameParameterNullability()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M(this string? i) { }
                    public static void N(this string? i) { }
                }
                """,
            FixedCode = """
                #nullable enable

                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(string? i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_Parameters_DifferentParameterNullability()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M(this string? i) { }
                    public static void N(this string i) { }
                }
                """,
            FixedCode = """
                #nullable enable

                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(string? i)
                    {
                        public void M() { }
                    }
                
                    public static void N(this string i) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_Parameters_SameParameterDynamic()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M(this {|CS1103:dynamic|} i) { }
                    public static void N(this {|CS1103:dynamic|} i) { }
                }
                """,
            FixedCode = """
                #nullable enable

                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(dynamic i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleGrouping_Parameters_DifferentDynamic()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                using System;
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M(this {|CS1103:dynamic|} i) { }
                    public static void N(this object i) { }
                }
                """,
            FixedCode = """
                #nullable enable

                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(dynamic i)
                    {
                        public void M() { }
                    }
                
                    public static void N(this object i) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTriviaMove1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    public static void M(this int i) { }

                    [||]public static void N(this int j) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    public static void M(this int i) { }

                    extension(int j)
                    {
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTriviaMove2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    public static void M(this int i) { }

                    /// <summary></summary>
                    [||]public static void N(this int j) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    public static void M(this int i) { }

                    extension(int j)
                    {
                        /// <summary></summary>
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestGroupingWithNonExtension1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    public static void O() { }

                    public static void M(this int i) { }

                    [||]public static void N(this int i) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    public static void O() { }

                    extension(int i)
                    {
                        public void M() { }

                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestGroupingWithNonExtension2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    public static void M(this int i) { }

                    public static void O() { }

                    [||]public static void N(this int i) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(int i)
                    {
                        public void M() { }

                        public void N() { }
                    }

                    public static void O() { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestGroupingWithNonExtension3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                static class C
                {
                    public static void M(this int i) { }

                    [||]public static void N(this int i) { }
                
                    public static void O() { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(int i)
                    {
                        public void M() { }

                        public void N() { }
                    }

                    public static void O() { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFixClass1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                [||]static class C
                {
                    public static void M(this int i) { }

                    public static void N(this int i) { }
                
                    public static void O(this int j) { }
                
                    public static void P(this int j) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(int i)
                    {
                        public void M() { }

                        public void N() { }
                    }

                    extension(int j)
                    {
                        public void O() { }

                        public void P() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFixClass2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                [||]static class C
                {
                    public static void M(this int i) { }
                
                    public static void O(this int j) { }

                    public static void N(this int i) { }
                
                    public static void P(this int j) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
            
                static class C
                {
                    extension(int i)
                    {
                        public void M() { }

                        public void N() { }
                    }

                    extension(int j)
                    {
                        public void O() { }

                        public void P() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCodeBody1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M(this int i)
                    {
                        return;
                    }
                }
                """,
            FixedCode = """
                static class C
                {
                    extension(int i)
                    {
                        public void M()
                        {
                            return;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCodeBody2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                static class C
                {
                    [||]public static DateTime M(this int i)
                    {
                        return new()
                        {
                        };
                    }
                }
                """,
            FixedCode = """
                using System;

                static class C
                {
                    extension(int i)
                    {
                        public DateTime M()
                        {
                            return new()
                            {
                            };
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleConstraints1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<K,V>(this IDictionary<K,V> map) where K : struct where V : class { }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                static class C
                {
                    extension<K, V>(IDictionary<K, V> map)
                        where K : struct
                        where V : class
                    {
                        public void M() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleConstraints2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<K,V>(this IList<K> map) where K : struct where V : class { }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                static class C
                {
                    extension<K>(IList<K> map) where K : struct
                    {
                        public void M<V>() where V : class { }
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }
}
