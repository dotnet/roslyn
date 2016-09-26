// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,18): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // H.Dummy(2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,20): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //         (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 20),
                // (16,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(16, 13),
                // (16,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(16, 17),
                // (16,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(16, 21),
                // (16,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TypeVarNotFound,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,18): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // H.Dummy(2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,20): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //         (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 20),
                // (16,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(16, 13),
                // (16,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(16, 17),
                // (16,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(16, 21),
                // (16,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                VerifyModelForDeclarationPattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,15): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // if ((2 is int x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 15),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,24): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //             (42 is int x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 24),
                // (16,28): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x5'
                //     H.Dummy("52" is string x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("<invalid-global-code>", "x5").WithLocation(16, 28),
                // (24,12): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x6'
                //     string x6 = "6";
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x6").WithArguments("<invalid-global-code>", "x6").WithLocation(24, 12),
                // (30,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(30, 13),
                // (30,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(30, 17),
                // (30,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(30, 21),
                // (30,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(30, 25),
                // (30,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(30, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                VerifyModelForDeclarationPattern(model, x5Decl[1], x5Ref[1]);
            }

            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TypeVarNotFound,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,15): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // if ((2 is var x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 15),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,24): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //             (42 is var x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 24),
                // (21,25): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x5'
                //     H.Dummy("52" is var x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("<invalid-global-code>", "x5").WithLocation(21, 25),
                // (16,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(16, 13),
                // (16,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(16, 17),
                // (16,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(16, 21),
                // (16,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(16, 25),
                // (16,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(16, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,24): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // yield return (2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,33): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                      (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 33),
                // (16,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(16, 13),
                // (16,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(16, 17),
                // (16,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(16, 21),
                // (16,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                Assert.Equal("System.Int32", ((FieldSymbol)compilation.GetSemanticModel(tree).GetDeclaredSymbol(x1Decl)).Type.ToTestDisplayString());

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TypeVarNotFound,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,24): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // yield return (2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,33): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                      (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 33),
                // (16,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(16, 13),
                // (16,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(16, 17),
                // (16,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(16, 21),
                // (16,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,18): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // return (2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,27): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 27),
                // (16,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(16, 13),
                // (16,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(16, 17),
                // (16,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(16, 21),
                // (16,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TypeVarNotFound,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,18): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // return (2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,27): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 27),
                // (16,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(16, 13),
                // (16,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(16, 17),
                // (16,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(16, 21),
                // (16,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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

