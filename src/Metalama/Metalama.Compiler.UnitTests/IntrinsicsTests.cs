﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Metalama.Compiler.UnitTests
{
    public class IntrinsicsTests : CSharpTestBase
    {
        private readonly List<MetadataReference> _references = new();

        public IntrinsicsTests()
        {
#if NET472_OR_GREATER
            _references.Add(MetadataReference.CreateFromFile(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\netstandard.dll"));
#endif

            _references.Add(MetadataReference.CreateFromFile(typeof(Intrinsics).Assembly.Location));
        }

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
    RuntimeTypeHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeTypeHandle(""{docId}"");
}}";

            var comp2 = CreateCompilation(new[] { originalCode, generatedCode }, _references);
            CompileAndVerify(comp2).VerifyDiagnostics().VerifyIL("G.M", @"
{
    // Code size        6 (0x6)
    .maxstack  1
    IL_0000:  ldtoken    ""C""
    IL_0005:  ret
}");
        }

        [Fact]
        public void GetGenericTypeHandle()
        {
            var originalCode = @"
class C<T> { }";

            var comp1 = CreateCompilation(originalCode);
            comp1.VerifyDiagnostics();

            var symbol = comp1.GetSymbolsWithName("C").Single();

            var docId = DocumentationCommentId.CreateDeclarationId(symbol);

            var generatedCode = $@"
using System;

class G
{{
    RuntimeTypeHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeTypeHandle(""{docId}"");
}}";

            var comp2 = CreateCompilation(new[] { originalCode, generatedCode }, _references);
            CompileAndVerify(comp2).VerifyDiagnostics().VerifyIL("G.M", @"
{
    // Code size        6 (0x6)
    .maxstack  1
    IL_0000:  ldtoken    ""C<T>""
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
    RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(""{docId}"");
}}";

            var comp2 = CreateCompilation(new[] { originalCode, generatedCode }, _references);
            CompileAndVerify(comp2).VerifyDiagnostics().VerifyIL("G.M", @"
{
    // Code size        6 (0x6)
    .maxstack  1
    IL_0000:  ldtoken    ""void C.M()""
    IL_0005:  ret
}");
        }

        [Fact]
        public void GetGenericMethodHandle()
        {
            var originalCode = @"
class C
{
    void M<T>() {}
}";

            var comp1 = CreateCompilation(originalCode);
            comp1.VerifyDiagnostics();

            var symbol = comp1.GetSymbolsWithName("M").Single();

            var docId = DocumentationCommentId.CreateDeclarationId(symbol);

            var generatedCode = $@"
using System;

class G
{{
    RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(""{docId}"");
}}";

            var comp2 = CreateCompilation(new[] { originalCode, generatedCode }, _references);
            CompileAndVerify(comp2).VerifyDiagnostics().VerifyIL("G.M", @"
{
    // Code size        6 (0x6)
    .maxstack  1
    IL_0000:  ldtoken    ""void C.M<T>()""
    IL_0005:  ret
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/49692")]
        public void GetLocalMethodHandle()
        {
            var originalCode = @"
class C
{
    void M() { void Local() {} Local(); }
}";

            var comp1 = CreateCompilation(originalCode);
            comp1.VerifyDiagnostics();

            var syntax = comp1.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LocalFunctionStatementSyntax>().Single();

            var symbol = comp1.GetSemanticModel(comp1.SyntaxTrees.Single()).GetDeclaredSymbol(syntax)!;

            var docId = DocumentationCommentId.CreateDeclarationId(symbol);

            var generatedCode = $@"
using System;

class G
{{
    RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(""{docId}"");
}}";

            var comp2 = CreateCompilation(new[] { originalCode, generatedCode }, _references);
            CompileAndVerify(comp2).VerifyDiagnostics().VerifyIL("G.M", @"
{
    // Code size        6 (0x6)
    .maxstack  1
    IL_0000:  ldtoken    ""void C.M.Local()""
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
    RuntimeFieldHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeFieldHandle(""{docId}"");
}}";

            var comp2 = CreateCompilation(new[] { originalCode, generatedCode }, _references);
            CompileAndVerify(comp2).VerifyIL("G.M", @"
{
    // Code size        6 (0x6)
    .maxstack  1
    IL_0000:  ldtoken    ""int C.f""
    IL_0005:  ret
}");
        }

        [Fact]
        public void GetFieldInGenericTypeHandle()
        {
            var originalCode = @"
class C<T>
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
    RuntimeFieldHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeFieldHandle(""{docId}"");
}}";

            var comp2 = CreateCompilation(new[] { originalCode, generatedCode }, _references);
            CompileAndVerify(comp2).VerifyIL("G.M", @"
{
    // Code size        6 (0x6)
    .maxstack  1
    IL_0000:  ldtoken    ""int C<T>.f""
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
    RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(42);
}";

            var comp = CreateCompilation(code, _references);
            comp.VerifyDiagnostics(
                // (6,75): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //     RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(42);
                Diagnostic(Microsoft.CodeAnalysis.CSharp.ErrorCode.ERR_BadArgType, "42").WithArguments("1", "int", "string").WithLocation(6, 84));
        }

        [Fact]
        public void WrongArgumentCount()
        {
            var code = @"
using System;

class G
{
    RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle("""", """");
}";

            var comp = CreateCompilation(code, _references);
            comp.VerifyDiagnostics(
                // (6,52): error CS1501: No overload for method 'GetRuntimeMethodHandle' takes 2 arguments
                //     RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle("", "");
                Diagnostic(Microsoft.CodeAnalysis.CSharp.ErrorCode.ERR_BadArgCount, "GetRuntimeMethodHandle").WithArguments("GetRuntimeMethodHandle", "2").WithLocation(6, 61));
        }

        [Fact]
        public void NullArgument()
        {
            var code = @"
using System;

class G
{
    RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(null);
}";

            var comp = CreateCompilation(code, _references);
            comp.VerifyEmitDiagnostics(
                // (6,32): error LAMA0606: Argument 'null' is not valid for Metalama.Compiler intrinsic method 'Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(string)'.
                //     RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(null);
                Diagnostic(MetalamaErrorCode.ERR_InvalidIntrinsicUse, "Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(null)").WithArguments("null", "Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(string)").WithLocation(6, 32));
        }

        [Fact]
        public void IncorrectArgument()
        {
            var code = @"
using System;

class G
{
    RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(""incorrect"");
}";

            var comp = CreateCompilation(code, _references);
            comp.VerifyEmitDiagnostics(
                // (6,32): error LAMA0606: Argument '"incorrect"' is not valid for Metalama.Compiler intrinsic method 'Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(string)'.
                //     RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle("incorrect");
                Diagnostic(MetalamaErrorCode.ERR_InvalidIntrinsicUse, @"Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(""incorrect"")").WithArguments("\"incorrect\"", "Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(string)").WithLocation(6, 32));
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
        return Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(docId);
    }
}";

            var comp = CreateCompilation(code, _references);
            comp.VerifyEmitDiagnostics(
                // (9,16): error LAMA0606: Argument 'docId' is not valid for Metalama.Compiler intrinsic method 'Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(string)'.
                //         return Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(docId);
                Diagnostic(MetalamaErrorCode.ERR_InvalidIntrinsicUse, "Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(docId)").WithArguments("docId", "Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(string)").WithLocation(9, 16));
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
    RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(""M:C.op_Explicit"");
}";

            var comp = CreateCompilation(code, _references);
            comp.VerifyEmitDiagnostics(
                // (12,32): error LAMA0606: Argument '"M:C.op_Explicit"' is not valid for Metalama.Compiler intrinsic method 'Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(string)'.
                //     RuntimeMethodHandle M() => Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle("M:C.op_Explicit");
                Diagnostic(MetalamaErrorCode.ERR_InvalidIntrinsicUse, @"Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(""M:C.op_Explicit"")").WithArguments("\"M:C.op_Explicit\"", "Metalama.Compiler.Intrinsics.GetRuntimeMethodHandle(string)").WithLocation(12, 32));
        }
    }
}
