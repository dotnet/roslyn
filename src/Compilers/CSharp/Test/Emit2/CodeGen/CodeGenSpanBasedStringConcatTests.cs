// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen;

public class CodeGenSpanBasedStringConcatTests : CSharpTestBase
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpan1()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                }

                static string M1(string s, char c) => s + c;
                static string M2(string s, char c) => c + s;
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sccs" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       21 (0x15)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       21 (0x15)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpan2()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'C';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                }

                static string M1(string s, char c) => s + char.ToLowerInvariant(c);
                static string M2(string s, char c) => char.ToLowerInvariant(c) + s;
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sccs" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       26 (0x1a)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  call       "char char.ToLowerInvariant(char)"
              IL_000c:  stloc.0
              IL_000d:  ldloca.s   V_0
              IL_000f:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0014:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0019:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       26 (0x1a)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0019:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpan_SideEffect()
    {
        var source = """
            using System;

            public class Test
            {
                private static int stringCounter;
                private static int charCounterPlusOne = 1;

                static void Main()
                {
                    Console.WriteLine(M1());
                    Console.WriteLine(M2());
                }

                static string M1() => GetStringWithSideEffect() + GetCharWithSideEffect();
                static string M2() => GetCharWithSideEffect() + GetStringWithSideEffect();

                private static string GetStringWithSideEffect()
                {
                    Console.Write(stringCounter++);
                    return "s";
                }

                private static char GetCharWithSideEffect()
                {
                    Console.Write(charCounterPlusOne++);
                    return 'c';
                }
            }
            """;

        var expectedOutput = """
            01sc
            21cs
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? expectedOutput : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       29 (0x1d)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "char Test.GetCharWithSideEffect()"
              IL_000f:  stloc.0
              IL_0010:  ldloca.s   V_0
              IL_0012:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0017:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001c:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       29 (0x1d)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001c:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpan_ReferenceToSameLocation()
    {
        var source = """
            using System;

            var c = new C();
            c.M();

            class C
            {
                public char c = 'a';

                public ref char GetC()
                {
                    c = 'b';
                    return ref c;
                }

                public void M()
                {
                    Console.Write(c.ToString() + GetC());
                }
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "ab" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("C.M", """
            {
              // Code size       40 (0x28)
              .maxstack  2
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "char C.c"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "ref char C.GetC()"
              IL_0014:  ldind.u2
              IL_0015:  stloc.1
              IL_0016:  ldloca.s   V_1
              IL_0018:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001d:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0022:  call       "void System.Console.Write(string)"
              IL_0027:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpan_MutateLocal()
    {
        var source = """
            using System;

            var c = new C();
            Console.WriteLine(c.M());

            class C
            {
                public string M()
                {
                    var c = 'a';
                    return c + SneakyLocalChange(ref c);
                }

                private string SneakyLocalChange(ref char local)
                {
                    local = 'b';
                    return "b";
                }
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "ab" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("C.M", """
            {
              // Code size       31 (0x1f)
              .maxstack  3
              .locals init (char V_0, //c
                            char V_1)
              IL_0000:  ldc.i4.s   97
              IL_0002:  stloc.0
              IL_0003:  ldloc.0
              IL_0004:  stloc.1
              IL_0005:  ldloca.s   V_1
              IL_0007:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000c:  ldarg.0
              IL_000d:  ldloca.s   V_0
              IL_000f:  call       "string C.SneakyLocalChange(ref char)"
              IL_0014:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0019:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001e:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/72232")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatTwo_ReadOnlySpan_NullConcatArgument(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var c = 'c';
                    Console.Write(M1(c));
                    Console.Write(M2(c));
                }

                static string M1(char c) => c + (string)null;
                static string M2(char c) => (string)null + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "cc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        var expectedEquivalentIL = """
            {
              // Code size       17 (0x11)
              .maxstack  2
              IL_0000:  ldarga.s   V_0
              IL_0002:  call       "string char.ToString()"
              IL_0007:  dup
              IL_0008:  brtrue.s   IL_0010
              IL_000a:  pop
              IL_000b:  ldstr      ""
              IL_0010:  ret
            }
            """;

        verifier.VerifyIL("Test.M1", expectedEquivalentIL);
        verifier.VerifyIL("Test.M2", expectedEquivalentIL);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatTwo_ConstantCharToString(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    Console.Write(M1(s));
                    Console.Write(M2(s));
                }

                static string M1(string s) => s + 'c'.ToString();
                static string M2(string s) => 'c'.ToString() + s;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sccs" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // Instead of emitting this as a span-based concat of string and char we recognize "constantChar.ToString()" pattern and lower that argument to a constant string
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       12 (0xc)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldstr      "c"
              IL_0006:  call       "string string.Concat(string, string)"
              IL_000b:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       12 (0xc)
              .maxstack  2
              IL_0000:  ldstr      "c"
              IL_0005:  ldarg.0
              IL_0006:  call       "string string.Concat(string, string)"
              IL_000b:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatTwo_AllConstantCharToStrings(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    Console.Write(M());
                }

                static string M() => 'a'.ToString() + 'b'.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "ab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // Instead of emitting this as a span-based concat of 2 chars we recognize "constantChar.ToString()" pattern and lower both arguments to a constant string
        // which we can then fold into a single constant string and avoid concatenation entirely
        verifier.VerifyIL("Test.M", """
            {
              // Code size        6 (0x6)
              .maxstack  1
              IL_0000:  ldstr      "ab"
              IL_0005:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    public void ConcatTwoCharToStrings(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var c1 = 'a';
                    var c2 = 'b';
                    Console.Write(M(c1, c2));
                }

                static string M(char c1, char c2) => c1.ToString() + c2.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "ab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size       24 (0x18)
              .maxstack  2
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.1
              IL_000a:  stloc.1
              IL_000b:  ldloca.s   V_1
              IL_000d:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0012:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0017:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    public void ConcatTwo_ReadOnlySpan_MissingMemberForOptimization(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                }

                static string M1(string s, char c) => s + c;
                static string M2(string s, char c) => c + s;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((SpecialMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sccs" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       14 (0xe)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  call       "string string.Concat(string, string)"
              IL_000d:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       14 (0xe)
              .maxstack  2
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  call       "string string.Concat(string, string)"
              IL_000d:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_MissingObjectToString()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                }

                static string M1(string s, char c) => s + c;
                static string M2(string s, char c) => c + s;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing(SpecialMember.System_Object__ToString);

        // We don't use object.ToString() or char.ToString() in the final codegen.
        var verifier = CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsCoreClr ? "sccs" : null, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       21 (0x15)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  ret
            }
            """);

        verifier.VerifyIL("Test.M2", """
            {
              // Code size       21 (0x15)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_CharDoesntOverrideObjectToString()
    {
        var corlib_cs = """
            namespace System
            {
                public class Object
                {
                    public virtual string ToString() => null;
                }
                public class String
                {
                    public static string Concat(string str0, string str1) => null;
                    public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1) => null;
                    public static implicit operator ReadOnlySpan<char>(string value) => default;
                }
                public class ValueType { }
                public struct Char { }
                public struct Void { }
                public struct Int32 { }
                public struct Byte { }
                public struct Boolean { }
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(ref readonly T reference) { }
                }
                public class Enum : ValueType { }
                public class Attribute { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets validOn) { }

                    public bool AllowMultiple { get { return default; } set { } }
                    public bool Inherited { get { return default; } set { } }
                }
            }
            """;

        var corlib = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

        var source = """
            public class Test
            {
                static string M(string s, char c) => s + c;
            }
            """;

        var comp = CreateEmptyCompilation(source, [corlib]);
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation: comp, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);
        verifier.VerifyDiagnostics();

        // No matter whether `char` directly overrides `ToString` or not, we still produce span-based concat if we can
        verifier.VerifyIL("Test.M", """
            {
              // Code size       21 (0x15)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpanConstructorParameterIsOrdinaryRef()
    {
        var corlib_cs = """
            namespace System
            {
                public class Object
                {
                    public virtual string ToString() => null;
                }
                public class String
                {
                    public static string Concat(string str0, string str1) => null;
                    public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1) => null;
                    public static implicit operator ReadOnlySpan<char>(string value) => default;
                }
                public class ValueType { }
                public struct Char
                {
                    public override string ToString() => null;
                }
                public struct Void { }
                public struct Int32 { }
                public struct Byte { }
                public struct Boolean { }
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(ref T reference) { }
                }
                public class Enum : ValueType { }
                public class Attribute { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets validOn) { }

                    public bool AllowMultiple { get { return default; } set { } }
                    public bool Inherited { get { return default; } set { } }
                }
            }
            """;

        var corlib = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

        var source = """
            public class Test
            {
                static string M(string s, char c) => s + c;
            }
            """;

        var comp = CreateEmptyCompilation(source, [corlib]);
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation: comp, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("Test.M", """
            {
              // Code size       21 (0x15)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref char)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpanConstructorParameterIsOut()
    {
        var corlib_cs = """
            namespace System
            {
                public class Object
                {
                    public virtual string ToString() => null;
                }
                public class String
                {
                    public static string Concat(string str0, string str1) => null;
                    public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1) => null;
                    public static implicit operator ReadOnlySpan<char>(string value) => default;
                }
                public class ValueType { }
                public struct Char
                {
                    public override string ToString() => null;
                }
                public struct Void { }
                public struct Int32 { }
                public struct Byte { }
                public struct Boolean { }
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(out T reference) { reference = default; }
                }
                public class Enum : ValueType { }
                public class Attribute { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets validOn) { }

                    public bool AllowMultiple { get { return default; } set { } }
                    public bool Inherited { get { return default; } set { } }
                }
            }
            """;

        var corlib = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

        var source = """
            public class Test
            {
                static string M(string s, char c) => s + c;
            }
            """;

        var comp = CreateEmptyCompilation(source, [corlib]);
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation: comp, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);
        verifier.VerifyDiagnostics();

        // Constructor of ReadOnlySpan<char> has unexpected `out` reference. Fallback to string-based concat
        verifier.VerifyIL("Test.M", """
            {
              // Code size       14 (0xe)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  call       "string string.Concat(string, string)"
              IL_000d:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_Await()
    {
        var source = """
            using System;
            using System.Threading.Tasks;

            public class Test
            {
                static async Task Main()
                {
                    Console.Write(await M());
                }

                static async Task<string> M()
                {
                    return (await GetStringAsync()) + (await GetCharAsync());
                }

                static async Task<string> GetStringAsync()
                {
                    await Task.Yield();
                    return "s";
                }

                static async Task<char> GetCharAsync()
                {
                    await Task.Yield();
                    return 'c';
                }
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sc" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
            {
              // Code size      276 (0x114)
              .maxstack  3
              .locals init (int V_0,
                            string V_1,
                            char V_2,
                            System.Runtime.CompilerServices.TaskAwaiter<string> V_3,
                            System.Runtime.CompilerServices.TaskAwaiter<char> V_4,
                            System.Exception V_5)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int Test.<M>d__1.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0048
                IL_000a:  ldloc.0
                IL_000b:  ldc.i4.1
                IL_000c:  beq        IL_00a7
                IL_0011:  call       "System.Threading.Tasks.Task<string> Test.GetStringAsync()"
                IL_0016:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<string> System.Threading.Tasks.Task<string>.GetAwaiter()"
                IL_001b:  stloc.3
                IL_001c:  ldloca.s   V_3
                IL_001e:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<string>.IsCompleted.get"
                IL_0023:  brtrue.s   IL_0064
                IL_0025:  ldarg.0
                IL_0026:  ldc.i4.0
                IL_0027:  dup
                IL_0028:  stloc.0
                IL_0029:  stfld      "int Test.<M>d__1.<>1__state"
                IL_002e:  ldarg.0
                IL_002f:  ldloc.3
                IL_0030:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_0035:  ldarg.0
                IL_0036:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_003b:  ldloca.s   V_3
                IL_003d:  ldarg.0
                IL_003e:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<string>, Test.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<string>, ref Test.<M>d__1)"
                IL_0043:  leave      IL_0113
                IL_0048:  ldarg.0
                IL_0049:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_004e:  stloc.3
                IL_004f:  ldarg.0
                IL_0050:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_0055:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<string>"
                IL_005b:  ldarg.0
                IL_005c:  ldc.i4.m1
                IL_005d:  dup
                IL_005e:  stloc.0
                IL_005f:  stfld      "int Test.<M>d__1.<>1__state"
                IL_0064:  ldarg.0
                IL_0065:  ldloca.s   V_3
                IL_0067:  call       "string System.Runtime.CompilerServices.TaskAwaiter<string>.GetResult()"
                IL_006c:  stfld      "string Test.<M>d__1.<>7__wrap1"
                IL_0071:  call       "System.Threading.Tasks.Task<char> Test.GetCharAsync()"
                IL_0076:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<char> System.Threading.Tasks.Task<char>.GetAwaiter()"
                IL_007b:  stloc.s    V_4
                IL_007d:  ldloca.s   V_4
                IL_007f:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<char>.IsCompleted.get"
                IL_0084:  brtrue.s   IL_00c4
                IL_0086:  ldarg.0
                IL_0087:  ldc.i4.1
                IL_0088:  dup
                IL_0089:  stloc.0
                IL_008a:  stfld      "int Test.<M>d__1.<>1__state"
                IL_008f:  ldarg.0
                IL_0090:  ldloc.s    V_4
                IL_0092:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_0097:  ldarg.0
                IL_0098:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_009d:  ldloca.s   V_4
                IL_009f:  ldarg.0
                IL_00a0:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<char>, Test.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<char>, ref Test.<M>d__1)"
                IL_00a5:  leave.s    IL_0113
                IL_00a7:  ldarg.0
                IL_00a8:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_00ad:  stloc.s    V_4
                IL_00af:  ldarg.0
                IL_00b0:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_00b5:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<char>"
                IL_00bb:  ldarg.0
                IL_00bc:  ldc.i4.m1
                IL_00bd:  dup
                IL_00be:  stloc.0
                IL_00bf:  stfld      "int Test.<M>d__1.<>1__state"
                IL_00c4:  ldloca.s   V_4
                IL_00c6:  call       "char System.Runtime.CompilerServices.TaskAwaiter<char>.GetResult()"
                IL_00cb:  stloc.2
                IL_00cc:  ldarg.0
                IL_00cd:  ldfld      "string Test.<M>d__1.<>7__wrap1"
                IL_00d2:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
                IL_00d7:  ldloca.s   V_2
                IL_00d9:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
                IL_00de:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
                IL_00e3:  stloc.1
                IL_00e4:  leave.s    IL_00ff
              }
              catch System.Exception
              {
                IL_00e6:  stloc.s    V_5
                IL_00e8:  ldarg.0
                IL_00e9:  ldc.i4.s   -2
                IL_00eb:  stfld      "int Test.<M>d__1.<>1__state"
                IL_00f0:  ldarg.0
                IL_00f1:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_00f6:  ldloc.s    V_5
                IL_00f8:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetException(System.Exception)"
                IL_00fd:  leave.s    IL_0113
              }
              IL_00ff:  ldarg.0
              IL_0100:  ldc.i4.s   -2
              IL_0102:  stfld      "int Test.<M>d__1.<>1__state"
              IL_0107:  ldarg.0
              IL_0108:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
              IL_010d:  ldloc.1
              IL_010e:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetResult(string)"
              IL_0113:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_UserDefinedReadOnlySpan()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                }

                static string M1(string s, char c) => s + c;
                static string M2(string s, char c) => c + s;
            }

            namespace System
            {
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(ref readonly T reference) { }
                }
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sccs" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       21 (0x15)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       21 (0x15)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatThree_ReadOnlySpan1(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                }

                static string M1(string s, char c) => c + s + s;
                static string M2(string s, char c) => s + c + s;
                static string M3(string s, char c) => s + s + c;
                static string M4(string s, char c) => c + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "cssscsssccsc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  stloc.0
              IL_000e:  ldloca.s   V_0
              IL_0010:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       30 (0x1e)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.1
              IL_0010:  stloc.1
              IL_0011:  ldloca.s   V_1
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0018:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001d:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatThree_ReadOnlySpan2(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'C';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                }

                static string M1(string s, char c) => char.ToLowerInvariant(c) + s + s;
                static string M2(string s, char c) => s + char.ToLowerInvariant(c) + s;
                static string M3(string s, char c) => s + s + char.ToLowerInvariant(c);
                static string M4(string s, char c) => char.ToLowerInvariant(c) + s + char.ToLowerInvariant(c);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "cssscsssccsc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       32 (0x20)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       32 (0x20)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  call       "char char.ToLowerInvariant(char)"
              IL_000c:  stloc.0
              IL_000d:  ldloca.s   V_0
              IL_000f:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       32 (0x20)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  call       "char char.ToLowerInvariant(char)"
              IL_0012:  stloc.0
              IL_0013:  ldloca.s   V_0
              IL_0015:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001a:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       40 (0x28)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  ldarg.1
              IL_0015:  call       "char char.ToLowerInvariant(char)"
              IL_001a:  stloc.1
              IL_001b:  ldloca.s   V_1
              IL_001d:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0022:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0027:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData("(s + c) + s")]
    [InlineData("s + (c + s)")]
    [InlineData("string.Concat(s, c.ToString()) + s")]
    [InlineData("s + string.Concat(c.ToString(), s)")]
    public void ConcatThree_ReadOnlySpan_OperandGroupingAndUserInputOfStringBasedConcats(string expression)
    {
        var source = $$"""
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M(s, c));
                }

                static string M(string s, char c) => {{expression}};
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "scs" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatThree_ReadOnlySpan_SideEffect(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                private static int stringCounter;
                private static int charCounterPlusOne = 1;

                static void Main()
                {
                    Console.WriteLine(M1());
                    Console.WriteLine(M2());
                    Console.WriteLine(M3());
                    Console.WriteLine(M4());
                }

                static string M1() => GetCharWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect();
                static string M2() => GetStringWithSideEffect() + GetCharWithSideEffect() + GetStringWithSideEffect();
                static string M3() => GetStringWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect();
                static string M4() => GetCharWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect();

                private static string GetStringWithSideEffect()
                {
                    Console.Write(stringCounter++);
                    return "s";
                }

                private static char GetCharWithSideEffect()
                {
                    Console.Write(charCounterPlusOne++);
                    return 'c';
                }
            }
            """;

        var expectedOutput = """
            101css
            223scs
            453ssc
            465csc
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? expectedOutput : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       39 (0x27)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0026:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       39 (0x27)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "char Test.GetCharWithSideEffect()"
              IL_000f:  stloc.0
              IL_0010:  ldloca.s   V_0
              IL_0012:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0026:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       39 (0x27)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "string Test.GetStringWithSideEffect()"
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  call       "char Test.GetCharWithSideEffect()"
              IL_0019:  stloc.0
              IL_001a:  ldloca.s   V_0
              IL_001c:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0021:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0026:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       42 (0x2a)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "char Test.GetCharWithSideEffect()"
              IL_001c:  stloc.1
              IL_001d:  ldloca.s   V_1
              IL_001f:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0024:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0029:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatThree_ReadOnlySpan_ReferenceToSameLocation(int? missingUnimportantMember)
    {
        var source = """
            using System;

            var c = new C();
            c.M();

            class C
            {
                public char c = 'a';

                public ref char GetC()
                {
                    c = 'b';
                    return ref c;
                }

                public void M()
                {
                    Console.Write("a" + c + GetC());
                }
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "aab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       50 (0x32)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldstr      "a"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  ldarg.0
              IL_000b:  ldfld      "char C.c"
              IL_0010:  stloc.0
              IL_0011:  ldloca.s   V_0
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0018:  ldarg.0
              IL_0019:  call       "ref char C.GetC()"
              IL_001e:  ldind.u2
              IL_001f:  stloc.1
              IL_0020:  ldloca.s   V_1
              IL_0022:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0027:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002c:  call       "void System.Console.Write(string)"
              IL_0031:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatThree_ReadOnlySpan_MutateLocal(int? missingUnimportantMember)
    {
        var source = """
            using System;

            var c = new C();
            Console.WriteLine(c.M());

            class C
            {
                public string M()
                {
                    var c = 'a';
                    return "a" + c + SneakyLocalChange(ref c);
                }

                private char SneakyLocalChange(ref char local)
                {
                    local = 'b';
                    return 'b';
                }
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "aab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       44 (0x2c)
              .maxstack  4
              .locals init (char V_0, //c
                            char V_1,
                            char V_2)
              IL_0000:  ldc.i4.s   97
              IL_0002:  stloc.0
              IL_0003:  ldstr      "a"
              IL_0008:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000d:  ldloc.0
              IL_000e:  stloc.1
              IL_000f:  ldloca.s   V_1
              IL_0011:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0016:  ldarg.0
              IL_0017:  ldloca.s   V_0
              IL_0019:  call       "char C.SneakyLocalChange(ref char)"
              IL_001e:  stloc.2
              IL_001f:  ldloca.s   V_2
              IL_0021:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0026:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002b:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatThree_ReadOnlySpan_NullConcatArgument(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                }

                static string M1(string s, char c) => (string)null + s + c;
                static string M2(string s, char c) => s + (c + (string)null);
                static string M3(string s, char c) => (s + c) + (string)null;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "scscsc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        var expectedEquivalentIL = """
            {
              // Code size       21 (0x15)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  ret
            }
            """;

        verifier.VerifyIL("Test.M1", expectedEquivalentIL);
        verifier.VerifyIL("Test.M2", expectedEquivalentIL);
        verifier.VerifyIL("Test.M3", expectedEquivalentIL);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatThree_ConstantCharToString(int? missingUnimportantMember)
    {
        var source = """
            using System;
            
            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    Console.Write(M1(s));
                    Console.Write(M2(s));
                    Console.Write(M3(s));
                    Console.Write(M4(s));
                }
            
                static string M1(string s) => 'c'.ToString() + s + s;
                static string M2(string s) => s + 'c'.ToString() + s;
                static string M3(string s) => s + s + 'c'.ToString();
                static string M4(string s) => 'c'.ToString() + s + 'c'.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "cssscsssccsc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // Instead of emitting this as a span-based concat of strings and chars we recognize "constantChar.ToString()" pattern and lower that arguments to constant strings
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       13 (0xd)
              .maxstack  3
              IL_0000:  ldstr      "c"
              IL_0005:  ldarg.0
              IL_0006:  ldarg.0
              IL_0007:  call       "string string.Concat(string, string, string)"
              IL_000c:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       13 (0xd)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldstr      "c"
              IL_0006:  ldarg.0
              IL_0007:  call       "string string.Concat(string, string, string)"
              IL_000c:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       13 (0xd)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  ldstr      "c"
              IL_0007:  call       "string string.Concat(string, string, string)"
              IL_000c:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       17 (0x11)
              .maxstack  3
              IL_0000:  ldstr      "c"
              IL_0005:  ldarg.0
              IL_0006:  ldstr      "c"
              IL_000b:  call       "string string.Concat(string, string, string)"
              IL_0010:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatThree_AllConstantCharToStrings(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    Console.Write(M());
                }

                static string M() => 'a'.ToString() + 'b'.ToString() + 'c'.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // Instead of emitting this as a span-based concat of 3 chars we recognize "constantChar.ToString()" pattern and lower all arguments to a constant string
        // which we can then fold into a single constant string and avoid concatenation entirely
        verifier.VerifyIL("Test.M", """
            {
              // Code size        6 (0x6)
              .maxstack  1
              IL_0000:  ldstr      "abc"
              IL_0005:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatThreeCharToStrings(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var c1 = 'a';
                    var c2 = 'b';
                    var c3 = 'c';
                    Console.Write(M(c1, c2, c3));
                }

                static string M(char c1, char c2, char c3) => c1.ToString() + c2.ToString() + c3.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size       33 (0x21)
              .maxstack  3
              .locals init (char V_0,
                            char V_1,
                            char V_2)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.1
              IL_000a:  stloc.1
              IL_000b:  ldloca.s   V_1
              IL_000d:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0012:  ldarg.2
              IL_0013:  stloc.2
              IL_0014:  ldloca.s   V_2
              IL_0016:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    public void ConcatThree_ReadOnlySpan_MissingMemberForOptimization(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                }

                static string M1(string s, char c) => c + s + s;
                static string M2(string s, char c) => s + c + s;
                static string M3(string s, char c) => s + s + c;
                static string M4(string s, char c) => c + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((SpecialMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "cssscsssccsc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       15 (0xf)
              .maxstack  3
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.0
              IL_0009:  call       "string string.Concat(string, string, string)"
              IL_000e:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       15 (0xf)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  ldarg.0
              IL_0009:  call       "string string.Concat(string, string, string)"
              IL_000e:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       15 (0xf)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  ldarga.s   V_1
              IL_0004:  call       "string char.ToString()"
              IL_0009:  call       "string string.Concat(string, string, string)"
              IL_000e:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       21 (0x15)
              .maxstack  3
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarga.s   V_1
              IL_000a:  call       "string char.ToString()"
              IL_000f:  call       "string string.Concat(string, string, string)"
              IL_0014:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatThree_UserInputOfSpanBasedConcat_ConcatWithString(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    Console.Write(M1(s1.AsSpan(), s2, s3));
                    Console.Write(M2(s1.AsSpan(), s2, s3));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, string s3) => string.Concat(s1, s2.AsSpan()) + s3;
                static string M2(ReadOnlySpan<char> s1, string s2, string s3) => s3 + string.Concat(s1, s2.AsSpan());
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       19 (0x13)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarg.2
              IL_000d:  call       "string string.Concat(string, string)"
              IL_0012:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       19 (0x13)
              .maxstack  3
              IL_0000:  ldarg.2
              IL_0001:  ldarg.0
              IL_0002:  ldarg.1
              IL_0003:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0008:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000d:  call       "string string.Concat(string, string)"
              IL_0012:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatThree_UserInputOfSpanBasedConcat_ConcatWithChar()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var c = 'c';
                    Console.Write(M1(s1.AsSpan(), s2, c));
                    Console.Write(M2(s1.AsSpan(), s2, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, char c) => string.Concat(s1, s2.AsSpan()) + c;
                static string M2(ReadOnlySpan<char> s1, string s2, char c) => c + string.Concat(s1, s2.AsSpan());
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccab" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       32 (0x20)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0011:  ldarg.2
              IL_0012:  stloc.0
              IL_0013:  ldloca.s   V_0
              IL_0015:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001a:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001f:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       32 (0x20)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.2
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  ldarg.1
              IL_000b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0010:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001f:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    public void ConcatThree_UserInputOfSpanBasedConcat_ConcatWithChar_MissingMemberForSpanBasedConcat(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var c = 'c';
                    Console.Write(M1(s1.AsSpan(), s2, c));
                    Console.Write(M2(s1.AsSpan(), s2, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, char c) => string.Concat(s1, s2.AsSpan()) + c;
                static string M2(ReadOnlySpan<char> s1, string s2, char c) => c + string.Concat(s1, s2.AsSpan());
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((SpecialMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       25 (0x19)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarga.s   V_2
              IL_000e:  call       "string char.ToString()"
              IL_0013:  call       "string string.Concat(string, string)"
              IL_0018:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       25 (0x19)
              .maxstack  3
              IL_0000:  ldarga.s   V_2
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.1
              IL_0009:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0013:  call       "string string.Concat(string, string)"
              IL_0018:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatThree_MissingObjectToString()
    {
        var source = """
            using System;
            
            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                }
            
                static string M1(string s, char c) => c + s + s;
                static string M2(string s, char c) => s + c + s;
                static string M3(string s, char c) => s + s + c;
                static string M4(string s, char c) => c + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing(SpecialMember.System_Object__ToString);

        var verifier = CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsCoreClr ? "cssscsssccsc" : null, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("Test.M1", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  stloc.0
              IL_000e:  ldloca.s   V_0
              IL_0010:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       30 (0x1e)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.1
              IL_0010:  stloc.1
              IL_0011:  ldloca.s   V_1
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0018:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001d:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatThree_CharDoesntOverrideObjectToString()
    {
        var corlib_cs = """
            namespace System
            {
                public class Object
                {
                    public virtual string ToString() => null;
                }
                public class String
                {
                    public static string Concat(string str0, string str1) => null;
                    public static string Concat(string str0, string str1, string str2) => null;
                    public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1, ReadOnlySpan<char> str2) => null;
                    public static implicit operator ReadOnlySpan<char>(string value) => default;
                }
                public class ValueType { }
                public struct Char { }
                public struct Void { }
                public struct Int32 { }
                public struct Byte { }
                public struct Boolean { }
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(ref readonly T reference) { }
                }
                public class Enum : ValueType { }
                public class Attribute { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets validOn) { }

                    public bool AllowMultiple { get { return default; } set { } }
                    public bool Inherited { get { return default; } set { } }
                }
            }
            """;

        var corlib = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

        var source = """
            public class Test
            {
                static string M(string s, char c) => s + c + s;
            }
            """;

        var comp = CreateEmptyCompilation(source, [corlib]);
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation: comp, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);
        verifier.VerifyDiagnostics();

        // No matter whether `char` directly overrides `ToString` or not, we still produce span-based concat if we can
        verifier.VerifyIL("Test.M", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatThree_ReadOnlySpanConstructorParameterIsOrdinaryRef()
    {
        var corlib_cs = """
            namespace System
            {
                public class Object
                {
                    public virtual string ToString() => null;
                }
                public class String
                {
                    public static string Concat(string str0, string str1) => null;
                    public static string Concat(string str0, string str1, string str2) => null;
                    public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1, ReadOnlySpan<char> str2) => null;
                    public static implicit operator ReadOnlySpan<char>(string value) => default;
                }
                public class ValueType { }
                public struct Char
                {
                    public override string ToString() => null;
                }
                public struct Void { }
                public struct Int32 { }
                public struct Byte { }
                public struct Boolean { }
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(ref T reference) { }
                }
                public class Enum : ValueType { }
                public class Attribute { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets validOn) { }

                    public bool AllowMultiple { get { return default; } set { } }
                    public bool Inherited { get { return default; } set { } }
                }
            }
            """;

        var corlib = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

        var source = """
            public class Test
            {
                static string M(string s, char c) => s + c + s;
            }
            """;

        var comp = CreateEmptyCompilation(source, [corlib]);
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation: comp, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("Test.M", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatThree_ReadOnlySpanConstructorParameterIsOut()
    {
        var corlib_cs = """
            namespace System
            {
                public class Object
                {
                    public virtual string ToString() => null;
                }
                public class String
                {
                    public static string Concat(string str0, string str1) => null;
                    public static string Concat(string str0, string str1, string str2) => null;
                    public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1, ReadOnlySpan<char> str2) => null;
                    public static implicit operator ReadOnlySpan<char>(string value) => default;
                }
                public class ValueType { }
                public struct Char
                {
                    public override string ToString() => null;
                }
                public struct Void { }
                public struct Int32 { }
                public struct Byte { }
                public struct Boolean { }
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(out T reference) { reference = default; }
                }
                public class Enum : ValueType { }
                public class Attribute { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets validOn) { }

                    public bool AllowMultiple { get { return default; } set { } }
                    public bool Inherited { get { return default; } set { } }
                }
            }
            """;

        var corlib = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

        var source = """
            public class Test
            {
                static string M(string s, char c) => s + c + s;
            }
            """;

        var comp = CreateEmptyCompilation(source, [corlib]);
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation: comp, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);
        verifier.VerifyDiagnostics();

        // Constructor of ReadOnlySpan<char> has unexpected `out` reference. Fallback to string-based concat
        verifier.VerifyIL("Test.M", """
            {
              // Code size       15 (0xf)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  ldarg.0
              IL_0009:  call       "string string.Concat(string, string, string)"
              IL_000e:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatThree_Await()
    {
        var source = """
            using System;
            using System.Threading.Tasks;

            public class Test
            {
                static async Task Main()
                {
                    Console.Write(await M());
                }

                static async Task<string> M()
                {
                    return (await GetStringAsync()) + (await GetCharAsync()) + (await GetStringAsync());
                }

                static async Task<string> GetStringAsync()
                {
                    await Task.Yield();
                    return "s";
                }

                static async Task<char> GetCharAsync()
                {
                    await Task.Yield();
                    return 'c';
                }
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "scs" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
            {
              // Code size      398 (0x18e)
              .maxstack  3
              .locals init (int V_0,
                            string V_1,
                            char V_2,
                            string V_3,
                            System.Runtime.CompilerServices.TaskAwaiter<string> V_4,
                            System.Runtime.CompilerServices.TaskAwaiter<char> V_5,
                            System.Exception V_6)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int Test.<M>d__1.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  switch    (
                    IL_0052,
                    IL_00b5,
                    IL_0117)
                IL_0019:  call       "System.Threading.Tasks.Task<string> Test.GetStringAsync()"
                IL_001e:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<string> System.Threading.Tasks.Task<string>.GetAwaiter()"
                IL_0023:  stloc.s    V_4
                IL_0025:  ldloca.s   V_4
                IL_0027:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<string>.IsCompleted.get"
                IL_002c:  brtrue.s   IL_006f
                IL_002e:  ldarg.0
                IL_002f:  ldc.i4.0
                IL_0030:  dup
                IL_0031:  stloc.0
                IL_0032:  stfld      "int Test.<M>d__1.<>1__state"
                IL_0037:  ldarg.0
                IL_0038:  ldloc.s    V_4
                IL_003a:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_003f:  ldarg.0
                IL_0040:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_0045:  ldloca.s   V_4
                IL_0047:  ldarg.0
                IL_0048:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<string>, Test.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<string>, ref Test.<M>d__1)"
                IL_004d:  leave      IL_018d
                IL_0052:  ldarg.0
                IL_0053:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_0058:  stloc.s    V_4
                IL_005a:  ldarg.0
                IL_005b:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_0060:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<string>"
                IL_0066:  ldarg.0
                IL_0067:  ldc.i4.m1
                IL_0068:  dup
                IL_0069:  stloc.0
                IL_006a:  stfld      "int Test.<M>d__1.<>1__state"
                IL_006f:  ldarg.0
                IL_0070:  ldloca.s   V_4
                IL_0072:  call       "string System.Runtime.CompilerServices.TaskAwaiter<string>.GetResult()"
                IL_0077:  stfld      "string Test.<M>d__1.<>7__wrap2"
                IL_007c:  call       "System.Threading.Tasks.Task<char> Test.GetCharAsync()"
                IL_0081:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<char> System.Threading.Tasks.Task<char>.GetAwaiter()"
                IL_0086:  stloc.s    V_5
                IL_0088:  ldloca.s   V_5
                IL_008a:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<char>.IsCompleted.get"
                IL_008f:  brtrue.s   IL_00d2
                IL_0091:  ldarg.0
                IL_0092:  ldc.i4.1
                IL_0093:  dup
                IL_0094:  stloc.0
                IL_0095:  stfld      "int Test.<M>d__1.<>1__state"
                IL_009a:  ldarg.0
                IL_009b:  ldloc.s    V_5
                IL_009d:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_00a2:  ldarg.0
                IL_00a3:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_00a8:  ldloca.s   V_5
                IL_00aa:  ldarg.0
                IL_00ab:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<char>, Test.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<char>, ref Test.<M>d__1)"
                IL_00b0:  leave      IL_018d
                IL_00b5:  ldarg.0
                IL_00b6:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_00bb:  stloc.s    V_5
                IL_00bd:  ldarg.0
                IL_00be:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_00c3:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<char>"
                IL_00c9:  ldarg.0
                IL_00ca:  ldc.i4.m1
                IL_00cb:  dup
                IL_00cc:  stloc.0
                IL_00cd:  stfld      "int Test.<M>d__1.<>1__state"
                IL_00d2:  ldloca.s   V_5
                IL_00d4:  call       "char System.Runtime.CompilerServices.TaskAwaiter<char>.GetResult()"
                IL_00d9:  stloc.2
                IL_00da:  ldarg.0
                IL_00db:  ldloc.2
                IL_00dc:  stfld      "char Test.<M>d__1.<>7__wrap1"
                IL_00e1:  call       "System.Threading.Tasks.Task<string> Test.GetStringAsync()"
                IL_00e6:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<string> System.Threading.Tasks.Task<string>.GetAwaiter()"
                IL_00eb:  stloc.s    V_4
                IL_00ed:  ldloca.s   V_4
                IL_00ef:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<string>.IsCompleted.get"
                IL_00f4:  brtrue.s   IL_0134
                IL_00f6:  ldarg.0
                IL_00f7:  ldc.i4.2
                IL_00f8:  dup
                IL_00f9:  stloc.0
                IL_00fa:  stfld      "int Test.<M>d__1.<>1__state"
                IL_00ff:  ldarg.0
                IL_0100:  ldloc.s    V_4
                IL_0102:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_0107:  ldarg.0
                IL_0108:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_010d:  ldloca.s   V_4
                IL_010f:  ldarg.0
                IL_0110:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<string>, Test.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<string>, ref Test.<M>d__1)"
                IL_0115:  leave.s    IL_018d
                IL_0117:  ldarg.0
                IL_0118:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_011d:  stloc.s    V_4
                IL_011f:  ldarg.0
                IL_0120:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_0125:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<string>"
                IL_012b:  ldarg.0
                IL_012c:  ldc.i4.m1
                IL_012d:  dup
                IL_012e:  stloc.0
                IL_012f:  stfld      "int Test.<M>d__1.<>1__state"
                IL_0134:  ldloca.s   V_4
                IL_0136:  call       "string System.Runtime.CompilerServices.TaskAwaiter<string>.GetResult()"
                IL_013b:  stloc.3
                IL_013c:  ldarg.0
                IL_013d:  ldfld      "string Test.<M>d__1.<>7__wrap2"
                IL_0142:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
                IL_0147:  ldarg.0
                IL_0148:  ldflda     "char Test.<M>d__1.<>7__wrap1"
                IL_014d:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
                IL_0152:  ldloc.3
                IL_0153:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
                IL_0158:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
                IL_015d:  stloc.1
                IL_015e:  leave.s    IL_0179
              }
              catch System.Exception
              {
                IL_0160:  stloc.s    V_6
                IL_0162:  ldarg.0
                IL_0163:  ldc.i4.s   -2
                IL_0165:  stfld      "int Test.<M>d__1.<>1__state"
                IL_016a:  ldarg.0
                IL_016b:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_0170:  ldloc.s    V_6
                IL_0172:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetException(System.Exception)"
                IL_0177:  leave.s    IL_018d
              }
              IL_0179:  ldarg.0
              IL_017a:  ldc.i4.s   -2
              IL_017c:  stfld      "int Test.<M>d__1.<>1__state"
              IL_0181:  ldarg.0
              IL_0182:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
              IL_0187:  ldloc.1
              IL_0188:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetResult(string)"
              IL_018d:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatThree_UserDefinedReadOnlySpan()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                }

                static string M1(string s, char c) => c + s + s;
                static string M2(string s, char c) => s + c + s;
                static string M3(string s, char c) => s + s + c;
                static string M4(string s, char c) => c + s + c;
            }

            namespace System
            {
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(ref readonly T reference) { }
                }
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "cssscsssccsc" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        comp.VerifyIL("Test.M3", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  stloc.0
              IL_000e:  ldloca.s   V_0
              IL_0010:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        comp.VerifyIL("Test.M4", """
            {
              // Code size       30 (0x1e)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.1
              IL_0010:  stloc.1
              IL_0011:  ldloca.s   V_1
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0018:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001d:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatFour_ReadOnlySpan1(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                    Console.Write(M5(s, c));
                    Console.Write(M6(s, c));
                    Console.Write(M7(s, c));
                }

                static string M1(string s, char c) => c + s + s + s;
                static string M2(string s, char c) => s + c + s + s;
                static string M3(string s, char c) => s + s + c + s;
                static string M4(string s, char c) => s + s + s + c;
                static string M5(string s, char c) => c + s + c + s;
                static string M6(string s, char c) => s + c + s + c;
                static string M7(string s, char c) => c + s + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "csssscsssscssssccscsscsccssc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  stloc.0
              IL_000e:  ldloca.s   V_0
              IL_0010:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.0
              IL_000d:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0012:  ldarg.1
              IL_0013:  stloc.0
              IL_0014:  ldloca.s   V_0
              IL_0016:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.1
              IL_0010:  stloc.1
              IL_0011:  ldloca.s   V_1
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0018:  ldarg.0
              IL_0019:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.1
              IL_0016:  stloc.1
              IL_0017:  ldloca.s   V_1
              IL_0019:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);
        verifier.VerifyIL("Test.M7", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.1
              IL_0016:  stloc.1
              IL_0017:  ldloca.s   V_1
              IL_0019:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatFour_ReadOnlySpan2(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'C';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                    Console.Write(M5(s, c));
                    Console.Write(M6(s, c));
                    Console.Write(M7(s, c));
                }

                static string M1(string s, char c) => char.ToLowerInvariant(c) + s + s + s;
                static string M2(string s, char c) => s + char.ToLowerInvariant(c) + s + s;
                static string M3(string s, char c) => s + s + char.ToLowerInvariant(c) + s;
                static string M4(string s, char c) => s + s + s + char.ToLowerInvariant(c);
                static string M5(string s, char c) => char.ToLowerInvariant(c) + s + char.ToLowerInvariant(c) + s;
                static string M6(string s, char c) => s + char.ToLowerInvariant(c) + s + char.ToLowerInvariant(c);
                static string M7(string s, char c) => char.ToLowerInvariant(c) + s + s + char.ToLowerInvariant(c);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "csssscsssscssssccscsscsccssc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  ldarg.0
              IL_001b:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  call       "char char.ToLowerInvariant(char)"
              IL_000c:  stloc.0
              IL_000d:  ldloca.s   V_0
              IL_000f:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  ldarg.0
              IL_001b:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  call       "char char.ToLowerInvariant(char)"
              IL_0012:  stloc.0
              IL_0013:  ldloca.s   V_0
              IL_0015:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001a:  ldarg.0
              IL_001b:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.0
              IL_000d:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0012:  ldarg.1
              IL_0013:  call       "char char.ToLowerInvariant(char)"
              IL_0018:  stloc.0
              IL_0019:  ldloca.s   V_0
              IL_001b:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       46 (0x2e)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  ldarg.1
              IL_0015:  call       "char char.ToLowerInvariant(char)"
              IL_001a:  stloc.1
              IL_001b:  ldloca.s   V_1
              IL_001d:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0022:  ldarg.0
              IL_0023:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0028:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002d:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       46 (0x2e)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  call       "char char.ToLowerInvariant(char)"
              IL_000c:  stloc.0
              IL_000d:  ldloca.s   V_0
              IL_000f:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  ldarg.1
              IL_001b:  call       "char char.ToLowerInvariant(char)"
              IL_0020:  stloc.1
              IL_0021:  ldloca.s   V_1
              IL_0023:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0028:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002d:  ret
            }
            """);
        verifier.VerifyIL("Test.M7", """
            {
              // Code size       46 (0x2e)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  ldarg.1
              IL_001b:  call       "char char.ToLowerInvariant(char)"
              IL_0020:  stloc.1
              IL_0021:  ldloca.s   V_1
              IL_0023:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0028:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002d:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData("(s + c) + s + s")]
    [InlineData("s + (c + s) + s")]
    [InlineData("s + c + (s + s)")]
    [InlineData("(s + c + s) + s")]
    [InlineData("s + (c + s + s)")]
    [InlineData("(s + c) + (s + s)")]
    [InlineData("string.Concat(s, c.ToString()) + s + s")]
    [InlineData("s + string.Concat(c.ToString(), s) + s")]
    [InlineData("s + c + string.Concat(s, s)")]
    [InlineData("string.Concat(s, c.ToString(), s) + s")]
    [InlineData("s + string.Concat(c.ToString(), s, s)")]
    [InlineData("string.Concat(s, c.ToString()) + string.Concat(s, s)")]
    public void ConcatFour_ReadOnlySpan_OperandGroupingAndUserInputOfStringBasedConcats(string expression)
    {
        var source = $$"""
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M(s, c));
                }

                static string M(string s, char c) => {{expression}};
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "scss" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatFour_ReadOnlySpan_SideEffect(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                private static int stringCounter;
                private static int charCounterPlusOne = 1;

                static void Main()
                {
                    Console.WriteLine(M1());
                    Console.WriteLine(M2());
                    Console.WriteLine(M3());
                    Console.WriteLine(M4());
                    Console.WriteLine(M5());
                    Console.WriteLine(M6());
                    Console.WriteLine(M7());
                }

                static string M1() => GetCharWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect();
                static string M2() => GetStringWithSideEffect() + GetCharWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect();
                static string M3() => GetStringWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect() + GetStringWithSideEffect();
                static string M4() => GetStringWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect();
                static string M5() => GetCharWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect() + GetStringWithSideEffect();
                static string M6() => GetStringWithSideEffect() + GetCharWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect();
                static string M7() => GetCharWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect();

                private static string GetStringWithSideEffect()
                {
                    Console.Write(stringCounter++);
                    return "s";
                }

                private static char GetCharWithSideEffect()
                {
                    Console.Write(charCounterPlusOne++);
                    return 'c';
                }
            }
            """;

        var expectedOutput = """
            1012csss
            3245scss
            6738sscs
            910114sssc
            512613cscs
            147158scsc
            9161710cssc
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? expectedOutput : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       49 (0x31)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "string Test.GetStringWithSideEffect()"
              IL_0026:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_002b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0030:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       49 (0x31)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "char Test.GetCharWithSideEffect()"
              IL_000f:  stloc.0
              IL_0010:  ldloca.s   V_0
              IL_0012:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "string Test.GetStringWithSideEffect()"
              IL_0026:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_002b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0030:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       49 (0x31)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "string Test.GetStringWithSideEffect()"
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  call       "char Test.GetCharWithSideEffect()"
              IL_0019:  stloc.0
              IL_001a:  ldloca.s   V_0
              IL_001c:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0021:  call       "string Test.GetStringWithSideEffect()"
              IL_0026:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_002b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0030:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       49 (0x31)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "string Test.GetStringWithSideEffect()"
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  call       "string Test.GetStringWithSideEffect()"
              IL_0019:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001e:  call       "char Test.GetCharWithSideEffect()"
              IL_0023:  stloc.0
              IL_0024:  ldloca.s   V_0
              IL_0026:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_002b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0030:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       52 (0x34)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "char Test.GetCharWithSideEffect()"
              IL_001c:  stloc.1
              IL_001d:  ldloca.s   V_1
              IL_001f:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0024:  call       "string Test.GetStringWithSideEffect()"
              IL_0029:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_002e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0033:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       52 (0x34)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "char Test.GetCharWithSideEffect()"
              IL_000f:  stloc.0
              IL_0010:  ldloca.s   V_0
              IL_0012:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "char Test.GetCharWithSideEffect()"
              IL_0026:  stloc.1
              IL_0027:  ldloca.s   V_1
              IL_0029:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_002e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0033:  ret
            }
            """);
        verifier.VerifyIL("Test.M7", """
            {
              // Code size       52 (0x34)
              .maxstack  4
              .locals init (char V_0,
                          char V_1)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "char Test.GetCharWithSideEffect()"
              IL_0026:  stloc.1
              IL_0027:  ldloca.s   V_1
              IL_0029:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_002e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0033:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatFour_ReadOnlySpan_ReferenceToSameLocation(int? missingUnimportantMember)
    {
        var source = """
            using System;

            var c = new C();
            c.M();

            class C
            {
                public char c = 'a';

                public ref char GetC() => ref c;

                public ref char GetC2()
                {
                    c = 'b';
                    return ref c;
                }

                public void M()
                {
                    Console.Write("a" + c + GetC() + GetC2());
                }
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "aaab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       65 (0x41)
              .maxstack  4
              .locals init (char V_0,
                            char V_1,
                            char V_2)
              IL_0000:  ldstr      "a"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  ldarg.0
              IL_000b:  ldfld      "char C.c"
              IL_0010:  stloc.0
              IL_0011:  ldloca.s   V_0
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0018:  ldarg.0
              IL_0019:  call       "ref char C.GetC()"
              IL_001e:  ldind.u2
              IL_001f:  stloc.1
              IL_0020:  ldloca.s   V_1
              IL_0022:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0027:  ldarg.0
              IL_0028:  call       "ref char C.GetC2()"
              IL_002d:  ldind.u2
              IL_002e:  stloc.2
              IL_002f:  ldloca.s   V_2
              IL_0031:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0036:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_003b:  call       "void System.Console.Write(string)"
              IL_0040:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatFour_ReadOnlySpan_MutateLocal(int? missingUnimportantMember)
    {
        var source = """
            using System;

            var c = new C();
            Console.WriteLine(c.M());

            class C
            {
                public string M()
                {
                    var c1 = 'a';
                    var c2 = 'a';
                    return c1 + SneakyLocalChange(ref c1) + c2 + SneakyLocalChange(ref c2);
                }

                private string SneakyLocalChange(ref char local)
                {
                    local = 'b';
                    return "b";
                }
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       56 (0x38)
              .maxstack  5
              .locals init (char V_0, //c1
                            char V_1, //c2
                            char V_2,
                            char V_3)
              IL_0000:  ldc.i4.s   97
              IL_0002:  stloc.0
              IL_0003:  ldc.i4.s   97
              IL_0005:  stloc.1
              IL_0006:  ldloc.0
              IL_0007:  stloc.2
              IL_0008:  ldloca.s   V_2
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  ldloca.s   V_0
              IL_0012:  call       "string C.SneakyLocalChange(ref char)"
              IL_0017:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001c:  ldloc.1
              IL_001d:  stloc.3
              IL_001e:  ldloca.s   V_3
              IL_0020:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0025:  ldarg.0
              IL_0026:  ldloca.s   V_1
              IL_0028:  call       "string C.SneakyLocalChange(ref char)"
              IL_002d:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0032:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0037:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_4ReadOnlySpans)]
    public void ConcatFour_ReadOnlySpan_NullConcatArgument(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                    Console.Write(M5(s, c));
                    Console.Write(M6(s, c));
                }

                static string M1(string s, char c) => (string)null + s + c + s;
                static string M2(string s, char c) => s + (string)null + c + s;
                static string M3(string s, char c) => s + ((string)null + c) + s;
                static string M4(string s, char c) => s + c + (string)null + s;
                static string M5(string s, char c) => s + (c + (string)null) + s;
                static string M6(string s, char c) => s + c + s + (string)null;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "scsscsscsscsscsscs" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        var expectedEquivalentIL = """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """;

        verifier.VerifyIL("Test.M1", expectedEquivalentIL);
        verifier.VerifyIL("Test.M2", expectedEquivalentIL);
        verifier.VerifyIL("Test.M3", expectedEquivalentIL);
        verifier.VerifyIL("Test.M4", expectedEquivalentIL);
        verifier.VerifyIL("Test.M5", expectedEquivalentIL);
        verifier.VerifyIL("Test.M6", expectedEquivalentIL);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_4ReadOnlySpans)]
    public void ConcatFour_ConstantCharToString(int? missingUnimportantMember)
    {
        var source = """
            using System;
            
            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    Console.Write(M1(s));
                    Console.Write(M2(s));
                    Console.Write(M3(s));
                    Console.Write(M4(s));
                    Console.Write(M5(s));
                    Console.Write(M6(s));
                    Console.Write(M7(s));
                }
            
                static string M1(string s) => 'c'.ToString() + s + s + s;
                static string M2(string s) => s + 'c'.ToString() + s + s;
                static string M3(string s) => s + s + 'c'.ToString() + s;
                static string M4(string s) => s + s + s + 'c'.ToString();
                static string M5(string s) => 'c'.ToString() + s + 'c'.ToString() + s;
                static string M6(string s) => s + 'c'.ToString() + s + 'c'.ToString();
                static string M7(string s) => 'c'.ToString() + s + s + 'c'.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "csssscsssscssssccscsscsccssc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // Instead of emitting this as a span-based concat of strings and chars we recognize "constantChar.ToString()" pattern and lower that arguments to constant strings
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       14 (0xe)
              .maxstack  4
              IL_0000:  ldstr      "c"
              IL_0005:  ldarg.0
              IL_0006:  ldarg.0
              IL_0007:  ldarg.0
              IL_0008:  call       "string string.Concat(string, string, string, string)"
              IL_000d:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       14 (0xe)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldstr      "c"
              IL_0006:  ldarg.0
              IL_0007:  ldarg.0
              IL_0008:  call       "string string.Concat(string, string, string, string)"
              IL_000d:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       14 (0xe)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  ldstr      "c"
              IL_0007:  ldarg.0
              IL_0008:  call       "string string.Concat(string, string, string, string)"
              IL_000d:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       14 (0xe)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  ldarg.0
              IL_0003:  ldstr      "c"
              IL_0008:  call       "string string.Concat(string, string, string, string)"
              IL_000d:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       18 (0x12)
              .maxstack  4
              IL_0000:  ldstr      "c"
              IL_0005:  ldarg.0
              IL_0006:  ldstr      "c"
              IL_000b:  ldarg.0
              IL_000c:  call       "string string.Concat(string, string, string, string)"
              IL_0011:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       18 (0x12)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldstr      "c"
              IL_0006:  ldarg.0
              IL_0007:  ldstr      "c"
              IL_000c:  call       "string string.Concat(string, string, string, string)"
              IL_0011:  ret
            }
            """);
        verifier.VerifyIL("Test.M7", """
            {
              // Code size       18 (0x12)
              .maxstack  4
              IL_0000:  ldstr      "c"
              IL_0005:  ldarg.0
              IL_0006:  ldarg.0
              IL_0007:  ldstr      "c"
              IL_000c:  call       "string string.Concat(string, string, string, string)"
              IL_0011:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_4ReadOnlySpans)]
    public void ConcatFour_AllConstantCharToStrings(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    Console.Write(M());
                }

                static string M() => 'a'.ToString() + 'b'.ToString() + 'c'.ToString() + 'd'.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcd" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // Instead of emitting this as a span-based concat of 4 chars we recognize "constantChar.ToString()" pattern and lower all arguments to a constant string
        // which we can then fold into a single constant string and avoid concatenation entirely
        verifier.VerifyIL("Test.M", """
            {
              // Code size        6 (0x6)
              .maxstack  1
              IL_0000:  ldstr      "abcd"
              IL_0005:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatFourCharToStrings(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var c1 = 'a';
                    var c2 = 'b';
                    var c3 = 'c';
                    var c4 = 'd';
                    Console.Write(M(c1, c2, c3, c4));
                }

                static string M(char c1, char c2, char c3, char c4) => c1.ToString() + c2.ToString() + c3.ToString() + c4.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcd" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size       42 (0x2a)
              .maxstack  4
              .locals init (char V_0,
                            char V_1,
                            char V_2,
                            char V_3)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.1
              IL_000a:  stloc.1
              IL_000b:  ldloca.s   V_1
              IL_000d:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0012:  ldarg.2
              IL_0013:  stloc.2
              IL_0014:  ldloca.s   V_2
              IL_0016:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001b:  ldarg.3
              IL_001c:  stloc.3
              IL_001d:  ldloca.s   V_3
              IL_001f:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0024:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0029:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)SpecialMember.System_String__Concat_4ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    public void ConcatFour_ReadOnlySpan_MissingMemberForOptimization(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                    Console.Write(M5(s, c));
                    Console.Write(M6(s, c));
                    Console.Write(M7(s, c));
                }

                static string M1(string s, char c) => c + s + s + s;
                static string M2(string s, char c) => s + c + s + s;
                static string M3(string s, char c) => s + s + c + s;
                static string M4(string s, char c) => s + s + s + c;
                static string M5(string s, char c) => c + s + c + s;
                static string M6(string s, char c) => s + c + s + c;
                static string M7(string s, char c) => c + s + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((SpecialMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "csssscsssscssssccscsscsccssc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       16 (0x10)
              .maxstack  4
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.0
              IL_0009:  ldarg.0
              IL_000a:  call       "string string.Concat(string, string, string, string)"
              IL_000f:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       16 (0x10)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  ldarg.0
              IL_0009:  ldarg.0
              IL_000a:  call       "string string.Concat(string, string, string, string)"
              IL_000f:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       16 (0x10)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  ldarga.s   V_1
              IL_0004:  call       "string char.ToString()"
              IL_0009:  ldarg.0
              IL_000a:  call       "string string.Concat(string, string, string, string)"
              IL_000f:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       16 (0x10)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  ldarg.0
              IL_0003:  ldarga.s   V_1
              IL_0005:  call       "string char.ToString()"
              IL_000a:  call       "string string.Concat(string, string, string, string)"
              IL_000f:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       22 (0x16)
              .maxstack  4
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarga.s   V_1
              IL_000a:  call       "string char.ToString()"
              IL_000f:  ldarg.0
              IL_0010:  call       "string string.Concat(string, string, string, string)"
              IL_0015:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       22 (0x16)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  ldarg.0
              IL_0009:  ldarga.s   V_1
              IL_000b:  call       "string char.ToString()"
              IL_0010:  call       "string string.Concat(string, string, string, string)"
              IL_0015:  ret
            }
            """);
        verifier.VerifyIL("Test.M7", """
            {
              // Code size       22 (0x16)
              .maxstack  4
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.0
              IL_0009:  ldarga.s   V_1
              IL_000b:  call       "string char.ToString()"
              IL_0010:  call       "string string.Concat(string, string, string, string)"
              IL_0015:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf2_ConcatWithString(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    Console.Write(M1(s1.AsSpan(), s2, s3));
                    Console.Write(M2(s1.AsSpan(), s2, s3));
                    Console.Write(M3(s1.AsSpan(), s2, s3));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, string s3) => string.Concat(s1, s2.AsSpan()) + s3 + s3;
                static string M2(ReadOnlySpan<char> s1, string s2, string s3) => s3 + s3 + string.Concat(s1, s2.AsSpan());
                static string M3(ReadOnlySpan<char> s1, string s2, string s3) => s3 + string.Concat(s1, s2.AsSpan()) + s3;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccccabcabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       20 (0x14)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarg.2
              IL_000d:  ldarg.2
              IL_000e:  call       "string string.Concat(string, string, string)"
              IL_0013:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       20 (0x14)
              .maxstack  4
              IL_0000:  ldarg.2
              IL_0001:  ldarg.2
              IL_0002:  ldarg.0
              IL_0003:  ldarg.1
              IL_0004:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0009:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000e:  call       "string string.Concat(string, string, string)"
              IL_0013:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       20 (0x14)
              .maxstack  3
              IL_0000:  ldarg.2
              IL_0001:  ldarg.0
              IL_0002:  ldarg.1
              IL_0003:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0008:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000d:  ldarg.2
              IL_000e:  call       "string string.Concat(string, string, string)"
              IL_0013:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatFour_UserInputOfSpanBasedConcatOf2_ConcatWithChar()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var c = 'c';
                    Console.Write(M1(s1.AsSpan(), s2, c));
                    Console.Write(M2(s1.AsSpan(), s2, c));
                    Console.Write(M3(s1.AsSpan(), s2, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, char c) => string.Concat(s1, s2.AsSpan()) + c + c;
                static string M2(ReadOnlySpan<char> s1, string s2, char c) => c.ToString() + c + string.Concat(s1, s2.AsSpan());
                static string M3(ReadOnlySpan<char> s1, string s2, char c) => c + string.Concat(s1, s2.AsSpan()) + c;
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccccabcabc" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       41 (0x29)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0011:  ldarg.2
              IL_0012:  stloc.0
              IL_0013:  ldloca.s   V_0
              IL_0015:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001a:  ldarg.2
              IL_001b:  stloc.1
              IL_001c:  ldloca.s   V_1
              IL_001e:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0023:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0028:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       41 (0x29)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.2
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.2
              IL_000a:  stloc.1
              IL_000b:  ldloca.s   V_1
              IL_000d:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0012:  ldarg.0
              IL_0013:  ldarg.1
              IL_0014:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0019:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001e:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0023:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0028:  ret
            }
            """);
        comp.VerifyIL("Test.M3", """
            {
              // Code size       41 (0x29)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.2
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  ldarg.1
              IL_000b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0010:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  ldarg.2
              IL_001b:  stloc.1
              IL_001c:  ldloca.s   V_1
              IL_001e:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0023:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0028:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatFour_UserInputOfSpanBasedConcatOf2_ConcatWithStringAndChar()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var c = 'd';
                    Console.Write(M1(s1.AsSpan(), s2, s3, c));
                    Console.Write(M2(s1.AsSpan(), s2, s3, c));
                    Console.Write(M3(s1.AsSpan(), s2, s3, c));
                    Console.Write(M4(s1.AsSpan(), s2, s3, c));
                    Console.Write(M5(s1.AsSpan(), s2, s3, c));
                    Console.Write(M6(s1.AsSpan(), s2, s3, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, string s3, char c) => string.Concat(s1, s2.AsSpan()) + s3 + c;
                static string M2(ReadOnlySpan<char> s1, string s2, string s3, char c) => string.Concat(s1, s2.AsSpan()) + c + s3;
                static string M3(ReadOnlySpan<char> s1, string s2, string s3, char c) => s3 + c + string.Concat(s1, s2.AsSpan());
                static string M4(ReadOnlySpan<char> s1, string s2, string s3, char c) => c + s3 + string.Concat(s1, s2.AsSpan());
                static string M5(ReadOnlySpan<char> s1, string s2, string s3, char c) => s3 + string.Concat(s1, s2.AsSpan()) + c;
                static string M6(ReadOnlySpan<char> s1, string s2, string s3, char c) => c + string.Concat(s1, s2.AsSpan()) + s3;
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcdabdccdabdcabcabddabc" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       38 (0x26)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0011:  ldarg.2
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  ldarg.3
              IL_0018:  stloc.0
              IL_0019:  ldloca.s   V_0
              IL_001b:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       38 (0x26)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0011:  ldarg.3
              IL_0012:  stloc.0
              IL_0013:  ldloca.s   V_0
              IL_0015:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001a:  ldarg.2
              IL_001b:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        comp.VerifyIL("Test.M3", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.2
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.3
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  ldarg.1
              IL_0011:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0016:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001b:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        comp.VerifyIL("Test.M4", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.3
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.2
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  ldarg.1
              IL_0011:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0016:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001b:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        comp.VerifyIL("Test.M5", """
            {
              // Code size       38 (0x26)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.2
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  ldarg.1
              IL_0008:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000d:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  ldarg.3
              IL_0018:  stloc.0
              IL_0019:  ldloca.s   V_0
              IL_001b:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        comp.VerifyIL("Test.M6", """
            {
              // Code size       38 (0x26)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.3
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  ldarg.1
              IL_000b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0010:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  ldarg.2
              IL_001b:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    public void ConcatFour_TwoUserInputsOfSpanBasedConcatOf2(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var s4 = "d";
                    Console.Write(M(s1.AsSpan(), s2.AsSpan(), s3.AsSpan(), s4.AsSpan()));
                }

                static string M(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3, ReadOnlySpan<char> s4) => string.Concat(s1, s2) + string.Concat(s3, s4);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcd" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size       20 (0x14)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0007:  ldarg.2
              IL_0008:  ldarg.3
              IL_0009:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000e:  call       "string string.Concat(string, string)"
              IL_0013:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf3_ConcatWithString(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var s4 = "d";
                    Console.Write(M1(s1.AsSpan(), s2, s3.AsSpan(), s4));
                    Console.Write(M2(s1.AsSpan(), s2, s3.AsSpan(), s4));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, string s4) => string.Concat(s1, s2.AsSpan(), s3) + s4;
                static string M2(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, string s4) => s4 + string.Concat(s1, s2.AsSpan(), s3);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcddabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       20 (0x14)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000d:  ldarg.3
              IL_000e:  call       "string string.Concat(string, string)"
              IL_0013:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       20 (0x14)
              .maxstack  4
              IL_0000:  ldarg.3
              IL_0001:  ldarg.0
              IL_0002:  ldarg.1
              IL_0003:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0008:  ldarg.2
              IL_0009:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000e:  call       "string string.Concat(string, string)"
              IL_0013:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatFour_UserInputOfSpanBasedConcatOf3_ConcatWithChar()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var c = 'd';
                    Console.Write(M1(s1.AsSpan(), s2, s3.AsSpan(), c));
                    Console.Write(M2(s1.AsSpan(), s2, s3.AsSpan(), c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, char c) => string.Concat(s1, s2.AsSpan(), s3) + c;
                static string M2(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, char c) => c + string.Concat(s1, s2.AsSpan(), s3);
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcddabc" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       33 (0x21)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000d:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0012:  ldarg.3
              IL_0013:  stloc.0
              IL_0014:  ldloca.s   V_0
              IL_0016:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.3
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  ldarg.1
              IL_000b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0010:  ldarg.2
              IL_0011:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf2_ConcatWithChar_MissingMemberForSpanBasedConcatConcat(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var c = 'c';
                    Console.Write(M1(s1.AsSpan(), s2, c));
                    Console.Write(M2(s1.AsSpan(), s2, c));
                    Console.Write(M3(s1.AsSpan(), s2, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, char c) => string.Concat(s1, s2.AsSpan()) + c + c;
                static string M2(ReadOnlySpan<char> s1, string s2, char c) => c.ToString() + c + string.Concat(s1, s2.AsSpan());
                static string M3(ReadOnlySpan<char> s1, string s2, char c) => c + string.Concat(s1, s2.AsSpan()) + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((SpecialMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccccabcabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       32 (0x20)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarga.s   V_2
              IL_000e:  call       "string char.ToString()"
              IL_0013:  ldarga.s   V_2
              IL_0015:  call       "string char.ToString()"
              IL_001a:  call       "string string.Concat(string, string, string)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       32 (0x20)
              .maxstack  4
              IL_0000:  ldarga.s   V_2
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarga.s   V_2
              IL_0009:  call       "string char.ToString()"
              IL_000e:  ldarg.0
              IL_000f:  ldarg.1
              IL_0010:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  call       "string string.Concat(string, string, string)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       32 (0x20)
              .maxstack  3
              IL_0000:  ldarga.s   V_2
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.1
              IL_0009:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0013:  ldarga.s   V_2
              IL_0015:  call       "string char.ToString()"
              IL_001a:  call       "string string.Concat(string, string, string)"
              IL_001f:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf2_ConcatWithStringAndChar_MissingMemberForSpanBasedConcat(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var c = 'd';
                    Console.Write(M1(s1.AsSpan(), s2, s3, c));
                    Console.Write(M2(s1.AsSpan(), s2, s3, c));
                    Console.Write(M3(s1.AsSpan(), s2, s3, c));
                    Console.Write(M4(s1.AsSpan(), s2, s3, c));
                    Console.Write(M5(s1.AsSpan(), s2, s3, c));
                    Console.Write(M6(s1.AsSpan(), s2, s3, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, string s3, char c) => string.Concat(s1, s2.AsSpan()) + s3 + c;
                static string M2(ReadOnlySpan<char> s1, string s2, string s3, char c) => string.Concat(s1, s2.AsSpan()) + c + s3;
                static string M3(ReadOnlySpan<char> s1, string s2, string s3, char c) => s3 + c + string.Concat(s1, s2.AsSpan());
                static string M4(ReadOnlySpan<char> s1, string s2, string s3, char c) => c + s3 + string.Concat(s1, s2.AsSpan());
                static string M5(ReadOnlySpan<char> s1, string s2, string s3, char c) => s3 + string.Concat(s1, s2.AsSpan()) + c;
                static string M6(ReadOnlySpan<char> s1, string s2, string s3, char c) => c + string.Concat(s1, s2.AsSpan()) + s3;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((SpecialMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcdabdccdabdcabcabddabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       26 (0x1a)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarg.2
              IL_000d:  ldarga.s   V_3
              IL_000f:  call       "string char.ToString()"
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       26 (0x1a)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarga.s   V_3
              IL_000e:  call       "string char.ToString()"
              IL_0013:  ldarg.2
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       26 (0x1a)
              .maxstack  4
              IL_0000:  ldarg.2
              IL_0001:  ldarga.s   V_3
              IL_0003:  call       "string char.ToString()"
              IL_0008:  ldarg.0
              IL_0009:  ldarg.1
              IL_000a:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       26 (0x1a)
              .maxstack  4
              IL_0000:  ldarga.s   V_3
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.2
              IL_0008:  ldarg.0
              IL_0009:  ldarg.1
              IL_000a:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       26 (0x1a)
              .maxstack  3
              IL_0000:  ldarg.2
              IL_0001:  ldarg.0
              IL_0002:  ldarg.1
              IL_0003:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0008:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000d:  ldarga.s   V_3
              IL_000f:  call       "string char.ToString()"
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       26 (0x1a)
              .maxstack  3
              IL_0000:  ldarga.s   V_3
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.1
              IL_0009:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0013:  ldarg.2
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf3_ConcatWithChar_MissingMemberForSpanBasedConcat(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var c = 'd';
                    Console.Write(M1(s1.AsSpan(), s2, s3.AsSpan(), c));
                    Console.Write(M2(s1.AsSpan(), s2, s3.AsSpan(), c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, char c) => string.Concat(s1, s2.AsSpan(), s3) + c;
                static string M2(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, char c) => c + string.Concat(s1, s2.AsSpan(), s3);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((SpecialMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcddabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       26 (0x1a)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000d:  ldarga.s   V_3
              IL_000f:  call       "string char.ToString()"
              IL_0014:  call       "string string.Concat(string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       26 (0x1a)
              .maxstack  4
              IL_0000:  ldarga.s   V_3
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.1
              IL_0009:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000e:  ldarg.2
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  call       "string string.Concat(string, string)"
              IL_0019:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatFour_MissingObjectToString()
    {
        var source = """
            using System;
            
            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                    Console.Write(M5(s, c));
                    Console.Write(M6(s, c));
                    Console.Write(M7(s, c));
                }
            
                static string M1(string s, char c) => c + s + s + s;
                static string M2(string s, char c) => s + c + s + s;
                static string M3(string s, char c) => s + s + c + s;
                static string M4(string s, char c) => s + s + s + c;
                static string M5(string s, char c) => c + s + c + s;
                static string M6(string s, char c) => s + c + s + c;
                static string M7(string s, char c) => c + s + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing(SpecialMember.System_Object__ToString);

        // We don't use object.ToString() or char.ToString() in the final codegen.
        var verifier = CompileAndVerify(compilation: comp, expectedOutput: ExecutionConditionUtil.IsCoreClr ? "csssscsssscssssccscsscsccssc" : null, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("Test.M1", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);

        verifier.VerifyIL("Test.M2", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);

        verifier.VerifyIL("Test.M3", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  stloc.0
              IL_000e:  ldloca.s   V_0
              IL_0010:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);

        verifier.VerifyIL("Test.M4", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.0
              IL_000d:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0012:  ldarg.1
              IL_0013:  stloc.0
              IL_0014:  ldloca.s   V_0
              IL_0016:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);

        verifier.VerifyIL("Test.M5", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.1
              IL_0010:  stloc.1
              IL_0011:  ldloca.s   V_1
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0018:  ldarg.0
              IL_0019:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);

        verifier.VerifyIL("Test.M6", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.1
              IL_0016:  stloc.1
              IL_0017:  ldloca.s   V_1
              IL_0019:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);

        verifier.VerifyIL("Test.M7", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.1
              IL_0016:  stloc.1
              IL_0017:  ldloca.s   V_1
              IL_0019:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatFour_CharDoesntOverrideObjectToString()
    {
        var corlib_cs = """
            namespace System
            {
                public class Object
                {
                    public virtual string ToString() => null;
                }
                public class String
                {
                    public static string Concat(string str0, string str1) => null;
                    public static string Concat(string str0, string str1, string str2) => null;
                    public static string Concat(string str0, string str1, string str2, string str3) => null;
                    public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1, ReadOnlySpan<char> str2, ReadOnlySpan<char> str3) => null;
                    public static implicit operator ReadOnlySpan<char>(string value) => default;
                }
                public class ValueType { }
                public struct Char { }
                public struct Void { }
                public struct Int32 { }
                public struct Byte { }
                public struct Boolean { }
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(ref readonly T reference) { }
                }
                public class Enum : ValueType { }
                public class Attribute { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets validOn) { }

                    public bool AllowMultiple { get { return default; } set { } }
                    public bool Inherited { get { return default; } set { } }
                }
            }
            """;

        var corlib = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

        var source = """
            public class Test
            {
                static string M(string s, char c) => s + c + s + s;
            }
            """;

        var comp = CreateEmptyCompilation(source, [corlib]);
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation: comp, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);
        verifier.VerifyDiagnostics();

        // No matter whether `char` directly overrides `ToString` or not, we still produce span-based concat if we can
        verifier.VerifyIL("Test.M", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatFour_ReadOnlySpanConstructorParameterIsOrdinaryRef()
    {
        var corlib_cs = """
            namespace System
            {
                public class Object
                {
                    public virtual string ToString() => null;
                }
                public class String
                {
                    public static string Concat(string str0, string str1) => null;
                    public static string Concat(string str0, string str1, string str2) => null;
                    public static string Concat(string str0, string str1, string str2, string str3) => null;
                    public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1, ReadOnlySpan<char> str2, ReadOnlySpan<char> str3) => null;
                    public static implicit operator ReadOnlySpan<char>(string value) => default;
                }
                public class ValueType { }
                public struct Char
                {
                    public override string ToString() => null;
                }
                public struct Void { }
                public struct Int32 { }
                public struct Byte { }
                public struct Boolean { }
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(ref T reference) { }
                }
                public class Enum : ValueType { }
                public class Attribute { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets validOn) { }

                    public bool AllowMultiple { get { return default; } set { } }
                    public bool Inherited { get { return default; } set { } }
                }
            }
            """;

        var corlib = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

        var source = """
            public class Test
            {
                static string M(string s, char c) => s + c + s + s;
            }
            """;

        var comp = CreateEmptyCompilation(source, [corlib]);
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation: comp, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("Test.M", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatFour_ReadOnlySpanConstructorParameterIsOut()
    {
        var corlib_cs = """
            namespace System
            {
                public class Object
                {
                    public virtual string ToString() => null;
                }
                public class String
                {
                    public static string Concat(string str0, string str1) => null;
                    public static string Concat(string str0, string str1, string str2) => null;
                    public static string Concat(string str0, string str1, string str2, string str3) => null;
                    public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1, ReadOnlySpan<char> str2, ReadOnlySpan<char> str3) => null;
                    public static implicit operator ReadOnlySpan<char>(string value) => default;
                }
                public class ValueType { }
                public struct Char
                {
                    public override string ToString() => null;
                }
                public struct Void { }
                public struct Int32 { }
                public struct Byte { }
                public struct Boolean { }
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(out T reference) { reference = default; }
                }
                public class Enum : ValueType { }
                public class Attribute { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets validOn) { }

                    public bool AllowMultiple { get { return default; } set { } }
                    public bool Inherited { get { return default; } set { } }
                }
            }
            """;

        var corlib = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

        var source = """
            public class Test
            {
                static string M(string s, char c) => s + c + s + s;
            }
            """;

        var comp = CreateEmptyCompilation(source, [corlib]);
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation: comp, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);
        verifier.VerifyDiagnostics();

        // Constructor of ReadOnlySpan<char> has unexpected `out` reference. Fallback to string-based concat
        verifier.VerifyIL("Test.M", """
            {
              // Code size       16 (0x10)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  ldarg.0
              IL_0009:  ldarg.0
              IL_000a:  call       "string string.Concat(string, string, string, string)"
              IL_000f:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatFour_Await()
    {
        var source = """
            using System;
            using System.Threading.Tasks;

            public class Test
            {
                static async Task Main()
                {
                    Console.Write(await M());
                }

                static async Task<string> M()
                {
                    return (await GetStringAsync()) + (await GetCharAsync()) + (await GetCharAsync()) + (await GetStringAsync());
                }

                static async Task<string> GetStringAsync()
                {
                    await Task.Yield();
                    return "s";
                }

                static async Task<char> GetCharAsync()
                {
                    await Task.Yield();
                    return 'c';
                }
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sccs" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
            {
              // Code size      519 (0x207)
              .maxstack  4
              .locals init (int V_0,
                            string V_1,
                            char V_2,
                            char V_3,
                            string V_4,
                            System.Runtime.CompilerServices.TaskAwaiter<string> V_5,
                            System.Runtime.CompilerServices.TaskAwaiter<char> V_6,
                            System.Exception V_7)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int Test.<M>d__1.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  switch    (
                    IL_0056,
                    IL_00b9,
                    IL_011e,
                    IL_0183)
                IL_001d:  call       "System.Threading.Tasks.Task<string> Test.GetStringAsync()"
                IL_0022:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<string> System.Threading.Tasks.Task<string>.GetAwaiter()"
                IL_0027:  stloc.s    V_5
                IL_0029:  ldloca.s   V_5
                IL_002b:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<string>.IsCompleted.get"
                IL_0030:  brtrue.s   IL_0073
                IL_0032:  ldarg.0
                IL_0033:  ldc.i4.0
                IL_0034:  dup
                IL_0035:  stloc.0
                IL_0036:  stfld      "int Test.<M>d__1.<>1__state"
                IL_003b:  ldarg.0
                IL_003c:  ldloc.s    V_5
                IL_003e:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_0043:  ldarg.0
                IL_0044:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_0049:  ldloca.s   V_5
                IL_004b:  ldarg.0
                IL_004c:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<string>, Test.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<string>, ref Test.<M>d__1)"
                IL_0051:  leave      IL_0206
                IL_0056:  ldarg.0
                IL_0057:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_005c:  stloc.s    V_5
                IL_005e:  ldarg.0
                IL_005f:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_0064:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<string>"
                IL_006a:  ldarg.0
                IL_006b:  ldc.i4.m1
                IL_006c:  dup
                IL_006d:  stloc.0
                IL_006e:  stfld      "int Test.<M>d__1.<>1__state"
                IL_0073:  ldarg.0
                IL_0074:  ldloca.s   V_5
                IL_0076:  call       "string System.Runtime.CompilerServices.TaskAwaiter<string>.GetResult()"
                IL_007b:  stfld      "string Test.<M>d__1.<>7__wrap3"
                IL_0080:  call       "System.Threading.Tasks.Task<char> Test.GetCharAsync()"
                IL_0085:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<char> System.Threading.Tasks.Task<char>.GetAwaiter()"
                IL_008a:  stloc.s    V_6
                IL_008c:  ldloca.s   V_6
                IL_008e:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<char>.IsCompleted.get"
                IL_0093:  brtrue.s   IL_00d6
                IL_0095:  ldarg.0
                IL_0096:  ldc.i4.1
                IL_0097:  dup
                IL_0098:  stloc.0
                IL_0099:  stfld      "int Test.<M>d__1.<>1__state"
                IL_009e:  ldarg.0
                IL_009f:  ldloc.s    V_6
                IL_00a1:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_00a6:  ldarg.0
                IL_00a7:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_00ac:  ldloca.s   V_6
                IL_00ae:  ldarg.0
                IL_00af:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<char>, Test.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<char>, ref Test.<M>d__1)"
                IL_00b4:  leave      IL_0206
                IL_00b9:  ldarg.0
                IL_00ba:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_00bf:  stloc.s    V_6
                IL_00c1:  ldarg.0
                IL_00c2:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_00c7:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<char>"
                IL_00cd:  ldarg.0
                IL_00ce:  ldc.i4.m1
                IL_00cf:  dup
                IL_00d0:  stloc.0
                IL_00d1:  stfld      "int Test.<M>d__1.<>1__state"
                IL_00d6:  ldloca.s   V_6
                IL_00d8:  call       "char System.Runtime.CompilerServices.TaskAwaiter<char>.GetResult()"
                IL_00dd:  stloc.2
                IL_00de:  ldarg.0
                IL_00df:  ldloc.2
                IL_00e0:  stfld      "char Test.<M>d__1.<>7__wrap1"
                IL_00e5:  call       "System.Threading.Tasks.Task<char> Test.GetCharAsync()"
                IL_00ea:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<char> System.Threading.Tasks.Task<char>.GetAwaiter()"
                IL_00ef:  stloc.s    V_6
                IL_00f1:  ldloca.s   V_6
                IL_00f3:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<char>.IsCompleted.get"
                IL_00f8:  brtrue.s   IL_013b
                IL_00fa:  ldarg.0
                IL_00fb:  ldc.i4.2
                IL_00fc:  dup
                IL_00fd:  stloc.0
                IL_00fe:  stfld      "int Test.<M>d__1.<>1__state"
                IL_0103:  ldarg.0
                IL_0104:  ldloc.s    V_6
                IL_0106:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_010b:  ldarg.0
                IL_010c:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_0111:  ldloca.s   V_6
                IL_0113:  ldarg.0
                IL_0114:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<char>, Test.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<char>, ref Test.<M>d__1)"
                IL_0119:  leave      IL_0206
                IL_011e:  ldarg.0
                IL_011f:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_0124:  stloc.s    V_6
                IL_0126:  ldarg.0
                IL_0127:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<char> Test.<M>d__1.<>u__2"
                IL_012c:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<char>"
                IL_0132:  ldarg.0
                IL_0133:  ldc.i4.m1
                IL_0134:  dup
                IL_0135:  stloc.0
                IL_0136:  stfld      "int Test.<M>d__1.<>1__state"
                IL_013b:  ldloca.s   V_6
                IL_013d:  call       "char System.Runtime.CompilerServices.TaskAwaiter<char>.GetResult()"
                IL_0142:  stloc.3
                IL_0143:  ldarg.0
                IL_0144:  ldloc.3
                IL_0145:  stfld      "char Test.<M>d__1.<>7__wrap2"
                IL_014a:  call       "System.Threading.Tasks.Task<string> Test.GetStringAsync()"
                IL_014f:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<string> System.Threading.Tasks.Task<string>.GetAwaiter()"
                IL_0154:  stloc.s    V_5
                IL_0156:  ldloca.s   V_5
                IL_0158:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<string>.IsCompleted.get"
                IL_015d:  brtrue.s   IL_01a0
                IL_015f:  ldarg.0
                IL_0160:  ldc.i4.3
                IL_0161:  dup
                IL_0162:  stloc.0
                IL_0163:  stfld      "int Test.<M>d__1.<>1__state"
                IL_0168:  ldarg.0
                IL_0169:  ldloc.s    V_5
                IL_016b:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_0170:  ldarg.0
                IL_0171:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_0176:  ldloca.s   V_5
                IL_0178:  ldarg.0
                IL_0179:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<string>, Test.<M>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<string>, ref Test.<M>d__1)"
                IL_017e:  leave      IL_0206
                IL_0183:  ldarg.0
                IL_0184:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_0189:  stloc.s    V_5
                IL_018b:  ldarg.0
                IL_018c:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<string> Test.<M>d__1.<>u__1"
                IL_0191:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<string>"
                IL_0197:  ldarg.0
                IL_0198:  ldc.i4.m1
                IL_0199:  dup
                IL_019a:  stloc.0
                IL_019b:  stfld      "int Test.<M>d__1.<>1__state"
                IL_01a0:  ldloca.s   V_5
                IL_01a2:  call       "string System.Runtime.CompilerServices.TaskAwaiter<string>.GetResult()"
                IL_01a7:  stloc.s    V_4
                IL_01a9:  ldarg.0
                IL_01aa:  ldfld      "string Test.<M>d__1.<>7__wrap3"
                IL_01af:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
                IL_01b4:  ldarg.0
                IL_01b5:  ldflda     "char Test.<M>d__1.<>7__wrap1"
                IL_01ba:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
                IL_01bf:  ldarg.0
                IL_01c0:  ldflda     "char Test.<M>d__1.<>7__wrap2"
                IL_01c5:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
                IL_01ca:  ldloc.s    V_4
                IL_01cc:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
                IL_01d1:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
                IL_01d6:  stloc.1
                IL_01d7:  leave.s    IL_01f2
              }
              catch System.Exception
              {
                IL_01d9:  stloc.s    V_7
                IL_01db:  ldarg.0
                IL_01dc:  ldc.i4.s   -2
                IL_01de:  stfld      "int Test.<M>d__1.<>1__state"
                IL_01e3:  ldarg.0
                IL_01e4:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
                IL_01e9:  ldloc.s    V_7
                IL_01eb:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetException(System.Exception)"
                IL_01f0:  leave.s    IL_0206
              }
              IL_01f2:  ldarg.0
              IL_01f3:  ldc.i4.s   -2
              IL_01f5:  stfld      "int Test.<M>d__1.<>1__state"
              IL_01fa:  ldarg.0
              IL_01fb:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string> Test.<M>d__1.<>t__builder"
              IL_0200:  ldloc.1
              IL_0201:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<string>.SetResult(string)"
              IL_0206:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatFour_UserDefinedReadOnlySpan()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                    Console.Write(M5(s, c));
                    Console.Write(M6(s, c));
                    Console.Write(M7(s, c));
                }

                static string M1(string s, char c) => c + s + s + s;
                static string M2(string s, char c) => s + c + s + s;
                static string M3(string s, char c) => s + s + c + s;
                static string M4(string s, char c) => s + s + s + c;
                static string M5(string s, char c) => c + s + c + s;
                static string M6(string s, char c) => s + c + s + c;
                static string M7(string s, char c) => c + s + s + c;
            }

            namespace System
            {
                public struct ReadOnlySpan<T>
                {
                    public ReadOnlySpan(ref readonly T reference) { }
                }
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "csssscsssscssssccscsscsccssc" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        comp.VerifyIL("Test.M3", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  stloc.0
              IL_000e:  ldloca.s   V_0
              IL_0010:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        comp.VerifyIL("Test.M4", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.0
              IL_000d:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0012:  ldarg.1
              IL_0013:  stloc.0
              IL_0014:  ldloca.s   V_0
              IL_0016:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        comp.VerifyIL("Test.M5", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.1
              IL_0010:  stloc.1
              IL_0011:  ldloca.s   V_1
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0018:  ldarg.0
              IL_0019:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);
        comp.VerifyIL("Test.M6", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.1
              IL_0016:  stloc.1
              IL_0017:  ldloca.s   V_1
              IL_0019:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);
        comp.VerifyIL("Test.M7", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.1
              IL_0016:  stloc.1
              IL_0017:  ldloca.s   V_1
              IL_0019:  newobj     "System.ReadOnlySpan<char>..ctor(ref readonly char)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_4ReadOnlySpans)]
    public void ConcatFive_Char(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(CharInFirstFourArgs(s, c));
                    Console.Write(CharAfterFirstFourArgs(s, c));
                }

                static string CharInFirstFourArgs(string s, char c) => s + c + s + s + s;
                static string CharAfterFirstFourArgs(string s, char c) => s + s + s + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "scsssssssc" : null, verify: ExecutionConditionUtil.IsCoreClr ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // When lengths of inputs are low it is actually more optimal to first concat 4 operands with span-based concat and then concat that result with the remaining operand.
        // However when inputs become large enough cost of allocating a params array becomes lower than cost of creating an intermediate string.
        // + code size for using params array is less than code size from intermediate concat approach, especially when the amount of operands is high.
        // So in the end we always prefer overload with params array when there are 5+ operands
        verifier.VerifyIL("Test.CharInFirstFourArgs", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              IL_0000:  ldc.i4.5
              IL_0001:  newarr     "string"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldarg.0
              IL_0009:  stelem.ref
              IL_000a:  dup
              IL_000b:  ldc.i4.1
              IL_000c:  ldarga.s   V_1
              IL_000e:  call       "string char.ToString()"
              IL_0013:  stelem.ref
              IL_0014:  dup
              IL_0015:  ldc.i4.2
              IL_0016:  ldarg.0
              IL_0017:  stelem.ref
              IL_0018:  dup
              IL_0019:  ldc.i4.3
              IL_001a:  ldarg.0
              IL_001b:  stelem.ref
              IL_001c:  dup
              IL_001d:  ldc.i4.4
              IL_001e:  ldarg.0
              IL_001f:  stelem.ref
              IL_0020:  call       "string string.Concat(params string[])"
              IL_0025:  ret
            }
            """);
        verifier.VerifyIL("Test.CharAfterFirstFourArgs", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              IL_0000:  ldc.i4.5
              IL_0001:  newarr     "string"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldarg.0
              IL_0009:  stelem.ref
              IL_000a:  dup
              IL_000b:  ldc.i4.1
              IL_000c:  ldarg.0
              IL_000d:  stelem.ref
              IL_000e:  dup
              IL_000f:  ldc.i4.2
              IL_0010:  ldarg.0
              IL_0011:  stelem.ref
              IL_0012:  dup
              IL_0013:  ldc.i4.3
              IL_0014:  ldarg.0
              IL_0015:  stelem.ref
              IL_0016:  dup
              IL_0017:  ldc.i4.4
              IL_0018:  ldarga.s   V_1
              IL_001a:  call       "string char.ToString()"
              IL_001f:  stelem.ref
              IL_0020:  call       "string string.Concat(params string[])"
              IL_0025:  ret
            }
            """);

        comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net100);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        verifier = CompileAndVerify(compilation: comp, expectedOutput: ExecutionConditionUtil.IsCoreClr ? "scsssssssc" : null, verify: Verification.Fails);

        verifier.VerifyIL("Test.CharInFirstFourArgs", """
            {
              // Code size       78 (0x4e)
              .maxstack  2
              .locals init (System.Runtime.CompilerServices.InlineArray5<string> V_0)
              IL_0000:  ldloca.s   V_0
              IL_0002:  initobj    "System.Runtime.CompilerServices.InlineArray5<string>"
              IL_0008:  ldloca.s   V_0
              IL_000a:  ldc.i4.0
              IL_000b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0010:  ldarg.0
              IL_0011:  stind.ref
              IL_0012:  ldloca.s   V_0
              IL_0014:  ldc.i4.1
              IL_0015:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_001a:  ldarga.s   V_1
              IL_001c:  call       "string char.ToString()"
              IL_0021:  stind.ref
              IL_0022:  ldloca.s   V_0
              IL_0024:  ldc.i4.2
              IL_0025:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_002a:  ldarg.0
              IL_002b:  stind.ref
              IL_002c:  ldloca.s   V_0
              IL_002e:  ldc.i4.3
              IL_002f:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0034:  ldarg.0
              IL_0035:  stind.ref
              IL_0036:  ldloca.s   V_0
              IL_0038:  ldc.i4.4
              IL_0039:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_003e:  ldarg.0
              IL_003f:  stind.ref
              IL_0040:  ldloca.s   V_0
              IL_0042:  ldc.i4.5
              IL_0043:  call       "System.ReadOnlySpan<string> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<System.Runtime.CompilerServices.InlineArray5<string>, string>(in System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0048:  call       "string string.Concat(params System.ReadOnlySpan<string>)"
              IL_004d:  ret
            }
            """);
        verifier.VerifyIL("Test.CharAfterFirstFourArgs", """
            {
              // Code size       78 (0x4e)
              .maxstack  2
              .locals init (System.Runtime.CompilerServices.InlineArray5<string> V_0)
              IL_0000:  ldloca.s   V_0
              IL_0002:  initobj    "System.Runtime.CompilerServices.InlineArray5<string>"
              IL_0008:  ldloca.s   V_0
              IL_000a:  ldc.i4.0
              IL_000b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0010:  ldarg.0
              IL_0011:  stind.ref
              IL_0012:  ldloca.s   V_0
              IL_0014:  ldc.i4.1
              IL_0015:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_001a:  ldarg.0
              IL_001b:  stind.ref
              IL_001c:  ldloca.s   V_0
              IL_001e:  ldc.i4.2
              IL_001f:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0024:  ldarg.0
              IL_0025:  stind.ref
              IL_0026:  ldloca.s   V_0
              IL_0028:  ldc.i4.3
              IL_0029:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_002e:  ldarg.0
              IL_002f:  stind.ref
              IL_0030:  ldloca.s   V_0
              IL_0032:  ldc.i4.4
              IL_0033:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0038:  ldarga.s   V_1
              IL_003a:  call       "string char.ToString()"
              IL_003f:  stind.ref
              IL_0040:  ldloca.s   V_0
              IL_0042:  ldc.i4.5
              IL_0043:  call       "System.ReadOnlySpan<string> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<System.Runtime.CompilerServices.InlineArray5<string>, string>(in System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0048:  call       "string string.Concat(params System.ReadOnlySpan<string>)"
              IL_004d:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData("(s + s) + c + s + s")]
    [InlineData("s + (s + c) + s + s")]
    [InlineData("s + s + (c + s) + s")]
    [InlineData("s + s + c + (s + s)")]
    [InlineData("(s + s + c) + s + s")]
    [InlineData("s + (s + c + s) + s")]
    [InlineData("s + s + (c + s + s)")]
    [InlineData("(s + s + c + s) + s")]
    [InlineData("s + (s + c + s + s)")]
    [InlineData("(s + s) + (c + s) + s")]
    [InlineData("(s + s) + c + (s + s)")]
    [InlineData("s + (s + c) + (s + s)")]
    [InlineData("(s + s + c) + (s + s)")]
    [InlineData("(s + s) + (c + s + s)")]
    [InlineData("string.Concat(s, s) + c + s + s")]
    [InlineData("s + string.Concat(s, c.ToString()) + s + s")]
    [InlineData("s + s + string.Concat(c.ToString(), s) + s")]
    [InlineData("s + s + c + string.Concat(s, s)")]
    [InlineData("string.Concat(s, s, c.ToString()) + s + s")]
    [InlineData("s + string.Concat(s, c.ToString(), s) + s")]
    [InlineData("s + s + string.Concat(c.ToString(), s, s)")]
    [InlineData("string.Concat(s, s, c.ToString(), s) + s")]
    [InlineData("s + string.Concat(s, c.ToString(), s, s)")]
    [InlineData("string.Concat(s, s) + string.Concat(c.ToString(), s) + s")]
    [InlineData("string.Concat(s, s) + c + string.Concat(s, s)")]
    [InlineData("s + string.Concat(s, c.ToString()) + string.Concat(s, s)")]
    [InlineData("string.Concat(s, s, c.ToString()) + string.Concat(s, s)")]
    [InlineData("string.Concat(s, s) + string.Concat(c.ToString(), s, s)")]
    public void ConcatFive_Char_OperandGroupingAndUserInputOfStringBasedConcats(string expression)
    {
        var source = $$"""
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M(s, c));
                }

                static string M(string s, char c) => {{expression}};
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sscss" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              IL_0000:  ldc.i4.5
              IL_0001:  newarr     "string"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldarg.0
              IL_0009:  stelem.ref
              IL_000a:  dup
              IL_000b:  ldc.i4.1
              IL_000c:  ldarg.0
              IL_000d:  stelem.ref
              IL_000e:  dup
              IL_000f:  ldc.i4.2
              IL_0010:  ldarga.s   V_1
              IL_0012:  call       "string char.ToString()"
              IL_0017:  stelem.ref
              IL_0018:  dup
              IL_0019:  ldc.i4.3
              IL_001a:  ldarg.0
              IL_001b:  stelem.ref
              IL_001c:  dup
              IL_001d:  ldc.i4.4
              IL_001e:  ldarg.0
              IL_001f:  stelem.ref
              IL_0020:  call       "string string.Concat(params string[])"
              IL_0025:  ret
            }
            """);

        comp = CompileAndVerify(source, expectedOutput: ExecutionConditionUtil.IsCoreClr ? "sscss" : null, targetFramework: TargetFramework.Net100, verify: Verification.Fails);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M", """
            {
              // Code size       78 (0x4e)
              .maxstack  2
              .locals init (System.Runtime.CompilerServices.InlineArray5<string> V_0)
              IL_0000:  ldloca.s   V_0
              IL_0002:  initobj    "System.Runtime.CompilerServices.InlineArray5<string>"
              IL_0008:  ldloca.s   V_0
              IL_000a:  ldc.i4.0
              IL_000b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0010:  ldarg.0
              IL_0011:  stind.ref
              IL_0012:  ldloca.s   V_0
              IL_0014:  ldc.i4.1
              IL_0015:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_001a:  ldarg.0
              IL_001b:  stind.ref
              IL_001c:  ldloca.s   V_0
              IL_001e:  ldc.i4.2
              IL_001f:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0024:  ldarga.s   V_1
              IL_0026:  call       "string char.ToString()"
              IL_002b:  stind.ref
              IL_002c:  ldloca.s   V_0
              IL_002e:  ldc.i4.3
              IL_002f:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0034:  ldarg.0
              IL_0035:  stind.ref
              IL_0036:  ldloca.s   V_0
              IL_0038:  ldc.i4.4
              IL_0039:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_003e:  ldarg.0
              IL_003f:  stind.ref
              IL_0040:  ldloca.s   V_0
              IL_0042:  ldc.i4.5
              IL_0043:  call       "System.ReadOnlySpan<string> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<System.Runtime.CompilerServices.InlineArray5<string>, string>(in System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0048:  call       "string string.Concat(params System.ReadOnlySpan<string>)"
              IL_004d:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData((int)SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)SpecialMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)SpecialMember.System_String__Concat_2ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_3ReadOnlySpans)]
    [InlineData((int)SpecialMember.System_String__Concat_4ReadOnlySpans)]
    public void ConcatFiveCharToStrings(int? missingUnimportantMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var c1 = 'a';
                    var c2 = 'b';
                    var c3 = 'c';
                    var c4 = 'd';
                    var c5 = 'e';
                    Console.Write(M(c1, c2, c3, c4, c5));
                }

                static string M(char c1, char c2, char c3, char c4, char c5) => c1.ToString() + c2.ToString() + c3.ToString() + c4.ToString() + c5.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcde" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size       62 (0x3e)
              .maxstack  4
              IL_0000:  ldc.i4.5
              IL_0001:  newarr     "string"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldarga.s   V_0
              IL_000a:  call       "string char.ToString()"
              IL_000f:  stelem.ref
              IL_0010:  dup
              IL_0011:  ldc.i4.1
              IL_0012:  ldarga.s   V_1
              IL_0014:  call       "string char.ToString()"
              IL_0019:  stelem.ref
              IL_001a:  dup
              IL_001b:  ldc.i4.2
              IL_001c:  ldarga.s   V_2
              IL_001e:  call       "string char.ToString()"
              IL_0023:  stelem.ref
              IL_0024:  dup
              IL_0025:  ldc.i4.3
              IL_0026:  ldarga.s   V_3
              IL_0028:  call       "string char.ToString()"
              IL_002d:  stelem.ref
              IL_002e:  dup
              IL_002f:  ldc.i4.4
              IL_0030:  ldarga.s   V_4
              IL_0032:  call       "string char.ToString()"
              IL_0037:  stelem.ref
              IL_0038:  call       "string string.Concat(params string[])"
              IL_003d:  ret
            }
            """);

        comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net100);

        if (missingUnimportantMember.HasValue)
        {
            comp.MakeMemberMissing((SpecialMember)missingUnimportantMember.Value);
        }

        verifier = CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsCoreClr ? "abcde" : null, verify: Verification.Fails);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size      102 (0x66)
              .maxstack  2
              .locals init (System.Runtime.CompilerServices.InlineArray5<string> V_0)
              IL_0000:  ldloca.s   V_0
              IL_0002:  initobj    "System.Runtime.CompilerServices.InlineArray5<string>"
              IL_0008:  ldloca.s   V_0
              IL_000a:  ldc.i4.0
              IL_000b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0010:  ldarga.s   V_0
              IL_0012:  call       "string char.ToString()"
              IL_0017:  stind.ref
              IL_0018:  ldloca.s   V_0
              IL_001a:  ldc.i4.1
              IL_001b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0020:  ldarga.s   V_1
              IL_0022:  call       "string char.ToString()"
              IL_0027:  stind.ref
              IL_0028:  ldloca.s   V_0
              IL_002a:  ldc.i4.2
              IL_002b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0030:  ldarga.s   V_2
              IL_0032:  call       "string char.ToString()"
              IL_0037:  stind.ref
              IL_0038:  ldloca.s   V_0
              IL_003a:  ldc.i4.3
              IL_003b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0040:  ldarga.s   V_3
              IL_0042:  call       "string char.ToString()"
              IL_0047:  stind.ref
              IL_0048:  ldloca.s   V_0
              IL_004a:  ldc.i4.4
              IL_004b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0050:  ldarga.s   V_4
              IL_0052:  call       "string char.ToString()"
              IL_0057:  stind.ref
              IL_0058:  ldloca.s   V_0
              IL_005a:  ldc.i4.5
              IL_005b:  call       "System.ReadOnlySpan<string> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<System.Runtime.CompilerServices.InlineArray5<string>, string>(in System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0060:  call       "string string.Concat(params System.ReadOnlySpan<string>)"
              IL_0065:  ret
            }
            """);
    }

    [Fact]
    public void ConcatFive_InlineArrayLocalReuse()
    {
        var source = """
            string s = "a";
            System.Console.WriteLine(s + s + s + s + s);
            System.Console.WriteLine(s + s + s + s + s);
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.Fails, expectedOutput: ExecutionConditionUtil.IsCoreClr ? """
            aaaaa
            aaaaa
            """ : null);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size      159 (0x9f)
              .maxstack  2
              .locals init (string V_0, //s
                            System.Runtime.CompilerServices.InlineArray5<string> V_1,
                            System.Runtime.CompilerServices.InlineArray5<string> V_2)
              IL_0000:  ldstr      "a"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_1
              IL_0008:  initobj    "System.Runtime.CompilerServices.InlineArray5<string>"
              IL_000e:  ldloca.s   V_1
              IL_0010:  ldc.i4.0
              IL_0011:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0016:  ldloc.0
              IL_0017:  stind.ref
              IL_0018:  ldloca.s   V_1
              IL_001a:  ldc.i4.1
              IL_001b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0020:  ldloc.0
              IL_0021:  stind.ref
              IL_0022:  ldloca.s   V_1
              IL_0024:  ldc.i4.2
              IL_0025:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_002a:  ldloc.0
              IL_002b:  stind.ref
              IL_002c:  ldloca.s   V_1
              IL_002e:  ldc.i4.3
              IL_002f:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0034:  ldloc.0
              IL_0035:  stind.ref
              IL_0036:  ldloca.s   V_1
              IL_0038:  ldc.i4.4
              IL_0039:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_003e:  ldloc.0
              IL_003f:  stind.ref
              IL_0040:  ldloca.s   V_1
              IL_0042:  ldc.i4.5
              IL_0043:  call       "System.ReadOnlySpan<string> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<System.Runtime.CompilerServices.InlineArray5<string>, string>(in System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0048:  call       "string string.Concat(params System.ReadOnlySpan<string>)"
              IL_004d:  call       "void System.Console.WriteLine(string)"
              IL_0052:  ldloca.s   V_2
              IL_0054:  initobj    "System.Runtime.CompilerServices.InlineArray5<string>"
              IL_005a:  ldloca.s   V_2
              IL_005c:  ldc.i4.0
              IL_005d:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0062:  ldloc.0
              IL_0063:  stind.ref
              IL_0064:  ldloca.s   V_2
              IL_0066:  ldc.i4.1
              IL_0067:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_006c:  ldloc.0
              IL_006d:  stind.ref
              IL_006e:  ldloca.s   V_2
              IL_0070:  ldc.i4.2
              IL_0071:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0076:  ldloc.0
              IL_0077:  stind.ref
              IL_0078:  ldloca.s   V_2
              IL_007a:  ldc.i4.3
              IL_007b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0080:  ldloc.0
              IL_0081:  stind.ref
              IL_0082:  ldloca.s   V_2
              IL_0084:  ldc.i4.4
              IL_0085:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_008a:  ldloc.0
              IL_008b:  stind.ref
              IL_008c:  ldloca.s   V_2
              IL_008e:  ldc.i4.5
              IL_008f:  call       "System.ReadOnlySpan<string> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<System.Runtime.CompilerServices.InlineArray5<string>, string>(in System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0094:  call       "string string.Concat(params System.ReadOnlySpan<string>)"
              IL_0099:  call       "void System.Console.WriteLine(string)"
              IL_009e:  ret
            }
            """);
    }

    [Fact]
    public void ConcatMultiple_InlineArrayLocalReuse()
    {
        var source = """
            string s = "a";
            System.Console.WriteLine(s + s + s + s + s);
            System.Console.WriteLine(s + s + s + s + s + s);
            System.Console.WriteLine(s + s + s + s + s + s + s);
            """;

        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.Fails, expectedOutput: ExecutionConditionUtil.IsCoreClr ? """
            aaaaa
            aaaaaa
            aaaaaaa
            """ : null);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size      265 (0x109)
              .maxstack  2
              .locals init (string V_0, //s
                            System.Runtime.CompilerServices.InlineArray5<string> V_1,
                            System.Runtime.CompilerServices.InlineArray6<string> V_2,
                            System.Runtime.CompilerServices.InlineArray7<string> V_3)
              IL_0000:  ldstr      "a"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_1
              IL_0008:  initobj    "System.Runtime.CompilerServices.InlineArray5<string>"
              IL_000e:  ldloca.s   V_1
              IL_0010:  ldc.i4.0
              IL_0011:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0016:  ldloc.0
              IL_0017:  stind.ref
              IL_0018:  ldloca.s   V_1
              IL_001a:  ldc.i4.1
              IL_001b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0020:  ldloc.0
              IL_0021:  stind.ref
              IL_0022:  ldloca.s   V_1
              IL_0024:  ldc.i4.2
              IL_0025:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_002a:  ldloc.0
              IL_002b:  stind.ref
              IL_002c:  ldloca.s   V_1
              IL_002e:  ldc.i4.3
              IL_002f:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0034:  ldloc.0
              IL_0035:  stind.ref
              IL_0036:  ldloca.s   V_1
              IL_0038:  ldc.i4.4
              IL_0039:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray5<string>, string>(ref System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_003e:  ldloc.0
              IL_003f:  stind.ref
              IL_0040:  ldloca.s   V_1
              IL_0042:  ldc.i4.5
              IL_0043:  call       "System.ReadOnlySpan<string> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<System.Runtime.CompilerServices.InlineArray5<string>, string>(in System.Runtime.CompilerServices.InlineArray5<string>, int)"
              IL_0048:  call       "string string.Concat(params System.ReadOnlySpan<string>)"
              IL_004d:  call       "void System.Console.WriteLine(string)"
              IL_0052:  ldloca.s   V_2
              IL_0054:  initobj    "System.Runtime.CompilerServices.InlineArray6<string>"
              IL_005a:  ldloca.s   V_2
              IL_005c:  ldc.i4.0
              IL_005d:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray6<string>, string>(ref System.Runtime.CompilerServices.InlineArray6<string>, int)"
              IL_0062:  ldloc.0
              IL_0063:  stind.ref
              IL_0064:  ldloca.s   V_2
              IL_0066:  ldc.i4.1
              IL_0067:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray6<string>, string>(ref System.Runtime.CompilerServices.InlineArray6<string>, int)"
              IL_006c:  ldloc.0
              IL_006d:  stind.ref
              IL_006e:  ldloca.s   V_2
              IL_0070:  ldc.i4.2
              IL_0071:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray6<string>, string>(ref System.Runtime.CompilerServices.InlineArray6<string>, int)"
              IL_0076:  ldloc.0
              IL_0077:  stind.ref
              IL_0078:  ldloca.s   V_2
              IL_007a:  ldc.i4.3
              IL_007b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray6<string>, string>(ref System.Runtime.CompilerServices.InlineArray6<string>, int)"
              IL_0080:  ldloc.0
              IL_0081:  stind.ref
              IL_0082:  ldloca.s   V_2
              IL_0084:  ldc.i4.4
              IL_0085:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray6<string>, string>(ref System.Runtime.CompilerServices.InlineArray6<string>, int)"
              IL_008a:  ldloc.0
              IL_008b:  stind.ref
              IL_008c:  ldloca.s   V_2
              IL_008e:  ldc.i4.5
              IL_008f:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray6<string>, string>(ref System.Runtime.CompilerServices.InlineArray6<string>, int)"
              IL_0094:  ldloc.0
              IL_0095:  stind.ref
              IL_0096:  ldloca.s   V_2
              IL_0098:  ldc.i4.6
              IL_0099:  call       "System.ReadOnlySpan<string> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<System.Runtime.CompilerServices.InlineArray6<string>, string>(in System.Runtime.CompilerServices.InlineArray6<string>, int)"
              IL_009e:  call       "string string.Concat(params System.ReadOnlySpan<string>)"
              IL_00a3:  call       "void System.Console.WriteLine(string)"
              IL_00a8:  ldloca.s   V_3
              IL_00aa:  initobj    "System.Runtime.CompilerServices.InlineArray7<string>"
              IL_00b0:  ldloca.s   V_3
              IL_00b2:  ldc.i4.0
              IL_00b3:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray7<string>, string>(ref System.Runtime.CompilerServices.InlineArray7<string>, int)"
              IL_00b8:  ldloc.0
              IL_00b9:  stind.ref
              IL_00ba:  ldloca.s   V_3
              IL_00bc:  ldc.i4.1
              IL_00bd:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray7<string>, string>(ref System.Runtime.CompilerServices.InlineArray7<string>, int)"
              IL_00c2:  ldloc.0
              IL_00c3:  stind.ref
              IL_00c4:  ldloca.s   V_3
              IL_00c6:  ldc.i4.2
              IL_00c7:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray7<string>, string>(ref System.Runtime.CompilerServices.InlineArray7<string>, int)"
              IL_00cc:  ldloc.0
              IL_00cd:  stind.ref
              IL_00ce:  ldloca.s   V_3
              IL_00d0:  ldc.i4.3
              IL_00d1:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray7<string>, string>(ref System.Runtime.CompilerServices.InlineArray7<string>, int)"
              IL_00d6:  ldloc.0
              IL_00d7:  stind.ref
              IL_00d8:  ldloca.s   V_3
              IL_00da:  ldc.i4.4
              IL_00db:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray7<string>, string>(ref System.Runtime.CompilerServices.InlineArray7<string>, int)"
              IL_00e0:  ldloc.0
              IL_00e1:  stind.ref
              IL_00e2:  ldloca.s   V_3
              IL_00e4:  ldc.i4.5
              IL_00e5:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray7<string>, string>(ref System.Runtime.CompilerServices.InlineArray7<string>, int)"
              IL_00ea:  ldloc.0
              IL_00eb:  stind.ref
              IL_00ec:  ldloca.s   V_3
              IL_00ee:  ldc.i4.6
              IL_00ef:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<System.Runtime.CompilerServices.InlineArray7<string>, string>(ref System.Runtime.CompilerServices.InlineArray7<string>, int)"
              IL_00f4:  ldloc.0
              IL_00f5:  stind.ref
              IL_00f6:  ldloca.s   V_3
              IL_00f8:  ldc.i4.7
              IL_00f9:  call       "System.ReadOnlySpan<string> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<System.Runtime.CompilerServices.InlineArray7<string>, string>(in System.Runtime.CompilerServices.InlineArray7<string>, int)"
              IL_00fe:  call       "string string.Concat(params System.ReadOnlySpan<string>)"
              IL_0103:  call       "void System.Console.WriteLine(string)"
              IL_0108:  ret
            }
            """);
    }

    [Fact]
    public void ConcatMoreThan16ArgsWithReadOnlySpan()
    {
        var source = $$"""
            string s = "a";
            System.Console.WriteLine(s {{string.Join("", Enumerable.Repeat(" + s", 16))}});
            """;

        var comp = CompileAndVerify(source, targetFramework: TargetFramework.Net100, verify: Verification.Fails, expectedOutput: ExecutionConditionUtil.IsCoreClr ? new string('a', 17) : null);
        comp.VerifyDiagnostics();
        comp.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size      212 (0xd4)
              .maxstack  2
              .locals init (string V_0, //s
                          <>y__InlineArray17<string> V_1)
              IL_0000:  ldstr      "a"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_1
              IL_0008:  initobj    "<>y__InlineArray17<string>"
              IL_000e:  ldloca.s   V_1
              IL_0010:  ldc.i4.0
              IL_0011:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_0016:  ldloc.0
              IL_0017:  stind.ref
              IL_0018:  ldloca.s   V_1
              IL_001a:  ldc.i4.1
              IL_001b:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_0020:  ldloc.0
              IL_0021:  stind.ref
              IL_0022:  ldloca.s   V_1
              IL_0024:  ldc.i4.2
              IL_0025:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_002a:  ldloc.0
              IL_002b:  stind.ref
              IL_002c:  ldloca.s   V_1
              IL_002e:  ldc.i4.3
              IL_002f:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_0034:  ldloc.0
              IL_0035:  stind.ref
              IL_0036:  ldloca.s   V_1
              IL_0038:  ldc.i4.4
              IL_0039:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_003e:  ldloc.0
              IL_003f:  stind.ref
              IL_0040:  ldloca.s   V_1
              IL_0042:  ldc.i4.5
              IL_0043:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_0048:  ldloc.0
              IL_0049:  stind.ref
              IL_004a:  ldloca.s   V_1
              IL_004c:  ldc.i4.6
              IL_004d:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_0052:  ldloc.0
              IL_0053:  stind.ref
              IL_0054:  ldloca.s   V_1
              IL_0056:  ldc.i4.7
              IL_0057:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_005c:  ldloc.0
              IL_005d:  stind.ref
              IL_005e:  ldloca.s   V_1
              IL_0060:  ldc.i4.8
              IL_0061:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_0066:  ldloc.0
              IL_0067:  stind.ref
              IL_0068:  ldloca.s   V_1
              IL_006a:  ldc.i4.s   9
              IL_006c:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_0071:  ldloc.0
              IL_0072:  stind.ref
              IL_0073:  ldloca.s   V_1
              IL_0075:  ldc.i4.s   10
              IL_0077:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_007c:  ldloc.0
              IL_007d:  stind.ref
              IL_007e:  ldloca.s   V_1
              IL_0080:  ldc.i4.s   11
              IL_0082:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_0087:  ldloc.0
              IL_0088:  stind.ref
              IL_0089:  ldloca.s   V_1
              IL_008b:  ldc.i4.s   12
              IL_008d:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_0092:  ldloc.0
              IL_0093:  stind.ref
              IL_0094:  ldloca.s   V_1
              IL_0096:  ldc.i4.s   13
              IL_0098:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_009d:  ldloc.0
              IL_009e:  stind.ref
              IL_009f:  ldloca.s   V_1
              IL_00a1:  ldc.i4.s   14
              IL_00a3:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_00a8:  ldloc.0
              IL_00a9:  stind.ref
              IL_00aa:  ldloca.s   V_1
              IL_00ac:  ldc.i4.s   15
              IL_00ae:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_00b3:  ldloc.0
              IL_00b4:  stind.ref
              IL_00b5:  ldloca.s   V_1
              IL_00b7:  ldc.i4.s   16
              IL_00b9:  call       "ref string <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray17<string>, string>(ref <>y__InlineArray17<string>, int)"
              IL_00be:  ldloc.0
              IL_00bf:  stind.ref
              IL_00c0:  ldloca.s   V_1
              IL_00c2:  ldc.i4.s   17
              IL_00c4:  call       "System.ReadOnlySpan<string> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray17<string>, string>(in <>y__InlineArray17<string>, int)"
              IL_00c9:  call       "string string.Concat(params System.ReadOnlySpan<string>)"
              IL_00ce:  call       "void System.Console.WriteLine(string)"
              IL_00d3:  ret
            }
            """);
    }

    [Fact]
    public void ConcatReadOnlySpan_NoInlineArraySupport()
    {
        var source = """
                string s = "a";
                System.Console.WriteLine(s + s + s + s + s);
                """;

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
        // Make this member missing to simulate lack of InlineArray support.
        comp.MakeMemberMissing(SpecialMember.System_Runtime_CompilerServices_InlineArrayAttribute__ctor);
        var verifier = CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsCoreClr ? "aaaaa" : null, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       43 (0x2b)
              .maxstack  4
              .locals init (string V_0) //s
              IL_0000:  ldstr      "a"
              IL_0005:  stloc.0
              IL_0006:  ldc.i4.5
              IL_0007:  newarr     "string"
              IL_000c:  dup
              IL_000d:  ldc.i4.0
              IL_000e:  ldloc.0
              IL_000f:  stelem.ref
              IL_0010:  dup
              IL_0011:  ldc.i4.1
              IL_0012:  ldloc.0
              IL_0013:  stelem.ref
              IL_0014:  dup
              IL_0015:  ldc.i4.2
              IL_0016:  ldloc.0
              IL_0017:  stelem.ref
              IL_0018:  dup
              IL_0019:  ldc.i4.3
              IL_001a:  ldloc.0
              IL_001b:  stelem.ref
              IL_001c:  dup
              IL_001d:  ldc.i4.4
              IL_001e:  ldloc.0
              IL_001f:  stelem.ref
              IL_0020:  call       "string string.Concat(params string[])"
              IL_0025:  call       "void System.Console.WriteLine(string)"
              IL_002a:  ret
            }
            """);
    }

    [Fact]
    public void ConcatReadOnlySpan_InExpressionTree()
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            string s = "a";
            Expression<Func<string>> expr = () => s + s + s + s + s;
            System.Console.WriteLine(expr.Compile()());
            """;

        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net100);
        var verifier = CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsCoreClr ? "aaaaa" : null, verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size      278 (0x116)
              .maxstack  3
              .locals init (Program.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
              IL_0000:  newobj     "Program.<>c__DisplayClass0_0..ctor()"
              IL_0005:  stloc.0
              IL_0006:  ldloc.0
              IL_0007:  ldstr      "a"
              IL_000c:  stfld      "string Program.<>c__DisplayClass0_0.s"
              IL_0011:  ldloc.0
              IL_0012:  ldtoken    "Program.<>c__DisplayClass0_0"
              IL_0017:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_001c:  call       "System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)"
              IL_0021:  ldtoken    "string Program.<>c__DisplayClass0_0.s"
              IL_0026:  call       "System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)"
              IL_002b:  call       "System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)"
              IL_0030:  ldloc.0
              IL_0031:  ldtoken    "Program.<>c__DisplayClass0_0"
              IL_0036:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_003b:  call       "System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)"
              IL_0040:  ldtoken    "string Program.<>c__DisplayClass0_0.s"
              IL_0045:  call       "System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)"
              IL_004a:  call       "System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)"
              IL_004f:  ldtoken    "string string.Concat(string, string)"
              IL_0054:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)"
              IL_0059:  castclass  "System.Reflection.MethodInfo"
              IL_005e:  call       "System.Linq.Expressions.BinaryExpression System.Linq.Expressions.Expression.Add(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Reflection.MethodInfo)"
              IL_0063:  ldloc.0
              IL_0064:  ldtoken    "Program.<>c__DisplayClass0_0"
              IL_0069:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_006e:  call       "System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)"
              IL_0073:  ldtoken    "string Program.<>c__DisplayClass0_0.s"
              IL_0078:  call       "System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)"
              IL_007d:  call       "System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)"
              IL_0082:  ldtoken    "string string.Concat(string, string)"
              IL_0087:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)"
              IL_008c:  castclass  "System.Reflection.MethodInfo"
              IL_0091:  call       "System.Linq.Expressions.BinaryExpression System.Linq.Expressions.Expression.Add(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Reflection.MethodInfo)"
              IL_0096:  ldloc.0
              IL_0097:  ldtoken    "Program.<>c__DisplayClass0_0"
              IL_009c:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_00a1:  call       "System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)"
              IL_00a6:  ldtoken    "string Program.<>c__DisplayClass0_0.s"
              IL_00ab:  call       "System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)"
              IL_00b0:  call       "System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)"
              IL_00b5:  ldtoken    "string string.Concat(string, string)"
              IL_00ba:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)"
              IL_00bf:  castclass  "System.Reflection.MethodInfo"
              IL_00c4:  call       "System.Linq.Expressions.BinaryExpression System.Linq.Expressions.Expression.Add(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Reflection.MethodInfo)"
              IL_00c9:  ldloc.0
              IL_00ca:  ldtoken    "Program.<>c__DisplayClass0_0"
              IL_00cf:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_00d4:  call       "System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)"
              IL_00d9:  ldtoken    "string Program.<>c__DisplayClass0_0.s"
              IL_00de:  call       "System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)"
              IL_00e3:  call       "System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)"
              IL_00e8:  ldtoken    "string string.Concat(string, string)"
              IL_00ed:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)"
              IL_00f2:  castclass  "System.Reflection.MethodInfo"
              IL_00f7:  call       "System.Linq.Expressions.BinaryExpression System.Linq.Expressions.Expression.Add(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Reflection.MethodInfo)"
              IL_00fc:  call       "System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()"
              IL_0101:  call       "System.Linq.Expressions.Expression<System.Func<string>> System.Linq.Expressions.Expression.Lambda<System.Func<string>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])"
              IL_0106:  callvirt   "System.Func<string> System.Linq.Expressions.Expression<System.Func<string>>.Compile()"
              IL_010b:  callvirt   "string System.Func<string>.Invoke()"
              IL_0110:  call       "void System.Console.WriteLine(string)"
              IL_0115:  ret
            }
            """);
    }
}
