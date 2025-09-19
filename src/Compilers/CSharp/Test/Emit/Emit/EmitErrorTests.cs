// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// this place is dedicated to emit/codegen related error tests
    /// </summary>
    public class EmitErrorTests : EmitMetadataTestBase
    {
        #region "Mixed Error Tests"

        [WorkItem(543039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543039")]
        [Fact]
        public void BadConstantInOtherAssemblyUsedByField()
        {
            string source1 = @"
public class A
{
    public const int x = x;
}
";
            var compilation1 = CreateCompilation(source1);
            compilation1.VerifyDiagnostics(
                // (4,22): error CS0110: The evaluation of the constant value for 'A.x' involves a circular definition
                Diagnostic(CSharp.ErrorCode.ERR_CircConstValue, "x").WithArguments("A.x"));

            string source2 = @"
public class B
{
    public const int y = A.x;

    public static void Main()
    {
        System.Console.WriteLine(""Hello"");
    }
}
";
            VerifyEmitDiagnostics(source2, compilation1);
        }

        [WorkItem(543039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543039")]
        [Fact]
        public void BadConstantInOtherAssemblyUsedByLocal()
        {
            string source1 = @"
public class A
{
    public const int x = x;
}
";
            var compilation1 = CreateCompilation(source1);
            compilation1.VerifyDiagnostics(
                // (4,22): error CS0110: The evaluation of the constant value for 'A.x' involves a circular definition
                Diagnostic(CSharp.ErrorCode.ERR_CircConstValue, "x").WithArguments("A.x"));

            string source2 = @"
public class B
{
    public static void Main()
    {
        const int y = A.x;
        System.Console.WriteLine(""Hello"");
    }
}
";
            VerifyEmitDiagnostics(source2, compilation1,
                // (6,19): warning CS0219: The variable 'y' is assigned but its value is never used
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));
        }

        [WorkItem(543039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543039")]
        [Fact]
        public void BadDefaultArgumentInOtherAssembly()
        {
            string source1 = @"
public class A
{
    public const int x = x;

    public static int Goo(int y = x) { return y; }
}
";
            var compilation1 = CreateCompilation(source1);
            compilation1.VerifyDiagnostics(
                // (4,22): error CS0110: The evaluation of the constant value for 'A.x' involves a circular definition
                Diagnostic(CSharp.ErrorCode.ERR_CircConstValue, "x").WithArguments("A.x"));

            string source2 = @"
public class B
{
    public static void Main()
    {
        System.Console.WriteLine(A.Goo());
    }
}
";
            // ILVerify null ref
            // Tracked by https://github.com/dotnet/roslyn/issues/58652
            var compilation2 = CompileAndVerify(
                source2,
                new[] { new CSharpCompilationReference(compilation1) },
                verify: Verification.Fails);
        }

        [WorkItem(543039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543039")]
        [Fact]
        public void BadDefaultArgumentInOtherAssembly_Decimal()
        {
            string source1 = @"
public class A
{
    public const decimal x = x;

    public static decimal Goo(decimal y = x) { return y; }
}
";
            var compilation1 = CreateCompilation(source1);
            compilation1.VerifyDiagnostics(
                // (4,22): error CS0110: The evaluation of the constant value for 'A.x' involves a circular definition
                Diagnostic(ErrorCode.ERR_CircConstValue, "x").WithArguments("A.x"));

            string source2 = @"
public class B
{
    public static void Main()
    {
        System.Console.WriteLine(A.Goo());
    }
}
";
            // ILVerify null ref
            // Tracked by https://github.com/dotnet/roslyn/issues/58652
            var compilation2 = CompileAndVerify(
                source2,
                new[] { new CSharpCompilationReference(compilation1) },
                verify: Verification.Fails);
        }

        [WorkItem(543039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543039")]
        [Fact]
        public void BadDefaultArgumentInOtherAssembly_UserDefinedType()
        {
            string source1 = @"
public struct S 
{
    public override string ToString() { return ""S::ToString""; }
}

public class A
{
    public static S Goo(S p = 42) { return p; }
}
";
            var compilation1 = CreateCompilation(source1);
            compilation1.VerifyDiagnostics(
                // (9,27): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type 'S'
                //     public static S Goo(S p = 42) { return p; }
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "p").WithArguments("int", "S").WithLocation(9, 27));

            string source2 = @"
public class B
{
    public static void Main()
    {
        System.Console.WriteLine(A.Goo());
    }
}
";

            // ILVerify null ref
            // Tracked by https://github.com/dotnet/roslyn/issues/58652
            var compilation2 = CompileAndVerify(
                source2,
                new[] { new CSharpCompilationReference(compilation1) },
                verify: Verification.Fails);
            compilation2.VerifyIL("B.Main()", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloc.0
  IL_0009:  call       ""S A.Goo(S)""
  IL_000e:  box        ""S""
  IL_0013:  call       ""void System.Console.WriteLine(object)""
  IL_0018:  ret
}");
        }

        [WorkItem(543039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543039")]
        [Fact]
        public void BadReturnTypeInOtherAssembly()
        {
            string source1 = @"
public class A
{
    public static Missing Goo() { return null; }
}
";
            var compilation1 = CreateCompilation(source1);
            compilation1.VerifyDiagnostics(
                // (4,19): error CS0246: The type or namespace name 'Missing' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Missing").WithArguments("Missing"));

            string source2 = @"
public class B
{
    public static void Main()
    {
        var f = A.Goo();
        System.Console.WriteLine(f);
    }
}
";
            VerifyEmitDiagnostics(source2, compilation1);
        }

        private static void VerifyEmitDiagnostics(string source2, CSharpCompilation compilation1, params DiagnosticDescription[] expectedDiagnostics)
        {
            var compilation2 = CreateCompilation(source2, new MetadataReference[] { new CSharpCompilationReference(compilation1) });
            compilation2.VerifyDiagnostics(expectedDiagnostics);

            using (var executableStream = new MemoryStream())
            {
                var result = compilation2.Emit(executableStream);
                Assert.False(result.Success);

                result.Diagnostics.Verify(expectedDiagnostics.Concat(new[]
                {
                    // error CS7038: Failed to emit module 'Test': Unable to determine specific cause of the failure.
                    Diagnostic(ErrorCode.ERR_ModuleEmitFailure).WithArguments(compilation2.AssemblyName, "Unable to determine specific cause of the failure.")
                }).ToArray());
            }

            using (var executableStream = new MemoryStream())
            {
                var result = compilation2.Emit(executableStream, options: new EmitOptions(metadataOnly: true));
                Assert.True(result.Success);
                result.Diagnostics.Verify();
            }
        }

        [Fact, WorkItem(530211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530211")]
        public void ModuleNameMismatch()
        {
            var moduleSource = "class Test {}";
            var netModule = CreateCompilation(moduleSource, options: TestOptions.ReleaseModule, assemblyName: "ModuleNameMismatch");

            var moduleMetadata = ModuleMetadata.CreateFromImage(netModule.EmitToArray());

            var source = @"class Module1 { }";

            var compilationOK = CreateCompilation(source, new MetadataReference[] { moduleMetadata.GetReference(filePath: @"R:\A\B\ModuleNameMismatch.netmodule") });

            CompileAndVerify(compilationOK);

            var compilationError = CreateCompilation(source, new MetadataReference[] { moduleMetadata.GetReference(filePath: @"R:\A\B\ModuleNameMismatch.mod") });

            compilationError.VerifyDiagnostics(
                // error CS7086: Module name 'ModuleNameMismatch.netmodule' stored in 'ModuleNameMismatch.mod' must match its filename.
                Diagnostic(ErrorCode.ERR_NetModuleNameMismatch).WithArguments("ModuleNameMismatch.netmodule", "ModuleNameMismatch.mod"));
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void CS0204_ERR_TooManyLocals()
        {
            var builder = new System.Text.StringBuilder();
            builder.Append(@"
public class A
{
    public static int Main ()
        {
");
            for (int i = 0; i < 65536; i++)
            {
                builder.AppendLine(string.Format("    int i{0} = {0};", i));
            }

            builder.Append(@"
        return 1;
        }
}
");

            //Compiling this with optimizations enabled causes the stack scheduler to eliminate a bunch of these locals.
            //It could eliminate 'em all, but doesn't.
            var warnOpts = new System.Collections.Generic.Dictionary<string, ReportDiagnostic>();
            warnOpts.Add(MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_UnreferencedVarAssg), ReportDiagnostic.Suppress);
            var compilation1 = CreateCompilation(builder.ToString(), null, TestOptions.DebugDll.WithSpecificDiagnosticOptions(warnOpts));
            compilation1.VerifyEmitDiagnostics(
                // (4,23): error CS0204: Only 65534 locals, including those generated by the compiler, are allowed
                //     public static int Main ()
                Diagnostic(ErrorCode.ERR_TooManyLocals, "Main"));
        }

        [Fact, WorkItem(8287, "https://github.com/dotnet/roslyn/issues/8287")]
        public void TooManyUserStrings()
        {
            var builder = new System.Text.StringBuilder();
            var expectedOutputBuilder = new System.Text.StringBuilder();
            builder.Append(@"
public class A
{
    public static void Main ()
        {
");
            for (int i = 0; i < 11; i++)
            {
                builder.Append("System.Console.WriteLine(\"");
                builder.Append((char)('A' + i), 1000000);
                expectedOutputBuilder.Append((char)('A' + i), 1000000);
                expectedOutputBuilder.AppendLine();
                builder.Append("\");");
                builder.AppendLine();
            }

            builder.Append(@"
        }
}
");

            var source = builder.ToString();
            var expectedOutput = expectedOutputBuilder.ToString();

            var expectedDiagnostics = new[]
            {
                // (15,26): error CS8103: Combined length of user strings used by the program exceeds allowed limit. Try to decrease use of string literals or try the EXPERIMENTAL feature flag 'experimental-data-section-string-literals'.
                // System.Console.WriteLine("J...J");
                Diagnostic(ErrorCode.ERR_TooManyUserStrings, '"' + new string('J', 1000000) + '"').WithLocation(15, 26),
                // (16,26): error CS8103: Combined length of user strings used by the program exceeds allowed limit. Try to decrease use of string literals or try the EXPERIMENTAL feature flag 'experimental-data-section-string-literals'.
                // System.Console.WriteLine("K...K");
                Diagnostic(ErrorCode.ERR_TooManyUserStrings, '"' + new string('K', 1000000) + '"').WithLocation(16, 26)
            };

            CreateCompilation(source).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(source,
                parseOptions: TestOptions.Regular.WithFeature("experimental-data-section-string-literals", "1000000"))
                .VerifyEmitDiagnostics(expectedDiagnostics);

            CompileAndVerify(source,
                parseOptions: TestOptions.Regular.WithFeature("experimental-data-section-string-literals"),
                verify: Verification.Fails,
                expectedOutput: expectedOutput).VerifyDiagnostics();

            CompileAndVerify(source,
                parseOptions: TestOptions.Regular.WithFeature("experimental-data-section-string-literals", "0"),
                verify: Verification.Fails,
                expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        #endregion
    }
}
