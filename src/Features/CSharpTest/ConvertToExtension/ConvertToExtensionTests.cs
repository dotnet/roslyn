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
    public Task TestBaseCase()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestRefReceiver()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestInReceiver()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestRefReadonlyReceiver()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestNotOnCSharp13()
        => new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M(this int i) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
        }.RunAsync();

    [Fact]
    public Task TestWithMultipleParameters()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestInsideNamespace1()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestInsideNamespace2()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestNotWithNoParameters()
        => new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public static void M() { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestNotInsideStruct()
        => new VerifyCS.Test
        {
            TestCode = """
                struct C
                {
                    [||]public static void M(this int i) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
            TestState =
            {
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(1,8): error CS1106: Extension method must be defined in a non-generic static class
                    DiagnosticResult.CompilerError("CS1106").WithSpan(1, 8, 1, 9),
                }
            }
        }.RunAsync();

    [Fact]
    public Task TestNotInsideInstanceClass()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    [||]public static void M(this int i) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
            TestState =
            {
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(1,7): error CS1106: Extension method must be defined in a non-generic static class
                    DiagnosticResult.CompilerError("CS1106").WithSpan(1, 7, 1, 8),
                }
            }
        }.RunAsync();

    [Fact]
    public Task TestNotForInstanceMethod()
        => new VerifyCS.Test
        {
            TestCode = """
                static class C
                {
                    [||]public void M(this int i) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
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

    [Fact]
    public Task TestNotForNestedClass()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
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

    [Fact]
    public Task TestWithReferencedTypeParameterInOrder1()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestWithReferencedTypeParameterInOrder2()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestWithReferencedTypeParameterInOrder3()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestWithReferencedTypeParameterInOrder4()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestWithReferencedTypeParameterNotInOrder1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                static class C
                {
                    [||]public static void M<K,V>(this IList<V> list) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping1()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping1_A()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping2()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping2_A()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_MatchingAttributes1()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_MatchingAttributes2()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_MatchingAttributes3()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_MatchingAttributes4()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_NonMatchingAttributes1()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_NonMatchingAttributes2()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_NonMatchingAttributes3()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_NonMatchingAttributes4()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_UnrelatedAttributes()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_TypeParameters1()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_TypeParameters_DifferentName()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_TypeParameters_SameAttributes()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_TypeParameters_DifferentAttributes()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_TypeParameters_SameConstructorConstraint()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_TypeParameters_DifferentConstructorConstraint()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_TypeParameters_SameClassConstraint()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_TypeParameters_DifferentClassConstraint()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_TypeParameters_SameTypeConstraint()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_TypeParameters_DifferentTypeConstraint()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_Parameters_SameParameterRef()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_Parameters_DifferentParameterRef()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_Parameters_SameParameterName()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_Parameters_DifferentParameterName()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_Parameters_SameParameterTupleNames()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_Parameters_DifferentParameterTupleNames()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_Parameters_SameParameterNullability()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_Parameters_DifferentParameterNullability()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_Parameters_SameParameterDynamic()
        => new VerifyCS.Test
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
                    extension({|CS1103:dynamic|} i)
                    {
                        public void M() { }
                        public void N() { }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestSimpleGrouping_Parameters_DifferentDynamic()
        => new VerifyCS.Test
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
                    extension({|CS1103:dynamic|} i)
                    {
                        public void M() { }
                    }
                
                    public static void N(this object i) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestTriviaMove1()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestTriviaMove2()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestGroupingWithNonExtension1()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestGroupingWithNonExtension2()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestGroupingWithNonExtension3()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestFixClass1()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestFixClass2()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestCodeBody1()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestCodeBody2()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestMultipleConstraints1()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestMultipleConstraints2()
        => new VerifyCS.Test
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();
}
