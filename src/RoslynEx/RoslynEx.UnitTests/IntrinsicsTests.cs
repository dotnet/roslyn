using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace RoslynEx.UnitTests
{
    public class IntrinsicsTests : CSharpTestBase
    {
        [Fact]
        public void GetTypeHandle()
        {
            var originalCode = @"
class C { }";

            var comp1 = CreateCompilation(originalCode);
            comp1.VerifyDiagnostics();

            var symbol = comp1.GetSymbolsWithName("C").Single();

            var docId = DocumentationCommentId.CreateDeclarationId(symbol);

            var generatedCode = $@"
using System;

class G
{{
    RuntimeTypeHandle M() => RoslynEx.Intrinsics.GetRuntimeTypeHandle(""{docId}"");
}}";

            var comp2 = CreateCompilation(new[] { originalCode, generatedCode }, new[] { MetadataReference.CreateFromFile(typeof(Intrinsics).Assembly.Location) });
            CompileAndVerify(comp2).VerifyDiagnostics().VerifyIL("G.M", @"
{
    // Code size        6 (0x6)
    .maxstack  1
    IL_0000:  ldtoken    ""C""
    IL_0005:  ret
}");
        }

        [Fact]
        public void GetMethodHandle()
        {
            var originalCode = @"
class C
{
    void M() {}
}";

            var comp1 = CreateCompilation(originalCode);
            comp1.VerifyDiagnostics();

            var symbol = comp1.GetSymbolsWithName("M").Single();

            var docId = DocumentationCommentId.CreateDeclarationId(symbol);

            var generatedCode = $@"
using System;

class G
{{
    RuntimeMethodHandle M() => RoslynEx.Intrinsics.GetRuntimeMethodHandle(""{docId}"");
}}";

            var comp2 = CreateCompilation(new[] { originalCode, generatedCode }, new[] { MetadataReference.CreateFromFile(typeof(Intrinsics).Assembly.Location) });
            CompileAndVerify(comp2).VerifyDiagnostics().VerifyIL("G.M", @"
{
    // Code size        6 (0x6)
    .maxstack  1
    IL_0000:  ldtoken    ""void C.M()""
    IL_0005:  ret
}");
        }

        [Fact]
        public void GetFieldHandle()
        {
            var originalCode = @"
class C
{
    int f;
}";

            var comp1 = CreateCompilation(originalCode);

            var symbol = comp1.GetSymbolsWithName("f").Single();

            var docId = DocumentationCommentId.CreateDeclarationId(symbol);

            var generatedCode = $@"
using System;

class G
{{
    RuntimeFieldHandle M() => RoslynEx.Intrinsics.GetRuntimeFieldHandle(""{docId}"");
}}";

            var comp2 = CreateCompilation(new[] { originalCode, generatedCode }, new[] { MetadataReference.CreateFromFile(typeof(Intrinsics).Assembly.Location) });
            CompileAndVerify(comp2).VerifyIL("G.M", @"
{
    // Code size        6 (0x6)
    .maxstack  1
    IL_0000:  ldtoken    ""int C.f""
    IL_0005:  ret
}");
        }

        [Fact]
        public void WrongArgumentType()
        {
            var code = @"
using System;

class G
{
    RuntimeMethodHandle M() => RoslynEx.Intrinsics.GetRuntimeMethodHandle(42);
}";

            var comp = CreateCompilation(code, new[] { MetadataReference.CreateFromFile(typeof(Intrinsics).Assembly.Location) });
            comp.VerifyDiagnostics(
                // (6,75): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //     RuntimeMethodHandle M() => RoslynEx.Intrinsics.GetRuntimeMethodHandle(42);
                Diagnostic(Microsoft.CodeAnalysis.CSharp.ErrorCode.ERR_BadArgType, "42").WithArguments("1", "int", "string").WithLocation(6, 75));
        }

        [Fact]
        public void WrongArgumentCount()
        {
            var code = @"
using System;

class G
{
    RuntimeMethodHandle M() => RoslynEx.Intrinsics.GetRuntimeMethodHandle("""", """");
}";

            var comp = CreateCompilation(code, new[] { MetadataReference.CreateFromFile(typeof(Intrinsics).Assembly.Location) });
            comp.VerifyDiagnostics(
                // (6,52): error CS1501: No overload for method 'GetRuntimeMethodHandle' takes 2 arguments
                //     RuntimeMethodHandle M() => RoslynEx.Intrinsics.GetRuntimeMethodHandle("", "");
                Diagnostic(Microsoft.CodeAnalysis.CSharp.ErrorCode.ERR_BadArgCount, "GetRuntimeMethodHandle").WithArguments("GetRuntimeMethodHandle", "2").WithLocation(6, 52));
        }

        [Fact]
        public void NullArgument()
        {
            var code = @"
using System;

class G
{
    RuntimeMethodHandle M() => RoslynEx.Intrinsics.GetRuntimeMethodHandle(null);
}";

            var comp = CreateCompilation(code, new[] { MetadataReference.CreateFromFile(typeof(Intrinsics).Assembly.Location) });
            comp.VerifyEmitDiagnostics(
                // (6,32): error RE0006: Argument 'null' is not valid for RoslynEx intrinsic method 'RoslynEx.Intrinsics.GetRuntimeMethodHandle(string)'.
                //     RuntimeMethodHandle M() => RoslynEx.Intrinsics.GetRuntimeMethodHandle(null);
                Diagnostic(ErrorCode.ERR_InvalidIntrinsicUse, "RoslynEx.Intrinsics.GetRuntimeMethodHandle(null)").WithArguments("null", "RoslynEx.Intrinsics.GetRuntimeMethodHandle(string)").WithLocation(6, 32));
        }

        [Fact]
        public void IncorrectArgument()
        {
            var code = @"
using System;

class G
{
    RuntimeMethodHandle M() => RoslynEx.Intrinsics.GetRuntimeMethodHandle(""incorrect"");
}";

            var comp = CreateCompilation(code, new[] { MetadataReference.CreateFromFile(typeof(Intrinsics).Assembly.Location) });
            comp.VerifyEmitDiagnostics(
                // (6,32): error RE0006: Argument '"incorrect"' is not valid for RoslynEx intrinsic method 'RoslynEx.Intrinsics.GetRuntimeMethodHandle(string)'.
                //     RuntimeMethodHandle M() => RoslynEx.Intrinsics.GetRuntimeMethodHandle("incorrect");
                Diagnostic(ErrorCode.ERR_InvalidIntrinsicUse, @"RoslynEx.Intrinsics.GetRuntimeMethodHandle(""incorrect"")").WithArguments("\"incorrect\"", "RoslynEx.Intrinsics.GetRuntimeMethodHandle(string)").WithLocation(6, 32));
        }

        [Fact]
        public void NonConstantArgument()
        {
            var code = @"
using System;

class G
{
    RuntimeMethodHandle M()
    {
        var docId = ""T:G"";
        return RoslynEx.Intrinsics.GetRuntimeMethodHandle(docId);
    }
}";

            var comp = CreateCompilation(code, new[] { MetadataReference.CreateFromFile(typeof(Intrinsics).Assembly.Location) });
            comp.VerifyEmitDiagnostics(
                // (9,16): error RE0006: Argument 'docId' is not valid for RoslynEx intrinsic method 'RoslynEx.Intrinsics.GetRuntimeMethodHandle(string)'.
                //         return RoslynEx.Intrinsics.GetRuntimeMethodHandle(docId);
                Diagnostic(ErrorCode.ERR_InvalidIntrinsicUse, "RoslynEx.Intrinsics.GetRuntimeMethodHandle(docId)").WithArguments("docId", "RoslynEx.Intrinsics.GetRuntimeMethodHandle(string)").WithLocation(9, 16));
        }

        [Fact]
        public void AmbiguousArgument()
        {
            var code = @"
using System;

class C
{
	public static explicit operator int(C c) => 0;
	public static explicit operator string(C c) => """";
}

class G
{
    RuntimeMethodHandle M() => RoslynEx.Intrinsics.GetRuntimeMethodHandle(""M:C.op_Explicit"");
}";

            var comp = CreateCompilation(code, new[] { MetadataReference.CreateFromFile(typeof(Intrinsics).Assembly.Location) });
            comp.VerifyEmitDiagnostics(
                // (12,32): error RE0006: Argument '"M:C.op_Explicit"' is not valid for RoslynEx intrinsic method 'RoslynEx.Intrinsics.GetRuntimeMethodHandle(string)'.
                //     RuntimeMethodHandle M() => RoslynEx.Intrinsics.GetRuntimeMethodHandle("M:C.op_Explicit");
                Diagnostic(ErrorCode.ERR_InvalidIntrinsicUse, @"RoslynEx.Intrinsics.GetRuntimeMethodHandle(""M:C.op_Explicit"")").WithArguments("\"M:C.op_Explicit\"", "RoslynEx.Intrinsics.GetRuntimeMethodHandle(string)").WithLocation(12, 32));
        }
    }
}
