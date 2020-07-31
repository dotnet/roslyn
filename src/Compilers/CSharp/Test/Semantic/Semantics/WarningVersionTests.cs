// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            CreateCompilation(source).VerifyDiagnostics();
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
            CreateCompilation(source).VerifyDiagnostics();
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
            CreateCompilation(source).VerifyDiagnostics();
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
            var moduleReference = comp1.EmitToImageReference();

            var source2 =
@"public struct Program
{
    public Struct Property { get; }
    public Program(int dummy)
    {
    }
}";
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (4,12): warning CS8822: Auto-implemented property 'Program.Property' must be fully assigned before control is returned to the caller.
                //     public Program(int dummy)
                Diagnostic(ErrorCode.WRN_UnassignedThisAutoProperty, "Program").WithArguments("Program.Property").WithLocation(4, 12)
                );
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
            var moduleReference = comp1.EmitToImageReference();

            var source2 =
@"public struct Program
{
    public Struct Field;
    public Program(int dummy)
    {
    }
}";
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (4,12): warning CS8823: Field 'Program.Field' must be fully assigned before control is returned to the caller
                //     public Program(int dummy)
                Diagnostic(ErrorCode.WRN_UnassignedThis, "Program").WithArguments("Program.Field").WithLocation(4, 12)
                );
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
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }).VerifyDiagnostics(
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
            var moduleReference = comp1.EmitToImageReference();

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
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (6,21): warning CS8825: Use of possibly unassigned auto-implemented property 'Property'
                //         Struct v2 = Property;
                Diagnostic(ErrorCode.WRN_UseDefViolationProperty, "Property").WithArguments("Property").WithLocation(6, 21)
                );
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
            var moduleReference = comp1.EmitToImageReference();

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
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (6,21): warning CS8826: Use of possibly unassigned field 'Field'
                //         Struct v2 = Field;
                Diagnostic(ErrorCode.WRN_UseDefViolationField, "Field").WithArguments("Field").WithLocation(6, 21)
                );
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
            var moduleReference = comp1.EmitToImageReference();

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
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (6,22): warning CS8827: The 'this' object cannot be used before all of its fields have been assigned
                //         Program p2 = this;
                Diagnostic(ErrorCode.WRN_UseDefViolationThis, "this").WithArguments("this").WithLocation(6, 22)
                );
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
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }).VerifyDiagnostics(
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
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }).VerifyDiagnostics(
                );
            CreateCompilation(source2, references: new MetadataReference[] { moduleReference }, options: TestOptions.ReleaseDll.WithWarningLevel(5)).VerifyDiagnostics(
                // (6,18): warning CS8829: Use of unassigned local variable 'r1'
                //         var r2 = r1;
                Diagnostic(ErrorCode.WRN_UseDefViolation, "r1").WithArguments("r1").WithLocation(6, 18)
                );
        }
    }
}
