// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_000f:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_0012:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001c:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    public void ConcatTwoCharToStrings(int? missingUnimportantWellKnownMember)
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

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
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
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.1
              IL_000a:  stloc.1
              IL_000b:  ldloca.s   V_1
              IL_000d:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0012:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0017:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    [InlineData((int)WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
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
        comp.MakeMemberMissing((WellKnownMember)member);

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

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThree_ReadOnlySpan1(int? missingUnimportantWellKnownMember)
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

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
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
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_0010:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.1
              IL_0010:  stloc.1
              IL_0011:  ldloca.s   V_1
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0018:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001d:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThree_ReadOnlySpan2(int? missingUnimportantWellKnownMember)
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

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
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
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_000f:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_0015:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  ldarg.1
              IL_0015:  call       "char char.ToLowerInvariant(char)"
              IL_001a:  stloc.1
              IL_001b:  ldloca.s   V_1
              IL_001d:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThree_ReadOnlySpan_SideEffect(int? missingUnimportantWellKnownMember)
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

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
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
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_0012:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_001c:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
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
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "char Test.GetCharWithSideEffect()"
              IL_001c:  stloc.1
              IL_001d:  ldloca.s   V_1
              IL_001f:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0024:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0029:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThree_ReadOnlySpan_ReferenceToSameLocation(int? missingUnimportantWellKnownMember)
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

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
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
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0018:  ldarg.0
              IL_0019:  call       "ref char C.GetC()"
              IL_001e:  ldind.u2
              IL_001f:  stloc.1
              IL_0020:  ldloca.s   V_1
              IL_0022:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0027:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002c:  call       "void System.Console.Write(string)"
              IL_0031:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThree_ReadOnlySpan_MutateLocal(int? missingUnimportantWellKnownMember)
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

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
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
              IL_0011:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0016:  ldarg.0
              IL_0017:  ldloca.s   V_0
              IL_0019:  call       "char C.SneakyLocalChange(ref char)"
              IL_001e:  stloc.2
              IL_001f:  ldloca.s   V_2
              IL_0021:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0026:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002b:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThreeCharToStrings(int? missingUnimportantWellKnownMember)
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

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
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
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.1
              IL_000a:  stloc.1
              IL_000b:  ldloca.s   V_1
              IL_000d:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0012:  ldarg.2
              IL_0013:  stloc.2
              IL_0014:  ldloca.s   V_2
              IL_0016:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    [InlineData((int)WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
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
        comp.MakeMemberMissing((WellKnownMember)member);

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
}