class H
{
    public static System.Exception Dummy(params object[] x) {return null;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,24): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // throw H.Dummy(2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,26): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //               (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 26),
                // (16,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(16, 13),
                // (16,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(16, 17),
                // (16,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(16, 21),
                // (16,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

class H
{
    public static System.Exception Dummy(params object[] x) {return null;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                Assert.Equal("System.Int32", ((FieldSymbol)compilation.GetSemanticModel(tree).GetDeclaredSymbol(x1Decl)).Type.ToTestDisplayString());

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TypeVarNotFound,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,24): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // throw H.Dummy(2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,26): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //               (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 26),
                // (16,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(16, 13),
                // (16,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(16, 17),
                // (16,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(16, 21),
                // (16,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                VerifyModelForDeclarationPattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,19): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // switch ((2 is int x2)) {default: break;}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 19),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,28): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                 (42 is int x4))) {default: break;}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 28),
                // (17,28): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x5'
                //     H.Dummy("52" is string x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("<invalid-global-code>", "x5").WithLocation(17, 28),
                // (25,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(25, 13),
                // (25,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(25, 17),
                // (25,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(25, 21),
                // (25,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(25, 25),
                // (25,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(25, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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
class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                VerifyModelForDeclarationPattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,19): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // switch ((2 is var x2)) {default: break;}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 19),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,28): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                 (42 is var x4))) {default: break;}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 28),
                // (17,25): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x5'
                //     H.Dummy("52" is var x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("<invalid-global-code>", "x5").WithLocation(17, 25),
                // (6,15): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                // switch ((2 is var x2)) {default: break;}
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(6, 15),
                // (8,15): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                // switch ((3 is var x3)) {default: break;}
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(8, 15),
                // (11,24): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                // switch (H.Dummy((41 is var x4),
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(11, 24),
                // (12,24): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //                 (42 is var x4))) {default: break;}
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(12, 24),
                // (14,16): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                // switch ((51 is var x5)) 
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(14, 16),
                // (17,21): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //     H.Dummy("52" is var x5);
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(17, 21),
                // (2,15): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                // switch ((1 is var x1)) {default: break;}
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(2, 15),
                // (25,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(25, 13),
                // (25,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(25, 17),
                // (25,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(25, 21),
                // (25,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(25, 25),
                // (25,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(25, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,18): error CS0102: The type 'Script' already contains a definition for 'x2'
                // while ((2 is int x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,27): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                (42 is int x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 27),
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
                VerifyModelForDeclarationPattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,18): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // while ((2 is int x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,27): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                (42 is int x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 27),
                // (16,28): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x5'
                //     H.Dummy("52" is string x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("<invalid-global-code>", "x5").WithLocation(16, 28),
                // (23,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                // (23,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(23, 17),
                // (23,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(23, 21),
                // (23,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(23, 25),
                // (23,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(23, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,18): error CS0102: The type 'Script' already contains a definition for 'x2'
                // while ((2 is var x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,27): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                (42 is var x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 27),
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
                VerifyModelForDeclarationPattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TypeVarNotFound,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,18): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // while ((2 is var x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 18),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,27): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                (42 is var x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 27),
                // (16,25): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x5'
                //     H.Dummy("52" is var x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("<invalid-global-code>", "x5").WithLocation(16, 25),
                // (23,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                // (23,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(23, 17),
                // (23,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(23, 21),
                // (23,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(23, 25),
                // (23,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(23, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
            }
        }

        [Fact]
        public void GlobalCode_WhileStatement_03()
        {
            string source =
@"
System.Console.WriteLine(x1);
while ((1 is var x1)) 
{
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
    public static bool Dummy(params object[] x) {return false;}
}
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
        }

        [Fact]
        public void GlobalCode_WhileStatement_04()
        {
            string source =
@"
bool x0 = true;
System.Console.WriteLine(x1);
while (x0 && (1 is var x1)) 
    H.Dummy((""11"" is var x1) && (x0 = false), x1);
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,24): error CS0102: The type 'Script' already contains a definition for 'x2'
                // do {} while ((2 is int x2));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,33): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                      (42 is int x4)));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 33),
                // (24,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(24, 17),
                // (24,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(24, 21),
                // (24,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(24, 25)
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
                VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref[0]);
                VerifyModelForDeclarationField(model, x5Decl[1], x5Ref[1], x5Ref[2]);
            }

            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,24): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // do {} while ((2 is int x2));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,33): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                      (42 is int x4)));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 33),
                // (19,19): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x5'
                // while ((51 is int x5));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("<invalid-global-code>", "x5").WithLocation(19, 19),
                // (24,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(24, 13),
                // (24,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(24, 17),
                // (24,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(24, 21),
                // (24,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(24, 25),
                // (24,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(24, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (6,24): error CS0102: The type 'Script' already contains a definition for 'x2'
                // do {} while ((2 is var x2));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,33): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                      (42 is var x4)));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 33),
                // (24,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(24, 17),
                // (24,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(24, 21),
                // (24,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(24, 25)
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
                VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref[0]);
                VerifyModelForDeclarationField(model, x5Decl[1], x5Ref[1], x5Ref[2]);
            }

            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TypeVarNotFound,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,24): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // do {} while ((2 is var x2));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,33): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                      (42 is var x4)));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 33),
                // (19,19): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x5'
                // while ((51 is var x5));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("<invalid-global-code>", "x5").WithLocation(19, 19),
                // (24,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(24, 13),
                // (24,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(24, 17),
                // (24,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(24, 21),
                // (24,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(24, 25),
                // (24,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(24, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
            }
        }

        [Fact]
        public void GlobalCode_DoStatement_03()
        {
            string source =
@"
System.Console.WriteLine(x1);
do
{
    H.Dummy(""11"" is var x1);
    System.Console.WriteLine(x1);
}
while ((1 is var x1) && false);
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[1]);
            VerifyModelForDeclarationField(model, x1Decl[1], x1Ref[0], x1Ref[2]);
        }

        [Fact]
        public void GlobalCode_DoStatement_04()
        {
            string source =
@"
System.Console.WriteLine(x1);
do 
    H.Dummy((""11"" is var x1), x1);
while ((1 is var x1) && false);
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[1]);
            VerifyModelForDeclarationField(model, x1Decl[1], x1Ref[0], x1Ref[2]);
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

