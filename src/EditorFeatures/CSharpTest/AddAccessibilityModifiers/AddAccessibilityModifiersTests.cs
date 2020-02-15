// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers.CSharpAddAccessibilityModifiersDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers.CSharpAddAccessibilityModifiersCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddAccessibilityModifiers
{
    public partial class AddAccessibilityModifiersTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpAddAccessibilityModifiersDiagnosticAnalyzer(), new CSharpAddAccessibilityModifiersCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public async Task TestAllConstructs()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
namespace Outer
{
    namespace Inner1.Inner2
    {
        class [|C|]
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
            event Action I.e6 { add { } remote { } }

            static C() { }
            [|C|]() { }
            public C(int i) { }

            ~C() { }

            void [|M1|]() { }
            public void M2() { }
            void I.M3() { }
            partial void M4() { }

            int [|P1|] { get; }
            public int P2 { get; }
            int I.P3 { get; }

            int [|this|][int i] { get; }
            public int this[string s] { get; }
            int I.this[bool b] { get; }
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
}",
                    },
                    ExpectedDiagnostics =
                    {
                        // Test0.cs(16,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(16, 19, 16, 25).WithArguments("Action"),
                        // Test0.cs(17,26): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(17, 26, 17, 32).WithArguments("Action"),
                        // Test0.cs(19,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(19, 19, 19, 25).WithArguments("Action"),
                        // Test0.cs(20,26): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(20, 26, 20, 32).WithArguments("Action"),
                        // Test0.cs(21,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(21, 19, 21, 25).WithArguments("Action"),
                        // Test0.cs(21,26): error CS0540: 'C.I.e6': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(21, 26, 21, 27).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.e6", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(21,28): error CS0065: 'C.I.e6': event property must have both add and remove accessors
                        DiagnosticResult.CompilerError("CS0065").WithSpan(21, 28, 21, 30).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.e6"),
                        // Test0.cs(21,41): error CS1055: An add or remove accessor expected
                        DiagnosticResult.CompilerError("CS1055").WithSpan(21, 41, 21, 47),
                        // Test0.cs(31,18): error CS0540: 'C.I.M3()': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(31, 18, 31, 19).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.M3()", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(32,26): error CS0751: A partial method must be declared within a partial class, partial struct, or partial interface
                        DiagnosticResult.CompilerError("CS0751").WithSpan(32, 26, 32, 28),
                        // Test0.cs(32,26): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M4()'
                        DiagnosticResult.CompilerError("CS0759").WithSpan(32, 26, 32, 28).WithArguments("Outer.Inner1.Inner2.C.M4()"),
                        // Test0.cs(36,17): error CS0540: 'C.I.P3': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(36, 17, 36, 18).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.P3", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(38,31): error CS0501: 'C.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(38, 31, 38, 34).WithArguments("Outer.Inner1.Inner2.C.this[int].get"),
                        // Test0.cs(39,41): error CS0501: 'C.this[string].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(39, 41, 39, 44).WithArguments("Outer.Inner1.Inner2.C.this[string].get"),
                        // Test0.cs(40,17): error CS0540: 'C.I.this[bool]': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(40, 17, 40, 18).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.this[bool]", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(40,34): error CS0501: 'C.I.this[bool].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(40, 34, 40, 37).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.this[bool].get"),
                        // Test0.cs(45,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(45, 19, 45, 25).WithArguments("Action"),
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"
namespace Outer
{
    namespace Inner1.Inner2
    {
        internal class C
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
            event Action I.e6 { add { } remote { } }

            static C() { }

            private C() { }
            public C(int i) { }

            ~C() { }

            private void M1() { }
            public void M2() { }
            void I.M3() { }
            partial void M4() { }

            private int P1 { get; }
            public int P2 { get; }
            int I.P3 { get; }

            private int this[int i] { get; }
            public int this[string s] { get; }
            int I.this[bool b] { get; }
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
}",
                    },
                    ExpectedDiagnostics =
                    {
                        // Test0.cs(16,27): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(16, 27, 16, 33).WithArguments("Action"),
                        // Test0.cs(17,26): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(17, 26, 17, 32).WithArguments("Action"),
                        // Test0.cs(19,27): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(19, 27, 19, 33).WithArguments("Action"),
                        // Test0.cs(20,26): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(20, 26, 20, 32).WithArguments("Action"),
                        // Test0.cs(21,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(21, 19, 21, 25).WithArguments("Action"),
                        // Test0.cs(21,26): error CS0540: 'C.I.e6': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(21, 26, 21, 27).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.e6", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(21,28): error CS0065: 'C.I.e6': event property must have both add and remove accessors
                        DiagnosticResult.CompilerError("CS0065").WithSpan(21, 28, 21, 30).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.e6"),
                        // Test0.cs(21,41): error CS1055: An add or remove accessor expected
                        DiagnosticResult.CompilerError("CS1055").WithSpan(21, 41, 21, 47),
                        // Test0.cs(32,18): error CS0540: 'C.I.M3()': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(32, 18, 32, 19).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.M3()", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(33,26): error CS0751: A partial method must be declared within a partial class, partial struct, or partial interface
                        DiagnosticResult.CompilerError("CS0751").WithSpan(33, 26, 33, 28),
                        // Test0.cs(33,26): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M4()'
                        DiagnosticResult.CompilerError("CS0759").WithSpan(33, 26, 33, 28).WithArguments("Outer.Inner1.Inner2.C.M4()"),
                        // Test0.cs(37,17): error CS0540: 'C.I.P3': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(37, 17, 37, 18).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.P3", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(39,39): error CS0501: 'C.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(39, 39, 39, 42).WithArguments("Outer.Inner1.Inner2.C.this[int].get"),
                        // Test0.cs(40,41): error CS0501: 'C.this[string].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(40, 41, 40, 44).WithArguments("Outer.Inner1.Inner2.C.this[string].get"),
                        // Test0.cs(41,17): error CS0540: 'C.I.this[bool]': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(41, 17, 41, 18).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.this[bool]", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(41,34): error CS0501: 'C.I.this[bool].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(41, 34, 41, 37).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.this[bool].get"),
                        // Test0.cs(46,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(46, 19, 46, 25).WithArguments("Action"),
                    },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public async Task TestRefStructs()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
namespace Test
{
    ref struct [|S1|] { }
}", @"
namespace Test
{
    internal ref struct S1 { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public async Task TestReadOnlyStructs()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
namespace Test
{
    readonly struct [|S1|] { }
}", @"
namespace Test
{
    internal readonly struct S1 { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public async Task TestAllConstructsWithOmit()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
namespace Outer
{
    namespace Inner1.Inner2
    {
        internal class [|C|]
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
            event Action I.e6 { add { } remote { } }

            static C() { }

            private [|C|]() { }
            public C(int i) { }

            ~C() { }

            private void [|M1|]() { }
            public void M2() { }
            void I.M3() { }
            partial void M4() { }

            private int [|P1|] { get; }
            public int P2 { get; }
            int I.P3 { get; }

            private int [|this|][int i] { get; }
            public int this[string s] { get; }
            int I.this[bool b] { get; }
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
}",
                    },
                    ExpectedDiagnostics =
                    {
                        // Test0.cs(16,27): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(16, 27, 16, 33).WithArguments("Action"),
                        // Test0.cs(17,26): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(17, 26, 17, 32).WithArguments("Action"),
                        // Test0.cs(19,27): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(19, 27, 19, 33).WithArguments("Action"),
                        // Test0.cs(20,26): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(20, 26, 20, 32).WithArguments("Action"),
                        // Test0.cs(21,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(21, 19, 21, 25).WithArguments("Action"),
                        // Test0.cs(21,26): error CS0540: 'C.I.e6': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(21, 26, 21, 27).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.e6", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(21,28): error CS0065: 'C.I.e6': event property must have both add and remove accessors
                        DiagnosticResult.CompilerError("CS0065").WithSpan(21, 28, 21, 30).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.e6"),
                        // Test0.cs(21,41): error CS1055: An add or remove accessor expected
                        DiagnosticResult.CompilerError("CS1055").WithSpan(21, 41, 21, 47),
                        // Test0.cs(32,18): error CS0540: 'C.I.M3()': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(32, 18, 32, 19).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.M3()", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(33,26): error CS0751: A partial method must be declared within a partial class, partial struct, or partial interface
                        DiagnosticResult.CompilerError("CS0751").WithSpan(33, 26, 33, 28),
                        // Test0.cs(33,26): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M4()'
                        DiagnosticResult.CompilerError("CS0759").WithSpan(33, 26, 33, 28).WithArguments("Outer.Inner1.Inner2.C.M4()"),
                        // Test0.cs(37,17): error CS0540: 'C.I.P3': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(37, 17, 37, 18).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.P3", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(39,39): error CS0501: 'C.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(39, 39, 39, 42).WithArguments("Outer.Inner1.Inner2.C.this[int].get"),
                        // Test0.cs(40,41): error CS0501: 'C.this[string].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(40, 41, 40, 44).WithArguments("Outer.Inner1.Inner2.C.this[string].get"),
                        // Test0.cs(41,17): error CS0540: 'C.I.this[bool]': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(41, 17, 41, 18).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.this[bool]", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(41,34): error CS0501: 'C.I.this[bool].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(41, 34, 41, 37).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.this[bool].get"),
                        // Test0.cs(46,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(46, 19, 46, 25).WithArguments("Action"),
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"
namespace Outer
{
    namespace Inner1.Inner2
    {
        class C
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
            event Action I.e6 { add { } remote { } }

            static C() { }

            C() { }
            public C(int i) { }

            ~C() { }

            void M1() { }
            public void M2() { }
            void I.M3() { }
            partial void M4() { }

            int P1 { get; }
            public int P2 { get; }
            int I.P3 { get; }

            int this[int i] { get; }
            public int this[string s] { get; }
            int I.this[bool b] { get; }
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
}",
                    },
                    ExpectedDiagnostics =
                    {
                        // Test0.cs(16,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(16, 19, 16, 25).WithArguments("Action"),
                        // Test0.cs(17,26): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(17, 26, 17, 32).WithArguments("Action"),
                        // Test0.cs(19,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(19, 19, 19, 25).WithArguments("Action"),
                        // Test0.cs(20,26): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(20, 26, 20, 32).WithArguments("Action"),
                        // Test0.cs(21,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(21, 19, 21, 25).WithArguments("Action"),
                        // Test0.cs(21,26): error CS0540: 'C.I.e6': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(21, 26, 21, 27).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.e6", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(21,28): error CS0065: 'C.I.e6': event property must have both add and remove accessors
                        DiagnosticResult.CompilerError("CS0065").WithSpan(21, 28, 21, 30).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.e6"),
                        // Test0.cs(21,41): error CS1055: An add or remove accessor expected
                        DiagnosticResult.CompilerError("CS1055").WithSpan(21, 41, 21, 47),
                        // Test0.cs(32,18): error CS0540: 'C.I.M3()': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(32, 18, 32, 19).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.M3()", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(33,26): error CS0751: A partial method must be declared within a partial class, partial struct, or partial interface
                        DiagnosticResult.CompilerError("CS0751").WithSpan(33, 26, 33, 28),
                        // Test0.cs(33,26): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M4()'
                        DiagnosticResult.CompilerError("CS0759").WithSpan(33, 26, 33, 28).WithArguments("Outer.Inner1.Inner2.C.M4()"),
                        // Test0.cs(37,17): error CS0540: 'C.I.P3': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(37, 17, 37, 18).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.P3", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(39,31): error CS0501: 'C.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(39, 31, 39, 34).WithArguments("Outer.Inner1.Inner2.C.this[int].get"),
                        // Test0.cs(40,41): error CS0501: 'C.this[string].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(40, 41, 40, 44).WithArguments("Outer.Inner1.Inner2.C.this[string].get"),
                        // Test0.cs(41,17): error CS0540: 'C.I.this[bool]': containing type does not implement interface 'I'
                        DiagnosticResult.CompilerError("CS0540").WithSpan(41, 17, 41, 18).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.this[bool]", "Outer.Inner1.Inner2.I"),
                        // Test0.cs(41,34): error CS0501: 'C.I.this[bool].get' must declare a body because it is not marked abstract, extern, or partial
                        DiagnosticResult.CompilerError("CS0501").WithSpan(41, 34, 41, 37).WithArguments("Outer.Inner1.Inner2.C.Outer.Inner1.Inner2.I.this[bool].get"),
                        // Test0.cs(46,19): error CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(46, 19, 46, 25).WithArguments("Action"),
                    },
                },
                Options =
                {
                    { CodeStyleOptions.RequireAccessibilityModifiers, AccessibilityModifiersRequired.OmitIfDefault },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public async Task TestRefStructsWithOmit()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace Test
{
    internal ref struct [|S1|] { }
}",
                FixedCode = @"
namespace Test
{
    ref struct S1 { }
}",
                Options =
                {
                    { CodeStyleOptions.RequireAccessibilityModifiers, AccessibilityModifiersRequired.OmitIfDefault },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public async Task TestReadOnlyStructsWithOmit()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace Test
{
    internal readonly struct [|S1|] { }
}",
                FixedCode = @"
namespace Test
{
    readonly struct S1 { }
}",
                Options =
                {
                    { CodeStyleOptions.RequireAccessibilityModifiers, AccessibilityModifiersRequired.OmitIfDefault },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public async Task TestClassOutsideNamespace()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
internal class [|C1|] { }",
                FixedCode = @"
class C1 { }",
                Options =
                {
                    { CodeStyleOptions.RequireAccessibilityModifiers, AccessibilityModifiersRequired.OmitIfDefault },
                },
            }.RunAsync();
        }
    }
}
