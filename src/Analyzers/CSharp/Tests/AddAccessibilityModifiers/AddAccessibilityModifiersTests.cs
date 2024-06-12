// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddAccessibilityModifiers
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpAddAccessibilityModifiersDiagnosticAnalyzer,
        CSharpAddAccessibilityModifiersCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
    public class AddAccessibilityModifiersTests
    {
        [Theory, CombinatorialData]
        public void TestStandardProperty(AnalyzerProperty property)
            => VerifyCS.VerifyStandardProperty(property);

        [Fact]
        public async Task TestAllConstructs()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                using System;
                namespace Outer
                {
                    namespace Inner1.Inner2
                    {
                        partial class [|C|] : I
                        {
                            class [|NestedClass|] { }

                            struct [|NestedStruct|] { }

                            int [|f1|];
                            int [|f2|], f3;
                            public int f4;

                            event Action [|e1|], e2;
                            public event Action e3;

                            event Action [|e4|] { add { } remove { } }
                            public event Action e5 { add { } remove { } }
                            event Action I.e6 { add { } remove { } }

                            static C() { }
                            [|C|]() { }
                            public C(int i) { }

                            ~C() { }

                            void [|M1|]() { }
                            public void M2() { }
                            void I.M3() { }
                            partial void M4();
                            partial void M4() { }

                            int [|P1|] { get; }
                            public int P2 { get; }
                            int I.P3 { get; }

                            int [|this|][int i] => throw null;
                            public int this[string s] => throw null;
                            int I.this[bool b] => throw null;
                        }

                        interface [|I|]
                        {
                            event Action e6;
                            void M3();
                            int P3 { get; }
                            int this[bool b] { get; }
                        }

                        delegate void [|D|]();

                        enum [|E|]
                        {
                            EMember
                        }
                    }
                }
                """,
                """
                using System;
                namespace Outer
                {
                    namespace Inner1.Inner2
                    {
                        internal partial class C : I
                        {
                            private class NestedClass { }

                            private struct NestedStruct { }

                            private int f1;
                            private int f2, f3;
                            public int f4;

                            private event Action e1, e2;
                            public event Action e3;

                            private event Action e4 { add { } remove { } }
                            public event Action e5 { add { } remove { } }
                            event Action I.e6 { add { } remove { } }

                            static C() { }

                            private C() { }
                            public C(int i) { }

                            ~C() { }

                            private void M1() { }
                            public void M2() { }
                            void I.M3() { }
                            partial void M4();
                            partial void M4() { }

                            private int P1 { get; }
                            public int P2 { get; }
                            int I.P3 { get; }

                            private int this[int i] => throw null;
                            public int this[string s] => throw null;
                            int I.this[bool b] => throw null;
                        }

                        internal interface I
                        {
                            event Action e6;
                            void M3();
                            int P3 { get; }
                            int this[bool b] { get; }
                        }

                        internal delegate void D();

                        internal enum E
                        {
                            EMember
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRefStructs()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                namespace Test
                {
                    ref struct [|S1|] { }
                }
                """, """
                namespace Test
                {
                    internal ref struct S1 { }
                }
                """);
        }

        [Fact]
        public async Task TestRecords()
        {
            var source = """
                record [|Record|]
                {
                    int [|field|];
                }
                namespace System.Runtime.CompilerServices
                {
                    public sealed class IsExternalInit
                    {
                    }
                }
                """;
            var fixedSource = """
                internal record Record
                {
                    private int field;
                }
                namespace System.Runtime.CompilerServices
                {
                    public sealed class IsExternalInit
                    {
                    }
                }
                """;

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp9,
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task TestRecordStructs()
        {
            var source = """
                record struct [|Record|]
                {
                    int [|field|];
                }
                """;
            var fixedSource = """
                internal record struct Record
                {
                    private int field;
                }
                """;

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp12,
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task TestReadOnlyStructs()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                namespace Test
                {
                    readonly struct [|S1|] { }
                }
                """, """
                namespace Test
                {
                    internal readonly struct S1 { }
                }
                """);
        }

        [Fact]
        public async Task TestAllConstructsWithOmit()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        """
                        using System;
                        namespace Outer
                        {
                            namespace Inner1.Inner2
                            {
                                internal partial class [|C|] : I
                                {
                                    private class [|NestedClass|] { }

                                    private struct [|NestedStruct|] { }

                                    private int [|f1|];
                                    private int [|f2|], f3;
                                    public int f4;

                                    private event Action [|e1|], e2;
                                    public event Action e3;

                                    private event Action [|e4|] { add { } remove { } }
                                    public event Action e5 { add { } remove { } }
                                    event Action I.e6 { add { } remove { } }

                                    static C() { }

                                    private [|C|]() { }
                                    public C(int i) { }

                                    ~C() { }

                                    private void [|M1|]() { }
                                    public void M2() { }
                                    void I.M3() { }
                                    partial void M4();
                                    partial void M4() { }

                                    private int [|P1|] { get; }
                                    public int P2 { get; }
                                    int I.P3 { get; }

                                    private int [|this|][int i] => throw null;
                                    public int this[string s] => throw null;
                                    int I.this[bool b] => throw null;
                                }

                                internal interface [|I|]
                                {
                                    event Action e6;
                                    void M3();
                                    int P3 { get; }
                                    int this[bool b] { get; }
                                }

                                internal delegate void [|D|]();

                                internal enum [|E|]
                                {
                                    EMember
                                }
                            }
                        }
                        """,
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        """
                        using System;
                        namespace Outer
                        {
                            namespace Inner1.Inner2
                            {
                                partial class C : I
                                {
                                    class NestedClass { }

                                    struct NestedStruct { }

                                    int f1;
                                    int f2, f3;
                                    public int f4;

                                    event Action e1, e2;
                                    public event Action e3;

                                    event Action e4 { add { } remove { } }
                                    public event Action e5 { add { } remove { } }
                                    event Action I.e6 { add { } remove { } }

                                    static C() { }

                                    C() { }
                                    public C(int i) { }

                                    ~C() { }

                                    void M1() { }
                                    public void M2() { }
                                    void I.M3() { }
                                    partial void M4();
                                    partial void M4() { }

                                    int P1 { get; }
                                    public int P2 { get; }
                                    int I.P3 { get; }

                                    int this[int i] => throw null;
                                    public int this[string s] => throw null;
                                    int I.this[bool b] => throw null;
                                }

                                interface I
                                {
                                    event Action e6;
                                    void M3();
                                    int P3 { get; }
                                    int this[bool b] { get; }
                                }

                                delegate void D();

                                enum E
                                {
                                    EMember
                                }
                            }
                        }
                        """,
                    },
                },
                Options =
                {
                    { CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.OmitIfDefault },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestRefStructsWithOmit()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                namespace Test
                {
                    internal ref struct [|S1|] { }
                }
                """,
                FixedCode = """
                namespace Test
                {
                    ref struct S1 { }
                }
                """,
                Options =
                {
                    { CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.OmitIfDefault },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestReadOnlyStructsWithOmit()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                namespace Test
                {
                    internal readonly struct [|S1|] { }
                }
                """,
                FixedCode = """
                namespace Test
                {
                    readonly struct S1 { }
                }
                """,
                Options =
                {
                    { CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.OmitIfDefault },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestClassOutsideNamespace()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                internal class [|C1|] { }
                """,
                FixedCode = """
                class C1 { }
                """,
                Options =
                {
                    { CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.OmitIfDefault },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestExternMethod()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                using System;
                using System.Runtime.InteropServices;

                internal class Program
                {
                    [DllImport("User32.dll", CharSet = CharSet.Unicode)]
                    static extern int [|MessageBox|](IntPtr h, string m, string c, int type);
                }
                """,
                """
                using System;
                using System.Runtime.InteropServices;

                internal class Program
                {
                    [DllImport("User32.dll", CharSet = CharSet.Unicode)]
                    private static extern int [|MessageBox|](IntPtr h, string m, string c, int type);
                }
                """);
        }

        [Fact]
        public async Task TestVolatile()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                internal class Program
                {
                    volatile int [|x|];
                }
                """,
                """
                internal class Program
                {
                    private volatile int x;
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48899")]
        public async Task TestAbstractMethod()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                public abstract class TestClass
                {
                    abstract string {|CS0621:[|Test|]|} { get; }
                }
                """,
                """
                public abstract class TestClass
                {
                    protected abstract string Test { get; }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48899")]
        public async Task TestOverriddenMethod()
        {
            await VerifyCS.VerifyCodeFixAsync("""
                public abstract class TestClass
                {
                    public abstract string Test { get; }
                }

                public class Derived : TestClass
                {
                    override string {|CS0507:{|CS0621:[|Test|]|}|} { get; }
                }
                """,
                """
                public abstract class TestClass
                {
                    public abstract string Test { get; }
                }

                public class Derived : TestClass
                {
                    public override string Test { get; }
                }
                """);
        }

        [Fact]
        public async Task TestFileScopedNamespaces()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                namespace Test;

                struct [|S1|] { }
                """,
                FixedCode = """
                namespace Test;

                internal struct S1 { }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55703")]
        public async Task TestPartial_WithExistingModifier()
        {
            var source = """
                partial class [|C|]
                {
                }

                public partial class C
                {
                }
                """;
            var fixedSource = """
                public partial class C
                {
                }

                public partial class C
                {
                }
                """;

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp12,
            };

            await test.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58914")]
        public async Task TestStaticOperatorInInterface()
        {
            var source = """
                internal interface I<T> where T : I<T>
                {
                    abstract static int operator +(T x);
                }

                internal class C : I<C>
                {
                    static int I<C>.operator +(C x)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp12,
                ReferenceAssemblies = Testing.ReferenceAssemblies.Net.Net60
            };

            await test.RunAsync();
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("record struct")]
        [InlineData("interface")]
        [InlineData("enum")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/62259")]
        public async Task TestFileDeclaration(string declarationKind)
        {
            var source = $"file {declarationKind} C {{ }}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62259")]
        public async Task TestFileDelegate()
        {
            var source = "file delegate void M();";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("record struct")]
        [InlineData("interface")]
        [InlineData("enum")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/62259")]
        public async Task TestNestedFileDeclaration(string declarationKind)
        {
            var source = $$"""
                file class C1
                {
                    {{declarationKind}} [|C2|] { }
                }
                """;

            var fixedSource = $$"""
                file class C1
                {
                    private {{declarationKind}} C2 { }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29633")]
        public async Task TestTitle1()
        {
            var source = """
                internal class C
                {
                    int [|field|];
                }
                """;
            var fixedSource = """
                internal class C
                {
                    private int field;
                }
                """;

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp12,
                CodeActionEquivalenceKey = nameof(AnalyzersResources.Add_accessibility_modifiers),
            };

            await test.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29633")]
        public async Task TestTitle2()
        {
            var source = """
                class C
                {
                    private int [|field|];
                }
                """;
            var fixedSource = """
                class C
                {
                    int field;
                }
                """;

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp12,
                CodeActionEquivalenceKey = nameof(AnalyzersResources.Remove_accessibility_modifiers),
                Options =
                {
                    { CodeStyleOptions2.AccessibilityModifiersRequired,AccessibilityModifiersRequired.OmitIfDefault }
                }
            };

            await test.RunAsync();
        }
    }
}
