// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTests_Global : PatternMatchingTestBase
    {

        [Fact]
        public void GlobalCode_ExpressionStatement_01()
        {
            string source =
@"
H.Dummy(1 is int x1);
H.Dummy(x1);

object x2;
H.Dummy(2 is int x2);

H.Dummy(3 is int x3);
object x3;

H.Dummy((41 is int x4),
        (42 is int x4));

void Test()
{
    H.Dummy(x1, x2, x3, x4);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,18): error CS0102: The type 'Script' already contains a definition for 'x2'
                // H.Dummy(2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,20): error CS0102: The type 'Script' already contains a definition for 'x4'
                //         (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 20),
                // (16,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(16, 17),
                // (16,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(16, 21),
                // (16,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (6,18): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // H.Dummy(2 is int x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 18),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,20): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //         (42 is int x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 20),
                    // (19,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(19, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDuplicateVariableDeclarationInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDuplicateVariableDeclarationInSameScope(model, x4Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_ExpressionStatement_02()
        {
            string source =
@"
H.Dummy(1 is var x1);
H.Dummy(x1);

object x2;
H.Dummy(2 is var x2);

H.Dummy(3 is var x3);
object x3;

H.Dummy((41 is var x4),
        (42 is var x4));

void Test()
{
    H.Dummy(x1, x2, x3, x4);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,18): error CS0102: The type 'Script' already contains a definition for 'x2'
                // H.Dummy(2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,20): error CS0102: The type 'Script' already contains a definition for 'x4'
                //         (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 20),
                // (16,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(16, 17),
                // (16,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(16, 21),
                // (16,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (6,18): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // H.Dummy(2 is var x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 18),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,20): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //         (42 is var x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 20),
                    // (19,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(19, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDuplicateVariableDeclarationInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDuplicateVariableDeclarationInSameScope(model, x4Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_ExpressionStatement_03()
        {
            string source =
@"
System.Console.WriteLine(x1);
H.Dummy(1 is var x1);
Test();

void Test()
{
    System.Console.WriteLine(x1);
}
class H
{
    public static void Dummy(params object[] x) {}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_IfStatement_01()
        {
            string source =
@"
if ((1 is int x1)) {}
H.Dummy(x1);

object x2;
if ((2 is int x2)) {}

if ((3 is int x3)) {}
object x3;

if (H.Dummy((41 is int x4),
            (42 is int x4))) {}

if ((51 is int x5)) 
{
    H.Dummy(""52"" is string x5);
    H.Dummy(x5);
}
H.Dummy(x5);

int x6 = 6;
if (H.Dummy()) 
{
    string x6 = ""6"";
    H.Dummy(x6);
}

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,15): error CS0102: The type 'Script' already contains a definition for 'x2'
                // if ((2 is int x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 15),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,24): error CS0102: The type 'Script' already contains a definition for 'x4'
                //             (42 is int x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 24),
                // (30,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(30, 17),
                // (30,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(30, 21),
                // (30,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(30, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationField(model, x5Decl[0], x5Ref[1], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (6,15): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // if ((2 is int x2)) {}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 15),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,24): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //             (42 is int x4))) {}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 24),
                    // (16,28): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     H.Dummy("52" is string x5);
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(16, 28),
                    // (21,5): warning CS0219: The variable 'x6' is assigned but its value is never used
                    // int x6 = 6;
                    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x6").WithArguments("x6").WithLocation(21, 5),
                    // (24,12): error CS0136: A local or parameter named 'x6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     string x6 = "6";
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x6").WithArguments("x6").WithLocation(24, 12),
                    // (33,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(33, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref[1], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
            }
        }

        [Fact]
        public void GlobalCode_IfStatement_02()
        {
            string source =
@"
if ((1 is var x1)) {}
H.Dummy(x1);

object x2;
if ((2 is var x2)) {}

if ((3 is var x3)) {}
object x3;

if (H.Dummy((41 is var x4),
            (42 is var x4))) {}

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

if ((51 is var x5)) 
{
    H.Dummy(""52"" is var x5);
    H.Dummy(x5);
}
H.Dummy(x5);

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,15): error CS0102: The type 'Script' already contains a definition for 'x2'
                // if ((2 is var x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 15),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,24): error CS0102: The type 'Script' already contains a definition for 'x4'
                //             (42 is var x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 24),
                // (16,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(16, 17),
                // (16,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(16, 21),
                // (16,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationField(model, x5Decl[0], x5Ref[0], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[1]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (6,15): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // if ((2 is var x2)) {}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 15),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,24): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //             (42 is var x4))) {}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 24),
                    // (16,29): error CS0841: Cannot use local variable 'x5' before it is declared
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x5").WithArguments("x5").WithLocation(16, 29),
                    // (21,25): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     H.Dummy("52" is var x5);
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(21, 25),
                    // (26,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(26, 1),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref[0], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[1]);
            }
        }

        [Fact]
        public void GlobalCode_IfStatement_03()
        {
            string source =
@"
System.Console.WriteLine(x1);
if ((1 is var x1)) 
{
    H.Dummy(""11"" is var x1);
    System.Console.WriteLine(x1);
}
Test();

void Test()
{
    System.Console.WriteLine(x1);
}
class H
{
    public static bool Dummy(params object[] x) {return false;}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
11
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl[0], x1Ref[0], x1Ref[2]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
        }

        [Fact]
        public void GlobalCode_IfStatement_04()
        {
            string source =
@"
System.Console.WriteLine(x1);
if ((1 is var x1)) 
    H.Dummy((""11"" is var x1), x1);
Test();

void Test()
{
    System.Console.WriteLine(x1);
}

class H
{
    public static void Dummy(object x, object y)
    {
        System.Console.WriteLine(y);
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
11
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl[0], x1Ref[0], x1Ref[2]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
        }

        [Fact]
        public void GlobalCode_YieldReturnStatement_01()
        {
            string source =
@"
yield return (1 is int x1);
H.Dummy(x1);

object x2;
yield return (2 is int x2);

yield return (3 is int x3);
object x3;

yield return H.Dummy((41 is int x4),
                     (42 is int x4));

void Test()
{
    H.Dummy(x1, x2, x3, x4);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,24): error CS0102: The type 'Script' already contains a definition for 'x2'
                // yield return (2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,33): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                      (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 33),
                // (2,1): error CS7020: Cannot use 'yield' in top-level script code
                // yield return (1 is int x1);
                Diagnostic(ErrorCode.ERR_YieldNotAllowedInScript, "yield").WithLocation(2, 1),
                // (6,1): error CS7020: Cannot use 'yield' in top-level script code
                // yield return (2 is int x2);
                Diagnostic(ErrorCode.ERR_YieldNotAllowedInScript, "yield").WithLocation(6, 1),
                // (8,1): error CS7020: Cannot use 'yield' in top-level script code
                // yield return (3 is int x3);
                Diagnostic(ErrorCode.ERR_YieldNotAllowedInScript, "yield").WithLocation(8, 1),
                // (11,1): error CS7020: Cannot use 'yield' in top-level script code
                // yield return H.Dummy((41 is int x4),
                Diagnostic(ErrorCode.ERR_YieldNotAllowedInScript, "yield").WithLocation(11, 1),
                // (16,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(16, 17),
                // (16,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(16, 21),
                // (16,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (2,1): error CS1624: The body of '<top-level-statements-entry-point>' cannot be an iterator block because 'void' is not an iterator interface type
                    // yield return (1 is int x1);
                    Diagnostic(ErrorCode.ERR_BadIteratorReturn, "yield return (1 is int x1);").WithArguments("<top-level-statements-entry-point>", "void").WithLocation(2, 1),
                    // (6,24): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // yield return (2 is int x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 24),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,33): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                      (42 is int x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 33),
                    // (19,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(19, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_YieldReturnStatement_02()
        {
            string source =
@"
yield return (1 is var x1);
H.Dummy(x1);

object x2;
yield return (2 is var x2);

yield return (3 is var x3);
object x3;

yield return H.Dummy((41 is var x4),
                     (42 is var x4));

void Test()
{
    H.Dummy(x1, x2, x3, x4);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,24): error CS0102: The type 'Script' already contains a definition for 'x2'
                // yield return (2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,33): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                      (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 33),
                // (2,1): error CS7020: Cannot use 'yield' in top-level script code
                // yield return (1 is var x1);
                Diagnostic(ErrorCode.ERR_YieldNotAllowedInScript, "yield").WithLocation(2, 1),
                // (6,1): error CS7020: Cannot use 'yield' in top-level script code
                // yield return (2 is var x2);
                Diagnostic(ErrorCode.ERR_YieldNotAllowedInScript, "yield").WithLocation(6, 1),
                // (8,1): error CS7020: Cannot use 'yield' in top-level script code
                // yield return (3 is var x3);
                Diagnostic(ErrorCode.ERR_YieldNotAllowedInScript, "yield").WithLocation(8, 1),
                // (11,1): error CS7020: Cannot use 'yield' in top-level script code
                // yield return H.Dummy((41 is var x4),
                Diagnostic(ErrorCode.ERR_YieldNotAllowedInScript, "yield").WithLocation(11, 1),
                // (16,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(16, 17),
                // (16,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(16, 21),
                // (16,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);
                Assert.Equal("System.Int32", ((IFieldSymbol)compilation.GetSemanticModel(tree).GetDeclaredSymbol(x1Decl)).Type.ToTestDisplayString());

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (2,1): error CS1624: The body of '<top-level-statements-entry-point>' cannot be an iterator block because 'void' is not an iterator interface type
                    // yield return (1 is var x1);
                    Diagnostic(ErrorCode.ERR_BadIteratorReturn, "yield return (1 is var x1);").WithArguments("<top-level-statements-entry-point>", "void").WithLocation(2, 1),
                    // (6,24): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // yield return (2 is var x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 24),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,33): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                      (42 is var x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 33),
                    // (19,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(19, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_ReturnStatement_01()
        {
            string source =
@"
return (1 is int x1);
H.Dummy(x1);

object x2;
return (2 is int x2);

return (3 is int x3);
object x3;

return H.Dummy((41 is int x4),
               (42 is int x4));

void Test()
{
    H.Dummy(x1, x2, x3, x4);
}

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,18): error CS0102: The type 'Script' already contains a definition for 'x2'
                // return (2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,27): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 27),
                // (3,1): warning CS0162: Unreachable code detected
                // H.Dummy(x1);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "H").WithLocation(3, 1),
                // (16,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(16, 17),
                // (16,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(16, 21),
                // (16,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (2,9): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                    // return (1 is int x1);
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "1 is int x1").WithArguments("bool", "int").WithLocation(2, 9),
                    // (3,1): warning CS0162: Unreachable code detected
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.WRN_UnreachableCode, "H").WithLocation(3, 1),
                    // (6,18): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // return (2 is int x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 18),
                    // (8,9): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                    // return (3 is int x3);
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "3 is int x3").WithArguments("bool", "int").WithLocation(8, 9),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,27): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                (42 is int x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 27),
                    // (14,6): warning CS8321: The local function 'Test' is declared but never used
                    // void Test()
                    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Test").WithArguments("Test").WithLocation(14, 6)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_ReturnStatement_02()
        {
            string source =
@"
return (1 is var x1);
H.Dummy(x1);

object x2;
return (2 is var x2);

return (3 is var x3);
object x3;

return H.Dummy((41 is var x4),
               (42 is var x4));

void Test()
{
    H.Dummy(x1, x2, x3, x4);
}

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,18): error CS0102: The type 'Script' already contains a definition for 'x2'
                // return (2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,27): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 27),
                // (3,1): warning CS0162: Unreachable code detected
                // H.Dummy(x1);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "H").WithLocation(3, 1),
                // (16,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(16, 17),
                // (16,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(16, 21),
                // (16,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (2,9): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                    // return (1 is var x1);
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "1 is var x1").WithArguments("bool", "int").WithLocation(2, 9),
                    // (3,1): warning CS0162: Unreachable code detected
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.WRN_UnreachableCode, "H").WithLocation(3, 1),
                    // (6,18): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // return (2 is var x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 18),
                    // (8,9): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                    // return (3 is var x3);
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "3 is var x3").WithArguments("bool", "int").WithLocation(8, 9),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,27): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                (42 is var x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 27),
                    // (14,6): warning CS8321: The local function 'Test' is declared but never used
                    // void Test()
                    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Test").WithArguments("Test").WithLocation(14, 6)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_ReturnStatement_03()
        {
            string source =
@"
System.Console.WriteLine(x1);
Test();
return H.Dummy((1 is var x1), x1);

void Test()
{
    System.Console.WriteLine(x1);
}

class H
{
    public static bool Dummy(object x, object y) 
    {
        System.Console.WriteLine(y);
        return true;
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
0
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_ThrowStatement_01()
        {
            string source =
@"
throw H.Dummy(1 is int x1);
H.Dummy(x1);

object x2;
throw H.Dummy(2 is int x2);

throw H.Dummy(3 is int x3);
object x3;

throw H.Dummy((41 is int x4),
              (42 is int x4));

void Test()
{
    H.Dummy(x1, x2, x3, x4);
}

Test();

class H
{
    public static System.Exception Dummy(params object[] x) {return null;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,24): error CS0102: The type 'Script' already contains a definition for 'x2'
                // throw H.Dummy(2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,26): error CS0102: The type 'Script' already contains a definition for 'x4'
                //               (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 26),
                // (3,1): warning CS0162: Unreachable code detected
                // H.Dummy(x1);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "H").WithLocation(3, 1),
                // (16,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(16, 17),
                // (16,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(16, 21),
                // (16,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,1): warning CS0162: Unreachable code detected
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.WRN_UnreachableCode, "H").WithLocation(3, 1),
                    // (6,24): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // throw H.Dummy(2 is int x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 24),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,26): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //               (42 is int x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 26)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_ThrowStatement_02()
        {
            string source =
@"
throw H.Dummy(1 is var x1);
H.Dummy(x1);

object x2;
throw H.Dummy(2 is var x2);

throw H.Dummy(3 is var x3);
object x3;

throw H.Dummy((41 is var x4),
              (42 is var x4));

void Test()
{
    H.Dummy(x1, x2, x3, x4);
}

Test();

class H
{
    public static System.Exception Dummy(params object[] x) {return null;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,24): error CS0102: The type 'Script' already contains a definition for 'x2'
                // throw H.Dummy(2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,26): error CS0102: The type 'Script' already contains a definition for 'x4'
                //               (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 26),
                // (3,1): warning CS0162: Unreachable code detected
                // H.Dummy(x1);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "H").WithLocation(3, 1),
                // (16,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(16, 17),
                // (16,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(16, 21),
                // (16,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);
                Assert.Equal("System.Int32", ((IFieldSymbol)compilation.GetSemanticModel(tree).GetDeclaredSymbol(x1Decl)).Type.ToTestDisplayString());

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,1): warning CS0162: Unreachable code detected
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.WRN_UnreachableCode, "H").WithLocation(3, 1),
                    // (6,24): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // throw H.Dummy(2 is var x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 24),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,26): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //               (42 is var x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 26)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_SwitchStatement_01()
        {
            string source =
@"
switch ((1 is int x1)) {default: break;}
H.Dummy(x1);

object x2;
switch ((2 is int x2)) {default: break;}

switch ((3 is int x3)) {default: break;}
object x3;

switch (H.Dummy((41 is int x4),
                (42 is int x4))) {default: break;}

switch ((51 is int x5)) 
{
default:
    H.Dummy(""52"" is string x5);
    H.Dummy(x5);
    break;
}
H.Dummy(x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,19): error CS0102: The type 'Script' already contains a definition for 'x2'
                // switch ((2 is int x2)) {default: break;}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 19),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,28): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                 (42 is int x4))) {default: break;}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 28),
                // (25,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(25, 17),
                // (25,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(25, 21),
                // (25,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(25, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationField(model, x5Decl[0], x5Ref[1], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (6,19): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // switch ((2 is int x2)) {default: break;}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 19),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,28): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                 (42 is int x4))) {default: break;}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 28),
                    // (17,28): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     H.Dummy("52" is string x5);
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(17, 28),
                    // (28,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(28, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref[1], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
            }
        }

        [Fact]
        public void GlobalCode_SwitchStatement_02()
        {
            string source =
@"
switch ((1 is var x1)) {default: break;}
H.Dummy(x1);

object x2;
switch ((2 is var x2)) {default: break;}

switch ((3 is var x3)) {default: break;}
object x3;

switch (H.Dummy((41 is var x4),
                (42 is var x4))) {default: break;}

switch ((51 is var x5)) 
{
default:
    H.Dummy(""52"" is var x5);
    H.Dummy(x5);
    break;
}
H.Dummy(x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,19): error CS0102: The type 'Script' already contains a definition for 'x2'
                // switch ((2 is var x2)) {default: break;}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 19),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,28): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                 (42 is var x4))) {default: break;}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 28),
                // (25,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(25, 17),
                // (25,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(25, 21),
                // (25,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(25, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationField(model, x5Decl[0], x5Ref[1], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (6,19): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // switch ((2 is var x2)) {default: break;}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 19),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,28): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                 (42 is var x4))) {default: break;}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 28),
                    // (17,25): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     H.Dummy("52" is var x5);
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(17, 25),
                    // (28,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(28, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref[1], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
            }
        }

        [Fact]
        public void GlobalCode_SwitchStatement_03()
        {
            string source =
@"
System.Console.WriteLine(x1);
switch ((1 is var x1)) 
{
default:
    H.Dummy(""11"" is var x1);
    System.Console.WriteLine(x1);
    break;
}
Test();

void Test()
{
    System.Console.WriteLine(x1);
}
class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
11
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl[0], x1Ref[0], x1Ref[2]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
        }

        [Fact]
        public void GlobalCode_WhileStatement_01()
        {
            string source =
@"
while ((1 is int x1)) {}
H.Dummy(x1);

object x2;
while ((2 is int x2)) {}

while ((3 is int x3)) {}
object x3;

while (H.Dummy((41 is int x4),
               (42 is int x4))) {}

while ((51 is int x5)) 
{
    H.Dummy(""52"" is string x5);
    H.Dummy(x5);
}
H.Dummy(x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (3,9): error CS0103: The name 'x1' does not exist in the current context
                // H.Dummy(x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(3, 9),
                // (12,27): error CS0128: A local variable or function named 'x4' is already defined in this scope
                //                (42 is int x4))) {}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 27),
                // (16,28): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     H.Dummy("52" is string x5);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(16, 28),
                // (19,9): error CS0103: The name 'x5' does not exist in the current context
                // H.Dummy(x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(19, 9),
                // (23,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                // (23,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(23, 25),
                // (23,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(23, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
                VerifyNotInScope(model, x5Ref[1]);
                VerifyNotInScope(model, x5Ref[2]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,9): error CS0103: The name 'x1' does not exist in the current context
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(3, 9),
                    // (6,18): error CS0136: A local or parameter named 'x2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // while ((2 is int x2)) {}
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x2").WithArguments("x2").WithLocation(6, 18),
                    // (8,18): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // while ((3 is int x3)) {}
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(8, 18),
                    // (12,27): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                (42 is int x4))) {}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 27),
                    // (16,28): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     H.Dummy("52" is string x5);
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(16, 28),
                    // (19,9): error CS0103: The name 'x5' does not exist in the current context
                    // H.Dummy(x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(19, 9),
                    // (23,13): error CS0103: The name 'x1' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                    // (23,25): error CS0103: The name 'x4' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(23, 25),
                    // (23,29): error CS0103: The name 'x5' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(23, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
                VerifyNotInScope(model, x5Ref[1]);
                VerifyNotInScope(model, x5Ref[2]);
            }
        }

        [Fact]
        public void GlobalCode_WhileStatement_02()
        {
            string source =
@"
while ((1 is var x1)) {}
H.Dummy(x1);

object x2;
while ((2 is var x2)) {}

while ((3 is var x3)) {}
object x3;

while (H.Dummy((41 is var x4),
               (42 is var x4))) {}

while ((51 is var x5)) 
{
    H.Dummy(""52"" is var x5);
    H.Dummy(x5);
}
H.Dummy(x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (3,9): error CS0103: The name 'x1' does not exist in the current context
                // H.Dummy(x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(3, 9),
                // (12,27): error CS0128: A local variable or function named 'x4' is already defined in this scope
                //                (42 is var x4))) {}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 27),
                // (16,25): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     H.Dummy("52" is var x5);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(16, 25),
                // (19,9): error CS0103: The name 'x5' does not exist in the current context
                // H.Dummy(x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(19, 9),
                // (23,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                // (23,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(23, 25),
                // (23,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(23, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
                VerifyNotInScope(model, x5Ref[1]);
                VerifyNotInScope(model, x5Ref[2]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,9): error CS0103: The name 'x1' does not exist in the current context
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(3, 9),
                    // (6,18): error CS0136: A local or parameter named 'x2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // while ((2 is var x2)) {}
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x2").WithArguments("x2").WithLocation(6, 18),
                    // (8,18): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // while ((3 is var x3)) {}
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(8, 18),
                    // (12,27): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                (42 is var x4))) {}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 27),
                    // (16,25): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     H.Dummy("52" is var x5);
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(16, 25),
                    // (19,9): error CS0103: The name 'x5' does not exist in the current context
                    // H.Dummy(x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(19, 9),
                    // (23,13): error CS0103: The name 'x1' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                    // (23,25): error CS0103: The name 'x4' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(23, 25),
                    // (23,29): error CS0103: The name 'x5' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(23, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
                VerifyNotInScope(model, x5Ref[1]);
                VerifyNotInScope(model, x5Ref[2]);
            }
        }

        [Fact]
        public void GlobalCode_WhileStatement_03()
        {
            string source =
@"
while ((1 is var x1)) 
{
    System.Console.WriteLine(x1);
    break;
}

class H
{
    public static bool Dummy(params object[] x) {return false;}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput: @"1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void GlobalCode_DoStatement_01()
        {
            string source =
@"
do {} while ((1 is int x1));
H.Dummy(x1);

object x2;
do {} while ((2 is int x2));

do {} while ((3 is int x3));
object x3;

do {} while (H.Dummy((41 is int x4),
                     (42 is int x4)));

do 
{
    H.Dummy(""52"" is string x5);
    H.Dummy(x5);
}
while ((51 is int x5));
H.Dummy(x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (3,9): error CS0103: The name 'x1' does not exist in the current context
                // H.Dummy(x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(3, 9),
                // (12,33): error CS0128: A local variable or function named 'x4' is already defined in this scope
                //                      (42 is int x4)));
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 33),
                // (16,28): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     H.Dummy("52" is string x5);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(16, 28),
                // (20,9): error CS0103: The name 'x5' does not exist in the current context
                // H.Dummy(x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(20, 9),
                // (24,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(24, 13),
                // (24,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(24, 25),
                // (24,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(24, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1]);
                VerifyNotInScope(model, x5Ref[1]);
                VerifyNotInScope(model, x5Ref[2]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,9): error CS0103: The name 'x1' does not exist in the current context
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(3, 9),
                    // (6,24): error CS0136: A local or parameter named 'x2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // do {} while ((2 is int x2));
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x2").WithArguments("x2").WithLocation(6, 24),
                    // (8,24): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // do {} while ((3 is int x3));
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(8, 24),
                    // (12,33): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                      (42 is int x4)));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 33),
                    // (16,28): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     H.Dummy("52" is string x5);
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(16, 28),
                    // (20,9): error CS0103: The name 'x5' does not exist in the current context
                    // H.Dummy(x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(20, 9),
                    // (24,13): error CS0103: The name 'x1' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(24, 13),
                    // (24,25): error CS0103: The name 'x4' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(24, 25),
                    // (24,29): error CS0103: The name 'x5' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(24, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1]);
                VerifyNotInScope(model, x5Ref[1]);
                VerifyNotInScope(model, x5Ref[2]);
            }
        }

        [Fact]
        public void GlobalCode_DoStatement_02()
        {
            string source =
@"
do {} while ((1 is var x1));
H.Dummy(x1);

object x2;
do {} while ((2 is var x2));

do {} while ((3 is var x3));
object x3;

do {} while (H.Dummy((41 is var x4),
                     (42 is var x4)));

do 
{
    H.Dummy(""52"" is var x5);
    H.Dummy(x5);
}
while ((51 is var x5));
H.Dummy(x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (3,9): error CS0103: The name 'x1' does not exist in the current context
                // H.Dummy(x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(3, 9),
                // (12,33): error CS0128: A local variable or function named 'x4' is already defined in this scope
                //                      (42 is var x4)));
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 33),
                // (16,25): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     H.Dummy("52" is var x5);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(16, 25),
                // (20,9): error CS0103: The name 'x5' does not exist in the current context
                // H.Dummy(x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(20, 9),
                // (24,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(24, 13),
                // (24,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(24, 25),
                // (24,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(24, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1]);
                VerifyNotInScope(model, x5Ref[1]);
                VerifyNotInScope(model, x5Ref[2]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,9): error CS0103: The name 'x1' does not exist in the current context
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(3, 9),
                    // (6,24): error CS0136: A local or parameter named 'x2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // do {} while ((2 is var x2));
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x2").WithArguments("x2").WithLocation(6, 24),
                    // (8,24): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // do {} while ((3 is var x3));
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(8, 24),
                    // (12,33): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                      (42 is var x4)));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 33),
                    // (16,25): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     H.Dummy("52" is var x5);
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(16, 25),
                    // (20,9): error CS0103: The name 'x5' does not exist in the current context
                    // H.Dummy(x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(20, 9),
                    // (24,13): error CS0103: The name 'x1' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(24, 13),
                    // (24,25): error CS0103: The name 'x4' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(24, 25),
                    // (24,29): error CS0103: The name 'x5' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(24, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1]);
                VerifyNotInScope(model, x5Ref[1]);
                VerifyNotInScope(model, x5Ref[2]);
            }
        }

        [Fact]
        public void GlobalCode_DoStatement_03()
        {
            string source =
@"
int f = 1;

do
{
}
while ((f++ is var x1) && Test(x1) < 3);

int Test(int x)
{
    System.Console.WriteLine(x);
    return x;
}

class H
{
    public static bool Dummy(params object[] x) {return false;}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"1
2
3").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void GlobalCode_LockStatement_01()
        {
            string source =
@"
lock (H.Dummy(1 is int x1)) {}
H.Dummy(x1);

object x2;
lock (H.Dummy(2 is int x2)) {}

lock (H.Dummy(3 is int x3)) {}
object x3;

lock (H.Dummy((41 is int x4),
              (42 is int x4))) {}

lock (H.Dummy(51 is int x5)) 
{
    H.Dummy(""52"" is string x5);
    H.Dummy(x5);
}
H.Dummy(x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static object Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,24): error CS0102: The type 'Script' already contains a definition for 'x2'
                // lock (H.Dummy(2 is int x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,26): error CS0102: The type 'Script' already contains a definition for 'x4'
                //               (42 is int x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 26),
                // (23,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(23, 17),
                // (23,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(23, 21),
                // (23,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(23, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationField(model, x5Decl[0], x5Ref[1], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (6,24): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // lock (H.Dummy(2 is int x2)) {}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 24),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,26): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //               (42 is int x4))) {}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 26),
                    // (16,28): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     H.Dummy("52" is string x5);
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(16, 28),
                    // (26,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(26, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref[1], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
            }
        }

        [Fact]
        public void GlobalCode_LockStatement_02()
        {
            string source =
@"
lock (H.Dummy(1 is var x1)) {}
H.Dummy(x1);

object x2;
lock (H.Dummy(2 is var x2)) {}

lock (H.Dummy(3 is var x3)) {}
object x3;

lock (H.Dummy((41 is var x4),
              (42 is var x4))) {}

lock (H.Dummy(51 is var x5)) 
{
    H.Dummy(""52"" is var x5);
    H.Dummy(x5);
}
H.Dummy(x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static object Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,24): error CS0102: The type 'Script' already contains a definition for 'x2'
                // lock (H.Dummy(2 is var x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,26): error CS0102: The type 'Script' already contains a definition for 'x4'
                //               (42 is var x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 26),
                // (23,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(23, 17),
                // (23,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(23, 21),
                // (23,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(23, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationField(model, x5Decl[0], x5Ref[1], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (6,24): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // lock (H.Dummy(2 is var x2)) {}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 24),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,26): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //               (42 is var x4))) {}
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 26),
                    // (16,25): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     H.Dummy("52" is var x5);
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(16, 25),
                    // (26,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(26, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Decl.Length);
                Assert.Equal(3, x5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref[1], x5Ref[2]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[1], x5Ref[0]);
            }
        }

        [Fact]
        public void GlobalCode_LockStatement_03()
        {
            string source =
@"
System.Console.WriteLine(x1);
lock (H.Dummy(1 is var x1)) 
{
    H.Dummy(""11"" is var x1);
    System.Console.WriteLine(x1);
}
Test();

void Test()
{
    System.Console.WriteLine(x1);
}
class H
{
    public static object Dummy(params object[] x) {return new object();}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
11
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl[0], x1Ref[0], x1Ref[2]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
        }

        [Fact]
        public void GlobalCode_LockStatement_04()
        {
            string source =
@"
System.Console.WriteLine(x1);
lock (H.Dummy(1 is var x1)) 
    H.Dummy((""11"" is var x1), x1);
Test();

void Test()
{
    System.Console.WriteLine(x1);
}

class H
{
    public static void Dummy(object x, object y)
    {
        System.Console.WriteLine(y);
    }
    public static object Dummy(object x)
    {
        return x;
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
11
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl[0], x1Ref[0], x1Ref[2]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
        }

        [Fact, WorkItem(13716, "https://github.com/dotnet/roslyn/issues/13716")]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void GlobalCode_DeconstructionDeclarationStatement_01()
        {
            string source =
@"
(bool a, int b) = ((1 is int x1), 1);
H.Dummy(x1);

object x2;
(bool c, int d) = ((2 is int x2), 2);

(bool e, int f) = ((3 is int x3), 3);
object x3;

(bool g, bool h) = ((41 is int x4),
                    (42 is int x4));

(bool x5, bool x6) = ((5 is int x5),
                      (6 is int x6));

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5, x6);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                                  options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                    // (6,30): error CS0102: The type 'Script' already contains a definition for 'x2'
                    // (bool c, int d) = ((2 is int x2), 2);
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 30),
                    // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                    // object x3;
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                    // (12,32): error CS0102: The type 'Script' already contains a definition for 'x4'
                    //                     (42 is int x4));
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 32),
                    // (14,33): error CS0102: The type 'Script' already contains a definition for 'x5'
                    // (bool x5, bool x6) = ((5 is int x5),
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("Script", "x5").WithLocation(14, 33),
                    // (15,33): error CS0102: The type 'Script' already contains a definition for 'x6'
                    //                       (6 is int x6));
                    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x6").WithArguments("Script", "x6").WithLocation(15, 33),
                    // (19,17): error CS0229: Ambiguity between 'x2' and 'x2'
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(19, 17),
                    // (19,21): error CS0229: Ambiguity between 'x3' and 'x3'
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(19, 21),
                    // (19,25): error CS0229: Ambiguity between 'x4' and 'x4'
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(19, 25),
                    // (19,29): error CS0229: Ambiguity between 'x5' and 'x5'
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_AmbigMember, "x5").WithArguments("x5", "x5").WithLocation(19, 29),
                    // (19,33): error CS0229: Ambiguity between 'x6' and 'x6'
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_AmbigMember, "x6").WithArguments("x6", "x6").WithLocation(19, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x5Decl, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x6Decl, x6Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                                  options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (6,30): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // (bool c, int d) = ((2 is int x2), 2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 30),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (12,32): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                     (42 is int x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 32),
                    // (14,33): error CS0128: A local variable or function named 'x5' is already defined in this scope
                    // (bool x5, bool x6) = ((5 is int x5),
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(14, 33),
                    // (15,33): error CS0128: A local variable or function named 'x6' is already defined in this scope
                    //                       (6 is int x6));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(15, 33),
                    // (22,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(22, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x5Decl);
                VerifyNotAPatternLocal(model, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x6Decl);
                VerifyNotAPatternLocal(model, x6Ref);
            }
        }

        [Fact]
        public void GlobalCode_LabeledStatement_01()
        {
            string source =
@"
a: H.Dummy(1 is int x1);
H.Dummy(x1);

object x2;
b: H.Dummy(2 is int x2);

c: H.Dummy(3 is int x3);
object x3;

d: H.Dummy((41 is int x4),
           (42 is int x4));

void Test()
{
    H.Dummy(x1, x2, x3, x4);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,21): error CS0102: The type 'Script' already contains a definition for 'x2'
                // b: H.Dummy(2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 21),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,23): error CS0102: The type 'Script' already contains a definition for 'x4'
                //            (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 23),
                // (2,1): warning CS0164: This label has not been referenced
                // a: H.Dummy(1 is int x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(2, 1),
                // (6,1): warning CS0164: This label has not been referenced
                // b: H.Dummy(2 is int x2);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "b").WithLocation(6, 1),
                // (8,1): warning CS0164: This label has not been referenced
                // c: H.Dummy(3 is int x3);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(8, 1),
                // (11,1): warning CS0164: This label has not been referenced
                // d: H.Dummy((41 is int x4),
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "d").WithLocation(11, 1),
                // (16,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(16, 17),
                // (16,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(16, 21),
                // (16,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (2,1): warning CS0164: This label has not been referenced
                    // a: H.Dummy(1 is int x1);
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(2, 1),
                    // (6,1): warning CS0164: This label has not been referenced
                    // b: H.Dummy(2 is int x2);
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "b").WithLocation(6, 1),
                    // (6,21): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // b: H.Dummy(2 is int x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 21),
                    // (8,1): warning CS0164: This label has not been referenced
                    // c: H.Dummy(3 is int x3);
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(8, 1),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (11,1): warning CS0164: This label has not been referenced
                    // d: H.Dummy((41 is int x4),
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "d").WithLocation(11, 1),
                    // (12,23): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //            (42 is int x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 23),
                    // (19,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(19, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_LabeledStatement_02()
        {
            string source =
@"
a: H.Dummy(1 is var x1);
H.Dummy(x1);

object x2;
b: H.Dummy(2 is var x2);

c: H.Dummy(3 is var x3);
object x3;

d: H.Dummy((41 is var x4),
           (42 is var x4));

void Test()
{
    H.Dummy(x1, x2, x3, x4);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,21): error CS0102: The type 'Script' already contains a definition for 'x2'
                // b: H.Dummy(2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 21),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,23): error CS0102: The type 'Script' already contains a definition for 'x4'
                //            (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 23),
                // (2,1): warning CS0164: This label has not been referenced
                // a: H.Dummy(1 is var x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(2, 1),
                // (6,1): warning CS0164: This label has not been referenced
                // b: H.Dummy(2 is var x2);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "b").WithLocation(6, 1),
                // (8,1): warning CS0164: This label has not been referenced
                // c: H.Dummy(3 is var x3);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(8, 1),
                // (11,1): warning CS0164: This label has not been referenced
                // d: H.Dummy((41 is var x4),
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "d").WithLocation(11, 1),
                // (16,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(16, 17),
                // (16,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(16, 21),
                // (16,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (2,1): warning CS0164: This label has not been referenced
                    // a: H.Dummy(1 is var x1);
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(2, 1),
                    // (6,1): warning CS0164: This label has not been referenced
                    // b: H.Dummy(2 is var x2);
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "b").WithLocation(6, 1),
                    // (6,21): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // b: H.Dummy(2 is var x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 21),
                    // (8,1): warning CS0164: This label has not been referenced
                    // c: H.Dummy(3 is var x3);
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(8, 1),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (11,1): warning CS0164: This label has not been referenced
                    // d: H.Dummy((41 is var x4),
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "d").WithLocation(11, 1),
                    // (12,23): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //            (42 is var x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 23),
                    // (19,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(19, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_LabeledStatement_03()
        {
            string source =
@"
System.Console.WriteLine(x1);
a:b:c:H.Dummy(1 is var x1);
Test();

void Test()
{
    System.Console.WriteLine(x1);
}
class H
{
    public static void Dummy(params object[] x) {}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
1").VerifyDiagnostics(
                // (3,1): warning CS0164: This label has not been referenced
                // a:b:c:(1 is var x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(3, 1),
                // (3,3): warning CS0164: This label has not been referenced
                // a:b:c:(1 is var x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "b").WithLocation(3, 3),
                // (3,5): warning CS0164: This label has not been referenced
                // a:b:c:(1 is var x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(3, 5)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_LabeledStatement_04()
        {
            string source =
@"
a: 
bool b = (1 is int x1);
H.Dummy(x1);
object x2;
c:
bool d = (2 is int x2);
e:
bool f = (3 is int x3);
object x3;
g:
bool h = H.Dummy((41 is int x4),
                 (42 is int x4));
i:
bool x5 = (5 is int x5);
H.Dummy(x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (7,20): error CS0102: The type 'Script' already contains a definition for 'x2'
                // bool d = (2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(7, 20),
                // (10,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(10, 8),
                // (13,29): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                  (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(13, 29),
                // (2,1): warning CS0164: This label has not been referenced
                // a: 
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(2, 1),
                // (6,1): warning CS0164: This label has not been referenced
                // c:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(6, 1),
                // (8,1): warning CS0164: This label has not been referenced
                // e:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "e").WithLocation(8, 1),
                // (11,1): warning CS0164: This label has not been referenced
                // g:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "g").WithLocation(11, 1),
                // (14,1): warning CS0164: This label has not been referenced
                // i:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "i").WithLocation(14, 1),
                // (20,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(20, 17),
                // (20,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(20, 21),
                // (20,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(20, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Ref.Length);
                VerifyModelForDeclarationField(model, x5Decl, x5Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (2,1): warning CS0164: This label has not been referenced
                    // a: 
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(2, 1),
                    // (6,1): warning CS0164: This label has not been referenced
                    // c:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(6, 1),
                    // (7,20): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // bool d = (2 is int x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(7, 20),
                    // (8,1): warning CS0164: This label has not been referenced
                    // e:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "e").WithLocation(8, 1),
                    // (10,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(10, 8),
                    // (10,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(10, 8),
                    // (11,1): warning CS0164: This label has not been referenced
                    // g:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "g").WithLocation(11, 1),
                    // (13,29): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                  (42 is int x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 29),
                    // (14,1): warning CS0164: This label has not been referenced
                    // i:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "i").WithLocation(14, 1),
                    // (15,21): error CS0128: A local variable or function named 'x5' is already defined in this scope
                    // bool x5 = (5 is int x5);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(15, 21),
                    // (23,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(23, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Ref.Length);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x5Decl);
                VerifyNotAPatternLocal(model, x5Ref[0]);
                VerifyNotAPatternLocal(model, x5Ref[1]);
            }
        }

        [Fact]
        public void GlobalCode_LabeledStatement_05()
        {
            string source =
@"
a: 
bool b = (1 is var x1);
H.Dummy(x1);
object x2;
c:
bool d = (2 is var x2);
e:
bool f = (3 is var x3);
object x3;
g:
bool h = H.Dummy((41 is var x4),
                 (42 is var x4));
i:
bool x5 = (5 is var x5);
H.Dummy(x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (7,20): error CS0102: The type 'Script' already contains a definition for 'x2'
                // bool d = (2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(7, 20),
                // (10,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(10, 8),
                // (13,29): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                  (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(13, 29),
                // (2,1): warning CS0164: This label has not been referenced
                // a: 
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(2, 1),
                // (6,1): warning CS0164: This label has not been referenced
                // c:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(6, 1),
                // (8,1): warning CS0164: This label has not been referenced
                // e:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "e").WithLocation(8, 1),
                // (11,1): warning CS0164: This label has not been referenced
                // g:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "g").WithLocation(11, 1),
                // (14,1): warning CS0164: This label has not been referenced
                // i:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "i").WithLocation(14, 1),
                // (20,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(20, 17),
                // (20,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(20, 21),
                // (20,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(20, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Ref.Length);
                VerifyModelForDeclarationField(model, x5Decl, x5Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (2,1): warning CS0164: This label has not been referenced
                    // a: 
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(2, 1),
                    // (6,1): warning CS0164: This label has not been referenced
                    // c:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(6, 1),
                    // (7,20): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // bool d = (2 is var x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(7, 20),
                    // (8,1): warning CS0164: This label has not been referenced
                    // e:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "e").WithLocation(8, 1),
                    // (10,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(10, 8),
                    // (10,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(10, 8),
                    // (11,1): warning CS0164: This label has not been referenced
                    // g:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "g").WithLocation(11, 1),
                    // (13,29): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                  (42 is var x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 29),
                    // (14,1): warning CS0164: This label has not been referenced
                    // i:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "i").WithLocation(14, 1),
                    // (15,21): error CS0128: A local variable or function named 'x5' is already defined in this scope
                    // bool x5 = (5 is var x5);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(15, 21),
                    // (23,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(23, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Ref.Length);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x5Decl);
                VerifyNotAPatternLocal(model, x5Ref[0]);
                VerifyNotAPatternLocal(model, x5Ref[1]);
            }
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void GlobalCode_LabeledStatement_06()
        {
            string source =
@"
System.Console.WriteLine(x1);
a:b:c:
var d = (1 is var x1);
Test();

void Test()
{
    System.Console.WriteLine(x1);
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
1").VerifyDiagnostics(
                // (3,1): warning CS0164: This label has not been referenced
                // a:b:c:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(3, 1),
                // (3,3): warning CS0164: This label has not been referenced
                // a:b:c:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "b").WithLocation(3, 3),
                // (3,5): warning CS0164: This label has not been referenced
                // a:b:c:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(3, 5)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void GlobalCode_LabeledStatement_07()
        {
            string source =
@"l1:
(bool a, int b) = ((1 is int x1), 1);
H.Dummy(x1);
object x2;
l2:
(bool c, int d) = ((2 is int x2), 2);
l3:
(bool e, int f) = ((3 is int x3), 3);
object x3;
l4:
(bool g, bool h) = ((41 is int x4),
                    (42 is int x4));
l5:
(bool x5, bool x6) = ((5 is int x5),
                      (6 is int x6));

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5, x6);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                                  options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,30): error CS0102: The type 'Script' already contains a definition for 'x2'
                // (bool c, int d) = ((2 is int x2), 2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 30),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,32): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                     (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 32),
                // (14,33): error CS0102: The type 'Script' already contains a definition for 'x5'
                // (bool x5, bool x6) = ((5 is int x5),
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("Script", "x5").WithLocation(14, 33),
                // (15,33): error CS0102: The type 'Script' already contains a definition for 'x6'
                //                       (6 is int x6));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x6").WithArguments("Script", "x6").WithLocation(15, 33),
                // (1,1): warning CS0164: This label has not been referenced
                // l1:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l1").WithLocation(1, 1),
                // (5,1): warning CS0164: This label has not been referenced
                // l2:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l2").WithLocation(5, 1),
                // (7,1): warning CS0164: This label has not been referenced
                // l3:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l3").WithLocation(7, 1),
                // (10,1): warning CS0164: This label has not been referenced
                // l4:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l4").WithLocation(10, 1),
                // (13,1): warning CS0164: This label has not been referenced
                // l5:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l5").WithLocation(13, 1),
                // (19,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(19, 17),
                // (19,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(19, 21),
                // (19,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(19, 25),
                // (19,29): error CS0229: Ambiguity between 'x5' and 'x5'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x5").WithArguments("x5", "x5").WithLocation(19, 29),
                // (19,33): error CS0229: Ambiguity between 'x6' and 'x6'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x6").WithArguments("x6", "x6").WithLocation(19, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(1, x5Ref.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x5Decl, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(1, x6Ref.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x6Decl, x6Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                                  options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (1,1): warning CS0164: This label has not been referenced
                    // l1:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l1").WithLocation(1, 1),
                    // (5,1): warning CS0164: This label has not been referenced
                    // l2:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l2").WithLocation(5, 1),
                    // (6,30): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // (bool c, int d) = ((2 is int x2), 2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 30),
                    // (7,1): warning CS0164: This label has not been referenced
                    // l3:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l3").WithLocation(7, 1),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (10,1): warning CS0164: This label has not been referenced
                    // l4:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l4").WithLocation(10, 1),
                    // (12,32): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                     (42 is int x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 32),
                    // (13,1): warning CS0164: This label has not been referenced
                    // l5:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l5").WithLocation(13, 1),
                    // (14,33): error CS0128: A local variable or function named 'x5' is already defined in this scope
                    // (bool x5, bool x6) = ((5 is int x5),
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(14, 33),
                    // (15,33): error CS0128: A local variable or function named 'x6' is already defined in this scope
                    //                       (6 is int x6));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(15, 33),
                    // (22,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(22, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(1, x5Ref.Length);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x5Decl);
                VerifyNotAPatternLocal(model, x5Ref[0]);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(1, x6Ref.Length);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x6Decl);
                VerifyNotAPatternLocal(model, x6Ref[0]);
            }
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void GlobalCode_LabeledStatement_08()
        {
            string source =
@"l1:
(bool a, int b) = ((1 is var x1), 1);
H.Dummy(x1);
object x2;
l2:
(bool c, int d) = ((2 is var x2), 2);
l3:
(bool e, int f) = ((3 is var x3), 3);
object x3;
l4:
(bool g, bool h) = ((41 is var x4),
                    (42 is var x4));
l5:
(bool x5, bool x6) = ((5 is var x5),
                      (6 is var x6));

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5, x6);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                                  options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,30): error CS0102: The type 'Script' already contains a definition for 'x2'
                // (bool c, int d) = ((2 is var x2), 2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 30),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,32): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                     (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 32),
                // (14,33): error CS0102: The type 'Script' already contains a definition for 'x5'
                // (bool x5, bool x6) = ((5 is var x5),
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("Script", "x5").WithLocation(14, 33),
                // (15,33): error CS0102: The type 'Script' already contains a definition for 'x6'
                //                       (6 is var x6));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x6").WithArguments("Script", "x6").WithLocation(15, 33),
                // (1,1): warning CS0164: This label has not been referenced
                // l1:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l1").WithLocation(1, 1),
                // (5,1): warning CS0164: This label has not been referenced
                // l2:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l2").WithLocation(5, 1),
                // (7,1): warning CS0164: This label has not been referenced
                // l3:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l3").WithLocation(7, 1),
                // (10,1): warning CS0164: This label has not been referenced
                // l4:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l4").WithLocation(10, 1),
                // (13,1): warning CS0164: This label has not been referenced
                // l5:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l5").WithLocation(13, 1),
                // (19,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(19, 17),
                // (19,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(19, 21),
                // (19,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(19, 25),
                // (19,29): error CS0229: Ambiguity between 'x5' and 'x5'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x5").WithArguments("x5", "x5").WithLocation(19, 29),
                // (19,33): error CS0229: Ambiguity between 'x6' and 'x6'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x6").WithArguments("x6", "x6").WithLocation(19, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(1, x5Ref.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x5Decl, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(1, x6Ref.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x6Decl, x6Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                                  options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (1,1): warning CS0164: This label has not been referenced
                    // l1:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l1").WithLocation(1, 1),
                    // (5,1): warning CS0164: This label has not been referenced
                    // l2:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l2").WithLocation(5, 1),
                    // (6,30): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // (bool c, int d) = ((2 is var x2), 2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(6, 30),
                    // (7,1): warning CS0164: This label has not been referenced
                    // l3:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l3").WithLocation(7, 1),
                    // (9,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (9,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(9, 8),
                    // (10,1): warning CS0164: This label has not been referenced
                    // l4:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l4").WithLocation(10, 1),
                    // (12,32): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                     (42 is var x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(12, 32),
                    // (13,1): warning CS0164: This label has not been referenced
                    // l5:
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "l5").WithLocation(13, 1),
                    // (14,33): error CS0128: A local variable or function named 'x5' is already defined in this scope
                    // (bool x5, bool x6) = ((5 is var x5),
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(14, 33),
                    // (15,33): error CS0128: A local variable or function named 'x6' is already defined in this scope
                    //                       (6 is var x6));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(15, 33),
                    // (22,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(22, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(1, x5Ref.Length);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x5Decl);
                VerifyNotAPatternLocal(model, x5Ref[0]);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(1, x6Ref.Length);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x6Decl);
                VerifyNotAPatternLocal(model, x6Ref[0]);
            }
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void GlobalCode_LabeledStatement_09()
        {
            string source =
@"
System.Console.WriteLine(x1);
a:b:c:
var (d, e) = ((1 is var x1), 1);
Test();

void Test()
{
    System.Console.WriteLine(x1);
}
";

            var compilation = CreateCompilationWithMscorlib461(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                              options: TestOptions.DebugExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
1").VerifyDiagnostics(
                // (3,1): warning CS0164: This label has not been referenced
                // a:b:c:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(3, 1),
                // (3,3): warning CS0164: This label has not been referenced
                // a:b:c:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "b").WithLocation(3, 3),
                // (3,5): warning CS0164: This label has not been referenced
                // a:b:c:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(3, 5)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_FieldDeclaration_01()
        {
            string source =
@"
 
bool b = (1 is int x1);
H.Dummy(x1);

object x2;
bool d = (2 is int x2);

bool f = (3 is int x3);
object x3;

bool h = H.Dummy((41 is int x4),
                 (42 is int x4));

bool x5 = 
          (5 is int x5);

bool i = (5 is int x6),
         x6;

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5, x6);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (7,20): error CS0102: The type 'Script' already contains a definition for 'x2'
                // bool d = (2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(7, 20),
                // (10,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(10, 8),
                // (13,29): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                  (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(13, 29),
                // (16,21): error CS0102: The type 'Script' already contains a definition for 'x5'
                //           (5 is int x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("Script", "x5").WithLocation(16, 21),
                // (19,10): error CS0102: The type 'Script' already contains a definition for 'x6'
                //          x6;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x6").WithArguments("Script", "x6").WithLocation(19, 10),
                // (23,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(23, 17),
                // (23,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(23, 21),
                // (23,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(23, 25),
                // (23,29): error CS0229: Ambiguity between 'x5' and 'x5'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x5").WithArguments("x5", "x5").WithLocation(23, 29),
                // (23,33): error CS0229: Ambiguity between 'x6' and 'x6'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x6").WithArguments("x6", "x6").WithLocation(23, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x5Decl, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x6Decl, x6Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (7,20): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // bool d = (2 is int x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(7, 20),
                    // (10,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(10, 8),
                    // (10,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(10, 8),
                    // (13,29): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                  (42 is int x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 29),
                    // (16,21): error CS0128: A local variable or function named 'x5' is already defined in this scope
                    //           (5 is int x5);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(16, 21),
                    // (19,10): error CS0128: A local variable or function named 'x6' is already defined in this scope
                    //          x6;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(19, 10),
                    // (19,10): warning CS0168: The variable 'x6' is declared but never used
                    //          x6;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x6").WithArguments("x6").WithLocation(19, 10),
                    // (26,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(26, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x5Decl);
                VerifyNotAPatternLocal(model, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl, x6Ref);
            }
        }

        [Fact]
        public void GlobalCode_FieldDeclaration_02()
        {
            string source =
@"
 
bool b = (1 is var x1);
H.Dummy(x1);

object x2;
bool d = (2 is var x2);

bool f = (3 is var x3);
object x3;

bool h = H.Dummy((41 is var x4),
                 (42 is var x4));

bool x5 = 
          (5 is var x5);

bool i = (5 is var x6),
         x6;

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5, x6);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (7,20): error CS0102: The type 'Script' already contains a definition for 'x2'
                // bool d = (2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(7, 20),
                // (10,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(10, 8),
                // (13,29): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                  (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(13, 29),
                // (16,21): error CS0102: The type 'Script' already contains a definition for 'x5'
                //           (5 is var x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("Script", "x5").WithLocation(16, 21),
                // (19,10): error CS0102: The type 'Script' already contains a definition for 'x6'
                //          x6;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x6").WithArguments("Script", "x6").WithLocation(19, 10),
                // (23,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(23, 17),
                // (23,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(23, 21),
                // (23,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(23, 25),
                // (23,29): error CS0229: Ambiguity between 'x5' and 'x5'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x5").WithArguments("x5", "x5").WithLocation(23, 29),
                // (23,33): error CS0229: Ambiguity between 'x6' and 'x6'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x6").WithArguments("x6", "x6").WithLocation(23, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x5Decl, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x6Decl, x6Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (7,20): error CS0128: A local variable or function named 'x2' is already defined in this scope
                    // bool d = (2 is var x2);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(7, 20),
                    // (10,8): error CS0128: A local variable or function named 'x3' is already defined in this scope
                    // object x3;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(10, 8),
                    // (10,8): warning CS0168: The variable 'x3' is declared but never used
                    // object x3;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x3").WithArguments("x3").WithLocation(10, 8),
                    // (13,29): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                  (42 is var x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 29),
                    // (16,21): error CS0128: A local variable or function named 'x5' is already defined in this scope
                    //           (5 is var x5);
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(16, 21),
                    // (19,10): error CS0128: A local variable or function named 'x6' is already defined in this scope
                    //          x6;
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(19, 10),
                    // (19,10): warning CS0168: The variable 'x6' is declared but never used
                    //          x6;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x6").WithArguments("x6").WithLocation(19, 10),
                    // (26,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(26, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x5Decl);
                VerifyNotAPatternLocal(model, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl, x6Ref);
            }
        }

        [Fact]
        public void GlobalCode_FieldDeclaration_03()
        {
            string source =
@"
System.Console.WriteLine(x1);
var d = (1 is var x1);
Test();

void Test()
{
    System.Console.WriteLine(x1);
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_FieldDeclaration_04()
        {
            string source =
@"
static var a = InitA();
System.Console.WriteLine(x1);
static var b = (1 is var x1);
Test();
static var c = InitB();

void Test()
{
    System.Console.WriteLine(x1);
}

static object InitA()
{
    System.Console.WriteLine(""InitA {0}"", x1);
    return null;
}

static object InitB()
{
    System.Console.WriteLine(""InitB {0}"", x1);
    return null;
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"InitA 0
InitB 1
1
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(4, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_FieldDeclaration_05()
        {
            string source =
@"
 
bool b = (1 is var x1);
static var d = x1;

static void Test()
{
    H.Dummy(x1);
}

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            compilation.VerifyDiagnostics(
                // (4,16): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                // static var d = x1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(4, 16),
                // (8,13): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                //     H.Dummy(x1);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(8, 13)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_FieldDeclaration_06()
        {
            string source =
@"
 
bool b = (1 is int x1);
static var d = x1;

static void Test()
{
    H.Dummy(x1);
}

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            compilation.VerifyDiagnostics(
                // (4,16): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                // static var d = x1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(4, 16),
                // (8,13): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                //     H.Dummy(x1);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(8, 13)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_FieldDeclaration_07()
        {
            string source =
@"
Test();
bool a = (1 is var x1), b = Test(), c = (2 is var x2);
Test();

bool Test()
{
    System.Console.WriteLine(""{0} {1}"", x1, x2);
    return false;
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0 0
1 0
1 2").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForDeclarationField(model, x2Decl, x2Ref);
        }

        [Fact]
        public void GlobalCode_PropertyDeclaration_01()
        {
            string source =
@"
 
bool b { get; } = (1 is int x1);
H.Dummy(x1);

object x2;
bool d { get; } = (2 is int x2);

bool f { get; } = (3 is int x3);
object x3;

bool h { get; } = H.Dummy((41 is int x4),
                          (42 is int x4));

bool x5 { get; } = 
          (5 is int x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (7,29): error CS0102: The type 'Script' already contains a definition for 'x2'
                // bool d { get; } = (2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(7, 29),
                // (10,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(10, 8),
                // (13,38): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                           (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(13, 38),
                // (16,21): error CS0102: The type 'Script' already contains a definition for 'x5'
                //           (5 is int x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("Script", "x5").WithLocation(16, 21),
                // (20,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(20, 17),
                // (20,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(20, 21),
                // (20,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(20, 25),
                // (20,29): error CS0229: Ambiguity between 'x5' and 'x5'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x5").WithArguments("x5", "x5").WithLocation(20, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x5Decl, x5Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,6): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // bool b { get; } = (1 is int x1);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "b").WithLocation(3, 6),
                    // (4,9): error CS0103: The name 'x1' does not exist in the current context
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(4, 9),
                    // (7,6): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // bool d { get; } = (2 is int x2);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "d").WithLocation(7, 6),
                    // (9,6): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // bool f { get; } = (3 is int x3);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "f").WithLocation(9, 6),
                    // (12,6): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // bool h { get; } = H.Dummy((41 is int x4),
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "h").WithLocation(12, 6),
                    // (13,38): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                           (42 is int x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 38),
                    // (15,6): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // bool x5 { get; } = 
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "x5").WithLocation(15, 6),
                    // (20,13): error CS0103: The name 'x1' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(20, 13),
                    // (20,25): error CS0103: The name 'x4' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(20, 25),
                    // (20,29): error CS0103: The name 'x5' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(20, 29),
                    // (23,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(23, 1),
                    // (23,1): error CS0165: Use of unassigned local variable 'x3'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x3").WithLocation(23, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl);
                VerifyNotInScope(model, x5Ref);
            }
        }

        [Fact]
        public void GlobalCode_PropertyDeclaration_02()
        {
            string source =
@"
 
bool b { get; } = (1 is var x1);
H.Dummy(x1);

object x2;
bool d { get; } = (2 is var x2);

bool f { get; } = (3 is var x3);
object x3;

bool h { get; } = H.Dummy((41 is var x4),
                          (42 is var x4));

bool x5 { get; } = 
          (5 is var x5);

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (7,29): error CS0102: The type 'Script' already contains a definition for 'x2'
                // bool d { get; } = (2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(7, 29),
                // (10,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(10, 8),
                // (13,38): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                           (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(13, 38),
                // (16,21): error CS0102: The type 'Script' already contains a definition for 'x5'
                //           (5 is var x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("Script", "x5").WithLocation(16, 21),
                // (20,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(20, 17),
                // (20,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(20, 21),
                // (20,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(20, 25),
                // (20,29): error CS0229: Ambiguity between 'x5' and 'x5'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x5").WithArguments("x5", "x5").WithLocation(20, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x5Decl, x5Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,6): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // bool b { get; } = (1 is var x1);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "b").WithLocation(3, 6),
                    // (4,9): error CS0103: The name 'x1' does not exist in the current context
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(4, 9),
                    // (7,6): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // bool d { get; } = (2 is var x2);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "d").WithLocation(7, 6),
                    // (9,6): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // bool f { get; } = (3 is var x3);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "f").WithLocation(9, 6),
                    // (12,6): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // bool h { get; } = H.Dummy((41 is var x4),
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "h").WithLocation(12, 6),
                    // (13,38): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                           (42 is var x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 38),
                    // (15,6): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // bool x5 { get; } = 
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "x5").WithLocation(15, 6),
                    // (20,13): error CS0103: The name 'x1' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(20, 13),
                    // (20,25): error CS0103: The name 'x4' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(20, 25),
                    // (20,29): error CS0103: The name 'x5' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(20, 29),
                    // (23,1): error CS0165: Use of unassigned local variable 'x2'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x2").WithLocation(23, 1),
                    // (23,1): error CS0165: Use of unassigned local variable 'x3'
                    // Test();
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "Test()").WithArguments("x3").WithLocation(23, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl);
                VerifyNotInScope(model, x5Ref);
            }
        }

        [Fact]
        public void GlobalCode_PropertyDeclaration_03()
        {
            string source =
@"
System.Console.WriteLine(x1);
bool d { get; set; } = (1 is var x1);
Test();

void Test()
{
    System.Console.WriteLine(x1);
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_PropertyDeclaration_04()
        {
            string source =
@"
static var a = InitA();
System.Console.WriteLine(x1);
static bool b { get; } = (1 is var x1);
Test();
static var c = InitB();

void Test()
{
    System.Console.WriteLine(x1);
}

static object InitA()
{
    System.Console.WriteLine(""InitA {0}"", x1);
    return null;
}

static object InitB()
{
    System.Console.WriteLine(""InitB {0}"", x1);
    return null;
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"InitA 0
InitB 1
1
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(4, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_PropertyDeclaration_05()
        {
            string source =
@"
 
bool b { get; } = (1 is var x1);
static var d = x1;

static void Test()
{
    H.Dummy(x1);
}

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            compilation.VerifyDiagnostics(
                // (4,16): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                // static var d = x1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(4, 16),
                // (8,13): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                //     H.Dummy(x1);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(8, 13)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_PropertyDeclaration_06()
        {
            string source =
@"
 
bool b { get; } = (1 is int x1);
static var d = x1;

static void Test()
{
    H.Dummy(x1);
}

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            compilation.VerifyDiagnostics(
                // (4,16): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                // static var d = x1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(4, 16),
                // (8,13): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                //     H.Dummy(x1);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(8, 13)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_EventDeclaration_01()
        {
            string source =
@"
 
event System.Action b = H.Dummy(1 is int x1);
H.Dummy(x1);

object x2;
event System.Action d = H.Dummy(2 is int x2);

event System.Action f = H.Dummy(3 is int x3);
object x3;

event System.Action h = H.Dummy((41 is int x4),
                        (42 is int x4));

event System.Action x5 = 
          H.Dummy(5 is int x5);

event System.Action i = H.Dummy(5 is int x6),
         x6;

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5, x6);
}

class H
{
    public static System.Action Dummy(params object[] x) {return null;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (7,42): error CS0102: The type 'Script' already contains a definition for 'x2'
                // event System.Action d = H.Dummy(2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(7, 42),
                // (10,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(10, 8),
                // (13,36): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                         (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(13, 36),
                // (16,28): error CS0102: The type 'Script' already contains a definition for 'x5'
                //           H.Dummy(5 is int x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("Script", "x5").WithLocation(16, 28),
                // (19,10): error CS0102: The type 'Script' already contains a definition for 'x6'
                //          x6;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x6").WithArguments("Script", "x6").WithLocation(19, 10),
                // (23,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(23, 17),
                // (23,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(23, 21),
                // (23,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(23, 25),
                // (23,29): error CS0229: Ambiguity between 'x5' and 'x5'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x5").WithArguments("x5", "x5").WithLocation(23, 29),
                // (23,33): error CS0229: Ambiguity between 'x6' and 'x6'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x6").WithArguments("x6", "x6").WithLocation(23, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x5Decl, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x6Decl, x6Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action b = H.Dummy(1 is int x1);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "b").WithLocation(3, 21),
                    // (4,9): error CS0103: The name 'x1' does not exist in the current context
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(4, 9),
                    // (7,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action d = H.Dummy(2 is int x2);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "d").WithLocation(7, 21),
                    // (9,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action f = H.Dummy(3 is int x3);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "f").WithLocation(9, 21),
                    // (12,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action h = H.Dummy((41 is int x4),
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "h").WithLocation(12, 21),
                    // (13,36): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                         (42 is int x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 36),
                    // (15,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action x5 = 
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "x5").WithLocation(15, 21),
                    // (18,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action i = H.Dummy(5 is int x6),
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "i").WithLocation(18, 21),
                    // (21,6): warning CS8321: The local function 'Test' is declared but never used
                    // void Test()
                    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Test").WithArguments("Test").WithLocation(21, 6),
                    // (23,13): error CS0103: The name 'x1' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                    // (23,25): error CS0103: The name 'x4' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(23, 25),
                    // (23,29): error CS0103: The name 'x5' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(23, 29),
                    // (23,33): error CS0103: The name 'x6' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x6").WithArguments("x6").WithLocation(23, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl);
                VerifyNotInScope(model, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl);
                VerifyNotInScope(model, x6Ref);
            }
        }

        [Fact]
        public void GlobalCode_EventDeclaration_02()
        {
            string source =
@"
 
event System.Action b = H.Dummy(1 is var x1);
H.Dummy(x1);

object x2;
event System.Action d = H.Dummy(2 is var x2);

event System.Action f = H.Dummy(3 is var x3);
object x3;

event System.Action h = H.Dummy((41 is var x4),
                        (42 is var x4));

event System.Action x5 = 
          H.Dummy(5 is var x5);

event System.Action i = H.Dummy(5 is var x6),
         x6;

void Test()
{
    H.Dummy(x1, x2, x3, x4, x5, x6);
}

class H
{
    public static System.Action Dummy(params object[] x) {return null;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (7,42): error CS0102: The type 'Script' already contains a definition for 'x2'
                // event System.Action d = H.Dummy(2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(7, 42),
                // (10,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(10, 8),
                // (13,36): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                         (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(13, 36),
                // (16,28): error CS0102: The type 'Script' already contains a definition for 'x5'
                //           H.Dummy(5 is var x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("Script", "x5").WithLocation(16, 28),
                // (19,10): error CS0102: The type 'Script' already contains a definition for 'x6'
                //          x6;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x6").WithArguments("Script", "x6").WithLocation(19, 10),
                // (23,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(23, 17),
                // (23,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(23, 21),
                // (23,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(23, 25),
                // (23,29): error CS0229: Ambiguity between 'x5' and 'x5'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x5").WithArguments("x5", "x5").WithLocation(23, 29),
                // (23,33): error CS0229: Ambiguity between 'x6' and 'x6'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x6").WithArguments("x6", "x6").WithLocation(23, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[0], x4Ref);
                VerifyModelForDeclarationFieldDuplicate(model, x4Decl[1], x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x5Decl, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationFieldDuplicate(model, x6Decl, x6Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action b = H.Dummy(1 is var x1);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "b").WithLocation(3, 21),
                    // (4,9): error CS0103: The name 'x1' does not exist in the current context
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(4, 9),
                    // (7,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action d = H.Dummy(2 is var x2);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "d").WithLocation(7, 21),
                    // (9,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action f = H.Dummy(3 is var x3);
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "f").WithLocation(9, 21),
                    // (12,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action h = H.Dummy((41 is var x4),
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "h").WithLocation(12, 21),
                    // (13,36): error CS0128: A local variable or function named 'x4' is already defined in this scope
                    //                         (42 is var x4));
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 36),
                    // (15,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action x5 = 
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "x5").WithLocation(15, 21),
                    // (18,21): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // event System.Action i = H.Dummy(5 is var x6),
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "i").WithLocation(18, 21),
                    // (21,6): warning CS8321: The local function 'Test' is declared but never used
                    // void Test()
                    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Test").WithArguments("Test").WithLocation(21, 6),
                    // (23,13): error CS0103: The name 'x1' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                    // (23,25): error CS0103: The name 'x4' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(23, 25),
                    // (23,29): error CS0103: The name 'x5' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(23, 29),
                    // (23,33): error CS0103: The name 'x6' does not exist in the current context
                    //     H.Dummy(x1, x2, x3, x4, x5, x6);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x6").WithArguments("x6").WithLocation(23, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl);
                VerifyNotAPatternLocal(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotAPatternLocal(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl[0]);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyNotInScope(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl);
                VerifyNotInScope(model, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl);
                VerifyNotInScope(model, x6Ref);
            }
        }

        [Fact]
        public void GlobalCode_EventDeclaration_03()
        {
            string source =
@"
System.Console.WriteLine(x1);
event System.Action d = H.Dummy(1 is var x1);
Test();

void Test()
{
    System.Console.WriteLine(x1);
}

class H
{
    public static System.Action Dummy(params object[] x) {return null;}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_EventDeclaration_04()
        {
            string source =
@"
static var a = InitA();
System.Console.WriteLine(x1);
static event System.Action b = H.Dummy(1 is var x1);
Test();
static var c = InitB();

void Test()
{
    System.Console.WriteLine(x1);
}

static object InitA()
{
    System.Console.WriteLine(""InitA {0}"", x1);
    return null;
}

static object InitB()
{
    System.Console.WriteLine(""InitB {0}"", x1);
    return null;
}

class H
{
   public static System.Action Dummy(params object[] x) {return null;}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"InitA 0
InitB 1
1
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(4, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_EventDeclaration_05()
        {
            string source =
@"
 
event System.Action b = H.Dummy(1 is var x1);
static var d = x1;

static void Test()
{
    H.Dummy(x1);
}

class H
{
    public static System.Action Dummy(params object[] x) {return null;}
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            compilation.VerifyDiagnostics(
                // (4,16): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                // static var d = x1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(4, 16),
                // (8,13): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                //     H.Dummy(x1);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(8, 13)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_EventDeclaration_06()
        {
            string source =
@"
 
event System.Action b = H.Dummy(1 is int x1);
static var d = x1;

static void Test()
{
    H.Dummy(x1);
}

class H
{
    public static System.Action Dummy(params object[] x) {return null;}
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            compilation.VerifyDiagnostics(
                // (4,16): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                // static var d = x1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(4, 16),
                // (8,13): error CS0120: An object reference is required for the non-static field, method, or property 'x1'
                //     H.Dummy(x1);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x1").WithArguments("x1").WithLocation(8, 13)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_EventDeclaration_07()
        {
            string source =
@"
Test();
event System.Action a = H.Dummy(1 is var x1), b = Test(), c = H.Dummy(2 is var x2);
Test();

System.Action Test()
{
    System.Console.WriteLine(""{0} {1}"", x1, x2);
    return null;
}

class H
{
    public static System.Action Dummy(params object[] x) {return null;}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"0 0
1 0
1 2").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationField(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForDeclarationField(model, x2Decl, x2Ref);
        }

        [Fact]
        public void GlobalCode_DeclaratorArguments_01()
        {
            string source =
@"
 
bool a, b(""5948"" is var x1);
H.Dummy(x1);

void Test()
{
    H.Dummy(x1);
}

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (3,10): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                // bool a, b("5948" is var x1);
                Diagnostic(ErrorCode.ERR_BadVarDecl, @"(""5948"" is var x1").WithLocation(3, 10),
                // (3,10): error CS1003: Syntax error, '[' expected
                // bool a, b("5948" is var x1);
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[").WithLocation(3, 10),
                // (3,27): error CS1003: Syntax error, ']' expected
                // bool a, b("5948" is var x1);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(3, 27)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                AssertContainedInDeclaratorArguments(x1Decl);
                // the following would fail due to https://github.com/dotnet/roslyn/issues/13569
                // VerifyModelForDeclarationField(model, x1Decl, x1Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,6): warning CS0168: The variable 'a' is declared but never used
                    // bool a, b("5948" is var x1);
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "a").WithArguments("a").WithLocation(3, 6),
                    // (3,9): warning CS0168: The variable 'b' is declared but never used
                    // bool a, b("5948" is var x1);
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "b").WithArguments("b").WithLocation(3, 9),
                    // (3,10): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                    // bool a, b("5948" is var x1);
                    Diagnostic(ErrorCode.ERR_BadVarDecl, @"(""5948"" is var x1").WithLocation(3, 10),
                    // (3,10): error CS1003: Syntax error, '[' expected
                    // bool a, b("5948" is var x1);
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[").WithLocation(3, 10),
                    // (3,27): error CS1003: Syntax error, ']' expected
                    // bool a, b("5948" is var x1);
                    Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(3, 27),
                    // (4,9): error CS0165: Use of unassigned local variable 'x1'
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(4, 9),
                    // (6,6): warning CS8321: The local function 'Test' is declared but never used
                    // void Test()
                    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Test").WithArguments("Test").WithLocation(6, 6)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                AssertContainedInDeclaratorArguments(x1Decl);
                VerifyModelForDeclarationOrVarSimplePatternWithoutDataFlow(model, x1Decl, x1Ref);
            }
        }

        [Fact]
        public void GlobalCode_DeclaratorArguments_02()
        {
            string source =
@"
label: 
bool a, b((1 is var x1));
H.Dummy(x1);

void Test()
{
    H.Dummy(x1);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (3,10): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                // bool a, b((1 is var x1));
                Diagnostic(ErrorCode.ERR_BadVarDecl, "((1 is var x1)").WithLocation(3, 10),
                // (3,10): error CS1003: Syntax error, '[' expected
                // bool a, b((1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[").WithLocation(3, 10),
                // (3,24): error CS1003: Syntax error, ']' expected
                // bool a, b((1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(3, 24),
                // (2,1): warning CS0164: This label has not been referenced
                // label: 
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label").WithLocation(2, 1)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                AssertContainedInDeclaratorArguments(x1Decl);
                VerifyModelForDeclarationField(model, x1Decl, x1Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (2,1): warning CS0164: This label has not been referenced
                    // label: 
                    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label").WithLocation(2, 1),
                    // (3,6): warning CS0168: The variable 'a' is declared but never used
                    // bool a, b((1 is var x1));
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "a").WithArguments("a").WithLocation(3, 6),
                    // (3,9): warning CS0168: The variable 'b' is declared but never used
                    // bool a, b((1 is var x1));
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "b").WithArguments("b").WithLocation(3, 9),
                    // (3,10): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                    // bool a, b((1 is var x1));
                    Diagnostic(ErrorCode.ERR_BadVarDecl, "((1 is var x1)").WithLocation(3, 10),
                    // (3,10): error CS1003: Syntax error, '[' expected
                    // bool a, b((1 is var x1));
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[").WithLocation(3, 10),
                    // (3,24): error CS1003: Syntax error, ']' expected
                    // bool a, b((1 is var x1));
                    Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(3, 24),
                    // (4,9): error CS0165: Use of unassigned local variable 'x1'
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(4, 9)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                AssertContainedInDeclaratorArguments(x1Decl);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);
            }
        }

        [Fact]
        public void GlobalCode_DeclaratorArguments_03()
        {
            string source =
@"
 
event System.Action a, b(H.Dummy(1 is var x1));
H.Dummy(x1);

void Test()
{
    H.Dummy(x1);
}

Test();

class H
{
    public static bool Dummy(params object[] x) {return false;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (3,25): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                // event System.Action a, b(H.Dummy(1 is var x1));
                Diagnostic(ErrorCode.ERR_BadVarDecl, "(H.Dummy(1 is var x1)").WithLocation(3, 25),
                // (3,25): error CS1003: Syntax error, '[' expected
                // event System.Action a, b(H.Dummy(1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[").WithLocation(3, 25),
                // (3,46): error CS1003: Syntax error, ']' expected
                // event System.Action a, b(H.Dummy(1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(3, 46)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                AssertContainedInDeclaratorArguments(x1Decl);
                // the following would fail due to https://github.com/dotnet/roslyn/issues/13569
                // VerifyModelForDeclarationField(model, x1Decl, x1Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,25): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                    // event System.Action a, b(H.Dummy(1 is var x1));
                    Diagnostic(ErrorCode.ERR_BadVarDecl, "(H.Dummy(1 is var x1)").WithLocation(3, 25),
                    // (3,25): error CS1003: Syntax error, '[' expected
                    // event System.Action a, b(H.Dummy(1 is var x1));
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[").WithLocation(3, 25),
                    // (3,46): error CS1003: Syntax error, ']' expected
                    // event System.Action a, b(H.Dummy(1 is var x1));
                    Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(3, 46),
                    // (4,9): error CS0103: The name 'x1' does not exist in the current context
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(4, 9),
                    // (8,13): error CS0103: The name 'x1' does not exist in the current context
                    //     H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(8, 13)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                AssertContainedInDeclaratorArguments(x1Decl);
                // the following would fail due to https://github.com/dotnet/roslyn/issues/13569
                // VerifyModelForDeclarationOrVarSimplePatternWithoutDataFlow(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);
            }
        }

        [Fact]
        public void GlobalCode_DeclaratorArguments_04()
        {
            string source =
@"

fixed bool a[2], b[H.Dummy(1 is var x1)];
H.Dummy(x1);

void Test()
{
    H.Dummy(x1);
}

Test();

class H
{
    public static int Dummy(params object[] x) {return 0;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
                                                                  parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (3,18): error CS1642: Fixed size buffer fields may only be members of structs
                // fixed bool a[2], b[H.Dummy(1 is var x1)];
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "b").WithLocation(3, 18),
                // (3,20): error CS0133: The expression being assigned to 'b' must be constant
                // fixed bool a[2], b[H.Dummy(1 is var x1)];
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "H.Dummy(1 is var x1)").WithArguments("b").WithLocation(3, 20),
                // (3,12): error CS1642: Fixed size buffer fields may only be members of structs
                // fixed bool a[2], b[H.Dummy(1 is var x1)];
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "a").WithLocation(3, 12),
                // (3,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // fixed bool a[2], b[H.Dummy(1 is var x1)];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a[2]").WithLocation(3, 12)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                AssertContainedInDeclaratorArguments(x1Decl);
                // the following would fail due to https://github.com/dotnet/roslyn/issues/13569
                // VerifyModelForDeclarationField(model, x1Decl, x1Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (3,12): error CS0116: A namespace cannot directly contain members such as fields or methods
                    // fixed bool a[2], b[H.Dummy(1 is var x1)];
                    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "a").WithLocation(3, 12),
                    // (3,18): error CS1642: Fixed size buffer fields may only be members of structs
                    // fixed bool a[2], b[H.Dummy(1 is var x1)];
                    Diagnostic(ErrorCode.ERR_FixedNotInStruct, "b").WithLocation(3, 18),
                    // (3,12): error CS1642: Fixed size buffer fields may only be members of structs
                    // fixed bool a[2], b[H.Dummy(1 is var x1)];
                    Diagnostic(ErrorCode.ERR_FixedNotInStruct, "a").WithLocation(3, 12),
                    // (3,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    // fixed bool a[2], b[H.Dummy(1 is var x1)];
                    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a[2]").WithLocation(3, 12),
                    // (3,20): error CS0133: The expression being assigned to '<invalid-global-code>.b' must be constant
                    // fixed bool a[2], b[H.Dummy(1 is var x1)];
                    Diagnostic(ErrorCode.ERR_NotConstantExpression, "H.Dummy(1 is var x1)").WithArguments("<invalid-global-code>.b").WithLocation(3, 20),
                    // (4,9): error CS0103: The name 'x1' does not exist in the current context
                    // H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(4, 9),
                    // (8,13): error CS0103: The name 'x1' does not exist in the current context
                    //     H.Dummy(x1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(8, 13)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                AssertContainedInDeclaratorArguments(x1Decl);
                // the following would fail due to https://github.com/dotnet/roslyn/issues/13569
                //VerifyModelForDeclarationOrVarSimplePatternWithoutDataFlow(model, x1Decl);
                VerifyNotInScope(model, x1Ref[0]);
                VerifyNotInScope(model, x1Ref[1]);
            }
        }

        [Fact]
        public void GlobalCode_RestrictedType_01()
        {
            string source =
@"

H.Dummy(null is System.ArgIterator x1);

class H
{
    public static void Dummy(params object[] x) {}
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            compilation.GetDeclarationDiagnostics().Verify(
                // (3,17): error CS0610: Field or property cannot be of type 'ArgIterator'
                // H.Dummy(null is System.ArgIterator x1);
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(3, 17)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            VerifyModelForDeclarationField(model, x1Decl);
        }

        [Fact]
        public void GlobalCode_StaticType_01()
        {
            string source =
@"
H.Dummy(null is StaticType x1);

class H
{
    public static void Dummy(params object[] x) {}
}

static class StaticType{}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            compilation.GetDeclarationDiagnostics().Verify(
                // (2,28): error CS0723: Cannot declare a variable of static type 'StaticType'
                // H.Dummy(null is StaticType x1);
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "x1").WithArguments("StaticType").WithLocation(2, 28)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            VerifyModelForDeclarationField(model, x1Decl);
        }

        [Fact]
        public void GlobalCode_AliasInfo_01()
        {
            string source =
@"
H.Dummy(1 is var x1);

class H
{
    public static void Dummy(params object[] x) {}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            Assert.True(x1Decl.Parent is VarPatternSyntax);
        }

        [Fact]
        public void GlobalCode_AliasInfo_02()
        {
            string source =
@"
using @var = System.Int32;

H.Dummy(1 is var x1);

class H
{
    public static void Dummy(params object[] x) {}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics(
                // (4,14): error CS8508: The syntax 'var' for a pattern is not permitted to refer to a type, but 'var' is in scope here.
                // H.Dummy(1 is var x1);
                Diagnostic(ErrorCode.ERR_VarMayNotBindToType, "var").WithArguments("var").WithLocation(4, 14)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            Assert.True(x1Decl.Parent is VarPatternSyntax);
        }

        [Fact]
        public void GlobalCode_AliasInfo_03()
        {
            string source =
@"
using a = System.Int32;

H.Dummy(1 is a x1);

class H
{
    public static void Dummy(params object[] x) {}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1 = (DeclarationPatternSyntax)x1Decl.Parent;
            Assert.Equal("a=System.Int32", model.GetAliasInfo(x1.Type).ToTestDisplayString());
        }

        [Fact]
        public void GlobalCode_AliasInfo_04()
        {
            string source =
@"
H.Dummy(1 is int x1);

class H
{
    public static void Dummy(params object[] x) {}
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1 = (DeclarationPatternSyntax)x1Decl.Parent;
            Assert.Null(model.GetAliasInfo(x1.Type));
        }

        [Fact]
        public void GlobalCode_Catch_01()
        {
            var source =
@"
bool Dummy(params object[] x) {return true;}

try {}
catch when (123 is var x1 && x1 > 0)
{
    Dummy(x1);
}

var x4 = 11;
Dummy(x4);

try {}
catch when (123 is var x4 && x4 > 0)
{
    Dummy(x4);
}

try {}
catch when (x6 && 123 is var x6)
{
    Dummy(x6);
}

try {}
catch when (123 is var x7 && x7 > 0)
{
    var x7 = 12;
    Dummy(x7);
}

try {}
catch when (123 is var x8 && x8 > 0)
{
    Dummy(x8);
}

System.Console.WriteLine(x8);

try {}
catch when (123 is var x9 && x9 > 0)
{   
    Dummy(x9);
    try {}
    catch when (123 is var x9 && x9 > 0) // 2
    {
        Dummy(x9);
    }
}

try {}
catch when (y10 is var x10)
{   
    var y10 = 12;
    Dummy(y10);
}

//    try {}
//    catch when (y11 is var x11)
//    {   
//        let y11 = 12;
//        Dummy(y11);
//    }

try {}
catch when (Dummy(123 is var x14, 
                    123 is var x14, // 2
                    x14))
{
    Dummy(x14);
}

try {}
catch (System.Exception x15)
        when (Dummy(123 is var x15, x15))
{
    Dummy(x15);
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
                compilation.VerifyDiagnostics(
                // (20,13): error CS0841: Cannot use local variable 'x6' before it is declared
                // catch when (x6 && 123 is var x6)
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(20, 13),
                // (28,9): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(28, 9),
                // (38,26): error CS0103: The name 'x8' does not exist in the current context
                // System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(38, 26),
                // (45,28): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     catch when (123 is var x9 && x9 > 0) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(45, 28),
                // (52,13): error CS0103: The name 'y10' does not exist in the current context
                // catch when (y10 is var x10)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(52, 13),
                // (67,32): error CS0128: A local variable or function named 'x14' is already defined in this scope
                //                     123 is var x14, // 2
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(67, 32),
                // (75,32): error CS0128: A local variable or function named 'x15' is already defined in this scope
                //         when (Dummy(123 is var x15, x15))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(75, 32)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclaration(tree, "x1");
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x4Decl = GetPatternDeclaration(tree, "x4");
                var x4Ref = GetReferences(tree, "x4").ToArray();
                Assert.Equal(3, x4Ref.Length);
                VerifyNotAPatternLocal(model, x4Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl, x4Ref[1], x4Ref[2]);

                var x6Decl = GetPatternDeclaration(tree, "x6");
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(2, x6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl, x6Ref);

                var x7Decl = GetPatternDeclaration(tree, "x7");
                var x7Ref = GetReferences(tree, "x7").ToArray();
                Assert.Equal(2, x7Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x7Decl, x7Ref[0]);
                VerifyNotAPatternLocal(model, x7Ref[1]);

                var x8Decl = GetPatternDeclaration(tree, "x8");
                var x8Ref = GetReferences(tree, "x8").ToArray();
                Assert.Equal(3, x8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x8Decl, x8Ref[0], x8Ref[1]);
                VerifyNotInScope(model, x8Ref[2]);

                var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
                var x9Ref = GetReferences(tree, "x9").ToArray();
                Assert.Equal(2, x9Decl.Length);
                Assert.Equal(4, x9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

                var y10Ref = GetReferences(tree, "y10").ToArray();
                Assert.Equal(2, y10Ref.Length);
                VerifyNotInScope(model, y10Ref[0]);
                VerifyNotAPatternLocal(model, y10Ref[1]);

                var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
                var x14Ref = GetReferences(tree, "x14").ToArray();
                Assert.Equal(2, x14Decl.Length);
                Assert.Equal(2, x14Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x14Decl[0], x14Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x14Decl[1]);

                var x15Decl = GetPatternDeclaration(tree, "x15");
                var x15Ref = GetReferences(tree, "x15").ToArray();
                Assert.Equal(2, x15Ref.Length);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x15Decl);
                VerifyNotAPatternLocal(model, x15Ref[0]);
                VerifyNotAPatternLocal(model, x15Ref[1]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (14,24): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // catch when (123 is var x4 && x4 > 0)
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(14, 24),
                    // (20,13): error CS0841: Cannot use local variable 'x6' before it is declared
                    // catch when (x6 && 123 is var x6)
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(20, 13),
                    // (28,9): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     var x7 = 12;
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(28, 9),
                    // (38,26): error CS0103: The name 'x8' does not exist in the current context
                    // System.Console.WriteLine(x8);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(38, 26),
                    // (45,28): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     catch when (123 is var x9 && x9 > 0) // 2
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(45, 28),
                    // (52,13): error CS0103: The name 'y10' does not exist in the current context
                    // catch when (y10 is var x10)
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(52, 13),
                    // (67,32): error CS0128: A local variable or function named 'x14' is already defined in this scope
                    //                     123 is var x14, // 2
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(67, 32),
                    // (75,32): error CS0128: A local variable or function named 'x15' is already defined in this scope
                    //         when (Dummy(123 is var x15, x15))
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(75, 32)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclaration(tree, "x1");
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x4Decl = GetPatternDeclaration(tree, "x4");
                var x4Ref = GetReferences(tree, "x4").ToArray();
                Assert.Equal(3, x4Ref.Length);
                VerifyNotAPatternLocal(model, x4Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl, x4Ref[1], x4Ref[2]);

                var x6Decl = GetPatternDeclaration(tree, "x6");
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(2, x6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl, x6Ref);

                var x7Decl = GetPatternDeclaration(tree, "x7");
                var x7Ref = GetReferences(tree, "x7").ToArray();
                Assert.Equal(2, x7Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x7Decl, x7Ref[0]);
                VerifyNotAPatternLocal(model, x7Ref[1]);

                var x8Decl = GetPatternDeclaration(tree, "x8");
                var x8Ref = GetReferences(tree, "x8").ToArray();
                Assert.Equal(3, x8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x8Decl, x8Ref[0], x8Ref[1]);
                VerifyNotInScope(model, x8Ref[2]);

                var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
                var x9Ref = GetReferences(tree, "x9").ToArray();
                Assert.Equal(2, x9Decl.Length);
                Assert.Equal(4, x9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

                var y10Ref = GetReferences(tree, "y10").ToArray();
                Assert.Equal(2, y10Ref.Length);
                VerifyNotInScope(model, y10Ref[0]);
                VerifyNotAPatternLocal(model, y10Ref[1]);

                var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
                var x14Ref = GetReferences(tree, "x14").ToArray();
                Assert.Equal(2, x14Decl.Length);
                Assert.Equal(2, x14Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x14Decl[0], x14Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x14Decl[1]);

                var x15Decl = GetPatternDeclaration(tree, "x15");
                var x15Ref = GetReferences(tree, "x15").ToArray();
                Assert.Equal(2, x15Ref.Length);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x15Decl);
                VerifyNotAPatternLocal(model, x15Ref[0]);
                VerifyNotAPatternLocal(model, x15Ref[1]);
            }
        }

        [Fact]
        public void GlobalCode_Catch_02()
        {
            var source =
@"
try
{
    throw new System.InvalidOperationException();
}
catch (System.Exception e) when (Dummy(e is var x1, x1))
{
    System.Console.WriteLine(x1.GetType());
}

static bool Dummy(object y, object z) 
{
    System.Console.WriteLine(z.GetType());
    return true;
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
            CompileAndVerify(compilation, expectedOutput:
@"System.InvalidOperationException
System.InvalidOperationException");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_Block_01()
        {
            string source =
@"
{
    H.Dummy(1 is var x1);
    H.Dummy(x1);
}

object x2;
{
    H.Dummy(2 is var x2);
    H.Dummy(x2);
}
{
    H.Dummy(3 is var x3);
}
H.Dummy(x3);

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (15,9): error CS0103: The name 'x3' does not exist in the current context
                // H.Dummy(x3);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(15, 9)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(1, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotInScope(model, x3Ref);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (7,8): warning CS0168: The variable 'x2' is declared but never used
                    // object x2;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x2").WithArguments("x2").WithLocation(7, 8),
                    // (9,22): error CS0136: A local or parameter named 'x2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     H.Dummy(2 is var x2);
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x2").WithArguments("x2").WithLocation(9, 22),
                    // (15,9): error CS0103: The name 'x3' does not exist in the current context
                    // H.Dummy(x3);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(15, 9)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(1, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl);
                VerifyNotInScope(model, x3Ref);
            }
        }

        [Fact]
        public void GlobalCode_Block_02()
        {
            string source =
@"
{
    var tmp = 1 is var x1;
    System.Console.WriteLine(x1);
    Test();

    void Test()
    {
        System.Console.WriteLine(x1);
    }
}
";

            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"1
1").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_For_01()
        {
            var source =
@"
bool Dummy(params object[] x) {return true;}

for (
        Dummy(true is var x1 && x1)
        ;;)
{
    Dummy(x1);
}

for ( // 2
        Dummy(true is var x2 && x2)
        ;;)
    Dummy(x2);

var x4 = 11;
Dummy(x4);

for (
        Dummy(true is var x4 && x4)
        ;;)
    Dummy(x4);

for (
        Dummy(x6 && true is var x6)
        ;;)
    Dummy(x6);

for (
        Dummy(true is var x7 && x7)
        ;;)
{
    var x7 = 12;
    Dummy(x7);
}

for (
        Dummy(true is var x8 && x8)
        ;;)
    Dummy(x8);

System.Console.WriteLine(x8);

for (
        Dummy(true is var x9 && x9)
        ;;)
{   
    Dummy(x9);
    for (
            Dummy(true is var x9 && x9) // 2
            ;;)
        Dummy(x9);
}

for (
        Dummy(y10 is var x10)
        ;;)
{   
    var y10 = 12;
    Dummy(y10);
}

//    for (
//         Dummy(y11 is var x11)
//         ;;)
//    {   
//        let y11 = 12;
//        Dummy(y11);
//    }

for (
        Dummy(y12 is var x12)
        ;;)
    var y12 = 12;

//    for (
//         Dummy(y13 is var x13)
//         ;;)
//        let y13 = 12;

for (
        Dummy(1 is var x14, 
            2 is var x14, 
            x14)
        ;;)
{
    Dummy(x14);
}
";

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
                compilation.VerifyDiagnostics(
                // (74,5): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //     var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(74, 5),
                // (25,15): error CS0841: Cannot use local variable 'x6' before it is declared
                //         Dummy(x6 && true is var x6)
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(25, 15),
                // (33,9): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(33, 9),
                // (42,26): error CS0103: The name 'x8' does not exist in the current context
                // System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(42, 26),
                // (50,31): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             Dummy(true is var x9 && x9) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(50, 31),
                // (56,15): error CS0103: The name 'y10' does not exist in the current context
                //         Dummy(y10 is var x10)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(56, 15),
                // (72,15): error CS0103: The name 'y12' does not exist in the current context
                //         Dummy(y12 is var x12)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(72, 15),
                // (83,22): error CS0128: A local variable or function named 'x14' is already defined in this scope
                //             2 is var x14, 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(83, 22),
                // (11,1): warning CS0162: Unreachable code detected
                // for ( // 2
                Diagnostic(ErrorCode.WRN_UnreachableCode, "for").WithLocation(11, 1),
                // (74,9): warning CS0219: The variable 'y12' is assigned but its value is never used
                //     var y12 = 12;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(74, 9)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").ToArray();
                Assert.Equal(2, x2Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").Single();
                var x4Ref = GetReferences(tree, "x4").ToArray();
                Assert.Equal(3, x4Ref.Length);
                VerifyNotAPatternLocal(model, x4Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl, x4Ref[1], x4Ref[2]);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(2, x6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl, x6Ref);

                var x7Decl = GetPatternDeclarations(tree, "x7").Single();
                var x7Ref = GetReferences(tree, "x7").ToArray();
                Assert.Equal(2, x7Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x7Decl, x7Ref[0]);
                VerifyNotAPatternLocal(model, x7Ref[1]);

                var x8Decl = GetPatternDeclarations(tree, "x8").Single();
                var x8Ref = GetReferences(tree, "x8").ToArray();
                Assert.Equal(3, x8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x8Decl, x8Ref[0], x8Ref[1]);
                VerifyNotInScope(model, x8Ref[2]);

                var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
                var x9Ref = GetReferences(tree, "x9").ToArray();
                Assert.Equal(2, x9Decl.Length);
                Assert.Equal(4, x9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

                var y10Ref = GetReferences(tree, "y10").ToArray();
                Assert.Equal(2, y10Ref.Length);
                VerifyNotInScope(model, y10Ref[0]);
                VerifyNotAPatternLocal(model, y10Ref[1]);

                var y12Ref = GetReferences(tree, "y12").Single();
                VerifyNotInScope(model, y12Ref);

                var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
                var x14Ref = GetReferences(tree, "x14").ToArray();
                Assert.Equal(2, x14Decl.Length);
                Assert.Equal(2, x14Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x14Decl[0], x14Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x14Decl[1]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (20,27): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //         Dummy(true is var x4 && x4)
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(20, 27),
                    // (74,5): error CS1023: Embedded statement cannot be a declaration or labeled statement
                    //     var y12 = 12;
                    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(74, 5),
                    // (25,15): error CS0841: Cannot use local variable 'x6' before it is declared
                    //         Dummy(x6 && true is var x6)
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(25, 15),
                    // (33,9): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     var x7 = 12;
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(33, 9),
                    // (42,26): error CS0103: The name 'x8' does not exist in the current context
                    // System.Console.WriteLine(x8);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(42, 26),
                    // (50,31): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //             Dummy(true is var x9 && x9) // 2
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(50, 31),
                    // (56,15): error CS0103: The name 'y10' does not exist in the current context
                    //         Dummy(y10 is var x10)
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(56, 15),
                    // (72,15): error CS0103: The name 'y12' does not exist in the current context
                    //         Dummy(y12 is var x12)
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(72, 15),
                    // (83,22): error CS0128: A local variable or function named 'x14' is already defined in this scope
                    //             2 is var x14, 
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(83, 22),
                    // (11,1): warning CS0162: Unreachable code detected
                    // for ( // 2
                    Diagnostic(ErrorCode.WRN_UnreachableCode, "for").WithLocation(11, 1),
                    // (74,9): warning CS0219: The variable 'y12' is assigned but its value is never used
                    //     var y12 = 12;
                    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(74, 9)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").ToArray();
                Assert.Equal(2, x2Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").Single();
                var x4Ref = GetReferences(tree, "x4").ToArray();
                Assert.Equal(3, x4Ref.Length);
                VerifyNotAPatternLocal(model, x4Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl, x4Ref[1], x4Ref[2]);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(2, x6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl, x6Ref);

                var x7Decl = GetPatternDeclarations(tree, "x7").Single();
                var x7Ref = GetReferences(tree, "x7").ToArray();
                Assert.Equal(2, x7Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x7Decl, x7Ref[0]);
                VerifyNotAPatternLocal(model, x7Ref[1]);

                var x8Decl = GetPatternDeclarations(tree, "x8").Single();
                var x8Ref = GetReferences(tree, "x8").ToArray();
                Assert.Equal(3, x8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x8Decl, x8Ref[0], x8Ref[1]);
                VerifyNotInScope(model, x8Ref[2]);

                var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
                var x9Ref = GetReferences(tree, "x9").ToArray();
                Assert.Equal(2, x9Decl.Length);
                Assert.Equal(4, x9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

                var y10Ref = GetReferences(tree, "y10").ToArray();
                Assert.Equal(2, y10Ref.Length);
                VerifyNotInScope(model, y10Ref[0]);
                VerifyNotAPatternLocal(model, y10Ref[1]);

                var y12Ref = GetReferences(tree, "y12").Single();
                VerifyNotInScope(model, y12Ref);

                var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
                var x14Ref = GetReferences(tree, "x14").ToArray();
                Assert.Equal(2, x14Decl.Length);
                Assert.Equal(2, x14Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x14Decl[0], x14Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x14Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_For_02()
        {
            var source =
@"
bool f = true;

for (Dummy(f, ((f ? 10 : 20)) is var x0, x0); 
        Dummy(f, ((f ? 1 : 2)) is var x1, x1); 
        Dummy(f, ((f ? 100 : 200)) is var x2, x2), Dummy(true, null, x2))
{
    System.Console.WriteLine(x0);
    System.Console.WriteLine(x1);
    f = false;
}

static bool Dummy(bool x, object y, object z) 
{
    System.Console.WriteLine(z);
    return x;
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
            CompileAndVerify(compilation, expectedOutput:
@"10
1
10
1
200
200
2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x0Decl = GetPatternDeclarations(tree, "x0").Single();
            var x0Ref = GetReferences(tree, "x0").ToArray();
            Assert.Equal(2, x0Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x0Decl, x0Ref);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);
        }

        [Fact]
        public void GlobalCode_Foreach_01()
        {
            var source =
@"
System.Collections.IEnumerable Dummy(params object[] x) {return null;}

foreach (var i in Dummy(true is var x1 && x1))
{
    Dummy(x1);
}

foreach (var i in Dummy(true is var x2 && x2))
    Dummy(x2);

var x4 = 11;
Dummy(x4);

foreach (var i in Dummy(true is var x4 && x4))
    Dummy(x4);

foreach (var i in Dummy(x6 && true is var x6))
    Dummy(x6);

foreach (var i in Dummy(true is var x7 && x7))
{
    var x7 = 12;
    Dummy(x7);
}

foreach (var i in Dummy(true is var x8 && x8))
    Dummy(x8);

System.Console.WriteLine(x8);

foreach (var i1 in Dummy(true is var x9 && x9))
{   
    Dummy(x9);
    foreach (var i2 in Dummy(true is var x9 && x9)) // 2
        Dummy(x9);
}

foreach (var i in Dummy(y10 is var x10))
{   
    var y10 = 12;
    Dummy(y10);
}

//    foreach (var i in Dummy(y11 is var x11))
//    {   
//        let y11 = 12;
//        Dummy(y11);
//    }

foreach (var i in Dummy(y12 is var x12))
    var y12 = 12;

//    foreach (var i in Dummy(y13 is var x13))
//        let y13 = 12;

foreach (var i in Dummy(1 is var x14, 
                        2 is var x14, 
                        x14))
{
    Dummy(x14);
}

foreach (var x15 in 
                    Dummy(1 is var x15, x15))
{
    Dummy(x15);
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
                compilation.VerifyDiagnostics(
                // (52,5): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //     var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(52, 5),
                // (18,25): error CS0841: Cannot use local variable 'x6' before it is declared
                // foreach (var i in Dummy(x6 && true is var x6))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(18, 25),
                // (23,9): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(23, 9),
                // (30,26): error CS0103: The name 'x8' does not exist in the current context
                // System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(30, 26),
                // (35,42): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     foreach (var i2 in Dummy(true is var x9 && x9)) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(35, 42),
                // (39,25): error CS0103: The name 'y10' does not exist in the current context
                // foreach (var i in Dummy(y10 is var x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(39, 25),
                // (51,25): error CS0103: The name 'y12' does not exist in the current context
                // foreach (var i in Dummy(y12 is var x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(51, 25),
                // (58,34): error CS0128: A local variable or function named 'x14' is already defined in this scope
                //                         2 is var x14, 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(58, 34),
                // (64,14): error CS0136: A local or parameter named 'x15' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // foreach (var x15 in 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x15").WithArguments("x15").WithLocation(64, 14),
                // (52,9): warning CS0219: The variable 'y12' is assigned but its value is never used
                //     var y12 = 12;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(52, 9)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").ToArray();
                Assert.Equal(2, x2Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").Single();
                var x4Ref = GetReferences(tree, "x4").ToArray();
                Assert.Equal(3, x4Ref.Length);
                VerifyNotAPatternLocal(model, x4Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl, x4Ref[1], x4Ref[2]);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(2, x6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl, x6Ref);

                var x7Decl = GetPatternDeclarations(tree, "x7").Single();
                var x7Ref = GetReferences(tree, "x7").ToArray();
                Assert.Equal(2, x7Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x7Decl, x7Ref[0]);
                VerifyNotAPatternLocal(model, x7Ref[1]);

                var x8Decl = GetPatternDeclarations(tree, "x8").Single();
                var x8Ref = GetReferences(tree, "x8").ToArray();
                Assert.Equal(3, x8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x8Decl, x8Ref[0], x8Ref[1]);
                VerifyNotInScope(model, x8Ref[2]);

                var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
                var x9Ref = GetReferences(tree, "x9").ToArray();
                Assert.Equal(2, x9Decl.Length);
                Assert.Equal(4, x9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

                var y10Ref = GetReferences(tree, "y10").ToArray();
                Assert.Equal(2, y10Ref.Length);
                VerifyNotInScope(model, y10Ref[0]);
                VerifyNotAPatternLocal(model, y10Ref[1]);

                var y12Ref = GetReferences(tree, "y12").Single();
                VerifyNotInScope(model, y12Ref);

                var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
                var x14Ref = GetReferences(tree, "x14").ToArray();
                Assert.Equal(2, x14Decl.Length);
                Assert.Equal(2, x14Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x14Decl[0], x14Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x14Decl[1]);

                var x15Decl = GetPatternDeclarations(tree, "x15").Single();
                var x15Ref = GetReferences(tree, "x15").ToArray();
                Assert.Equal(2, x15Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x15Decl, x15Ref[0]);
                VerifyNotAPatternLocal(model, x15Ref[1]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (15,37): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // foreach (var i in Dummy(true is var x4 && x4))
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(15, 37),
                    // (52,5): error CS1023: Embedded statement cannot be a declaration or labeled statement
                    //     var y12 = 12;
                    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(52, 5),
                    // (18,25): error CS0841: Cannot use local variable 'x6' before it is declared
                    // foreach (var i in Dummy(x6 && true is var x6))
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(18, 25),
                    // (23,9): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     var x7 = 12;
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(23, 9),
                    // (30,26): error CS0103: The name 'x8' does not exist in the current context
                    // System.Console.WriteLine(x8);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(30, 26),
                    // (35,42): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     foreach (var i2 in Dummy(true is var x9 && x9)) // 2
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(35, 42),
                    // (39,25): error CS0103: The name 'y10' does not exist in the current context
                    // foreach (var i in Dummy(y10 is var x10))
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(39, 25),
                    // (51,25): error CS0103: The name 'y12' does not exist in the current context
                    // foreach (var i in Dummy(y12 is var x12))
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(51, 25),
                    // (58,34): error CS0128: A local variable or function named 'x14' is already defined in this scope
                    //                         2 is var x14, 
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(58, 34),
                    // (64,14): error CS0136: A local or parameter named 'x15' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // foreach (var x15 in 
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x15").WithArguments("x15").WithLocation(64, 14),
                    // (52,9): warning CS0219: The variable 'y12' is assigned but its value is never used
                    //     var y12 = 12;
                    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(52, 9)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").ToArray();
                Assert.Equal(2, x2Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").Single();
                var x4Ref = GetReferences(tree, "x4").ToArray();
                Assert.Equal(3, x4Ref.Length);
                VerifyNotAPatternLocal(model, x4Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl, x4Ref[1], x4Ref[2]);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(2, x6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl, x6Ref);

                var x7Decl = GetPatternDeclarations(tree, "x7").Single();
                var x7Ref = GetReferences(tree, "x7").ToArray();
                Assert.Equal(2, x7Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x7Decl, x7Ref[0]);
                VerifyNotAPatternLocal(model, x7Ref[1]);

                var x8Decl = GetPatternDeclarations(tree, "x8").Single();
                var x8Ref = GetReferences(tree, "x8").ToArray();
                Assert.Equal(3, x8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x8Decl, x8Ref[0], x8Ref[1]);
                VerifyNotInScope(model, x8Ref[2]);

                var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
                var x9Ref = GetReferences(tree, "x9").ToArray();
                Assert.Equal(2, x9Decl.Length);
                Assert.Equal(4, x9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

                var y10Ref = GetReferences(tree, "y10").ToArray();
                Assert.Equal(2, y10Ref.Length);
                VerifyNotInScope(model, y10Ref[0]);
                VerifyNotAPatternLocal(model, y10Ref[1]);

                var y12Ref = GetReferences(tree, "y12").Single();
                VerifyNotInScope(model, y12Ref);

                var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
                var x14Ref = GetReferences(tree, "x14").ToArray();
                Assert.Equal(2, x14Decl.Length);
                Assert.Equal(2, x14Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x14Decl[0], x14Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x14Decl[1]);

                var x15Decl = GetPatternDeclarations(tree, "x15").Single();
                var x15Ref = GetReferences(tree, "x15").ToArray();
                Assert.Equal(2, x15Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x15Decl, x15Ref[0]);
                VerifyNotAPatternLocal(model, x15Ref[1]);
            }
        }

        [Fact]
        public void GlobalCode_Foreach_02()
        {
            var source =
@"
bool f = true;

foreach (var i in Dummy(3 is var x1, x1))
{
    System.Console.WriteLine(x1);
}

static System.Collections.IEnumerable Dummy(object y, object z) 
{
    System.Console.WriteLine(z);
    return ""a"";
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
            CompileAndVerify(compilation, expectedOutput:
@"3
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_Lambda_01()
        {
            var source =
@"
bool Dummy(params object[] x) {return true;}

Dummy((System.Func<int, bool>) (o => o is var x3 && x3 > 0));

Dummy((System.Func<bool, bool>) (o => x4 && o is var x4));

Dummy((System.Func<int, int, bool>) ((o1, o2) => o1 is var x5 && 
                                                        o2 is var x5 && 
                                                        x5 > 0));

Dummy((System.Func<int, bool>) (o => o is var x6 && x6 > 0), (System.Func<int, bool>) (o => o is var x6 && x6 > 0));

Dummy(x7, 1);
Dummy(x7, 
        (System.Func<int, bool>) (o => o is var x7 && x7 > 0), 
        x7);
Dummy(x7, 2); 

Dummy(true is var x8 && x8, (System.Func<bool, bool>) (o => o is var y8 && x8));

Dummy(true is var x9, 
        (System.Func<int, bool>) (o => o is var x9 && 
                                        x9 > 0), x9);

Dummy((System.Func<int, bool>) (o => o is var x10 && 
                                        x10 > 0),
        true is var x10, x10);

var x11 = 11;
Dummy(x11);
Dummy((System.Func<int, bool>) (o => o is var x11 && 
                                        x11 > 0), x11);

Dummy((System.Func<int, bool>) (o => o is var x12 && 
                                        x12 > 0), 
        x12);
var x12 = 11;
Dummy(x12);
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
                compilation.VerifyDiagnostics(
                // (6,39): error CS0841: Cannot use local variable 'x4' before it is declared
                // Dummy((System.Func<bool, bool>) (o => x4 && o is var x4));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(6, 39),
                // (9,67): error CS0128: A local variable or function named 'x5' is already defined in this scope
                //                                                         o2 is var x5 && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(9, 67),
                // (14,7): error CS0103: The name 'x7' does not exist in the current context
                // Dummy(x7, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(14, 7),
                // (15,7): error CS0103: The name 'x7' does not exist in the current context
                // Dummy(x7, 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(15, 7),
                // (17,9): error CS0103: The name 'x7' does not exist in the current context
                //         x7);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(17, 9),
                // (18,7): error CS0103: The name 'x7' does not exist in the current context
                // Dummy(x7, 2); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(18, 7)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").Single();
                var x4Ref = GetReferences(tree, "x4").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").Single();
                Assert.Equal(2, x5Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x5Decl[1]);

                var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(2, x6Decl.Length);
                Assert.Equal(2, x6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl[0], x6Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl[1], x6Ref[1]);

                var x7Decl = GetPatternDeclarations(tree, "x7").Single();
                var x7Ref = GetReferences(tree, "x7").ToArray();
                Assert.Equal(5, x7Ref.Length);
                VerifyNotInScope(model, x7Ref[0]);
                VerifyNotInScope(model, x7Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x7Decl, x7Ref[2]);
                VerifyNotInScope(model, x7Ref[3]);
                VerifyNotInScope(model, x7Ref[4]);

                var x8Decl = GetPatternDeclarations(tree, "x8").Single();
                var x8Ref = GetReferences(tree, "x8").ToArray();
                Assert.Equal(2, x8Ref.Length);
                VerifyModelForDeclarationField(model, x8Decl, x8Ref);

                var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
                var x9Ref = GetReferences(tree, "x9").ToArray();
                Assert.Equal(2, x9Decl.Length);
                Assert.Equal(2, x9Ref.Length);
                VerifyModelForDeclarationField(model, x9Decl[0], x9Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[1], x9Ref[0]);

                var x10Decl = GetPatternDeclarations(tree, "x10").ToArray();
                var x10Ref = GetReferences(tree, "x10").ToArray();
                Assert.Equal(2, x10Decl.Length);
                Assert.Equal(2, x10Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x10Decl[0], x10Ref[0]);
                VerifyModelForDeclarationField(model, x10Decl[1], x10Ref[1]);

                var x11Decl = GetPatternDeclarations(tree, "x11").Single();
                var x11Ref = GetReferences(tree, "x11").ToArray();
                Assert.Equal(3, x11Ref.Length);
                VerifyNotAPatternLocal(model, x11Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x11Decl, x11Ref[1]);
                VerifyNotAPatternLocal(model, x11Ref[2]);

                var x12Decl = GetPatternDeclarations(tree, "x12").Single();
                var x12Ref = GetReferences(tree, "x12").ToArray();
                Assert.Equal(3, x12Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x12Decl, x12Ref[0]);
                VerifyNotAPatternLocal(model, x12Ref[1]);
                VerifyNotAPatternLocal(model, x12Ref[2]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (6,39): error CS0841: Cannot use local variable 'x4' before it is declared
                    // Dummy((System.Func<bool, bool>) (o => x4 && o is var x4));
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(6, 39),
                    // (9,67): error CS0128: A local variable or function named 'x5' is already defined in this scope
                    //                                                         o2 is var x5 && 
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(9, 67),
                    // (14,7): error CS0103: The name 'x7' does not exist in the current context
                    // Dummy(x7, 1);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(14, 7),
                    // (15,7): error CS0103: The name 'x7' does not exist in the current context
                    // Dummy(x7, 
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(15, 7),
                    // (17,9): error CS0103: The name 'x7' does not exist in the current context
                    //         x7);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(17, 9),
                    // (18,7): error CS0103: The name 'x7' does not exist in the current context
                    // Dummy(x7, 2); 
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(18, 7),
                    // (37,9): error CS0841: Cannot use local variable 'x12' before it is declared
                    //         x12);
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(37, 9)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").Single();
                var x4Ref = GetReferences(tree, "x4").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
                var x5Ref = GetReferences(tree, "x5").Single();
                Assert.Equal(2, x5Decl.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x5Decl[0], x5Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x5Decl[1]);

                var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(2, x6Decl.Length);
                Assert.Equal(2, x6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl[0], x6Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl[1], x6Ref[1]);

                var x7Decl = GetPatternDeclarations(tree, "x7").Single();
                var x7Ref = GetReferences(tree, "x7").ToArray();
                Assert.Equal(5, x7Ref.Length);
                VerifyNotInScope(model, x7Ref[0]);
                VerifyNotInScope(model, x7Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x7Decl, x7Ref[2]);
                VerifyNotInScope(model, x7Ref[3]);
                VerifyNotInScope(model, x7Ref[4]);

                var x8Decl = GetPatternDeclarations(tree, "x8").Single();
                var x8Ref = GetReferences(tree, "x8").ToArray();
                Assert.Equal(2, x8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x8Decl, x8Ref);

                var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
                var x9Ref = GetReferences(tree, "x9").ToArray();
                Assert.Equal(2, x9Decl.Length);
                Assert.Equal(2, x9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[0], x9Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[1], x9Ref[0]);

                var x10Decl = GetPatternDeclarations(tree, "x10").ToArray();
                var x10Ref = GetReferences(tree, "x10").ToArray();
                Assert.Equal(2, x10Decl.Length);
                Assert.Equal(2, x10Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x10Decl[0], x10Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x10Decl[1], x10Ref[1]);

                var x11Decl = GetPatternDeclarations(tree, "x11").Single();
                var x11Ref = GetReferences(tree, "x11").ToArray();
                Assert.Equal(3, x11Ref.Length);
                VerifyNotAPatternLocal(model, x11Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x11Decl, x11Ref[1]);
                VerifyNotAPatternLocal(model, x11Ref[2]);

                var x12Decl = GetPatternDeclarations(tree, "x12").Single();
                var x12Ref = GetReferences(tree, "x12").ToArray();
                Assert.Equal(3, x12Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x12Decl, x12Ref[0]);
                VerifyNotAPatternLocal(model, x12Ref[1]);
                VerifyNotAPatternLocal(model, x12Ref[2]);
            }
        }

        [Fact]
        [WorkItem(16935, "https://github.com/dotnet/roslyn/issues/16935")]
        public void GlobalCode_Lambda_02()
        {
            var source =
@"
System.Func<bool> l = () => 1 is int x1 && Dummy(x1); 
System.Console.WriteLine(l());

static bool Dummy(int x) 
{
    System.Console.WriteLine(x);
    return true;
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
            CompileAndVerify(compilation, expectedOutput: @"1
True");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_Lambda_03()
        {
            var source =
@"
System.Console.WriteLine(((System.Func<bool>)(() => 1 is int x1 && Dummy(x1)))());

static bool Dummy(int x) 
{
    System.Console.WriteLine(x);
    return true;
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
            CompileAndVerify(compilation, expectedOutput: @"1
True");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);
        }

        [Fact]
        public void GlobalCode_Query_01()
        {
            var source =
@"
using System.Linq;

bool Dummy(params object[] x) {return true;}

var r01 = from x in new[] { 1 is var y1 ? y1 : 0, y1}
            select x + y1;

Dummy(y1); 

var r02 = from x1 in new[] { 1 is var y2 ? y2 : 0}
            from x2 in new[] { x1 is var z2 ? z2 : 0, z2, y2}
            select x1 + x2 + y2 + 
                    z2;

Dummy(z2); 

var r03 = from x1 in new[] { 1 is var y3 ? y3 : 0}
            let x2 = x1 is var z3 && z3 > 0 && y3 < 0 
            select new { x1, x2, y3,
                        z3};

Dummy(z3); 

var r04 = from x1 in new[] { 1 is var y4 ? y4 : 0}
            join x2 in new[] { 2 is var z4 ? z4 : 0, z4, y4}
                    on x1 + y4 + z4 + (3 is var u4 ? u4 : 0) + 
                            v4 
                        equals x2 + y4 + z4 + (4 is var v4 ? v4 : 0) +
                            u4 
            select new { x1, x2, y4, z4, 
                        u4, v4 };

Dummy(z4); 
Dummy(u4); 
Dummy(v4); 

var r05 = from x1 in new[] { 1 is var y5 ? y5 : 0}
            join x2 in new[] { 2 is var z5 ? z5 : 0, z5, y5}
                    on x1 + y5 + z5 + (3 is var u5 ? u5 : 0) + 
                            v5 
                        equals x2 + y5 + z5 + (4 is var v5 ? v5 : 0) +
                            u5 
            into g
            select new { x1, y5, z5, g,
                        u5, v5 };

Dummy(z5); 
Dummy(u5); 
Dummy(v5); 

var r06 = from x in new[] { 1 is var y6 ? y6 : 0}
            where x > y6 && 1 is var z6 && z6 == 1
            select x + y6 +
                    z6;

Dummy(z6); 

var r07 = from x in new[] { 1 is var y7 ? y7 : 0}
            orderby x > y7 && 1 is var z7 && z7 == 
                    u7,
                    x > y7 && 1 is var u7 && u7 == 
                    z7   
            select x + y7 +
                    z7 + u7;

Dummy(z7); 
Dummy(u7); 

var r08 = from x in new[] { 1 is var y8 ? y8 : 0}
            select x > y8 && 1 is var z8 && z8 == 1;

Dummy(z8); 

var r09 = from x in new[] { 1 is var y9 ? y9 : 0}
            group x > y9 && 1 is var z9 && z9 == 
                u9
            by
                x > y9 && 1 is var u9 && u9 == 
                z9;   

Dummy(z9); 
Dummy(u9); 

var r10 = from x1 in new[] { 1 is var y10 ? y10 : 0}
            from y10 in new[] { 1 }
            select x1 + y10;

var r11 = from x1 in new[] { 1 is var y11 ? y11 : 0}
            let y11 = x1 + 1
            select x1 + y11;
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemCoreRef }, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
                compilation.VerifyDiagnostics(
                // (14,21): error CS0103: The name 'z2' does not exist in the current context
                //                     z2;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(14, 21),
                // (21,25): error CS0103: The name 'z3' does not exist in the current context
                //                         z3};
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(21, 25),
                // (28,29): error CS0103: The name 'v4' does not exist in the current context
                //                             v4 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(28, 29),
                // (30,29): error CS1938: The name 'u4' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
                //                             u4 
                Diagnostic(ErrorCode.ERR_QueryInnerKey, "u4").WithArguments("u4").WithLocation(30, 29),
                // (32,25): error CS0103: The name 'u4' does not exist in the current context
                //                         u4, v4 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(32, 25),
                // (32,29): error CS0103: The name 'v4' does not exist in the current context
                //                         u4, v4 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(32, 29),
                // (41,29): error CS0103: The name 'v5' does not exist in the current context
                //                             v5 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(41, 29),
                // (43,29): error CS1938: The name 'u5' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
                //                             u5 
                Diagnostic(ErrorCode.ERR_QueryInnerKey, "u5").WithArguments("u5").WithLocation(43, 29),
                // (46,25): error CS0103: The name 'u5' does not exist in the current context
                //                         u5, v5 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(46, 25),
                // (46,29): error CS0103: The name 'v5' does not exist in the current context
                //                         u5, v5 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(46, 29),
                // (55,21): error CS0103: The name 'z6' does not exist in the current context
                //                     z6;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(55, 21),
                // (61,21): error CS0103: The name 'u7' does not exist in the current context
                //                     u7,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(61, 21),
                // (63,21): error CS0103: The name 'z7' does not exist in the current context
                //                     z7   
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(63, 21),
                // (65,21): error CS0103: The name 'z7' does not exist in the current context
                //                     z7 + u7;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(65, 21),
                // (65,26): error CS0103: The name 'u7' does not exist in the current context
                //                     z7 + u7;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(65, 26),
                // (80,17): error CS0103: The name 'z9' does not exist in the current context
                //                 z9;   
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z9").WithArguments("z9").WithLocation(80, 17),
                // (77,17): error CS0103: The name 'u9' does not exist in the current context
                //                 u9
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u9").WithArguments("u9").WithLocation(77, 17),
                // (16,7): error CS0103: The name 'z2' does not exist in the current context
                // Dummy(z2); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(16, 7),
                // (23,7): error CS0103: The name 'z3' does not exist in the current context
                // Dummy(z3); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(23, 7),
                // (35,7): error CS0103: The name 'u4' does not exist in the current context
                // Dummy(u4); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(35, 7),
                // (36,7): error CS0103: The name 'v4' does not exist in the current context
                // Dummy(v4); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(36, 7),
                // (49,7): error CS0103: The name 'u5' does not exist in the current context
                // Dummy(u5); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(49, 7),
                // (50,7): error CS0103: The name 'v5' does not exist in the current context
                // Dummy(v5); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(50, 7),
                // (57,7): error CS0103: The name 'z6' does not exist in the current context
                // Dummy(z6); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(57, 7),
                // (67,7): error CS0103: The name 'z7' does not exist in the current context
                // Dummy(z7); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(67, 7),
                // (68,7): error CS0103: The name 'u7' does not exist in the current context
                // Dummy(u7); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(68, 7),
                // (73,7): error CS0103: The name 'z8' does not exist in the current context
                // Dummy(z8); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z8").WithArguments("z8").WithLocation(73, 7),
                // (82,7): error CS0103: The name 'z9' does not exist in the current context
                // Dummy(z9); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z9").WithArguments("z9").WithLocation(82, 7),
                // (83,7): error CS0103: The name 'u9' does not exist in the current context
                // Dummy(u9); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u9").WithArguments("u9").WithLocation(83, 7)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var y1Decl = GetPatternDeclarations(tree, "y1").Single();
                var y1Ref = GetReferences(tree, "y1").ToArray();
                Assert.Equal(4, y1Ref.Length);
                VerifyModelForDeclarationField(model, y1Decl, y1Ref);

                var y2Decl = GetPatternDeclarations(tree, "y2").Single();
                var y2Ref = GetReferences(tree, "y2").ToArray();
                Assert.Equal(3, y2Ref.Length);
                VerifyModelForDeclarationField(model, y2Decl, y2Ref);

                var z2Decl = GetPatternDeclarations(tree, "z2").Single();
                var z2Ref = GetReferences(tree, "z2").ToArray();
                Assert.Equal(4, z2Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z2Decl, z2Ref[0], z2Ref[1]);
                VerifyNotInScope(model, z2Ref[2]);
                VerifyNotInScope(model, z2Ref[3]);

                var y3Decl = GetPatternDeclarations(tree, "y3").Single();
                var y3Ref = GetReferences(tree, "y3").ToArray();
                Assert.Equal(3, y3Ref.Length);
                VerifyModelForDeclarationField(model, y3Decl, y3Ref);

                var z3Decl = GetPatternDeclarations(tree, "z3").Single();
                var z3Ref = GetReferences(tree, "z3").ToArray();
                Assert.Equal(3, z3Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z3Decl, z3Ref[0]);
                VerifyNotInScope(model, z3Ref[1]);
                VerifyNotInScope(model, z3Ref[2]);

                var y4Decl = GetPatternDeclarations(tree, "y4").Single();
                var y4Ref = GetReferences(tree, "y4").ToArray();
                Assert.Equal(5, y4Ref.Length);
                VerifyModelForDeclarationField(model, y4Decl, y4Ref);

                var z4Decl = GetPatternDeclarations(tree, "z4").Single();
                var z4Ref = GetReferences(tree, "z4").ToArray();
                Assert.Equal(6, z4Ref.Length);
                VerifyModelForDeclarationField(model, z4Decl, z4Ref);

                var u4Decl = GetPatternDeclarations(tree, "u4").Single();
                var u4Ref = GetReferences(tree, "u4").ToArray();
                Assert.Equal(4, u4Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, u4Decl, u4Ref[0]);
                VerifyNotInScope(model, u4Ref[1]);
                VerifyNotInScope(model, u4Ref[2]);
                VerifyNotInScope(model, u4Ref[3]);

                var v4Decl = GetPatternDeclarations(tree, "v4").Single();
                var v4Ref = GetReferences(tree, "v4").ToArray();
                Assert.Equal(4, v4Ref.Length);
                VerifyNotInScope(model, v4Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, v4Decl, v4Ref[1]);
                VerifyNotInScope(model, v4Ref[2]);
                VerifyNotInScope(model, v4Ref[3]);

                var y5Decl = GetPatternDeclarations(tree, "y5").Single();
                var y5Ref = GetReferences(tree, "y5").ToArray();
                Assert.Equal(5, y5Ref.Length);
                VerifyModelForDeclarationField(model, y5Decl, y5Ref);

                var z5Decl = GetPatternDeclarations(tree, "z5").Single();
                var z5Ref = GetReferences(tree, "z5").ToArray();
                Assert.Equal(6, z5Ref.Length);
                VerifyModelForDeclarationField(model, z5Decl, z5Ref);

                var u5Decl = GetPatternDeclarations(tree, "u5").Single();
                var u5Ref = GetReferences(tree, "u5").ToArray();
                Assert.Equal(4, u5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, u5Decl, u5Ref[0]);
                VerifyNotInScope(model, u5Ref[1]);
                VerifyNotInScope(model, u5Ref[2]);
                VerifyNotInScope(model, u5Ref[3]);

                var v5Decl = GetPatternDeclarations(tree, "v5").Single();
                var v5Ref = GetReferences(tree, "v5").ToArray();
                Assert.Equal(4, v5Ref.Length);
                VerifyNotInScope(model, v5Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, v5Decl, v5Ref[1]);
                VerifyNotInScope(model, v5Ref[2]);
                VerifyNotInScope(model, v5Ref[3]);

                var y6Decl = GetPatternDeclarations(tree, "y6").Single();
                var y6Ref = GetReferences(tree, "y6").ToArray();
                Assert.Equal(3, y6Ref.Length);
                VerifyModelForDeclarationField(model, y6Decl, y6Ref);

                var z6Decl = GetPatternDeclarations(tree, "z6").Single();
                var z6Ref = GetReferences(tree, "z6").ToArray();
                Assert.Equal(3, z6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z6Decl, z6Ref[0]);
                VerifyNotInScope(model, z6Ref[1]);
                VerifyNotInScope(model, z6Ref[2]);

                var y7Decl = GetPatternDeclarations(tree, "y7").Single();
                var y7Ref = GetReferences(tree, "y7").ToArray();
                Assert.Equal(4, y7Ref.Length);
                VerifyModelForDeclarationField(model, y7Decl, y7Ref);

                var z7Decl = GetPatternDeclarations(tree, "z7").Single();
                var z7Ref = GetReferences(tree, "z7").ToArray();
                Assert.Equal(4, z7Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z7Decl, z7Ref[0]);
                VerifyNotInScope(model, z7Ref[1]);
                VerifyNotInScope(model, z7Ref[2]);
                VerifyNotInScope(model, z7Ref[3]);

                var u7Decl = GetPatternDeclarations(tree, "u7").Single();
                var u7Ref = GetReferences(tree, "u7").ToArray();
                Assert.Equal(4, u7Ref.Length);
                VerifyNotInScope(model, u7Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, u7Decl, u7Ref[1]);
                VerifyNotInScope(model, u7Ref[2]);
                VerifyNotInScope(model, u7Ref[3]);

                var y8Decl = GetPatternDeclarations(tree, "y8").Single();
                var y8Ref = GetReferences(tree, "y8").ToArray();
                Assert.Equal(2, y8Ref.Length);
                VerifyModelForDeclarationField(model, y8Decl, y8Ref);

                var z8Decl = GetPatternDeclarations(tree, "z8").Single();
                var z8Ref = GetReferences(tree, "z8").ToArray();
                Assert.Equal(2, z8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z8Decl, z8Ref[0]);
                VerifyNotInScope(model, z8Ref[1]);

                var y9Decl = GetPatternDeclarations(tree, "y9").Single();
                var y9Ref = GetReferences(tree, "y9").ToArray();
                Assert.Equal(3, y9Ref.Length);
                VerifyModelForDeclarationField(model, y9Decl, y9Ref);

                var z9Decl = GetPatternDeclarations(tree, "z9").Single();
                var z9Ref = GetReferences(tree, "z9").ToArray();
                Assert.Equal(3, z9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z9Decl, z9Ref[0]);
                VerifyNotInScope(model, z9Ref[1]);
                VerifyNotInScope(model, z9Ref[2]);

                var u9Decl = GetPatternDeclarations(tree, "u9").Single();
                var u9Ref = GetReferences(tree, "u9").ToArray();
                Assert.Equal(3, u9Ref.Length);
                VerifyNotInScope(model, u9Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, u9Decl, u9Ref[1]);
                VerifyNotInScope(model, u9Ref[2]);

                var y10Decl = GetPatternDeclarations(tree, "y10").Single();
                var y10Ref = GetReferences(tree, "y10").ToArray();
                Assert.Equal(2, y10Ref.Length);
                VerifyModelForDeclarationField(model, y10Decl, y10Ref[0]);
                VerifyNotAPatternField(model, y10Ref[1]);

                var y11Decl = GetPatternDeclarations(tree, "y11").Single();
                var y11Ref = GetReferences(tree, "y11").ToArray();
                Assert.Equal(2, y11Ref.Length);
                VerifyModelForDeclarationField(model, y11Decl, y11Ref[0]);
                VerifyNotAPatternField(model, y11Ref[1]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemCoreRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                // (14,21): error CS0103: The name 'z2' does not exist in the current context
                //                     z2;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(14, 21),
                // (21,25): error CS0103: The name 'z3' does not exist in the current context
                //                         z3};
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(21, 25),
                // (28,29): error CS0103: The name 'v4' does not exist in the current context
                //                             v4 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(28, 29),
                // (30,29): error CS1938: The name 'u4' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
                //                             u4 
                Diagnostic(ErrorCode.ERR_QueryInnerKey, "u4").WithArguments("u4").WithLocation(30, 29),
                // (32,25): error CS0103: The name 'u4' does not exist in the current context
                //                         u4, v4 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(32, 25),
                // (32,29): error CS0103: The name 'v4' does not exist in the current context
                //                         u4, v4 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(32, 29),
                // (41,29): error CS0103: The name 'v5' does not exist in the current context
                //                             v5 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(41, 29),
                // (43,29): error CS1938: The name 'u5' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
                //                             u5 
                Diagnostic(ErrorCode.ERR_QueryInnerKey, "u5").WithArguments("u5").WithLocation(43, 29),
                // (46,25): error CS0103: The name 'u5' does not exist in the current context
                //                         u5, v5 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(46, 25),
                // (46,29): error CS0103: The name 'v5' does not exist in the current context
                //                         u5, v5 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(46, 29),
                // (55,21): error CS0103: The name 'z6' does not exist in the current context
                //                     z6;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(55, 21),
                // (61,21): error CS0103: The name 'u7' does not exist in the current context
                //                     u7,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(61, 21),
                // (63,21): error CS0103: The name 'z7' does not exist in the current context
                //                     z7   
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(63, 21),
                // (65,21): error CS0103: The name 'z7' does not exist in the current context
                //                     z7 + u7;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(65, 21),
                // (65,26): error CS0103: The name 'u7' does not exist in the current context
                //                     z7 + u7;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(65, 26),
                // (80,17): error CS0103: The name 'z9' does not exist in the current context
                //                 z9;   
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z9").WithArguments("z9").WithLocation(80, 17),
                // (77,17): error CS0103: The name 'u9' does not exist in the current context
                //                 u9
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u9").WithArguments("u9").WithLocation(77, 17),
                // (16,7): error CS0103: The name 'z2' does not exist in the current context
                // Dummy(z2); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(16, 7),
                // (23,7): error CS0103: The name 'z3' does not exist in the current context
                // Dummy(z3); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(23, 7),
                // (35,7): error CS0103: The name 'u4' does not exist in the current context
                // Dummy(u4); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(35, 7),
                // (36,7): error CS0103: The name 'v4' does not exist in the current context
                // Dummy(v4); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(36, 7),
                // (49,7): error CS0103: The name 'u5' does not exist in the current context
                // Dummy(u5); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(49, 7),
                // (50,7): error CS0103: The name 'v5' does not exist in the current context
                // Dummy(v5); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(50, 7),
                // (57,7): error CS0103: The name 'z6' does not exist in the current context
                // Dummy(z6); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(57, 7),
                // (67,7): error CS0103: The name 'z7' does not exist in the current context
                // Dummy(z7); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(67, 7),
                // (68,7): error CS0103: The name 'u7' does not exist in the current context
                // Dummy(u7); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(68, 7),
                // (73,7): error CS0103: The name 'z8' does not exist in the current context
                // Dummy(z8); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z8").WithArguments("z8").WithLocation(73, 7),
                // (82,7): error CS0103: The name 'z9' does not exist in the current context
                // Dummy(z9); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z9").WithArguments("z9").WithLocation(82, 7),
                // (83,7): error CS0103: The name 'u9' does not exist in the current context
                // Dummy(u9); 
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u9").WithArguments("u9").WithLocation(83, 7),
                // (86,18): error CS1931: The range variable 'y10' conflicts with a previous declaration of 'y10'
                //             from y10 in new[] { 1 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y10").WithArguments("y10").WithLocation(86, 18),
                // (90,17): error CS1931: The range variable 'y11' conflicts with a previous declaration of 'y11'
                //             let y11 = x1 + 1
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y11").WithArguments("y11").WithLocation(90, 17)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var y1Decl = GetPatternDeclarations(tree, "y1").Single();
                var y1Ref = GetReferences(tree, "y1").ToArray();
                Assert.Equal(4, y1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, y1Decl, y1Ref);

                var y2Decl = GetPatternDeclarations(tree, "y2").Single();
                var y2Ref = GetReferences(tree, "y2").ToArray();
                Assert.Equal(3, y2Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, y2Decl, y2Ref);

                var z2Decl = GetPatternDeclarations(tree, "z2").Single();
                var z2Ref = GetReferences(tree, "z2").ToArray();
                Assert.Equal(4, z2Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z2Decl, z2Ref[0], z2Ref[1]);
                VerifyNotInScope(model, z2Ref[2]);
                VerifyNotInScope(model, z2Ref[3]);

                var y3Decl = GetPatternDeclarations(tree, "y3").Single();
                var y3Ref = GetReferences(tree, "y3").ToArray();
                Assert.Equal(3, y3Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, y3Decl, y3Ref);

                var z3Decl = GetPatternDeclarations(tree, "z3").Single();
                var z3Ref = GetReferences(tree, "z3").ToArray();
                Assert.Equal(3, z3Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z3Decl, z3Ref[0]);
                VerifyNotInScope(model, z3Ref[1]);
                VerifyNotInScope(model, z3Ref[2]);

                var y4Decl = GetPatternDeclarations(tree, "y4").Single();
                var y4Ref = GetReferences(tree, "y4").ToArray();
                Assert.Equal(5, y4Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, y4Decl, y4Ref);

                var z4Decl = GetPatternDeclarations(tree, "z4").Single();
                var z4Ref = GetReferences(tree, "z4").ToArray();
                Assert.Equal(6, z4Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z4Decl, z4Ref);

                var u4Decl = GetPatternDeclarations(tree, "u4").Single();
                var u4Ref = GetReferences(tree, "u4").ToArray();
                Assert.Equal(4, u4Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, u4Decl, u4Ref[0]);
                VerifyNotInScope(model, u4Ref[1]);
                VerifyNotInScope(model, u4Ref[2]);
                VerifyNotInScope(model, u4Ref[3]);

                var v4Decl = GetPatternDeclarations(tree, "v4").Single();
                var v4Ref = GetReferences(tree, "v4").ToArray();
                Assert.Equal(4, v4Ref.Length);
                VerifyNotInScope(model, v4Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, v4Decl, v4Ref[1]);
                VerifyNotInScope(model, v4Ref[2]);
                VerifyNotInScope(model, v4Ref[3]);

                var y5Decl = GetPatternDeclarations(tree, "y5").Single();
                var y5Ref = GetReferences(tree, "y5").ToArray();
                Assert.Equal(5, y5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, y5Decl, y5Ref);

                var z5Decl = GetPatternDeclarations(tree, "z5").Single();
                var z5Ref = GetReferences(tree, "z5").ToArray();
                Assert.Equal(6, z5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z5Decl, z5Ref);

                var u5Decl = GetPatternDeclarations(tree, "u5").Single();
                var u5Ref = GetReferences(tree, "u5").ToArray();
                Assert.Equal(4, u5Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, u5Decl, u5Ref[0]);
                VerifyNotInScope(model, u5Ref[1]);
                VerifyNotInScope(model, u5Ref[2]);
                VerifyNotInScope(model, u5Ref[3]);

                var v5Decl = GetPatternDeclarations(tree, "v5").Single();
                var v5Ref = GetReferences(tree, "v5").ToArray();
                Assert.Equal(4, v5Ref.Length);
                VerifyNotInScope(model, v5Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, v5Decl, v5Ref[1]);
                VerifyNotInScope(model, v5Ref[2]);
                VerifyNotInScope(model, v5Ref[3]);

                var y6Decl = GetPatternDeclarations(tree, "y6").Single();
                var y6Ref = GetReferences(tree, "y6").ToArray();
                Assert.Equal(3, y6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, y6Decl, y6Ref);

                var z6Decl = GetPatternDeclarations(tree, "z6").Single();
                var z6Ref = GetReferences(tree, "z6").ToArray();
                Assert.Equal(3, z6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z6Decl, z6Ref[0]);
                VerifyNotInScope(model, z6Ref[1]);
                VerifyNotInScope(model, z6Ref[2]);

                var y7Decl = GetPatternDeclarations(tree, "y7").Single();
                var y7Ref = GetReferences(tree, "y7").ToArray();
                Assert.Equal(4, y7Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, y7Decl, y7Ref);

                var z7Decl = GetPatternDeclarations(tree, "z7").Single();
                var z7Ref = GetReferences(tree, "z7").ToArray();
                Assert.Equal(4, z7Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z7Decl, z7Ref[0]);
                VerifyNotInScope(model, z7Ref[1]);
                VerifyNotInScope(model, z7Ref[2]);
                VerifyNotInScope(model, z7Ref[3]);

                var u7Decl = GetPatternDeclarations(tree, "u7").Single();
                var u7Ref = GetReferences(tree, "u7").ToArray();
                Assert.Equal(4, u7Ref.Length);
                VerifyNotInScope(model, u7Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, u7Decl, u7Ref[1]);
                VerifyNotInScope(model, u7Ref[2]);
                VerifyNotInScope(model, u7Ref[3]);

                var y8Decl = GetPatternDeclarations(tree, "y8").Single();
                var y8Ref = GetReferences(tree, "y8").ToArray();
                Assert.Equal(2, y8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, y8Decl, y8Ref);

                var z8Decl = GetPatternDeclarations(tree, "z8").Single();
                var z8Ref = GetReferences(tree, "z8").ToArray();
                Assert.Equal(2, z8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z8Decl, z8Ref[0]);
                VerifyNotInScope(model, z8Ref[1]);

                var y9Decl = GetPatternDeclarations(tree, "y9").Single();
                var y9Ref = GetReferences(tree, "y9").ToArray();
                Assert.Equal(3, y9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, y9Decl, y9Ref);

                var z9Decl = GetPatternDeclarations(tree, "z9").Single();
                var z9Ref = GetReferences(tree, "z9").ToArray();
                Assert.Equal(3, z9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, z9Decl, z9Ref[0]);
                VerifyNotInScope(model, z9Ref[1]);
                VerifyNotInScope(model, z9Ref[2]);

                var u9Decl = GetPatternDeclarations(tree, "u9").Single();
                var u9Ref = GetReferences(tree, "u9").ToArray();
                Assert.Equal(3, u9Ref.Length);
                VerifyNotInScope(model, u9Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, u9Decl, u9Ref[1]);
                VerifyNotInScope(model, u9Ref[2]);

                var y10Decl = GetPatternDeclarations(tree, "y10").Single();
                var y10Ref = GetReferences(tree, "y10").ToArray();
                Assert.Equal(2, y10Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, y10Decl, y10Ref[0]);
                VerifyNotAPatternLocal(model, y10Ref[1]);

                var y11Decl = GetPatternDeclarations(tree, "y11").Single();
                var y11Ref = GetReferences(tree, "y11").ToArray();
                Assert.Equal(2, y11Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, y11Decl, y11Ref[0]);
                VerifyNotAPatternLocal(model, y11Ref[1]);
            }
        }

        [Fact]
        public void GlobalCode_Query_02()
        {
            var source =
@"
using System.Linq;

var res = from x1 in new[] { 1 is var y1 && Print(y1) ? 2 : 0}
            select Print(x1);

res.ToArray(); 

static bool Print(object x) 
{
    System.Console.WriteLine(x);
    return true;
}
";
            var compilation = CreateCompilationWithMscorlib461(source, new[] { SystemCoreRef }, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            CompileAndVerify(compilation, expectedOutput:
@"1
2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yDecl = GetPatternDeclarations(tree, "y1").Single();
            var yRef = GetReferences(tree, "y1").Single();
            VerifyModelForDeclarationField(model, yDecl, yRef);
        }

        [Fact]
        public void GlobalCode_Using_01()
        {
            var source =
@"
System.IDisposable Dummy(params object[] x) {return null;}

using (Dummy(true is var x1, x1))
{
    Dummy(x1);
}
 
using (Dummy(true is var x2, x2))
    Dummy(x2);
 
var x4 = 11;
Dummy(x4);

using (Dummy(true is var x4, x4))
    Dummy(x4);
 
using (Dummy(x6 && true is var x6))
    Dummy(x6);
 
using (Dummy(true is var x7 && x7))
{
    var x7 = 12;
    Dummy(x7);
}
 
using (Dummy(true is var x8, x8))
    Dummy(x8);

System.Console.WriteLine(x8);
 
using (Dummy(true is var x9, x9))
{   
    Dummy(x9);
    using (Dummy(true is var x9, x9)) // 2
        Dummy(x9);
}

using (Dummy(y10 is var x10, x10))
{   
    var y10 = 12;
    Dummy(y10);
}

//    using (Dummy(y11 is var x11, x11))
//    {   
//        let y11 = 12;
//        Dummy(y11);
//    }

using (Dummy(y12 is var x12, x12))
    var y12 = 12;

//    using (Dummy(y13 is var x13, x13))
//        let y13 = 12;
 
using (Dummy(1 is var x14, 
                2 is var x14, 
                x14))
{
    Dummy(x14);
}
";
            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
                compilation.VerifyDiagnostics(
                // (52,5): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //     var y12 = 12;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(52, 5),
                // (18,14): error CS0841: Cannot use local variable 'x6' before it is declared
                // using (Dummy(x6 && true is var x6))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(18, 14),
                // (23,9): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(23, 9),
                // (30,26): error CS0103: The name 'x8' does not exist in the current context
                // System.Console.WriteLine(x8);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(30, 26),
                // (35,30): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     using (Dummy(true is var x9, x9)) // 2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(35, 30),
                // (39,14): error CS0103: The name 'y10' does not exist in the current context
                // using (Dummy(y10 is var x10, x10))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(39, 14),
                // (51,14): error CS0103: The name 'y12' does not exist in the current context
                // using (Dummy(y12 is var x12, x12))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(51, 14),
                // (58,26): error CS0128: A local variable or function named 'x14' is already defined in this scope
                //                 2 is var x14, 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(58, 26),
                // (52,9): warning CS0219: The variable 'y12' is assigned but its value is never used
                //     var y12 = 12;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(52, 9)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").ToArray();
                Assert.Equal(2, x2Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").Single();
                var x4Ref = GetReferences(tree, "x4").ToArray();
                Assert.Equal(3, x4Ref.Length);
                VerifyNotAPatternLocal(model, x4Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl, x4Ref[1], x4Ref[2]);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(2, x6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl, x6Ref);

                var x7Decl = GetPatternDeclarations(tree, "x7").Single();
                var x7Ref = GetReferences(tree, "x7").ToArray();
                Assert.Equal(2, x7Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x7Decl, x7Ref[0]);
                VerifyNotAPatternLocal(model, x7Ref[1]);

                var x8Decl = GetPatternDeclarations(tree, "x8").Single();
                var x8Ref = GetReferences(tree, "x8").ToArray();
                Assert.Equal(3, x8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x8Decl, x8Ref[0], x8Ref[1]);
                VerifyNotInScope(model, x8Ref[2]);

                var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
                var x9Ref = GetReferences(tree, "x9").ToArray();
                Assert.Equal(2, x9Decl.Length);
                Assert.Equal(4, x9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

                var x10Decl = GetPatternDeclarations(tree, "x10").Single();
                var x10Ref = GetReferences(tree, "x10").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x10Decl, x10Ref);

                var y10Ref = GetReferences(tree, "y10").ToArray();
                Assert.Equal(2, y10Ref.Length);
                VerifyNotInScope(model, y10Ref[0]);
                VerifyNotAPatternLocal(model, y10Ref[1]);

                var y12Ref = GetReferences(tree, "y12").Single();
                VerifyNotInScope(model, y12Ref);

                var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
                var x14Ref = GetReferences(tree, "x14").ToArray();
                Assert.Equal(2, x14Decl.Length);
                Assert.Equal(2, x14Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x14Decl[0], x14Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x14Decl[1]);
            }

            {
                var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular9);

                compilation.VerifyDiagnostics(
                    // (15,26): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    // using (Dummy(true is var x4, x4))
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(15, 26),
                    // (18,14): error CS0841: Cannot use local variable 'x6' before it is declared
                    // using (Dummy(x6 && true is var x6))
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(18, 14),
                    // (23,9): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     var x7 = 12;
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(23, 9),
                    // (30,26): error CS0103: The name 'x8' does not exist in the current context
                    // System.Console.WriteLine(x8);
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(30, 26),
                    // (35,30): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     using (Dummy(true is var x9, x9)) // 2
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(35, 30),
                    // (39,14): error CS0103: The name 'y10' does not exist in the current context
                    // using (Dummy(y10 is var x10, x10))
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(39, 14),
                    // (51,14): error CS0103: The name 'y12' does not exist in the current context
                    // using (Dummy(y12 is var x12, x12))
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(51, 14),
                    // (52,5): error CS1023: Embedded statement cannot be a declaration or labeled statement
                    //     var y12 = 12;
                    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(52, 5),
                    // (52,9): warning CS0219: The variable 'y12' is assigned but its value is never used
                    //     var y12 = 12;
                    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(52, 9),
                    // (58,26): error CS0128: A local variable or function named 'x14' is already defined in this scope
                    //                 2 is var x14, 
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(58, 26)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").ToArray();
                Assert.Equal(2, x2Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").Single();
                var x4Ref = GetReferences(tree, "x4").ToArray();
                Assert.Equal(3, x4Ref.Length);
                VerifyNotAPatternLocal(model, x4Ref[0]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x4Decl, x4Ref[1], x4Ref[2]);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(2, x6Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x6Decl, x6Ref);

                var x7Decl = GetPatternDeclarations(tree, "x7").Single();
                var x7Ref = GetReferences(tree, "x7").ToArray();
                Assert.Equal(2, x7Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x7Decl, x7Ref[0]);
                VerifyNotAPatternLocal(model, x7Ref[1]);

                var x8Decl = GetPatternDeclarations(tree, "x8").Single();
                var x8Ref = GetReferences(tree, "x8").ToArray();
                Assert.Equal(3, x8Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x8Decl, x8Ref[0], x8Ref[1]);
                VerifyNotInScope(model, x8Ref[2]);

                var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
                var x9Ref = GetReferences(tree, "x9").ToArray();
                Assert.Equal(2, x9Decl.Length);
                Assert.Equal(4, x9Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
                VerifyModelForDeclarationOrVarSimplePattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

                var x10Decl = GetPatternDeclarations(tree, "x10").Single();
                var x10Ref = GetReferences(tree, "x10").Single();
                VerifyModelForDeclarationOrVarSimplePattern(model, x10Decl, x10Ref);

                var y10Ref = GetReferences(tree, "y10").ToArray();
                Assert.Equal(2, y10Ref.Length);
                VerifyNotInScope(model, y10Ref[0]);
                VerifyNotAPatternLocal(model, y10Ref[1]);

                var y12Ref = GetReferences(tree, "y12").Single();
                VerifyNotInScope(model, y12Ref);

                var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
                var x14Ref = GetReferences(tree, "x14").ToArray();
                Assert.Equal(2, x14Decl.Length);
                Assert.Equal(2, x14Ref.Length);
                VerifyModelForDeclarationOrVarSimplePattern(model, x14Decl[0], x14Ref);
                VerifyModelForDeclarationOrVarPatternDuplicateInSameScope(model, x14Decl[1]);
            }
        }

        [Fact]
        public void GlobalCode_Using_02()
        {
            var source =
@"
using (System.IDisposable d1 = Dummy(new C(""a""), (new C(""b"")) is var x1),
                            d2 = Dummy(new C(""c""), (new C(""d"")) is var x2))
{
    System.Console.WriteLine(d1);
    System.Console.WriteLine(x1);
    System.Console.WriteLine(d2);
    System.Console.WriteLine(x2);
}

using (Dummy(new C(""e""), (new C(""f"")) is var x1))
{
    System.Console.WriteLine(x1);
}

static System.IDisposable Dummy(System.IDisposable x, params object[] y) {return x;}

class C : System.IDisposable
{
    private readonly string _val;

    public C(string val)
    {
        _val = val;
    }

    public void Dispose()
    {
        System.Console.WriteLine(""Disposing {0}"", _val);
    }

    public override string ToString()
    {
        return _val;
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
            CompileAndVerify(compilation, expectedOutput:
@"a
b
c
d
Disposing c
Disposing a
f
Disposing e");
        }
    }
}
