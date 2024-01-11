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
}
