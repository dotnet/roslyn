// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests that exercise warnings that are under control of the compiler option <see cref="CompilationOptions.WarningLevel"/>
    /// for values >= 5.
    /// </summary>
    public class WarningVersionTests : CompilingTestBase
    {
        [Fact]
        public void WRN_NubExprIsConstBool2()
        {
            var source = @"
class Program
{
    public static void M(S s)
    {
        if (s == null) { }
        if (s != null) { }
    }
}
struct S
{
    public static bool operator==(S s1, S s2) => false;
    public static bool operator!=(S s1, S s2) => true;
    public override bool Equals(object other) => false;
    public override int GetHashCode() => 0;
}
";
            var whenWave5 = new[]
            {
                // (6,13): warning CS8073: The result of the expression is always 'false' since a value of type 'S' is never equal to 'null' of type 'S?'
                //         if (s == null) { }
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "s == null").WithArguments("false", "S", "S?").WithLocation(6, 13),
                // (7,13): warning CS8073: The result of the expression is always 'true' since a value of type 'S' is never equal to 'null' of type 'S?'
                //         if (s != null) { }
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "s != null").WithArguments("true", "S", "S?").WithLocation(7, 13)
            };
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel)).VerifyDiagnostics();
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(3)).VerifyDiagnostics();
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(4)).VerifyDiagnostics();
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(whenWave5);
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6)).VerifyDiagnostics(whenWave5);
        }

        [Fact]
        public void WRN_StaticInAsOrIs()
        {
            var source = @"
class Program
{
    public static void M(object o)
    {
        if (o is SC)
            _ = o as SC;
    }
}
static class SC { }
";
            var whenWave5 = new[]
            {
                // (6,13): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'SC'
                //         if (o is SC)
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, "o is SC").WithArguments("SC").WithLocation(6, 13),
                // (7,17): warning CS7023: The second operand of an 'is' or 'as' operator may not be static type 'SC'
                //             _ = o as SC;
                Diagnostic(ErrorCode.WRN_StaticInAsOrIs, "o as SC").WithArguments("SC").WithLocation(7, 17)
            };
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel)).VerifyDiagnostics();
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(4)).VerifyDiagnostics();
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(whenWave5);
        }

        [Fact]
        public void WRN_PrecedenceInversion()
        {
            var source = @"
using System;

class X
{
    public bool Select<T>(Func<int, T> selector) => true;
    public static int operator +(Action a, X right) => 0;
}

class P
{
    static void M1()
    {
        var src = new X();
        var b = false && from x in src select x;
    }
    static void M2()
    {
        var x = new X();
        var i = ()=>{} + x;
    }
}";
            var whenWave5 = new[]
            {
                // (15,26): warning CS8848: Operator 'from' cannot be used here due to precedence. Use parentheses to disambiguate.
                //         var b = false && from x in src select x;
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "from x in src").WithArguments("from").WithLocation(15, 26),
                // (20,24): warning CS8848: Operator '+' cannot be used here due to precedence. Use parentheses to disambiguate.
                //         var i = ()=>{} + x;
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "+").WithArguments("+").WithLocation(20, 24)
            };
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel)).VerifyDiagnostics();
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(4)).VerifyDiagnostics();
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(whenWave5);
        }

        [Fact]
        public void WRN_UnassignedThisAutoProperty()
        {
            var source = @"
public struct Struct
{
    private string data;
}
";
            var comp1 = CreateCompilation(source, options: TestOptions.DebugModule);
            var moduleReference = new[] { comp1.EmitToImageReference() };

            var source2 =
@"public struct Program
{
    public Struct Property { get; }
    public Program(int dummy)
    {
    }
}";
            var expectedIL = @"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Struct Program.<Property>k__BackingField""
  IL_0007:  initobj    ""Struct""
  IL_000d:  ret
}
";
            // C# 10
            var verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program..ctor", expectedIL);

            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(5),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (4,12): warning CS8880: Auto-implemented property 'Program.Property' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                //     public Program(int dummy)
                Diagnostic(ErrorCode.WRN_UnassignedThisAutoPropertyUnsupportedVersion, "Program").WithArguments("Program.Property", "11.0").WithLocation(4, 12));
            verifier.VerifyIL("Program..ctor", expectedIL);

            // C# 11+
            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(5),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program..ctor", expectedIL);

            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll
                    .WithWarningLevel(5)
                    .WithSpecificDiagnosticOptions(ReportStructInitializationWarnings),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (4,12): warning CS9020: Control is returned to caller before auto-implemented property 'Program.Property' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public Program(int dummy)
                Diagnostic(ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion, "Program").WithArguments("Program.Property").WithLocation(4, 12));
            verifier.VerifyIL("Program..ctor", expectedIL);
        }

        [Fact]
        public void WRN_UnassignedThis()
        {
            var source = @"
public struct Struct
{
    private string data;
}
";
            var comp1 = CreateCompilation(source, options: TestOptions.DebugModule);
            var moduleReference = new[] { comp1.EmitToImageReference() };

            var source2 =
@"public struct Program
{
    public Struct Field;
    public Program(int dummy)
    {
    }
}";
            var expectedIL = @"
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Struct Program.Field""
  IL_0007:  initobj    ""Struct""
  IL_000d:  ret
}
";
            // C# 10
            var verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program..ctor", expectedIL);

            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(5),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (4,12): warning CS8881: Field 'Program.Field' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public Program(int dummy)
                Diagnostic(ErrorCode.WRN_UnassignedThisUnsupportedVersion, "Program").WithArguments("Program.Field", "11.0").WithLocation(4, 12));
            verifier.VerifyIL("Program..ctor", expectedIL);

            // C# 11+
            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll
                    .WithWarningLevel(5),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program..ctor", expectedIL);

            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll
                    .WithWarningLevel(5)
                    .WithSpecificDiagnosticOptions(ReportStructInitializationWarnings),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (4,12): warning CS9021: Control is returned to caller before field 'Program.Field' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public Program(int dummy)
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "Program").WithArguments("Program.Field").WithLocation(4, 12));
            verifier.VerifyIL("Program..ctor", expectedIL);
        }

        [Fact]
        public void UnassignedThisField_And_UnassignedLocal()
        {
            var source = @"
public struct Struct
{
    private string data;
}
";
            var comp1 = CreateCompilation(source, options: TestOptions.DebugModule);
            var moduleReference = new[] { comp1.EmitToImageReference() };

            var source2 =
@"public struct Program
{
    public Struct Field;
    public Program(int dummy)
    {
        Struct s;
        s.ToString();
    }
}";
            var expectedIL = @"
{
  // Code size       28 (0x1c)
  .maxstack  1
  .locals init (Struct V_0) //s
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Struct Program.Field""
  IL_0007:  initobj    ""Struct""
  IL_000d:  ldloca.s   V_0
  IL_000f:  constrained. ""Struct""
  IL_0015:  callvirt   ""string object.ToString()""
  IL_001a:  pop
  IL_001b:  ret
}
";
            // C# 10
            var verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program..ctor", expectedIL);

            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(5),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (4,12): warning CS8881: Field 'Program.Field' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public Program(int dummy)
                Diagnostic(ErrorCode.WRN_UnassignedThisUnsupportedVersion, "Program").WithArguments("Program.Field", "11.0").WithLocation(4, 12),
                // (7,9): warning CS8887: Use of unassigned local variable 's'
                //         s.ToString();
                Diagnostic(ErrorCode.WRN_UseDefViolation, "s").WithArguments("s").WithLocation(7, 9));
            verifier.VerifyIL("Program..ctor", expectedIL);

            // C# 11+
            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(5),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (7,9): warning CS8887: Use of unassigned local variable 's'
                //         s.ToString();
                Diagnostic(ErrorCode.WRN_UseDefViolation, "s").WithArguments("s").WithLocation(7, 9));
            verifier.VerifyIL("Program..ctor", expectedIL);

            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll
                    .WithWarningLevel(5)
                    .WithSpecificDiagnosticOptions(ReportStructInitializationWarnings),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (4,12): warning CS9021: Control is returned to caller before field 'Program.Field' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public Program(int dummy)
                Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "Program").WithArguments("Program.Field").WithLocation(4, 12),
                // (7,9): warning CS8887: Use of unassigned local variable 's'
                //         s.ToString();
                Diagnostic(ErrorCode.WRN_UseDefViolation, "s").WithArguments("s").WithLocation(7, 9));
            verifier.VerifyIL("Program..ctor", expectedIL);
        }

        [Fact]
        public void WRN_ParamUnassigned()
        {
            var source = @"
public struct Struct
{
    private string data;
}
";
            var comp1 = CreateCompilation(source, options: TestOptions.DebugModule);
            var moduleReference = comp1.EmitToImageReference();

            var source2 =
@"public class Program
{
    void M(out Struct param)
    {
    }
}";
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel)).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (3,10): warning CS8824: The out parameter 'param' must be assigned to before control leaves the current method
                //     void M(out Struct param)
                Diagnostic(ErrorCode.WRN_ParamUnassigned, "M").WithArguments("param").WithLocation(3, 10)
                );
        }

        [Fact]
        public void WRN_UseDefViolationProperty()
        {
            var source = @"
public struct Struct
{
    private string data;
}
";
            var comp1 = CreateCompilation(source, options: TestOptions.DebugModule);
            var moduleReference = new[] { comp1.EmitToImageReference() };

            var source2 =
@"public struct Program
{
    public Struct Property { get; }
    Program(int dummy)
    {
        Struct v2 = Property;
        Property = default;
    }
}";
            var expectedIL = @"
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (Struct V_0) //v2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Struct Program.<Property>k__BackingField""
  IL_0007:  initobj    ""Struct""
  IL_000d:  ldarg.0
  IL_000e:  call       ""readonly Struct Program.Property.get""
  IL_0013:  stloc.0
  IL_0014:  ldarg.0
  IL_0015:  ldflda     ""Struct Program.<Property>k__BackingField""
  IL_001a:  initobj    ""Struct""
  IL_0020:  ret
}
";
            // C# 10
            var verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program..ctor", expectedIL);

            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(5),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (6,21): warning CS9016: Use of possibly unassigned auto-implemented property 'Property'. Consider updating to language version '11.0' to auto-default the property.
                //         Struct v2 = Property;
                Diagnostic(ErrorCode.WRN_UseDefViolationPropertyUnsupportedVersion, "Property").WithArguments("Property", "11.0").WithLocation(6, 21));
            verifier.VerifyIL("Program..ctor", expectedIL);

            // C# 11+
            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(5),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program..ctor", expectedIL);

            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll
                    .WithWarningLevel(5)
                    .WithSpecificDiagnosticOptions(ReportStructInitializationWarnings),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (6,21): warning CS9014: Use of possibly unassigned auto-implemented property 'Property'
                //         Struct v2 = Property;
                Diagnostic(ErrorCode.WRN_UseDefViolationPropertySupportedVersion, "Property").WithArguments("Property").WithLocation(6, 21));
            verifier.VerifyIL("Program..ctor", expectedIL);
        }

        [Fact]
        public void WRN_UseDefViolationField()
        {
            var source = @"
public struct Struct
{
    private string data;
}
";
            var comp1 = CreateCompilation(source, options: TestOptions.DebugModule);
            var moduleReference = new[] { comp1.EmitToImageReference() };

            var source2 =
@"public struct Program
{
    public Struct Field;
    Program(int dummy)
    {
        Struct v2 = Field;
        Field = default;
    }
}";
            var expectedIL = @"
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (Struct V_0) //v2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Struct Program.Field""
  IL_0007:  initobj    ""Struct""
  IL_000d:  ldarg.0
  IL_000e:  ldfld      ""Struct Program.Field""
  IL_0013:  stloc.0
  IL_0014:  ldarg.0
  IL_0015:  ldflda     ""Struct Program.Field""
  IL_001a:  initobj    ""Struct""
  IL_0020:  ret
}
";
            // C# 10
            var verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program..ctor", expectedIL);

            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(5),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (6,21): warning CS9017: Use of possibly unassigned field 'Field'. Consider updating to language version '11.0' to auto-default the field.
                //         Struct v2 = Field;
                Diagnostic(ErrorCode.WRN_UseDefViolationFieldUnsupportedVersion, "Field").WithArguments("Field", "11.0").WithLocation(6, 21));
            verifier.VerifyIL("Program..ctor", expectedIL);

            // C# 11+
            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(5),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program..ctor", expectedIL);
            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll
                    .WithWarningLevel(5)
                    .WithSpecificDiagnosticOptions(ReportStructInitializationWarnings),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (6,21): warning CS9014: Use of possibly unassigned field 'Field'
                //         Struct v2 = Field;
                Diagnostic(ErrorCode.WRN_UseDefViolationFieldSupportedVersion, "Field").WithArguments("Field").WithLocation(6, 21));
            verifier.VerifyIL("Program..ctor", expectedIL);
        }

        [Fact]
        public void WRN_UseDefViolationThis()
        {
            var source = @"
public struct Struct
{
    private string data;
}
";
            var comp1 = CreateCompilation(source, options: TestOptions.DebugModule);
            var moduleReference = new[] { comp1.EmitToImageReference() };

            var source2 =
@"public struct Program
{
    public Struct Field;
    Program(int dummy)
    {
        Program p2 = this;
        this.Field = default;
    }
}";
            var expectedIL = @"
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (Program V_0) //p2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Struct Program.Field""
  IL_0007:  initobj    ""Struct""
  IL_000d:  ldarg.0
  IL_000e:  ldobj      ""Program""
  IL_0013:  stloc.0
  IL_0014:  ldarg.0
  IL_0015:  ldflda     ""Struct Program.Field""
  IL_001a:  initobj    ""Struct""
  IL_0020:  ret
}
";
            // C# 10
            var verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program..ctor", expectedIL);

            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(5),
                parseOptions: TestOptions.Regular10,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (6,22): warning CS8885: The 'this' object cannot be used before all of its fields have been assigned. Consider updating to language version '11.0' to auto-default the unassigned fields.
                //         Program p2 = this;
                Diagnostic(ErrorCode.WRN_UseDefViolationThisUnsupportedVersion, "this").WithArguments("11.0").WithLocation(6, 22));
            verifier.VerifyIL("Program..ctor", expectedIL);

            // C# 11+
            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll.WithWarningLevel(5),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program..ctor", expectedIL);

            verifier = CompileAndVerify(
                source2,
                references: moduleReference,
                options: TestOptions.DebugDll
                    .WithWarningLevel(5)
                    .WithSpecificDiagnosticOptions(ReportStructInitializationWarnings),
                parseOptions: TestOptions.Regular11,
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (6,22): warning CS9020: The 'this' object is read before all of its fields have been assigned, causing preceding implicit assignments of 'default' to non-explicitly assigned fields.
                //         Program p2 = this;
                Diagnostic(ErrorCode.WRN_UseDefViolationThisSupportedVersion, "this").WithLocation(6, 22));
            verifier.VerifyIL("Program..ctor", expectedIL);
        }

        [Fact]
        public void WRN_UseDefViolationOut()
        {
            var source = @"
public struct Struct
{
    private string data;
}
";
            var comp1 = CreateCompilation(source, options: TestOptions.DebugModule);
            var moduleReference = comp1.EmitToImageReference();

            var source2 =
@"class Program
{
    public static void M(out Struct r1)
    {
        var r2 = r1;
        r1 = default;
    }
}";
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel)).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (5,18): warning CS8828: Use of unassigned out parameter 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.WRN_UseDefViolationOut, "r1").WithArguments("r1").WithLocation(5, 18)
                );
        }

        [Fact]
        public void WRN_UseDefViolation()
        {
            var source = @"
public struct Struct
{
    private string data;
}
";
            var comp1 = CreateCompilation(source, options: TestOptions.DebugModule);
            var moduleReference = comp1.EmitToImageReference();

            var source2 =
@"class Program
{
    public static void Main()
    {
        Struct r1;
        var r2 = r1;
    }
}";
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(CodeAnalysis.Diagnostic.DefaultWarningLevel)).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (6,18): warning CS8829: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.WRN_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
        }
    }
}
