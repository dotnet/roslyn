// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class InteractiveUsingTests : CSharpTestBase
    {
        [Fact]
        public void Using()
        {
            var sub = CreateSubmission("using System; typeof(String)");
            sub.VerifyDiagnostics();

            Assert.Equal(SpecialType.System_String, GetSpeculativeType(sub, "String").SpecialType);
        }

        [Fact]
        public void Alias()
        {
            var sub = CreateSubmission("using I = System.Int32; typeof(I)");
            sub.VerifyDiagnostics();

            Assert.Equal(SpecialType.System_Int32, GetSpeculativeType(sub, "I").SpecialType);
        }

        [Fact]
        public void UsingStatic()
        {
            var sub = CreateSubmission("using static System.Environment; NewLine");
            sub.VerifyDiagnostics();

            Assert.Equal(SymbolKind.Property, GetSpeculativeSymbol(sub, "NewLine").Kind);
        }

        [WorkItem(5450, "https://github.com/dotnet/roslyn/issues/5450")]
        [Fact]
        public void GlobalUsings()
        {
            var sub1 = CreateSubmission(
                "Combine(Environment.NewLine, Environment.NewLine)",
                options: TestOptions.DebugDll.WithUsings("System", "System.IO.Path"));
            sub1.VerifyDiagnostics();

            // No global usings specified - expect to reuse previous.
            var sub2 = CreateSubmission(
                "Combine(Environment.NewLine, Environment.NewLine)",
                previous: sub1);
            sub2.VerifyDiagnostics();

            // Global usings specified - expect to append to previous.
            var sub3 = CreateSubmission(
                "new StringBuilder().Append(Combine(Environment.NewLine, Environment.NewLine))",
                previous: sub2,
                options: TestOptions.DebugDll.WithUsings("System.Text"));
            sub3.VerifyDiagnostics();
        }

        [WorkItem(4811, "https://github.com/dotnet/roslyn/issues/4811")]
        [Fact]
        public void AliasCurrentSubmission()
        {
            const string source = @"
using T = Type;

class Type { }
";

            var sub = (Compilation)CreateSubmission(source);
            sub.VerifyDiagnostics();

            var typeSymbol = sub.ScriptClass.GetMember("Type");

            var tree = sub.SyntaxTrees.Single();
            var model = sub.GetSemanticModel(tree);
            var syntax = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single();

            var aliasSymbol = model.GetDeclaredSymbol(syntax);
            Assert.Equal(SymbolKind.Alias, aliasSymbol.Kind);
            Assert.Equal(typeSymbol, ((IAliasSymbol)aliasSymbol).Target);

            Assert.Equal(typeSymbol, model.GetSymbolInfo(syntax.Name).Symbol);

            Assert.Equal(typeSymbol, GetSpeculativeType(sub, "Type"));
            Assert.Equal(typeSymbol, GetSpeculativeType(sub, "T"));
        }

        [WorkItem(4811, "https://github.com/dotnet/roslyn/issues/4811")]
        [Fact]
        public void AliasPreviousSubmission()
        {
            var sub1 = CreateSubmission("class A { }");
            var sub2 = CreateSubmission("class B : A { }", previous: sub1);
            var sub3 = CreateSubmission("class C : B { }", previous: sub2);

            CreateSubmission("using A1 = A;", previous: sub3).VerifyDiagnostics();
            CreateSubmission("using B1 = B;", previous: sub3).VerifyDiagnostics();

            var sub4 = CreateSubmission("using C1 = C; typeof(C1)", previous: sub3);
            sub4.VerifyDiagnostics();

            var typeSymbol = ((Compilation)sub3).ScriptClass.GetMember("C");

            var tree = sub4.SyntaxTrees.Single();
            var model = sub4.GetSemanticModel(tree);
            var syntax = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single();

            var aliasSymbol = model.GetDeclaredSymbol(syntax);
            Assert.Equal(SymbolKind.Alias, aliasSymbol.Kind);
            Assert.Equal(typeSymbol, ((IAliasSymbol)aliasSymbol).Target);

            Assert.Equal(typeSymbol, model.GetSymbolInfo(syntax.Name).Symbol);

            Assert.Equal(typeSymbol, GetSpeculativeType(sub4, "C1"));
        }

        [Fact]
        public void AliasUnqualified()
        {
            const string source = @"
using I = Int32;
using System;
";
            var expectedDiagnostics = new[]
            {
                // (2,11): error CS0246: The type or namespace name 'Int32' could not be found (are you missing a using directive or an assembly reference?)
                // using I = Int32;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Int32").WithArguments("Int32").WithLocation(2, 11)
            };

            CreateCompilation(source).GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Hidden).Verify(expectedDiagnostics);
            CreateSubmission(source).GetDiagnostics().Verify(expectedDiagnostics);
        }

        [Fact]
        public void AliasUnqualified_GlobalUsing()
        {
            const string source = @"
using I = Int32;
";
            var expectedDiagnostics = new[]
            {
                // (2,11): error CS0246: The type or namespace name 'Int32' could not be found (are you missing a using directive or an assembly reference?)
                // using I = Int32;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Int32").WithArguments("Int32").WithLocation(2, 11)
            };

            var options = TestOptions.DebugDll.WithUsings("System");

            CreateCompilation(source, options: options).GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Hidden).Verify(expectedDiagnostics);
            CreateSubmission(source, options: options).GetDiagnostics().Verify(expectedDiagnostics);
        }

        [Fact]
        public void AliasOtherAlias()
        {
            const string source = @"
using I = System.Int32;
using J = I;
";
            var expectedDiagnostics = new[]
            {
                // (3,11): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // using J = I;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(3, 11)
            };

            CreateCompilation(source).GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Hidden).Verify(expectedDiagnostics);
            CreateSubmission(source).GetDiagnostics().Verify(expectedDiagnostics);
        }

        [Fact]
        public void AliasHiding()
        {
            var sub1 = CreateSubmission("using A = System.Int32; typeof(A)");
            Assert.Equal(SpecialType.System_Int32, GetSpeculativeType(sub1, "A").SpecialType);

            var sub2 = CreateSubmission("using A = System.Int16; typeof(A)", previous: sub1);
            Assert.Equal(SpecialType.System_Int16, GetSpeculativeType(sub2, "A").SpecialType);

            var sub3 = CreateSubmission("class A { }", previous: sub2);
            Assert.Equal(((Compilation)sub3).ScriptClass, GetSpeculativeType(sub3, "A").ContainingType);

            var sub4 = CreateSubmission("using A = System.Int64; typeof(A)", previous: sub3);
            Assert.Equal(SpecialType.System_Int64, GetSpeculativeType(sub4, "A").SpecialType);
        }

        [WorkItem(4811, "https://github.com/dotnet/roslyn/issues/4811")]
        [Fact]
        public void UsingStaticCurrentSubmission()
        {
            const string source = @"
using static Type;

class Type
{
    public static readonly int Field = 1;
}
";

            var sub = (Compilation)CreateSubmission(source);
            sub.VerifyDiagnostics();

            Assert.Equal(sub.ScriptClass.GetMember("Type"), GetSpeculativeSymbol(sub, "Field").ContainingType);
        }

        [WorkItem(4811, "https://github.com/dotnet/roslyn/issues/4811")]
        [Fact]
        public void UsingStaticPreviousSubmission()
        {
            var sub1 = CreateSubmission("class A { public static int AA; }");
            var sub2 = CreateSubmission("class B { public static int BB; }", previous: sub1);
            var sub3 = CreateSubmission("class C { public static int CC; }", previous: sub2);

            CreateSubmission("using static A;", previous: sub3).VerifyDiagnostics();
            CreateSubmission("using static B;", previous: sub3).VerifyDiagnostics();

            var sub4 = CreateSubmission("using static C;", previous: sub3);
            sub4.VerifyDiagnostics();

            var typeSymbol = ((Compilation)sub3).ScriptClass.GetMember("C");

            Assert.Equal(typeSymbol, GetSpeculativeSymbol(sub4, "CC").ContainingType);
        }

        [Fact]
        public void UsingStaticUnqualified()
        {
            const string source = @"
using static Path;
using System.IO;
";
            var expectedDiagnostics = new[]
            {
                // (2,14): error CS0246: The type or namespace name 'Path' could not be found (are you missing a using directive or an assembly reference?)
                // using static Path;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Path").WithArguments("Path").WithLocation(2, 14)
            };

            CreateCompilation(source).GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Hidden).Verify(expectedDiagnostics);
            CreateSubmission(source).GetDiagnostics().Verify(expectedDiagnostics);
        }

        [Fact]
        public void UsingStaticUnqualified_GlobalUsing()
        {
            const string source = @"
using static Path;
";
            var expectedDiagnostics = new[]
            {
                // (2,14): error CS0246: The type or namespace name 'Path' could not be found (are you missing a using directive or an assembly reference?)
                // using static Path;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Path").WithArguments("Path").WithLocation(2, 14)
            };

            var options = TestOptions.DebugDll.WithUsings("System");

            CreateCompilation(source, options: options).GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Hidden).Verify(expectedDiagnostics);
            CreateSubmission(source, options: options).GetDiagnostics().Verify(expectedDiagnostics);
        }

        [Fact]
        public void DuplicateUsing_SameSubmission()
        {
            CreateSubmission("using System; using System;").VerifyDiagnostics(
                // (1,21): warning CS0105: The using directive for 'System' appeared previously in this namespace
                // using System; using System;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "System").WithArguments("System").WithLocation(1, 21));
        }

        [Fact]
        public void DuplicateUsing_DifferentSubmissions()
        {
            CreateSubmission("using System;", previous: CreateSubmission("using System;")).VerifyDiagnostics();
        }

        [Fact]
        public void DuplicateGlobalUsing_SameSubmission()
        {
            CreateSubmission("typeof(String)", options: TestOptions.DebugDll.WithUsings("System", "System")).VerifyDiagnostics();
        }

        [Fact]
        public void DuplicateGlobalUsing_PreviousSubmission()
        {
            var options = TestOptions.DebugDll.WithUsings("System");

            var sub1 = CreateSubmission("typeof(String)", options: options);
            sub1.VerifyDiagnostics();

            var sub2 = CreateSubmission("typeof(String)", options: options, previous: sub1);
            sub2.VerifyDiagnostics();
        }

        [Fact]
        public void UsingsRebound()
        {
            const string libSourceTemplate = @"
namespace A
{{
    public class A{0} {{ }}
}}

namespace B
{{
    public class B{0} {{ }}
}}
";

            var lib1 = CreateCompilation(string.Format(libSourceTemplate, 1), assemblyName: "Lib1").EmitToImageReference();
            var lib2 = CreateCompilation(string.Format(libSourceTemplate, 2), assemblyName: "Lib2").EmitToImageReference();

            var options = TestOptions.DebugDll.WithUsings("B");

            var sub1 = CreateSubmission("using A; typeof(A1) == typeof(B1)", new[] { lib1 }, options);
            sub1.VerifyDiagnostics();

            var sub2 = CreateSubmission("typeof(A1) == typeof(B1) && typeof(A2) == typeof(B2)", new[] { lib1, lib2 }, options: options, previous: sub1);
            sub2.VerifyDiagnostics();
        }

        [WorkItem(5423, "https://github.com/dotnet/roslyn/issues/5423")]
        [Fact]
        private void UsingsFromLoadedScript()
        {
            const string scriptSource = @"
using static System.IO.Path;
using System.IO;
using F = System.IO.File;

class C { }
";

            const string submissionSource = @"
#load ""a.csx""

System.Type t;

GetTempPath(); // using static not exposed
t = typeof(File); // using not exposed
t = typeof(F); // using alias not exposed

t = typeof(C); // declaration exposed
";

            var resolver = TestSourceReferenceResolver.Create(new Dictionary<string, string>
            {
                { "a.csx", scriptSource }
            });

            var compilation = CreateSubmission(
                submissionSource,
                options: TestOptions.DebugDll.WithSourceReferenceResolver(resolver));

            compilation.VerifyDiagnostics(
                // (6,1): error CS0103: The name 'GetTempPath' does not exist in the current context
                // GetTempPath(); // using static not exposed
                Diagnostic(ErrorCode.ERR_NameNotInContext, "GetTempPath").WithArguments("GetTempPath").WithLocation(6, 1),
                // (7,12): error CS0246: The type or namespace name 'File' could not be found (are you missing a using directive or an assembly reference?)
                // t = typeof(File); // using not exposed
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "File").WithArguments("File").WithLocation(7, 12),
                // (8,12): error CS0246: The type or namespace name 'F' could not be found (are you missing a using directive or an assembly reference?)
                // t = typeof(F); // using alias not exposed
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "F").WithArguments("F").WithLocation(8, 12));
        }

        [WorkItem(5423, "https://github.com/dotnet/roslyn/issues/5423")]
        [Fact]
        private void UsingsToLoadedScript()
        {
            const string scriptSource = @"
using System.Collections.Generic;
using AL = System.Collections.ArrayList;
using static System.Math;

class D { }

System.Type t;

// Previous submission
GetCommandLineArgs(); // using static not exposed
t = typeof(StringBuilder); // using not exposed
t = typeof(P); // using alias not exposed
t = typeof(B); // declaration exposed

// Current submission
GetTempPath(); // using static not exposed
t = typeof(File); // using not exposed
t = typeof(F); // using alias not exposed
t = typeof(C); // declaration exposed

// Current file - all available
Sin(1);
t = typeof(List<int>);
t = typeof(AL);
t = typeof(D);
";

            const string previousSubmissionSource = @"
using static System.Environment;
using System.Text;
using P = System.IO.Path;

class B { }
";

            const string submissionSource = @"
#load ""a.csx""

using static System.IO.Path;
using System.IO;
using F = System.IO.File;

class C { }
";

            var resolver = TestSourceReferenceResolver.Create(new Dictionary<string, string>
            {
                { "a.csx", scriptSource }
            });

            var compilation = CreateSubmission(
                submissionSource,
                options: TestOptions.DebugDll.WithSourceReferenceResolver(resolver),
                previous: CreateSubmission(previousSubmissionSource));

            compilation.VerifyDiagnostics(
                // Previous submission

                // a.csx(11,1): error CS0103: The name 'GetCommandLineArgs' does not exist in the current context
                // GetCommandLineArgs(); // using static not exposed
                Diagnostic(ErrorCode.ERR_NameNotInContext, "GetCommandLineArgs").WithArguments("GetCommandLineArgs").WithLocation(11, 1),
                // a.csx(12,12): error CS0246: The type or namespace name 'StringBuilder' could not be found (are you missing a using directive or an assembly reference?)
                // t = typeof(StringBuilder); // using not exposed
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "StringBuilder").WithArguments("StringBuilder").WithLocation(12, 12),
                // a.csx(13,12): error CS0246: The type or namespace name 'P' could not be found (are you missing a using directive or an assembly reference?)
                // t = typeof(P); // using alias not exposed
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "P").WithArguments("P").WithLocation(13, 12),

                // Current submission

                // a.csx(17,1): error CS0103: The name 'GetTempPath' does not exist in the current context
                // GetTempPath(); // using static not exposed
                Diagnostic(ErrorCode.ERR_NameNotInContext, "GetTempPath").WithArguments("GetTempPath").WithLocation(17, 1),
                // a.csx(18,12): error CS0246: The type or namespace name 'File' could not be found (are you missing a using directive or an assembly reference?)
                // t = typeof(File); // using not exposed
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "File").WithArguments("File").WithLocation(18, 12),
                // a.csx(19,12): error CS0246: The type or namespace name 'F' could not be found (are you missing a using directive or an assembly reference?)
                // t = typeof(F); // using alias not exposed
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "F").WithArguments("F").WithLocation(19, 12));
        }

        [Fact]
        private void GlobalUsingsToLoadedScript()
        {
            const string scriptSource = @"
System.Type t;

GetTempPath(); // global using static exposed
t = typeof(File); // global using exposed
";

            const string submissionSource = @"
#load ""a.csx""
";

            var resolver = TestSourceReferenceResolver.Create(new Dictionary<string, string>
            {
                { "a.csx", scriptSource }
            });

            var compilation = CreateSubmission(
                submissionSource,
                options: TestOptions.DebugDll.WithSourceReferenceResolver(resolver).WithUsings("System.IO", "System.IO.Path"));

            compilation.VerifyDiagnostics();
        }

        [WorkItem(4811, "https://github.com/dotnet/roslyn/issues/4811")]
        [Fact]
        public void ConsumePreviousSubmissionUsings_Valid()
        {
            const string libSource = @"
namespace NOuter
{
    public class Test { }

    namespace NInner
    {
        public static class COuter
        {
            public static void M() { }

            public static class CInner
            {
                public static void N() { }
            }
        }
    }
}
";

            var lib = CreateCompilation(libSource).EmitToImageReference();
            var refs = new[] { lib };

            var submissions = new[]
            {
                "using NOuter;",
                "typeof(Test)",
                "using NI = NOuter.NInner;",
                "typeof(NI.COuter)",
                "using static NI.COuter;",
                "M()",
                "using static NI.COuter.CInner;",
                "N()",
            };

            CSharpCompilation prev = null;
            foreach (var submission in submissions)
            {
                var curr = CreateSubmission(submission, refs, previous: prev);
                curr.VerifyDiagnostics();
                prev = curr;
            }
        }

        [Fact]
        public void ConsumePreviousSubmissionUsings_Invalid()
        {
            const string libSource = @"
namespace NOuter
{
    public class COuter { }

    namespace NInner
    {
        public static class CInner
        {
        }
    }
}
";

            var lib = CreateCompilation(libSource).EmitToImageReference();
            var refs = new[] { lib };

            CreateSubmission("using NInner;", refs, previous: CreateSubmission("using NOuter;", refs)).VerifyDiagnostics(
                // (1,7): error CS0246: The type or namespace name 'NInner' could not be found (are you missing a using directive or an assembly reference?)
                // using NInner;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NInner").WithArguments("NInner").WithLocation(1, 7));

            CreateSubmission("using NI = NInner;", refs, previous: CreateSubmission("using NOuter;", refs)).VerifyDiagnostics(
                // (1,12): error CS0246: The type or namespace name 'NInner' could not be found (are you missing a using directive or an assembly reference?)
                // using NI = NInner;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NInner").WithArguments("NInner").WithLocation(1, 12));

            CreateSubmission("using static COuter;", refs, previous: CreateSubmission("using NOuter;", refs)).VerifyDiagnostics(
                // (1,14): error CS0246: The type or namespace name 'COuter' could not be found (are you missing a using directive or an assembly reference?)
                // using static COuter;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "COuter").WithArguments("COuter").WithLocation(1, 14));

            CreateSubmission("using static NInner.CInner;", refs, previous: CreateSubmission("using NOuter;", refs)).VerifyDiagnostics(
                // (1,14): error CS0246: The type or namespace name 'NInner' could not be found (are you missing a using directive or an assembly reference?)
                // using static NInner.CInner;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NInner").WithArguments("NInner").WithLocation(1, 14));
        }

        private static ISymbol GetSpeculativeSymbol(Compilation comp, string name)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            return model.GetSpeculativeSymbolInfo(
                tree.Length,
                SyntaxFactory.IdentifierName(name),
                SpeculativeBindingOption.BindAsExpression).Symbol;
        }

        private static ITypeSymbol GetSpeculativeType(Compilation comp, string name)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            return model.GetSpeculativeTypeInfo(
                tree.Length,
                SyntaxFactory.IdentifierName(name),
                SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
        }
    }
}
