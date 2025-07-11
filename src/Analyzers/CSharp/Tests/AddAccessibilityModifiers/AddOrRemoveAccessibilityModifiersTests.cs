// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.AddOrRemoveAccessibilityModifiers;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddOrRemoveAccessibilityModifiers;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpAddOrRemoveAccessibilityModifiersDiagnosticAnalyzer,
    CSharpAddOrRemoveAccessibilityModifiersCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddOrRemoveAccessibilityModifiers)]
public sealed class AddOrRemoveAccessibilityModifiersTests
{
    [Theory, CombinatorialData]
    public void TestStandardProperty(AnalyzerProperty property)
        => VerifyCS.VerifyStandardProperty(property);

    [Fact]
    public Task TestAllConstructs()
        => VerifyCS.VerifyCodeFixAsync(
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

    [Fact]
    public Task TestRefStructs()
        => VerifyCS.VerifyCodeFixAsync("""
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

    [Fact]
    public async Task TestRecords()
    {
        var test = new VerifyCS.Test
        {
            TestCode = """
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
            """,
            FixedCode = """
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
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task TestRecordStructs()
    {
        var test = new VerifyCS.Test
        {
            TestCode = """
            record struct [|Record|]
            {
                int [|field|];
            }
            """,
            FixedCode = """
            internal record struct Record
            {
                private int field;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        };

        await test.RunAsync();
    }

    [Fact]
    public Task TestReadOnlyStructs()
        => VerifyCS.VerifyCodeFixAsync("""
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

    [Fact]
    public Task TestAllConstructsWithOmit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestRefStructsWithOmit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestReadOnlyStructsWithOmit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestClassOutsideNamespace()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestExternMethod()
        => VerifyCS.VerifyCodeFixAsync("""
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

    [Fact]
    public Task TestVolatile()
        => VerifyCS.VerifyCodeFixAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48899")]
    public Task TestAbstractMethod()
        => VerifyCS.VerifyCodeFixAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48899")]
    public Task TestOverriddenMethod()
        => VerifyCS.VerifyCodeFixAsync("""
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

    [Fact]
    public Task TestFileScopedNamespaces()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55703")]
    public async Task TestPartial_WithExistingModifier()
    {
        var test = new VerifyCS.Test
        {
            TestCode = """
            partial class [|C|]
            {
            }

            public partial class C
            {
            }
            """,
            FixedCode = """
            public partial class C
            {
            }

            public partial class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58914")]
    public async Task TestStaticOperatorInInterface()
    {
        var test = new VerifyCS.Test
        {
            TestCode = """
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
            """,
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
    public Task TestFileDeclaration(string declarationKind)
        => new VerifyCS.Test
        {
            TestCode = $"file {declarationKind} C {{ }}",
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62259")]
    public Task TestFileDelegate()
        => new VerifyCS.Test
        {
            TestCode = "file delegate void M();",
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record")]
    [InlineData("record struct")]
    [InlineData("interface")]
    [InlineData("enum")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/62259")]
    public Task TestNestedFileDeclaration(string declarationKind)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            file class C1
            {
                {{declarationKind}} [|C2|] { }
            }
            """,
            FixedCode = $$"""
            file class C1
            {
                private {{declarationKind}} C2 { }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29633")]
    public async Task TestTitle1()
    {
        var test = new VerifyCS.Test
        {
            TestCode = """
            internal class C
            {
                int [|field|];
            }
            """,
            FixedCode = """
            internal class C
            {
                private int field;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            CodeActionEquivalenceKey = nameof(AnalyzersResources.Add_accessibility_modifiers),
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29633")]
    public Task TestTitle2()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                private int [|field|];
            }
            """,
            FixedCode = """
            class C
            {
                int field;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            CodeActionEquivalenceKey = nameof(AnalyzersResources.Remove_accessibility_modifiers),
            Options =
            {
                { CodeStyleOptions2.AccessibilityModifiersRequired,AccessibilityModifiersRequired.OmitIfDefault }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74244")]
    public Task TestAlwaysForInterfaceMembers()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                interface [|I|]
                {
                    void [|M|]();
                    int [|P|] { get; }
                    event Action [|E|];

                    class [|Nested|]
                    {
                        void [|M|]() { }
                        int [|P|] { get; }
                        event Action [|E|];
                    }
                }
                """,
            FixedCode = """
                using System;
                
                internal interface I
                {
                    public void M();
                    public int P { get; }
                    public event Action E;
                
                    public class Nested
                    {
                        private void M() { }
                        private int P { get; }
                        private event Action E;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            CodeActionEquivalenceKey = nameof(AnalyzersResources.Add_accessibility_modifiers),
            Options =
            {
                { CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Always }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74244")]
    public Task TestOmitIfDefaultForInterfaceMembers()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                
                interface I
                {
                    public void [|M|]();
                    public int [|P|] { get; }
                    public event Action [|E|];
                
                    public class [|Nested|]
                    {
                        private void [|M|]() { }
                        private int [|P|] { get; }
                        private event Action [|E|];
                    }
                }
                """,
            FixedCode = """
                using System;
                
                interface I
                {
                    void M();
                    int P { get; }
                    event Action E;
                
                    class Nested
                    {
                        void M() { }
                        int P { get; }
                        event Action E;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            CodeActionEquivalenceKey = nameof(AnalyzersResources.Remove_accessibility_modifiers),
            Options =
            {
                { CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.OmitIfDefault }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74244")]
    public Task TestForNonInterfaceMembers()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                
                internal interface I
                {
                    public void [|M|]();
                    public int [|P|] { get; }
                    public event Action [|E|];
                
                    public class [|Nested|]
                    {
                        private void M() { }
                        private int P { get; }
                        private event Action E;
                    }
                }
                """,
            FixedCode = """
                using System;
                
                internal interface I
                {
                    void M();
                    int P { get; }
                    event Action E;
                
                    class Nested
                    {
                        private void M() { }
                        private int P { get; }
                        private event Action E;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            CodeActionEquivalenceKey = nameof(AnalyzersResources.Remove_accessibility_modifiers),
            Options =
            {
                { CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.ForNonInterfaceMembers }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78395")]
    public Task TestOmitIfDefaultWithFixedField()
        => new VerifyCS.Test
        {
            TestCode = """
                unsafe struct MyStruct
                {
                    private fixed long [|_data|][100];
                }
                """,
            FixedCode = """
                unsafe struct MyStruct
                {
                    fixed long _data[100];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            CodeActionEquivalenceKey = nameof(AnalyzersResources.Remove_accessibility_modifiers),
            Options =
            {
                { CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.OmitIfDefault }
            }
        }.RunAsync();
}
