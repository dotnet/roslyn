// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    
    public partial class SuppressMessageAttributeTests
    {
        [Fact]
        public void TestLocalSuppressionOnType()
        {
            VerifyCSharp(@"
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"")]
public class C
{
}
public class C1
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") },
                GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "C1"));
        }

        [Fact]
        public void TestLocalSuppressionOnMember()
        {
            VerifyCSharp(@"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"")]
    public void Foo() {}
    public void Foo1() {}
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("Foo") },
                GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "Foo1"));
        }

        [Fact]
        public void TestGlobalSuppressionOnNamespaces()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Namespace"", Target=""N"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Namespace"", Target=""N.N1"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Namespace"", Target=""N4"")]

namespace N
{
    namespace N1
    {
        namespace N2.N3
        {
        }
    }
}

namespace N4
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("N") },
                GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "N2"),
                GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "N3"));
        }

        [Fact]
        public void TestGlobalSuppressionOnTypes()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""Ef"")]
[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""C"")]
[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""Ele"")]

public class E
{
}
public interface Ef
{
}
public struct Egg
{
}
public delegate void Ele<T1,T2>(T1 x, T2 y);
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("E") },
                new[] { GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "Egg"),
                        GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "Ele")});
        }

        [Fact]
        public void TestGlobalSuppressionOnTypesNested()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""type"", Target=""C.A1"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""type"", Target=""C+A2"")]
[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""member"", Target=""C+A3"")]
[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""member"", Target=""C.A4"")]

public class C
{
    public class A1 { }
    public class A2 { }
    public class A3 { }
    public delegate void A4();
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("A") },
                new[] { GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "A1"),
                        GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "A3"),
                        GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "A4")});
        }

        [Fact]
        public void TestGlobalSuppressionOnMembers()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Member"", Target=""C.#M1"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Member"", Target=""C.#M3`1()"")]

public class C
{
    int M1;
    public void M2() {}
    public static void M3<T>() {}
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("M") },
                new[] { GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "M2")});
        }

        [Fact]
        public void TestMockSyntaxDiagnosticAnalyzerCSharp()
        {
            VerifyCSharp("// Comment",
                new[] { new WarningOnSingleLineCommentAnalyzer() },
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "// Comment"));
        }

        [Fact]
        public void TestMockSyntaxDiagnosticAnalyzerBasic()
        {
            VerifyBasic("' Comment",
                new[] { new WarningOnSingleLineCommentAnalyzer() },
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "' Comment"));
        }

        [Fact]
        public void TestSuppressSyntaxDiagnosticsInModuleCSharp()
        {
            VerifyCSharp(@"
// 0
[module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Syntax"")]
// 1
public class C
{
    // 2
    public void Foo() // 3
    {
        // 4
    }
}
// 5
",
                new[] { new WarningOnSingleLineCommentAnalyzer() });
        }

        [Fact]
        public void TestSuppressSyntaxDiagnosticsInModuleBasic()
        {
            VerifyBasic(@"
' 0
<module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Syntax"")>
' 1
Public Class C
    ' 2
    Public Sub Foo() ' 3
        ' 4
    End Sub
End Class
' 5
",
                new[] { new WarningOnSingleLineCommentAnalyzer() });
        }

        [Fact]
        public void TestSuppressSyntaxDiagnosticsInModule()
        {
            VerifyBasic(@"
<assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Syntax"", Scope=""type"", Target=""M.C"")>

Module M
    Class C
    End Class
End Module
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") }, 
                GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "C"));
        }

        [Fact]
        public void TestSuppressSyntaxDiagnosticsInNamespaceDeclarationCSharp()
        {
            VerifyCSharp(@"
// 0
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Syntax"", Scope=""namespace"", Target=""A"")]
// 1
namespace A
// 2
{
    // 3
    namespace B
    {
    }
}
",
                new[] { new WarningOnSingleLineCommentAnalyzer() },
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "// 0"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "// 1"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "// 2"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "// 3"));
        }

        [Fact(Skip = "Bug 896727")]
        public void TestSuppressSyntaxDiagnosticsInNamespaceDeclarationBasic()
        {
            VerifyBasic(@"
' 0
<assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Syntax"", Scope:=""Namespace"", Target:=""A"")>
' 1
Namespace A
' 2
    Namespace B 
    ' 3
    End Namespace
End Namespace
",
                new[] { new WarningOnSingleLineCommentAnalyzer() },
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "' 0"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "' 1"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "' 2"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "' 3"));
        }

        [Fact]
        public void TestSuppressSyntaxDiagnosticsInTypeDeclarationCSharp()
        {
            VerifyCSharp(@"
// 0
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Syntax"")]
// 1
public class C
{
    // 2
    public void Foo() // 3
    {
        // 4
    }
} // 5
",
                new[] { new WarningOnSingleLineCommentAnalyzer() },
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "// 0"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "// 5"));
        }

        [Fact(Skip = "Bug 896727")]
        public void TestSuppressSyntaxDiagnosticsInTypeDeclarationBasic()
        {
            VerifyBasic(@"
' 0
<System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Syntax"")>
' 1
Public Class C
    ' 2
    Public Sub Foo() ' 3
        ' 4
    End Sub
End Class ' 5
",
                new[] { new WarningOnSingleLineCommentAnalyzer() },
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "' 0"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "' 5"));
        }

        [Fact]
        public void TestSuppressSyntaxDiagnosticsInMemberDeclarationsCSharp()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

