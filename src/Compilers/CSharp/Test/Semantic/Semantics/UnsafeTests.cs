// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) lock statements.
    /// </summary>
    public class UnsafeTests : CompilingTestBase
    {
        private static string GetEscapedNewLine()
        {
            if (Environment.NewLine == "\n")
            {
                return @"\n";
            }
            else if (Environment.NewLine == "\r\n")
            {
                return @"\r\n";
            }
            else
            {
                throw new Exception("Unrecognized new line");
            }
        }

        #region Unsafe regions

        [Fact]
        public void FixedSizeBuffer()
        {
            var text1 = @"
using System;
using System.Runtime.InteropServices;

public static class R
{
    public unsafe struct S
    {
        public fixed byte Buffer[16];
    }
}";
            var comp1 = CreateCompilation(text1, assemblyName: "assembly1", references: new[] { MscorlibRef_v20 },
                options: TestOptions.UnsafeDebugDll);

            var ref1 = comp1.EmitToImageReference();

            var text2 = @"
using System;

class C
{
    unsafe void M(byte* p)
    {
        R.S* p2 = (R.S*)p;
        IntPtr p3 = M2((IntPtr)p2[0].Buffer);
    }

    unsafe IntPtr M2(IntPtr p) => p;
}";
            var comp2 = CreateCompilationWithMscorlib45(text2,
                references: new[] { ref1 },
                options: TestOptions.UnsafeDebugDll);
            comp2.VerifyDiagnostics(
    // warning CS1701: Assuming assembly reference 'mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' used by 'assembly1' matches identity 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' of 'mscorlib', you may need to supply runtime policy
    Diagnostic(ErrorCode.WRN_UnifyReferenceMajMin).WithArguments("mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "assembly1", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "mscorlib").WithLocation(1, 1),
    // warning CS1701: Assuming assembly reference 'mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' used by 'assembly1' matches identity 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' of 'mscorlib', you may need to supply runtime policy
    Diagnostic(ErrorCode.WRN_UnifyReferenceMajMin).WithArguments("mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "assembly1", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "mscorlib").WithLocation(1, 1),
    // warning CS1701: Assuming assembly reference 'mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' used by 'assembly1' matches identity 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' of 'mscorlib', you may need to supply runtime policy
    Diagnostic(ErrorCode.WRN_UnifyReferenceMajMin).WithArguments("mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "assembly1", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "mscorlib").WithLocation(1, 1),
    // warning CS1701: Assuming assembly reference 'mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' used by 'assembly1' matches identity 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' of 'mscorlib', you may need to supply runtime policy
    Diagnostic(ErrorCode.WRN_UnifyReferenceMajMin).WithArguments("mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "assembly1", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "mscorlib").WithLocation(1, 1),
    // warning CS1701: Assuming assembly reference 'mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' used by 'assembly1' matches identity 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' of 'mscorlib', you may need to supply runtime policy
    Diagnostic(ErrorCode.WRN_UnifyReferenceMajMin).WithArguments("mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "assembly1", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "mscorlib").WithLocation(1, 1));
        }

        [Fact]
        public void CompilationNotUnsafe1()
        {
            var text = @"
unsafe class C
{
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll.WithAllowUnsafe(false)).VerifyDiagnostics(
                // (2,14): error CS0227: Unsafe code may only appear if compiling with /unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "C"));

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void CompilationNotUnsafe2()
        {
            var text = @"
class C
{
    unsafe void Foo()
    {
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll.WithAllowUnsafe(false)).VerifyDiagnostics(
                // (4,17): error CS0227: Unsafe code may only appear if compiling with /unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "Foo"));

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void CompilationNotUnsafe3()
        {
            var text = @"
class C
{
    void Foo()
    {
        unsafe { }
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll.WithAllowUnsafe(false)).VerifyDiagnostics(
                // (6,9): error CS0227: Unsafe code may only appear if compiling with /unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe"));

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void IteratorUnsafe1()
        {
            var text = @"
unsafe class C
{
    System.Collections.Generic.IEnumerator<int> Foo()
    {
        yield return 1;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void IteratorUnsafe2()
        {
            var text = @"
class C
{
    unsafe System.Collections.Generic.IEnumerator<int> Foo()
    {
        yield return 1;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,56): error CS1629: Unsafe code may not appear in iterators
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "Foo"));
        }

        [Fact]
        public void IteratorUnsafe3()
        {
            var text = @"
class C
{
    System.Collections.Generic.IEnumerator<int> Foo()
    {
        unsafe { }
        yield return 1;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,9): error CS1629: Unsafe code may not appear in iterators
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "unsafe"));
        }

        [Fact]
        public void IteratorUnsafe4()
        {
            var text = @"
unsafe class C
{
    System.Collections.Generic.IEnumerator<int> Foo()
    {
        unsafe { }
        yield return 1;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,9): error CS1629: Unsafe code may not appear in iterators
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "unsafe"));
        }

        [WorkItem(546657, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546657")]
        [Fact]
        public void IteratorUnsafe5()
        {
            var text = @"
unsafe class C
{
    System.Collections.Generic.IEnumerator<int> Foo()
    {
        System.Action a = () => { unsafe { } };
        yield return 1;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,9): error CS1629: Unsafe code may not appear in iterators
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "unsafe"));
        }

        [Fact]
        public void UnsafeModifier()
        {
            var text = @"
unsafe class C
{
    unsafe C() { }
    unsafe ~C() { }
    unsafe static void Static() { }
    unsafe void Instance() { }
    unsafe struct Inner { }
    unsafe int field = 1;
    unsafe event System.Action Event;
    unsafe int Property { get; set; }
    unsafe int this[int x] { get { return field; } set { } }
    unsafe public static C operator +(C c1, C c2) { return c1; }
    unsafe public static implicit operator int(C c) { return 0; }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (10,32): warning CS0067: The event 'C.Event' is never used
                //     unsafe event System.Action Event;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("C.Event"));
        }

        [Fact]
        public void TypeIsUnsafe()
        {
            var text = @"
unsafe class C<T>
{
    int* f0;
    int** f1;
    int*[] f2;
    int*[][] f3;
    C<int*> f4;
    C<int**> f5;
    C<int*[]> f6;
    C<int*[][]> f7;
}
";

            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics(
                // (8,7): error CS0306: The type 'int*' may not be used as a type argument
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "int*").WithArguments("int*"),
                // (9,7): error CS0306: The type 'int**' may not be used as a type argument
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "int**").WithArguments("int**"),

                // (4,10): warning CS0169: The field 'C<T>.f0' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f0").WithArguments("C<T>.f0"),
                // (5,11): warning CS0169: The field 'C<T>.f1' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f1").WithArguments("C<T>.f1"),
                // (6,12): warning CS0169: The field 'C<T>.f2' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f2").WithArguments("C<T>.f2"),
                // (7,14): warning CS0169: The field 'C<T>.f3' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f3").WithArguments("C<T>.f3"),
                // (8,13): warning CS0169: The field 'C<T>.f4' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f4").WithArguments("C<T>.f4"),
                // (9,14): warning CS0169: The field 'C<T>.f5' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f5").WithArguments("C<T>.f5"),
                // (10,15): warning CS0169: The field 'C<T>.f6' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f6").WithArguments("C<T>.f6"),
                // (11,17): warning CS0169: The field 'C<T>.f7' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f7").WithArguments("C<T>.f7"));

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            var fieldTypes = Enumerable.Range(0, 8).Select(i => type.GetMember<FieldSymbol>("f" + i).Type).ToArray();

            Assert.True(fieldTypes[0].IsUnsafe());
            Assert.True(fieldTypes[1].IsUnsafe());
            Assert.True(fieldTypes[2].IsUnsafe());
            Assert.True(fieldTypes[3].IsUnsafe());

            Assert.False(fieldTypes[4].IsUnsafe());
            Assert.False(fieldTypes[5].IsUnsafe());
            Assert.False(fieldTypes[6].IsUnsafe());
            Assert.False(fieldTypes[7].IsUnsafe());
        }

        [Fact]
        public void UnsafeFieldTypes()
        {
            var template = @"
{0} class C
{{
    public {1} int* f = null, g = null;
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     public  int* f = null, g = null;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*")
                );
        }

        [Fact]
        public void UnsafeLocalTypes()
        {
            var template = @"
{0} class C
{{
    void M()
    {{
        {1} 
        {{
            int* f = null, g = null;
        }}
    }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"));
        }

        [Fact]
        public void UnsafeMethodSignatures()
        {
            var template = @"
{0} interface I
{{
    {1} int* M(long* p, byte* q);
}}

{0} class C
{{
    {1} int* M(long* p, byte* q) {{ throw null; }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "long*"),
                // (4,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "byte*"),
                // (4,6): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),

                // (9,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "long*"),
                // (9,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "byte*"),
                // (9,6): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"));
        }

        [Fact]
        public void DelegateSignatures()
        {
            var template = @"
{0} class C
{{
    {1} delegate int* M(long* p, byte* q);
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "long*"),
                // (4,31): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "byte*"),
                // (4,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"));
        }

        [Fact]
        public void UnsafeConstructorSignatures()
        {
            var template = @"
{0} class C
{{
    {1} C(long* p, byte* q) {{ throw null; }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,8): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "long*"),
                // (4,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "byte*"));
        }

        [Fact]
        public void UnsafeOperatorSignatures()
        {
            var template = @"
{0} class C
{{
    public static {1} C operator +(C c, int* p) {{ throw null; }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,38): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"));
        }

        [Fact]
        public void UnsafeConversionSignatures()
        {
            var template = @"
{0} class C
{{
    public static {1} explicit operator C(int* p) {{ throw null; }}
    public static {1} explicit operator byte*(C c) {{ throw null; }}
    public static {1} implicit operator C(short* p) {{ throw null; }}
    public static {1} implicit operator long*(C c) {{ throw null; }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,40): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (5,38): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "byte*"),
                // (6,40): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "short*"),
                // (7,38): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "long*"));
        }

        [Fact]
        public void UnsafePropertySignatures()
        {
            var template = @"
{0} interface I
{{
    {1} int* P {{ get; set; }}
}}

{0} class C
{{
    {1} int* P {{ get; set; }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,6): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,6): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"));
        }

        [Fact]
        public void UnsafeIndexerSignatures()
        {
            var template = @"
{0} interface I
{{
    {1} int* this[long* p, byte* q] {{ get; set; }}
}}

{0} class C
{{
    {1} int* this[long* p, byte* q] {{ get {{ throw null; }} set {{ throw null; }} }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,6): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (4,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "long*"),
                // (4,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "byte*"),
                // (9,6): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "long*"),
                // (9,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "byte*"));
        }

        [Fact]
        public void UnsafeEventSignatures()
        {
            var template = @"
{0} interface I
{{
    {1} event int* E;
}}

{0} class C
{{
    {1} event int* E1;
    {1} event int* E2 {{ add {{ }} remove {{ }} }}
}}
";
            DiagnosticDescription[] expected =
            {
                // (4,17): error CS0066: 'I.E': event must be of a delegate type
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "E").WithArguments("I.E"),
                // (9,17): error CS0066: 'C.E1': event must be of a delegate type
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "E1").WithArguments("C.E1"),
                // (10,17): error CS0066: 'C.E2': event must be of a delegate type
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "E2").WithArguments("C.E2"),
                // (9,17): warning CS0067: The event 'C.E1' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("C.E1")
            };

            CompareUnsafeDiagnostics(template, expected, expected);
        }

        [Fact]
        public void UnsafeTypeArguments()
        {
            var template = @"
{0} interface I<T>
{{
    {1} void Test(I<int*> i);
}}

{0} class C<T>
{{
    {1} void Test(C<int*> c) {{ }}
}}
";
            DiagnosticDescription[] expected =
            {
                // (4,24): error CS0306: The type 'int*' may not be used as a type argument
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "i").WithArguments("int*"),
                // (9,24): error CS0306: The type 'int*' may not be used as a type argument
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "c").WithArguments("int*")
            };

            CompareUnsafeDiagnostics(template, expected, expected);
        }

        [Fact]
        public void UnsafeExpressions1()
        {
            var template = @"
{0} class C
{{
    void Test()
    {{
        {1}
        {{
            Unsafe(); //CS0214
        }}

        {1}
        {{
            var x = Unsafe(); //CS0214
        }}

        {1}
        {{
            var x = Unsafe(); //CS0214
            var y = Unsafe(); //CS0214 suppressed
        }}

        {1}
        {{
            Unsafe(null); //CS0214
        }}
    }}

    {1} int* Unsafe() {{ return null; }} //CS0214
    {1} void Unsafe(int* p) {{ }} //CS0214
}}
";

            CompareUnsafeDiagnostics(template,
                // (28,6): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      int* Unsafe() { return null; } //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (29,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      void Unsafe(int* p) { } //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (8,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             Unsafe(); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (13,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             var x = Unsafe(); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (18,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             var x = Unsafe(); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (19,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             var y = Unsafe(); //CS0214 suppressed
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (24,20): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             Unsafe(null); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (24,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             Unsafe(null); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe(null)")
                );
        }

        [Fact]
        public void UnsafeExpressions2()
        {
            var template = @"
{0} class C
{{
    {1} int* Field = Unsafe(); //CS0214 * 2

    {1} C()
    {{
        Unsafe(); //CS0214
    }}

    {1} ~C()
    {{
        Unsafe(); //CS0214
    }}

    {1} void Test()
    {{
        Unsafe(); //CS0214
    }}

    {1} event System.Action E
    {{
        add {{ Unsafe(); }} //CS0214
        remove {{ Unsafe(); }} //CS0214
    }}

    {1} int P
    {{
        set {{ Unsafe(); }} //CS0214
    }}

    {1} int this[int x]
    {{
        set {{ Unsafe(); }} //CS0214
    }}

    {1} public static implicit operator int(C c)
    {{
        Unsafe(); //CS0214
        return 0;
    }}

    {1} public static C operator +(C c)
    {{
        Unsafe(); //CS0214
        return c;
    }}

    {1} static int* Unsafe() {{ return null; }} //CS0214
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,6): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      int* Field = Unsafe(); //CS0214 * 2
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (4,19): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      int* Field = Unsafe(); //CS0214 * 2
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (8,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Unsafe(); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (13,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Unsafe(); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (18,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Unsafe(); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (23,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         add { Unsafe(); } //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (24,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         remove { Unsafe(); } //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (29,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         set { Unsafe(); } //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (34,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         set { Unsafe(); } //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (39,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Unsafe(); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (45,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Unsafe(); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (49,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      static int* Unsafe() { return null; } //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"));
        }

        [Fact]
        public void UnsafeExpressions3()
        {
            var template = @"
{0} class C
{{
    {1} void Test(int* p = Unsafe()) //CS0214 * 2
    {{
        System.Action a1 = () => Unsafe(); //CS0214

        System.Action a2 = () =>
        {{
            Unsafe(); //CS0214
        }};
    }}

    {1} static int* Unsafe() {{ return null; }} //CS0214
}}
";

            DiagnosticDescription[] expectedWithoutUnsafe =
            {
                // (4,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      void Test(int* p = Unsafe()) //CS0214 * 2
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (4,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      void Test(int* p = Unsafe()) //CS0214 * 2
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (4,25): error CS1736: Default parameter value for 'p' must be a compile-time constant
                //      void Test(int* p = Unsafe()) //CS0214 * 2
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "Unsafe()").WithArguments("p"),
                // (6,34): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         System.Action a1 = () => Unsafe(); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (10,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             Unsafe(); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Unsafe()"),
                // (14,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      static int* Unsafe() { return null; } //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*")
            };

            DiagnosticDescription[] expectedWithUnsafe =
            {
                // (4,25): error CS1736: Default parameter value for 'p' must be a compile-time constant
                //      void Test(int* p = Unsafe()) //CS0214 * 2
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "Unsafe()").WithArguments("p")
            };

            CompareUnsafeDiagnostics(template, expectedWithoutUnsafe, expectedWithUnsafe);
        }

        [Fact]
        public void UnsafeIteratorSignatures()
        {
            var template = @"
{0} class C
{{
    {1} System.Collections.Generic.IEnumerable<int> Iterator(int* p)
    {{
        yield return 1;
    }}
}}
";

            var withoutUnsafe = string.Format(template, "", "");
            CreateCompilationWithMscorlib(withoutUnsafe, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // CONSIDER: We should probably suppress CS0214 (like Dev10 does) because it's
                // confusing, but we don't have a good way to do so, because we don't know that
                // the method is an iterator until we bind the body and we certainly don't want
                // to do that just to figure out the types of the parameters.

                // (4,59): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (4,64): error CS1637: Iterators cannot have unsafe parameters or yield types
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "p"));

            var withUnsafeOnType = string.Format(template, "unsafe", "");
            CreateCompilationWithMscorlib(withUnsafeOnType, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,64): error CS1637: Iterators cannot have unsafe parameters or yield types
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "p"));

            var withUnsafeOnMembers = string.Format(template, "", "unsafe");
            CreateCompilationWithMscorlib(withUnsafeOnMembers, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,64): error CS1637: Iterators cannot have unsafe parameters or yield types
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "p"),
                // (4,56): error CS1629: Unsafe code may not appear in iterators
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "Iterator")); //this is for putting "unsafe" on an iterator, not for the parameter type

            var withUnsafeOnTypeAndMembers = string.Format(template, "unsafe", "unsafe");
            CreateCompilationWithMscorlib(withUnsafeOnTypeAndMembers, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,64): error CS1637: Iterators cannot have unsafe parameters or yield types
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "p"),
                // (4,56): error CS1629: Unsafe code may not appear in iterators
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "Iterator")); //this is for putting "unsafe" on an iterator, not for the parameter type
        }

        [Fact]
        public void UnsafeInAttribute1()
        {
            var text = @"
unsafe class Attr : System.Attribute
{
    [Attr(null)] // Dev10: doesn't matter that the type and member are both 'unsafe'
    public unsafe Attr(int* i)
    {
    }
}
";
            // CONSIDER: Dev10 reports CS0214 (unsafe) and CS0182 (not a constant), but this makes
            // just as much sense.
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,6): error CS0181: Attribute constructor parameter 'i' has type 'int*', which is not a valid attribute parameter type
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Attr").WithArguments("i", "int*"));
        }

        [Fact]
        public void UnsafeInAttribute2()
        {
            var text = @"
unsafe class Attr : System.Attribute
{
    [Attr(Unsafe() == null)] // Not a constant
    public unsafe Attr(bool b)
    {
    }

    static int* Unsafe()
    {
        return null;
    }
}
";
            // CONSIDER: Dev10 reports both CS0214 (unsafe) and CS0182 (not a constant), but this makes
            // just as much sense.
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "Unsafe() == null"));
        }

        [Fact]
        public void TypeofNeverUnsafe()
        {
            var text = @"
class C<T>
{
    void Test()
    {
        System.Type t;

        t = typeof(int*);
        t = typeof(int**);
        t = typeof(int*[]);
        t = typeof(int*[][]);

        t = typeof(C<int*>); // CS0306
        t = typeof(C<int**>); // CS0306
        t = typeof(C<int*[]>);
        t = typeof(C<int*[][]>);
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (13,22): error CS0306: The type 'int*' may not be used as a type argument
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "int*").WithArguments("int*"),
                // (14,22): error CS0306: The type 'int**' may not be used as a type argument
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "int**").WithArguments("int**"));
        }

        [Fact]
        public void UnsafeOnEnum()
        {
            var text = @"
unsafe enum E
{
    A
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (2,13): error CS0106: The modifier 'unsafe' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("unsafe"));
        }

        [WorkItem(543834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543834")]
        [Fact]
        public void UnsafeOnDelegates()
        {
            var text = @"
public unsafe delegate void TestDelegate();
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll.WithAllowUnsafe(false)).VerifyDiagnostics(
                // (2,29): error CS0227: Unsafe code may only appear if compiling with /unsafe
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "TestDelegate"));

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [WorkItem(543835, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543835")]
        [Fact]
        public void UnsafeOnConstField()
        {
            var text = @"
public class Main
{
    unsafe public const int number = 0;
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (4,29): error CS0106: The modifier 'unsafe' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "number").WithArguments("unsafe"));

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,29): error CS0106: The modifier 'unsafe' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "number").WithArguments("unsafe"));
        }

        [Fact]
        public void UnsafeOnExplicitInterfaceImplementation()
        {
            var text = @"
interface I
{
    int P { get; set; }
    void M();
    event System.Action E;
}

class C : I
{
    unsafe int I.P { get; set; }
    unsafe void I.M() { }
    unsafe event System.Action I.E { add { } remove { } }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [WorkItem(544417, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544417")]
        [Fact]
        public void UnsafeCallParamArrays()
        {
            var template = @"
{0} class C
{{
    {1} static void Main()
    {{
        {{ Foo(); }}
        {{ Foo(null); }}
        {{ Foo((int*)1); }}
        {{ Foo(new int*[2]); }}
    }}

    {1} static void Foo(params int*[] x) {{ }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (12,29): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      static void Foo(params int*[] x) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (6,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo(); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Foo()"),
                // (7,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (7,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Foo(null)"),
                // (8,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (8,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "(int*)1"),
                // (8,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Foo((int*)1)"),
                // (9,19): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo(new int*[2]); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo(new int*[2]); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new int*[2]"),
                // (9,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo(new int*[2]); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Foo(new int*[2])")
                );
        }

        [WorkItem(544938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544938")]
        [Fact]
        public void UnsafeCallOptionalParameters()
        {
            var template = @"
{0} class C
{{
    {1} static void Main()
    {{
        {{ Foo(); }}
        {{ Foo(null); }}
        {{ Foo((int*)1); }}
    }}

    {1} static void Foo(int* p = null) {{ }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (11,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      static void Foo(int* p = null) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (6,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo(); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Foo()"),
                // (7,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (7,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Foo(null)"),
                // (8,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (8,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "(int*)1"),
                // (8,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { Foo((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Foo((int*)1)")
                );
        }

        [WorkItem(544938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544938")]
        [Fact]
        public void UnsafeDelegateCallParamArrays()
        {
            var template = @"
{0} class C
{{
    {1} static void Main()
    {{
        D d = null;
        {{ d(); }}
        {{ d(null); }}
        {{ d((int*)1); }}
        {{ d(new int*[2]); }}
    }}

    {1} delegate void D(params int*[] x);
}}
";

            CompareUnsafeDiagnostics(template,
                // (13,29): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      delegate void D(params int*[] x);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (7,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d(); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "d()"),
                // (8,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (8,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "d(null)"),
                // (9,14): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "(int*)1"),
                // (9,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "d((int*)1)"),
                // (10,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d(new int*[2]); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (10,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d(new int*[2]); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new int*[2]"),
                // (10,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d(new int*[2]); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "d(new int*[2])")
                );
        }

        [WorkItem(544938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544938")]
        [Fact]
        public void UnsafeDelegateCallOptionalParameters()
        {
            var template = @"
{0} class C
{{
    {1} static void Main()
    {{
        D d = null;
        {{ d(); }}
        {{ d(null); }}
        {{ d((int*)1); }}
    }}

    {1} delegate void D(int* p = null);
}}
";

            CompareUnsafeDiagnostics(template,
                // (12,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      delegate void D(int* p = null);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (7,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d(); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "d()"),
                // (8,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (8,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "d(null)"),
                // (9,14): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "(int*)1"),
                // (9,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "d((int*)1)")
                );
        }

        [WorkItem(544938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544938")]
        [Fact]
        public void UnsafeObjectCreationParamArrays()
        {
            var template = @"
{0} class C
{{
    {1} static void Main()
    {{
        C c;
        {{ c = new C(); }}
        {{ c = new C(null); }}
        {{ c = new C((int*)1); }}
        {{ c = new C(new int*[2]); }}
    }}

    {1} C(params int*[] x) {{ }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (13,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      C(params int*[] x) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (7,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C(); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C()"),
                // (8,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (8,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C(null)"),
                // (9,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "(int*)1"),
                // (9,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C((int*)1)"),
                // (10,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C(new int*[2]); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (10,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C(new int*[2]); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new int*[2]"),
                // (10,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C(new int*[2]); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C(new int*[2])")
                );
        }

        [WorkItem(544938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544938")]
        [Fact]
        public void UnsafeObjectCreationOptionalParameters()
        {
            var template = @"
{0} class C
{{
    {1} static void Main()
    {{
        C c;
        {{ c = new C(); }}
        {{ c = new C(null); }}
        {{ c = new C((int*)1); }}
    }}

    {1} C(int* p = null) {{ }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (12,8): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      C(int* p = null) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (7,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C(); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C()"),
                // (8,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (8,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C(null); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C(null)"),
                // (9,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "(int*)1"),
                // (9,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { c = new C((int*)1); }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new C((int*)1)")
                );
        }

        [WorkItem(544938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544938")]
        [Fact]
        public void UnsafeIndexerParamArrays()
        {
            var template = @"
{0} class C
{{
    {1} static void Main()
    {{
        C c = new C();
        {{ int x = c[1]; }} // NOTE: as in dev10, this does not produce an error (would for a call).
        {{ int x = c[1, null]; }} // NOTE: as in dev10, this does not produce an error (would for a call).
        {{ int x = c[1, (int*)1]; }}
        {{ int x = c[1, new int*[2]]; }}
    }}

    {1} int this[int x, params int*[] a] {{ get {{ return 0; }} set {{ }} }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (13,29): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      int this[int x, params int*[] a] { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { int x = c[1, (int*)1]; }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,24): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { int x = c[1, (int*)1]; }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "(int*)1"),
                // (10,28): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { int x = c[1, new int*[2]]; }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (10,24): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { int x = c[1, new int*[2]]; }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "new int*[2]")
                );
        }

        [WorkItem(544938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544938")]
        [Fact]
        public void UnsafeIndexerOptionalParameters()
        {
            var template = @"
{0} class C
{{
    {1} static void Main()
    {{
        C c = new C();
        {{ int x = c[1]; }} // NOTE: as in dev10, this does not produce an error (would for a call).
        {{ int x = c[1, null]; }} // NOTE: as in dev10, this does not produce an error (would for a call).
        {{ int x = c[1, (int*)1]; }}
    }}

    {1} int this[int x, int* p = null] {{ get {{ return 0; }} set {{ }} }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (12,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      int this[int x, int* p = null] { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { int x = c[1, (int*)1]; }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,24): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { int x = c[1, (int*)1]; }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "(int*)1")
                );
        }

        [WorkItem(544938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544938")]
        [Fact]
        public void UnsafeAttributeParamArrays()
        {
            var template = @"
[A]
{0} class A : System.Attribute
{{
    {1} A(params int*[] a) {{ }}
}}
";

            CompareUnsafeDiagnostics(template,
                new[]
                {
                // CONSIDER: this differs slightly from dev10, but is clearer.
                // (2,2): error CS0181: Attribute constructor parameter 'a' has type 'int*[]', which is not a valid attribute parameter type
                // [A]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A").WithArguments("a", "int*[]"),

                // (5,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      A(params int*[] a) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*")
                },
                new[]
                {
                // CONSIDER: this differs slightly from dev10, but is clearer.
                // (2,2): error CS0181: Attribute constructor parameter 'a' has type 'int*[]', which is not a valid attribute parameter type
                // [A]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A").WithArguments("a", "int*[]")
                });
        }

        [WorkItem(544938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544938")]
        [Fact]
        public void UnsafeAttributeOptionalParameters()
        {
            var template = @"
[A]
{0} class A : System.Attribute
{{
    {1} A(int* p = null) {{ }}
}}
";

            CompareUnsafeDiagnostics(template,
                new[]
                {
                // CONSIDER: this differs slightly from dev10, but is clearer.
                // (2,2): error CS0181: Attribute constructor parameter 'p' has type 'int*', which is not a valid attribute parameter type
                // [A]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A").WithArguments("p", "int*"),

                // (5,8): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      A(int* p = null) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*")
                },
                new[]
                {
                // CONSIDER: this differs slightly from dev10, but is clearer.
                // (2,2): error CS0181: Attribute constructor parameter 'p' has type 'int*', which is not a valid attribute parameter type
                // [A]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "A").WithArguments("p", "int*")
                });
        }

        [WorkItem(544938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544938")]
        [Fact]
        public void UnsafeDelegateAssignment()
        {
            var template = @"
{0} class C
{{
    {1} static void Main()
    {{
        D d;
        {{ d = delegate {{ }}; }}
        {{ d = null; }}
        {{ d = Foo; }}
    }}

    {1} delegate void D(int* x = null);
    {1} static void Foo(int* x = null) {{ }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (9,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         { d = Foo; }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Foo"),

                // (12,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      delegate void D(int* x = null);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (13,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      static void Foo(int* x = null) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"));
        }

        private static void CompareUnsafeDiagnostics(string template, params DiagnosticDescription[] expectedWithoutUnsafe)
        {
            CompareUnsafeDiagnostics(template, expectedWithoutUnsafe, new DiagnosticDescription[0]);
        }

        private static void CompareUnsafeDiagnostics(string template, DiagnosticDescription[] expectedWithoutUnsafe, DiagnosticDescription[] expectedWithUnsafe)
        {
            // NOTE: ERR_UnsafeNeeded is not affected by the presence/absence of the /unsafe flag.
            var withoutUnsafe = string.Format(template, "", "");
            CreateCompilationWithMscorlib(withoutUnsafe).VerifyDiagnostics(expectedWithoutUnsafe);
            CreateCompilationWithMscorlib(withoutUnsafe, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedWithoutUnsafe);

            var withUnsafeOnType = string.Format(template, "unsafe", "");
            CreateCompilationWithMscorlib(withUnsafeOnType, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedWithUnsafe);

            var withUnsafeOnMembers = string.Format(template, "", "unsafe");
            CreateCompilationWithMscorlib(withUnsafeOnMembers, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedWithUnsafe);

            var withUnsafeOnTypeAndMembers = string.Format(template, "unsafe", "unsafe");
            CreateCompilationWithMscorlib(withUnsafeOnTypeAndMembers, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedWithUnsafe);
        }

        [WorkItem(544097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544097")]
        [Fact]
        public void MethodCallWithNullAsPointerArg()
        {
            var template = @"
{0} class Test
{{
    {1} static void Foo(void* p) {{ }}
    {1} static void Main()
    {{
        Foo(null);
    }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      static void Foo(void* p) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "void*"),
                // (7,13): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Foo(null);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (7,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         Foo(null);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Foo(null)")
                );
        }

        [WorkItem(544097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544097")]
        [Fact]
        public void MethodCallWithUnsafeArgument()
        {
            var template = @"
{0} class Test
{{
    {1} int M(params int*[] p) {{ return 0; }}
    {1} public static implicit operator int*(Test t) {{ return null; }}

    {1} void M()
    {{
        {{
            int x = M(null); //CS0214
        }}
        {{
            int x = M(null, null); //CS0214
        }}
        {{
            int x = M(this); //CS0214
        }}
    }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (5,38): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      public static implicit operator int*(Test t) { return null; }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (4,19): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      int M(params int*[] p) { return 0; }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (10,23): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             int x = M(null); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (10,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             int x = M(null); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "M(null)"),
                // (13,23): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             int x = M(null, null); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (13,29): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             int x = M(null, null); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (13,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             int x = M(null, null); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "M(null, null)"),
                // (16,23): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             int x = M(this); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "this"),
                // (16,21): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             int x = M(this); //CS0214
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "M(this)")
                );
        }

        [WorkItem(544097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544097")]
        [Fact]
        public void IndexerAccessWithUnsafeArgument()
        {
            var template = @"
{0} class Test
{{
    {1} int this[params int*[] p] {{ get {{ return 0; }} set {{ }} }}
    {1} public static implicit operator int*(Test t) {{ return null; }}

    {1} void M()
    {{
        {{
            int x = this[null]; //CS0214 seems appropriate, but dev10 accepts
        }}
        {{
            int x = this[null, null]; //CS0214 seems appropriate, but dev10 accepts
        }}
        {{
            int x = this[this]; //CS0214 seems appropriate, but dev10 accepts
        }}
    }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      int this[int* p] { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (5,38): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      public static implicit operator int*(Test t) { return null; }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"));
        }

        [WorkItem(544097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544097")]
        [Fact]
        public void ConstructorInitializerWithUnsafeArgument()
        {
            var template = @"
{0} class Base
{{
    {1} public Base(int* p) {{ }}
}}

{0} class Derived : Base
{{
    {1} public Derived() : base(null) {{ }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      public Base(int* p) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,30): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      public Derived() : base(null) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "null"),
                // (9,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      public Derived() : base(null) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "base")
                );
        }

        [WorkItem(544286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544286")]
        [Fact]
        public void UnsafeLambdaParameterType()
        {
            var template = @"
{0} class Program
{{
    {1} delegate void F(int* x);
    
    {1} static void Main()
    {{
        F e = x => {{ }};
    }}
}}
";

            CompareUnsafeDiagnostics(template,
                // (4,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      delegate void F(int* x);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (8,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         F e = x => { };
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x"));
        }

        #endregion Unsafe regions

        #region Non-moveable variables

        [Fact]
        public void NonMoveableVariables_Parameters()
        {
            var text = @"
class C
{
    void M(int x, ref int y, out int z, params int[] p)
    {
        M(x, ref y, out z, p);
    }
}
";
            var expected = @"
No, Call 'M(x, ref y, out z, p)' is not a non-moveable variable
No, ThisReference 'M' is not a non-moveable variable
Yes, Parameter 'x' is a non-moveable variable with underlying symbol 'x'
No, Parameter 'y' is not a non-moveable variable
No, Parameter 'z' is not a non-moveable variable
Yes, Parameter 'p' is a non-moveable variable with underlying symbol 'p'
".Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_Locals()
        {
            var text = @"
class C
{
    void M(params object[] p)
    {
        C c = null;
        int x = 0;
        M(c, x);
    }
}
";
            var expected = @"
No, TypeExpression 'C' is not a non-moveable variable
No, Conversion 'null' is not a non-moveable variable
No, Literal 'null' is not a non-moveable variable
No, TypeExpression 'int' is not a non-moveable variable
No, Literal '0' is not a non-moveable variable
No, Call 'M(c, x)' is not a non-moveable variable
No, ThisReference 'M' is not a non-moveable variable
No, Conversion 'c' is not a non-moveable variable
Yes, Local 'c' is a non-moveable variable with underlying symbol 'c'
No, Conversion 'x' is not a non-moveable variable
Yes, Local 'x' is a non-moveable variable with underlying symbol 'x'
".Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_Fields1()
        {
            var text = @"
class C
{
    public S1 s;
    public C c;

    void M(params object[] p)
    {
        C c = new C();
        S1 s = new S1();

        M(this, this.s, this.s.s, this.s.c, this.c.s, this.c.c);
        M(c, c.s, c.s.s, c.s.c, c.c.s, c.c.c);
        M(s, s.s, s.s.i);
    }
}

struct S1
{
    public S2 s;
    public C c;
}

struct S2
{
    public int i;
}
";
            var expected = @"
No, TypeExpression 'C' is not a non-moveable variable
No, ObjectCreationExpression 'new C()' is not a non-moveable variable
No, TypeExpression 'S1' is not a non-moveable variable
No, ObjectCreationExpression 'new S1()' is not a non-moveable variable
No, Call 'M(this, this.s, this.s.s, this.s.c, this.c.s, this.c.c)' is not a non-moveable variable
No, ThisReference 'M' is not a non-moveable variable
No, Conversion 'this' is not a non-moveable variable
No, ThisReference 'this' is not a non-moveable variable
No, Conversion 'this.s' is not a non-moveable variable
No, FieldAccess 'this.s' is not a non-moveable variable
No, ThisReference 'this' is not a non-moveable variable
No, Conversion 'this.s.s' is not a non-moveable variable
No, FieldAccess 'this.s.s' is not a non-moveable variable
No, FieldAccess 'this.s' is not a non-moveable variable
No, ThisReference 'this' is not a non-moveable variable
No, Conversion 'this.s.c' is not a non-moveable variable
No, FieldAccess 'this.s.c' is not a non-moveable variable
No, FieldAccess 'this.s' is not a non-moveable variable
No, ThisReference 'this' is not a non-moveable variable
No, Conversion 'this.c.s' is not a non-moveable variable
No, FieldAccess 'this.c.s' is not a non-moveable variable
No, FieldAccess 'this.c' is not a non-moveable variable
No, ThisReference 'this' is not a non-moveable variable
No, Conversion 'this.c.c' is not a non-moveable variable
No, FieldAccess 'this.c.c' is not a non-moveable variable
No, FieldAccess 'this.c' is not a non-moveable variable
No, ThisReference 'this' is not a non-moveable variable
No, Call 'M(c, c.s, c.s.s, c.s.c, c.c.s, c.c.c)' is not a non-moveable variable
No, ThisReference 'M' is not a non-moveable variable
No, Conversion 'c' is not a non-moveable variable
Yes, Local 'c' is a non-moveable variable with underlying symbol 'c'
No, Conversion 'c.s' is not a non-moveable variable
No, FieldAccess 'c.s' is not a non-moveable variable
Yes, Local 'c' is a non-moveable variable with underlying symbol 'c'
No, Conversion 'c.s.s' is not a non-moveable variable
No, FieldAccess 'c.s.s' is not a non-moveable variable
No, FieldAccess 'c.s' is not a non-moveable variable
Yes, Local 'c' is a non-moveable variable with underlying symbol 'c'
No, Conversion 'c.s.c' is not a non-moveable variable
No, FieldAccess 'c.s.c' is not a non-moveable variable
No, FieldAccess 'c.s' is not a non-moveable variable
Yes, Local 'c' is a non-moveable variable with underlying symbol 'c'
No, Conversion 'c.c.s' is not a non-moveable variable
No, FieldAccess 'c.c.s' is not a non-moveable variable
No, FieldAccess 'c.c' is not a non-moveable variable
Yes, Local 'c' is a non-moveable variable with underlying symbol 'c'
No, Conversion 'c.c.c' is not a non-moveable variable
No, FieldAccess 'c.c.c' is not a non-moveable variable
No, FieldAccess 'c.c' is not a non-moveable variable
Yes, Local 'c' is a non-moveable variable with underlying symbol 'c'
No, Call 'M(s, s.s, s.s.i)' is not a non-moveable variable
No, ThisReference 'M' is not a non-moveable variable
No, Conversion 's' is not a non-moveable variable
Yes, Local 's' is a non-moveable variable with underlying symbol 's'
No, Conversion 's.s' is not a non-moveable variable
Yes, FieldAccess 's.s' is a non-moveable variable with underlying symbol 's'
Yes, Local 's' is a non-moveable variable with underlying symbol 's'
No, Conversion 's.s.i' is not a non-moveable variable
Yes, FieldAccess 's.s.i' is a non-moveable variable with underlying symbol 's'
Yes, FieldAccess 's.s' is a non-moveable variable with underlying symbol 's'
Yes, Local 's' is a non-moveable variable with underlying symbol 's'
".Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_Fields2()
        {
            var text = @"
class Base
{
    public int i;
}

class Derived : Base
{
    void M()
    {
        base.i = 0;
    }
}
";
            var expected = @"
No, AssignmentOperator 'base.i = 0' is not a non-moveable variable
No, FieldAccess 'base.i' is not a non-moveable variable
No, BaseReference 'base' is not a non-moveable variable
No, Literal '0' is not a non-moveable variable
".Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_Fields3()
        {
            var text = @"
struct S
{
    static int i;

    void M()
    {
        S.i = 0;
    }
}
";
            var expected = @"
No, AssignmentOperator 'S.i = 0' is not a non-moveable variable
No, FieldAccess 'S.i' is not a non-moveable variable
No, TypeExpression 'S' is not a non-moveable variable
No, Literal '0' is not a non-moveable variable
".Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_Fields4()
        {
            var text = @"
struct S
{
    int i;

    void M(params object[] p)
    {
        // rvalues are never non-moveable.
        M(new S().i, default(S).i, MakeS().i, (new S[1])[0].i);
    }

    S MakeS() 
    { 
        return default(S); 
    }
}
";
            var expected = @"
No, Call 'M(new S().i, default(S).i, MakeS().i, (new S[1])[0].i)' is not a non-moveable variable
No, ThisReference 'M' is not a non-moveable variable
No, Conversion 'new S().i' is not a non-moveable variable
No, FieldAccess 'new S().i' is not a non-moveable variable
No, ObjectCreationExpression 'new S()' is not a non-moveable variable
No, Conversion 'default(S).i' is not a non-moveable variable
No, FieldAccess 'default(S).i' is not a non-moveable variable
No, DefaultOperator 'default(S)' is not a non-moveable variable
No, Conversion 'MakeS().i' is not a non-moveable variable
No, FieldAccess 'MakeS().i' is not a non-moveable variable
No, Call 'MakeS()' is not a non-moveable variable
No, ThisReference 'MakeS' is not a non-moveable variable
No, Conversion '(new S[1])[0].i' is not a non-moveable variable
No, FieldAccess '(new S[1])[0].i' is not a non-moveable variable
No, ArrayAccess '(new S[1])[0]' is not a non-moveable variable
No, ArrayCreation 'new S[1]' is not a non-moveable variable
No, Literal '1' is not a non-moveable variable
No, Literal '0' is not a non-moveable variable
".Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_Events()
        {
            var text = @"
struct S
{
    public event System.Action E;
    public event System.Action F { add { } remove { } }

    void M(params object[] p)
    {
        C c = new C();
        S s = new S();

        M(c.E, c.F); //note: note legal to pass F
        M(s.E, s.F); //note: note legal to pass F
    }
}

class C
{
    public event System.Action E;
    public event System.Action F { add { } remove { } }
}
";
            var expected = @"
No, TypeExpression 'C' is not a non-moveable variable
No, ObjectCreationExpression 'new C()' is not a non-moveable variable
No, TypeExpression 'S' is not a non-moveable variable
No, ObjectCreationExpression 'new S()' is not a non-moveable variable
No, Call 'M(c.E, c.F)' is not a non-moveable variable
No, ThisReference 'M' is not a non-moveable variable
No, Conversion 'c.E' is not a non-moveable variable
No, BadExpression 'c.E' is not a non-moveable variable
No, EventAccess 'c.E' is not a non-moveable variable
Yes, Local 'c' is a non-moveable variable with underlying symbol 'c'
No, Conversion 'c.F' is not a non-moveable variable
No, BadExpression 'c.F' is not a non-moveable variable
No, EventAccess 'c.F' is not a non-moveable variable
Yes, Local 'c' is a non-moveable variable with underlying symbol 'c'
No, Call 'M(s.E, s.F)' is not a non-moveable variable
No, ThisReference 'M' is not a non-moveable variable
No, Conversion 's.E' is not a non-moveable variable
Yes, EventAccess 's.E' is a non-moveable variable with underlying symbol 's'
Yes, Local 's' is a non-moveable variable with underlying symbol 's'
No, Conversion 's.F' is not a non-moveable variable
No, BadExpression 's.F' is not a non-moveable variable
No, EventAccess 's.F' is not a non-moveable variable
Yes, Local 's' is a non-moveable variable with underlying symbol 's'
".Trim();

            CheckNonMoveableVariables(text, expected, expectError: true);
        }

        [Fact]
        public void NonMoveableVariables_Lambda1()
        {
            var text = @"
class C
{
    void M(params object[] p)
    {
        int i = 0; // NOTE: considered non-moveable even though it will be hoisted - lambdas handled separately.
        i++;
        System.Action a = () =>
        {
            int j = i;
            j++;
        };
    }
}
";
            var expected = string.Format(@"
No, TypeExpression 'int' is not a non-moveable variable
No, Literal '0' is not a non-moveable variable
No, IncrementOperator 'i++' is not a non-moveable variable
Yes, Local 'i' is a non-moveable variable with underlying symbol 'i'
No, TypeExpression 'System.Action' is not a non-moveable variable
No, Conversion '() =>{0}        {{{0}            int j = i;{0}            j++;{0}        }}' is not a non-moveable variable
No, Lambda '() =>{0}        {{{0}            int j = i;{0}            j++;{0}        }}' is not a non-moveable variable
No, TypeExpression 'int' is not a non-moveable variable
Yes, Local 'i' is a non-moveable variable with underlying symbol 'i'
No, IncrementOperator 'j++' is not a non-moveable variable
Yes, Local 'j' is a non-moveable variable with underlying symbol 'j'
", GetEscapedNewLine()).Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_Lambda2()
        {
            var text = @"
class C
{
    void M()
    {
        int i = 0; // NOTE: considered non-moveable even though it will be hoisted - lambdas handled separately.
        i++;
        System.Func<int, System.Func<int, int>> a = p => q => p + q + i;
    }
}
";
            var expected = @"
No, TypeExpression 'int' is not a non-moveable variable
No, Literal '0' is not a non-moveable variable
No, IncrementOperator 'i++' is not a non-moveable variable
Yes, Local 'i' is a non-moveable variable with underlying symbol 'i'
No, TypeExpression 'System.Func<int, System.Func<int, int>>' is not a non-moveable variable
No, Conversion 'p => q => p + q + i' is not a non-moveable variable
No, Lambda 'p => q => p + q + i' is not a non-moveable variable
No, Conversion 'q => p + q + i' is not a non-moveable variable
No, Lambda 'q => p + q + i' is not a non-moveable variable
No, BinaryOperator 'p + q + i' is not a non-moveable variable
No, BinaryOperator 'p + q' is not a non-moveable variable
Yes, Parameter 'p' is a non-moveable variable with underlying symbol 'p'
Yes, Parameter 'q' is a non-moveable variable with underlying symbol 'q'
Yes, Local 'i' is a non-moveable variable with underlying symbol 'i'
".Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_Dereference()
        {
            var text = @"
struct S
{
    int i;

    unsafe void Test(S* p)
    {
        S s;
        s = *p;
        s = p[0];

        int j;
        j = (*p).i;
        j = p[0].i;
        j = p->i;
    }
}
";
            var expected = @"
No, TypeExpression 'S' is not a non-moveable variable
No, AssignmentOperator 's = *p' is not a non-moveable variable
Yes, Local 's' is a non-moveable variable with underlying symbol 's'
Yes, PointerIndirectionOperator '*p' is a non-moveable variable
Yes, Parameter 'p' is a non-moveable variable with underlying symbol 'p'
No, AssignmentOperator 's = p[0]' is not a non-moveable variable
Yes, Local 's' is a non-moveable variable with underlying symbol 's'
Yes, PointerElementAccess 'p[0]' is a non-moveable variable
Yes, Parameter 'p' is a non-moveable variable with underlying symbol 'p'
No, Literal '0' is not a non-moveable variable
No, TypeExpression 'int' is not a non-moveable variable
No, AssignmentOperator 'j = (*p).i' is not a non-moveable variable
Yes, Local 'j' is a non-moveable variable with underlying symbol 'j'
Yes, FieldAccess '(*p).i' is a non-moveable variable
Yes, PointerIndirectionOperator '*p' is a non-moveable variable
Yes, Parameter 'p' is a non-moveable variable with underlying symbol 'p'
No, AssignmentOperator 'j = p[0].i' is not a non-moveable variable
Yes, Local 'j' is a non-moveable variable with underlying symbol 'j'
Yes, FieldAccess 'p[0].i' is a non-moveable variable
Yes, PointerElementAccess 'p[0]' is a non-moveable variable
Yes, Parameter 'p' is a non-moveable variable with underlying symbol 'p'
No, Literal '0' is not a non-moveable variable
No, AssignmentOperator 'j = p->i' is not a non-moveable variable
Yes, Local 'j' is a non-moveable variable with underlying symbol 'j'
Yes, FieldAccess 'p->i' is a non-moveable variable
Yes, PointerIndirectionOperator 'p' is a non-moveable variable
Yes, Parameter 'p' is a non-moveable variable with underlying symbol 'p'
".Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_StackAlloc()
        {
            var text = @"
struct S
{
    unsafe void Test()
    {
        int* p = stackalloc int[1];
    }
}
";
            var expected = @"
No, TypeExpression 'int*' is not a non-moveable variable
Yes, StackAllocArrayCreation 'stackalloc int[1]' is a non-moveable variable
No, Literal '1' is not a non-moveable variable
".Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_TypeParameters1()
        {
            var text = @"
class C
{
    public C c;

    void M<T>(T t, C c) where T : C
    {
        M(t, t.c);
    }
}
";
            var expected = @"
No, Call 'M(t, t.c)' is not a non-moveable variable
No, ThisReference 'M' is not a non-moveable variable
Yes, Parameter 't' is a non-moveable variable with underlying symbol 't'
No, FieldAccess 't.c' is not a non-moveable variable
Yes, Parameter 't' is a non-moveable variable with underlying symbol 't'
".Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_TypeParameters2()
        {
            var text = @"
class D : C<S>
{
    public override void M<U>(U u, int j)
    {
        M(u, u.i); // effective base type (System.ValueType) does not have a member 'i'
    }
}

abstract class C<T>
{
    public abstract void M<U>(U u, int i) where U : T;
}

struct S
{
    public int i;
}
";
            var expected = @"
No, Call 'M(u, u.i)' is not a non-moveable variable
No, ThisReference 'M' is not a non-moveable variable
Yes, Parameter 'u' is a non-moveable variable with underlying symbol 'u'
No, BadExpression 'u.i' is not a non-moveable variable
Yes, Parameter 'u' is a non-moveable variable with underlying symbol 'u'
".Trim();

            CheckNonMoveableVariables(text, expected, expectError: true);
        }

        [Fact]
        public void NonMoveableVariables_RangeVariables1()
        {
            var text = @"
using System.Linq;

class C
{
    void M(int[] array)
    {
        var result = 
            from i in array 
            from j in array 
            select i + j;
    }
}
";
            var expected = string.Format(@"
No, TypeExpression 'var' is not a non-moveable variable
No, QueryClause 'from i in array {0}            from j in array {0}            select i + j' is not a non-moveable variable
No, QueryClause 'select i + j' is not a non-moveable variable
No, QueryClause 'from j in array' is not a non-moveable variable
No, Call 'from j in array' is not a non-moveable variable
No, Conversion 'from i in array' is not a non-moveable variable
No, QueryClause 'from i in array' is not a non-moveable variable
Yes, Parameter 'array' is a non-moveable variable with underlying symbol 'array'
No, QueryClause 'from j in array' is not a non-moveable variable
No, Conversion 'array' is not a non-moveable variable
No, Lambda 'array' is not a non-moveable variable
No, Conversion 'array' is not a non-moveable variable
Yes, Parameter 'array' is a non-moveable variable with underlying symbol 'array'
No, Conversion 'i + j' is not a non-moveable variable
No, Lambda 'i + j' is not a non-moveable variable
No, BinaryOperator 'i + j' is not a non-moveable variable
Yes, RangeVariable 'i' is a non-moveable variable with underlying symbol 'i'
Yes, Parameter 'i' is a non-moveable variable with underlying symbol 'i'
Yes, RangeVariable 'j' is a non-moveable variable with underlying symbol 'j'
Yes, Parameter 'j' is a non-moveable variable with underlying symbol 'j'
", GetEscapedNewLine()).Trim();

            CheckNonMoveableVariables(text, expected);
        }

        [Fact]
        public void NonMoveableVariables_RangeVariables2()
        {
            var text = @"
using System;

class Test
{
    void M(C c)
    {
        var result = from x in c
                     where x > 0 //int
                     where x.Length < 2 //string
                     select char.IsLetter(x); //char
    }
}

class C
{
    public D Where(Func<int, bool> predicate)
    {
        return new D();
    }
}

class D
{
    public char[] Where(Func<string, bool> predicate)
    {
        return new char[10];
    }
}

static class Extensions
{
    public static object Select(this char[] array, Func<char, bool> func)
    {
        return null;
    }
}
";

            var expected = string.Format(@"
No, TypeExpression 'var' is not a non-moveable variable
No, QueryClause 'from x in c{0}                     where x > 0 //int{0}                     where x.Length < 2 //string{0}                     select char.IsLetter(x)' is not a non-moveable variable
No, QueryClause 'select char.IsLetter(x)' is not a non-moveable variable
No, Call 'select char.IsLetter(x)' is not a non-moveable variable
No, QueryClause 'where x.Length < 2' is not a non-moveable variable
No, Call 'where x.Length < 2' is not a non-moveable variable
No, QueryClause 'where x > 0' is not a non-moveable variable
No, Call 'where x > 0' is not a non-moveable variable
No, QueryClause 'from x in c' is not a non-moveable variable
Yes, Parameter 'c' is a non-moveable variable with underlying symbol 'c'
No, Conversion 'x > 0' is not a non-moveable variable
No, Lambda 'x > 0' is not a non-moveable variable
No, BinaryOperator 'x > 0' is not a non-moveable variable
Yes, RangeVariable 'x' is a non-moveable variable with underlying symbol 'x'
Yes, Parameter 'x' is a non-moveable variable with underlying symbol 'x'
No, Literal '0' is not a non-moveable variable
No, Conversion 'x.Length < 2' is not a non-moveable variable
No, Lambda 'x.Length < 2' is not a non-moveable variable
No, BinaryOperator 'x.Length < 2' is not a non-moveable variable
No, PropertyAccess 'x.Length' is not a non-moveable variable
Yes, RangeVariable 'x' is a non-moveable variable with underlying symbol 'x'
Yes, Parameter 'x' is a non-moveable variable with underlying symbol 'x'
No, Literal '2' is not a non-moveable variable
No, Conversion 'char.IsLetter(x)' is not a non-moveable variable
No, Lambda 'char.IsLetter(x)' is not a non-moveable variable
No, Call 'char.IsLetter(x)' is not a non-moveable variable
No, TypeExpression 'char' is not a non-moveable variable
Yes, RangeVariable 'x' is a non-moveable variable with underlying symbol 'x'
Yes, Parameter 'x' is a non-moveable variable with underlying symbol 'x'
", GetEscapedNewLine()).Trim();

            CheckNonMoveableVariables(text, expected);
        }

        private static void CheckNonMoveableVariables(string text, string expected, bool expectError = false)
        {
            var compilation = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.UnsafeReleaseDll);
            var compilationDiagnostics = compilation.GetDiagnostics();
            if (expectError != compilationDiagnostics.Any(diag => diag.Severity == DiagnosticSeverity.Error))
            {
                compilationDiagnostics.Verify();
                Assert.True(false);
            }

            var tree = compilation.SyntaxTrees.Single();
            var methodDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var methodBody = methodDecl.Body;
            var model = compilation.GetSemanticModel(tree);
            var binder = ((CSharpSemanticModel)model).GetEnclosingBinder(methodBody.SpanStart);

            Assert.NotNull(binder);
            Assert.Equal(SymbolKind.Method, binder.ContainingMemberOrLambda.Kind);

            var unusedDiagnostics = DiagnosticBag.GetInstance();
            var block = binder.BindBlock(methodBody, unusedDiagnostics);
            unusedDiagnostics.Free();

            var builder = ArrayBuilder<string>.GetInstance();

            NonMoveableVariableVisitor.Process(block, binder, builder);


            var actual = string.Join(Environment.NewLine, builder);

            Assert.Equal(expected, actual);

            builder.Free();
        }

        private class NonMoveableVariableVisitor : BoundTreeWalkerWithStackGuard
        {
            private readonly Binder _binder;
            private readonly ArrayBuilder<string> _builder;

            private NonMoveableVariableVisitor(Binder binder, ArrayBuilder<string> builder)
            {
                _binder = binder;
                _builder = builder;
            }

            public static void Process(BoundBlock block, Binder binder, ArrayBuilder<string> builder)
            {
                var visitor = new NonMoveableVariableVisitor(binder, builder);
                visitor.Visit(block);
            }

            public override BoundNode Visit(BoundNode node)
            {
                var expr = node as BoundExpression;
                if (expr != null)
                {
                    var text = node.Syntax.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(text, quote: false);
                        Symbol accessedLocalOrParameterOpt;
                        bool isNonMoveableVariable = _binder.IsNonMoveableVariable(expr, out accessedLocalOrParameterOpt);

                        if (isNonMoveableVariable)
                        {
                            _builder.Add(string.Format("Yes, {0} '{1}' is a non-moveable variable{2}",
                                expr.Kind,
                                text,
                                accessedLocalOrParameterOpt == null ? "" : string.Format(" with underlying symbol '{0}'", accessedLocalOrParameterOpt.Name)));
                        }
                        else
                        {
                            _builder.Add(string.Format("No, {0} '{1}' is not a non-moveable variable", expr.Kind, text));
                        }
                    }
                }

                return base.Visit(node);
            }

            protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
            {
                return false;
            }
        }

        #endregion Non-moveable variables

        #region IsManagedType

        [Fact]
        public void IsManagedType_Array()
        {
            var text = @"
class C
{
    int[] f1;
    int[,] f2;
    int[][] f3;
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            Assert.True(type.GetMembers().OfType<FieldSymbol>().All(field => field.Type.IsManagedType));
        }

        [Fact]
        public void IsManagedType_Pointer()
        {
            var text = @"
unsafe class C
{
    int* f1;
    int** f2;
    void* f3;
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            Assert.True(type.GetMembers().OfType<FieldSymbol>().All(field => !field.Type.IsManagedType));
        }

        [Fact]
        public void IsManagedType_Dynamic()
        {
            var text = @"
class C
{
    dynamic f1;
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            Assert.True(type.GetMembers().OfType<FieldSymbol>().All(field => field.Type.IsManagedType));
        }

        [Fact]
        public void IsManagedType_Error()
        {
            var text = @"
class C<T>
{
    C f1;
    C<int, int> f2;
    Garbage f3;
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            Assert.True(type.GetMembers().OfType<FieldSymbol>().All(field => field.Type.IsManagedType));
        }

        [Fact]
        public void IsManagedType_TypeParameter()
        {
            var text = @"
class C<T, U> where U : struct
{
    T f1;
    U f2;
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            Assert.True(type.GetMembers().OfType<FieldSymbol>().All(field => field.Type.IsManagedType));
        }

        [Fact]
        public void IsManagedType_AnonymousType()
        {
            var text = @"
class C
{
    void M()
    {
        var local1 = new { };
        var local2 = new { F = 1 };
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            Assert.True(tree.GetCompilationUnitRoot().DescendantNodes().OfType<AnonymousObjectCreationExpressionSyntax>().
                Select(syntax => model.GetTypeInfo(syntax).Type).All(type => ((TypeSymbol)type).IsManagedType));
        }

        [Fact]
        public void IsManagedType_Class()
        {
            var text = @"
class Outer
{
    Outer f1;
    Outer.Inner f2;
    string f3;    

    class Inner { }
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Outer");

            Assert.True(type.GetMembers().OfType<FieldSymbol>().All(field => field.Type.IsManagedType));
        }

        [Fact]
        public void IsManagedType_GenericClass()
        {
            var text = @"
class Outer<T>
{
    Outer<T> f1;
    Outer<T>.Inner f2;
    Outer<int> f1;
    Outer<string>.Inner f2;

    class Inner { }
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Outer");

            Assert.True(type.GetMembers().OfType<FieldSymbol>().All(field => field.Type.IsManagedType));
        }

        [Fact]
        public void IsManagedType_ManagedSpecialTypes()
        {
            var text = @"
class C
{
    object f1;
    string f2;
    System.Collections.IEnumerable f3;
    int? f4;
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            Assert.True(type.GetMembers().OfType<FieldSymbol>().All(field => field.Type.IsManagedType));
        }

        [Fact]
        public void IsManagedType_NonManagedSpecialTypes()
        {
            var text = @"
class C
{
    bool f1;
    char f2;
    sbyte f3;
    byte f4;
    short f5;
    ushort f6;
    int f7;
    uint f8;
    long f9;
    ulong f10;
    decimal f11;
    float f12;
    double f13;
    System.IntPtr f14;
    System.UIntPtr f14;
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            Assert.True(type.GetMembers().OfType<FieldSymbol>().All(field => !field.Type.IsManagedType));
        }

        [Fact]
        public void IsManagedType_Void()
        {
            var text = @"
class C
{
    void M() { }
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMember<MethodSymbol>("M");

            Assert.False(method.ReturnType.IsManagedType);
        }

        [Fact]
        public void IsManagedType_Enum()
        {
            var text = @"
enum E { A }

class C
{
    enum E { A }
}

class D<T>
{
    enum E { A }
}

struct S
{
    enum E { A }
}

struct R<T>
{
    enum E { A }
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var globalNamespace = compilation.GlobalNamespace;
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("E").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<NamedTypeSymbol>("E").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("D").GetMember<NamedTypeSymbol>("E").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S").GetMember<NamedTypeSymbol>("E").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("R").GetMember<NamedTypeSymbol>("E").IsManagedType);
        }

        [Fact]
        public void IsManagedType_EmptyStruct()
        {
            var text = @"
struct S { }

struct P<T> { }

class C
{
    struct S { }
}

class D<T>
{
    struct S { }
}

struct Q
{
    struct S { }
}

struct R<T>
{
    struct S { }
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var globalNamespace = compilation.GlobalNamespace;
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("P").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<NamedTypeSymbol>("S").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("D").GetMember<NamedTypeSymbol>("S").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("Q").GetMember<NamedTypeSymbol>("S").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("R").GetMember<NamedTypeSymbol>("S").IsManagedType);
        }

        [Fact]
        public void IsManagedType_SubstitutedStruct()
        {
            var text = @"
class C<U>
{
    S<U> f1;
    S<int> f2;
    S<U>.R f3;
    S<int>.R f4;
}

struct S<T>
{
    struct R { }
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            Assert.True(type.GetMembers().OfType<FieldSymbol>().All(field => field.Type.IsManagedType));
        }

        [Fact]
        public void IsManagedType_NonEmptyStruct()
        {
            var text = @"
struct S1
{
    int f;
}

struct S2
{
    object f;
}

struct S3
{
    S1 s;
}

struct S4
{
    S2 s;
}

struct S5
{
    S1 s1;
    S2 s2;
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var globalNamespace = compilation.GlobalNamespace;
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S1").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S2").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S3").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S4").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S5").IsManagedType);
        }

        [Fact]
        public void IsManagedType_StaticFieldStruct()
        {
            var text = @"
struct S1
{
    static object o;
    int f;
}

struct S2
{
    static object o;
    object f;
}

struct S3
{
    static object o;
    S1 s;
}

struct S4
{
    static object o;
    S2 s;
}

struct S5
{
    static object o;
    S1 s1;
    S2 s2;
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var globalNamespace = compilation.GlobalNamespace;
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S1").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S2").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S3").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S4").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S5").IsManagedType);
        }

        [Fact]
        public void IsManagedType_AutoPropertyStruct()
        {
            var text = @"
struct S1
{
    int f { get; set; }
}

struct S2
{
    object f { get; set; }
}

struct S3
{
    S1 s { get; set; }
}

struct S4
{
    S2 s { get; set; }
}

struct S5
{
    S1 s1 { get; set; }
    S2 s2 { get; set; }
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var globalNamespace = compilation.GlobalNamespace;
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S1").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S2").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S3").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S4").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S5").IsManagedType);
        }

        [Fact]
        public void IsManagedType_PropertyStruct()
        {
            var text = @"
struct S1
{
    object o { get { return null; } set { } }
    int f { get; set; }
}

struct S2
{
    object o { get { return null; } set { } }
    object f { get; set; }
}

struct S3
{
    object o { get { return null; } set { } }
    S1 s { get; set; }
}

struct S4
{
    object o { get { return null; } set { } }
    S2 s { get; set; }
}

struct S5
{
    object o { get { return null; } set { } }
    S1 s1 { get; set; }
    S2 s2 { get; set; }
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var globalNamespace = compilation.GlobalNamespace;
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S1").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S2").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S3").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S4").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S5").IsManagedType);
        }

        [Fact]
        public void IsManagedType_EventStruct()
        {
            var text = @"
struct S1
{
    event System.Action E; // has field
}

struct S2
{
    event System.Action E { add { } remove { } } // no field
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var globalNamespace = compilation.GlobalNamespace;
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("S1").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S2").IsManagedType);
        }

        [Fact]
        public void IsManagedType_ExpandingStruct()
        {
            var text = @"
struct X<T> { public T t; }
struct W<T> { X<W<W<T>>> x; }
";
            var compilation = CreateCompilationWithMscorlib(text);
            var globalNamespace = compilation.GlobalNamespace;
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("X").IsManagedType); // because of X.t
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("W").IsManagedType);
        }

        [Fact]
        public void IsManagedType_CyclicStruct()
        {
            var text = @"
struct S
{
    S s;
}

struct R
{
    object o;
    S s;
}
";
            var compilation = CreateCompilationWithMscorlib(text);
            var globalNamespace = compilation.GlobalNamespace;
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("R").IsManagedType);
        }

        [Fact]
        public void IsManagedType_CyclicStructChain()
        {
            var text = @"
struct Q { R r; }
struct R { A a; object o }
struct S { A a; }

//cycle
struct A { B b; }
struct B { C c; }
struct C { D d; }
struct D { A a; }
";
            var compilation = CreateCompilationWithMscorlib(text);
            var globalNamespace = compilation.GlobalNamespace;
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("Q").IsManagedType);
            Assert.True(globalNamespace.GetMember<NamedTypeSymbol>("R").IsManagedType);
            Assert.False(globalNamespace.GetMember<NamedTypeSymbol>("S").IsManagedType);
        }

        [Fact]
        public void IsManagedType_SpecialClrTypes()
        {
            var text = @"
class C { }
";
            var compilation = CreateCompilationWithMscorlib(text);
            Assert.False(compilation.GetSpecialType(SpecialType.System_ArgIterator).IsManagedType);
            Assert.False(compilation.GetSpecialType(SpecialType.System_RuntimeArgumentHandle).IsManagedType);
            Assert.False(compilation.GetSpecialType(SpecialType.System_TypedReference).IsManagedType);
        }

        [Fact]
        public void ERR_ManagedAddr_ShallowRecursive()
        {
            var text = @"
public unsafe struct S1
{
    public S1* s; //CS0208
    public object o;
}

public unsafe struct S2
{
    public S2* s; //fine
    public int i;
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,12): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S1')
                //     public S1* s; //CS0523
                Diagnostic(ErrorCode.ERR_ManagedAddr, "S1*").WithArguments("S1"));
        }

        [Fact]
        public void ERR_ManagedAddr_DeepRecursive()
        {
            var text = @"
public unsafe struct A
{
    public B** bb; //CS0208
    public object o;

    public struct B
    {
        public C*[] cc; //CS0208

        public struct C
        {
            public A*[,][] aa; //CS0208
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (13,20): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('A')
                //             public A*[,][] aa; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "A*").WithArguments("A"),
                // (9,16): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('A.B.C')
                //         public C*[] cc; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "C*").WithArguments("A.B.C"),
                // (4,12): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('A.B')
                //     public B** bb; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "B*").WithArguments("A.B"));
        }

        [Fact]
        public void ERR_ManagedAddr_Alias()
        {
            var text = @"
using Alias = S;

public unsafe struct S
{
    public Alias* s; //CS0208
    public object o;
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,12): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //     public Alias* s; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "Alias*").WithArguments("S"));
        }

        [Fact]
        public void ERR_ManagedAddr_Members()
        {
            var text = @"
public unsafe struct S
{
    S* M() { return M(); }
    void M(S* p) { }

    S* P { get; set; }
    
    S* this[int x] { get { return M(); } set { } }
    int this[S* p] { get { return 0; } set { } }

    public S* s; //CS0208
    public object o;
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,5): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //     S* M() { return M(); }
                Diagnostic(ErrorCode.ERR_ManagedAddr, "S*").WithArguments("S"),
                // (5,12): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //     void M(S* p) { }
                Diagnostic(ErrorCode.ERR_ManagedAddr, "S*").WithArguments("S"),
                // (7,5): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //     S* P { get; set; }
                Diagnostic(ErrorCode.ERR_ManagedAddr, "S*").WithArguments("S"),
                // (9,5): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //     S* this[int x] { get { return M(); } set { } }
                Diagnostic(ErrorCode.ERR_ManagedAddr, "S*").WithArguments("S"),
                // (10,14): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //     int this[S* p] { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_ManagedAddr, "S*").WithArguments("S"),
                // (12,12): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //     public S* s; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "S*").WithArguments("S"));
        }

        #endregion IsManagedType

        #region AddressOf operand kinds

        [Fact]
        public void AddressOfExpressionKinds_Simple()
        {
            var text = @"
unsafe class C
{
    void M(int param)
    {
        int local;
        int* p;
        p = &param;
        p = &local;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void AddressOfExpressionKinds_Dereference()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        int x;
        int* p = &x;
        p = &(*p);
        p = &p[0];
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void AddressOfExpressionKinds_Struct()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        S1 s;
        S1* p1 = &s;
        S2* p2 = &s.s;
        S3* p3 = &s.s.s;
        int* p4 = &s.s.s.x;

        p2 = &(p1->s);
        p3 = &(p2->s);
        p4 = &(p3->x);

        p2 = &((*p1).s);
        p3 = &((*p2).s);
        p4 = &((*p3).x);
    }
}

struct S1
{
    public S2 s;
}

struct S2
{
    public S3 s;
}

struct S3
{
    public int x;
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [WorkItem(529267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529267")]
        [Fact]
        public void AddressOfExpressionKinds_RangeVariable()
        {
            var text = @"
using System.Linq;

unsafe class C
{
    int M(int param)
    {
        var z = from x in new int[2] select Foo(&x);

        return 0;
    }

    int Foo(int* p) { return 0; }
}
";
            // NOTE: this is a breaking change - dev10 allows this.
            CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,50): error CS0211: Cannot take the address of the given expression
                //         var z = from x in new int[2] select Foo(&x);
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "x").WithArguments("x"));
        }

        [Fact]
        public void AddressOfExpressionKinds_ReadOnlyLocal()
        {
            var text = @"
class Test { static void Main() { } }

unsafe class C
{
    int[] array;

    void M()
    {
        int* p;

        const int x = 1;
        p = &x; //CS0211

        foreach (int y in new int[1])
        {
            p = &y; //CS0459
        }

        using (S s = new S())
        {
            S* sp = &s; //CS0459
        }

        fixed (int* a = &array[0])
        {
            int** pp = &a; //CS0459
        }
    }
}

struct S : System.IDisposable
{
    public void Dispose() { }
}
";
            CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (13,14): error CS0211: Cannot take the address of the given expression
                //         p = &x; //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "x"),
                // (17,18): error CS0459: Cannot take the address of a read-only local variable
                //             p = &y; //CS0459
                Diagnostic(ErrorCode.ERR_AddrOnReadOnlyLocal, "y"),
                // (22,22): error CS0459: Cannot take the address of a read-only local variable
                //             S* sp = &s; //CS0459
                Diagnostic(ErrorCode.ERR_AddrOnReadOnlyLocal, "s"),
                // (27,25): error CS0459: Cannot take the address of a read-only local variable
                //             int** pp = &a; //CS0459
                Diagnostic(ErrorCode.ERR_AddrOnReadOnlyLocal, "a"),
                // (6,11): warning CS0649: Field 'C.array' is never assigned to, and will always have its default value null
                //     int[] array;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "array").WithArguments("C.array", "null"));
        }

        [Fact]
        public void AddressOfExpressionKinds_Failure()
        {
            var text = @"
class Base
{
    public int f = 2;
}

unsafe class C : Base
{
    event System.Action E;
    event System.Action F { add { } remove { } }
    int instanceField;
    int staticField;
    int this[int x] { get { return 0; } set { } }
    int P { get; set; }

    int M(int param)
    {
        int local;
        int[] array = new int[1];
        System.Func<int> func = () => 1;

        int* p;
        p = &1; //CS0211 (can't addr)
        p = &array[0]; //CS0212 (need fixed)
        p = &(local = 1); //CS0211
        p = &foo; //CS0103 (no foo)
        p = &base.f; //CS0212
        p = &(local + local); //CS0211
        p = &M(local); //CS0211
        p = &func(); //CS0211
        p = &(local += local); //CS0211
        p = &(local == 0 ? local : param); //CS0211
        p = &((int)param); //CS0211
        p = &default(int); //CS0211
        p = &delegate { return 1; }; //CS0211
        p = &instanceField; //CS0212
        p = &staticField; //CS0212
        p = &(local++); //CS0211
        p = &this[0]; //CS0211
        p = &(() => 1); //CS0211
        p = &M; //CS0211
        p = &(new System.Int32()); //CS0211
        p = &P; //CS0211
        p = &sizeof(int); //CS0211
        p = &this.instanceField; //CS0212
        p = &(+local); //CS0211

        int** pp;
        pp = &(&local); //CS0211

        var q = &(new { }); //CS0208, CS0211 (managed)
        var r = &(new int[1]); //CS0208, CS0211 (managed)
        var s = &(array as object); //CS0208, CS0211 (managed)
        var t = &E; //CS0208
        var u = &F; //CS0079 (can't use event like that)
        var v = &(E += null); //CS0211
        var w = &(F += null); //CS0211
        var x = &(array is object); //CS0211
        var y = &(array ?? array); //CS0208, CS0211 (managed)
        var aa = &this; //CS0208, CS0459 (readonly)
        var bb = &typeof(int); //CS0208, CS0211 (managed)
        var cc = &Color.Red; //CS0211

        return 0;
    }

    int Foo(int* p) { return 0; }

    static void Main() { }
}

unsafe struct S
{
    S(int x)
    {
        var aa = &this; //CS0212 (need fixed)
    }
}

enum Color
{
    Red,
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (23,14): error CS0211: Cannot take the address of the given expression
                //         p = &1; //CS0211 (can't addr)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "1"),
                // (24,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &array[0]; //CS0212 (need fixed)
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&array[0]"),
                // (25,15): error CS0211: Cannot take the address of the given expression
                //         p = &(local = 1); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "local = 1"),
                // (26,14): error CS0103: The name 'foo' does not exist in the current context
                //         p = &foo; //CS0103 (no foo)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "foo").WithArguments("foo"),
                // (27,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &base.f; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&base.f"),
                // (28,15): error CS0211: Cannot take the address of the given expression
                //         p = &(local + local); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "local + local"),
                // (29,14): error CS0211: Cannot take the address of the given expression
                //         p = &M(local); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "M(local)"),
                // (30,14): error CS0211: Cannot take the address of the given expression
                //         p = &func(); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "func()"),
                // (31,15): error CS0211: Cannot take the address of the given expression
                //         p = &(local += local); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "local += local"),
                // (32,15): error CS0211: Cannot take the address of the given expression
                //         p = &(local == 0 ? local : param); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "local == 0 ? local : param"),
                // (33,15): error CS0211: Cannot take the address of the given expression
                //         p = &((int)param); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "(int)param"),
                // (34,14): error CS0211: Cannot take the address of the given expression
                //         p = &default(int); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "default(int)"),
                // (35,14): error CS0211: Cannot take the address of the given expression
                //         p = &delegate { return 1; }; //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "delegate { return 1; }"),
                // (36,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &instanceField; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&instanceField"),
                // (37,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &staticField; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&staticField"),
                // (38,15): error CS0211: Cannot take the address of the given expression
                //         p = &(local++); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "local++"),
                // (39,14): error CS0211: Cannot take the address of the given expression
                //         p = &this[0]; //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "this[0]").WithArguments("C.this[int]"),
                // (40,15): error CS0211: Cannot take the address of the given expression
                //         p = &(() => 1); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "() => 1"),
                // (41,14): error CS0211: Cannot take the address of the given expression
                //         p = &M; //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "M").WithArguments("M", "method group"),
                // (42,15): error CS0211: Cannot take the address of the given expression
                //         p = &(new System.Int32()); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "new System.Int32()"),
                // (43,14): error CS0211: Cannot take the address of the given expression
                //         p = &P; //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "P").WithArguments("C.P"),
                // (44,14): error CS0211: Cannot take the address of the given expression
                //         p = &sizeof(int); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "sizeof(int)"),
                // (45,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &this.instanceField; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&this.instanceField"),
                // (46,15): error CS0211: Cannot take the address of the given expression
                //         p = &(+local); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "+local"),
                // (49,16): error CS0211: Cannot take the address of the given expression
                //         pp = &(&local); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "&local"),
                // (51,19): error CS0211: Cannot take the address of the given expression
                //         var q = &(new { }); //CS0208, CS0211 (managed)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "new { }"),
                // (52,19): error CS0211: Cannot take the address of the given expression
                //         var r = &(new int[1]); //CS0208, CS0211 (managed)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "new int[1]"),
                // (53,19): error CS0211: Cannot take the address of the given expression
                //         var s = &(array as object); //CS0208, CS0211 (managed)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "array as object"),
                // (54,17): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('System.Action')
                //         var t = &E; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "&E").WithArguments("System.Action"),
                // (55,18): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         var u = &F; //CS0079 (can't use event like that)
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (56,19): error CS0211: Cannot take the address of the given expression
                //         var v = &(E += null); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "E += null"),
                // (57,19): error CS0211: Cannot take the address of the given expression
                //         var w = &(F += null); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "F += null"),
                // (58,19): error CS0211: Cannot take the address of the given expression
                //         var x = &(array is object); //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "array is object"),
                // (59,19): error CS0211: Cannot take the address of the given expression
                //         var y = &(array ?? array); //CS0208, CS0211 (managed)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "array ?? array"),
                // (60,19): error CS0459: Cannot take the address of a read-only local variable
                //         var aa = &this; //CS0208, CS0459 (readonly)
                Diagnostic(ErrorCode.ERR_AddrOnReadOnlyLocal, "this").WithArguments("this"),
                // (61,19): error CS0211: Cannot take the address of the given expression
                //         var bb = &typeof(int); //CS0208, CS0211 (managed)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "typeof(int)"),
                // (62,19): error CS0211: Cannot take the address of the given expression
                //         var cc = &Color.Red; //CS0211
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "Color.Red"),
                // (76,18): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         var aa = &this; //CS0212 (need fixed)
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&this"));
        }

        #endregion AddressOf operand kinds

        #region AddressOf diagnostics

        [Fact]
        public void AddressOfManaged()
        {
            var text = @"
unsafe class C
{
    void M<T>(T t)
    {
        var p0 = &t; //CS0208

        C c = new C();
        var p1 = &c; //CS0208

        S s = new S();
        var p2 = &s; //CS0208
        
        var anon = new { };
        var p3 = &anon; //CS0208
    }
}

public struct S
{
    public string s;
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,18): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('T')
                //         var p0 = &t; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "&t").WithArguments("T"),
                // (9,18): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('C')
                //         var p1 = &c; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "&c").WithArguments("C"),
                // (12,18): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //         var p2 = &s; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "&s").WithArguments("S"),
                // (15,18): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('<empty anonymous type>')
                //         var p3 = &anon; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "&anon").WithArguments("<empty anonymous type>"));
        }

        [Fact]
        public void AddressOfManaged_Cycle()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        S s = new S();
        var p = &s; //CS0208
    }
}

public struct S
{
    public S s; //CS0523
    public object o;
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (13,14): error CS0523: Struct member 'S.s' of type 'S' causes a cycle in the struct layout
                //     public S s; //CS0523
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "s").WithArguments("S.s", "S"),
                // (7,17): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //         var p = &s; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "&s").WithArguments("S"));
        }

        [Fact]
        public void AddressOfMoveableVariable()
        {
            var text = @"
class Base
{
    public int instanceField;
    public int staticField;
}

unsafe class Derived : Base
{
    void M(ref int refParam, out int outParam)
    {
        Derived d = this;
        int[] array = new int[2];

        int* p;
        
        p = &instanceField; //CS0212
        p = &this.instanceField; //CS0212
        p = &base.instanceField; //CS0212
        p = &d.instanceField; //CS0212
        
        p = &staticField; //CS0212
        p = &this.staticField; //CS0212
        p = &base.staticField; //CS0212
        p = &d.staticField; //CS0212

        p = &array[0]; //CS0212

        p = &refParam; //CS0212
        p = &outParam; //CS0212
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (17,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &instanceField; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&instanceField"),
                // (18,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &this.instanceField; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&this.instanceField"),
                // (19,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &base.instanceField; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&base.instanceField"),
                // (20,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &d.instanceField; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&d.instanceField"),
                // (22,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &staticField; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&staticField"),
                // (23,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &this.staticField; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&this.staticField"),
                // (24,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &base.staticField; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&base.staticField"),
                // (25,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &d.staticField; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&d.staticField"),
                // (27,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &array[0]; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&array[0]"),
                // (29,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &refParam; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&refParam"),
                // (30,13): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         p = &outParam; //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&outParam"));
        }

        [Fact]
        public void AddressOfInitializes()
        {
            var text = @"
public struct S
{
    public int x;
    public int y;
}

unsafe class C
{
    void M()
    {
        S s;
        int* p = &s.x;
        int x = s.x; //fine
        int y = s.y; //cs0170 (uninitialized)
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (15,17): error CS0170: Use of possibly unassigned field 'y'
                //         int y = s.y; //cs0170 (uninitialized)
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "s.y").WithArguments("y"));
        }

        [Fact]
        public void AddressOfCapturedLocal1()
        {
            var text = @"
unsafe class C
{
    void M(System.Action a)
    {
        int x;
        int* p = &x; //before capture
        M(() => { x++; });
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,11): error CS1686: Local 'x' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //         M(&x, () => { x++; });
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&x").WithArguments("x"));
        }

        [Fact]
        public void AddressOfCapturedLocal2()
        {
            var text = @"
unsafe class C
{
    void M(System.Action a)
    {
        int x = 1;
        M(() => { x++; });
        int* p = &x; //after capture
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,18): error CS1686: Local 'x' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //         int* p = &x;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&x").WithArguments("x"));
        }

        [Fact]
        public void AddressOfCapturedLocal3()
        {
            var text = @"
unsafe class C
{
    void M(System.Action a)
    {
        int x;
        M(() => { int* p = &x; }); // in lambda
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,28): error CS1686: Local 'x' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //         M(() => { int* p = &x; }); // in lambda
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&x").WithArguments("x"));
        }

        [Fact]
        public void AddressOfCapturedLocal4()
        {
            var text = @"
unsafe class C
{
    void M(System.Action a)
    {
        int x;
        int* p = &x; //only report the first
        M(() => { p = &x; });
        p = &x;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,28): error CS1686: Local 'x' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //         M(() => { int* p = &x; }); // in lambda
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&x").WithArguments("x"));
        }

        [Fact]
        public void AddressOfCapturedStructField1()
        {
            var text = @"
unsafe struct S
{
    int x;    

    void M(System.Action a)
    {
        S s;
        int* p = &s.x; //before capture
        M(() => { s.x++; });
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,18): error CS1686: Local 's' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //         int* p = &s.x; //before capture
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&s.x").WithArguments("s"));
        }

        [Fact]
        public void AddressOfCapturedStructField2()
        {
            var text = @"
unsafe struct S
{
    int x;    

    void M(System.Action a)
    {
        S s;
        s.x = 1;
        M(() => { s.x++; });
        int* p = &s.x; //after capture
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (11,18): error CS1686: Local 's' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //         int* p = &s.x; //after capture
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&s.x").WithArguments("s"));
        }

        [Fact]
        public void AddressOfCapturedStructField3()
        {
            var text = @"
unsafe struct S
{
    int x;    

    void M(System.Action a)
    {
        S s;
        M(() => { int* p = &s.x; }); // in lambda
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,28): error CS1686: Local 's' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //         M(() => { int* p = &s.x; }); // in lambda
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&s.x").WithArguments("s"));
        }

        [Fact]
        public void AddressOfCapturedStructField4()
        {
            var text = @"
unsafe struct S
{
    int x;    

    void M(System.Action a)
    {
        S s;
        int* p = &s.x; //only report the first
        M(() => { p = &s.x; });
        p = &s.x;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,18): error CS1686: Local 's' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //         int* p = &s.x; //only report the first
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&s.x").WithArguments("s"));
        }

        [Fact]
        public void AddressOfCapturedParameters()
        {
            var text = @"
unsafe struct S
{
    int x;    

    void M(int x, S s, System.Action a)
    {
        M(x, s, () => 
        {
            int* p1 = &x;
            int* p2 = &s.x;
        });
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (10,23): error CS1686: Local 'x' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //             int* p1 = &x;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&x").WithArguments("x"),
                // (11,23): error CS1686: Local 's' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //             int* p2 = &s.x;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&s.x").WithArguments("s")
                );
        }

        [WorkItem(657083, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657083")]
        [Fact]
        public void CaptureStructWithFixedArray()
        {
            var text = @"
unsafe public struct Test
{
    private delegate int D();
    public fixed int i[1];
    public int foo()
    {
        Test t = this;
        t.i[0] = 5;
        D d = () => t.i[0];
        return d();
    }
}";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "t.i").WithArguments("t")
                );
        }

        [Fact]
        public void AddressOfCapturedMoveable1()
        {
            var text = @"
unsafe class C
{
    int x;    

    void M(System.Action a)
    {
        fixed(int* p = &x) //fine - error only applies to non-moveable variables
        {
            M(() => x++);
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void AddressOfCapturedMoveable2()
        {
            var text = @"
unsafe class C
{
    void M(ref int x, System.Action a)
    {
        fixed (int* p = &x) //fine - error only applies to non-moveable variables
        {
            M(ref x, () => x++);
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,28): error CS1628: Cannot use ref or out parameter 'x' inside an anonymous method, lambda expression, or query expression
                //             M(ref x, () => x++);
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x").WithArguments("x"));
        }

        [WorkItem(543989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543989")]
        [Fact]
        public void AddressOfInsideAnonymousTypes()
        {
            var text = @"
public class C
{
    public static void Main()
    {
        int x = 10;
        unsafe
        {
            var t = new { p1 = &x };
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                //(9,27): error CS0828: Cannot assign int* to anonymous type property
                //             p1 = &x
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "p1 = &x").WithArguments("int*"));
        }

        [WorkItem(544537, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544537")]
        [Fact]
        public void AddressOfStaticReadonlyFieldInsideFixed()
        {
            var text = @"
public class Test
{
    static readonly int R1 = 45;

    unsafe public static void Main()
    {
        fixed (int* v1 = &R1) { }
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,27): error CS0211: Cannot take the address of the given expression
                //         fixed (int* v1 = &R1) { }
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "R1"));
        }

        #endregion AddressOf diagnostics

        #region AddressOf SemanticModel tests

        [Fact]
        public void AddressOfSemanticModelAPIs()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        int x;
        int* p = &x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            Assert.Equal(SyntaxKind.AddressOfExpression, syntax.Kind());

            var symbolInfo = model.GetSymbolInfo(syntax);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

            var typeInfo = model.GetTypeInfo(syntax);
            var type = typeInfo.Type;
            var conv = model.GetConversion(syntax);
            Assert.NotNull(type);
            Assert.Same(type, typeInfo.ConvertedType);
            Assert.Equal(Conversion.Identity, conv);
            Assert.Equal(TypeKind.Pointer, type.TypeKind);
            Assert.Equal(SpecialType.System_Int32, ((PointerTypeSymbol)type).PointedAtType.SpecialType);

            var declaredSymbol = model.GetDeclaredSymbol(syntax.Ancestors().OfType<VariableDeclaratorSyntax>().First());
            Assert.NotNull(declaredSymbol);
            Assert.Equal(SymbolKind.Local, declaredSymbol.Kind);
            Assert.Equal("p", declaredSymbol.Name);
            Assert.Equal(type, ((LocalSymbol)declaredSymbol).Type.TypeSymbol);
        }

        [Fact]
        public void SpeculativelyBindPointerToManagedType()
        {
            var text = @"
unsafe struct S
{
    public object o;
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Single();
            Assert.Equal(SyntaxKind.FieldDeclaration, syntax.Kind());

            model.GetSpeculativeTypeInfo(syntax.SpanStart, SyntaxFactory.ParseTypeName("S*"), SpeculativeBindingOption.BindAsTypeOrNamespace);

            // Specifically don't see diagnostic from speculative binding.
            compilation.VerifyDiagnostics(
                // (4,19): warning CS0649: Field 'S.o' is never assigned to, and will always have its default value null
                //     public object o;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "o").WithArguments("S.o", "null"));
        }

        [WorkItem(544346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544346")]
        [Fact]
        public void AddressOfLambdaExpr1()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        var i1 = &()=>5;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            Assert.Equal(SyntaxKind.AddressOfExpression, syntax.Kind());
            Assert.Equal("&()", syntax.ToString()); //NOTE: not actually lambda

            var symbolInfo = model.GetSymbolInfo(syntax);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

            var typeInfo = model.GetTypeInfo(syntax);
            var type = typeInfo.Type;
            var conv = model.GetConversion(syntax);
            Assert.NotNull(type);
            Assert.Same(type, typeInfo.ConvertedType);
            Assert.Equal(Conversion.Identity, conv);

            Assert.Equal("?*", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Pointer, typeInfo.Type.TypeKind);
            Assert.Equal(TypeKind.Error, ((PointerTypeSymbol)typeInfo.Type).PointedAtType.TypeKind);
        }

        [WorkItem(544346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544346")]
        [Fact]
        public void AddressOfLambdaExpr2()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        var i1 = &(()=>5);
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            Assert.Equal(SyntaxKind.AddressOfExpression, syntax.Kind());
            Assert.Equal("&(()=>5)", syntax.ToString());

            var symbolInfo = model.GetSymbolInfo(syntax);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

            var typeInfo = model.GetTypeInfo(syntax);
            var type = typeInfo.Type;
            var conv = model.GetConversion(syntax);
            Assert.NotNull(type);
            Assert.Same(type, typeInfo.ConvertedType);
            Assert.Equal(Conversion.Identity, conv);

            Assert.Equal("?*", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Pointer, typeInfo.Type.TypeKind);
            Assert.Equal(TypeKind.Error, ((PointerTypeSymbol)typeInfo.Type).PointedAtType.TypeKind);
        }

        [WorkItem(544346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544346")]
        [Fact]
        public void AddressOfMethodGroup()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        var i1 = &M;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            Assert.Equal(SyntaxKind.AddressOfExpression, syntax.Kind());

            var symbolInfo = model.GetSymbolInfo(syntax);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

            var typeInfo = model.GetTypeInfo(syntax);
            var type = typeInfo.Type;
            var conv = model.GetConversion(syntax);
            Assert.NotNull(type);
            Assert.Same(type, typeInfo.ConvertedType);
            Assert.Equal(Conversion.Identity, conv);

            Assert.Equal("?*", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Pointer, typeInfo.Type.TypeKind);
            Assert.Equal(TypeKind.Error, ((PointerTypeSymbol)typeInfo.Type).PointedAtType.TypeKind);
        }


        #endregion AddressOf SemanticModel tests

        #region Dereference diagnostics

        [Fact]
        public void DereferenceSuccess()
        {
            var text = @"
unsafe class C
{
    int M(int* p)
    {
        return *p;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void DereferenceNullLiteral()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        int x = *null;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,17): error CS0193: The * or -> operator must be applied to a pointer
                //         int x = *null;
                Diagnostic(ErrorCode.ERR_PtrExpected, "*null"));
        }

        [Fact]
        public void DereferenceNonPointer()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        int p = 1;
        int x = *p;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,17): error CS0193: The * or -> operator must be applied to a pointer
                //         int x = *p;
                Diagnostic(ErrorCode.ERR_PtrExpected, "*p"));
        }

        [Fact]
        public void DereferenceVoidPointer()
        {
            var text = @"
unsafe class C
{
    void M(void* p)
    {
        var x = *p;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,17): error CS0242: The operation in question is undefined on void pointers
                //         var x = *p;
                Diagnostic(ErrorCode.ERR_VoidError, "*p"));
        }

        [Fact]
        public void DereferenceUninitialized()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        int* p;
        int x = *p;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,18): error CS0165: Use of unassigned local variable 'p'
                //         int x = *p;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "p").WithArguments("p"));
        }

        #endregion Dereference diagnostics

        #region Dereference SemanticModel tests

        [Fact]
        public void DereferenceSemanticModelAPIs()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        int x;
        int* p = &x;
        x = *p;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Last();
            Assert.Equal(SyntaxKind.PointerIndirectionExpression, syntax.Kind());

            var symbolInfo = model.GetSymbolInfo(syntax);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

            var typeInfo = model.GetTypeInfo(syntax);
            var type = typeInfo.Type;
            var conv = model.GetConversion(syntax);
            Assert.NotNull(type);
            Assert.Same(type, typeInfo.ConvertedType);
            Assert.Equal(Conversion.Identity, conv);
            Assert.Equal(SpecialType.System_Int32, type.SpecialType);
        }

        #endregion Dereference SemanticModel tests

        #region PointerMemberAccess diagnostics

        [Fact]
        public void PointerMemberAccessSuccess()
        {
            var text = @"
unsafe class C
{
    string M(int* p)
    {
        return p->ToString();
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void PointerMemberAccessAddress()
        {
            var text = @"
unsafe struct S
{
    int x;

    void M(S* sp)
    {
        int* ip = &(sp->x);
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void PointerMemberAccessNullLiteral()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        string x = null->ToString(); //Roslyn: CS0193 / Dev10: CS0023
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,20): error CS0193: The * or -> operator must be applied to a pointer
                //         string x = null->ToString(); //Roslyn: CS0193 / Dev10: CS0023
                Diagnostic(ErrorCode.ERR_PtrExpected, "null->ToString"));
        }

        [Fact]
        public void PointerMemberAccessMethodGroup()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        string x = M->ToString(); //Roslyn: CS0193 / Dev10: CS0023
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,20): error CS0193: The * or -> operator must be applied to a pointer
                //         string x = M->ToString(); //Roslyn: CS0193 / Dev10: CS0023
                Diagnostic(ErrorCode.ERR_PtrExpected, "M->ToString"));
        }

        [Fact]
        public void PointerMemberAccessLambda()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        string x = (z => z)->ToString(); //Roslyn: CS0193 / Dev10: CS0023
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,20): error CS0193: The * or -> operator must be applied to a pointer
                //         string x = (z => z)->ToString(); //Roslyn: CS0193 / Dev10: CS0023
                Diagnostic(ErrorCode.ERR_PtrExpected, "(z => z)->ToString"));
        }

        [Fact]
        public void PointerMemberAccessNonPointer()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        int p = 1;
        int x = p->GetHashCode();
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,17): error CS0193: The * or -> operator must be applied to a pointer
                //         int x = p->GetHashCode();
                Diagnostic(ErrorCode.ERR_PtrExpected, "p->GetHashCode"));
        }

        [Fact]
        public void PointerMemberAccessVoidPointer()
        {
            var text = @"
unsafe class C
{
    void M(void* p)
    {
        var x = p->GetHashCode();
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,17): error CS0242: The operation in question is undefined on void pointers
                //         var x = p->GetHashCode();
                Diagnostic(ErrorCode.ERR_VoidError, "p->GetHashCode"));
        }

        [Fact]
        public void PointerMemberAccessUninitialized()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        int* p;
        int x = p->GetHashCode();
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,18): error CS0165: Use of unassigned local variable 'p'
                //         int x = *p;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "p").WithArguments("p"));
        }

        [Fact]
        public void PointerMemberAccessMemberKinds()
        {
            var text = @"
unsafe struct S
{
    int InstanceField;
    static int StaticField;

    int InstanceProperty { get; set; }
    static int StaticProperty { get; set; }

    // No syntax for indexer access.
    //int this[int x] { get { return 0; } set { } }

    void InstanceMethod() { }
    static void StaticMethod() { }

    // No syntax for type member access.
    //delegate void Delegate();
    //struct Type { }

    static void Main()
    {
        S s;
        S* p = &s;

        p->InstanceField = 1;
        p->StaticField = 1; //CS0176

        p->InstanceProperty = 2;
        p->StaticProperty = 2; //CS0176

        p->InstanceMethod();
        p->StaticMethod(); //CS0176

        p->ExtensionMethod();

        System.Action a;
        a = p->InstanceMethod;
        a = p->StaticMethod; //CS0176
        a = p->ExtensionMethod; //CS1113
    }
}

static class Extensions
{
    public static void ExtensionMethod(this S s)
    {
    }
}
";
            CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (26,9): error CS0176: Member 'S.StaticField' cannot be accessed with an instance reference; qualify it with a type name instead
                //         p->StaticField = 1; //CS0176
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "p->StaticField").WithArguments("S.StaticField"),
                // (29,9): error CS0176: Member 'S.StaticProperty' cannot be accessed with an instance reference; qualify it with a type name instead
                //         p->StaticProperty = 2; //CS0176
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "p->StaticProperty").WithArguments("S.StaticProperty"),
                // (32,9): error CS0176: Member 'S.StaticMethod()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         p->StaticMethod(); //CS0176
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "p->StaticMethod").WithArguments("S.StaticMethod()"),
                // (38,13): error CS0176: Member 'S.StaticMethod()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         a = p->StaticMethod; //CS0176
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "p->StaticMethod").WithArguments("S.StaticMethod()"),
                // (39,13): error CS1113: Extension method 'Extensions.ExtensionMethod(S)' defined on value type 'S' cannot be used to create delegates
                //         a = p->ExtensionMethod; //CS1113
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "p->ExtensionMethod").WithArguments("Extensions.ExtensionMethod(S)", "S")
                );
        }

        // NOTE: a type with events is managed, so this is always an error case.
        [Fact]
        public void PointerMemberAccessEvents()
        {
            var text = @"
unsafe struct S
{
    event System.Action InstanceFieldLikeEvent;
    static event System.Action StaticFieldLikeEvent;

    event System.Action InstanceCustomEvent { add { } remove { } }
    static event System.Action StaticCustomEvent { add { } remove { } }

    static void Main()
    {
        S s;
        S* p = &s; //CS0208

        p->InstanceFieldLikeEvent += null;
        p->StaticFieldLikeEvent += null; //CS0176

        p->InstanceCustomEvent += null;
        p->StaticCustomEvent += null; //CS0176
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (13,9): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //         S* p = &s; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "S*").WithArguments("S"),
                // (13,16): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //         S* p = &s; //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "&s").WithArguments("S"),
                // (16,9): error CS0176: Member 'S.StaticFieldLikeEvent' cannot be accessed with an instance reference; qualify it with a type name instead
                //         p->StaticFieldLikeEvent += null; //CS0176
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "p->StaticFieldLikeEvent").WithArguments("S.StaticFieldLikeEvent"),
                // (19,9): error CS0176: Member 'S.StaticCustomEvent' cannot be accessed with an instance reference; qualify it with a type name instead
                //         p->StaticCustomEvent += null; //CS0176
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "p->StaticCustomEvent").WithArguments("S.StaticCustomEvent"),
                // (5,32): warning CS0067: The event 'S.StaticFieldLikeEvent' is never used
                //     static event System.Action StaticFieldLikeEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "StaticFieldLikeEvent").WithArguments("S.StaticFieldLikeEvent"),
                // (4,25): warning CS0067: The event 'S.InstanceFieldLikeEvent' is never used
                //     event System.Action InstanceFieldLikeEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "InstanceFieldLikeEvent").WithArguments("S.InstanceFieldLikeEvent")
                );
        }

        #endregion PointerMemberAccess diagnostics

        #region PointerMemberAccess SemanticModel tests

        [Fact]
        public void PointerMemberAccessSemanticModelAPIs()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        S s;
        S* p = &s;
        p->M();
    }
}

struct S
{
    public void M() { }
    public void M(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            Assert.Equal(SyntaxKind.PointerMemberAccessExpression, syntax.Kind());

            var receiverSyntax = syntax.Expression;
            var methodGroupSyntax = syntax;
            var callSyntax = syntax.Parent;

            var structType = compilation.GlobalNamespace.GetMember<TypeSymbol>("S");
            var structPointerType = new PointerTypeSymbol(TypeSymbolWithAnnotations.Create(structType));
            var structMethod1 = structType.GetMembers("M").OfType<MethodSymbol>().Single(m => m.ParameterCount == 0);
            var structMethod2 = structType.GetMembers("M").OfType<MethodSymbol>().Single(m => m.ParameterCount == 1);

            var receiverSummary = model.GetSemanticInfoSummary(receiverSyntax);
            var receiverSymbol = receiverSummary.Symbol;
            Assert.Equal(SymbolKind.Local, receiverSymbol.Kind);
            Assert.Equal(structPointerType, ((LocalSymbol)receiverSymbol).Type.TypeSymbol);
            Assert.Equal("p", receiverSymbol.Name);
            Assert.Equal(CandidateReason.None, receiverSummary.CandidateReason);
            Assert.Equal(0, receiverSummary.CandidateSymbols.Length);
            Assert.Equal(structPointerType, receiverSummary.Type);
            Assert.Equal(structPointerType, receiverSummary.ConvertedType);
            Assert.Equal(ConversionKind.Identity, receiverSummary.ImplicitConversion.Kind);
            Assert.Equal(0, receiverSummary.MethodGroup.Length);

            var methodGroupSummary = model.GetSemanticInfoSummary(methodGroupSyntax);
            Assert.Equal(structMethod1, methodGroupSummary.Symbol);
            Assert.Equal(CandidateReason.None, methodGroupSummary.CandidateReason);
            Assert.Equal(0, methodGroupSummary.CandidateSymbols.Length);
            Assert.Null(methodGroupSummary.Type);
            Assert.Null(methodGroupSummary.ConvertedType);
            Assert.Equal(ConversionKind.Identity, methodGroupSummary.ImplicitConversion.Kind);
            Assert.True(methodGroupSummary.MethodGroup.SetEquals(ImmutableArray.Create<IMethodSymbol>(structMethod1, structMethod2), EqualityComparer<IMethodSymbol>.Default));

            var callSummary = model.GetSemanticInfoSummary(callSyntax);
            Assert.Equal(structMethod1, callSummary.Symbol);
            Assert.Equal(CandidateReason.None, callSummary.CandidateReason);
            Assert.Equal(0, callSummary.CandidateSymbols.Length);
            Assert.Equal(SpecialType.System_Void, callSummary.Type.SpecialType);
            Assert.Equal(SpecialType.System_Void, callSummary.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, callSummary.ImplicitConversion.Kind);
            Assert.Equal(0, callSummary.MethodGroup.Length);
        }

        [Fact]
        public void PointerMemberAccessSemanticModelAPIs_ErrorScenario()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        S s;
        S* p = &s;
        s->M(); //should be 'p'
    }
}

struct S
{
    public void M() { }
    public void M(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            Assert.Equal(SyntaxKind.PointerMemberAccessExpression, syntax.Kind());

            var receiverSyntax = syntax.Expression;
            var methodGroupSyntax = syntax;
            var callSyntax = syntax.Parent;

            var structType = compilation.GlobalNamespace.GetMember<TypeSymbol>("S");
            var structMethod1 = structType.GetMembers("M").OfType<MethodSymbol>().Single(m => m.ParameterCount == 0);
            var structMethod2 = structType.GetMembers("M").OfType<MethodSymbol>().Single(m => m.ParameterCount == 1);
            var structMethods = ImmutableArray.Create<MethodSymbol>(structMethod1, structMethod2);

            var receiverSummary = model.GetSemanticInfoSummary(receiverSyntax);
            var receiverSymbol = receiverSummary.Symbol;
            Assert.Equal(SymbolKind.Local, receiverSymbol.Kind);
            Assert.Equal(structType, ((LocalSymbol)receiverSymbol).Type.TypeSymbol);
            Assert.Equal("s", receiverSymbol.Name);
            Assert.Equal(CandidateReason.None, receiverSummary.CandidateReason);
            Assert.Equal(0, receiverSummary.CandidateSymbols.Length);
            Assert.Equal(structType, receiverSummary.Type);
            Assert.Equal(structType, receiverSummary.ConvertedType);
            Assert.Equal(ConversionKind.Identity, receiverSummary.ImplicitConversion.Kind);
            Assert.Equal(0, receiverSummary.MethodGroup.Length);

            var methodGroupSummary = model.GetSemanticInfoSummary(methodGroupSyntax);
            Assert.Equal(structMethod1, methodGroupSummary.Symbol); // Have enough info for overload resolution.
            Assert.Null(methodGroupSummary.Type);
            Assert.Null(methodGroupSummary.ConvertedType);
            Assert.Equal(ConversionKind.Identity, methodGroupSummary.ImplicitConversion.Kind);
            Assert.True(methodGroupSummary.MethodGroup.SetEquals(StaticCast<IMethodSymbol>.From(structMethods), EqualityComparer<IMethodSymbol>.Default));

            var callSummary = model.GetSemanticInfoSummary(callSyntax);
            Assert.Equal(structMethod1, callSummary.Symbol); // Have enough info for overload resolution.
            Assert.Equal(SpecialType.System_Void, callSummary.Type.SpecialType);
            Assert.Equal(callSummary.Type, callSummary.ConvertedType);
            Assert.Equal(ConversionKind.Identity, callSummary.ImplicitConversion.Kind);
            Assert.Equal(0, callSummary.MethodGroup.Length);
        }

        #endregion PointerMemberAccess SemanticModel tests

        #region PointerElementAccess

        [Fact]
        public void PointerElementAccess_NoIndices()
        {
            var text = @"
unsafe struct S
{
    void M(S* p)
    {
        S s = p[];
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,17): error CS0443: Syntax error; value expected
                //         S s = p[];
                Diagnostic(ErrorCode.ERR_ValueExpected, "]"));
        }

        [Fact]
        public void PointerElementAccess_MultipleIndices()
        {
            var text = @"
unsafe struct S
{
    void M(S* p)
    {
        S s = p[1, 2];
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,15): error CS0196: A pointer must be indexed by only one value
                //         S s = p[1, 2];
                Diagnostic(ErrorCode.ERR_PtrIndexSingle, "p[1, 2]"));
        }

        [Fact]
        public void PointerElementAccess_RefIndex()
        {
            var text = @"
unsafe struct S
{
    void M(S* p)
    {
        int x = 1;
        S s = p[ref x];
    }
}
";
            // Dev10 gives an unhelpful syntax error.
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,21): error CS1615: Argument 1 should not be passed with the 'ref' keyword
                //         S s = p[ref x];
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "x").WithArguments("1", "ref"));
        }

        [Fact]
        public void PointerElementAccess_OutIndex()
        {
            var text = @"
unsafe struct S
{
    void M(S* p)
    {
        int x = 1;
        S s = p[out x];
    }
}
";
            // Dev10 gives an unhelpful syntax error.
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,21): error CS1615: Argument 1 should not be passed with the 'out' keyword
                //         S s = p[out x];
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "x").WithArguments("1", "out"));
        }

        [Fact]
        public void PointerElementAccess_NamedOffset()
        {
            var text = @"
unsafe struct S
{
    void M(S* p)
    {
        int x = 1;
        S s = p[index: x];
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,15): error CS1742: An array access may not have a named argument specifier
                //         S s = p[index: x];
                Diagnostic(ErrorCode.ERR_NamedArgumentForArray, "p[index: x]"));
        }

        [Fact]
        public void PointerElementAccess_VoidPointer()
        {
            var text = @"
unsafe struct S
{
    void M(void* p)
    {
        p[0] = null;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,9): error CS0242: The operation in question is undefined on void pointers
                //         p[0] = null;
                Diagnostic(ErrorCode.ERR_VoidError, "p"));
        }

        #endregion PointerElementAccess diagnostics

        #region PointerElementAccess SemanticModel tests

        [Fact]
        public void PointerElementAccessSemanticModelAPIs()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        const int size = 3;
        fixed(int* p = new int[size])
        {
            for (int i = 0; i < size; i++)
            {
                p[i] = i * i;
            }
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            Assert.Equal(SyntaxKind.ElementAccessExpression, syntax.Kind());

            var receiverSyntax = syntax.Expression;
            var indexSyntax = syntax.ArgumentList.Arguments.Single().Expression;
            var accessSyntax = syntax;

            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var intPointerType = new PointerTypeSymbol(TypeSymbolWithAnnotations.Create(intType));

            var receiverSummary = model.GetSemanticInfoSummary(receiverSyntax);
            var receiverSymbol = receiverSummary.Symbol;
            Assert.Equal(SymbolKind.Local, receiverSymbol.Kind);
            Assert.Equal(intPointerType, ((LocalSymbol)receiverSymbol).Type.TypeSymbol);
            Assert.Equal("p", receiverSymbol.Name);
            Assert.Equal(CandidateReason.None, receiverSummary.CandidateReason);
            Assert.Equal(0, receiverSummary.CandidateSymbols.Length);
            Assert.Equal(intPointerType, receiverSummary.Type);
            Assert.Equal(intPointerType, receiverSummary.ConvertedType);
            Assert.Equal(ConversionKind.Identity, receiverSummary.ImplicitConversion.Kind);
            Assert.Equal(0, receiverSummary.MethodGroup.Length);

            var indexSummary = model.GetSemanticInfoSummary(indexSyntax);
            var indexSymbol = indexSummary.Symbol;
            Assert.Equal(SymbolKind.Local, indexSymbol.Kind);
            Assert.Equal(intType, ((LocalSymbol)indexSymbol).Type.TypeSymbol);
            Assert.Equal("i", indexSymbol.Name);
            Assert.Equal(CandidateReason.None, indexSummary.CandidateReason);
            Assert.Equal(0, indexSummary.CandidateSymbols.Length);
            Assert.Equal(intType, indexSummary.Type);
            Assert.Equal(intType, indexSummary.ConvertedType);
            Assert.Equal(ConversionKind.Identity, indexSummary.ImplicitConversion.Kind);
            Assert.Equal(0, indexSummary.MethodGroup.Length);

            var accessSummary = model.GetSemanticInfoSummary(accessSyntax);
            Assert.Null(accessSummary.Symbol);
            Assert.Equal(CandidateReason.None, accessSummary.CandidateReason);
            Assert.Equal(0, accessSummary.CandidateSymbols.Length);
            Assert.Equal(intType, accessSummary.Type);
            Assert.Equal(intType, accessSummary.ConvertedType);
            Assert.Equal(ConversionKind.Identity, accessSummary.ImplicitConversion.Kind);
            Assert.Equal(0, accessSummary.MethodGroup.Length);
        }

        #endregion PointerElementAccess SemanticModel tests

        #region Pointer conversion tests

        [Fact]
        public void NullLiteralConversion()
        {
            var text = @"
unsafe struct S
{
    void M()
    {
        byte* b = null;
        int* i = null;
        S* s = null;
        void* v = null;
    }
}
";

            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            compilation.VerifyDiagnostics();

            foreach (var nullSyntax in tree.GetCompilationUnitRoot().DescendantTokens().Where(token => token.IsKind(SyntaxKind.NullKeyword)))
            {
                var node = (ExpressionSyntax)nullSyntax.Parent;
                var typeInfo = model.GetTypeInfo(node);
                var conv = model.GetConversion(node);
                Assert.Null(typeInfo.Type);
                Assert.Equal(TypeKind.Pointer, typeInfo.ConvertedType.TypeKind);
                Assert.Equal(ConversionKind.NullToPointer, conv.Kind);
            }
        }

        [Fact]
        public void VoidPointerConversion1()
        {
            var text = @"
unsafe struct S
{
    void M()
    {
        byte* b = null;
        int* i = null;
        S* s = null;

        void* v1 = b;
        void* v2 = i;
        void* v3 = s;
    }
}
";

            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            compilation.VerifyDiagnostics();

            foreach (var declarationSyntax in tree.GetCompilationUnitRoot().DescendantTokens().OfType<VariableDeclarationSyntax>().Where(syntax => syntax.GetFirstToken().IsKind(SyntaxKind.VoidKeyword)))
            {
                var value = declarationSyntax.Variables.Single().Initializer.Value;
                var typeInfo = model.GetTypeInfo(value);

                var type = typeInfo.Type;
                Assert.Equal(TypeKind.Pointer, type.TypeKind);
                Assert.NotEqual(SpecialType.System_Void, ((PointerTypeSymbol)type).PointedAtType.SpecialType);

                var convertedType = typeInfo.ConvertedType;
                Assert.Equal(TypeKind.Pointer, convertedType.TypeKind);
                Assert.Equal(SpecialType.System_Void, ((PointerTypeSymbol)convertedType).PointedAtType.SpecialType);

                var conv = model.GetConversion(value);
                Assert.Equal(ConversionKind.PointerToVoid, conv.Kind);
            }
        }

        [Fact]
        public void VoidPointerConversion2()
        {
            var text = @"
unsafe struct S
{
    void M()
    {
        void* v = null;
        void* vv1 = &v;
        void** vv2 = &v;
        void* vv3 = vv2;
        void** vv4 = vv3;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (10,22): error CS0266: Cannot implicitly convert type 'void*' to 'void**'. An explicit conversion exists (are you missing a cast?)
                //         void** vv4 = vv3;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "vv3").WithArguments("void*", "void**"));
        }

        [Fact]
        public void ExplicitPointerConversion()
        {
            var text = @"
unsafe struct S
{
    void M(int* i, byte* b, void* v, int** ii, byte** bb, void** vv)
    {
        i = (int*)b;
        i = (int*)v;
        i = (int*)ii;
        i = (int*)bb;
        i = (int*)vv;

        b = (byte*)i;
        b = (byte*)v;
        b = (byte*)ii;
        b = (byte*)bb;
        b = (byte*)vv;

        v = (void*)i;
        v = (void*)b;
        v = (void*)ii;
        v = (void*)bb;
        v = (void*)vv;

        ii = (int**)i;
        ii = (int**)b;
        ii = (int**)v;
        ii = (int**)bb;
        ii = (int**)vv;

        bb = (byte**)i;
        bb = (byte**)b;
        bb = (byte**)v;
        bb = (byte**)ii;
        bb = (byte**)vv;

        vv = (void**)i;
        vv = (void**)b;
        vv = (void**)v;
        vv = (void**)ii;
        vv = (void**)bb;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void ExplicitPointerNumericConversion()
        {
            var text = @"
unsafe struct S
{
    void M(int* pi, void* pv, sbyte sb, byte b, short s, ushort us, int i, uint ui, long l, ulong ul)
    {
        sb = (sbyte)pi;
        b = (byte)pi;
        s = (short)pi;
        us = (ushort)pi;
        i = (int)pi;
        ui = (uint)pi;
        l = (long)pi;
        ul = (ulong)pi;

        sb = (sbyte)pv;
        b = (byte)pv;
        s = (short)pv;
        us = (ushort)pv;
        i = (int)pv;
        ui = (uint)pv;
        l = (long)pv;
        ul = (ulong)pv;

        pi = (int*)sb;
        pi = (int*)b;
        pi = (int*)s;
        pi = (int*)us;
        pi = (int*)i;
        pi = (int*)ui;
        pi = (int*)l;
        pi = (int*)ul;

        pv = (void*)sb;
        pv = (void*)b;
        pv = (void*)s;
        pv = (void*)us;
        pv = (void*)i;
        pv = (void*)ui;
        pv = (void*)l;
        pv = (void*)ul;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void ExplicitPointerNumericConversion_Illegal()
        {
            var text = @"
unsafe struct S
{
    void M(int* pi, void* pv, bool b, char c, double d, decimal e, float f)
    {
        b = (bool)pi;
        c = (char)pi;
        d = (double)pi;
        e = (decimal)pi;
        f = (float)pi;

        b = (bool)pv;
        c = (char)pv;
        d = (double)pv;
        e = (decimal)pv;
        f = (float)pv;

        pi = (int*)b;
        pi = (int*)c;
        pi = (int*)d;
        pi = (int*)d;
        pi = (int*)f;

        pv = (void*)b;
        pv = (void*)c;
        pv = (void*)d;
        pv = (void*)e;
        pv = (void*)f;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,13): error CS0030: Cannot convert type 'int*' to 'bool'
                //         b = (bool)pi;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(bool)pi").WithArguments("int*", "bool"),
                // (7,13): error CS0030: Cannot convert type 'int*' to 'char'
                //         c = (char)pi;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(char)pi").WithArguments("int*", "char"),
                // (8,13): error CS0030: Cannot convert type 'int*' to 'double'
                //         d = (double)pi;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(double)pi").WithArguments("int*", "double"),
                // (9,13): error CS0030: Cannot convert type 'int*' to 'decimal'
                //         e = (decimal)pi;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(decimal)pi").WithArguments("int*", "decimal"),
                // (10,13): error CS0030: Cannot convert type 'int*' to 'float'
                //         f = (float)pi;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(float)pi").WithArguments("int*", "float"),
                // (12,13): error CS0030: Cannot convert type 'void*' to 'bool'
                //         b = (bool)pv;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(bool)pv").WithArguments("void*", "bool"),
                // (13,13): error CS0030: Cannot convert type 'void*' to 'char'
                //         c = (char)pv;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(char)pv").WithArguments("void*", "char"),
                // (14,13): error CS0030: Cannot convert type 'void*' to 'double'
                //         d = (double)pv;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(double)pv").WithArguments("void*", "double"),
                // (15,13): error CS0030: Cannot convert type 'void*' to 'decimal'
                //         e = (decimal)pv;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(decimal)pv").WithArguments("void*", "decimal"),
                // (16,13): error CS0030: Cannot convert type 'void*' to 'float'
                //         f = (float)pv;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(float)pv").WithArguments("void*", "float"),
                // (18,14): error CS0030: Cannot convert type 'bool' to 'int*'
                //         pi = (int*)b;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)b").WithArguments("bool", "int*"),
                // (19,14): error CS0030: Cannot convert type 'char' to 'int*'
                //         pi = (int*)c;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)c").WithArguments("char", "int*"),
                // (20,14): error CS0030: Cannot convert type 'double' to 'int*'
                //         pi = (int*)d;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)d").WithArguments("double", "int*"),
                // (21,14): error CS0030: Cannot convert type 'double' to 'int*'
                //         pi = (int*)d;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)d").WithArguments("double", "int*"),
                // (22,14): error CS0030: Cannot convert type 'float' to 'int*'
                //         pi = (int*)f;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)f").WithArguments("float", "int*"),
                // (24,14): error CS0030: Cannot convert type 'bool' to 'void*'
                //         pv = (void*)b;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)b").WithArguments("bool", "void*"),
                // (25,14): error CS0030: Cannot convert type 'char' to 'void*'
                //         pv = (void*)c;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)c").WithArguments("char", "void*"),
                // (26,14): error CS0030: Cannot convert type 'double' to 'void*'
                //         pv = (void*)d;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)d").WithArguments("double", "void*"),
                // (27,14): error CS0030: Cannot convert type 'decimal' to 'void*'
                //         pv = (void*)e;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)e").WithArguments("decimal", "void*"),
                // (28,14): error CS0030: Cannot convert type 'float' to 'void*'
                //         pv = (void*)f;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)f").WithArguments("float", "void*"));
        }

        [Fact]
        public void ExplicitPointerNumericConversion_Nullable()
        {
            var text = @"
unsafe struct S
{
    void M(int* pi, void* pv, sbyte? sb, byte? b, short? s, ushort? us, int? i, uint? ui, long? l, ulong? ul)
    {
        sb = (sbyte?)pi;
        b = (byte?)pi;
        s = (short?)pi;
        us = (ushort?)pi;
        i = (int?)pi;
        ui = (uint?)pi;
        l = (long?)pi;
        ul = (ulong?)pi;

        sb = (sbyte?)pv;
        b = (byte?)pv;
        s = (short?)pv;
        us = (ushort?)pv;
        i = (int?)pv;
        ui = (uint?)pv;
        l = (long?)pv;
        ul = (ulong?)pv;

        pi = (int*)sb;
        pi = (int*)b;
        pi = (int*)s;
        pi = (int*)us;
        pi = (int*)i;
        pi = (int*)ui;
        pi = (int*)l;
        pi = (int*)ul;

        pv = (void*)sb;
        pv = (void*)b;
        pv = (void*)s;
        pv = (void*)us;
        pv = (void*)i;
        pv = (void*)ui;
        pv = (void*)l;
        pv = (void*)ul;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (24,14): error CS0030: Cannot convert type 'sbyte?' to 'int*'
                //         pi = (int*)sb;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)sb").WithArguments("sbyte?", "int*"),
                // (25,14): error CS0030: Cannot convert type 'byte?' to 'int*'
                //         pi = (int*)b;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)b").WithArguments("byte?", "int*"),
                // (26,14): error CS0030: Cannot convert type 'short?' to 'int*'
                //         pi = (int*)s;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)s").WithArguments("short?", "int*"),
                // (27,14): error CS0030: Cannot convert type 'ushort?' to 'int*'
                //         pi = (int*)us;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)us").WithArguments("ushort?", "int*"),
                // (28,14): error CS0030: Cannot convert type 'int?' to 'int*'
                //         pi = (int*)i;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)i").WithArguments("int?", "int*"),
                // (29,14): error CS0030: Cannot convert type 'uint?' to 'int*'
                //         pi = (int*)ui;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)ui").WithArguments("uint?", "int*"),
                // (30,14): error CS0030: Cannot convert type 'long?' to 'int*'
                //         pi = (int*)l;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)l").WithArguments("long?", "int*"),
                // (31,14): error CS0030: Cannot convert type 'ulong?' to 'int*'
                //         pi = (int*)ul;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)ul").WithArguments("ulong?", "int*"),
                // (33,14): error CS0030: Cannot convert type 'sbyte?' to 'void*'
                //         pv = (void*)sb;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)sb").WithArguments("sbyte?", "void*"),
                // (34,14): error CS0030: Cannot convert type 'byte?' to 'void*'
                //         pv = (void*)b;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)b").WithArguments("byte?", "void*"),
                // (35,14): error CS0030: Cannot convert type 'short?' to 'void*'
                //         pv = (void*)s;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)s").WithArguments("short?", "void*"),
                // (36,14): error CS0030: Cannot convert type 'ushort?' to 'void*'
                //         pv = (void*)us;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)us").WithArguments("ushort?", "void*"),
                // (37,14): error CS0030: Cannot convert type 'int?' to 'void*'
                //         pv = (void*)i;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)i").WithArguments("int?", "void*"),
                // (38,14): error CS0030: Cannot convert type 'uint?' to 'void*'
                //         pv = (void*)ui;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)ui").WithArguments("uint?", "void*"),
                // (39,14): error CS0030: Cannot convert type 'long?' to 'void*'
                //         pv = (void*)l;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)l").WithArguments("long?", "void*"),
                // (40,14): error CS0030: Cannot convert type 'ulong?' to 'void*'
                //         pv = (void*)ul;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(void*)ul").WithArguments("ulong?", "void*"));
        }

        [Fact]
        public void PointerArrayConversion()
        {
            var text = @"
using System;

unsafe class C
{
    void M(int*[] api, void*[] apv, Array a)
    {
        a = api;
        a.GetValue(0); //runtime error
        a = apv;
        a.GetValue(0); //runtime error

        api = a; //CS0266
        apv = a; //CS0266

        api = (int*[])a;
        apv = (void*[])a;

        apv = api; //CS0029
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (13,15): error CS0266: Cannot implicitly convert type 'System.Array' to 'int*[]'. An explicit conversion exists (are you missing a cast?)
                //         api = a; //CS0266
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a").WithArguments("System.Array", "int*[]"),
                // (14,15): error CS0266: Cannot implicitly convert type 'System.Array' to 'void*[]'. An explicit conversion exists (are you missing a cast?)
                //         apv = a; //CS0266
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a").WithArguments("System.Array", "void*[]"),
                // (19,15): error CS0029: Cannot implicitly convert type 'int*[]' to 'void*[]'
                //         apv = api; //CS0029
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "api").WithArguments("int*[]", "void*[]"));
        }

        [Fact]
        public void PointerArrayToListConversion()
        {
            var text = @"
using System.Collections.Generic;

unsafe class C
{
    void M(int*[] api, void*[] apv)
    {
        To(api);
        To(apv);

        api = From(api[0]);
        apv = From(apv[0]);
    }

    void To<T>(IList<T> list)
    {
    }

    IList<T> From<T>(T t)
    {
        return null;
    }
}
";

            // NOTE: dev10 also reports some rather silly cascading CS0266s.
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,9): error CS0306: The type 'int*' may not be used as a type argument
                //         To(api);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "To").WithArguments("int*"),
                // (9,9): error CS0306: The type 'void*' may not be used as a type argument
                //         To(apv);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "To").WithArguments("void*"),
                // (11,15): error CS0306: The type 'int*' may not be used as a type argument
                //         api = From(api[0]);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "From").WithArguments("int*"),
                // (12,15): error CS0306: The type 'void*' may not be used as a type argument
                //         apv = From(apv[0]);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "From").WithArguments("void*"));
        }

        [Fact]
        public void PointerArrayToEnumerableConversion()
        {
            var text = @"
using System.Collections;

unsafe class C
{
    void M(int*[] api, void*[] apv)
    {
        IEnumerable e = api;
        e = apv;
    }
}
";

            // NOTE: as in Dev10, there's a runtime error if you try to access an element.
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        #endregion Pointer conversion tests

        #region Pointer arithmetic tests

        [Fact]
        public void PointerArithmetic_LegalNumeric()
        {
            var text = @"
unsafe class C
{
    void M(byte* p, int i, uint ui, long l, ulong ul)
    {
        p = p + i;
        p = i + p;
        p = p - i;

        p = p + ui;
        p = ui + p;
        p = p - ui;

        p = p + l;
        p = l + p;
        p = p - l;
        
        p = p + ul;
        p = ul + p;
        p = p - ul;
    }
}
";

            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            compilation.VerifyDiagnostics();

            var methodSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");
            var pointerType = methodSymbol.Parameters[0].Type.TypeSymbol;
            Assert.Equal(TypeKind.Pointer, pointerType.TypeKind);

            foreach (var binOpSyntax in tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>())
            {
                var summary = model.GetSemanticInfoSummary(binOpSyntax);

                if (binOpSyntax.Kind() == SyntaxKind.SimpleAssignmentExpression)
                {
                    Assert.Null(summary.Symbol);
                }
                else
                {
                    Assert.NotNull(summary.Symbol);
                    Assert.Equal(MethodKind.BuiltinOperator, ((MethodSymbol)summary.Symbol).MethodKind);
                }

                Assert.Equal(0, summary.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, summary.CandidateReason);
                Assert.Equal(pointerType, summary.Type);
                Assert.Equal(pointerType, summary.ConvertedType);
                Assert.Equal(Conversion.Identity, summary.ImplicitConversion);
                Assert.Equal(0, summary.MethodGroup.Length);
                Assert.Null(summary.Alias);
                Assert.False(summary.IsCompileTimeConstant);
                Assert.False(summary.ConstantValue.HasValue);
            }
        }

        [Fact]
        public void PointerArithmetic_LegalPointer()
        {
            var text = @"
unsafe class C
{
    void M(byte* p)
    {
        var diff = p - p;
    }
}
";

            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            compilation.VerifyDiagnostics();

            foreach (var binOpSyntax in tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>())
            {
                var summary = model.GetSemanticInfoSummary(binOpSyntax);
                Assert.Equal("System.Int64 System.Byte*.op_Subtraction(System.Byte* left, System.Byte* right)", summary.Symbol.ToTestDisplayString());
                Assert.Equal(0, summary.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, summary.CandidateReason);
                Assert.Equal(SpecialType.System_Int64, summary.Type.SpecialType);
                Assert.Equal(SpecialType.System_Int64, summary.ConvertedType.SpecialType);
                Assert.Equal(Conversion.Identity, summary.ImplicitConversion);
                Assert.Equal(0, summary.MethodGroup.Length);
                Assert.Null(summary.Alias);
                Assert.False(summary.IsCompileTimeConstant);
                Assert.False(summary.ConstantValue.HasValue);
            }
        }

        [Fact]
        public void PointerArithmetic_IllegalNumericSubtraction()
        {
            var text = @"
unsafe class C
{
    void M(byte* p, int i, uint ui, long l, ulong ul)
    {
        p = i - p;
        p = ui - p;
        p = l - p;
        p = ul - p;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,13): error CS0019: Operator '-' cannot be applied to operands of type 'int' and 'byte*'
                //         p = i - p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i - p").WithArguments("-", "int", "byte*"),
                // (7,13): error CS0019: Operator '-' cannot be applied to operands of type 'uint' and 'byte*'
                //         p = ui - p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "ui - p").WithArguments("-", "uint", "byte*"),
                // (8,13): error CS0019: Operator '-' cannot be applied to operands of type 'long' and 'byte*'
                //         p = l - p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "l - p").WithArguments("-", "long", "byte*"),
                // (9,13): error CS0019: Operator '-' cannot be applied to operands of type 'ulong' and 'byte*'
                //         p = ul - p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "ul - p").WithArguments("-", "ulong", "byte*"));
        }

        [Fact]
        public void PointerArithmetic_IllegalPointerSubtraction()
        {
            var text = @"
unsafe class C
{
    void M(byte* b, int* i, byte** bb, int** ii)
    {
        long l;

        l = b - i;
        l = b - bb;
        l = b - ii;

        l = i - b;
        l = i - bb;
        l = i - ii;

        l = bb - b;
        l = bb - i;
        l = bb - ii;

        l = ii - b;
        l = ii - i;
        l = ii - bb;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,13): error CS0019: Operator '-' cannot be applied to operands of type 'byte*' and 'int*'
                //         l = b - i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "b - i").WithArguments("-", "byte*", "int*"),
                // (9,13): error CS0019: Operator '-' cannot be applied to operands of type 'byte*' and 'byte**'
                //         l = b - bb;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "b - bb").WithArguments("-", "byte*", "byte**"),
                // (10,13): error CS0019: Operator '-' cannot be applied to operands of type 'byte*' and 'int**'
                //         l = b - ii;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "b - ii").WithArguments("-", "byte*", "int**"),
                // (12,13): error CS0019: Operator '-' cannot be applied to operands of type 'int*' and 'byte*'
                //         l = i - b;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i - b").WithArguments("-", "int*", "byte*"),
                // (13,13): error CS0019: Operator '-' cannot be applied to operands of type 'int*' and 'byte**'
                //         l = i - bb;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i - bb").WithArguments("-", "int*", "byte**"),
                // (14,13): error CS0019: Operator '-' cannot be applied to operands of type 'int*' and 'int**'
                //         l = i - ii;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i - ii").WithArguments("-", "int*", "int**"),
                // (16,13): error CS0019: Operator '-' cannot be applied to operands of type 'byte**' and 'byte*'
                //         l = bb - b;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "bb - b").WithArguments("-", "byte**", "byte*"),
                // (17,13): error CS0019: Operator '-' cannot be applied to operands of type 'byte**' and 'int*'
                //         l = bb - i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "bb - i").WithArguments("-", "byte**", "int*"),
                // (18,13): error CS0019: Operator '-' cannot be applied to operands of type 'byte**' and 'int**'
                //         l = bb - ii;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "bb - ii").WithArguments("-", "byte**", "int**"),
                // (20,13): error CS0019: Operator '-' cannot be applied to operands of type 'int**' and 'byte*'
                //         l = ii - b;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "ii - b").WithArguments("-", "int**", "byte*"),
                // (21,13): error CS0019: Operator '-' cannot be applied to operands of type 'int**' and 'int*'
                //         l = ii - i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "ii - i").WithArguments("-", "int**", "int*"),
                // (22,13): error CS0019: Operator '-' cannot be applied to operands of type 'int**' and 'byte**'
                //         l = ii - bb;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "ii - bb").WithArguments("-", "int**", "byte**"));
        }

        [Fact]
        public void PointerArithmetic_OtherOperators()
        {
            var text = @"
unsafe class C
{
    void M(byte* p, int i)
    {
        var r01 = p * i;
        var r02 = i * p;
        var r03 = p / i;
        var r04 = i / p;
        var r05 = p % i;
        var r06 = i % p;
        var r07 = p << i;
        var r08 = i << p;
        var r09 = p >> i;
        var r10 = i >> p;
        var r11 = p & i;
        var r12 = i & p;
        var r13 = p | i;
        var r14 = i | p;
        var r15 = p ^ i;
        var r16 = i ^ p;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,19): error CS0019: Operator '*' cannot be applied to operands of type 'byte*' and 'int'
                //         var r01 = p * i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p * i").WithArguments("*", "byte*", "int"),
                // (7,19): error CS0019: Operator '*' cannot be applied to operands of type 'int' and 'byte*'
                //         var r02 = i * p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i * p").WithArguments("*", "int", "byte*"),
                // (8,19): error CS0019: Operator '/' cannot be applied to operands of type 'byte*' and 'int'
                //         var r03 = p / i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p / i").WithArguments("/", "byte*", "int"),
                // (9,19): error CS0019: Operator '/' cannot be applied to operands of type 'int' and 'byte*'
                //         var r04 = i / p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i / p").WithArguments("/", "int", "byte*"),
                // (10,19): error CS0019: Operator '%' cannot be applied to operands of type 'byte*' and 'int'
                //         var r05 = p % i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p % i").WithArguments("%", "byte*", "int"),
                // (11,19): error CS0019: Operator '%' cannot be applied to operands of type 'int' and 'byte*'
                //         var r06 = i % p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i % p").WithArguments("%", "int", "byte*"),
                // (12,19): error CS0019: Operator '<<' cannot be applied to operands of type 'byte*' and 'int'
                //         var r07 = p << i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p << i").WithArguments("<<", "byte*", "int"),
                // (13,19): error CS0019: Operator '<<' cannot be applied to operands of type 'int' and 'byte*'
                //         var r08 = i << p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i << p").WithArguments("<<", "int", "byte*"),
                // (14,19): error CS0019: Operator '>>' cannot be applied to operands of type 'byte*' and 'int'
                //         var r09 = p >> i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p >> i").WithArguments(">>", "byte*", "int"),
                // (15,19): error CS0019: Operator '>>' cannot be applied to operands of type 'int' and 'byte*'
                //         var r10 = i >> p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i >> p").WithArguments(">>", "int", "byte*"),
                // (16,19): error CS0019: Operator '&' cannot be applied to operands of type 'byte*' and 'int'
                //         var r11 = p & i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p & i").WithArguments("&", "byte*", "int"),
                // (17,19): error CS0019: Operator '&' cannot be applied to operands of type 'int' and 'byte*'
                //         var r12 = i & p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i & p").WithArguments("&", "int", "byte*"),
                // (18,19): error CS0019: Operator '|' cannot be applied to operands of type 'byte*' and 'int'
                //         var r13 = p | i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p | i").WithArguments("|", "byte*", "int"),
                // (19,19): error CS0019: Operator '|' cannot be applied to operands of type 'int' and 'byte*'
                //         var r14 = i | p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i | p").WithArguments("|", "int", "byte*"),
                // (20,19): error CS0019: Operator '^' cannot be applied to operands of type 'byte*' and 'int'
                //         var r15 = p ^ i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p ^ i").WithArguments("^", "byte*", "int"),
                // (21,19): error CS0019: Operator '^' cannot be applied to operands of type 'int' and 'byte*'
                //         var r16 = i ^ p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i ^ p").WithArguments("^", "int", "byte*"));
        }

        [Fact]
        public void PointerArithmetic_NumericWidening()
        {
            var text = @"
unsafe class C
{
    void M(int* p, sbyte sb, byte b, short s, ushort us)
    {
        p = p + sb;
        p = p + b;
        p = p + s;
        p = p + us;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void PointerArithmetic_NumericUDC()
        {
            var text = @"
unsafe class C
{
    void M(int* p)
    {
        p = p + this;
    }

    public static implicit operator int(C c)
    {
        return 0;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void PointerArithmetic_Nullable()
        {
            var text = @"
unsafe class C
{
    void M(int* p, int? i)
    {
        p = p + i;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,13): error CS0019: Operator '+' cannot be applied to operands of type 'int*' and 'int?'
                //         p = p + i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p + i").WithArguments("+", "int*", "int?"));
        }

        [Fact]
        public void PointerArithmetic_Compound()
        {
            var text = @"
unsafe class C
{
    void M(int* p, int i)
    {
        p++;
        ++p;
        p--;
        --p;
        p += i;
        p -= i;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void PointerArithmetic_VoidPointer()
        {
            var text = @"
unsafe class C
{
    void M(void* p)
    {
        var diff = p - p;
        p = p + 1;
        p = p - 1;
        p = 1 + p;
        p = 1 - p;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,20): error CS0242: The operation in question is undefined on void pointers
                //         var diff = p - p;
                Diagnostic(ErrorCode.ERR_VoidError, "p - p"),
                // (7,13): error CS0242: The operation in question is undefined on void pointers
                //         p = p + 1;
                Diagnostic(ErrorCode.ERR_VoidError, "p + 1"),
                // (8,13): error CS0242: The operation in question is undefined on void pointers
                //         p = p - 1;
                Diagnostic(ErrorCode.ERR_VoidError, "p - 1"),
                // (9,13): error CS0242: The operation in question is undefined on void pointers
                //         p = 1 + p;
                Diagnostic(ErrorCode.ERR_VoidError, "1 + p"),
                // (10,13): error CS0242: The operation in question is undefined on void pointers
                //         p = 1 - p;
                Diagnostic(ErrorCode.ERR_VoidError, "1 - p"),
                // (10,13): error CS0019: Operator '-' cannot be applied to operands of type 'int' and 'void*'
                //         p = 1 - p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "1 - p").WithArguments("-", "int", "void*"));
        }

        [Fact]
        public void PointerArithmetic_VoidPointerPointer()
        {
            var text = @"
unsafe class C
{
    void M(void** p) //void** is not a void pointer
    {
        var diff = p - p;
        p = p + 1;
        p = p - 1;
        p = 1 + p;
        p = 1 - p;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (10,13): error CS0019: Operator '-' cannot be applied to operands of type 'int' and 'void**'
                //         p = 1 - p;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "1 - p").WithArguments("-", "int", "void**"));
        }

        #endregion Pointer arithmetic tests

        #region Pointer comparison tests

        [WorkItem(546712, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546712")]
        [Fact]
        public void PointerComparison_Null()
        {
            // We had a bug whereby overload resolution was failing if the null
            // was on the left. This test regresses the bug.
            var text = @"
unsafe struct S
{
    bool M(byte* pb, S* ps)
    {
        return null != pb && null == ps;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void PointerComparison_Pointer()
        {
            var text = @"
unsafe class C
{
    void M(byte* b, int* i, void* v)
    {
        bool result;
        byte* b2 = b;
        int* i2 = i;
        void* v2 = v;

        result = b == b2;
        result = b == i2;
        result = b == v2;

        result = i != i2;
        result = i != v2;
        result = i != b2;

        result = v <= v2;
        result = v <= b2;
        result = v <= i2;

        result = b >= b2;
        result = b >= i2;
        result = b >= v2;
        
        result = i < i2;
        result = i < v2;
        result = i < b2;
        
        result = v > v2;
        result = v > b2;
        result = v > i2;
    }
}
";

            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            compilation.VerifyDiagnostics();

            foreach (var binOpSyntax in tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>())
            {
                var summary = model.GetSemanticInfoSummary(binOpSyntax);

                if (binOpSyntax.Kind() == SyntaxKind.SimpleAssignmentExpression)
                {
                    Assert.Null(summary.Symbol);
                }
                else
                {
                    Assert.NotNull(summary.Symbol);
                    Assert.Equal(MethodKind.BuiltinOperator, ((MethodSymbol)summary.Symbol).MethodKind);
                }

                Assert.Equal(0, summary.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, summary.CandidateReason);
                Assert.Equal(SpecialType.System_Boolean, summary.Type.SpecialType);
                Assert.Equal(SpecialType.System_Boolean, summary.ConvertedType.SpecialType);
                Assert.Equal(Conversion.Identity, summary.ImplicitConversion);
                Assert.Equal(0, summary.MethodGroup.Length);
                Assert.Null(summary.Alias);
                Assert.False(summary.IsCompileTimeConstant);
                Assert.False(summary.ConstantValue.HasValue);
            }
        }

        [Fact]
        public void PointerComparison_PointerPointer()
        {
            var text = @"
unsafe class C
{
    void M(byte* b, byte** bb)
    {
        bool result;

        result = b == bb;
        result = b != bb;
        result = b <= bb;
        result = b >= bb;
        result = b < bb;
        result = b > bb;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void PointerComparison_Numeric()
        {
            var text = @"
unsafe class C
{
    void M(char* p, int i, uint ui, long l, ulong ul)
    {
        bool result;

        result = p == i;
        result = p != i;
        result = p <= i;
        result = p >= i;
        result = p < i;
        result = p > i;

        result = p == ui;
        result = p != ui;
        result = p <= ui;
        result = p >= ui;
        result = p < ui;
        result = p > ui;

        result = p == l;
        result = p != l;
        result = p <= l;
        result = p >= l;
        result = p < l;
        result = p > l;

        result = p == ul;
        result = p != ul;
        result = p <= ul;
        result = p >= ul;
        result = p < ul;
        result = p > ul;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,18): error CS0019: Operator '==' cannot be applied to operands of type 'char*' and 'int'
                //         result = p == i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p == i").WithArguments("==", "char*", "int"),
                // (9,18): error CS0019: Operator '!=' cannot be applied to operands of type 'char*' and 'int'
                //         result = p != i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p != i").WithArguments("!=", "char*", "int"),
                // (10,18): error CS0019: Operator '<=' cannot be applied to operands of type 'char*' and 'int'
                //         result = p <= i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p <= i").WithArguments("<=", "char*", "int"),
                // (11,18): error CS0019: Operator '>=' cannot be applied to operands of type 'char*' and 'int'
                //         result = p >= i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p >= i").WithArguments(">=", "char*", "int"),
                // (12,18): error CS0019: Operator '<' cannot be applied to operands of type 'char*' and 'int'
                //         result = p < i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p < i").WithArguments("<", "char*", "int"),
                // (13,18): error CS0019: Operator '>' cannot be applied to operands of type 'char*' and 'int'
                //         result = p > i;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p > i").WithArguments(">", "char*", "int"),
                // (15,18): error CS0019: Operator '==' cannot be applied to operands of type 'char*' and 'uint'
                //         result = p == ui;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p == ui").WithArguments("==", "char*", "uint"),
                // (16,18): error CS0019: Operator '!=' cannot be applied to operands of type 'char*' and 'uint'
                //         result = p != ui;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p != ui").WithArguments("!=", "char*", "uint"),
                // (17,18): error CS0019: Operator '<=' cannot be applied to operands of type 'char*' and 'uint'
                //         result = p <= ui;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p <= ui").WithArguments("<=", "char*", "uint"),
                // (18,18): error CS0019: Operator '>=' cannot be applied to operands of type 'char*' and 'uint'
                //         result = p >= ui;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p >= ui").WithArguments(">=", "char*", "uint"),
                // (19,18): error CS0019: Operator '<' cannot be applied to operands of type 'char*' and 'uint'
                //         result = p < ui;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p < ui").WithArguments("<", "char*", "uint"),
                // (20,18): error CS0019: Operator '>' cannot be applied to operands of type 'char*' and 'uint'
                //         result = p > ui;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p > ui").WithArguments(">", "char*", "uint"),
                // (22,18): error CS0019: Operator '==' cannot be applied to operands of type 'char*' and 'long'
                //         result = p == l;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p == l").WithArguments("==", "char*", "long"),
                // (23,18): error CS0019: Operator '!=' cannot be applied to operands of type 'char*' and 'long'
                //         result = p != l;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p != l").WithArguments("!=", "char*", "long"),
                // (24,18): error CS0019: Operator '<=' cannot be applied to operands of type 'char*' and 'long'
                //         result = p <= l;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p <= l").WithArguments("<=", "char*", "long"),
                // (25,18): error CS0019: Operator '>=' cannot be applied to operands of type 'char*' and 'long'
                //         result = p >= l;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p >= l").WithArguments(">=", "char*", "long"),
                // (26,18): error CS0019: Operator '<' cannot be applied to operands of type 'char*' and 'long'
                //         result = p < l;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p < l").WithArguments("<", "char*", "long"),
                // (27,18): error CS0019: Operator '>' cannot be applied to operands of type 'char*' and 'long'
                //         result = p > l;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p > l").WithArguments(">", "char*", "long"),
                // (29,18): error CS0019: Operator '==' cannot be applied to operands of type 'char*' and 'ulong'
                //         result = p == ul;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p == ul").WithArguments("==", "char*", "ulong"),
                // (30,18): error CS0019: Operator '!=' cannot be applied to operands of type 'char*' and 'ulong'
                //         result = p != ul;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p != ul").WithArguments("!=", "char*", "ulong"),
                // (31,18): error CS0019: Operator '<=' cannot be applied to operands of type 'char*' and 'ulong'
                //         result = p <= ul;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p <= ul").WithArguments("<=", "char*", "ulong"),
                // (32,18): error CS0019: Operator '>=' cannot be applied to operands of type 'char*' and 'ulong'
                //         result = p >= ul;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p >= ul").WithArguments(">=", "char*", "ulong"),
                // (33,18): error CS0019: Operator '<' cannot be applied to operands of type 'char*' and 'ulong'
                //         result = p < ul;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p < ul").WithArguments("<", "char*", "ulong"),
                // (34,18): error CS0019: Operator '>' cannot be applied to operands of type 'char*' and 'ulong'
                //         result = p > ul;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "p > ul").WithArguments(">", "char*", "ulong"));
        }

        #endregion Pointer comparison tests

        #region Fixed statement diagnostics

        [Fact]
        public void ERR_BadFixedInitType()
        {
            var text = @"
unsafe class C
{
    int x;

    void M()
    {
        fixed (int p = &x) //not a pointer
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,20): error CS0209: The type of a local declared in a fixed statement must be a pointer type
                //         fixed (int p = &x) //not a pointer
                Diagnostic(ErrorCode.ERR_BadFixedInitType, "p = &x"));
        }

        [Fact]
        public void ERR_FixedMustInit()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        fixed (int* p) //missing initializer
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,21): error CS0210: You must provide an initializer in a fixed or using statement declaration
                //         fixed (int* p) //missing initializer
                Diagnostic(ErrorCode.ERR_FixedMustInit, "p"));
        }

        [Fact]
        public void ERR_ImplicitlyTypedLocalCannotBeFixed1()
        {
            var text = @"
unsafe class C
{
    int x;

    void M()
    {
        fixed (var p = &x) //implicitly typed
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,20): error CS0821: Implicitly-typed local variables cannot be fixed
                //         fixed (var p = &x) //implicitly typed
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedLocalCannotBeFixed, "p = &x"));
        }

        [Fact]
        public void ERR_ImplicitlyTypedLocalCannotBeFixed2()
        {
            var text = @"
unsafe class C
{
    int x;

    void M()
    {
        fixed (var p = &x) //not implicitly typed
        {
        }
    }
}

class var
{
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,20): error CS0209: The type of a local declared in a fixed statement must be a pointer type
                //         fixed (var p = &x) //not implicitly typed
                Diagnostic(ErrorCode.ERR_BadFixedInitType, "p = &x"));
        }

        [Fact]
        public void ERR_MultiTypeInDeclaration()
        {
            var text = @"
unsafe class C
{
    int x;

    void M()
    {
        fixed (int* p = &x, var q = p) //multiple declarations (vs declarators)
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,29): error CS1044: Cannot use more than one type in a for, using, fixed, or declaration statement
                //         fixed (int* p = &x, var q = p) //multiple declarations (vs declarators)
                Diagnostic(ErrorCode.ERR_MultiTypeInDeclaration, "var"),

                // (8,33): error CS1026: ) expected
                //         fixed (int* p = &x, var q = p) //multiple declarations (vs declarators)
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "q"),
                // (8,38): error CS1002: ; expected
                //         fixed (int* p = &x, var q = p) //multiple declarations (vs declarators)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")"),
                // (8,38): error CS1513: } expected
                //         fixed (int* p = &x, var q = p) //multiple declarations (vs declarators)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")"),
                // (8,29): error CS0210: You must provide an initializer in a fixed or using statement declaration
                //         fixed (int* p = &x, var q = p) //multiple declarations (vs declarators)
                Diagnostic(ErrorCode.ERR_FixedMustInit, "var"),
                // (8,33): error CS0103: The name 'q' does not exist in the current context
                //         fixed (int* p = &x, var q = p) //multiple declarations (vs declarators)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "q").WithArguments("q"));
        }

        [Fact]
        public void ERR_BadCastInFixed_String()
        {
            var text = @"
unsafe class C
{
    public NotString n;

    void M()
    {
        fixed (char* p = (string)""hello"")
        {
        }

        fixed (char* p = (string)n)
        {
        }
    }
}

class NotString
{
    unsafe public static implicit operator string(NotString n)
    {
        return null;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,22): warning CS0649: Field 'C.n' is never assigned to, and will always have its default value null
                //     public NotString n;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "n").WithArguments("C.n", "null"));
        }

        [Fact]
        public void ERR_BadCastInFixed_Array()
        {
            var text = @"
unsafe class C
{
    public NotArray n;

    void M()
    {
        fixed (byte* p = (byte[])new byte[0])
        {
        }

        fixed (int* p = (int[])n)
        {
        }
    }
}

class NotArray
{
    unsafe public static implicit operator int[](NotArray n)
    {
        return null;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,21): warning CS0649: Field 'C.n' is never assigned to, and will always have its default value null
                //     public NotArray n;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "n").WithArguments("C.n", "null"));
        }

        [Fact]
        public void ERR_BadCastInFixed_Pointer()
        {
            var text = @"
unsafe class C
{
    public byte x;
    public NotPointer n;

    void M()
    {
        fixed (byte* p = (byte*)&x)
        {
        }

        fixed (int* p = n) //CS0213 (confusing, but matches dev10)
        {
        }

        fixed (int* p = (int*)n)
        {
        }
    }
}

class NotPointer
{
    unsafe public static implicit operator int*(NotPointer n)
    {
        return null;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,26): error CS0254: The right hand side of a fixed statement assignment may not be a cast expression
                //         fixed (byte* p = (byte*)&x)
                Diagnostic(ErrorCode.ERR_BadCastInFixed, "(byte*)&x"),
                // (13,25): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                //         fixed (int* p = n) //CS0213 (confusing, but matches dev10)
                Diagnostic(ErrorCode.ERR_FixedNotNeeded, "n"),
                // (17,25): error CS0254: The right hand side of a fixed statement assignment may not be a cast expression
                //         fixed (int* p = (int*)n)
                Diagnostic(ErrorCode.ERR_BadCastInFixed, "(int*)n"),
                // (5,16): warning CS0649: Field 'C.n' is never assigned to, and will always have its default value null
                //     NotPointer n;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "n").WithArguments("C.n", "null"));
        }

        [Fact]
        public void ERR_FixedLocalInLambda()
        {
            var text = @"
using System;

unsafe class C
{
    char c = 'a';
    char[] a = new char[1];

    static void Main()
    {
        C c = new C();
        fixed (char* p = &c.c, q = c.a, r = ""hello"")
        {
            System.Action a;
            a = () => Console.WriteLine(*p);
            a = () => Console.WriteLine(*q);
            a = () => Console.WriteLine(*r);
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (15,42): error CS1764: Cannot use fixed local 'p' inside an anonymous method, lambda expression, or query expression
                //             a = () => Console.WriteLine(*p);
                Diagnostic(ErrorCode.ERR_FixedLocalInLambda, "p").WithArguments("p"),
                // (16,42): error CS1764: Cannot use fixed local 'q' inside an anonymous method, lambda expression, or query expression
                //             a = () => Console.WriteLine(*q);
                Diagnostic(ErrorCode.ERR_FixedLocalInLambda, "q").WithArguments("q"),
                // (17,42): error CS1764: Cannot use fixed local 'r' inside an anonymous method, lambda expression, or query expression
                //             a = () => Console.WriteLine(*r);
                Diagnostic(ErrorCode.ERR_FixedLocalInLambda, "r").WithArguments("r"));
        }

        [Fact]
        public void NormalAddressOfInFixedStatement()
        {
            var text = @"
class Program
{
    int x;
    int[] a;

    unsafe static void Main()
    {
        Program p = new Program();
        int q;
        fixed (int* px = (&(p.a[*(&q)]))) //fine
        {
        }

        fixed (int* px = &(p.a[*(&p.x)])) //CS0212
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (15,34): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         fixed (int* px = &(p.a[*(&p.x)])) //CS0212
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&p.x"),
                // (5,11): warning CS0649: Field 'Program.a' is never assigned to, and will always have its default value null
                //     int[] a;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "a").WithArguments("Program.a", "null"));
        }

        [Fact]
        public void StackAllocInFixedStatement()
        {
            var text = @"
class Program
{
    unsafe static void Main()
    {
        fixed (int* p = stackalloc int[2]) //CS0213 - already fixed
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,25): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                //         fixed (int* p = stackalloc int[2])
                Diagnostic(ErrorCode.ERR_FixedNotNeeded, "stackalloc int[2]"));
        }

        [Fact]
        public void FixedInitializerRefersToPreviousVariable()
        {
            var text = @"
unsafe class C
{
    int f;
    int[] a;

    void Foo()
    {
        fixed (int* q = &f, r = &q[1]) //CS0213
        {
        }

        fixed (int* q = &f, r = &a[*q]) //fine
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,33): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                //         fixed (int* q = &f, r = &q[1]) //CS0213
                Diagnostic(ErrorCode.ERR_FixedNotNeeded, "&q[1]"),

                // (5,11): warning CS0649: Field 'C.a' is never assigned to, and will always have its default value null
                //     int[] a;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "a").WithArguments("C.a", "null"));
        }

        [Fact]
        public void NormalInitializerType_Null()
        {
            var text = @"
class Program
{
    unsafe static void Main()
    {
        fixed (int* p = null)
        {
        }
    }
}
";
            // Confusing, but matches Dev10.
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,25): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                //         fixed (int* p = null)
                Diagnostic(ErrorCode.ERR_FixedNotNeeded, "null"));
        }

        [Fact]
        public void NormalInitializerType_Lambda()
        {
            var text = @"
class Program
{
    unsafe static void Main()
    {
        fixed (int* p = (x => x))
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,26): error CS1660: Cannot convert lambda expression to type 'int*' because it is not a delegate type
                //         fixed (int* p = (x => x))
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "x => x").WithArguments("lambda expression", "int*"));
        }

        [Fact]
        public void NormalInitializerType_MethodGroup()
        {
            var text = @"
class Program
{
    unsafe static void Main()
    {
        fixed (int* p = Main)
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,25): error CS0428: Cannot convert method group 'Main' to non-delegate type 'int*'. Did you intend to invoke the method?
                //         fixed (int* p = Main)
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "int*"));
        }

        [Fact]
        public void NormalInitializerType_String()
        {
            var text = @"
class Program
{
    unsafe static void Main()
    {
        string s = ""hello"";

        fixed (char* p = s) //fine
        {
        }

        fixed (void* p = s) //fine
        {
        }

        fixed (int* p = s) //can't convert char* to int*
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (16,25): error CS0266: Cannot implicitly convert type 'char*' to 'int*'. An explicit conversion exists (are you missing a cast?)
                //         fixed (int* p = s) //can't convert char* to int*
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "s").WithArguments("char*", "int*"));
        }

        [Fact]
        public void NormalInitializerType_ArrayOfManaged()
        {
            var text = @"
class Program
{
    unsafe static void Main()
    {
        string[] a = new string[2];

        fixed (void* p = a) //string* is not a valid type
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,26): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('string')
                //         fixed (void* p = a)
                Diagnostic(ErrorCode.ERR_ManagedAddr, "a").WithArguments("string"));
        }

        [Fact]
        public void NormalInitializerType_Array()
        {
            var text = @"
class Program
{
    unsafe static void Main()
    {
        char[] a = new char[2];

        fixed (char* p = a) //fine
        {
        }

        fixed (void* p = a) //fine
        {
        }

        fixed (int* p = a) //can't convert char* to int*
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (16,25): error CS0266: Cannot implicitly convert type 'char*' to 'int*'. An explicit conversion exists (are you missing a cast?)
                //         fixed (int* p = a) //can't convert char* to int*
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a").WithArguments("char*", "int*"));
        }

        [Fact]
        public void NormalInitializerType_MultiDimensionalArray()
        {
            var text = @"
class Program
{
    unsafe static void Main()
    {
        char[,] a = new char[2,2];

        fixed (char* p = a) //fine
        {
        }

        fixed (void* p = a) //fine
        {
        }

        fixed (int* p = a) //can't convert char* to int*
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (16,25): error CS0266: Cannot implicitly convert type 'char*' to 'int*'. An explicit conversion exists (are you missing a cast?)
                //         fixed (int* p = a) //can't convert char* to int*
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a").WithArguments("char*", "int*"));
        }

        [Fact]
        public void NormalInitializerType_JaggedArray()
        {
            var text = @"
class Program
{
    unsafe static void Main()
    {
        char[][] a = new char[2][];

        fixed (void* p = a) //char[]* is not a valid type
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,26): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('char[]')
                //         fixed (void* p = a) //char[]* is not a valid type
                Diagnostic(ErrorCode.ERR_ManagedAddr, "a").WithArguments("char[]"));
        }

        #endregion Fixed statement diagnostics

        #region Fixed statement semantic model tests

        [Fact]
        public void FixedSemanticModelDeclaredSymbols()
        {
            var text = @"
using System;

unsafe class C
{
    char c = 'a';
    char[] a = new char[1];

    static void Main()
    {
        C c = new C();
        fixed (char* p = &c.c, q = c.a, r = ""hello"")
        {
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var declarators = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Reverse().Take(3).Reverse().ToArray();
            var declaredSymbols = declarators.Select(syntax => (LocalSymbol)model.GetDeclaredSymbol(syntax)).ToArray();

            foreach (var symbol in declaredSymbols)
            {
                Assert.NotNull(symbol);
                Assert.Equal(LocalDeclarationKind.FixedVariable, symbol.DeclarationKind);
                TypeSymbol type = symbol.Type.TypeSymbol;
                Assert.Equal(TypeKind.Pointer, type.TypeKind);
                Assert.Equal(SpecialType.System_Char, ((PointerTypeSymbol)type).PointedAtType.SpecialType);
            }
        }

        [Fact]
        public void FixedSemanticModelSymbolInfo()
        {
            var text = @"
using System;

unsafe class C
{
    char c = 'a';
    char[] a = new char[1];

    static void Main()
    {
        C c = new C();
        fixed (char* p = &c.c, q = c.a, r = ""hello"")
        {
            Console.WriteLine(*p);
            Console.WriteLine(*q);
            Console.WriteLine(*r);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var stringSymbol = compilation.GetSpecialType(SpecialType.System_String);
            var charSymbol = compilation.GetSpecialType(SpecialType.System_Char);
            var charPointerSymbol = new PointerTypeSymbol(TypeSymbolWithAnnotations.Create(charSymbol));

            const int numSymbols = 3;
            var declarators = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Reverse().Take(numSymbols).Reverse().ToArray();
            var dereferences = tree.GetCompilationUnitRoot().DescendantNodes().Where(syntax => syntax.IsKind(SyntaxKind.PointerIndirectionExpression)).ToArray();
            Assert.Equal(numSymbols, dereferences.Length);

            var declaredSymbols = declarators.Select(syntax => (LocalSymbol)model.GetDeclaredSymbol(syntax)).ToArray();

            var initializerSummaries = declarators.Select(syntax => model.GetSemanticInfoSummary(syntax.Initializer.Value)).ToArray();

            for (int i = 0; i < numSymbols; i++)
            {
                var summary = initializerSummaries[i];
                Assert.Equal(0, summary.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, summary.CandidateReason);
                Assert.Equal(summary.Type, summary.ConvertedType);
                Assert.Equal(Conversion.Identity, summary.ImplicitConversion);
                Assert.Equal(0, summary.MethodGroup.Length);
                Assert.Null(summary.Alias);
            }

            var summary0 = initializerSummaries[0];
            Assert.Null(summary0.Symbol);
            Assert.Equal(charPointerSymbol, summary0.Type);

            var summary1 = initializerSummaries[1];
            var arraySymbol = compilation.GlobalNamespace.GetMember<TypeSymbol>("C").GetMember<FieldSymbol>("a");
            Assert.Equal(arraySymbol, summary1.Symbol);
            Assert.Equal(arraySymbol.Type.TypeSymbol, summary1.Type);

            var summary2 = initializerSummaries[2];
            Assert.Null(summary2.Symbol);
            Assert.Equal(stringSymbol, summary2.Type);

            var accessSymbolInfos = dereferences.Select(syntax => model.GetSymbolInfo(((PrefixUnaryExpressionSyntax)syntax).Operand)).ToArray();

            for (int i = 0; i < numSymbols; i++)
            {
                SymbolInfo info = accessSymbolInfos[i];
                Assert.Equal(declaredSymbols[i], info.Symbol);
                Assert.Equal(0, info.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, info.CandidateReason);
            }
        }

        [Fact]
        public void FixedSemanticModelSymbolInfoConversions()
        {
            var text = @"
using System;

unsafe class C
{
    char c = 'a';
    char[] a = new char[1];

    static void Main()
    {
        C c = new C();
        fixed (void* p = &c.c, q = c.a, r = ""hello"")
        {
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var stringSymbol = compilation.GetSpecialType(SpecialType.System_String);
            var charSymbol = compilation.GetSpecialType(SpecialType.System_Char);
            var charPointerSymbol = new PointerTypeSymbol(TypeSymbolWithAnnotations.Create(charSymbol));
            var voidSymbol = compilation.GetSpecialType(SpecialType.System_Void);
            var voidPointerSymbol = new PointerTypeSymbol(TypeSymbolWithAnnotations.Create(voidSymbol));

            const int numSymbols = 3;
            var declarators = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Reverse().Take(numSymbols).Reverse().ToArray();
            var initializerSummaries = declarators.Select(syntax => model.GetSemanticInfoSummary(syntax.Initializer.Value)).ToArray();

            for (int i = 0; i < numSymbols; i++)
            {
                var summary = initializerSummaries[i];
                Assert.Equal(0, summary.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, summary.CandidateReason);
                Assert.Equal(0, summary.MethodGroup.Length);
                Assert.Null(summary.Alias);
            }

            var summary0 = initializerSummaries[0];
            Assert.Null(summary0.Symbol);
            Assert.Equal(charPointerSymbol, summary0.Type);
            Assert.Equal(voidPointerSymbol, summary0.ConvertedType);
            Assert.Equal(Conversion.PointerToVoid, summary0.ImplicitConversion);

            var summary1 = initializerSummaries[1];
            var arraySymbol = compilation.GlobalNamespace.GetMember<TypeSymbol>("C").GetMember<FieldSymbol>("a");
            Assert.Equal(arraySymbol, summary1.Symbol);
            Assert.Equal(arraySymbol.Type.TypeSymbol, summary1.Type);
            Assert.Equal(summary1.Type, summary1.ConvertedType);
            Assert.Equal(Conversion.Identity, summary1.ImplicitConversion);

            var summary2 = initializerSummaries[2];
            Assert.Null(summary2.Symbol);
            Assert.Equal(stringSymbol, summary2.Type);
            Assert.Equal(summary2.Type, summary2.ConvertedType);
            Assert.Equal(Conversion.Identity, summary2.ImplicitConversion);
        }

        #endregion Fixed statement semantic model tests

        #region sizeof diagnostic tests

        [Fact]
        public void SizeOfManaged()
        {
            var text = @"
unsafe class C
{
    void M<T>(T t)
    {
        int x;
        x = sizeof(T); //CS0208
        x = sizeof(C); //CS0208
        x = sizeof(S); //CS0208
    }
}

public struct S
{
    public string s;
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,13): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('T')
                //         x = sizeof(T); //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "sizeof(T)").WithArguments("T"),
                // (8,13): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('C')
                //         x = sizeof(C); //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "sizeof(C)").WithArguments("C"),
                // (9,13): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('S')
                //         x = sizeof(S); //CS0208
                Diagnostic(ErrorCode.ERR_ManagedAddr, "sizeof(S)").WithArguments("S"));
        }

        [Fact]
        public void SizeOfUnsafe1()
        {
            var template = @"
{0} struct S
{{
    {1} void M()
    {{
        int x;
        x = sizeof(S); // Type isn't unsafe, but expression is.
    }}
}}
";
            CompareUnsafeDiagnostics(template,
                // (7,13): error CS0233: 'S' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
                //         x = sizeof(S);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(S)").WithArguments("S"));
        }

        [Fact]
        public void SizeOfUnsafe2()
        {
            var template = @"
{0} class C
{{
    {1} void M()
    {{
        int x;
        x = sizeof(int*);
        x = sizeof(int**);
        x = sizeof(void*);
        x = sizeof(void**);
    }}
}}
";
            // CONSIDER: Dev10 reports ERR_SizeofUnsafe for each sizeof, but that seems redundant.
            CompareUnsafeDiagnostics(template,
                // (7,20): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         x = sizeof(int*);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (7,13): error CS0233: 'int*' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
                //         x = sizeof(int*);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(int*)").WithArguments("int*"),
                // (8,20): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         x = sizeof(int**);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (8,20): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         x = sizeof(int**);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int**"),
                // (8,13): error CS0233: 'int**' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
                //         x = sizeof(int**);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(int**)").WithArguments("int**"),
                // (9,20): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         x = sizeof(void*);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "void*"),
                // (9,13): error CS0233: 'void*' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
                //         x = sizeof(void*);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(void*)").WithArguments("void*"),
                // (10,20): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         x = sizeof(void**);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "void*"),
                // (10,20): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         x = sizeof(void**);
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "void**"),
                // (10,13): error CS0233: 'void**' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
                //         x = sizeof(void**);
                Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(void**)").WithArguments("void**")
                );
        }

        [Fact]
        public void SizeOfUnsafeInIterator()
        {
            var text = @"
struct S
{
    System.Collections.Generic.IEnumerable<int> M()
    {
        yield return sizeof(S);
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,22): error CS1629: Unsafe code may not appear in iterators
                //         yield return sizeof(S);
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "sizeof(S)"));
        }

        [Fact]
        public void SizeOfNonType1()
        {
            var text = @"
unsafe struct S
{
    void M()
    {
        S s = new S();
        int i = 0;

        i = sizeof(s);
        i = sizeof(i);
        i = sizeof(M);
    }
}
";
            // Not identical to Dev10, but same meaning.
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,20): error CS0118: 's' is a variable but is used like a type
                //         i = sizeof(s);
                Diagnostic(ErrorCode.ERR_BadSKknown, "s").WithArguments("s", "variable", "type"),
                // (10,20): error CS0118: 'i' is a variable but is used like a type
                //         i = sizeof(i);
                Diagnostic(ErrorCode.ERR_BadSKknown, "i").WithArguments("i", "variable", "type"),
                // (11,20): error CS0246: The type or namespace name 'M' could not be found (are you missing a using directive or an assembly reference?)
                //         i = sizeof(M);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "M").WithArguments("M"),
                // (6,11): warning CS0219: The variable 's' is assigned but its value is never used
                //         S s = new S();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s"));
        }

        [Fact]
        public void SizeOfNonType2()
        {
            var text = @"
unsafe struct S
{
    void M()
    {
        int i;
        i = sizeof(void);
        i = sizeof(this); //parser error
        i = sizeof(x => x); //parser error
    }
}
";
            // Not identical to Dev10, but same meaning.
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,20): error CS1547: Keyword 'void' cannot be used in this context
                //         i = sizeof(void);
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void"),
                // (8,20): error CS1031: Type expected
                //         i = sizeof(this); //parser error
                Diagnostic(ErrorCode.ERR_TypeExpected, "this"),
                // (8,20): error CS1026: ) expected
                //         i = sizeof(this); //parser error
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "this"),
                // (8,20): error CS1002: ; expected
                //         i = sizeof(this); //parser error
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "this"),
                // (8,24): error CS1002: ; expected
                //         i = sizeof(this); //parser error
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")"),
                // (8,24): error CS1513: } expected
                //         i = sizeof(this); //parser error
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")"),
                // (9,22): error CS1026: ) expected
                //         i = sizeof(x => x); //parser error
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "=>"),
                // (9,22): error CS1002: ; expected
                //         i = sizeof(x => x); //parser error
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=>"),
                // (9,22): error CS1513: } expected
                //         i = sizeof(x => x); //parser error
                Diagnostic(ErrorCode.ERR_RbraceExpected, "=>"),
                // (9,26): error CS1002: ; expected
                //         i = sizeof(x => x); //parser error
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")"),
                // (9,26): error CS1513: } expected
                //         i = sizeof(x => x); //parser error
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")"),
                // (9,20): error CS0246: The type or namespace name 'x' could not be found (are you missing a using directive or an assembly reference?)
                //         i = sizeof(x => x); //parser error
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "x").WithArguments("x"),
                // (9,25): error CS0103: The name 'x' does not exist in the current context
                //         i = sizeof(x => x); //parser error
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x"));
        }

        [Fact, WorkItem(529318, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529318")]
        public void SizeOfNull()
        {
            string text = @"
class Program
{
    int F1 = sizeof(null);
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (4,21): error CS1031: Type expected
            //     int F1 = sizeof(null);
            Diagnostic(ErrorCode.ERR_TypeExpected, "null"),
            // (4,21): error CS1026: ) expected
            //     int F1 = sizeof(null);
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "null"),
            // (4,21): error CS1003: Syntax error, ',' expected
            //     int F1 = sizeof(null);
            Diagnostic(ErrorCode.ERR_SyntaxError, "null").WithArguments(",", "null"),
            // (4,14): error CS0233: '?' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
            //     int F1 = sizeof(null);
            Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(").WithArguments("?"));
        }

        #endregion sizeof diagnostic tests

        #region sizeof semantic model tests

        private static readonly Dictionary<SpecialType, int> s_specialTypeSizeOfMap = new Dictionary<SpecialType, int>
        {
            { SpecialType.System_SByte, 1 },
            { SpecialType.System_Byte, 1 },
            { SpecialType.System_Int16, 2 },
            { SpecialType.System_UInt16, 2 },
            { SpecialType.System_Int32, 4 },
            { SpecialType.System_UInt32, 4 },
            { SpecialType.System_Int64, 8 },
            { SpecialType.System_UInt64, 8 },
            { SpecialType.System_Char, 2 },
            { SpecialType.System_Single, 4 },
            { SpecialType.System_Double, 8 },
            { SpecialType.System_Boolean, 1 },
            { SpecialType.System_Decimal, 16 },
        };

        [Fact]
        public void SizeOfSemanticModelSafe()
        {
            var text = @"
class Program
{
    static void Main()
    {
        int x;
        x = sizeof(sbyte);
        x = sizeof(byte);
        x = sizeof(short);
        x = sizeof(ushort);
        x = sizeof(int);
        x = sizeof(uint);
        x = sizeof(long);
        x = sizeof(ulong);
        x = sizeof(char);
        x = sizeof(float);
        x = sizeof(double);
        x = sizeof(bool);
        x = sizeof(decimal); //Supported by dev10, but not spec.
    }
}
";
            // NB: not unsafe
            var compilation = CreateCompilationWithMscorlib(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntaxes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SizeOfExpressionSyntax>();

            foreach (var syntax in syntaxes)
            {
                var typeSummary = model.GetSemanticInfoSummary(syntax.Type);
                var type = typeSummary.Symbol as TypeSymbol;

                Assert.NotNull(type);
                Assert.Equal(0, typeSummary.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, typeSummary.CandidateReason);
                Assert.NotEqual(SpecialType.None, type.SpecialType);
                Assert.Equal(type, typeSummary.Type);
                Assert.Equal(type, typeSummary.ConvertedType);
                Assert.Equal(Conversion.Identity, typeSummary.ImplicitConversion);
                Assert.Null(typeSummary.Alias);
                Assert.False(typeSummary.IsCompileTimeConstant);
                Assert.False(typeSummary.ConstantValue.HasValue);


                var sizeOfSummary = model.GetSemanticInfoSummary(syntax);

                Assert.Null(sizeOfSummary.Symbol);
                Assert.Equal(0, sizeOfSummary.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, sizeOfSummary.CandidateReason);
                Assert.Equal(SpecialType.System_Int32, sizeOfSummary.Type.SpecialType);
                Assert.Equal(sizeOfSummary.Type, sizeOfSummary.ConvertedType);
                Assert.Equal(Conversion.Identity, sizeOfSummary.ImplicitConversion);
                Assert.Equal(0, sizeOfSummary.MethodGroup.Length);
                Assert.Null(sizeOfSummary.Alias);
                Assert.True(sizeOfSummary.IsCompileTimeConstant);
                Assert.Equal(s_specialTypeSizeOfMap[type.SpecialType], sizeOfSummary.ConstantValue);
            }
        }

        [Fact]
        public void SizeOfSemanticModelEnum()
        {
            var text = @"
class Program
{
    static void Main()
    {
        int x;
        x = sizeof(E1);
        x = sizeof(E2);
    }
}

enum E1 : short
{
    A
}

enum E2 : long
{
    A
}
";
            // NB: not unsafe
            var compilation = CreateCompilationWithMscorlib(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntaxes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SizeOfExpressionSyntax>();

            foreach (var syntax in syntaxes)
            {
                var typeSummary = model.GetSemanticInfoSummary(syntax.Type);
                var type = typeSummary.Symbol as TypeSymbol;

                Assert.NotNull(type);
                Assert.Equal(0, typeSummary.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, typeSummary.CandidateReason);
                Assert.Equal(TypeKind.Enum, type.TypeKind);
                Assert.Equal(type, typeSummary.Type);
                Assert.Equal(type, typeSummary.ConvertedType);
                Assert.Equal(Conversion.Identity, typeSummary.ImplicitConversion);
                Assert.Null(typeSummary.Alias);
                Assert.False(typeSummary.IsCompileTimeConstant);
                Assert.False(typeSummary.ConstantValue.HasValue);


                var sizeOfSummary = model.GetSemanticInfoSummary(syntax);

                Assert.Null(sizeOfSummary.Symbol);
                Assert.Equal(0, sizeOfSummary.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, sizeOfSummary.CandidateReason);
                Assert.Equal(SpecialType.System_Int32, sizeOfSummary.Type.SpecialType);
                Assert.Equal(sizeOfSummary.Type, sizeOfSummary.ConvertedType);
                Assert.Equal(Conversion.Identity, sizeOfSummary.ImplicitConversion);
                Assert.Equal(0, sizeOfSummary.MethodGroup.Length);
                Assert.Null(sizeOfSummary.Alias);
                Assert.True(sizeOfSummary.IsCompileTimeConstant);
                Assert.Equal(s_specialTypeSizeOfMap[type.GetEnumUnderlyingType().SpecialType], sizeOfSummary.ConstantValue);
            }
        }

        [Fact]
        public void SizeOfSemanticModelUnsafe()
        {
            var text = @"
struct Outer
{
    unsafe static void Main()
    {
        int x;
        x = sizeof(Outer);
        x = sizeof(Inner);
        x = sizeof(Outer*);
        x = sizeof(Inner*);
        x = sizeof(Outer**);
        x = sizeof(Inner**);
    }

    struct Inner
    {
    }
}
";
            // NB: not unsafe
            var compilation = CreateCompilationWithMscorlib(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntaxes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SizeOfExpressionSyntax>();

            foreach (var syntax in syntaxes)
            {
                var typeSummary = model.GetSemanticInfoSummary(syntax.Type);
                var type = typeSummary.Symbol as TypeSymbol;

                Assert.NotNull(type);
                Assert.Equal(0, typeSummary.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, typeSummary.CandidateReason);
                Assert.Equal(SpecialType.None, type.SpecialType);
                Assert.Equal(type, typeSummary.Type);
                Assert.Equal(type, typeSummary.ConvertedType);
                Assert.Equal(Conversion.Identity, typeSummary.ImplicitConversion);
                Assert.Null(typeSummary.Alias);
                Assert.False(typeSummary.IsCompileTimeConstant);
                Assert.False(typeSummary.ConstantValue.HasValue);


                var sizeOfSummary = model.GetSemanticInfoSummary(syntax);

                Assert.Null(sizeOfSummary.Symbol);
                Assert.Equal(0, sizeOfSummary.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, sizeOfSummary.CandidateReason);
                Assert.Equal(SpecialType.System_Int32, sizeOfSummary.Type.SpecialType);
                Assert.Equal(sizeOfSummary.Type, sizeOfSummary.ConvertedType);
                Assert.Equal(Conversion.Identity, sizeOfSummary.ImplicitConversion);
                Assert.Equal(0, sizeOfSummary.MethodGroup.Length);
                Assert.Null(sizeOfSummary.Alias);
                Assert.False(sizeOfSummary.IsCompileTimeConstant);
                Assert.False(sizeOfSummary.ConstantValue.HasValue);
            }
        }

        #endregion sizeof semantic model tests

        #region stackalloc diagnostic tests

        [Fact]
        public void StackAllocUnsafe()
        {
            var template = @"
{0} struct S
{{
    {1} void M()
    {{
        int* p = stackalloc int[1];
    }}
}}
";
            CompareUnsafeDiagnostics(template,
                // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         int* p = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (6,18): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         int* p = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[1]")
                );
        }

        [Fact]
        public void StackAllocUnsafeInIterator()
        {
            var text = @"
struct S
{
    System.Collections.Generic.IEnumerable<int> M()
    {
        var p = stackalloc int[1];
        yield return 1;
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,17): error CS1629: Unsafe code may not appear in iterators
                //         var p = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "stackalloc int[1]"));
        }

        [Fact]
        public void ERR_NegativeStackAllocSize()
        {
            var text = @"
unsafe struct S
{
    void M()
    {
        int* p = stackalloc int[-1];
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,33): error CS0247: Cannot use a negative size with stackalloc
                //         int* p = stackalloc int[-1];
                Diagnostic(ErrorCode.ERR_NegativeStackAllocSize, "-1"));
        }

        [Fact]
        public void ERR_StackallocInCatchFinally_Catch()
        {
            var text = @"
unsafe class C
{
    int x = M(() =>
    {
        try
        {
            int* p = stackalloc int[1]; //fine
            System.Action a = () =>
            {
                try
                {
                    int* q = stackalloc int[1]; //fine
                }
                catch
                {
                    int* q = stackalloc int[1]; //CS0255
                }
            };
        }
        catch
        {
            int* p = stackalloc int[1]; //CS0255
            System.Action a = () =>
            {
                try
                {
                    int* q = stackalloc int[1]; //fine
                }
                catch
                {
                    int* q = stackalloc int[1]; //CS0255
                }
            };
        }
    });

    static int M(System.Action action)
    {
        try
        {
            int* p = stackalloc int[1]; //fine
            System.Action a = () =>
            {
                try
                {
                    int* q = stackalloc int[1]; //fine
                }
                catch
                {
                    int* q = stackalloc int[1]; //CS0255
                }
            };
        }
        catch
        {
            int* p = stackalloc int[1]; //CS0255
            System.Action a = () =>
            {
                try
                {
                    int* q = stackalloc int[1]; //fine
                }
                catch
                {
                    int* q = stackalloc int[1]; //CS0255
                }
            };
        }
        return 0;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (17,30): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* q = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"),
                // (23,22): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* p = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"),
                // (32,30): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* q = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"),
                // (51,30): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* q = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"),
                // (57,22): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* p = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"),
                // (66,30): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* q = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"));
        }

        [Fact]
        public void ERR_StackallocInCatchFinally_Finally()
        {
            var text = @"
unsafe class C
{
    int x = M(() =>
    {
        try
        {
            int* p = stackalloc int[1]; //fine
            System.Action a = () =>
            {
                try
                {
                    int* q = stackalloc int[1]; //fine
                }
                finally
                {
                    int* q = stackalloc int[1]; //CS0255
                }
            };
        }
        finally
        {
            int* p = stackalloc int[1]; //CS0255
            System.Action a = () =>
            {
                try
                {
                    int* q = stackalloc int[1]; //fine
                }
                finally
                {
                    int* q = stackalloc int[1]; //CS0255
                }
            };
        }
    });

    static int M(System.Action action)
    {
        try
        {
            int* p = stackalloc int[1]; //fine
            System.Action a = () =>
            {
                try
                {
                    int* q = stackalloc int[1]; //fine
                }
                finally
                {
                    int* q = stackalloc int[1]; //CS0255
                }
            };
        }
        finally
        {
            int* p = stackalloc int[1]; //CS0255
            System.Action a = () =>
            {
                try
                {
                    int* q = stackalloc int[1]; //fine
                }
                finally
                {
                    int* q = stackalloc int[1]; //CS0255
                }
            };
        }
        return 0;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (17,30): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* q = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"),
                // (23,22): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* p = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"),
                // (32,30): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* q = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"),
                // (51,30): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* q = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"),
                // (57,22): error CS0255: stackalloc may not be used in a catch or finally block
                //             int* p = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"),
                // (66,30): error CS0255: stackalloc may not be used in a catch or finally block
                //                     int* q = stackalloc int[1]; //CS0255
                Diagnostic(ErrorCode.ERR_StackallocInCatchFinally, "stackalloc int[1]"));
        }

        [Fact]
        public void ERR_BadStackAllocExpr()
        {
            var text = @"
unsafe class C
{
    static void Main()
    {
        int* p = stackalloc int;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,29): error CS1575: A stackalloc expression requires [] after type
                //         int* p = stackalloc int;
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int"));
        }

        [Fact]
        public void StackAllocCountType()
        {
            var text = @"
unsafe class C
{
    static void Main()
    {
        { int* p = stackalloc int[1]; } //fine
        { int* p = stackalloc int[1L]; } //CS0266 (could cast), even though constant value is fine
        { int* p = stackalloc int['c']; } //fine
        { int* p = stackalloc int[""hello""]; } // CS0029 (no conversion)
        { int* p = stackalloc int[Main]; } //CS0428 (method group conversion)
        { int* p = stackalloc int[x => x]; } //CS1660 (lambda conversion)
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,35): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         { int* p = stackalloc int[1L]; } //CS0266 (could cast), even though constant value is fine
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1L").WithArguments("long", "int"),
                // (9,35): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         { int* p = stackalloc int["hello"]; } // CS0029 (no conversion)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""hello""").WithArguments("string", "int"),
                // (10,35): error CS0428: Cannot convert method group 'Main' to non-delegate type 'int'. Did you intend to invoke the method?
                //         { int* p = stackalloc int[Main]; } //CS0428 (method group conversion)
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "int"),
                // (11,35): error CS1660: Cannot convert lambda expression to type 'int' because it is not a delegate type
                //         { int* p = stackalloc int[x => x]; } //CS1660 (lambda conversion)
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "x => x").WithArguments("lambda expression", "int"));
        }

        [Fact]
        public void StackAllocCountQuantity()
        {
            // These all give unhelpful parser errors in Dev10.  Let's see if we can do better.
            var text = @"
unsafe class C
{
    static void Main()
    {
        { int* p = stackalloc int[]; }
        { int* p = stackalloc int[1, 1]; }
        { int* p = stackalloc int[][]; }
        { int* p = stackalloc int[][1]; }
        { int* p = stackalloc int[1][]; }
        { int* p = stackalloc int[1][1]; }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,31): error CS1575: A stackalloc expression requires [] after type
                //         { int* p = stackalloc int[]; }
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int[]"),
                // (7,31): error CS1575: A stackalloc expression requires [] after type
                //         { int* p = stackalloc int[1, 1]; }
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int[1, 1]"),
                // (8,31): error CS1575: A stackalloc expression requires [] after type
                //         { int* p = stackalloc int[][]; }
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int[][]"),
                // (9,31): error CS1575: A stackalloc expression requires [] after type
                //         { int* p = stackalloc int[][1]; }
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int[][1]"),
                // (10,31): error CS1575: A stackalloc expression requires [] after type
                //         { int* p = stackalloc int[1][]; }
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int[1][]"),
                // (11,31): error CS1575: A stackalloc expression requires [] after type
                //         { int* p = stackalloc int[1][1]; }
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int[1][1]"),

                // CONSIDER: these are plausible, but not ideal.

                // (9,37): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         { int* p = stackalloc int[][1]; }
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "1"),
                // (11,38): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         { int* p = stackalloc int[1][1]; }
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "1"));
        }

        [Fact]
        public void StackAllocExplicitConversion()
        {
            var text = @"
unsafe class C
{
    static void Main()
    {
        { var p = (int*)stackalloc int[1]; }
        { var p = (void*)stackalloc int[1]; }
        { var p = (C)stackalloc int[1]; }
    }

    public static implicit operator C(int* p)
    {
        return null;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,25): error CS1525: Invalid expression term 'stackalloc'
                //         { var p = (int*)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc"),
                // (6,25): error CS1003: Syntax error, ',' expected
                //         { var p = (int*)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "stackalloc").WithArguments(",", "stackalloc"),
                // (6,36): error CS1002: ; expected
                //         { var p = (int*)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int"),
                // (6,40): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         { var p = (int*)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "1"),
                // (6,42): error CS1001: Identifier expected
                //         { var p = (int*)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";"),

                // (7,26): error CS1525: Invalid expression term 'stackalloc'
                //         { var p = (void*)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc"),
                // (7,26): error CS1003: Syntax error, ',' expected
                //         { var p = (void*)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "stackalloc").WithArguments(",", "stackalloc"),
                // (7,37): error CS1002: ; expected
                //         { var p = (void*)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int"),
                // (7,41): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         { var p = (void*)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "1"),
                // (7,43): error CS1001: Identifier expected
                //         { var p = (void*)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";"),

                // (8,22): error CS1525: Invalid expression term 'stackalloc'
                //         { var p = (C)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc"),
                // (8,22): error CS1003: Syntax error, ',' expected
                //         { var p = (C)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "stackalloc").WithArguments(",", "stackalloc"),
                // (8,33): error CS1002: ; expected
                //         { var p = (C)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int"),
                // (8,37): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         { var p = (C)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "1"),
                // (8,39): error CS1001: Identifier expected
                //         { var p = (C)stackalloc int[1]; }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";"));
        }

        [Fact]
        public void StackAllocNotExpression_FieldInitializer()
        {
            var text = @"
unsafe class C
{
    int* p = stackalloc int[1];
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,14): error CS1525: Invalid expression term 'stackalloc'
                //     int* p = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc"));
        }

        [Fact]
        public void StackAllocNotExpression_DefaultParameterValue()
        {
            var text = @"
unsafe class C
{
    void M(int* p = stackalloc int[1])
    {
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,21): error CS1525: Invalid expression term 'stackalloc'
                //     void M(int* p = stackalloc int[1])
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc"),
                // (4,21): error CS1003: Syntax error, ',' expected
                //     void M(int* p = stackalloc int[1])
                Diagnostic(ErrorCode.ERR_SyntaxError, "stackalloc").WithArguments(",", "stackalloc"),
                // (4,32): error CS1003: Syntax error, ',' expected
                //     void M(int* p = stackalloc int[1])
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",", "int"),
                // (4,36): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //     void M(int* p = stackalloc int[1])
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "1"),
                // (4,38): error CS1001: Identifier expected
                //     void M(int* p = stackalloc int[1])
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"),
                // (4,38): error CS1737: Optional parameters must appear after all required parameters
                //     void M(int* p = stackalloc int[1])
                Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ")"),
                // (4,17): error CS1750: A value of type '?' cannot be used as a default parameter
                // because there are no standard conversions to type 'int*' void M(int* p =
                //     stackalloc int[1])
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "p").WithArguments("?", "int*"));
        }

        [Fact]
        public void StackAllocNotExpression_ForLoop()
        {
            var text = @"
unsafe class C
{
    void M()
    {
        for (int* p = stackalloc int[1]; p != null; p++) //fine
        {
        }
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void StackAllocNotExpression_GlobalDeclaration()
        {
            var text = @"
unsafe int* p = stackalloc int[1];
";
            CreateCompilationWithMscorlib45(text, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (4,14): error CS1525: Invalid expression term 'stackalloc'
                //     int* p = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc"));
        }

        #endregion stackalloc diagnostic tests

        #region stackalloc semantic model tests

        [Fact]
        public void StackAllocSemanticModel()
        {
            var text = @"
class C
{
    unsafe static void Main()
    {
        const short count = 20;
        void* p = stackalloc char[count];
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var stackAllocSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<StackAllocArrayCreationExpressionSyntax>().Single();
            var arrayTypeSyntax = (ArrayTypeSyntax)stackAllocSyntax.Type;
            var typeSyntax = arrayTypeSyntax.ElementType;
            var countSyntax = arrayTypeSyntax.RankSpecifiers.Single().Sizes.Single();

            var stackAllocSummary = model.GetSemanticInfoSummary(stackAllocSyntax);
            Assert.Equal(SpecialType.System_Char, ((PointerTypeSymbol)stackAllocSummary.Type).PointedAtType.SpecialType);
            Assert.Equal(SpecialType.System_Void, ((PointerTypeSymbol)stackAllocSummary.ConvertedType).PointedAtType.SpecialType);
            Assert.Equal(Conversion.PointerToVoid, stackAllocSummary.ImplicitConversion);
            Assert.Null(stackAllocSummary.Symbol);
            Assert.Equal(0, stackAllocSummary.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, stackAllocSummary.CandidateReason);
            Assert.Equal(0, stackAllocSummary.MethodGroup.Length);
            Assert.Null(stackAllocSummary.Alias);
            Assert.False(stackAllocSummary.IsCompileTimeConstant);
            Assert.False(stackAllocSummary.ConstantValue.HasValue);

            var typeSummary = model.GetSemanticInfoSummary(typeSyntax);
            Assert.Equal(SpecialType.System_Char, typeSummary.Type.SpecialType);
            Assert.Equal(typeSummary.Type, typeSummary.ConvertedType);
            Assert.Equal(Conversion.Identity, typeSummary.ImplicitConversion);
            Assert.Equal(typeSummary.Symbol, typeSummary.Type);
            Assert.Equal(0, typeSummary.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, typeSummary.CandidateReason);
            Assert.Equal(0, typeSummary.MethodGroup.Length);
            Assert.Null(typeSummary.Alias);
            Assert.False(typeSummary.IsCompileTimeConstant);
            Assert.False(typeSummary.ConstantValue.HasValue);

            var countSummary = model.GetSemanticInfoSummary(countSyntax);
            Assert.Equal(SpecialType.System_Int16, countSummary.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, countSummary.ConvertedType.SpecialType);
            Assert.Equal(Conversion.ImplicitNumeric, countSummary.ImplicitConversion);
            var countSymbol = (LocalSymbol)countSummary.Symbol;
            Assert.Equal(countSummary.Type, countSymbol.Type.TypeSymbol);
            Assert.Equal("count", countSymbol.Name);
            Assert.Equal(0, countSummary.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, countSummary.CandidateReason);
            Assert.Equal(0, countSummary.MethodGroup.Length);
            Assert.Null(countSummary.Alias);
            Assert.True(countSummary.IsCompileTimeConstant);
            Assert.True(countSummary.ConstantValue.HasValue);
            Assert.Equal((short)20, countSummary.ConstantValue.Value);
        }

        #endregion stackalloc semantic model tests

        #region PointerTypes tests

        [WorkItem(543990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543990")]
        [Fact]
        public void PointerTypeInVolatileField()
        {
            string text = @"
unsafe class Test 
{
	static volatile int *px;
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,23): warning CS0169: The field 'Test.px' is never used
                // 	static volatile int *px;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "px").WithArguments("Test.px"));
        }

        [WorkItem(544003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544003")]
        [Fact]
        public void PointerTypesAsTypeArgs()
        {
            string text = @"
class A
{
    public class B{}
}
class C<T> : A
{
    // BREAKING: Dev10 (incorrectly) does not report ERR_ManagedAddr here.
    private static C<T*[]>.B b;
}
";
            var expected = new[]
            {
                // (8,22): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('T')
                //     private static C<T*[]>.B b;
                Diagnostic(ErrorCode.ERR_ManagedAddr, "T*").WithArguments("T"),

                // (8,30): warning CS0169: The field 'C<T>.b' is never used
                //     private static C<T*[]>.B b;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "b").WithArguments("C<T>.b")
            };

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(expected);
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expected);
        }

        [WorkItem(544003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544003")]
        [WorkItem(544232, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544232")]
        [Fact]
        public void PointerTypesAsTypeArgs2()
        {
            string text = @"
class A
{
    public class B{}
}
class C<T> : A
{
    // BREAKING: Dev10 does not report an error here because it does not look for ERR_ManagedAddr until
    // late in the binding process - at which point the type has been resolved to A.B.
    private static C<T*[]>.B b;

    // Workarounds
    private static B b1;
    private static A.B b2;

    // Dev10 and Roslyn produce the same diagnostic here.
    private static C<T*[]> c;
}
";
            var expected = new[]
            {
                // (10,22): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('T')
                //     private static C<T*[]>.B b;
                Diagnostic(ErrorCode.ERR_ManagedAddr, "T*").WithArguments("T"),
                // (17,22): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('T')
                //     private static C<T*[]> c;
                Diagnostic(ErrorCode.ERR_ManagedAddr, "T*").WithArguments("T"),

                // (10,30): warning CS0169: The field 'C<T>.b' is never used
                //     private static C<T*[]>.B b;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "b").WithArguments("C<T>.b"),
                // (13,22): warning CS0169: The field 'C<T>.b1' is never used
                //     private static B b1;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "b1").WithArguments("C<T>.b1"),
                // (14,24): warning CS0169: The field 'C<T>.b2' is never used
                //     private static A.B b2;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "b2").WithArguments("C<T>.b2"),
                // (17,28): warning CS0169: The field 'C<T>.c' is never used
                //     private static C<T*[]> c;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "c").WithArguments("C<T>.c")
            };

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(expected);
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expected);
        }

        [WorkItem(544003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544003")]
        [WorkItem(544232, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544232")]
        [Fact]
        public void PointerTypesAsTypeArgs3()
        {
            string text = @"
class A
{
    public class B{}
}
class C<T> : A
{
    // BREAKING: Dev10 does not report an error here because it does not look for ERR_ManagedAddr until
    // late in the binding process - at which point the type has been resolved to A.B.
    private static C<string*[]>.B b;

    // Dev10 and Roslyn produce the same diagnostic here.
    private static C<string*[]> c;
}
";
            var expected = new[]
            {
                // (10,22): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('string')
                //     private static C<T*[]>.B b;
                Diagnostic(ErrorCode.ERR_ManagedAddr, "string*").WithArguments("string"),
                // (15,22): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('string')
                //     private static C<T*[]> c;
                Diagnostic(ErrorCode.ERR_ManagedAddr, "string*").WithArguments("string"),

                // (10,30): warning CS0169: The field 'C<T>.b' is never used
                //     private static C<T*[]>.B b;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "b").WithArguments("C<T>.b"),
                // (15,28): warning CS0169: The field 'C<T>.c' is never used
                //     private static C<T*[]> c;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "c").WithArguments("C<T>.c")
            };

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(expected);
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expected);
        }

        #endregion PointerTypes tests

        #region misc unsafe tests

        [Fact, WorkItem(543988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543988")]
        public void UnsafeFieldInitializerInStruct()
        {
            string sourceCode = @"
public struct Test
{
    static unsafe int*[] myArray = new int*[100];
}
";
            var tree = Parse(sourceCode);
            var comp = CreateCompilationWithMscorlib(tree, options: TestOptions.UnsafeReleaseDll);
            var model = comp.GetSemanticModel(tree);

            model.GetDiagnostics().Verify();
        }

        [Fact, WorkItem(544143, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544143")]
        public void ConvertFromPointerToSelf()
        {
            string text = @"
struct C
{
    unsafe static public implicit operator long*(C* i)
    {
        return null;
    }
    unsafe static void Main()
    {
        C c = new C();
    }
}
";
            var comp = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyDiagnostics(
                // (4,44): error CS0556: User-defined conversion must convert to or from the enclosing type
                //     unsafe static public implicit operator long*(C* i)
                Diagnostic(ErrorCode.ERR_ConversionNotInvolvingContainedType, "long*"),
                // (10,11): warning CS0219: The variable 'c' is assigned but its value is never used
                //         C c = new C();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "c").WithArguments("c"));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var parameterSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().Single();

            var info = model.GetSemanticInfoSummary(parameterSyntax.Type);
        }

        [Fact]
        public void PointerVolatileField()
        {
            string text = @"
unsafe class C
{
    volatile int* p; //Spec section 18.2 specifically allows this.
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (4,19): warning CS0169: The field 'C.p' is never used
                //     volatile int* p; //Spec section 18.2 specifically allows this.
                Diagnostic(ErrorCode.WRN_UnreferencedField, "p").WithArguments("C.p"));
        }

        [Fact]
        public void SemanticModelPointerArrayForeachInfo()
        {
            var text = @"
using System;

unsafe class C
{
    static void Main()
    {
        foreach(int* element in new int*[3])
        {
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var foreachSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            // Reports members of non-generic interfaces (pointers can't be type arguments).
            var info = model.GetForEachStatementInfo(foreachSyntax);
            Assert.Equal(default(ForEachStatementInfo), info);
        }

        [Fact, WorkItem(544336, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544336")]
        public void PointerTypeAsDelegateParamInAnonMethod()
        {
            // It is legal to use a delegate with pointer types in a "safe" context
            // provided that you do not actually make a parameter of unsafe type!
            string sourceCode = @"
unsafe delegate int D(int* p);
class C 
{
	static void Main()
	{
		D d = delegate { return 1;};
	}
}
";
            var tree = Parse(sourceCode);
            var comp = CreateCompilationWithMscorlib(tree, options: TestOptions.UnsafeReleaseDll);
            var model = comp.GetSemanticModel(tree);

            model.GetDiagnostics().Verify();
        }

        [Fact(Skip = "529402"), WorkItem(529402, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529402")]
        public void DotOperatorOnPointerTypes()
        {
            string text = @"
unsafe class Program
{
    static void Main(string[] args)
    {
        int* i1 = null;
        i1.ToString();
    }
}
";
            var comp = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (7,9): error CS0023: Operator '.' cannot be applied to operand of type 'int*'
                //        i1.ToString();
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "i1.ToString").WithArguments(".", "int*"));
        }

        [Fact, WorkItem(545028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545028")]
        public void PointerToEnumInGeneric()
        {
            string text = @"
class C<T>
{
    enum E { }
    unsafe E* ptr;
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (5,15): warning CS0169: The field 'C<T>.ptr' is never used
                //     unsafe E* ptr;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "ptr").WithArguments("C<T>.ptr"));
        }

        [Fact]
        public void InvalidAsConversions()
        {
            string text = @"
using System;

public class Test
{
    unsafe void M<T>(T t)
    {
        int* p = null;
        Console.WriteLine(t as int*); // CS0244
        Console.WriteLine(p as T); // Dev10 reports CS0244 here as well, but CS0413 seems reasonable
        Console.WriteLine(null as int*); // CS0244
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (9,27): error CS0244: Neither 'is' nor 'as' is valid on pointer types
                //         Console.WriteLine(t as int*); // pointer
                Diagnostic(ErrorCode.ERR_PointerInAsOrIs, "t as int*"),
                // (10,27): error CS0413: The type parameter 'T' cannot be used with the 'as' operator because it does not have a class type constraint nor a 'class' constraint
                //         Console.WriteLine(p as T); // pointer
                Diagnostic(ErrorCode.ERR_AsWithTypeVar, "p as T").WithArguments("T"),
                // (11,27): error CS0244: Neither 'is' nor 'as' is valid on pointer types
                //         Console.WriteLine(null as int*); // pointer
                Diagnostic(ErrorCode.ERR_PointerInAsOrIs, "null as int*"));
        }

        [Fact]
        public void UnsafeConstructorInitializer()
        {
            string template = @"
{0} class A
{{
    {1} public A(params int*[] x) {{ }}
}}
 
{0} class B : A
{{
    {1} B(int x) {{ }}
    {1} B(double x) : base() {{ }}
}}
";
            CompareUnsafeDiagnostics(template,
                // (4,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      public A(params int*[] x) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*"),
                // (9,6): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      B(int x) { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "B"),
                // (10,20): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //      B(double x) : base() { }
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "base"));
        }

        [WorkItem(545985, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545985")]
        [Fact]
        public void UnboxPointer()
        {
            var text = @"
class C
{
    unsafe void Foo(object obj)
    {
        var x = (int*)obj;
    }
}
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,17): error CS0030: Cannot convert type 'object' to 'int*'
                //         var x = (int*)obj;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int*)obj").WithArguments("object", "int*"));
        }


        [Fact]
        public void FixedBuffersNoDefiniteAssignmentCheck()
        {
            var text = @"
    unsafe struct struct_ForTestingDefiniteAssignmentChecking        
    {
        //Definite Assignment Checking
        public fixed int FixedbuffInt[1024];
    }
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void FixedBuffersNoErorsOnValidTypes()
        {
            var text = @"
    unsafe struct struct_ForTestingDefiniteAssignmentChecking        
    {
    public fixed bool _Type1[10]; 
    public fixed byte _Type12[10]; 
    public fixed int _Type2[10]; 
    public fixed short _Type3[10]; 
    public fixed long _Type4[10]; 
    public fixed char _Type5[10]; 
    public fixed sbyte _Type6[10]; 
    public fixed ushort _Type7[10]; 
    public fixed uint _Type8[10]; 
    public fixed ulong _Type9[10]; 
    public fixed float _Type10[10]; 
    public fixed double _Type11[10];  
    }
";
            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact()]
        [WorkItem(547030, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547030")]
        public void FixedBuffersUsageScenarioInRange()
        {
            var text = @"
using System;

unsafe struct s
{
    public fixed bool  _buffer[5]; // This is a fixed buffer.
}

class Program
{
    static s _fixedBufferExample = new s();  

    static void Main()
    {
        // Store values in the fixed buffer.
        // ... The load the values from the fixed buffer.
        Store();
        Load();
   }

    unsafe static void Store()
    {
        // Put the fixed buffer in unmovable memory.
        // ... Then assign some elements.
        fixed (bool* buffer = _fixedBufferExample._buffer)
        {
            buffer[0] = true;
            buffer[1] = false;
            buffer[2] = true;
        }
    }

    unsafe static void Load()
    {
        // Put in unmovable memory.
        // ... Then load some values from the memory.
        fixed (bool* buffer = _fixedBufferExample._buffer)
        {
            Console.Write(buffer[0]);
            Console.Write(buffer[1]);
            Console.Write(buffer[2]);
        }
    }

}
";
            var compilation = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe);

            compilation.VerifyIL("Program.Store", @"{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (pinned bool& V_0) //buffer
  IL_0000:  ldsflda    ""s Program._fixedBufferExample""
  IL_0005:  ldflda     ""bool* s._buffer""
  IL_000a:  ldflda     ""bool s.<_buffer>e__FixedBuffer.FixedElementField""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  conv.i
  IL_0012:  ldc.i4.1
  IL_0013:  stind.i1
  IL_0014:  ldloc.0
  IL_0015:  conv.i
  IL_0016:  ldc.i4.1
  IL_0017:  add
  IL_0018:  ldc.i4.0
  IL_0019:  stind.i1
  IL_001a:  ldloc.0
  IL_001b:  conv.i
  IL_001c:  ldc.i4.2
  IL_001d:  add
  IL_001e:  ldc.i4.1
  IL_001f:  stind.i1
  IL_0020:  ldc.i4.0
  IL_0021:  conv.u
  IL_0022:  stloc.0
  IL_0023:  ret
}
");
            compilation.VerifyIL("Program.Load", @"
{
  // Code size       48 (0x30)
  .maxstack  2
  .locals init (pinned bool& V_0) //buffer
  IL_0000:  ldsflda    ""s Program._fixedBufferExample""
  IL_0005:  ldflda     ""bool* s._buffer""
  IL_000a:  ldflda     ""bool s.<_buffer>e__FixedBuffer.FixedElementField""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  conv.i
  IL_0012:  ldind.u1
  IL_0013:  call       ""void System.Console.Write(bool)""
  IL_0018:  ldloc.0
  IL_0019:  conv.i
  IL_001a:  ldc.i4.1
  IL_001b:  add
  IL_001c:  ldind.u1
  IL_001d:  call       ""void System.Console.Write(bool)""
  IL_0022:  ldloc.0
  IL_0023:  conv.i
  IL_0024:  ldc.i4.2
  IL_0025:  add
  IL_0026:  ldind.u1
  IL_0027:  call       ""void System.Console.Write(bool)""
  IL_002c:  ldc.i4.0
  IL_002d:  conv.u
  IL_002e:  stloc.0
  IL_002f:  ret
}
");
        }

        [Fact()]
        [WorkItem(547030, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547030")]
        public void FixedBuffersUsagescenarioOutOfRange()
        {
            // This should work as no range checking for unsafe code.
            //however the fact that we are writing bad unsafe code has potential for encountering problems            
            var text = @"
using System;

unsafe struct s
{
    public fixed bool  _buffer[5]; // This is a fixed buffer.
}

class Program
{
    static s _fixedBufferExample = new s();  

    static void Main()
    {
        // Store values in the fixed buffer.
        // ... The load the values from the fixed buffer.
        Store();
        Load();
   }

    unsafe static void Store()
    {
        // Put the fixed buffer in unmovable memory.
        // ... Then assign some elements.
        fixed (bool* buffer = _fixedBufferExample._buffer)
        {
            buffer[0] = true;
            buffer[8] = false;
            buffer[10] = true;
        }
    }

    unsafe static void Load()
    {
        // Put in unmovable memory.
        // ... Then load some values from the memory.
        fixed (bool* buffer = _fixedBufferExample._buffer)
        {
            Console.Write(buffer[0]);
            Console.Write(buffer[8]);
            Console.Write(buffer[10]);
        }
    }

}
";
            //IL Baseline rather than execute because I'm intentionally writing outside of bounds of buffer
            // This will compile without warning but runtime behavior is unpredictable.

            var compilation = CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe);
            compilation.VerifyIL("Program.Load", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (pinned bool& V_0) //buffer
  IL_0000:  ldsflda    ""s Program._fixedBufferExample""
  IL_0005:  ldflda     ""bool* s._buffer""
  IL_000a:  ldflda     ""bool s.<_buffer>e__FixedBuffer.FixedElementField""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  conv.i
  IL_0012:  ldind.u1
  IL_0013:  call       ""void System.Console.Write(bool)""
  IL_0018:  ldloc.0
  IL_0019:  conv.i
  IL_001a:  ldc.i4.8
  IL_001b:  add
  IL_001c:  ldind.u1
  IL_001d:  call       ""void System.Console.Write(bool)""
  IL_0022:  ldloc.0
  IL_0023:  conv.i
  IL_0024:  ldc.i4.s   10
  IL_0026:  add
  IL_0027:  ldind.u1
  IL_0028:  call       ""void System.Console.Write(bool)""
  IL_002d:  ldc.i4.0
  IL_002e:  conv.u
  IL_002f:  stloc.0
  IL_0030:  ret
}");

            compilation.VerifyIL("Program.Store", @"{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (pinned bool& V_0) //buffer
  IL_0000:  ldsflda    ""s Program._fixedBufferExample""
  IL_0005:  ldflda     ""bool* s._buffer""
  IL_000a:  ldflda     ""bool s.<_buffer>e__FixedBuffer.FixedElementField""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  conv.i
  IL_0012:  ldc.i4.1
  IL_0013:  stind.i1
  IL_0014:  ldloc.0
  IL_0015:  conv.i
  IL_0016:  ldc.i4.8
  IL_0017:  add
  IL_0018:  ldc.i4.0
  IL_0019:  stind.i1
  IL_001a:  ldloc.0
  IL_001b:  conv.i
  IL_001c:  ldc.i4.s   10
  IL_001e:  add
  IL_001f:  ldc.i4.1
  IL_0020:  stind.i1
  IL_0021:  ldc.i4.0
  IL_0022:  conv.u
  IL_0023:  stloc.0
  IL_0024:  ret
}");
        }

        [Fact]
        public void FixedBufferUsageWith_this()
        {
            var text = @"
using System;

unsafe struct s
    {
        private fixed ushort _e_res[4]; 
        void Error_UsingFixedBuffersWithThis()
        {
            fixed (ushort* abc = this._e_res)
            {
                abc[2] = 1;
            }
        }
        
    }
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll);
        }

        [Fact]
        [WorkItem(547074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547074")]
        public void FixedBufferWithNoSize()
        {
            var text = @"
unsafe struct S
{
    fixed[
}
";
            var s = CreateCompilationWithMscorlib(text).GlobalNamespace.GetMember<TypeSymbol>("S");
            foreach (var member in s.GetMembers())
            {
                var field = member as FieldSymbol;
                if (field != null)
                {
                    Assert.Equal(0, field.FixedSize);
                }
            }
        }

        [Fact()]
        [WorkItem(547030, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547030")]
        public void FixedBufferUsageDifferentAssemblies()
        {
            // Ensure fixed buffers work as expected when fixed buffer is created in different assembly to where it is consumed.
            var s1 =
@"using System;

namespace ClassLibrary1
{

    public unsafe struct FixedBufferExampleForSizes
    {
        public fixed bool _buffer[5]; // This is a fixed buffer.
    }
}";
            var s2 =
@"using System; using ClassLibrary1;

namespace ConsoleApplication30
{
    class Program
    {
        static FixedBufferExampleForSizes _fixedBufferExample = new FixedBufferExampleForSizes();

        static void Main()
        {
            // Store values in the fixed buffer.
            // ... The load the values from the fixed buffer.
            Store();
            Load();
        }

        unsafe static void Store()
        {
            // Put the fixed buffer in unmovable memory.
            // ... Then assign some elements.
            fixed (bool* buffer = _fixedBufferExample._buffer)
            {
                buffer[0] = true;
                buffer[10] = false;
            }
        }

        unsafe static void Load()
        {
            // Put in unmovable memory.
            // ... Then load some values from the memory.
            fixed (bool* buffer = _fixedBufferExample._buffer)
            {
                Console.Write(buffer[0]);
                Console.Write(buffer[10]);
            }
        }

    }
}";
            var comp1 = CompileAndVerify(s1, options: TestOptions.UnsafeReleaseDll).Compilation;

            var comp2 = CompileAndVerify(s2,
                options: TestOptions.UnsafeReleaseExe,
                additionalRefs: new MetadataReference[] { MetadataReference.CreateFromImage(comp1.EmitToArray()) },
                expectedOutput: "TrueFalse").Compilation;


            var s3 =
@"using System; using ClassLibrary1;

namespace ConsoleApplication30
{
    class Program
    {
        static FixedBufferExampleForSizes _fixedBufferExample = new FixedBufferExampleForSizes();

        static void Main()
        {
            // Store values in the fixed buffer.
            // ... The load the values from the fixed buffer.
            Store();
            Load();
        }

        unsafe static void Store()
        {
            // Put the fixed buffer in unmovable memory.
            // ... Then assign some elements.
            fixed (bool* buffer = _fixedBufferExample._buffer)
            {
                buffer[0] = true;
                buffer[10] = false;
                buffer[1024] = true;  //Intentionally outside range
            }
        }

        unsafe static void Load()
        {
            // Put in unmovable memory.
            // ... Then load some values from the memory.
            fixed (bool* buffer = _fixedBufferExample._buffer)
            {
                Console.Write(buffer[0]);
                Console.Write(buffer[10]);
                Console.Write(buffer[1024]);
            }
        }

    }
}";

            // Only compile this as its intentionally writing outside of fixed buffer boundaries and 
            // this doesn't warn but causes flakiness when executed.
            var comp3 = CompileAndVerify(s3,
                options: TestOptions.UnsafeReleaseDll,
                additionalRefs: new MetadataReference[] { MetadataReference.CreateFromImage(comp1.EmitToArray()) }).Compilation;
        }

        [Fact]
        public void FixedBufferUsageSizeCheckChar()
        {
            //Determine the Correct size based upon expansion for different no of elements

            var text = @"using System;

unsafe struct FixedBufferExampleForSizes1
{
    public fixed char _buffer[1]; // This is a fixed buffer.
}
unsafe struct FixedBufferExampleForSizes2
{
    public fixed char _buffer[2]; // This is a fixed buffer.
}

unsafe struct FixedBufferExampleForSizes3
{
    public fixed char _buffer[3]; // This is a fixed buffer.
}

class Program
{
    // Reference to struct containing a fixed buffer
    static FixedBufferExampleForSizes1 _fixedBufferExample1 = new FixedBufferExampleForSizes1();
    static FixedBufferExampleForSizes2 _fixedBufferExample2 = new FixedBufferExampleForSizes2();
    static FixedBufferExampleForSizes3 _fixedBufferExample3 = new FixedBufferExampleForSizes3();  

    static unsafe void Main()
    {                      
        int x = System.Runtime.InteropServices.Marshal.SizeOf(_fixedBufferExample1);          
        Console.Write(x.ToString());
        
        x = System.Runtime.InteropServices.Marshal.SizeOf(_fixedBufferExample2);
        Console.Write(x.ToString());

        x = System.Runtime.InteropServices.Marshal.SizeOf(_fixedBufferExample3);
        Console.Write(x.ToString());
    }   
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"246");
        }

        [Fact]
        public void FixedBufferUsageSizeCheckInt()
        {
            //Determine the Correct size based upon expansion for different no of elements

            var text = @"using System;

unsafe struct FixedBufferExampleForSizes1
{
    public fixed int _buffer[1]; // This is a fixed buffer.
}
unsafe struct FixedBufferExampleForSizes2
{
    public fixed int _buffer[2]; // This is a fixed buffer.
}

unsafe struct FixedBufferExampleForSizes3
{
    public fixed int _buffer[3]; // This is a fixed buffer.
}

class Program
{
    // Reference to struct containing a fixed buffer
    static FixedBufferExampleForSizes1 _fixedBufferExample1 = new FixedBufferExampleForSizes1();
    static FixedBufferExampleForSizes2 _fixedBufferExample2 = new FixedBufferExampleForSizes2();
    static FixedBufferExampleForSizes3 _fixedBufferExample3 = new FixedBufferExampleForSizes3();  

    static unsafe void Main()
    {                      
        int x = System.Runtime.InteropServices.Marshal.SizeOf(_fixedBufferExample1);          
        Console.Write(x.ToString());
        
        x = System.Runtime.InteropServices.Marshal.SizeOf(_fixedBufferExample2);
        Console.Write(x.ToString());

        x = System.Runtime.InteropServices.Marshal.SizeOf(_fixedBufferExample3);
        Console.Write(x.ToString());
    }   
}
";
            CompileAndVerify(text, options: TestOptions.UnsafeReleaseExe, expectedOutput: @"4812");
        }

        #endregion
    }
}
