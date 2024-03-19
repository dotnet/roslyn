// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCompoundAssignment
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUseCompoundCoalesceAssignmentDiagnosticAnalyzer,
        CSharpUseCompoundCoalesceAssignmentCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
    public class UseCompoundCoalesceAssignmentTests
    {
        private static async Task TestInRegularAndScriptAsync(string testCode, string fixedCode)
        {
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
            }.RunAsync();
        }
        private static async Task TestMissingAsync(string testCode, LanguageVersion languageVersion = LanguageVersion.CSharp8)
        {
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = languageVersion,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38059")]
        public async Task TestBaseCase()
        {
            await TestInRegularAndScriptAsync(
                """
                class Program
                {
                    private static string s_goo;
                    private static string Goo => s_goo [|??|] (s_goo = new string('c', 42));
                }
                """,
                """
                class Program
                {
                    private static string s_goo;
                    private static string Goo => s_goo ??= new string('c', 42);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44793")]
        public async Task TestMissingBeforeCSharp8()
        {
            await TestMissingAsync(
                """
                class Program
                {
                    private static string s_goo;
                    private static string Goo => s_goo ?? (s_goo = new string('c', 42));
                }
                """, LanguageVersion.CSharp7_3);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38059")]
        public async Task TestRightMustBeParenthesized()
        {
            await TestMissingAsync(
                """
                class Program
                {
                    private static string s_goo;
                    private static string Goo => {|CS0131:s_goo ?? s_goo|} = new string('c', 42);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38059")]
        public async Task TestRightMustBeAssignment()
        {
            await TestMissingAsync(
                """
                class Program
                {
                    private static string s_goo;
                    private static string Goo => {|CS0019:s_goo ?? (s_goo == new string('c', 42))|};
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38059")]
        public async Task TestRightMustBeSimpleAssignment()
        {
            await TestMissingAsync(
                """
                class Program
                {
                    private static string s_goo;
                    private static string Goo => s_goo ?? (s_goo ??= new string('c', 42));
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38059")]
        public async Task TestShapesMustBeTheSame()
        {
            await TestMissingAsync(
                """
                class Program
                {
                    private static string s_goo;
                    private static string s_goo2;
                    private static string Goo => s_goo ?? (s_goo2 = new string('c', 42));
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38059")]
        public async Task TestNoSideEffects1()
        {
            await TestMissingAsync(
                """
                class Program
                {
                    private static string s_goo;
                    private static string Goo => s_goo.GetType() ?? ({|CS0131:s_goo.GetType()|} = new string('c', 42));
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38059")]
        public async Task TestNoSideEffects2()
        {
            await TestInRegularAndScriptAsync(
                """
                class Program
                {
                    private string goo;
                    private string Goo => this.goo [|??|] (this.goo = new string('c', 42));
                }
                """,
                """
                class Program
                {
                    private string goo;
                    private string Goo => this.goo ??= new string('c', 42);
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38059")]
        public async Task TestNullableValueType()
        {
            await TestInRegularAndScriptAsync(
                """
                class Program
                {
                    void Goo()
                    {
                        int? a = null;
                        var x = a [|??|] (a = 1);
                    }
                }
                """,
                """
                class Program
                {
                    void Goo()
                    {
                        int? a = null;
                        var x = (int?)(a ??= 1);
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38059")]
        public async Task TestCastIfWouldAffectSemantics()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void M(int a) { }
                    static void M(int? a) { }

                    static void Main()
                    {
                        int? a = null;
                        M(a [|??|] (a = 1));
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void M(int a) { }
                    static void M(int? a) { }

                    static void Main()
                    {
                        int? a = null;
                        M((int?)(a ??= 1));
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38059")]
        public async Task TestDoNotCastIfNotNecessary()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void M(int? a) { }

                    static void Main()
                    {
                        int? a = null;
                        M(a [|??|] (a = 1));
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void M(int? a) { }

                    static void Main()
                    {
                        int? a = null;
                        M(a ??= 1);
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement1()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        [|if|] (o is null)
                        {
                            o = "";
                        }
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        o ??= "";
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_NotBeforeCSharp8()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                        {
                            o = "";
                        }
                    }
                }
                """, LanguageVersion.CSharp7_3);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_NotWithElseClause()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                        {
                            o = "";
                        }
                        else
                        {
                            Console.WriteLine();
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatementWithoutBlock()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        [|if|] (o is null)
                            o = "";
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        o ??= "";
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_WithEmptyBlock()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                        {
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_WithMultipleStatements()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                        {
                            o = "";
                            o = "";
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_EqualsEqualsCheck()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        [|if|] (o == null)
                        {
                            o = "";
                        }
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        o ??= "";
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_ReferenceEqualsCheck1()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        [|if|] (ReferenceEquals(o, null))
                        {
                            o = "";
                        }
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        o ??= "";
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_ReferenceEqualsCheck2()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        [|if|] (ReferenceEquals(null, o))
                        {
                            o = "";
                        }
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        o ??= "";
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_ReferenceEqualsCheck3()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        [|if|] (object.ReferenceEquals(null, o))
                        {
                            o = "";
                        }
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        o ??= "";
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_ReferenceEqualsCheck4()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (!object.ReferenceEquals(null, o))
                        {
                            o = "";
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_NotSimpleAssignment()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                        {
                            o ??= "";
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_OverloadedEquals_OkForString()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void Main(string o)
                    {
                        [|if|] (o == null)
                        {
                            o = "";
                        }
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void Main(string o)
                    {
                        o ??= "";
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_OverloadedEquals()
        {
            await TestMissingAsync(
                """
                using System;

                class X
                {
                    public static bool operator ==(X x1, X x2) => true;
                    public static bool operator !=(X x1, X x2) => !(x1 == x2);
                }

                class C
                {
                    static void Main(X o)
                    {
                        if (o == null)
                        {
                            o = new X();
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_AssignmentToDifferentValue()
        {
            await TestMissingAsync(
                """
                using System;

                class C
                {
                    static void Main(object o1, object o2)
                    {
                        if (o1 is null)
                        {
                            o2 = "";
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_SideEffects1()
        {
            await TestMissingAsync(
                """
                using System;

                class C
                {
                    private object o;

                    static void Main()
                    {
                        if (new C().o is null)
                        {
                            new C().o = "";
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_SideEffects2()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        [|if|] (o is null)
                        {
                            o = new C();
                        }
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        o ??= new C();
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_Trivia1()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        // Before
                        [|if|] (o is null)
                        {
                            o = new C();
                        } // After
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        // Before
                        o ??= new C(); // After
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_Trivia2()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        [|if|] (o is null)
                        {
                            // Before
                            o = new C(); // After
                        }
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        // Before
                        o ??= new C(); // After
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32985")]
        public async Task TestIfStatement_Trivia3()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        // Before1
                        [|if|] (o is null)
                        {
                            // Before2
                            o = new C(); // After
                        }
                    }
                }
                """,
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        // Before1
                        // Before2
                        o ??= new C(); // After
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63552")]
        public async Task TestIfStatementWithPreprocessorBlock1()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                        {
                #if true
                            o = "";
                #endif
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63552")]
        public async Task TestIfStatementWithPreprocessorBlock2()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                        {
                #if X
                            Console.WriteLine("Only run if o is null");
                #endif
                            o = "";
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63552")]
        public async Task TestIfStatementWithPreprocessorBlock3()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                        {
                #if X
                            Console.WriteLine("Only run if o is null");
                #else
                            o = "";
                #endif
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63552")]
        public async Task TestIfStatementWithPreprocessorBlock4()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                        {
                #if X
                            Console.WriteLine("Only run if o is null");
                #elif true
                            o = "";
                #endif
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63552")]
        public async Task TestIfStatementWithPreprocessorBlock5()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                        {
                #if true
                            o = "";
                #else
                            Console.WriteLine("Only run if o is null");
                #endif
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63552")]
        public async Task TestIfStatementWithPreprocessorBlock6()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                        {
                #if true
                            o = "";
                #elif X
                            Console.WriteLine("Only run if o is null");
                #endif
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63552")]
        public async Task TestIfStatementWithPreprocessorBlock7()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                #if true
                            o = "";
                #endif
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63552")]
        public async Task TestIfStatementWithPreprocessorBlock8()
        {
            await TestMissingAsync(
                """
                using System;
                class C
                {
                    static void Main(object o)
                    {
                        if (o is null)
                #if true
                            o = "";
                #else
                            o = "";
                #endif
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62473")]
        public async Task TestPointerCannotUseCoalesceAssignment()
        {
            // The purpose of this test is to keep track of whether the language
            // allows ??= for pointers in future. It should be kept in 'Preview'.
            // If the test failed because language added support and this is no longer
            // an error. The behavior for test 'TestPointer' below should be updated as well to suggest ??=
            // Note that, when ??= is supported for pointers, the analyzer should check the language version which supports it.
            await TestMissingAsync("""
                unsafe class Program
                {
                    private static void Main()
                    {
                        byte* ptr = null;
                        {|CS0019:ptr ??= Get()|};
                    }

                    static byte* Get() => null;
                }
                """, LanguageVersion.CSharp12);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62473")]
        public async Task TestPointer()
        {
            await TestMissingAsync("""
                unsafe class Program
                {
                    private static void Main()
                    {
                        byte* ptr = null;
                        if (ptr is null)
                        {
                            ptr = Get();
                        }
                    }

                    static byte* Get() => null;
                }
                """, LanguageVersion.CSharp12);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63551")]
        public async Task TestFunctionPointer()
        {
            await TestMissingAsync("""
                using System.Runtime.InteropServices;
                public unsafe class C {
                    [DllImport("A")]
                    private static extern delegate* unmanaged<void> GetFunc();

                    private delegate* unmanaged<void> s_func;

                    public delegate* unmanaged<void> M() {
                        delegate* unmanaged<void> func = s_func;
                        if (func == null)
                        {
                            func = s_func = GetFunc();
                        }
                        return func;
                    }
                }
                """, LanguageVersion.CSharp12);
        }
    }
}