public class C
{
    // 0
    [SuppressMessage(""Test"", ""Syntax"")]
    // 1
    int x;
    // 2

    [SuppressMessage(""Test"", ""Syntax"")]
    // 3
    public void Foo() // 4
    {
        // 5
    }

    // 6
}
",
                new[] { new WarningOnSingleLineCommentAnalyzer() },
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "// 0"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "// 1"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "// 2"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "// 6"));
        }

        [Fact(Skip = "Bug 896727")]
        public void TestSuppressSyntaxDiagnosticsInMemberDeclarationsBasic()
        {
            VerifyBasic(@"
Imports System.Diagnostics.CodeAnalysis

Public Class C
    ' 0
    <SuppressMessage(""Test"", ""Syntax"")> ' 1
    Dim x As Integer ' 2

    <SuppressMessage(""Test"", ""Syntax"")>
    Public Sub Foo() ' 4
        ' 5
    End Sub

    ' 6
End Class
",
                new[] { new WarningOnSingleLineCommentAnalyzer() },
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "' 0"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "' 1"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "' 2"),
                GetResult(WarningOnSingleLineCommentAnalyzer.Id, "' 6"));
        }

        [Fact]
        public void TestSuppressMessageOnAnalysisCompleted()
        {
            VerifyCSharp(
                @"[module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""AnalysisCompleted"")]",
                new[] { new WarningOnAnalysisCompletedAnalyzer() });
        }

        [Fact]
        public void TestSuppressMessageOnTypeDeclaration()
        {
            VerifyCSharp(@"
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""TypeDeclaration"")]
public class C
{
}",
                new[] { new WarningOnTypeDeclarationAnalyzer() });
        }

        [Fact]
        public void TestSuppressMessageOnPropertyAccessors()
        {
            VerifyCSharp(@"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"")]
    public string P { get; private set; }
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("get_") });
        }

        [Fact]
        public void TestSuppressMessageOnDelegateInvoke()
        {
            VerifyCSharp(@"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"")]
    delegate void D();
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("Invoke") });
        }

        [Fact]
        public void TestSuppressMessageOnCodeBodyCSharp()
        {
            VerifyCSharp(
                @"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""CodeBody"")]
    void Foo()
    {
        Foo();
    }
}
",
                new[] { new WarningOnCodeBodyAnalyzer(LanguageNames.CSharp) });
        }

        [Fact(Skip = "Bug 896727")]
        public void TestSuppressMessageOnCodeBodyBasic()
        {
            VerifyBasic(
                @"
Public Class C
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""CodeBody"")]
    Sub Foo()
        Foo()
    End Sub
End Class
",
                new[] { new WarningOnCodeBodyAnalyzer(LanguageNames.VisualBasic) });
        }

        [Fact]
        public void TestUnnecessaryScopeAndTarget()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[SuppressMessage(""Test"", ""Declaration"", Scope=""Type"")]
public class C1
{
}

[SuppressMessage(""Test"", ""Declaration"", Target=""C"")]
public class C2
{
}

[SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""C"")]
public class C3
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") });
        }

        [Fact]
        public void TestInvalidScopeOrTarget()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Class"", Target=""C"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Class"", Target=""E"")]

public class C
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") },
                GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "C"));
        }

        [Fact]
        public void TestMissingScopeOrTarget()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage(""Test"", ""Declaration"", Target=""C"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"")]

public class C
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") },
                GetResult(WarningOnNamePrefixDeclarationAnalyzer.Id, "C"));
        }

        private static void VerifyCSharp(string source, params IDiagnosticAnalyzer[] analyzers)
        {
            Verify(source, LanguageNames.CSharp, analyzers, new DiagnosticDescription[0]);
        }

        private static void VerifyCSharp(string source, IDiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] diagnostics)
        {
            Verify(source, LanguageNames.CSharp, analyzers, diagnostics);
        }

        private static void VerifyBasic(string source, params IDiagnosticAnalyzer[] analyzers)
        {
            Verify(source, LanguageNames.VisualBasic, analyzers, new DiagnosticDescription[0]);
        }

        private static void VerifyBasic(string source, IDiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] diagnostics)
        {
            Verify(source, LanguageNames.VisualBasic, analyzers, diagnostics);
        }

        private static void Verify(string source, string language, IDiagnosticAnalyzer[] analyzers, DiagnosticDescription[] diagnostics)
        {
            var compilation = CreateCompilation(source, language, analyzers);
            compilation.VerifyAnalyzerDiagnostics(analyzers, diagnostics);
        }

        private static Compilation CreateCompilation(string source, string language, IDiagnosticAnalyzer[] analyzers)
        {
            string fileName = language == LanguageNames.CSharp ? "Test.cs" : "Test.vb";
            string projectName = "TestProject";

            var syntaxTree = language == LanguageNames.CSharp ?
                CSharpSyntaxTree.ParseText(source, fileName) :
                VisualBasicSyntaxTree.ParseText(source, fileName);

            if (language == LanguageNames.CSharp)
            {
                return CSharpCompilation.Create(
                    projectName,
                    syntaxTrees: new[] { syntaxTree },
                    references: new[] { TestBase.MscorlibRef });
            }
            else
            {
                return VisualBasicCompilation.Create(
                    projectName,
                    syntaxTrees: new[] { syntaxTree },
                    references: new[] { TestBase.MscorlibRef });
            }
        }

        private static DiagnosticDescription GetResult(string id, string squiggledText)
        {
            return new DiagnosticDescription(id, false, squiggledText, null, null, null, false);
        }
    }
}
