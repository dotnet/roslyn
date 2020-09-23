// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers.CSharpAddAccessibilityModifiersDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers.CSharpAddAccessibilityModifiersCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddAccessibilityModifiers
{
    public class AddAccessibilityModifiersTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public void TestStandardProperties()
            => VerifyCS.VerifyStandardProperties();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public async Task TestAllConstructs()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"using System;
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
}",
                @"using System;
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
}");
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
        public async Task TestRecords()
        {
            var source = @"
record [|Record|]
{
    int [|field|];
}
namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}";
            var fixedSource = @"
internal record Record
{
    private int field;
}
namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}";

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            };

            await test.RunAsync();
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
                        @"using System;
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
}",
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"using System;
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
}",
                    },
                },
                Options =
                {
                    { CodeStyleOptions2.RequireAccessibilityModifiers, AccessibilityModifiersRequired.OmitIfDefault },
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
                    { CodeStyleOptions2.RequireAccessibilityModifiers, AccessibilityModifiersRequired.OmitIfDefault },
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
                    { CodeStyleOptions2.RequireAccessibilityModifiers, AccessibilityModifiersRequired.OmitIfDefault },
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
                    { CodeStyleOptions2.RequireAccessibilityModifiers, AccessibilityModifiersRequired.OmitIfDefault },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public async Task TestExternMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Runtime.InteropServices;

internal class Program
{
    [DllImport(""User32.dll"", CharSet = CharSet.Unicode)]
    static extern int [|MessageBox|](IntPtr h, string m, string c, int type);
}
",
@"
using System;
using System.Runtime.InteropServices;

internal class Program
{
    [DllImport(""User32.dll"", CharSet = CharSet.Unicode)]
    private static extern int [|MessageBox|](IntPtr h, string m, string c, int type);
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public async Task TestVolatile()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
internal class Program
{
    volatile int [|x|];
}
",
@"
internal class Program
{
    private volatile int x;
}
");
        }
    }
}