class H
{
    public static object Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                VerifyModelForDeclarationPattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,24): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // lock (H.Dummy(2 is int x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,26): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //               (42 is int x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 26),
                // (16,28): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x5'
                //     H.Dummy("52" is string x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("<invalid-global-code>", "x5").WithLocation(16, 28),
                // (23,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                // (23,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(23, 17),
                // (23,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(23, 21),
                // (23,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(23, 25),
                // (23,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(23, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

class H
{
    public static object Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                VerifyModelForDeclarationPattern(model, x5Decl[1], x5Ref[0]);
            }

            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TypeVarNotFound,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,24): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // lock (H.Dummy(2 is var x2)) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 24),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,26): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //               (42 is var x4))) {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 26),
                // (16,25): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x5'
                //     H.Dummy("52" is var x5);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x5").WithArguments("<invalid-global-code>", "x5").WithLocation(16, 25),
                // (23,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                // (23,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(23, 17),
                // (23,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(23, 21),
                // (23,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(23, 25),
                // (23,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(23, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/13716")]
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                                  options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (2,17): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                // (bool a, int b) = ((1 is int x1), 1);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(2, 17),
                // (2,17): error CS1525: Invalid expression term '='
                // (bool a, int b) = ((1 is int x1), 1);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(2, 17),
                // (6,17): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                // (bool c, int d) = ((2 is int x2), 2);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(6, 17),
                // (6,17): error CS1525: Invalid expression term '='
                // (bool c, int d) = ((2 is int x2), 2);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(6, 17),
                // (8,17): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                // (bool e, int f) = ((3 is int x3), 3);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(8, 17),
                // (8,17): error CS1525: Invalid expression term '='
                // (bool e, int f) = ((3 is int x3), 3);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(8, 17),
                // (11,18): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                // (bool g, bool h) = ((41 is int x4),
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(11, 18),
                // (11,18): error CS1525: Invalid expression term '='
                // (bool g, bool h) = ((41 is int x4),
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(11, 18),
                // (14,20): error CS1519: Invalid token '=' in class, struct, or interface member declaration
                // (bool x5, bool x6) = ((5 is int x5),
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(14, 20),
                // (14,20): error CS1525: Invalid expression term '='
                // (bool x5, bool x6) = ((5 is int x5),
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(14, 20),
                // (6,30): error CS0102: The type 'Script' already contains a definition for 'x2'
                // (bool c, int d) = ((2 is int x2), 2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("Script", "x2").WithLocation(6, 30),
                // (9,8): error CS0102: The type 'Script' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("Script", "x3").WithLocation(9, 8),
                // (12,32): error CS0102: The type 'Script' already contains a definition for 'x4'
                //                     (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("Script", "x4").WithLocation(12, 32),
                // (19,17): error CS0229: Ambiguity between 'x2' and 'x2'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x2").WithArguments("x2", "x2").WithLocation(19, 17),
                // (19,21): error CS0229: Ambiguity between 'x3' and 'x3'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x3").WithArguments("x3", "x3").WithLocation(19, 21),
                // (19,25): error CS0229: Ambiguity between 'x4' and 'x4'
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_AmbigMember, "x4").WithArguments("x4", "x4").WithLocation(19, 25)
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
                VerifyModelForDeclarationField(model, x5Decl, x5Ref[1]);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").ToArray();
                Assert.Equal(2, x6Ref.Length);
                VerifyModelForDeclarationField(model, x6Decl, x6Ref[1]);
            }

            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,30): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // (bool c, int d) = ((2 is int x2), 2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 30),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,32): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                     (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 32),
                // (19,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(19, 13),
                // (19,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(19, 17),
                // (19,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(19, 21),
                // (19,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(19, 25),
                // (19,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(19, 29),
                // (19,33): error CS0103: The name 'x6' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x6").WithArguments("x6").WithLocation(19, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,21): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // b: H.Dummy(2 is int x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 21),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,23): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //            (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 23),
                // (16,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(16, 13),
                // (16,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(16, 17),
                // (16,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(16, 21),
                // (16,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TypeVarNotFound,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,21): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // b: H.Dummy(2 is var x2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 21),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,23): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //            (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 23),
                // (16,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(16, 13),
                // (16,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(16, 17),
                // (16,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(16, 21),
                // (16,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(16, 25)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool b = (1 is int x1);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x1").WithLocation(3, 16),
                // (7,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool d = (2 is int x2);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x2").WithLocation(7, 16),
                // (9,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool f = (3 is int x3);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x3").WithLocation(9, 16),
                // (12,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool h = H.Dummy((41 is int x4),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(12, 25),
                // (13,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //                  (42 is int x4));
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(13, 25),
                // (13,29): error CS0128: A local variable named 'x4' is already defined in this scope
                //                  (42 is int x4));
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 29),
                // (15,17): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool x5 = (5 is int x5);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(15, 17),
                // (20,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(20, 13),
                // (20,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(20, 17),
                // (20,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(20, 21),
                // (20,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(20, 25),
                // (20,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(20, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationPattern(model, x1Decl);
                VerifyModelNotSupported(model, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationPattern(model, x2Decl);
                VerifyModelNotSupported(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationPattern(model, x3Decl);
                VerifyModelNotSupported(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationPattern(model, x4Decl[0]);
                VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyModelNotSupported(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Ref.Length);
                VerifyModelForDeclarationPattern(model, x5Decl);
                VerifyModelNotSupported(model, x5Ref);
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool b = (1 is var x1);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x1").WithLocation(3, 16),
                // (7,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool d = (2 is var x2);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x2").WithLocation(7, 16),
                // (9,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool f = (3 is var x3);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x3").WithLocation(9, 16),
                // (12,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool h = H.Dummy((41 is var x4),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x4").WithLocation(12, 25),
                // (13,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //                  (42 is var x4));
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x4").WithLocation(13, 25),
                // (13,29): error CS0128: A local variable named 'x4' is already defined in this scope
                //                  (42 is var x4));
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 29),
                // (15,17): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool x5 = (5 is var x5);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x5").WithLocation(15, 17),
                // (20,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(20, 13),
                // (20,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(20, 17),
                // (20,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(20, 21),
                // (20,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(20, 25),
                // (20,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(20, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationPattern(model, x1Decl);
                VerifyModelNotSupported(model, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationPattern(model, x2Decl);
                VerifyModelNotSupported(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationPattern(model, x3Decl);
                VerifyModelNotSupported(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationPattern(model, x4Decl[0]);
                VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyModelNotSupported(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").ToArray();
                Assert.Equal(2, x5Ref.Length);
                VerifyModelForDeclarationPattern(model, x5Decl);
                VerifyModelNotSupported(model, x5Ref);
            }
        }

        [Fact]
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,30): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // (bool c, int d) = ((2 is int x2), 2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 30),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,32): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                     (42 is int x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 32),
                // (19,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(19, 13),
                // (19,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(19, 17),
                // (19,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(19, 21),
                // (19,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(19, 25),
                // (19,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(19, 29),
                // (19,33): error CS0103: The name 'x6' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x6").WithArguments("x6").WithLocation(19, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TypeVarNotFound,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (6,30): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x2'
                // (bool c, int d) = ((2 is var x2), 2);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x2").WithArguments("<invalid-global-code>", "x2").WithLocation(6, 30),
                // (9,8): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x3'
                // object x3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x3").WithArguments("<invalid-global-code>", "x3").WithLocation(9, 8),
                // (12,32): error CS0102: The type '<invalid-global-code>' already contains a definition for 'x4'
                //                     (42 is var x4));
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "x4").WithArguments("<invalid-global-code>", "x4").WithLocation(12, 32),
                // (19,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(19, 13),
                // (19,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(19, 17),
                // (19,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(19, 21),
                // (19,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(19, 25),
                // (19,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(19, 29),
                // (19,33): error CS0103: The name 'x6' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x6").WithArguments("x6").WithLocation(19, 33)
                    );

                var tree = compilation.SyntaxTrees.Single();
                Assert.Empty(GetPatternDeclarations(tree));
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

            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool b = (1 is int x1);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x1").WithLocation(3, 16),
                // (7,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool d = (2 is int x2);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x2").WithLocation(7, 16),
                // (9,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool f = (3 is int x3);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x3").WithLocation(9, 16),
                // (12,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool h = H.Dummy((41 is int x4),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(12, 25),
                // (13,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //                  (42 is int x4));
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(13, 25),
                // (13,29): error CS0128: A local variable named 'x4' is already defined in this scope
                //                  (42 is int x4));
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 29),
                // (16,17): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //           (5 is int x5);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(16, 17),
                // (18,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool i = (5 is int x6),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(18, 16),
                // (23,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                // (23,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(23, 17),
                // (23,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(23, 21),
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
                VerifyModelForDeclarationPattern(model, x1Decl);
                VerifyModelNotSupported(model, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationPattern(model, x2Decl);
                VerifyModelNotSupported(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationPattern(model, x3Decl);
                VerifyModelNotSupported(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationPattern(model, x4Decl[0]);
                VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyModelNotSupported(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationPattern(model, x5Decl);
                VerifyModelNotSupported(model, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationPattern(model, x6Decl);
                VerifyModelNotSupported(model, x6Ref);
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool b = (1 is var x1);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x1").WithLocation(3, 16),
                // (7,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool d = (2 is var x2);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x2").WithLocation(7, 16),
                // (9,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool f = (3 is var x3);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x3").WithLocation(9, 16),
                // (12,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool h = H.Dummy((41 is var x4),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x4").WithLocation(12, 25),
                // (13,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //                  (42 is var x4));
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x4").WithLocation(13, 25),
                // (13,29): error CS0128: A local variable named 'x4' is already defined in this scope
                //                  (42 is var x4));
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 29),
                // (16,17): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //           (5 is var x5);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x5").WithLocation(16, 17),
                // (18,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool i = (5 is var x6),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x6").WithLocation(18, 16),
                // (23,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                // (23,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(23, 17),
                // (23,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(23, 21),
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
                VerifyModelForDeclarationPattern(model, x1Decl);
                VerifyModelNotSupported(model, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationPattern(model, x2Decl);
                VerifyModelNotSupported(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationPattern(model, x3Decl);
                VerifyModelNotSupported(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationPattern(model, x4Decl[0]);
                VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyModelNotSupported(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationPattern(model, x5Decl);
                VerifyModelNotSupported(model, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationPattern(model, x6Decl);
                VerifyModelNotSupported(model, x6Ref);
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool b { get; } = (1 is int x1);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x1").WithLocation(3, 25),
                // (7,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool d { get; } = (2 is int x2);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x2").WithLocation(7, 25),
                // (9,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool f { get; } = (3 is int x3);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x3").WithLocation(9, 25),
                // (12,34): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool h { get; } = H.Dummy((41 is int x4),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(12, 34),
                // (13,34): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //                           (42 is int x4));
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(13, 34),
                // (13,38): error CS0128: A local variable named 'x4' is already defined in this scope
                //                           (42 is int x4));
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 38),
                // (16,17): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //           (5 is int x5);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(16, 17),
                // (20,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(20, 13),
                // (20,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(20, 17),
                // (20,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(20, 21),
                // (20,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(20, 25),
                // (20,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(20, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationPattern(model, x1Decl);
                VerifyModelNotSupported(model, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationPattern(model, x2Decl);
                VerifyModelNotSupported(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationPattern(model, x3Decl);
                VerifyModelNotSupported(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationPattern(model, x4Decl[0]);
                VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyModelNotSupported(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationPattern(model, x5Decl);
                VerifyModelNotSupported(model, x5Ref);
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool b { get; } = (1 is var x1);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x1").WithLocation(3, 25),
                // (7,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool d { get; } = (2 is var x2);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x2").WithLocation(7, 25),
                // (9,25): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool f { get; } = (3 is var x3);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x3").WithLocation(9, 25),
                // (12,34): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // bool h { get; } = H.Dummy((41 is var x4),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x4").WithLocation(12, 34),
                // (13,34): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //                           (42 is var x4));
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x4").WithLocation(13, 34),
                // (13,38): error CS0128: A local variable named 'x4' is already defined in this scope
                //                           (42 is var x4));
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 38),
                // (16,17): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //           (5 is var x5);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x5").WithLocation(16, 17),
                // (20,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(20, 13),
                // (20,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(20, 17),
                // (20,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(20, 21),
                // (20,25): error CS0103: The name 'x4' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(20, 25),
                // (20,29): error CS0103: The name 'x5' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(20, 29)
                    );

                var tree = compilation.SyntaxTrees.Single();
                var model = compilation.GetSemanticModel(tree);

                var x1Decl = GetPatternDeclarations(tree, "x1").Single();
                var x1Ref = GetReferences(tree, "x1").ToArray();
                Assert.Equal(2, x1Ref.Length);
                VerifyModelForDeclarationPattern(model, x1Decl);
                VerifyModelNotSupported(model, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationPattern(model, x2Decl);
                VerifyModelNotSupported(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationPattern(model, x3Decl);
                VerifyModelNotSupported(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationPattern(model, x4Decl[0]);
                VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyModelNotSupported(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationPattern(model, x5Decl);
                VerifyModelNotSupported(model, x5Ref);
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,38): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // event System.Action b = H.Dummy(1 is int x1);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x1").WithLocation(3, 38),
                // (7,38): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // event System.Action d = H.Dummy(2 is int x2);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x2").WithLocation(7, 38),
                // (9,38): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // event System.Action f = H.Dummy(3 is int x3);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x3").WithLocation(9, 38),
                // (12,40): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // event System.Action h = H.Dummy((41 is int x4),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(12, 40),
                // (13,32): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //                         (42 is int x4));
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(13, 32),
                // (13,36): error CS0128: A local variable named 'x4' is already defined in this scope
                //                         (42 is int x4));
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 36),
                // (16,24): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //           H.Dummy(5 is int x5);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(16, 24),
                // (18,38): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // event System.Action i = H.Dummy(5 is int x6),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(18, 38),
                // (23,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                // (23,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(23, 17),
                // (23,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(23, 21),
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
                VerifyModelForDeclarationPattern(model, x1Decl);
                VerifyModelNotSupported(model, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationPattern(model, x2Decl);
                VerifyModelNotSupported(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationPattern(model, x3Decl);
                VerifyModelNotSupported(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationPattern(model, x4Decl[0]);
                VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyModelNotSupported(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationPattern(model, x5Decl);
                VerifyModelNotSupported(model, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationPattern(model, x6Decl);
                VerifyModelNotSupported(model, x6Ref);
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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,38): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // event System.Action b = H.Dummy(1 is var x1);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x1").WithLocation(3, 38),
                // (7,38): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // event System.Action d = H.Dummy(2 is var x2);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x2").WithLocation(7, 38),
                // (9,38): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // event System.Action f = H.Dummy(3 is var x3);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x3").WithLocation(9, 38),
                // (12,40): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // event System.Action h = H.Dummy((41 is var x4),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x4").WithLocation(12, 40),
                // (13,32): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //                         (42 is var x4));
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x4").WithLocation(13, 32),
                // (13,36): error CS0128: A local variable named 'x4' is already defined in this scope
                //                         (42 is var x4));
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(13, 36),
                // (16,24): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //           H.Dummy(5 is var x5);
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x5").WithLocation(16, 24),
                // (18,38): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                // event System.Action i = H.Dummy(5 is var x6),
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var x6").WithLocation(18, 38),
                // (23,13): error CS0103: The name 'x1' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(23, 13),
                // (23,17): error CS0103: The name 'x2' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(23, 17),
                // (23,21): error CS0103: The name 'x3' does not exist in the current context
                //     H.Dummy(x1, x2, x3, x4, x5, x6);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(23, 21),
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
                VerifyModelForDeclarationPattern(model, x1Decl);
                VerifyModelNotSupported(model, x1Ref);

                var x2Decl = GetPatternDeclarations(tree, "x2").Single();
                var x2Ref = GetReferences(tree, "x2").Single();
                VerifyModelForDeclarationPattern(model, x2Decl);
                VerifyModelNotSupported(model, x2Ref);

                var x3Decl = GetPatternDeclarations(tree, "x3").Single();
                var x3Ref = GetReferences(tree, "x3").Single();
                VerifyModelForDeclarationPattern(model, x3Decl);
                VerifyModelNotSupported(model, x3Ref);

                var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
                var x4Ref = GetReferences(tree, "x4").Single();
                Assert.Equal(2, x4Decl.Length);
                VerifyModelForDeclarationPattern(model, x4Decl[0]);
                VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl[1]);
                VerifyModelNotSupported(model, x4Ref);

                var x5Decl = GetPatternDeclarations(tree, "x5").Single();
                var x5Ref = GetReferences(tree, "x5").Single();
                VerifyModelForDeclarationPattern(model, x5Decl);
                VerifyModelNotSupported(model, x5Ref);

                var x6Decl = GetPatternDeclarations(tree, "x6").Single();
                var x6Ref = GetReferences(tree, "x6").Single();
                VerifyModelForDeclarationPattern(model, x6Decl);
                VerifyModelNotSupported(model, x6Ref);
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (3,10): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                // bool a, b("5948" is var x1);
                Diagnostic(ErrorCode.ERR_BadVarDecl, @"(""5948"" is var x1)").WithLocation(3, 10),
                // (3,10): error CS1003: Syntax error, '[' expected
                // bool a, b("5948" is var x1);
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "(").WithLocation(3, 10),
                // (3,28): error CS1003: Syntax error, ']' expected
                // bool a, b("5948" is var x1);
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";").WithLocation(3, 28)
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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,10): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                // bool a, b("5948" is var x1);
                Diagnostic(ErrorCode.ERR_BadVarDecl, @"(""5948"" is var x1)").WithLocation(3, 10),
                // (3,10): error CS1003: Syntax error, '[' expected
                // bool a, b("5948" is var x1);
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "(").WithLocation(3, 10),
                // (3,28): error CS1003: Syntax error, ']' expected
                // bool a, b("5948" is var x1);
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";").WithLocation(3, 28),
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
                VerifyModelNotSupported(model, x1Decl, x1Ref);
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

class H
{
    public static bool Dummy(params object[] x) {return true;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (3,10): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                // bool a, b((1 is var x1));
                Diagnostic(ErrorCode.ERR_BadVarDecl, "((1 is var x1))").WithLocation(3, 10),
                // (3,10): error CS1003: Syntax error, '[' expected
                // bool a, b((1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "(").WithLocation(3, 10),
                // (3,25): error CS1003: Syntax error, ']' expected
                // bool a, b((1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";").WithLocation(3, 25),
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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,10): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                // bool a, b((1 is var x1));
                Diagnostic(ErrorCode.ERR_BadVarDecl, "((1 is var x1))").WithLocation(3, 10),
                // (3,10): error CS1003: Syntax error, '[' expected
                // bool a, b((1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "(").WithLocation(3, 10),
                // (3,25): error CS1003: Syntax error, ']' expected
                // bool a, b((1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";").WithLocation(3, 25),
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
                VerifyModelNotSupported(model, x1Decl, x1Ref);
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

class H
{
    public static bool Dummy(params object[] x) {return false;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

                compilation.VerifyDiagnostics(
                // (3,25): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                // event System.Action a, b(H.Dummy(1 is var x1));
                Diagnostic(ErrorCode.ERR_BadVarDecl, "(H.Dummy(1 is var x1))").WithLocation(3, 25),
                // (3,25): error CS1003: Syntax error, '[' expected
                // event System.Action a, b(H.Dummy(1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "(").WithLocation(3, 25),
                // (3,47): error CS1003: Syntax error, ']' expected
                // event System.Action a, b(H.Dummy(1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";").WithLocation(3, 47)
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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,25): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                // event System.Action a, b(H.Dummy(1 is var x1));
                Diagnostic(ErrorCode.ERR_BadVarDecl, "(H.Dummy(1 is var x1))").WithLocation(3, 25),
                // (3,25): error CS1003: Syntax error, '[' expected
                // event System.Action a, b(H.Dummy(1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "(").WithLocation(3, 25),
                // (3,47): error CS1003: Syntax error, ']' expected
                // event System.Action a, b(H.Dummy(1 is var x1));
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";").WithLocation(3, 47),
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
                VerifyModelNotSupported(model, x1Decl, x1Ref);
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

class H
{
    public static int Dummy(params object[] x) {return 0;}
}
";
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"),
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
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);
                int[] exclude = new int[] { (int)ErrorCode.ERR_EOFExpected,
                                        (int)ErrorCode.ERR_CloseParenExpected,
                                        (int)ErrorCode.ERR_SemicolonExpected,
                                        (int)ErrorCode.ERR_TypeExpected,
                                        (int)ErrorCode.ERR_NamespaceUnexpected,
                                        (int)ErrorCode.ERR_TupleTooFewElements
                                      };

                compilation.GetDiagnostics().Where(d => !exclude.Contains(d.Code)).Verify(
                // (3,18): error CS1642: Fixed size buffer fields may only be members of structs
                // fixed bool a[2], b[H.Dummy(1 is var x1)];
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "b").WithLocation(3, 18),
                // (3,20): error CS0133: The expression being assigned to '<invalid-global-code>.b' must be constant
                // fixed bool a[2], b[H.Dummy(1 is var x1)];
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "H.Dummy(1 is var x1)").WithArguments("<invalid-global-code>.b").WithLocation(3, 20),
                // (3,12): error CS1642: Fixed size buffer fields may only be members of structs
                // fixed bool a[2], b[H.Dummy(1 is var x1)];
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "a").WithLocation(3, 12),
                // (3,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // fixed bool a[2], b[H.Dummy(1 is var x1)];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "a[2]").WithLocation(3, 12),
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
                VerifyModelNotSupported(model, x1Decl, x1Ref);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);


            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            Assert.Null(model.GetAliasInfo(x1Decl.Type));
        }

        [Fact]
        public void GlobalCode_AliasInfo_02()
        {
            string source =
@"
using var = System.Int32;

H.Dummy(1 is var x1);

class H
{
    public static void Dummy(params object[] x) {}
}
";

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            Assert.Equal("var=System.Int32", model.GetAliasInfo(x1Decl.Type).ToTestDisplayString());
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            Assert.Equal("a=System.Int32", model.GetAliasInfo(x1Decl.Type).ToTestDisplayString());
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

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            Assert.Null(model.GetAliasInfo(x1Decl.Type));
        }

    }
}
