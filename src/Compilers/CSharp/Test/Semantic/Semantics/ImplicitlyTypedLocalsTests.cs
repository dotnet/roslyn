// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class SyntaxBinderTests : CompilingTestBase
    {
        [Fact]
        public void ConstVarField1()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class var {}

class C 
{ 
    const var a = null;
}
");
            var fieldA = compilation.GlobalNamespace.GetMember<TypeSymbol>("C").GetMember<FieldSymbol>("a");
            var typeVar = compilation.GlobalNamespace.GetMember<TypeSymbol>("var");

            Assert.Equal(typeVar, fieldA.Type);
        }

        [Fact]
        public void ConstVarField2()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using var = System.Int32;

class C 
{ 
    const var a = 123;
}
");
            var fieldA = compilation.GlobalNamespace.GetMember<TypeSymbol>("C").GetMember<FieldSymbol>("a");

            Assert.Equal(SpecialType.System_Int32, fieldA.Type.SpecialType);
        }

        [Fact]
        public void ImplicitlyTypedVariableAssignedArrayInitializer()
        {
            string text = @"
var array = { 1, 2 };
";
            CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Script).VerifyDiagnostics(
                // (2,5): error CS0820: Cannot initialize an implicitly-typed variable with an array initializer
                // var array = { 1, 2 };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedArrayInitializer, "array = { 1, 2 }"));
        }

        [Fact]
        public void ImplicitlyTypedVariableCircularReferenceViaMemberAccess()
        {
            string text = @"
class Program
{
    static void Main(string[] args)
    {
        var x = y.Foo(x);
        var y = x.Foo(y);
    }
}";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,23): error CS0841: Cannot use local variable 'x' before it is declared
                //         var x = y.Foo(x);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x"),
                // (6,17): error CS0841: Cannot use local variable 'y' before it is declared
                //         var x = y.Foo(x);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y"),
                // (7,23): error CS0841: Cannot use local variable 'y' before it is declared
                //         var y = x.Foo(y);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y"),
                // (6,23): error CS0165: Use of unassigned local variable 'x'
                //         var x = y.Foo(x);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x"));
        }

        [WorkItem(545612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545612")]
        [Fact]
        public void VarTypeConflictsWithAlias()
        {
            string alias = @"using var = var;";
            string text = @"
class var { }
 
class B
{
    static void Main()
    {
        var a = 1;
        System.Console.WriteLine(a);
    }
}
";
            // If there's no alias to conflict with the type var, then compilation fails
            // because 1 cannot be converted to var.
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,17): error CS0029: Cannot implicitly convert type 'int' to 'var'
                //         var a = 1;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "var"));

            // However, once the alias is introduced, the local becomes implicitly typed
            // and everything works.
            var verifier = CompileAndVerify(alias + text, expectedOutput: "1");
            verifier.VerifyIL("B.Main", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""void System.Console.WriteLine(int)""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void VarBeforeCSharp3()
        {
            var source = @"
class C
{
    void M()
    {
        var v = 1;
        System.Console.WriteLine(v);
    }
}

class D
{
    class var
    {
        public static implicit operator var(int x) { return null; }
    }

    void M()
    {
        var v = 1;
        System.Console.WriteLine(v);
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp3)).VerifyDiagnostics();
            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp2)).VerifyDiagnostics(
                // (6,9): error CS8023: Feature 'implicitly typed local variable' is not available in C# 2.  Please use language version 3 or greater.
                //         var v = 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "var").WithArguments("implicitly typed local variable", "3"));
        }
    }
}
